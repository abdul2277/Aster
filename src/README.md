# Aster v27 — online updates

Update Aster now checks abdul2277/Aster on GitHub. Choose Check for updates, then Download & install and Restart into update. No ZIP selection is needed in v27.

The release must first be published with its signed aster-update.json asset. Until then, checks show an unavailable-release message. The app verifies a pinned release signature, download hash, package version and file inventory. Downloads support cancellation. Installation preserves local user data and leaves the old application folder intact.

Validation used mocked network responses; live GitHub publishing and downloads were not performed. Future releases must be signed with the same maintainer key. The updater does not silently install or check in the background.

--- Previous release notes ---

# Aster v26

NEW IN THIS RELEASE

- Performance compare: collect Before and After windows of 20 consecutive samples; review average/peak CPU and RAM and export a comparison. Samples follow your refresh interval (roughly 20–100 seconds per window). Pauses, long gaps or invalid readings cancel an incomplete capture. Starting a new completed Before clears the previous After.
- Session timeline: latest 200 page visits, pause/resume events, captures, pressure alerts and your own markers. Export as text. History is session-only.
- Maintenance planner: up to 24 saved reminders, due dates, repeat intervals, completion, one-day snooze and links to maintenance tools. Recurring reminders schedule from the completion date. Reminders appear inside Aster; no background notifications or automatic repairs.
- Command Center includes a Plan & Review section and a due-reminder count.

Find all three pages in Command Center, Tools hub or Ctrl+K. Update using this ZIP in Update Aster, or extract the whole folder and open Aster.exe.

Validation: capture timing and cancellation, comparison calculations, reminder persistence and recurrence, timeline limits and six-page navigation passed. No Windows repairs or real notifications were performed.

--- Previous release notes ---

# Aster v25

Air Defense feed now has Alerts & settings and YouTube player tabs. Your saved video plays inside the feed section using the embedded YouTube player; no separate browser window is opened. Started monitoring keeps its existing state across tab changes. Playback stops when leaving the video tab or feed section.

YouTube controls advertising and availability. This is not an ad blocker or an ad-free player. The video is not analyzed for alerts. Streaming still depends on network access and the video's embed permissions.

Navigation and player disposal were tested; actual streaming was not verified in this environment.

Install this ZIP through Update Aster, or extract the whole folder and open Aster.exe.

--- Previous release notes ---

# Aster v24

New: Command Center, configurable local performance alerts, and a previewable diagnostics ZIP.

Open Aster.exe. Command Center is in the main sidebar; all three new pages also appear in Tools hub and Ctrl+K search.

Performance alerts are off by default. Open Performance alerts to enable and save thresholds, sustained duration and cooldown. Alerts appear in the status bar and session history while Aster is open and monitoring; this is not a background service or phone notification. No applications are stopped automatically.

Diagnostics exports exactly the two previewed files: report.txt and performance.csv, with up to 60 recent aggregate samples. It excludes identifying hardware details, process names, notes, location and credentials. Review before sharing.

Use Update Aster in v23 to install Aster-Windows-v24.zip, or extract the whole ZIP and open Aster.exe. Keep all bundled files together.

--- Previous feature documentation ---

# Aster + Aura — v23

Aster is the new name for Pulse. Open **Aster.exe** for the system workspace, or **AuraWallpaper.exe** for live wallpapers. Extract the entire folder and keep its files together. Close older copies before starting the new version.

Portable 64-bit Windows programs built with the Windows .NET Framework. Source and Build.cmd are included.

## New in v23 — Update Aster

Select **Update Aster** in the sidebar (also in Tools hub). Choose a newer downloaded Aster ZIP. Aster checks its release.json app identity, version, file inventory, SHA-256 hashes, archive size and paths. It shows the version, size and package hash before installation. Only versions newer than installed 23.0 are accepted. Older packages without release.json cannot be installed through this page.

Select **Install reviewed update** and confirm that you trust the package. File hashes detect corruption; they do not authenticate a publisher. These packages are not cryptographically signed and there is no online update server or automatic check. No internet connection is needed to install a downloaded ZIP.

A new version folder is created under %LOCALAPPDATA%\Aster\Versions. The existing program folder is left intact. Your local home-location.json and AirDefense/places.json are copied to the new version. A home preset supplied by the ZIP is never imported during an update. Workspace settings, notebooks, cases and other user data remain in their existing shared AppData locations. The updater does not migrate those data formats. If a future release changes them, its migration behavior must be reviewed separately.

