# Windows installer release flow

> Release direction note: this Inno Setup flow remains valid for controlled direct Windows releases until the owner explicitly changes the release flow. Microsoft Store/MSIX was evaluated and discontinued for now. Future Windows trust/signing work should focus on a code signing certificate for the direct EXE/Inno installer. Do not change packaging scripts, upload scripts, `latest.json`, release validation, or installer behavior for this future-direction note.


Review date: 2026-06-18.

## Source of truth for current versions

These docs are a snapshot of the last known verified state. They can become stale and must not be used as the only source of truth for live versions. Always verify the live/public state before telling a tester that a version is current.

Check the public Windows direct release from the live website manifest:

```powershell
Invoke-RestMethod https://languagevoicetutor.com/releases/windows/direct/latest.json
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

Inno Setup is the primary Windows direct-download installer track for Language Voice Tutor.

## Current validated release

The public Windows direct manifest baseline must be checked from the live website `latest.json`. Last verified public snapshot: `latest.json` pointed to `LanguageVoiceTutorSetup-1.0.exe` with `version` set to `1.0`, `backendBaseUrl` set to `https://api.languagevoicetutor.com`, `minimumSupportedVersion` set to `1.0`, and `updateMode` set to `manual-confirmation`. Treat this as a controlled direct Windows release baseline only; do not describe any future local build as public/live unless the website `latest.json` points to it over HTTPS.

Windows Direct Release 1.0 is published on the public direct channel. This is not a claim that every operational area is broad-production-ready. Code signing remains deferred, so SmartScreen warnings are still expected until a signed installer is published.

## Decision

- Primary installer technology: Inno Setup 6.
- Public product name: Language Voice Tutor.
- Desktop window title: Language Voice Tutor Desktop.
- Stable installer AppId: `LanguageVoiceTutor.Desktop`.
- Default install directory: `{autopf}\Language Voice Tutor`, normally under Program Files.
- Expected installer artifact: `artifacts\installers\windows\LanguageVoiceTutorSetup-{version}.exe`.
- Server-ready direct-download output: `artifacts\releases\windows\direct`.
- Installed file names were renamed to `LanguageVoiceTutor.Desktop.*`. Installed tester/release output files now use `LanguageVoiceTutor.Desktop.*` names. Internal project, folder, and namespace names may remain `EnglishVoiceTutor.*` until a later safe cleanup to avoid risky project-wide churn.

Velopack is rejected/deprecated for this project because its Windows installer is a one-click installer and does not match the desired release-like wizard UX. ZIP packaging remains only an emergency/developer fallback. Microsoft Store/MSIX was evaluated and discontinued for now.

## Release backend lock

This packages tester/release installed builds with the fixed production backend `https://api.languagevoicetutor.com`. The release package script rejects any other `-BackendBaseUrl`; local or custom backend URLs are DEBUG/developer-only and must not be used for installed tester/release builds.

Release Settings must not expose Diagnostics or Backend URL editing. Testers do not need to run a local backend.

## Installer behavior

The Inno Setup installer:

- shows a normal wizard;
- shows the destination directory selection page;
- creates normal Windows Installed Apps / uninstall integration;
- creates a Start Menu shortcut named `Language Voice Tutor`;
- offers an optional Desktop shortcut named `Language Voice Tutor`;
- offers an optional final wizard action to launch Language Voice Tutor after installation;
- installs published app files only from `artifacts\publish\win-x64-inno`;
- does not package local app data, local auth session files, local settings, lesson history, backend environment files, or secrets;
- preserves user app data by default during update/reinstall.

Because the default installation directory is under Program Files, the installer requires administrator privileges. Standard uninstall removes installed application files and shortcuts. It should not delete user settings, session/account state, cache, or backend account data by default because those are outside the install directory and/or owned by the backend.

## Installed-version behavior

Installed-version checking is now part of the Windows installer foundation. The Inno Setup installer keeps the same `LanguageVoiceTutor.Desktop` AppId and reads the installed Language Voice Tutor version from the standard Inno uninstall registry entry before continuing.

