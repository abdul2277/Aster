"""Aster / Air Defense Alerts: public advisory proximity monitoring, Python 3.11+."""
from __future__ import annotations
import argparse
from concurrent.futures import ThreadPoolExecutor, as_completed
from dataclasses import asdict, dataclass
from datetime import datetime, timezone
from email.utils import parsedate_to_datetime
import hashlib
import json
import logging
import math
import os
from pathlib import Path
import re
import sqlite3
import time
import urllib.error
import urllib.parse
import urllib.request
import xml.etree.ElementTree as ET
import sys
sys.path.insert(0, str(Path(__file__).resolve().parent))
from telegram_monitor import channel_url, load_places, parse_preview

LOG = logging.getLogger("aster.air_defense")
MAX_BYTES = 2 * 1024 * 1024
UTC = timezone.utc


def number(value, low, high):
    if isinstance(value, bool) or value is None:
        raise ValueError("Missing or invalid number")
    value = float(value)
    if not math.isfinite(value) or not low <= value <= high:
        raise ValueError("Number out of range")
    return value


def coordinates(lat, lon):
    return number(lat, -90, 90), number(lon, -180, 180)


def haversine_km(lat1, lon1, lat2, lon2):
    """Great-circle distance on a mean-radius Earth; does not predict movement."""
    lat1, lon1 = coordinates(lat1, lon1)
    lat2, lon2 = coordinates(lat2, lon2)
    a, b = math.radians(lat1), math.radians(lat2)
    dlat, dlon = b - a, math.radians(lon2 - lon1)
    h = math.sin(dlat / 2) ** 2 + math.cos(a) * math.cos(b) * math.sin(dlon / 2) ** 2
    return 6371.0088 * 2 * math.asin(math.sqrt(max(0, min(1, h))))


def timestamp(value):
    if not isinstance(value, str) or not value.strip():
        raise ValueError("Explicit timestamp required")
    try:
        dt = datetime.fromisoformat(value.replace("Z", "+00:00"))
    except ValueError:
        dt = parsedate_to_datetime(value)
    if dt.tzinfo is None:
        raise ValueError("Timezone required")
    return dt.timestamp()


def clean(value, limit=300):
    return " ".join(str(value or "").split())[:limit]


def https_url(value):
    p = urllib.parse.urlsplit(value)
    if p.scheme != "https" or not p.hostname or p.username or p.password or p.fragment:
        raise ValueError("An HTTPS URL without user information or fragment is required")
    return value


@dataclass(frozen=True)
class Event:
    source: str
    id: str
    title: str
    lat: float
    lon: float
    published: float
    expires: float | None = None
    severity: str = "unspecified"
    url: str = ""

    def key(self):
        # Timestamp-only feed refreshes are not new alerts. Material revisions are.
        identity = [self.source, self.id, self.title, self.lat, self.lon, self.severity]
        return hashlib.sha256(json.dumps(identity, ensure_ascii=True).encode()).hexdigest()


def event_from(row, source):
    lat, lon = coordinates(row.get("latitude"), row.get("longitude"))
    identity = clean(row.get("id"), 512)
    title = clean(row.get("title"))
    if not identity or not title:
        raise ValueError("Stable ID and title required")
    link = clean(row.get("url"), 1000)
    if link:
        try:
            https_url(link)
        except ValueError:
            link = ""
    return Event(source, identity, title, lat, lon, timestamp(row.get("published")),
                 timestamp(row["expires"]) if row.get("expires") else None,
                 clean(row.get("severity") or "unspecified", 40), link)


