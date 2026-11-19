"""Public Telegram preview adapter for Aster. HTML previews, not an official JSON API."""
from __future__ import annotations
import argparse
from html.parser import HTMLParser
import hashlib
import json
import math
from pathlib import Path
import re
import sys
import unicodedata
from urllib.parse import urlsplit

USER = re.compile(r"[A-Za-z][A-Za-z0-9_]{3,31}\Z")
PAIR = re.compile(r"(?<![\w.\d+-])([+-]?\d{1,3}\.\d{2,9})\s*[,;/]\s*([+-]?\d{1,3}\.\d{2,9})(?![\w.\d])")
LABEL = re.compile(r"\b(?:lat|latitude)\s*[:=]\s*([+-]?\d{1,3}(?:\.\d+)?)\s*[,;/ ]+\s*(?:lon|long|longitude)\s*[:=]\s*([+-]?\d{1,3}(?:\.\d+)?)\b", re.I)
COMPASS = re.compile(r"(?<![\w.\d+-])(\d{1,2}(?:\.\d+)?)\s*°?\s*([NS])\s*[,;/ ]+\s*(\d{1,3}(?:\.\d+)?)\s*°?\s*([EW])\b", re.I)


def channel_url(value):
    value = str(value).strip()
    if value.startswith("https://"):
        p = urlsplit(value)
        if p.hostname != "t.me" or p.port is not None or p.username or p.password or p.query or p.fragment:
            raise ValueError("Use a public t.me channel URL or username")
        parts = p.path.strip("/").split("/")
        if len(parts) == 2 and parts[0] == "s":
            value = parts[1]
        elif len(parts) == 1:
            value = parts[0]
        else:
            raise ValueError("Expected channel, not a post/invite/private chat URL")
    else:
        value = value.removeprefix("@")
    if not USER.fullmatch(value):
        raise ValueError("Invalid public channel username")
    return "https://t.me/s/" + value.lower()


def normalized(text):
    result = []
    for char in unicodedata.normalize("NFKC", text):
        if char.isdecimal():
            result.append(str(unicodedata.decimal(char)))
        else:
            result.append({"٫": ".", "،": ","}.get(char, char))
    return "".join(result)


def valid_point(lat, lon):
    return math.isfinite(lat) and math.isfinite(lon) and -90 <= lat <= 90 and -180 <= lon <= 180


def load_places(path):
    path = Path(path)
    if not path.exists():
        return []
    if path.stat().st_size > 262144:
        raise ValueError("Place dictionary exceeds 256 KiB")
    data = json.loads(path.read_text(encoding="utf-8-sig"))
    places = data["places"]
    if not isinstance(places, list) or len(places) > 1000:
        raise ValueError("Expected at most 1000 places")
    used = set()
    for place in places:
        lat, lon = place["latitude"], place["longitude"]
        if isinstance(lat, bool) or isinstance(lon, bool) or not valid_point(float(lat), float(lon)):
            raise ValueError("Invalid place coordinates")
        names = [place["name"]] + place.get("aliases", [])
        for name in names:
            if not isinstance(name, str) or not 2 <= len(name) <= 100:
                raise ValueError("Invalid place name")
            name = normalized(name).casefold()
            if name in used:
                raise ValueError("Ambiguous duplicate place alias")
            used.add(name)
    return places


def locations(text, places):
    text = normalized(text)
    found = []
    for pattern in (LABEL, COMPASS, PAIR):
        for match in pattern.finditer(text):
            if pattern is COMPASS:
                lat = float(match[1]) * (-1 if match[2].upper() == "S" else 1)
                lon = float(match[3]) * (-1 if match[4].upper() == "W" else 1)
            else:
                lat, lon = float(match[1]), float(match[2])
            if valid_point(lat, lon) and (lat, lon) not in [(x[0], x[1]) for x in found]:
                found.append((lat, lon, "coordinates in channel text"))
    if found:
        return found[:20]  # Explicit points take precedence over place-name references.
    candidates = []
    for place in places:
        for name in [place["name"]] + place.get("aliases", []):
            name = normalized(name).casefold()
            for match in re.finditer(r"(?<!\w)" + re.escape(name) + r"(?!\w)", text.casefold()):
                candidates.append((match.start(), match.end(), place))
    accepted = []
    for start, end, place in sorted(candidates, key=lambda item: item[1] - item[0], reverse=True):
        if any(start < b and end > a for a, b in accepted):
            continue
        accepted.append((start, end))
        lat, lon = float(place["latitude"]), float(place["longitude"])
        if (lat, lon) not in [(x[0], x[1]) for x in found]:
            found.append((lat, lon, "approximate place reference: " + place["name"]))
    return found[:20]


