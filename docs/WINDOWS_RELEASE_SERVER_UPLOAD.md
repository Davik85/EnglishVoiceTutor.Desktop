# Windows release server upload

Review date: 2026-06-12.

## Current uploaded release

`0.1.28-tester.1` is the current public tester Windows direct manifest baseline. The public tester download path resolves through `latest.json` to `LanguageVoiceTutorSetup-0.1.28-tester.1.exe`.

The current manifest is served from:

```text
https://languagevoicetutor.com/releases/windows/direct/latest.json
```

The current manifest values are:

```text
version: 0.1.28-tester.1
installerFileName: LanguageVoiceTutorSetup-0.1.28-tester.1.exe
backendBaseUrl: https://api.languagevoicetutor.com
minimumSupportedVersion: 0.1.28-tester.1
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
LanguageVoiceTutorSetup-0.1.28-tester.1.exe
latest.json
changelog.json
known-issues.json
checksums.sha256
```

Generated files under `artifacts/` and installer `.exe` files must not be committed.

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

- `version` is `0.1.28-tester.1`;
- `installerFileName` is `LanguageVoiceTutorSetup-0.1.28-tester.1.exe`;
- `backendBaseUrl` is `https://api.languagevoicetutor.com`;
- `minimumSupportedVersion` is `0.1.28-tester.1`;
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

- public page downloads the correct `0.1.28-tester.1` installer;
- installed build uses only `https://api.languagevoicetutor.com`;
- registration/login work from another device;
- trial grant after registration;
- lesson start, TTS/bot voice, Conversation Mode, Lesson History, and Progress;
- auth session persistence across app restart and Windows restart;
- update/reinstall preservation of auth session, settings, history, and progress;
- smaller-screen/scaled-display Welcome layout with visible primary actions and no gray cover bars;
- no Diagnostics tab and no Backend URL field in release Settings.

## Deferred items

Code signing remains deferred. Production billing/Paddle/subscription payment lifecycle remains deferred. CMS published-snapshot learner runtime remains disabled/not the default until explicitly enabled and validated later.