def parse_feed(data, source):
    """Explicit adapters only. Never guess an incident's coordinates from article text."""
    if len(data) > MAX_BYTES:
        raise ValueError("Feed too large")
    kind = source["format"]
    rows = []
    if kind in ("json", "geojson"):
        document = json.loads(data)
        if kind == "json":
            rows = document["alerts"]
        else:
            if document.get("type") != "FeatureCollection":
                raise ValueError("Expected GeoJSON FeatureCollection")
            for feature in document["features"]:
                try:
                    geometry = feature["geometry"]
                    if geometry["type"] != "Point":
                        rows.append({})  # Areas cannot be replaced with a guessed center.
                        continue
                    lon, lat = geometry["coordinates"][:2]
                    props = dict(feature.get("properties") or {})
                    props.update(id=feature.get("id", props.get("id")), latitude=lat, longitude=lon)
                    rows.append(props)
                except (KeyError, TypeError, ValueError, IndexError):
                    rows.append({})
    elif kind == "rss":
        # UTF-8 only, forbid entity/DTD declarations before parsing.
        text = data.decode("utf-8-sig")
        if "<!DOCTYPE" in text.upper() or "<!ENTITY" in text.upper() or "\x00" in text:
            raise ValueError("DTD/entity declarations are not supported")
        root = ET.fromstring(text)
        atom = "{http://www.w3.org/2005/Atom}"
        if root.tag == "rss":
            items = root.findall("./channel/item")
        elif root.tag == atom + "feed":
            items = root.findall(atom + "entry")
        else:
            raise ValueError("Expected RSS 2.0 or Atom")
        for item in items:
            def field(*names):
                for name in names:
                    value = item.findtext(name)
                    if value:
                        return value
                return None
            point = field("{http://www.georss.org/georss}point")
            try:
                if point:
                    lat, lon = point.split()
                else:
                    lat = field("{http://www.w3.org/2003/01/geo/wgs84_pos#}lat")
                    lon = field("{http://www.w3.org/2003/01/geo/wgs84_pos#}long")
                link = field("link")
                if not link:
                    link = next((a.get("href") for a in item.findall(atom + "link")
                                 if a.get("rel", "alternate") == "alternate"), "")
                rows.append(dict(id=field("guid", atom + "id") or link,
                                 title=field("title", atom + "title"), latitude=lat, longitude=lon,
                                 published=field("pubDate", atom + "published", atom + "updated"), url=link))
            except ValueError:
                rows.append({})
    else:
        raise ValueError("Unsupported adapter")
    if not isinstance(rows, list) or len(rows) > 5000:
        raise ValueError("Expected at most 5000 alerts")
    events, skipped = [], 0
    for row in rows:
        try:
            events.append(event_from(row, source["name"]))
        except (ValueError, TypeError, KeyError, AttributeError, OverflowError):
            skipped += 1
    return events, skipped


class NoRedirect(urllib.request.HTTPRedirectHandler):
    def redirect_request(self, req, fp, code, msg, headers, newurl):
        return None  # Avoid leaking authorization to a redirected host.


class DeliveryError(Exception):
    def __init__(self, retry_after=30):
        super().__init__("Delivery was not confirmed")
        self.retry_after = min(86400, max(1, retry_after))


def request_bytes(url, *, headers=None, body=None, limit=MAX_BYTES):
    https_url(url)
    req = urllib.request.Request(url, data=body, headers=headers or {})
    with urllib.request.build_opener(NoRedirect()).open(req, timeout=10) as response:
        payload = response.read(limit + 1)
        if len(payload) > limit:
            raise ValueError("Response too large")
        return payload


def fetch(source):
    if source["format"] == "telegram":
        places = load_places(Path(__file__).resolve().parent / "places.json")
        payload = request_bytes(channel_url(source["url"]), headers={"User-Agent": "Aster-Public-Alert-Monitor/1.0", "Accept-Encoding": "identity"})
        rows, skipped = parse_preview(payload, source, places)
        events = []
        for row in rows:
            try:
                events.append(event_from(row, source["name"]))
            except (ValueError, TypeError, KeyError, OverflowError):
                skipped += 1
        return events, skipped
    headers = {"User-Agent": "Aster-Public-Alert-Monitor/1.0", "Accept-Encoding": "identity"}
    if source.get("bearer_token_env"):
        headers["Authorization"] = "Bearer " + os.environ[source["bearer_token_env"]]
    return parse_feed(request_bytes(source["url"], headers=headers), source)


class Telegram:
    def __init__(self, token, chat_id, transport=request_bytes):
        if not re.fullmatch(r"[0-9]+:[A-Za-z0-9_-]+", token):
            raise ValueError("Invalid Telegram token format")
        self.url = "https://api.telegram.org/bot" + token + "/sendMessage"
        self.chat_id, self.transport = chat_id, transport

    def send(self, message):
        body = json.dumps({"chat_id": self.chat_id, "text": message,
                           "disable_notification": False,
                           "link_preview_options": {"is_disabled": True}}).encode()
        try:
            result = json.loads(self.transport(self.url, headers={"Content-Type": "application/json"},
                                               body=body, limit=65536))
            if result.get("ok") is not True:
                delay = result.get("parameters", {}).get("retry_after", 30)
                raise DeliveryError(float(delay))
        except urllib.error.HTTPError as exc:
            delay = 30
            if exc.code == 429:
                try:
                    delay = float(json.loads(exc.read(65536))["parameters"]["retry_after"])
                except (ValueError, KeyError, TypeError):
                    pass
            raise DeliveryError(delay) from None
        except DeliveryError:
            raise
        except Exception:
            # Never surface exception URLs: the Telegram URL contains the secret.
            raise DeliveryError() from None