After installation, **Switch to updated Aster** closes the old copy and launches the new one. Existing unsaved-work and stop-monitoring prompts are respected; canceling closure cancels the switch. Monitoring stops when the old app closes and must be restarted in the new app. Telegram credentials remain session-only. A failed install never replaces the running version and never launches its incomplete folder. Incomplete new folders may remain on disk for manual cleanup. Old shortcuts still point to the old folder; update them to the new Aster.exe if you want that version to be your usual launch target.

You can return to the previous program by closing the new version and opening Aster.exe from the previous folder. This is not a rollback of shared user data. Backup center remains available for workspace backups.

Validation: isolated tests passed for newer-version acceptance; same/older-version, corrupt-hash, traversal, duplicate, unlisted-file and symlink rejection; package-change detection between review and install; new-folder installation; preserving home/place files; not importing a different home preset; and keeping the old program unchanged. Fixture executables were not run. Both real Windows builds passed.

For future releases: update CurrentVersion in Updates.cs, rebuild with Build.cmd, then run `runtime\python.exe -B Make-Update.py`. The helper generates release.json with the current source version and file hashes and builds the update-ready ZIP. Rebuild before packaging so the executable and source version agree. Keep release.json in the downloaded archive. This v23 package includes its own manifest for future tooling; v23 itself will correctly reject reinstalling the same version.

## New in v22 — watch the video inside Aster

Open **Air Defense feed → Watch video in Aster**, or **Tools hub → Appearance & Play → Video player**. The supplied YouTube video now loads in a WebView2 player inside the same Aster window. Press Play in the embedded player. Back to alerts returns to your existing alert settings and monitoring. Navigating away disposes the video control and stops playback; returning starts a fresh player. Reload player retries failed loads. The app does not open an external browser or autoplay the video.

The player retains YouTube's controls and branding, with an on-page network/embedding error message. Captions and quality options depend on the video and YouTube. No downloading or rehosting is performed. Owner-disabled embedding, unavailable videos, authentication or regional restrictions can prevent playback. The linked video remains bNyUyrR0PHo. The local player page uses an HTTPS virtual host and an origin/referrer for YouTube's embed; camera, microphone, location and other web permission requests are denied. Popups and downloads are blocked.

Keep the Video folder and three WebView2 SDK DLLs beside Aster.exe. Microsoft Edge WebView2 Runtime must be installed (it was detected on this computer). The SDK is bundled under WEBVIEW2-LICENSE.txt; the browser runtime is not bundled. Runtime installation information: https://developer.microsoft.com/microsoft-edge/webview2 . Browser profile/cache is stored locally in %LOCALAPPDATA%\Aster\VideoBrowser. Only opening the player contacts YouTube; the local home preset and Telegram credentials are not passed to this page.

Validation: both Windows programs built. A native smoke check initialized the installed WebView2 runtime, loaded the packaged local HTTPS player page, displayed its network-failure state and confirmed disposal. This environment could not reach YouTube's iframe API, so actual playback and this video's embedding eligibility remain unverified. No claim of successful video playback is made.

Embedding reference: https://developers.google.com/youtube/player_parameters . This release replaces the browser shortcut described in v21.

## New in v21 — your YouTube link

The Air Defense feed page has an **Open YouTube video** button. Clicking it opens the exact link you supplied in your default browser: https://www.youtube.com/watch?v=bNyUyrR0PHo . Nothing plays automatically. The button is a shortcut; the video is not treated as an alert feed.

Validation: both Windows applications compiled after the change. No browser was opened during the build.

## New in v20 — public Telegram channel feeds

Inside **Tools hub → Air Defense feed**, select **telegram** as a feed format and enter a public channel username or public t.me channel URL. Public HTML previews are polled every 30 seconds or longer, without a Telegram user login. The bundled **AirDefense/telegram_monitor.py** extracts text coordinates or locally configured place aliases, then uses the existing Haversine filter and notification queue. Your home preset is retained.

Configure name-to-coordinate mappings in **AirDefense/places.json**; the dictionary starts empty. Matches are marked approximate and unknown names are skipped. See **AirDefense/TELEGRAM.md** for supported coordinate formats, setup, standalone script use and reliability limits. The public preview is not a real-time stream and only exposes recent posts; coverage can be incomplete. No channels are preselected.