- Same-version install asks for reinstall confirmation and cancels if the user declines.
- Older installed version is treated as an update and may continue after the installer explains that it will update Language Voice Tutor.
- Newer installed version warns and blocks by default so testers do not accidentally downgrade to an older installer.
- If Language Voice Tutor is running, the installer uses Inno Setup close-application handling for `LanguageVoiceTutor.Desktop.exe` and also handles the legacy `EnglishVoiceTutor.Desktop.exe` process during updates from older installed builds; it must not silently install over a running app.
- During update/reinstall, the installer removes legacy `EnglishVoiceTutor.Desktop.*` application files only from the install folder so stale old-name binaries are not left beside the current `LanguageVoiceTutor.Desktop.*` files. It does not delete user AppData, settings, auth/session files, lesson history, progress, or backend data. The app must migrate preserved auth/session data from legacy `EnglishVoiceTutor.Desktop` local-data paths to the current `LanguageVoiceTutor.Desktop` local-data path when needed.

## Build the installer locally

Run from the repository root on Windows:

```powershell
$ReleaseVersion = "<next-tester-version>"
powershell -ExecutionPolicy Bypass -File .\scripts\package-windows-inno-release.ps1 -Version $ReleaseVersion
```

If `ISCC.exe` is not in a default location:

```powershell
$ReleaseVersion = "<next-tester-version>"
powershell -ExecutionPolicy Bypass -File .\scripts\package-windows-inno-release.ps1 -Version $ReleaseVersion -IsccPath "C:\Tools\Inno Setup 6\ISCC.exe"
```

Expected installer output:

```text
artifacts\installers\windows\LanguageVoiceTutorSetup-{version}.exe
```

Expected server-ready direct-download output:

```text
artifacts\releases\windows\direct\LanguageVoiceTutorSetup-{version}.exe
artifacts\releases\windows\direct\latest.json
artifacts\releases\windows\direct\changelog.json
artifacts\releases\windows\direct\known-issues.json
artifacts\releases\windows\direct\checksums.sha256
```

Generated files under `artifacts\` must not be committed. Generated artifacts are not source of truth for the public/live Windows release until uploaded and verified through live `latest.json`.

## Manifest and desktop update UX

`latest.json` includes product identity, platform, architecture, channel, version, UTC release date, installer filename/relative URL, SHA-256, size, non-secret `backendBaseUrl`, minimum supported version, manual-confirmation update mode, and release notes. It must not include absolute local file paths.

The desktop release UX has a simple user-facing **Check for updates** button in Settings. It fetches `latest.json`, validates manifest identity, compares installed and latest versions, asks before downloading/installing, verifies SHA-256 before starting the installer, and does not silently auto-update. The old technical update dashboard is not part of release UX.

Update/reinstall validation must confirm app data, persisted auth session storage, settings, and account-scoped local Progress/Lesson History survive. Update/reinstall must preserve login, settings, Lesson History, and Progress after the installed file-name rename.

## Validate release files

After building the Inno release, run:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\validate-windows-direct-release.ps1
```

Then verify over HTTPS from a client machine:

```powershell
$manifest = Invoke-RestMethod -Uri "https://languagevoicetutor.com/releases/windows/direct/latest.json?t=$(Get-Date -Format yyyyMMddHHmmss)"
$installerName = $manifest.installerFileName
Invoke-WebRequest -Uri "https://languagevoicetutor.com/releases/windows/direct/$($manifest.installerRelativeUrl)" -OutFile "$env:TEMP\$installerName"
Get-FileHash -Path "$env:TEMP\$installerName" -Algorithm SHA256
```

Compare the downloaded installer hash with `checksums.sha256` and the `installerSha256` value in `latest.json`.

## Code signing readiness

Code signing remains a planned release-hardening step, not an implemented packaging behavior. Windows Direct Release 1.0 remains unsigned under a documented owner-accepted exception, so SmartScreen/trust friction is a known release risk. For a future signed direct release, the final Inno Setup installer should be signed and signature verification must be added before upload. See `docs/WINDOWS_CODE_SIGNING_READINESS.md` for the current planning audit, non-secret handling rules, future signing/verification placement, and certificate option comparison.

## Security notes

- Do not commit generated `artifacts/` files or installer `.exe` files.
- Do not store secrets in the repository.
- Do not place API keys, OpenAI keys, backend secrets, local auth/session files, local settings, local lesson history, `.env` files, SSH private keys, passwords, tokens, or provider credentials in release files.
- Verify checksums after upload and again over HTTPS before sharing links.
- Keep the update UI manual: require explicit check/download/open actions, use the Inno installer, verify SHA-256, and avoid silent updates.
- Keep production billing/payment lifecycle deferred.