class Store:
    def __init__(self, path):
        self.db = sqlite3.connect(path)
        self.db.execute("PRAGMA journal_mode=WAL")
        self.db.execute("""CREATE TABLE IF NOT EXISTS alerts (
            key TEXT PRIMARY KEY, event TEXT NOT NULL, expires REAL NOT NULL,
            sent INTEGER NOT NULL DEFAULT 0, attempts INTEGER NOT NULL DEFAULT 0,
            due REAL NOT NULL DEFAULT 0)""")
        self.db.commit()

    def enqueue(self, event, expires):
        self.db.execute("INSERT OR IGNORE INTO alerts(key,event,expires) VALUES(?,?,?)",
                        (event.key(), json.dumps(asdict(event)), expires))
        self.db.commit()

    def prune(self, now, retention):
        self.db.execute("DELETE FROM alerts WHERE expires < ?", (now - retention,))
        self.db.commit()

    def close(self):
        self.db.close()


def eligible(event, cfg, now):
    age = now - event.published
    if age < -120 or age > cfg["max_age_seconds"]:
        return False
    if event.expires is not None and event.expires <= now:
        return False
    return haversine_km(*cfg["home"], event.lat, event.lon) <= cfg["radius_km"]


def notification(event, cfg):
    distance = haversine_km(*cfg["home"], event.lat, event.lon)
    when = datetime.fromtimestamp(event.published, UTC).isoformat()
    # Home coordinates never leave the machine. Event coordinates are public source data.
    return ("ASTER · AIR DEFENSE FEED\nNearby public report — not independently verified\n"
            f"{event.title}\nSource: {event.source}\nSeverity: {event.severity}\n"
            f"Distance: approximately {distance:.1f} km (radius {cfg['radius_km']:g} km)\n"
            f"Reported location: {event.lat:.5f}, {event.lon:.5f}\nPublished: {when}\n"
            f"{event.url}\nFollow official local emergency instructions.")


def drain(store, cfg, sender, now, clock=None):
    """Retry persisted pending alerts; only mark sent after Telegram confirms acceptance."""
    rows = store.db.execute("SELECT key,event,attempts FROM alerts WHERE sent=0 AND due<=? AND expires>? ORDER BY expires LIMIT 20", (now, now)).fetchall()
    for key, raw, attempts in rows:
        now = clock() if clock else now
        event = Event(**json.loads(raw))
        if not eligible(event, cfg, now):
            continue
        try:
            sender.send(notification(event, cfg))
        except DeliveryError as exc:
            backoff = max(exc.retry_after, min(900, 5 * 2 ** min(attempts, 8)))
            store.db.execute("UPDATE alerts SET attempts=attempts+1,due=? WHERE key=?", (now + backoff, key))
            store.db.commit()
            LOG.warning("Notification delivery unconfirmed; retry scheduled. No all-clear implied.")
            return now + backoff  # Honor rate limits globally for this sender.
        store.db.execute("UPDATE alerts SET sent=1 WHERE key=?", (key,))
        store.db.commit()
        LOG.info("Telegram accepted nearby alert %s", key[:12])
    return now


