# Aster
Aster is a work-in-progress task manager and system toolkit for Windows.

## Download
**[Aster-Windows-v2.8.zip](Aster-Windows-v2.8.zip)**: extract the whole ZIP and run `Aster.exe`. If you already have Aster 2.3 to 2.7, open **Update Aster**, choose the ZIP, then switch to the updated version. Your settings are kept.

Requires Windows 10/11 (64-bit) with .NET Framework 4.x. `Aster.exe` in this folder is the same 2.8 build.

## New in 2.8

- **Home that knows the time:** greeting and emoji follow the day (early start, morning, afternoon, evening, night) and update live, plus a "Right now" suggestion with a button that opens the tool. It rotates daily and points to Processes when your PC is busy.

## New in 2.7

- **Real Home screen:** logo, greeting, three live meters, one plain sentence about your PC, six quick tools and recent tools. Aster opens here.
- **Welcome tour:** five short skippable steps, shown once.
- **Safety net:** unexpected errors are saved as a report (user name removed) and you are offered a copy button.

## New in 2.6

- **Calmer Tools hub:** compact icon tiles, a quick-access row (pinned + recent), word-by-word search, star-to-pin, and **Simple mode** that hides 23 specialist tools from the hub and search (Preferences or the hub). Nothing is removed.

## New in 2.5

- **Ping monitor:** Save report and Copy report (host, loss, avg/min/max, jitter, verdict, recent replies).
- **Space finder:** Save CSV for Excel (formula-safe) and Copy summary for chat or email.
- **Power timer:** repeat a shutdown or restart on chosen days at a set time, using the Windows Task Scheduler, with a 60-second cancel window and a Remove repeat button.

## New in 2.4
| Tool | Where | What it does |
|---|---|---|
| **Space finder** | Hardware & Network | Shows the 100 largest files, folder sizes and space by file type for any folder or drive. Runs in the background, can be stopped, and is read-only. |
| **Ping monitor** | Hardware & Network | Live latency graph to your router or a public host, with avg/min/max, jitter, packet loss and a plain-language verdict. |
| **Power timer** | Productivity | Shutdown or restart after N minutes or at a clock time, with a live countdown and cancel. Also Lock now and Sleep now. Uses Windows' `shutdown.exe`, so `shutdown /a` always cancels. |

All 2.3 features (Horizon desktop, Desktop mods, online updates, monitoring, hardware, repair and productivity tools) are unchanged.

## Repository layout
- `Aster-Windows-v2.8.zip`: the full portable package (app, Python runtime for alerts, WebView2, AuraWallpaper)
- `src/`: C# source for Aster 2.4 (`Aster.csproj`, .NET Framework 4.8)
- `tests/CoreTests.cs`: automated checks for the 2.4 and 2.5 tool logic
- `tools/make_package.py`: builds an update-ready ZIP with a `release.json` manifest

See [BUILDING.md](BUILDING.md) for build and test steps.
