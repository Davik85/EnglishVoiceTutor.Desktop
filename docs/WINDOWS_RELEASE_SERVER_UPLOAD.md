# Windows release server upload

Review date: 2026-06-29.

## Source of truth for current versions

The live website manifest is the public source of truth for the Windows direct release:

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

Current public direct release values:

```text
channel: direct-public
version: 1.1
installerFileName: LanguageVoiceTutorSetup-1.1.exe
installerRelativeUrl: LanguageVoiceTutorSetup-1.1.exe
backendBaseUrl: https://api.languagevoicetutor.com
updateMode: manual-confirmation
minimumSupportedVersion: 1.1
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

- `version` is `1.1` or the intended newly uploaded direct version;
- `installerFileName` is `LanguageVoiceTutorSetup-1.1.exe` and `installerRelativeUrl` is `LanguageVoiceTutorSetup-1.1.exe`, or both match the intended installer;
- `backendBaseUrl` is `https://api.languagevoicetutor.com`;
- `updateMode` is `manual-confirmation`;
- `minimumSupportedVersion` is `1.1` for this uploaded direct release; this is intentional because Windows Direct `1.1` contains the desktop auth/session refresh stability fix;
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

Then open `https://languagevoicetutor.com/download.html` in a browser, click **Download for Windows**, and confirm the downloaded file name is the same as `$manifest.installerFileName`. The page must keep working as a controlled direct Windows release page, not as a broad public production launch announcement. When Website CMS has been published with static release details available, `download.html` should show the current Windows installer details from `latest.json`. This verification is separate from Website CMS Publish and does not itself publish public website HTML/CSS/JS.

## Download page behavior

The static public direct download page keeps `download.js` and `/releases/windows/direct/latest.json` support. It is also useful without JavaScript: when the local/public manifest is available, static HTML shows the current release details instead of only showing Loading or Unavailable.

Required static fallback text:

- “Current Windows direct release is available through the Download for Windows button.”
- “If release details do not load automatically, please contact [support@languagevoicetutor.com](mailto:support@languagevoicetutor.com).”

## Release-readiness status and deferred items

Current backend is production healthy at `https://api.languagevoicetutor.com`, release `0.1.35-backend.108`. Website Paddle-review polish is completed separately from this upload flow. Paddle live payment validation, Premium activation, failed-payment non-activation, cancel-renewal, and full-refund Premium revocation are complete; chargeback remains implemented/test-covered but not live-chargeback-tested and partial refunds remain conservative/manual-review. Legal/support/seller/AI/status pages are ready for owner/legal final review as drafts.

Do not state that the product is fully public production-ready. The current Windows release remains a public Windows direct release, not a full broad production-readiness claim, and not broad public production readiness.

Code signing remains deferred. Expanded customer portal/subscription management and broader paid-launch approval remain deferred. CMS published-snapshot runtime is active for published Windows direct lessons. Backend deployment, database migrations, static website publishing, and update UI remain separate work.

## Windows distribution boundary

Microsoft Store/MSIX was evaluated and discontinued for now. Do not use this upload process for Store/MSIX packages, and do not add MSIX prototype commands here.

This document uploads only the Direct EXE/Inno installer channel files to `/var/www/languagevoicetutor/releases/windows/direct` and maintains the direct `latest.json` flow. Backend deploy, Website CMS/static site publish, Windows direct installer upload, and database migrations remain separate processes.

## 2026-06-30 Paddle live checkout/Admin readiness update

Current production facts after backend `0.1.35-backend.83` and before any real live payment test:

- Backend health and database health are `200 Healthy`.
- Backend server-side Paddle configuration is in the existing env file `/etc/languagevoicetutor/backend.env`; do not invent a second env file and do not create Paddle live systemd drop-ins for this configuration.
- Backend current symlink is `/opt/languagevoicetutor/backend/current`; backend releases are under `/opt/languagevoicetutor/backend/releases/<version>`.
- AI Models persistent server data remains `/opt/languagevoicetutor/backend/site/content/ai-model-settings.json`; known-good models remain `gpt-5.5` for lesson tutor chat and `gpt-5.2` for feedback correction, lesson hints, and translation.
- Static website nginx root is `/var/www/languagevoicetutor/site`. The parent `/var/www/languagevoicetutor` is not the nginx static-site root and must not be used as the static website upload target.
- Public Paddle config is `/var/www/languagevoicetutor/site/paddle.public.json`; public Paddle checkout page is `/var/www/languagevoicetutor/site/pay.html`.
- Direct Windows release files are separate at `/var/www/languagevoicetutor/releases/windows/direct` and are not touched by static website upload.
- Active Windows delivery remains Direct EXE/Inno. Store/MSIX is discontinued and must not be reintroduced. Current direct public release is `1.1`; direct `latest.json` remains active with manual-confirmation update mode.
- Paddle website review is approved, `/pay.html` and `/paddle.public.json` are deployed/reachable, backend live Paddle env is configured, and a real transaction URL opened Paddle checkout with `Language Voice Tutor Pro`, `Pro Monthly`, `14.99 EUR`.
- Controlled live payment, webhook delivery, Premium entitlement activation, failed-payment non-activation, cancel-renewal, and full-refund Premium revocation are completed. Paid-launch readiness remains incomplete until final release-readiness review and remaining non-billing blockers are closed; chargeback remains implemented/test-covered but not live-chargeback-tested, partial refund remains conservative/manual-review, and expanded customer portal/subscription management is deferred.

Static website upload command must target the real nginx root:

```powershell
pwsh -NoProfile -ExecutionPolicy Bypass -File .\scripts\upload-static-site.ps1 `
  -ServerHost "lvt-server" `
  -ServerUser "deploy" `
  -RemotePath "/var/www/languagevoicetutor/site"
```

Safe backend env verification must redact secrets and must use the existing env file, for example:

```bash
sudo awk -F= '/^(Billing__|PaddleBilling__|PaddleWebhook__)/ { v=$2; if ($1 ~ /(ApiKey|SecretKey|Token)/) v=(length($2)>0 ? "SET" : "EMPTY"); print $1 "=" v }' /etc/languagevoicetutor/backend.env
```

Admin capabilities should now distinguish configuration from launch completion: configured live checkout/webhooks can be reported as available/configured, while `billingLivePaymentTestComplete=false` and `billingPaidLaunchReleaseComplete=false` continue to block paid launch until the controlled live payment path is documented.

## Static site upload boundary after Download page polish

`upload-static-site.ps1` is the public website upload helper, not a backend deployment helper and not a Windows installer release helper. Use PowerShell 7 / `pwsh` for the updated script:

```powershell
pwsh -NoProfile -ExecutionPolicy Bypass -File .\scripts\upload-static-site.ps1 `
  -ServerHost "lvt-server" `
  -ServerUser "deploy" `
  -RemotePath "/var/www/languagevoicetutor/site"
```

Current behavior: it uploads `site/public` root files and top-level folders such as `site/public/assets`, groups uploads instead of running one `mkdir`/`scp` per file, and skips `site/public/releases/**` completely. It must not upload `site/public/releases/windows/direct/latest.json`, does not manage `LanguageVoiceTutorSetup-1.1.exe`, does not manage any Windows release files, and does not deploy the backend. Windows direct release files remain managed only by the Windows direct release upload flow.
