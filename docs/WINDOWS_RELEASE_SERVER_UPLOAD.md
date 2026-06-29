# Windows release server upload

Review date: 2026-06-29.

## Source of truth for current versions

The live website manifest is the public source of truth for the Windows direct tester release:

```powershell
Invoke-RestMethod https://languagevoicetutor.com/releases/windows/direct/latest.json
```

The production backend release must be verified from the server `current` symlink:

```powershell
ssh lvt-server "readlink -f /opt/languagevoicetutor/backend/current"
```

Generated local files under `artifacts/` are not proof that a version is live on the public site. A locally built installer becomes public only after the Windows direct release files are uploaded to the website release folder and `latest.json` is verified over HTTPS. Generated artifacts must not be committed.

## Current uploaded release

Windows direct release manifest:

```text
https://languagevoicetutor.com/releases/windows/direct/latest.json
```

Current public tester values:

```text
version: 0.1.36-tester.31
installerFileName: LanguageVoiceTutorSetup-0.1.36-tester.31.exe
backendBaseUrl: https://api.languagevoicetutor.com
updateMode: manual-confirmation
minimumSupportedVersion: 0.1.36-tester.31
```

This release has been built, uploaded, and verified. The user confirmed the newly uploaded build works and that manual-confirmation update flow works on other devices.

Release/tester installed builds are server-only. The production backend URL for packaged non-Debug Windows builds is `https://api.languagevoicetutor.com`.

## Upload scope

Windows direct-release upload publishes static release files only. It does not deploy the backend, does not run EF migrations, does not upload secrets, does not publish public website HTML/CSS/JS, does not enable live Paddle, and does not make the product broadly public production-ready. It does not:

- deploy the backend;
- run EF migrations;
- apply reviewed SQL;
- publish Website CMS content;
- publish public website HTML/CSS/JS;
- upload secrets;
- enable live Paddle;
- make the product broadly public production-ready.

Backend deployment, database migrations, Website CMS content, static website publish, and Windows direct installer upload are separate flows. Do not manually `scp` installer files when `scripts/upload-windows-direct-release.ps1` exists.

## Dry-run upload

Use the repository upload helper in dry-run mode first. The helper validates the local direct-release folder, prints the SSH/SCP work it would perform, and does not upload files while `-DryRun` is present.

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\upload-windows-direct-release.ps1 `
  -ServerHost lvt-server `
  -ServerUser deploy `
  -RemotePath /var/www/languagevoicetutor/releases/windows/direct `
  -DryRun
```

If the release files are in a non-default local directory, pass `-ReleaseDirectory` explicitly:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\upload-windows-direct-release.ps1 `
  -ServerHost lvt-server `
  -ServerUser deploy `
  -RemotePath /var/www/languagevoicetutor/releases/windows/direct `
  -ReleaseDirectory .\artifacts\releases\windows\direct `
  -DryRun
```

The current deploy SSH user for these copy-ready commands is `deploy`. Use a different `-ServerUser` only for an intentionally reviewed non-default SSH account.

## Real upload

After the local release has been validated and the dry-run output is reviewed, run the same helper without `-DryRun`:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\upload-windows-direct-release.ps1 `
  -ServerHost lvt-server `
  -ServerUser deploy `
  -RemotePath /var/www/languagevoicetutor/releases/windows/direct
```

The helper uploads only `latest.json`, `changelog.json`, `known-issues.json`, `checksums.sha256`, and the installer file named by `latest.json` from the local Windows direct-release directory.

## Manifest verification

Verify the public manifest over HTTPS after upload:

```powershell
$manifest = Invoke-RestMethod https://languagevoicetutor.com/releases/windows/direct/latest.json
$manifest.version
$manifest.installerFileName
$manifest.backendBaseUrl
$manifest.updateMode
$manifest.installerRelativeUrl
$manifest.installerSha256
$manifest.checksums.sha256
```

Confirm:

- `version` is `0.1.36-tester.31` or the intended newly uploaded tester version;
- `installerFileName` is `LanguageVoiceTutorSetup-0.1.36-tester.31.exe` or the matching intended installer;
- `backendBaseUrl` is `https://api.languagevoicetutor.com`;
- `updateMode` is `manual-confirmation`;
- `minimumSupportedVersion` is `0.1.36-tester.31` for this uploaded tester release;
- `installerSha256` and `checksums.sha256` are present and agree with the uploaded installer hash.

## Installer download verification

Download the installer referenced by the manifest from the public site:

```powershell
$manifest = Invoke-RestMethod -Uri "https://languagevoicetutor.com/releases/windows/direct/latest.json?t=$(Get-Date -Format yyyyMMddHHmmss)"
$installerPath = Join-Path $env:TEMP $manifest.installerFileName
Invoke-WebRequest -Uri "https://languagevoicetutor.com/releases/windows/direct/$($manifest.installerRelativeUrl)" -OutFile $installerPath
Get-Item $installerPath | Select-Object FullName, Length
```

The downloaded file name must match `installerFileName` in the manifest.

## SHA-256 hash verification

Compare the downloaded installer hash to the manifest and checksum file:

```powershell
$actualHash = (Get-FileHash -Path $installerPath -Algorithm SHA256).Hash.ToLowerInvariant()
$expectedHash = ([string]$manifest.installerSha256).ToLowerInvariant()
$expectedChecksumHash = ([string]$manifest.checksums.sha256).ToLowerInvariant()
$actualHash
$expectedHash
$expectedChecksumHash
if ($actualHash -ne $expectedHash -or $actualHash -ne $expectedChecksumHash) { throw "Installer SHA-256 does not match manifest values." }
```

If the hash does not match, do not share the download link. Re-check the local release folder, upload target, and manifest before retrying.

## Download page button verification

Verify the download page resolves and that the page button downloads the same installer named by the manifest:

```powershell
Invoke-WebRequest https://languagevoicetutor.com/download.html -UseBasicParsing
```

Then open `https://languagevoicetutor.com/download.html` in a browser, click **Download for Windows**, and confirm the downloaded file name is the same as `$manifest.installerFileName`. The page must keep working as a controlled tester/direct Windows release page, not as a broad public production launch announcement. When Website CMS has been published with static release details available, `download.html` should show the current Windows installer details from `latest.json`. This verification is separate from Website CMS Publish and does not itself publish public website HTML/CSS/JS.

## Download page behavior

The static tester download page keeps `download.js` and `/releases/windows/direct/latest.json` support. It is also useful without JavaScript: when the local/public manifest is available, static HTML shows the current release details instead of only showing Loading or Unavailable.

Required static fallback text:

- “Current Windows tester release is available through the Download for Windows button.”
- “If release details do not load automatically, please contact [support@languagevoicetutor.com](mailto:support@languagevoicetutor.com).”

## Release-readiness status and deferred items

Current backend is production healthy at `https://api.languagevoicetutor.com`, release `0.1.35-backend.77`. Website Paddle-review polish is completed separately from this upload flow. Paddle live is not enabled yet. Legal/support/seller/AI/status pages are ready for owner/legal final review as drafts.

Do not state that the product is fully public production-ready. The current Windows release remains a controlled tester/direct Windows release, not a broad public production launch, and not broad public production readiness.

Code signing remains deferred. Production billing/Paddle/subscription payment lifecycle remains deferred. CMS published-snapshot runtime is active for controlled tester lessons. Backend deployment, database migrations, the download website, and update UI remain separate work.