Validation: both Windows builds passed, 31 offline Python tests passed, and native UI tests covered channel normalization, the new feed option, private-link rejection and polling limits. Live channel extraction and phone delivery remain unverified. Earlier release notes describe their respective versions; this release adds Telegram as a fourth source format.

## New in v19 — your supplied home location

The Air Defense feed page now loads your supplied latitude and longitude automatically from **home-location.json** beside Aster.exe. No Windows location lookup is needed for this preset. It remains the default on each launch; manual changes or Use my location replace it for the current session only. To permanently change it, edit the two values in home-location.json. Remove that file to return to manual/Windows location entry.

This personalized package contains your home coordinates in that local file. Telegram credentials remain session-only. The backend still sends only the public report location and approximate distance, not the home coordinates themselves. Feed URLs and Telegram details are still required; loading the preset does not start monitoring or notifications.

Validation: both executables compiled, and a native check confirmed that the supplied preset loads into the alert fields exactly. Earlier release notes describing session-only coordinates are superseded for this explicit local home preset.

## New in v18 — alerts inside Aster + Use my location

Open **Aster.exe → Tools hub → Monitoring → Air Defense feed**. The complete alert setup is embedded in Aster; no separate AsterAlerts.exe is needed or included. Select **Use my location** to ask Windows for a fresh location and fill latitude/longitude. If access is blocked, no provider is available, or no fresh fix arrives within 30 seconds, the page explains the issue; manual coordinates remain available.

Estimated accuracy is displayed when available. Fixes older than two minutes or more than 30 seconds in the future are ignored. This is a one-time reference point, not continuous tracking. Your current position is only your home position if you are at home. It remains fixed until changed. Stop monitoring before changing coordinates. Session coordinates and Telegram credentials are not saved to disk.

Switching Aster pages preserves fields and monitoring. Stop ends the worker. Closing Aster asks before ending an active monitor and releases the monitor and location lookup once closure completes. Keep Aster open for monitoring; it can be minimized. Real feed URLs and Telegram details are still required.

Validation: both Windows applications compiled. Simulated location checks covered autofill, accuracy, fresh/unknown/stale/future fixes and avoiding coordinate storage. Integration checks covered reusing the same control, retaining fields, an isolated dummy worker remaining active across page changes, Stop terminating it and disposal on application close. The embedded UI was visually inspected. Actual Windows location acquisition and live Telegram delivery were not tested.

Location provider reference: https://learn.microsoft.com/en-us/dotnet/api/system.device.location.geocoordinatewatcher?view=netframework-4.8.1

The notes below describe earlier releases; v18 replaces the separate-window workflow from v17.

## New in v17 — launchable Air Defense monitor

This package includes real Windows applications: **Aster.exe**, **AsterAlerts.exe**, and **AuraWallpaper.exe**. Extract the entire ZIP to a writable local folder. Open **Aster.exe**, then **Tools hub → Monitoring → Air Defense feed → Open alert monitor**. You can also open AsterAlerts.exe directly. Keep the runtime and AirDefense folders alongside the executables. Python is bundled, so no separate Python installation or command-line setup is required. This is a portable application, not an installer.

In the monitor window, enter latitude, longitude, radius, one or more actual public feed URLs and their formats. Check feeds performs a dry run without sending messages. To enable phone notifications, enter your own Telegram bot token and chat ID, then select Start alerts. Stop halts the worker. Closing the monitor asks to stop active monitoring; closing the main Aster window does not close the separate monitor.

Feed URLs and interval/radius settings are saved in %LOCALAPPDATA%\Aster\AirDefense\config.json. Home coordinates and Telegram credentials are session-only, passed to the worker through its environment, and need to be re-entered after closing. Do not put credentials in feed URLs. The GUI supports public unauthenticated feeds; advanced bearer-auth setup remains available through the backend configuration described in AirDefense/README.md. Existing Python CLI settings are not automatically migrated into this GUI.

No feeds are preconfigured and nothing starts automatically. The monitor supports the previously delivered GeoRSS/GeoJSON/normalized-JSON point adapters. Reports without valid location/time are skipped. Public reports may be delayed, unverified, revised or withdrawn; this does not replace official emergency alerts. See AirDefense/README.md for all adapter and delivery limitations. Source status and Telegram acceptance/failures appear in the monitor log.

