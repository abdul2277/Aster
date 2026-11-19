# Online updates

The Update Aster page now checks an HTTPS release feed, shows a newer version and release notes, downloads it with progress and cancellation, validates the full ZIP hash and every packaged file, installs to a new version folder, and offers a restart. It preserves the existing program and shared user settings. Save/stop-monitoring prompts still apply before restarting.

## Current limitation

There is no public Aster update host yet. The app honestly displays **Update service not connected**. The included feature cannot obtain future releases until someone publishes them. No placeholder URL is enabled and no account or hosting service was created. This release still needs to be installed once from the provided download.

## One-time publisher setup

1. Choose an HTTPS host for a small `latest.json` file and public release ZIPs. The host must return the files themselves, without a login or download landing page. HTTPS redirects are supported; redirects to HTTP are refused.
2. Build a release with Build.cmd, then run the bundled Python on Make-Update.py to produce its update-ready ZIP.
3. Run `runtime\python.exe Prepare-Online-Release.py <update-zip> <output-folder> --package-url <final-https-zip-url>`. This prepares a public ZIP with the home preset removed and the place dictionary cleared, then calculates the public ZIP's exact hash and size in `latest.json`. It does not upload anything. Do not publish a personalized ZIP directly.
4. Upload the generated public ZIP first. Then publish `latest.json` at a stable HTTPS address. Do not replace the ZIP contents after generating the feed.
5. In Aster, open Update Aster → Update service settings. Save the stable `latest.json` address once. Users can then check, download, install and restart from the app without selecting files.

Only connect a source you control or trust. HTTPS and hashes protect transfer integrity; they are not a separate publisher signature. Access to the release host must be protected. Aster sends its version in its User-Agent but sends no location, notes, Telegram credentials or telemetry in update requests. The server receives the normal network request and IP address.

Checks are user-initiated. Downloads have a 10-minute limit; feed checks have a 30-second limit. Incomplete/canceled or corrupt downloads are removed. Cancellation stops during download, then becomes unavailable once verification/installation starts. Older releases are never installed. A failed installation leaves the current app intact; an incomplete version folder is never launched.
