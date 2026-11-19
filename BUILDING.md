# Building Aster

## Build the app
Requires the .NET SDK (6 or newer). It works on Windows and on Linux.

```
cd src
dotnet build -c Release
```
Output: `src/bin/Release/net48/Aster.exe`. The .NET Framework 4.8 reference assemblies come from NuGet automatically.

> `src/` was recovered from the Aster 2.3 binary, because no 2.3 source was in this repository. All 2.3 types and members were checked against the original. The 2.4 tools live in `src/Aster/AsterNext.cs` (UI) and `src/Aster/AsterNextCore.cs` (logic).

## Run the tests
```
# Windows (csc) or Linux (mono's mcs)
mcs -out:core-tests.exe tests/CoreTests.cs src/Aster/AsterNextCore.cs
mono core-tests.exe        # or just core-tests.exe on Windows
```

## Make a release package
1. Copy the previous package folder and replace `Aster.exe` with the new build.
2. Bump `AsterUpdate.CurrentVersion` in `src/Aster/AsterUpdate.cs` before building.
3. Run `python tools/make_package.py <folder> <version> Aster-Windows-v<version>.zip`.

The script refuses to package `home-location.json`, so a public ZIP never includes a personal home preset. Online (signed) releases still need the maintainer's signing key and `Prepare-Online-Release.py`.
