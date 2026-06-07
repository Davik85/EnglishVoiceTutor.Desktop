# Windows direct-release server upload foundation

This document describes the safe, optional foundation for copying the already generated Windows direct-download release files to a future HTTPS static server folder.

The primary Windows installer track is the Inno Setup installer. ZIP packages remain only an emergency/developer fallback. This upload foundation does not deploy the backend, does not create the download website, does not implement update UI, does not make the app public-release ready, and does not sign the installer. Code signing is still deferred.

## Purpose

The Inno release script creates a server-ready release folder that can later be mirrored to a static HTTPS location. The folder is intended to hold the installer and small release metadata files for a future download page and future manual-confirmation update-check flow.

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
powershell -ExecutionPolicy Bypass -File .\scripts\package-windows-inno-release.ps1 -Version 0.1.5-tester.1
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
Invoke-WebRequest -Uri "https://example.com/releases/windows/direct/latest.json" -OutFile "$env:TEMP\latest.json"
Invoke-WebRequest -Uri "https://example.com/releases/windows/direct/LanguageVoiceTutorSetup-0.1.5-tester.1.exe" -OutFile "$env:TEMP\LanguageVoiceTutorSetup-0.1.5-tester.1.exe"
Invoke-WebRequest -Uri "https://example.com/releases/windows/direct/changelog.json" -OutFile "$env:TEMP\changelog.json"
Invoke-WebRequest -Uri "https://example.com/releases/windows/direct/known-issues.json" -OutFile "$env:TEMP\known-issues.json"
Invoke-WebRequest -Uri "https://example.com/releases/windows/direct/checksums.sha256" -OutFile "$env:TEMP\checksums.sha256"
Get-FileHash -Path "$env:TEMP\LanguageVoiceTutorSetup-0.1.5-tester.1.exe" -Algorithm SHA256
```

Compare the downloaded installer hash with `checksums.sha256` and the `installerSha256` value in `latest.json`.

## Security notes

- Do not store secrets in the repository.
- Do not place API keys, OpenAI keys, backend secrets, local auth/session files, local settings, local lesson history, `.env` files, SSH private keys, passwords, tokens, or provider credentials in release files.
- Use SSH keys or secure authentication managed outside the repository.
- Verify checksums after upload and again over HTTPS before sharing links.
- Keep backend deployment as a separate later step.
- Keep update UI as a separate later step. Any future update UI must require manual confirmation, use the Inno installer, and avoid update prompts during active lessons.
- External tester handoff remains blocked until server/static download, clean-machine install, and the controlled tester checklist all pass.
