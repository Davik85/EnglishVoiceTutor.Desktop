# Windows direct-release server upload foundation

This document describes the safe, optional foundation for copying the already generated Windows direct-download release files to a future HTTPS static server folder.

The primary Windows installer track is the Inno Setup installer. ZIP packages remain only an emergency/developer fallback. This upload foundation does not deploy the backend, does not create the download website, is consumed by the basic manual desktop update UI, does not make the app public-release ready, and does not sign the installer. Code signing is still deferred.

## Current v0.1.17-tester.1 hosting validation

Static Windows direct release hosting has been validated for `0.1.17-tester.1`. The Windows installer was generated, validated, uploaded, and the server-side release files were verified. `latest.json` for `0.1.17-tester.1` is available from the production domain.

This is hosting validation only. The desktop app now has a basic manual Settings update check UI that reads this `latest.json`, but hosting validation does not sign the installer and does not approve external tester handoff. Code signing remains deferred. The app still does not automatically check `latest.json` in the background and does not auto-update.


Backend API deployment is documented separately in [`BACKEND_SERVER_DEPLOYMENT.md`](BACKEND_SERVER_DEPLOYMENT.md). Keep the static Windows direct-download files on `languagevoicetutor.com` separate from the future backend API reverse proxy on `api.languagevoicetutor.com`.

## Purpose

Desktop authenticated session persistence is now part of the tester-readiness foundation. The desktop does not store raw passwords; token/session data is stored under the current user app-data folder with Windows DPAPI protection. Logout clears persisted auth session data. Reinstall/update should preserve user app data and session storage. Same-version installer reinstall confirmation remains in place. The basic in-app manual update check now reads `latest.json`, validates the manifest identity, compares installed versus latest tester versions, asks before downloading/installing, verifies SHA-256 before offering to start the installer, and does not perform silent auto-update. Testers should not update during an active lesson; deeper active-lesson integration remains follow-up. External tester handoff remains blocked until persisted-session verification and clean-machine smoke pass.

The Inno release script creates a server-ready release folder that can later be mirrored to a static HTTPS location. The folder is intended to hold the installer and small release metadata files for a future download page and future manual-confirmation update-check flow.

Production tester builds must use `https://api.languagevoicetutor.com`. Before tester handoff, clean-machine smoke must verify health, registration, login, settings sync, lesson start, history, progress, password reset, and update check from a real installed build, and the installed-build backend connectivity issue must be verified fixed on a second Windows device.

The backend remains the source of truth for accounts, access, subscriptions, lessons, AI calls, and runtime app behavior. The desktop app must not store or call OpenAI API keys directly, and release files must not contain API keys or other secrets.

## Local source folder

Generated files are written to:

```text
artifacts\releases\windows\direct
```

Expected files after a successful Inno release build:

```text
artifacts\releases\windows\direct\latest.json
artifacts\releases\windows\direct\changelog.json
artifacts\releases\windows\direct\known-issues.json
artifacts\releases\windows\direct\checksums.sha256
artifacts\releases\windows\direct\LanguageVoiceTutorSetup-{version}.exe
```

Generated artifacts under `artifacts\` must not be committed.

## Future server folder

Recommended future static server folder:

```text
/var/www/languagevoicetutor/releases/windows/direct
```

The upload script accepts a `-RemotePath` parameter, so the actual server path can change later without changing repository code.

## Expected public HTTPS URLs later

Once a server and HTTPS site are selected and configured, the expected public paths should be:

```text
/releases/windows/direct/latest.json
/releases/windows/direct/LanguageVoiceTutorSetup-{version}.exe
/releases/windows/direct/changelog.json
/releases/windows/direct/known-issues.json
/releases/windows/direct/checksums.sha256
```

These are path expectations only. This task does not configure nginx, another web server, DNS, certificates, backend deployment, or a download website.

## Validate local release files

Build the Inno release first on a Windows machine with Inno Setup installed:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\package-windows-inno-release.ps1 -Version 0.1.17-tester.1
```

Then validate the generated direct-release folder:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\validate-windows-direct-release.ps1
```

To validate a different folder:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\validate-windows-direct-release.ps1 -ReleaseDirectory "C:\path\to\direct"
```

The validation script does not require internet access. It checks required files, parses JSON, verifies the expected Language Voice Tutor Windows x64 manifest fields, rejects obvious local Windows paths in `latest.json`, confirms matching versions across manifests, and verifies the installer SHA-256 against both `latest.json` and `checksums.sha256`.

## Dry-run upload

Use dry-run mode before any real upload. Dry-run validates local files and prints the SSH/SCP commands that would run, but it does not create remote directories and does not copy files.

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\upload-windows-direct-release.ps1 `
  -ServerHost "example-host" `
  -ServerUser "deploy" `
  -RemotePath "/var/www/languagevoicetutor/releases/windows/direct" `
  -DryRun
```

Use placeholders until real server SSH access exists. Do not commit real hostnames, usernames, IP addresses, SSH key paths, passwords, tokens, or secrets.

## Upload after server SSH access exists

After a server is selected, HTTPS/static hosting is configured separately, and SSH authentication exists outside the repository, run:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\upload-windows-direct-release.ps1 `
  -ServerHost "your-server-host" `
  -ServerUser "your-ssh-user" `
  -RemotePath "/var/www/languagevoicetutor/releases/windows/direct"
```

If the server uses a non-default SSH port:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\upload-windows-direct-release.ps1 `
  -ServerHost "your-server-host" `
  -ServerUser "your-ssh-user" `
  -RemotePath "/var/www/languagevoicetutor/releases/windows/direct" `
  -SshPort 2222
```

