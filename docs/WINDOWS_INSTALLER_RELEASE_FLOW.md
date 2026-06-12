# Windows installer release flow

Review date: 2026-06-12.

Inno Setup is the primary Windows direct-download installer track for Language Voice Tutor.

## Current validated release

`0.1.28-tester.1` is the current public tester Windows direct manifest baseline. The Windows direct installer was built and validated. The public tester download page reads `/releases/windows/direct/latest.json`, and `latest.json` points to `LanguageVoiceTutorSetup-0.1.28-tester.1.exe` with `backendBaseUrl` set to `https://api.languagevoicetutor.com`, `minimumSupportedVersion` set to `0.1.28-tester.1`, and `updateMode` set to `manual-confirmation`.

This is a private tester/direct Windows release, not broad public production readiness. Code signing remains deferred, so SmartScreen warnings are still expected for controlled testers.

## Decision

- Primary installer technology: Inno Setup 6.
- Public product name: Language Voice Tutor.
- Desktop window title: Language Voice Tutor Desktop.
- Stable installer AppId: `LanguageVoiceTutor.Desktop`.
- Default install directory: `{autopf}\Language Voice Tutor`, normally under Program Files.
- Expected installer artifact: `artifacts\installers\windows\LanguageVoiceTutorSetup-{version}.exe`.
- Server-ready direct-download output: `artifacts\releases\windows\direct`.
- Installed tester/release output files now use `LanguageVoiceTutor.Desktop.*` names. Internal project, folder, and namespace names may remain `EnglishVoiceTutor.*` until a later safe cleanup to avoid risky project-wide churn.

Velopack is rejected/deprecated for this project because its Windows installer is a one-click installer and does not match the desired release-like wizard UX. ZIP packaging remains only an emergency/developer fallback. Microsoft Store/MSIX remains deferred.

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
- During update/reinstall, the installer removes legacy `EnglishVoiceTutor.Desktop.*` application files only from the install folder so stale old-name binaries are not left beside the current `LanguageVoiceTutor.Desktop.*` files. It does not delete user AppData, settings, auth/session files, lesson history, progress, or backend data.

## Build the installer locally

Run from the repository root on Windows:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\package-windows-inno-release.ps1 -Version 0.1.28-tester.1
```

If `ISCC.exe` is not in a default location:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\package-windows-inno-release.ps1 -Version 0.1.28-tester.1 -IsccPath "C:\Tools\Inno Setup 6\ISCC.exe"
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

Generated files under `artifacts\` must not be committed.

## Manifest and desktop update UX

`latest.json` includes product identity, platform, architecture, channel, version, UTC release date, installer filename/relative URL, SHA-256, size, non-secret `backendBaseUrl`, minimum supported version, manual-confirmation update mode, and release notes. It must not include absolute local file paths.

The desktop release UX has a simple user-facing **Check for updates** button in Settings. It fetches `latest.json`, validates manifest identity, compares installed and latest versions, asks before downloading/installing, verifies SHA-256 before starting the installer, and does not silently auto-update. The old technical update dashboard is not part of release UX.

Update/reinstall validation must confirm app data, persisted auth session storage, settings, and account-scoped local Progress/Lesson History survive.

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

## Security notes

- Do not commit generated `artifacts/` files or installer `.exe` files.
- Do not store secrets in the repository.
- Do not place API keys, OpenAI keys, backend secrets, local auth/session files, local settings, local lesson history, `.env` files, SSH private keys, passwords, tokens, or provider credentials in release files.
- Verify checksums after upload and again over HTTPS before sharing links.
- Keep the update UI manual: require explicit check/download/open actions, use the Inno installer, verify SHA-256, and avoid silent updates.
- Keep production billing/payment lifecycle deferred.
