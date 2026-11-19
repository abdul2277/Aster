# Windows app quick start

Open **Aster.exe → Tools hub → Air Defense feed**. All controls are inside Aster. Use **Use my location**, then enter real feed URLs and Telegram details. Switching pages keeps monitoring active; Stop or closing Aster stops it. Windows controls location access and availability. The Python instructions below are for developers only; the runtime is bundled.

# Aster — Air Defense Alerts backend

Standalone Python 3.11+ module, separate from the Aster desktop suite. No third-party packages. This package provides backend proximity and phone-alert logic; it does not install a desktop page, service, or background task automatically.

## What works

- Parallel polling of up to 20 configured HTTPS feeds, with four fetch workers.
- RSS 2.0 / Atom with GeoRSS Simple points or W3C latitude/longitude, GeoJSON Point collections, and the structured JSON contract below.
- Validated latitude/longitude, timezone-aware timestamps, and Haversine great-circle distance. The threshold is inclusive: distance <= radius (default 20 km).
- Nearby reports are queued and sent through Telegram `sendMessage` after receipt, with source, report coordinates, approximate distance, publication time, severity when supplied, and source link.
- SQLite queue survives restarts. Confirmed notifications are deduplicated; failures are retried with backoff, including Telegram's `retry_after`. Expired pending reports are not delivered.
- Dry run fetches and evaluates but sends nothing and does not alter the production database. Invalid records are counted, feed failures are visible, and one broken source does not prevent others from being processed.
- Home location and credentials are read from environment variables. Home GPS coordinates are not sent to feeds or Telegram. Distance and public report location are included in Telegram messages, so recipients may infer your approximate area.

## Set up locally

1. Install Python 3.11 or later. Copy `config.example.json` to `config.json`.
2. In the separate `air_defense_feed` configuration section, replace example URLs with the public providers you actually use. Remove unused sources. Example URLs intentionally fail validation; they are not real services. Use stable, unique source names.
3. Choose `rss`, `geojson`, or `json` per source. Provider-specific APIs must be normalized to the documented contract. For a bearer-authenticated API, add `"bearer_token_env": "ASTER_PROVIDER_TOKEN"` to that source and set that environment variable. Redirects are rejected; configure the final HTTPS endpoint.
4. Create a Telegram bot using the official BotFather, start a private conversation with it, and obtain your own destination chat ID. Enter values locally; do not paste credentials into chat or commit them to source control.
5. Open PowerShell in the extracted module folder and set values for the current session:

```powershell
$env:ASTER_HOME_LAT = Read-Host 'Home latitude (-90 to 90)'
$env:ASTER_HOME_LON = Read-Host 'Home longitude (-180 to 180)'
$env:ASTER_TELEGRAM_CHAT_ID = Read-Host 'Your Telegram chat ID'
$asterSecret = Read-Host 'Telegram bot token' -AsSecureString
$env:ASTER_TELEGRAM_TOKEN = [System.Net.NetworkCredential]::new('', $asterSecret).Password
python air_defense.py --config config.json --dry-run --once
```

After verifying the feed formats and dry-run counts, start continuous delivery:

```powershell
python air_defense.py --config config.json
```

Stop with Ctrl+C. `--once` performs one polling pass; queued failures need a later run to retry. No Telegram token/chat ID is needed for dry runs. Keep the process, internet connection and computer running; sleeping or closing the terminal stops monitoring. Set the polling interval to comply with each provider's usage policy. The default is 30 seconds, minimum 5 seconds. Requests have a 10-second socket timeout and a 2 MiB response limit. RSS is UTF-8 only.

Telegram bot notifications use the outbound `sendMessage` API, not a Telegram inbound webhook. No public server or webhook receiver is required. Telegram acceptance does not prove that a phone displayed or sounded the notification; phone/network/Telegram notification settings still apply.

## Supported source contracts

### Structured JSON / normalized regional API

Top-level `alerts` list, at most 5,000 records:

```json
{
  "alerts": [
    {
      "id": "provider-stable-advisory-id",
      "title": "Public advisory title",
      "latitude": 24.7000,
      "longitude": 46.7000,
      "published": "2026-10-13T12:30:00+03:00",
      "expires": "2026-10-13T12:45:00+03:00",
      "severity": "provider-reported severity",
      "url": "https://provider.example/advisory"
    }
  ]
}
```

