# Windows release server upload

Review date: 2026-06-18.

## Source of truth for current versions

These docs are a snapshot of the last known verified state. They can become stale and must not be used as the only source of truth for live versions. Always verify the live/public state before telling a tester that a version is current.

Check the public Windows direct tester release from the live website manifest:

```powershell
Invoke-RestMethod https://languagevoicetutor.com/releases/windows/direct/latest.json
```

If a PowerShell path reads raw manifest text and `ConvertFrom-Json` fails because a UTF-8 BOM is present at the start of `latest.json`, strip the BOM before parsing:

```powershell
($raw -replace "^\uFEFF", "") | ConvertFrom-Json
```

Check the production backend release from the server `current` symlink:

```powershell
ssh lvt-server "readlink -f /opt/languagevoicetutor/backend/current"
```

Check production backend health and database health:

```powershell
Invoke-WebRequest https://api.languagevoicetutor.com/health -UseBasicParsing
Invoke-WebRequest https://api.languagevoicetutor.com/api/health/database -UseBasicParsing
```

Generated local files under `artifacts/` are not proof that a version is live on the public site. A locally built installer becomes public only after the Windows direct release files are uploaded to the website release folder and `latest.json` is verified over HTTPS.

## Current uploaded release

The live public tester Windows direct manifest baseline must be checked from the website `latest.json`; it is always the source of truth. Last verified public snapshot: the public tester download path resolved through `latest.json` to `LanguageVoiceTutorSetup-0.1.36-tester.24.exe`. `0.1.36-tester.24` is the current verified uploaded tester build (previous tester release: `0.1.36-tester.15`), but still verify the website `latest.json` over HTTPS before announcing it to testers.

The current manifest is served from:

```text
https://languagevoicetutor.com/releases/windows/direct/latest.json
```

The current manifest values are:

```text
version: 0.1.36-tester.24
installerFileName: LanguageVoiceTutorSetup-0.1.36-tester.24.exe
backendBaseUrl: https://api.languagevoicetutor.com
minimumSupportedVersion: 0.1.36-tester.24
updateMode: manual-confirmation
```

The manifest `backendBaseUrl` is:

```text
https://api.languagevoicetutor.com
```

The public page at `https://languagevoicetutor.com` must continue to read `latest.json` and must not hardcode old installer filenames.

## Scope of upload

Windows direct-release upload publishes static release files only. It does not deploy the backend, does not run EF migrations, does not upload secrets, and does not make the product broadly public production-ready.

Release/tester installed builds are server-only. The only backend for packaged non-Debug Windows builds is `https://api.languagevoicetutor.com`. Local backend URLs are DEBUG/developer-only and must not be present as normal user Settings options. Diagnostics and Backend URL editing are not part of user/release Settings. Stale AppData `settings.json` backend URL values from older installs are ignored by release builds and are not written back into user-editable settings.

Clean-machine smoke must verify registration/login/lesson/history/progress/update from an installed build against the fixed production backend. The installed build connectivity signal is `GET https://api.languagevoicetutor.com/health`; registration calls `POST https://api.languagevoicetutor.com/api/auth/register`, login calls `POST https://api.languagevoicetutor.com/api/auth/login`, and auth restore calls `GET https://api.languagevoicetutor.com/api/auth/me`. Optional cloud settings or subscription/status endpoint failures must not block auth or lessons and must not be treated as the backend connectivity signal.

## Files to upload

The server-ready folder is:

```text
artifacts\releases\windows\direct
```

Expected files:

```text
LanguageVoiceTutorSetup-{version}.exe
latest.json
changelog.json
known-issues.json
checksums.sha256
```

Generated files under `artifacts/` and installer `.exe` files must not be committed. Generated artifacts are not source of truth for the public/live Windows release until uploaded and verified through live `latest.json`.

## Upload process

Use the Windows direct-release upload helper or equivalent manual SCP/rsync process to place the direct-release folder under the website's static release path. Keep the static website upload helper separate from the Windows release-file upload helper.

After upload, verify from a client machine:

```powershell
$manifest = Invoke-RestMethod -Uri "https://languagevoicetutor.com/releases/windows/direct/latest.json?t=$(Get-Date -Format yyyyMMddHHmmss)"
$manifest.version
$manifest.installerFileName
$manifest.backendBaseUrl
Invoke-WebRequest -Uri "https://languagevoicetutor.com/releases/windows/direct/$($manifest.installerRelativeUrl)" -OutFile "$env:TEMP\$($manifest.installerFileName)"
Get-FileHash -Path "$env:TEMP\$($manifest.installerFileName)" -Algorithm SHA256
```

Confirm:

- `version` matches the intended uploaded version and the live tester version you are announcing;
- `installerFileName` is `LanguageVoiceTutorSetup-{version}.exe` for the intended uploaded version;
- `backendBaseUrl` is `https://api.languagevoicetutor.com`;
- `minimumSupportedVersion` matches the intended support floor;
- `updateMode` is `manual-confirmation`;
- the SHA-256 matches `installerSha256` and `checksums.sha256`;
- the public download page downloads the same installer named by the manifest.

## Static tester download page

The static page under `site/public/` is the private tester download page. It reads `/releases/windows/direct/latest.json`, displays release details from the manifest, and builds the Windows download link from `installerRelativeUrl`. If the manifest cannot be loaded or is invalid, the page must keep the download button disabled instead of using a hardcoded installer fallback.

This page does not implement auto-update and does not replace the in-app manual update check. It must not include login, payment, pricing, account management, analytics, cookies, third-party fonts, external dependencies, or broad public marketing claims.

## Manual update UX verification

The desktop Settings UX has a single user-facing **Check for updates** button. The flow checks `latest.json`, validates manifest identity, compares versions, asks before download/install, verifies SHA-256 before launching the installer, and does not silently auto-update.

The old technical update dashboard in Diagnostics is not part of release UX. Release Settings must not show Diagnostics or Backend URL editing.

## Post-upload tester smoke

After upload, run or record a clean-machine smoke that covers:

- public page downloads the installer named by live `latest.json`;
- installed build uses only `https://api.languagevoicetutor.com`;
- registration/login work from another device;
- trial grant after registration;
- lesson start, TTS/bot voice, Conversation Mode, Lesson History, and Progress;
- auth session persistence across app restart and Windows restart;
- update/reinstall preservation of auth session, settings, history, and progress;
- smaller-screen/scaled-display Welcome layout with visible primary actions and no gray cover bars;
- Russian and French Welcome/start header text does not truncate or clip;
- CMS scenario edits and level profile edits appear in newly started desktop lessons after Save draft + Publish;
- no Diagnostics tab and no Backend URL field in release Settings.

## Deferred items

Code signing remains deferred. Production billing/Paddle/subscription payment lifecycle remains deferred. CMS published-snapshot runtime is active for controlled tester lessons, with static JSON fallback available for rollback; broad public production release remains deferred.
