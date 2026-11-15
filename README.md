# Aster
Aster is a work-in-progress task manager and system toolkit for Windows.

## Download
**[Aster-Windows-v2.4.zip](Aster-Windows-v2.4.zip)**: extract the whole ZIP and run `Aster.exe`. If you already have Aster 2.3, open **Update Aster**, choose the ZIP, then switch to the updated version. Your settings are kept.

Requires Windows 10/11 (64-bit) with .NET Framework 4.x. `Aster.exe` in this folder is the same 2.4 build.

## New in 2.4
| Tool | Where | What it does |
|---|---|---|
| **Space finder** | Hardware & Network | Shows the 100 largest files, folder sizes and space by file type for any folder or drive. Runs in the background, can be stopped, and is read-only. |
| **Ping monitor** | Hardware & Network | Live latency graph to your router or a public host, with avg/min/max, jitter, packet loss and a plain-language verdict. |
| **Power timer** | Productivity | Shutdown or restart after N minutes or at a clock time, with a live countdown and cancel. Also Lock now and Sleep now. Uses Windows' `shutdown.exe`, so `shutdown /a` always cancels. |

All 2.3 features (Horizon desktop, Desktop mods, online updates, monitoring, hardware, repair and productivity tools) are unchanged.

## Repository layout
- `Aster-Windows-v2.4.zip`: the full portable package (app, Python runtime for alerts, WebView2, AuraWallpaper)
- `src/`: C# source for Aster 2.4 (`Aster.csproj`, .NET Framework 4.8)
- `tests/CoreTests.cs`: automated checks for the 2.4 tool logic
- `tools/make_package.py`: builds an update-ready ZIP with a `release.json` manifest

See [BUILDING.md](BUILDING.md) for build and test steps.