This is a schema example, not an actual incident. `id`, `title`, coordinates and `published` are required. `expires`, `severity` and `url` are optional. Date strings must include a timezone; no heuristic epoch-unit conversion is used. Numeric strings for coordinates are accepted, but non-finite or out-of-range values are rejected. A link is included only if it is valid HTTPS. Missing coordinates/timestamps are skipped rather than guessed.

### GeoJSON

Use a `FeatureCollection` of `Feature` records with `geometry.type = "Point"`. Coordinates are **[longitude, latitude]**, optionally followed by altitude (ignored). Use the fields above inside `properties`, except coordinates. The feature's `id` overrides `properties.id` when present. Publication time is still required in `properties.published`; GeoJSON itself does not standardize advisory timestamps.

Polygons, circles, bounding boxes, GML, CAP, city names and arbitrary regional API schemas are not converted to points. They require dedicated adapters and area-intersection logic. A polygon centroid is not a reliable substitute for whether your home lies in a warning area. No automatic geocoding, source scraping or geolocation of people is performed.

### RSS / Atom

Each item needs a GUID/Atom ID (or source link fallback), title, `pubDate` or Atom `published`/`updated`, and either:

```xml
<georss:point xmlns:georss="http://www.georss.org/georss">24.7 46.7</georss:point>
```

or W3C `geo:lat` and `geo:long` in namespace `http://www.w3.org/2003/01/geo/wgs84_pos#`. GeoRSS points are **latitude longitude**. Publication time takes precedence over update time. Ordinary news RSS without explicit locations is not proximity-capable.

## Event validity and delivery semantics

Only reports no older than `max_age_seconds` (default 900 seconds), no more than 120 seconds in the future, and not expired are eligible. Keep the machine clock accurate. Distance is to the reported point, not an impact footprint, predicted trajectory or exact ground distance. No inference is made about whether a report represents an approaching threat.

Configure dedicated public emergency/advisory feeds. The generic adapter treats every valid nearby record as a report; it does not authenticate the underlying claim, classify general news, recognize drills, process cancellations or infer an all-clear. Filter tests, withdrawals and non-alert items in a provider-specific adapter before using such a feed. Pending records may remain queued after disappearing from a source until expiry. Public reports and outages can both be delayed; absence of messages does not mean safety. This supplements official emergency warnings and is not a guaranteed life-safety or air-defense detection system.

Deduplication is per source and stable ID plus title, point and severity. A timestamp-only refresh does not alert again; a materially revised record can. Two providers reporting the same incident may produce two alerts. State is retained until one day after expiry (or the configured maximum age, whichever is longer). Deleting/changing the database resets deduplication. Use one monitor/state file per destination/configuration. An OS file lock prevents concurrent processes using the same database. Do not share it across machines.

Telegram offers no application idempotency key for this request: a network failure after Telegram accepted a message but before local confirmation can cause a duplicate retry. Only confirmed acceptance marks a record sent. Delivery is therefore retry-based, not guaranteed exactly-once. Pending batches are limited to 20 per drain, and retries never intentionally bypass expiration.

Normal logs omit coordinates, URLs and credentials. The local SQLite file contains public report data and delivery state, without encryption. Protect it with normal OS access controls. Startup/configuration errors are deliberately terse to avoid exposing credentials from exception text.

## Embedding in Aster

Use this as its own worker process and expose a separate **Air Defense feed** section in your app for configuration and monitor status. Keep the process out of the UI thread. The core import points are `haversine_km`, `parse_feed`, `eligible`, `notification`, `Store`, `Telegram`, and `run`. They also support a different sender object with a `send(message)` method that raises `DeliveryError` on unconfirmed delivery. This package implements Telegram only, not a separate mobile push service.

## Tests and validation

```powershell
python -B -m unittest discover -s . -p test_air_defense.py -v
```

19 offline tests cover known distances, the antimeridian, invalid coordinates, radius/age/expiry boundaries, timestamp offsets, JSON/GeoJSON/RSS/Atom parsing, malformed records, XML guards, persistent deduplication, retries, Telegram rate limits and payloads, source failure isolation, dry-run behavior and concurrent-instance locking. Tests use synthetic reports and mocked network responses. No real feeds were configured and no Telegram messages were sent during development. Set `ASTER_TEST_TEMP` to a writable test directory if your environment restricts the default temporary directory.

References: [Telegram Bot API / sendMessage](https://core.telegram.org/bots/api#sendmessage), [GeoJSON standard (RFC 7946)](https://www.rfc-editor.org/rfc/rfc7946), [GeoRSS overview](https://georss.org/).