def load_config(path):
    cfg = json.loads(Path(path).read_text(encoding="utf-8-sig"))
    section = cfg["air_defense_feed"]
    section["home"] = coordinates(os.environ[section["home_lat_env"]], os.environ[section["home_lon_env"]])
    section["radius_km"] = number(section.get("radius_km", 20), .1, 1000)
    section["poll_seconds"] = number(section.get("poll_seconds", 30), 5, 3600)
    section["max_age_seconds"] = number(section.get("max_age_seconds", 900), 30, 86400)
    sources = section["sources"]
    if not isinstance(sources, list) or not 1 <= len(sources) <= 20:
        raise ValueError("Configure 1 to 20 sources")
    names = set()
    for source in sources:
        name = source["name"]
        if not isinstance(name, str) or not re.fullmatch(r"[A-Za-z0-9 _.-]{1,60}", name) or name in names:
            raise ValueError("Source names must be unique simple labels")
        names.add(name)
        if source["format"] == "telegram":
            source["url"] = channel_url(source["url"])
            if section["poll_seconds"] < 30:
                raise ValueError("Telegram preview polling requires at least 30 seconds")
        https_url(source["url"])
        if urllib.parse.urlsplit(source["url"]).hostname.endswith(".invalid"):
            raise ValueError("Replace example URLs with real provider URLs")
        if source["format"] not in ("json", "geojson", "rss", "telegram"):
            raise ValueError("Unknown source adapter")
        if source.get("bearer_token_env") and not os.environ.get(source["bearer_token_env"]):
            raise ValueError("Missing source credential environment variable")
    return section


class RunLock:
    """OS-released lock prevents two instances sharing a state file from double sending."""
    def __init__(self, path):
        self.file = open(str(path) + ".lock", "a+b")
        self.file.seek(0)
        self.file.write(b"0")
        self.file.flush()
        self.file.seek(0)
        try:
            if os.name == "nt":
                import msvcrt
                msvcrt.locking(self.file.fileno(), msvcrt.LK_NBLCK, 1)
            else:
                import fcntl
                fcntl.flock(self.file, fcntl.LOCK_EX | fcntl.LOCK_NB)
        except OSError:
            self.file.close()
            raise ValueError("Another monitor is already using this state file") from None

    def close(self):
        self.file.close()


def run(cfg, store, sender, once=False, dry_run=False):
    next_send = 0
    with ThreadPoolExecutor(max_workers=4) as pool:
        while True:
            started = time.monotonic()
            futures = {pool.submit(fetch, s): s for s in cfg["sources"]}
            for future in as_completed(futures):
                source = futures[future]
                try:
                    events, skipped = future.result()
                except Exception:
                    LOG.error("Source %s unavailable or invalid. Monitoring coverage degraded.", source["name"])
                    continue
                now = time.time()
                count = 0
                for event in events:
                    if eligible(event, cfg, now):
                        expiry = min(event.published + cfg["max_age_seconds"], event.expires or float("inf"))
                        store.enqueue(event, expiry)
                        count += 1
                LOG.info("Source %s: %d parsed, %d rejected, %d fresh nearby reports", source["name"], len(events), skipped, count)
                if not dry_run and now >= next_send:
                    next_send = drain(store, cfg, sender, now, clock=time.time)
            now = time.time()
            if dry_run:
                count = store.db.execute("SELECT COUNT(*) FROM alerts WHERE expires>?", (now,)).fetchone()[0]
                LOG.info("DRY RUN: %d unique nearby alerts; nothing sent", count)
            elif now >= next_send:
                next_send = drain(store, cfg, sender, now, clock=time.time)
            store.prune(now, max(86400, cfg["max_age_seconds"]))
            if once:
                return
            time.sleep(max(0.1, cfg["poll_seconds"] - (time.monotonic() - started)))


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--config", default="config.json")
    parser.add_argument("--once", action="store_true")
    parser.add_argument("--dry-run", action="store_true", help="Fetch/validate, never notify or alter production state")
    args = parser.parse_args()
    logging.basicConfig(level=logging.INFO, format="%(asctime)s %(levelname)s %(message)s")
    store = lock = None
    try:
        cfg = load_config(args.config)
        sender = None if args.dry_run else Telegram(os.environ[cfg["telegram_token_env"]], os.environ[cfg["telegram_chat_env"]])
        state = Path(args.config).resolve().parent / cfg.get("state_file", "air_defense.sqlite3")
        if not args.dry_run:
            lock = RunLock(state)
        store = Store(":memory:" if args.dry_run else state)
        run(cfg, store, sender, args.once, args.dry_run)
    except KeyboardInterrupt:
        LOG.info("Monitor stopped")
    except Exception as exc:
        LOG.error("Monitor stopped (%s). Check configuration, environment variables, and state-file access. Secrets omitted.", type(exc).__name__)
        return 1
    finally:
        if store:
            store.close()
        if lock:
            lock.close()
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
