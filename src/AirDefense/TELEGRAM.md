# Telegram channel monitoring in Aster v20

Aster now accepts public Telegram channels alongside RSS, GeoJSON and JSON feeds. It fetches public **HTML previews** at `https://t.me/s/<username>`; there is no assumed public t.me JSON API. This implementation uses Python's standard library, not Telethon, and requires no Telegram user login, API ID or session file. Outgoing phone notifications still use your own Telegram bot and chat ID.

## In Aster

1. Open **Tools hub → Air Defense feed**.
2. Add a feed row with a unique source name, select **telegram**, and enter `@channelname`, `channelname`, `https://t.me/channelname`, or `https://t.me/s/channelname`.
3. Leave polling at 30 seconds or longer. Your existing home preset and radius are used.
4. Select **Check feeds (no sends)** and inspect the results. Then use **Start alerts** when your Telegram bot details are entered.

No channels are added or monitored automatically. Private groups, invitations and individual post URLs are rejected. The existing HTTPS field accepts usernames only when telegram is selected. Public previews are visible on [Telegram's own channel](https://t.me/s/telegram); that channel is a format reference, not a threat feed or a preconfigured source.

## Extracting locations

The adapter extracts visible message text, the channel/message identifier, the post's publication timestamp, and the source post URL. It recognizes:

- Decimal `latitude, longitude`, separated by comma, semicolon or slash. Unlabelled values need at least two decimal places to reduce unrelated-number matches.
- Labels such as `latitude: 24.12 longitude: 46.34` or `lat=24.12, lon=46.34`.
- Decimal degrees with compass directions, such as `24.12 N, 46.34 E` or `24 S, 46 W`.
- Arabic decimal digits and Arabic decimal/comma punctuation.
- Place names and aliases explicitly configured in the local dictionary described below.

Coordinate values are validated against legal ranges. Explicit valid coordinates take precedence over place names. Up to 20 unique points per post are considered. No DMS conversion, OCR, photo/video analysis, map-link expansion, automatic geocoding or inferred city coordinates are performed. Posts without usable locations or timestamps are skipped and counted.

Every extracted point becomes an ordinary Aster event and passes through the existing **Haversine → radius → age/expiry → persistent deduplication → Telegram retry queue**. Outgoing messages include a source post link and a label identifying coordinate extraction or approximate place-name matching. They are marked unverified channel reports.

## Place dictionary

Edit **AirDefense/places.json** beside the script. It starts empty so Aster does not silently assign guessed coordinates to names. Add locations you have checked yourself, using a distinct canonical name and aliases in any language:

```json
{
  "places": [
    {
      "name": "Example District",
      "aliases": ["Example Neighborhood", "حي المثال"],
      "latitude": 0.0,
      "longitude": 0.0
    }
  ]
}
```

The names and coordinates above are fictional placeholders; replace them before use. A name's coordinate is a **representative point**, not an incident's verified location or an affected area's boundary. Matching a place name does not establish that the reported threat occurred there. Radius classification near boundaries can be wrong for broad areas. Add sufficiently specific names rather than ambiguous short words.

Matching is Unicode-normalized and case-insensitive with word boundaries. Longer overlapping names take precedence. Duplicate normalized aliases are rejected instead of choosing one location arbitrarily. The file is reloaded each poll, so edits apply without restarting. Invalid dictionaries make Telegram source reads fail visibly instead of silently ignoring your rules. Up to 1,000 places and a 256 KiB dictionary are supported. Do not distribute the personalized home-location.json if you intend to share a generic copy of Aster.

## Python entry points

**telegram_monitor.py** is both the adapter and a standalone command-line tool. The standalone tool uses the same core calculator and sender as Aster. From the package root:

```powershell
# Set ASTER_HOME_LAT and ASTER_HOME_LON in the process environment first.
# Default is ONE no-send check; replace the illustrative username.
.\runtime\python.exe -I -B .\AirDefense\telegram_monitor.py @channelname --radius 20

# Continuous delivery also needs ASTER_TELEGRAM_TOKEN and ASTER_TELEGRAM_CHAT_ID.
.\runtime\python.exe -I -B .\AirDefense\telegram_monitor.py @channelname --send --poll 30 --radius 20
```

The GUI automatically passes the selected coordinates to its worker; the standalone CLI requires those environment variables. For custom backend configuration, add this source to `air_defense_feed.sources`:

```json
{"name": "My public channel", "format": "telegram", "url": "@channelname"}
```

Adapter functions: `channel_url`, `locations`, `load_places`, `parse_preview`. Integration occurs in `air_defense.fetch`. No message content is executed or interpreted as instructions.

## Reliability limits

This is polling of a changing public webpage, not Telethon's real-time message stream. Only currently exposed recent posts are read; there is no historical pagination or catch-up cursor. Posts can be missed during an outage, high-volume bursts, preview restrictions, or HTML changes. An unavailable/unrecognized preview is reported as degraded coverage. Deleted posts, retractions, drills, negation, hearsay, forwarded context and conflicting reports are not interpreted automatically. A nearby location mentioned in ordinary news can trigger an alert too: choose focused advisory channels and review original posts.

Previously unseen recent posts can alert on first start. The default age limit is 15 minutes. Same-post material text/location changes can alert again, while unchanged posts are deduplicated. Different channels may produce separate notifications for the same incident. Telegram acceptance does not guarantee phone presentation, and uncertain delivery retries can duplicate messages. The monitor neither verifies threats nor issues an all-clear; keep using official emergency warning systems.

Validation: both Windows programs compile. **31 offline Python tests** pass, including preview parsing, coordinate/Arabic-digit handling, aliases, malformed/unknown sources, invalid timestamps, Haversine filtering and delivery deduplication. Native UI checks passed for Telegram selection, username normalization, private invite rejection and the polling minimum. Parser tests use synthetic HTML fixtures; live extraction against your channels and actual phone delivery have not been tested. No channel notifications were sent during development.
