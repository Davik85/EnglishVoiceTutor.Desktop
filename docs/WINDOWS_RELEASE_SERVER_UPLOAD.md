# Windows release server upload

Review date: 2026-06-28.

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
version: 0.1.36-tester.30
installerFileName: LanguageVoiceTutorSetup-0.1.36-tester.30.exe
backendBaseUrl: https://api.languagevoicetutor.com
updateMode: manual-confirmation
```

Release/tester installed builds are server-only. The production backend URL for packaged non-Debug Windows builds is `https://api.languagevoicetutor.com`.

## Upload scope

Windows direct-release upload publishes static release files only. It does not deploy the backend, does not run EF migrations, does not upload secrets, does not publish public website HTML/CSS/JS, does not enable live Paddle, and does not make the product broadly public production-ready.

Backend deployment, database migrations, static website publish, and Windows direct installer upload are separate flows.

## Upload process

Use the Windows direct-release upload helper:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\upload-windows-direct-release.ps1 -Version 0.1.36-tester.30
```

Do not manually `scp` installer files when `scripts/upload-windows-direct-release.ps1` exists.

After upload, verify from a client machine:

```powershell
$manifest = Invoke-RestMethod -Uri "https://languagevoicetutor.com/releases/windows/direct/latest.json?t=$(Get-Date -Format yyyyMMddHHmmss)"
$manifest.version
$manifest.installerFileName
$manifest.backendBaseUrl
$manifest.updateMode
Invoke-WebRequest -Uri "https://languagevoicetutor.com/releases/windows/direct/$($manifest.installerRelativeUrl)" -OutFile "$env:TEMP\$($manifest.installerFileName)"
Get-FileHash -Path "$env:TEMP\$($manifest.installerFileName)" -Algorithm SHA256
```

Confirm:

- `version` is `0.1.36-tester.30` or the intended newly uploaded tester version;
- `installerFileName` is `LanguageVoiceTutorSetup-0.1.36-tester.30.exe` or the matching intended installer;
- `backendBaseUrl` is `https://api.languagevoicetutor.com`;
- `updateMode` is `manual-confirmation`;
- the SHA-256 matches `installerSha256` and `checksums.sha256`;
- the public download page button downloads the same installer named by the manifest.

## Download page behavior

The static tester download page keeps `download.js` and `/releases/windows/direct/latest.json` support. It is also useful without JavaScript: when the local/public manifest is available, static HTML shows the current release details instead of only showing Loading or Unavailable.

Required static fallback text:

- “Current Windows tester release is available through the Download for Windows button.”
- “If release details do not load automatically, please contact [support@languagevoicetutor.com](mailto:support@languagevoicetutor.com).”

## Release-readiness status and deferred items

Current backend is production healthy at `https://api.languagevoicetutor.com`, release `0.1.35-backend.74`. Website Paddle-review polish is completed separately from this upload flow. Paddle live is not enabled yet. Legal/support/seller/AI/status pages are ready for owner/legal final review as drafts.

Do not state that the product is fully public production-ready. The current Windows release remains a controlled tester/direct Windows release, not a broad public production launch, and not broad public production readiness.

Code signing remains deferred. Production billing/Paddle/subscription payment lifecycle remains deferred. CMS published-snapshot runtime is active for controlled tester lessons. Backend deployment, database migrations, the download website, and update UI remain separate work.