Validation: all three Windows executables compiled; 19 offline backend tests passed using the bundled, isolated Python runtime. Native UI checks covered invalid coordinates, settings saving without session secrets, and rendering. The setup window was visually inspected. No live feeds or Telegram delivery were exercised. The bundled Python license is in runtime/LICENSE.txt.

## New in v16 — a dedicated Tools hub

The sidebar now keeps the main monitoring pages, System check, Preferences and your pins. Everything else lives in **Tools hub**, grouped into Monitoring, Hardware & Network, Repair & AI Help, Productivity, and Appearance & Play. Existing pages remain available through Ctrl+K and sidebar search. Search the hub by name or description, filter by category, pin favorite tools, or return to one of the six most recently visited pages in this session. Searching for startup finds Preferences, which contains the animation controls.

**System check** is a read-only, manually refreshed report of the latest CPU/memory sample, available fixed-drive space and reported power/battery state. It flags storage below 10% available, CPU at least 90%, and memory at least 85%. It shows the sample timestamp because paused monitoring can leave older readings. These are observations, not proof of a fault. It does not scan Windows file integrity, malware, updates, or physical disk health.

Copy or save the report locally, or prepare its text in Windows advisor for optional AI help. Preparing text does not send it or execute fixes. Storage and power queries run off the interface thread. Provider failures are shown as unavailable. Reports include drive letters and resource readings; review them before sharing.

v16 validation: both applications built successfully. Isolated checks passed for low-space boundaries, missing capacity, startup search, unique and complete tool catalog coverage, hub navigation and opening/leaving the System check page. The hub layout was visually inspected. No Windows repairs or settings changes were performed. External speed-test availability and real AI inference remain unverified.

## New in v15 — a cinematic welcome

Three theme-matched startup styles: Orbit (satellites around the Aster star), Warp (outward star trails), and Aurora (flowing ribbons). The star grows into view and the workspace fades in over 240 ms. This is a decorative introduction, not simulated diagnostics or a loading-progress indicator.

In **Preferences → Make an entrance**, select a style, choose 1.6 / 2.8 / 4.2 seconds, replay the introduction, or disable it for a quicker launch. The default is Orbit at 2.8 seconds. Enter, Escape, or Open Aster skips the intro; closing its window cancels launch. Reduced motion skips the startup animation and workspace fade; its preview stays still. This preference is shared with Aster's existing animated workspace views. Recovery, repair-plan and self-test launch modes bypass the introduction.

Animations use local drawing with no downloads, audio, services or extra hardware polling. The intro timer and image are disposed on close. Startup style and duration are stored with the existing local preferences and included in workspace backups. Unrecognized style/duration values fall back to Orbit / 2.8 seconds.

v15 validation: both executables compile. Isolated tests passed for legacy preference defaults, saving startup choices, invalid-setting fallback, rendering all three styles, timed completion, immediate skip, close-to-cancel, and static reduced-motion rendering. Orbit preview visually inspected. Real AI inference and the external speed-test service remain unverified; this release does not change those integrations.

## New in v14 — customize, compare and preserve

**Home layout:** show/hide seven Summary sections, or choose Everything, Essentials or Quiet dashboard. At least one section must remain visible. Save layout & open Summary applies the choice. This changes presentation, not the sampler or recorded history.

**Hardware compare:** load two exported Aster Hardware reports, or load an older report and capture current specifications. The side-by-side table identifies changed, added, removed and unchanged fields. Filter to changes, export the table as CSV, or prepare up to 35 differing fields (bounded text) for the optional AI counselor. Preparing text does not send it. Provider availability and device enumeration changes can also produce differences; a difference does not establish a fault. Reports must be under 1 MB and contain recognized Aster specification lines. Current capture can take time while Windows providers respond.