The script validates first, runs `ssh` to create the remote directory with `mkdir -p`, then uses `scp` to upload `latest.json`, `changelog.json`, `known-issues.json`, `checksums.sha256`, and the installer named by `latest.json`.

## Verify on the server after upload

After upload, verify the remote files and checksum over SSH. Example commands:

```powershell
ssh your-ssh-user@your-server-host "ls -lh /var/www/languagevoicetutor/releases/windows/direct"
ssh your-ssh-user@your-server-host "cd /var/www/languagevoicetutor/releases/windows/direct && sha256sum -c checksums.sha256"
```

If `sha256sum` is unavailable on the server, use the server's equivalent checksum tool and compare the result with `checksums.sha256` and `latest.json`.

## Verify over HTTPS later

After DNS, HTTPS, and static serving are configured separately, verify the public URLs from a client machine:

```powershell
$manifest = Invoke-RestMethod -Uri "https://example.com/releases/windows/direct/latest.json?t=$(Get-Date -Format yyyyMMddHHmmss)"
$installerName = $manifest.installerFileName
Invoke-WebRequest -Uri "https://example.com/releases/windows/direct/$($manifest.installerRelativeUrl)" -OutFile "$env:TEMP\$installerName"
Invoke-WebRequest -Uri "https://example.com/releases/windows/direct/changelog.json" -OutFile "$env:TEMP\changelog.json"
Invoke-WebRequest -Uri "https://example.com/releases/windows/direct/known-issues.json" -OutFile "$env:TEMP\known-issues.json"
Invoke-WebRequest -Uri "https://example.com/releases/windows/direct/checksums.sha256" -OutFile "$env:TEMP\checksums.sha256"
Get-FileHash -Path "$env:TEMP\$installerName" -Algorithm SHA256
```

Compare the downloaded installer hash with `checksums.sha256` and the `installerSha256` value in `latest.json`.

## Security notes

- Do not store secrets in the repository.
- Do not place API keys, OpenAI keys, backend secrets, local auth/session files, local settings, local lesson history, `.env` files, SSH private keys, passwords, tokens, or provider credentials in release files.
- Use SSH keys or secure authentication managed outside the repository.
- Verify checksums after upload and again over HTTPS before sharing links.
- Keep backend deployment as a separate later step.
- Keep the implemented update UI manual: require explicit check/download/open actions, use the Inno installer, verify SHA-256, and avoid update/install activity during active lessons.
- External tester handoff remains blocked until clean-machine install and the controlled tester checklist pass.

## Static tester download page foundation

A basic public download page foundation is now prepared under `site/public/`. The page is static and uses plain HTML, CSS, and JavaScript only. It reads `latest.json` from the existing Windows direct release folder at `/releases/windows/direct/latest.json` and uses `installerRelativeUrl` from that manifest for the primary Windows download button.

This tester page does not implement auto-update and complements the in-app manual update check, and does not change the Windows direct release files under `/releases/windows/direct`. It is only a tester download page for invited testers. The page includes the private tester status, release details when the manifest loads, the manifest installer filename as a visible mismatch check, the SmartScreen/code-signing-deferred warning, and the support email address. The page must not hardcode old installer filenames; if `latest.json` cannot be loaded or is invalid, the button must remain disabled instead of serving a fallback installer. After every Windows release upload, verify both the displayed version and the actual downloaded filename from the public page.

Use `scripts/upload-static-site.ps1` only to copy files from `site/public/` to the remote static website folder. The helper prints a summary, supports `-DryRun`, and must not be used for backend deployment or Windows release-file upload. Continue using the Windows direct-release upload helper only for release artifacts.

External tester handoff is still blocked until the manual update UI verification and the clean-machine smoke checklist pass. This static page does not make the desktop app public-release ready and does not approve external tester handoff.


## Windows installer installed-version check foundation

Installed-version checking is now part of the Windows installer foundation. The Inno Setup installer keeps the same `LanguageVoiceTutor.Desktop` AppId and reads the installed Language Voice Tutor version from the standard Inno uninstall registry entry before continuing. The next tester installer package should be built as `0.1.17-tester.1` unless release conventions intentionally choose a different SemVer-compatible tester version.

- Same-version install asks for reinstall confirmation with the message: "Language Voice Tutor version <version> is already installed. Do you want to reinstall the same version?" The installer continues only when the user confirms.
- Older installed version is treated as an update. The installer shows a clear update message and then continues through the normal installer flow.
- Newer installed version warns and blocks by default. The installer warns that installing the older package may downgrade the app, then exits without making changes.
- If Language Voice Tutor is running, the installer uses Inno Setup close-application handling for `EnglishVoiceTutor.Desktop.exe`; it must not silently install over a running app.

The basic in-app update UI now checks `latest.json` manually, verifies installer SHA-256, and guides the user through download/open actions with explicit confirmation. It is not a silent updater or background service. Active-lesson detection is documented in the UI as a safety rule; deeper Settings-level blocking remains future work because the installer cannot safely inspect lesson state and Settings does not currently expose active lesson state.

External tester handoff is still blocked until clean-machine smoke passes.


## Manifest and desktop update UX

The Windows release manifest supports the desktop manual-confirmation update flow. The Settings **Check for updates** action fetches `latest.json`, validates the manifest identity, compares installed and latest versions, asks before downloading/installing, verifies SHA-256 before starting the installer, and does not silently auto-update. The update flow must preserve app data, persisted auth session storage, and account-scoped local Progress/Lesson History through reinstall/update. Clean-machine smoke remains required before external tester handoff.