class PreviewParser(HTMLParser):
    VOID = {"area", "base", "br", "col", "embed", "hr", "img", "input", "link", "meta", "param", "source", "track", "wbr"}

    def __init__(self):
        super().__init__(convert_charrefs=True)
        self.stack, self.posts = [], []
        self.current = None
        self.depth = self.text_depth = 0

    def handle_starttag(self, tag, attrs):
        attrs = dict(attrs)
        classes = set(attrs.get("class", "").split())
        if tag == "br" and self.text_depth and self.current:
            self.current["chunks"].append("\n")
        if tag in self.VOID:
            return
        self.stack.append((tag, classes))
        if "tgme_widget_message" in classes and attrs.get("data-post"):
            self.current = {"id": attrs["data-post"], "chunks": [], "published": None}
            self.depth = len(self.stack)
            self.text_depth = 0
        if self.current:
            if "tgme_widget_message_text" in classes:
                self.text_depth = len(self.stack)
            if tag == "time" and any("tgme_widget_message_date" in c for _, c in self.stack):
                self.current["published"] = attrs.get("datetime")

    def handle_startendtag(self, tag, attrs):
        self.handle_starttag(tag, attrs)
        if tag not in self.VOID:
            self.handle_endtag(tag)

    def handle_data(self, data):
        if self.current and self.text_depth and not any(t in {"script", "style"} for t, _ in self.stack):
            self.current["chunks"].append(data)

    def handle_endtag(self, tag):
        index = next((i for i in range(len(self.stack) - 1, -1, -1) if self.stack[i][0] == tag), None)
        if index is None:
            return
        if self.current:
            if self.text_depth and index < self.text_depth:
                self.text_depth = 0
            if index < self.depth:
                self.current["text"] = "".join(self.current.pop("chunks"))
                self.posts.append(self.current)
                self.current = None
        del self.stack[index:]


def parse_preview(payload, source, places=()):
    if len(payload) > 2 * 1024 * 1024:
        raise ValueError("Preview too large")
    parser = PreviewParser()
    parser.feed(payload.decode("utf-8"))
    parser.close()
    if not parser.posts:
        raise ValueError("No public posts recognized; channel unavailable or preview format changed")
    username = channel_url(source["url"]).rsplit("/", 1)[1]
    rows, skipped = [], 0
    for post in parser.posts:
        if not re.fullmatch(re.escape(username) + r"/\d+", post["id"], re.I) or not post["published"]:
            skipped += 1
            continue
        points = locations(post["text"], places)
        if not points:
            skipped += 1
        for lat, lon, method in points:
            point_id = hashlib.sha256(f"{lat},{lon}".encode()).hexdigest()[:16]
            rows.append({"id": post["id"] + ":" + point_id,
                         "title": "[" + method + "] " + " ".join(post["text"].split())[:220],
                         "latitude": lat, "longitude": lon, "published": post["published"],
                         "severity": "unverified channel report", "url": "https://t.me/" + post["id"]})
    return rows, skipped


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("channels", nargs="+", help="Public usernames, @usernames or https://t.me/s/name")
    parser.add_argument("--send", action="store_true", help="Send alerts; default is one no-send check")
    parser.add_argument("--radius", type=float, default=20)
    parser.add_argument("--poll", type=float, default=30)
    parser.add_argument("--max-age", type=float, default=900)
    parser.add_argument("--state", default="telegram-alerts.sqlite3")
    args = parser.parse_args()
    # Bundle isolation excludes sibling paths; explicitly load this installed module directory.
    sys.path.insert(0, str(Path(__file__).resolve().parent))
    import air_defense as core
    import logging
    import os
    logging.basicConfig(level=logging.INFO, format="%(asctime)s %(levelname)s %(message)s")
    store = lock = None
    try:
        home = core.coordinates(os.environ["ASTER_HOME_LAT"], os.environ["ASTER_HOME_LON"])
        cfg = {"home": home, "radius_km": core.number(args.radius, .1, 1000),
               "poll_seconds": core.number(args.poll, 30, 3600),
               "max_age_seconds": core.number(args.max_age, 30, 86400),
               "sources": [{"name": channel_url(c).rsplit("/", 1)[1], "format": "telegram", "url": channel_url(c)} for c in args.channels]}
        if not 1 <= len(cfg["sources"]) <= 20:
            raise ValueError("Use 1 to 20 channels")
        sender = core.Telegram(os.environ["ASTER_TELEGRAM_TOKEN"], os.environ["ASTER_TELEGRAM_CHAT_ID"]) if args.send else None
        if args.send:
            lock = core.RunLock(args.state)
        store = core.Store(args.state if args.send else ":memory:")
        core.run(cfg, store, sender, once=not args.send, dry_run=not args.send)
    except KeyboardInterrupt:
        logging.info("Monitor stopped")
    except Exception as exc:
        logging.error("Telegram monitor stopped (%s); check inputs, environment variables and state access.", type(exc).__name__)
        return 1
    finally:
        if store:
            store.close()
        if lock:
            lock.close()
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