**Backup center:** create a local ZIP of saved Workspace.json preferences, PersonalWorkspace.json notebook/checklist, NetworkHistory.json and saved Cases/*.json. It excludes Windows files, wallpaper assets, Aura settings and recovery logs. Archives are not encrypted. Save current notes when prompted to include them.

Restore validates the archive and displays the files to add/replace before confirmation. Only matching files are replaced; unrelated local files remain. It validates allowed names and stored-data structure, rejects duplicate/unsafe entries and linked targets, and bounds each file to 1 MiB, the expanded total to 20 MiB and count to 600. Settings and notes reload after restoration. Close other Aster copies first so they do not overwrite restored settings with stale copies.

Before writing, Aster saves existing matching files under `%LOCALAPPDATA%\Pulse\Backups\before-restore-*.zip`. If a write fails it attempts to roll back files already written and reports any rollback failures. These backups restore replaced content; they do not remove newly added files when loaded later. If no matching files existed, the pre-restore archive is empty and has no prior content to recover. Save unsaved notebook/checklist edits before restoring if you want those edits included in the pre-restore backup.

v14 validation: both builds passed. Isolated tests covered backup creation/validation, selected-file restore, preservation of unrelated files, pre-restore recovery, path-traversal and malformed-data rejection, invalid preference rejection, validation before writes, hardware difference detection, report parsing, layout rendering and Quiet dashboard visibility. No real Aster data or Windows settings were restored or changed during tests. Network speed-test live service availability and real AI inference remain unverified from earlier releases.
## New in v13 — Network Lab

Open **Network lab** from the sidebar or Ctrl+K.

- Quick speed test: three 4 MiB downloads and three 2 MiB generated-data uploads (about 18 MiB payload).
- Standard: three 16 MiB downloads and three 8 MiB uploads (about 72 MiB payload).
- Latency-only: six zero-payload HTTPS requests; the first warms the connection and five are measured.
- Median download/upload rates in decimal Mbps, median HTTPS request latency, and mean absolute difference between successive latency samples. This variation is not packet loss or ICMP jitter.
- Connection details: on-demand local adapter status, reported link rate, local IP addresses, gateways and DNS servers. No internet request is made by that inventory. Link rate is not internet throughput.
- Up to 50 completed results saved locally; select a result to review, copy the most recent result or export history as CSV.

Tests contact `https://speed.cloudflare.com/__down` and `/__up` only when Start is clicked. Cloudflare receives your network address and generated test traffic; no personal files are uploaded. Requests use ordinary HTTPS verification, no automatic redirects/cookies, and uncompressed payloads. Windows proxy configuration may affect the connection. Each transfer has a 30-second limit and the complete run a three-minute limit. Cancel or leave the page to stop. Partial traffic may already have transferred. Failed/canceled runs are not saved as complete results.

These are sequential, single-connection application-throughput estimates, using the full request duration; they are not the official Cloudflare measurement engine or a maximum ISP line-speed measurement. Short tests may underestimate fast connections. VPNs, wireless conditions, competing traffic and server overhead affect results. Request headers, acknowledgements and upload responses add data beyond the stated test payload.

History is stored as plain JSON in `%LOCALAPPDATA%\Pulse\NetworkHistory.json`. Local adapter details are displayed separately and are not sent in the speed-test requests or stored in speed history.

Endpoint reference: https://github.com/cloudflare/speedtest/blob/main/README.md

v13 validation: executable builds passed. Tests with simulated HTTP responses passed for Mbps conversion, medians, timing variation, stated data budgets, complete request sequencing/byte accounting, latency-only mode, truncated downloads, cancellation, 50-result history retention, and Network Lab rendering/navigation. **A live test against the external service was not performed here.** Provider availability, rate limits, proxy and firewall behavior still require real-world validation. Preview is the idle interface from app-owned rendering; it contains no measured internet speed.
## New in v12 — 24 themes and a personal workspace

**Themes:** 24 searchable dark palettes. The original six are joined by Sapphire, Emerald, Cherry, Lavender, Copper, Iceberg, Moss, Plum, Cocoa, Indigo, Peach, Slate, Jade, Orchid, Sandstone, Arctic, Mulberry and Electric. Select a card to apply and save.

**Theme studio:** choose a dark background and bright accent with a color picker or hexadecimal values. Preview before applying. The app derives coordinated surfaces and checks its primary/muted text against the raised surface and accent contrast against raised/hero surfaces. These checks improve readability but are not a full accessibility audit. Custom theme recipes can be exported/imported as small JSON files containing colors; importing does not apply them until requested. One active custom recipe is stored in preferences; export additional recipes to keep a collection.

**Notebook:** a local scratchpad of up to 50,000 characters. Insert a timestamp, export text, or save the workspace. Unsaved text remains in memory while changing pages.

**Checklist:** up to 200 tasks with completion state, hide-completed filtering, text export, removal and one-level undo for the most recent removal. Undo is session-only. Notes and tasks are saved together with Save workspace. Closing Aster with changes prompts Save / Discard / Cancel.

Personal data is plain local JSON in `%LOCALAPPDATA%\Pulse\PersonalWorkspace.json`; each successful replacement keeps the previous save as `.backup`. It is not encrypted or cloud-synced. Unreadable or invalid stored data is not silently overwritten; those pages disable editing until the file is repaired/restored outside the app.

**Ctrl+K action launcher:** search all pages and four explicit actions: pause/resume monitoring, toggle distraction-free view, change app picture now, or save notebook/checklist. Enter/double-click selects; Escape cancels. It does not execute arbitrary commands or Windows repairs.

v12 validation: both applications built. Tests passed for unique themes, preset primary text contrast, custom-theme creation/invalid contrast rejection, invalid imports, notebook/checklist round-trip, previous-save backup, invalid-data preservation, page rendering, unsaved-note retention during navigation, and checklist removal/undo. No model inference, Windows repairs or wallpaper changes were exercised. Preview is an app-owned test rendering.
## New in v11 — daily app picture

Open **Preferences → A new look each day** and enable **Change my app picture every day**. This option starts disabled. Seven built-in designs rotate in order: Aster star, Orbit, Crescent, Sunrise, Bloom, Comet and Constellation.

The sidebar, Summary welcome picture and running main-window icon update automatically at midnight in the greeting timezone (Riyadh by default, or Windows local time). If Aster is closed, the picture is calculated on reopening; no background service or network request is required. The seven-day sequence repeats weekly.

Choose any design to use it today, use **Change now** to move to the next, or **Keep this picture** to disable rotation and hold the current image. Turning rotation on preserves the current picture for that day. Preferences persist in the existing local settings file. Changing the chosen time zone can immediately change the calculated calendar day and picture.

This does not modify the executable/desktop-shortcut icon, Windows account image or wallpaper. Windows may keep a cached taskbar icon. Aura's separate icon is unchanged.

v11 checks passed: both application builds; seven-day sequence and wraparound; fixed-image stability; all manual selections; Riyadh midnight; normalization of invalid selections; creation of all seven window icons; saved settings; live picture changes; and welcome-banner rendering. The preview is an app-owned test rendering.
## New in v10 — complete workspace themes

Open **Themes** in the sidebar, or **Preferences → Open theme gallery**. Click a preview card to apply and save one of six coordinated dark themes: Midnight, Amethyst, Lagoon, Rosewood, Ember or Graphite.

Themes color the main Aster workspace: navigation, page backgrounds, greeting banner, metric cards, chart surfaces and grids, tables, inputs and advisor tabs. Buttons have rounded corners, hover feedback and keyboard focus outlines. The selection persists in the existing Workspace.json preferences; invalid theme IDs fall back to Midnight. Applying colors does not restart monitoring or erase its history.

The gallery replaces the old accent-only selector. Playground scene palettes remain separate artwork choices; their old stored accent value no longer overrides the full workspace theme. Windows title bars, system dialogs and the separate Aura wallpaper application retain their own appearance. Separate repair/mini-monitor windows retain their previous styling.

v10 validation: both application builds passed; all six summary layouts rendered and were checked, along with the gallery, advisor, processes, preferences, Playground and focus timer. Tests covered persisted theme selection, unknown-theme fallback and sidebar filtering retaining the selected palette. No repairs, model inference or wallpaper changes were run. Preview is an app-owned rendering from the test environment.
## New in v9 — personal greetings and calendars

Summary now greets you by name and displays the weekday, Gregorian day/month/year, live clock and optional Hijri date. Settings are at the top of Preferences under Your daily welcome; click Save greeting preferences after editing.

- Morning: 05:00–11:59; afternoon: 12:00–16:59; evening: 17:00–21:59; peaceful night: 22:00–04:59.
- Defaults to your Windows username; set a custom display name or leave it blank for the username. Names are limited to 40 characters.
- Riyadh (UTC+03:00) is the default. You can select Windows local time instead and choose a 12- or 24-hour clock. The application does not change Windows time or timezone.
- The banner updates every second, including date and greeting transitions at midnight/time-of-day boundaries. Gregorian dates use an explicit day/month/year format independent of Windows culture.
- Optional Umm al-Qura Hijri date, Ramadan day greetings, a Ramadan countdown during Sha'ban, Eid al-Fitr on 1 Shawwal, Eid al-Adha on 10 Dhu al-Hijjah, and a new-year greeting on 1 Muharram.
- An optional −2 to +2-day display adjustment shifts only the Hijri date and related messages. Actual observance may follow local moon-sighting announcements; these messages use the selected calendar date and are not prayer/fasting-time calculations. Display rollover is at civil midnight, not sunset.
- Disabling the Hijri checkbox also disables its seasonal messages. Unsupported calendar dates display an unavailable message.

Calendar implementation: Microsoft .NET Umm al-Qura calendar: https://learn.microsoft.com/en-us/dotnet/api/system.globalization.umalquracalendar?view=netframework-4.8.1

v9 checks passed: both executable builds; greeting time boundaries; Riyadh midnight date conversion; Ramadan/Eid recognition; adjustment across a month boundary; hiding seasonal messages; out-of-range fallback; name filtering; saved preferences; and Summary/Preferences rendering. The preview is app-owned rendering with a test display name. No repairs, AI calls or wallpaper changes were performed.
## New in v8 — playful workspace

- Playground with Orbit, Aurora and Pixel Garden scenes. Orbit speed and wave/garden height respond to the latest existing CPU reading; stars, particles and other visual elements are decorative, not additional sensors or benchmark results.
- Nebula, Tide and Sunset palettes, a gradient Summary banner and rounded metric cards. Palette selection also changes the saved accent; Sunset is available in Preferences.
- Click a scene to launch a bounded burst of stardust. With animations disabled, clicking steps the static illustration instead.
- Focus timer with 5/25/50-minute presets, custom 1–180-minute sessions, start/resume, pause and reset. Presets replace the current countdown. It uses elapsed time and continues while navigating or minimizing Aster; closing Aster ends the session. Timer state and completion counts are session-only.
- Completed sessions display a congratulatory message and a brief particle celebration when the timer page is visible and animations are enabled. No sound, app blocking, system notification or Windows setting change occurs.
- Animate scenes is a saved option. Animated views redraw at up to 20 FPS only on Playground/Focus timer while not minimized. Minimized/other-page updates drop to one second. Animations do not create another system sampler; drawing itself still has a resource cost.

v8 validation: builds succeeded; all three scenes and the new pages rendered successfully. Tests passed for elapsed countdown timing, pause/resume, one-time completion, duration bounds, reduced motion, navigation/filtering, F11 state, settings, alert cooldown, help search and report-identifier filtering. The preview is an app-owned rendering from the test environment. No Windows repair, AI inference or wallpaper attachment was performed in these tests.
## Included from v7

- Aster name, new geometric star icon, simpler branding, cleaner grid lines and page-specific toolbars.
- Focus dashboard: CPU, memory, visible process count, Windows uptime and recent sampled ranges, all using the existing sampler.
- F11 distraction-free mode hides/restores navigation and the toolbar. Ctrl+K still opens quick navigation.
- Searchable Help center with ten practical guides and buttons leading to related tools. Works without an AI model.
- Hardware report: generate, review, copy or save CPU, GPU, RAM, storage, motherboard and BIOS specifications. Serial-number and device/processor-identifier fields are excluded. This is inventory, not a health certificate. Review before sharing.
- Existing Pulse cases, logs, preferences and Aura settings remain compatible through the same `%LOCALAPPDATA%\Pulse` folder. The legacy folder name is intentionally retained; no migration or deletion is performed.

## Monitoring and convenience

Summary provides metric cards, CPU/memory histories, top processes, hardware and supported NVIDIA sensor readings. Performance includes network/disk activity. Dedicated views cover CPU, GPU, memory, drives, BIOS, network adapters, services, startup entries, apps, drivers, users and connections.

Processes supports search, sort and reviewed termination with protected-process checks. Flight recorder captures a bounded session and exports CSV. App history is observed session activity, not Windows historical usage. The compact monitor reuses existing readings.

Use the header star to pin pages, search the sidebar, or press Ctrl+K. F5 refreshes the current view. Preferences saves Ocean/Violet/Mint accents, sampling intervals and always-on-top behavior. Sampling intervals are milliseconds: 1000/2000/3000/5000. Advisor Efficient/normal controls are session-only overrides.

Insights compares a captured baseline with current CPU/memory/process readings, and can prepare the evidence for AI. Differences do not prove cause and effect. Quiet alerts use explicit CPU >=90% / RAM >=85% rules over five consecutive samples, a five-minute cooldown per metric, and at most 100 session entries. No automatic process killing occurs.

## Windows advisor and recovery

Eight advisor tabs offer read-only diagnosis, optional local AI, reversible app optimizations, saved cases, offline error help, repair-log indexing, six prompt templates, and Windows-settings shortcuts.

Read-only diagnosis samples resource usage and reads free space, adapter availability, two restart flags, recent event metadata and supported GPU sensors. Provider failures are reported as unavailable.

Saved cases are plain local JSON files containing the displayed report, symptom, last question/answer and notes. Save is explicit. Restored reports are historical; run a fresh scan for current values. Earlier conversation context is cleared on restore. The list displays the newest 300 cases; older files remain on disk.

Log review accepts text files up to 256 KB, indexes hexadecimal codes and English exit-code markers, and can prepare the first 5,400 characters for optional AI review. Pattern matching is not AI or proof of successful repair.

Reviewed built-in repair plans use fixed commands: Windows scans (DISM CheckHealth, SFC verify-only, CHKDSK without repair switches), Windows-file repair (DISM RestoreHealth, SFC scannow, verification), and DNS-cache refresh. Separate recovery tools include other supported scans and Winsock reset. Exact actions are shown before elevation. Plans stop on failure or restart-required results; they never restart Windows automatically. Logs remain local. Tool completion does not guarantee the original symptom is fixed.

## Optional AI

No AI model is bundled. Install Ollama, download a local model and run its service. In Windows advisor → AI counselor, click Find local models, select a model, review your question and click Ask.

Aster connects only to the fixed local endpoint `http://127.0.0.1:11434`, excludes obvious cloud/remote and zero-size model entries, and does not follow redirects. Its local Ollama service controls actual inference. Diagnostic reports are included only with the checkbox selected. Prepared comparison data or logs are not sent until Ask is clicked. Review sensitive details first.

The counselor supports guided troubleshooting, error explanation, gaming investigation, diagnostic interpretation, log review and comparisons. AI responses are advice, never executable actions. Commands from model text are not parsed or executed. Canceling a request does not guarantee the local service immediately stops inference. The service is requested to unload the model after responding.

API references: https://docs.ollama.com/api/chat and https://docs.ollama.com/api/tags.

## Aura wallpaper companion

Includes procedural scenes, local image/video import, preview, playlists, monitor selection, frame/particle/speed controls, fit modes, volume, clock, shuffle, tray controls, optional startup and battery/fullscreen/session-lock pause behavior. Eco profile has backup/restore. Windows video codec and desktop attachment support vary. Failed attachment is reported rather than replaced with an overlay.

## Validation and limits

Both executables built successfully. Tests passed for page construction/rendering, smaller-window Focus layout, sidebar filtering, F11 toggle state, help search, hardware-identifier filtering, settings persistence/fallback, alert thresholds/cooldowns, case persistence, repair allowlists/stop conditions, model filtering, profile backup/undo and navigation.

Visual checks used app-owned rendering in a restricted test environment. Hardware providers may be unavailable there. Actual local-model inference, Windows repair execution and desktop wallpaper attachment were not exercised in this release. No Windows repairs or desktop settings were changed by testing. This application does not supply unsupported CPU temperature/power readings or a fabricated system health score.

## Error-help references

- https://learn.microsoft.com/en-us/troubleshoot/windows-client/installing-updates-features-roles/troubleshoot-windows-update-error-0x80070005
- https://learn.microsoft.com/en-us/troubleshoot/windows-server/installing-updates-features-roles/troubleshoot-windows-update-error-0x80070002
- https://learn.microsoft.com/en-us/troubleshoot/windows-server/installing-updates-features-roles/troubleshoot-windows-update-error-0x80070070
- https://learn.microsoft.com/en-us/troubleshoot/windows-client/application-management/dotnet-framework-35-installation-error







