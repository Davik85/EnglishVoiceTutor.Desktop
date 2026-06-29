# Command Playbook

Review date: 2026-06-18.

## Source of truth for current versions

These docs are a snapshot of the last known verified state. They can become stale and must not be used as the only source of truth for live versions. Always verify the live/public state before telling a tester that a version is current.

Check the public Windows direct tester release from the live website manifest:

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

## Public website deployment commands

Use this section for public landing page and website-file deploys only. This is separate from backend deployment and separate from Windows installer/release upload.

Critical paths:

- Public website nginx root: `/var/www/languagevoicetutor/site`
- Do not upload website files to `/var/www/languagevoicetutor/`; that parent directory is not the nginx root for the public website.
- Windows release files are separate from website files and are served from `/var/www/languagevoicetutor/releases/windows/direct` through an nginx alias for `/releases/windows/direct/`.

Public website source files in this repository live under `site/public/`. Upload public website files only to `/var/www/languagevoicetutor/site` unless nginx diagnostics prove the root changed.

### Pre-deployment diagnostics

Check the nginx website root and release alias before uploading:

```powershell
ssh lvt-server "sudo nginx -T 2>/dev/null | sed -n '/server_name languagevoicetutor.com/,/}/p' | grep -E 'root /var/www/languagevoicetutor/site|alias /var/www/languagevoicetutor/releases/windows/direct'"
```

List the current website files from the correct website root:

```powershell
ssh lvt-server "find /var/www/languagevoicetutor/site -maxdepth 3 -type f | sort"
```

Verify the Windows release alias directory separately:

```powershell
ssh lvt-server "find /var/www/languagevoicetutor/releases/windows/direct -maxdepth 1 -type f | sort"
```

Optional public alias verification before a website deploy:

```powershell
Invoke-WebRequest https://languagevoicetutor.com/releases/windows/direct/latest.json -UseBasicParsing
```

### Backup the current public website root

Create a timestamped backup from the correct website root before upload:

```powershell
ssh lvt-server "backup=/var/www/languagevoicetutor/site.backup.$(date -u +%Y%m%dT%H%M%SZ); sudo cp -a /var/www/languagevoicetutor/site \"$backup\"; echo \"$backup\""
```

Keep the printed backup path for rollback.

### Upload public website files

Upload landing page files to `/var/www/languagevoicetutor/site`, not to `/var/www/languagevoicetutor/`:

```powershell
scp .\site\public\index.html lvt-server:/tmp/index.html
scp .\site\public\download.html lvt-server:/tmp/download.html
scp .\site\public\styles.css lvt-server:/tmp/styles.css
scp .\site\public\download.js lvt-server:/tmp/download.js
ssh lvt-server "sudo mv /tmp/index.html /var/www/languagevoicetutor/site/index.html && sudo mv /tmp/download.html /var/www/languagevoicetutor/site/download.html && sudo mv /tmp/styles.css /var/www/languagevoicetutor/site/styles.css && sudo mv /tmp/download.js /var/www/languagevoicetutor/site/download.js"
```

Upload landing images and their README to the landing assets directory under the correct nginx root:

```powershell
ssh lvt-server "sudo mkdir -p /var/www/languagevoicetutor/site/assets/images/landing"
scp .\site\public\assets\images\landing\windows-desktop.webp lvt-server:/tmp/windows-desktop.webp
scp .\site\public\assets\images\landing\mobile.webp lvt-server:/tmp/mobile.webp
scp .\site\public\assets\images\landing\README.md lvt-server:/tmp/landing-README.md
ssh lvt-server "sudo mv /tmp/windows-desktop.webp /var/www/languagevoicetutor/site/assets/images/landing/windows-desktop.webp && sudo mv /tmp/mobile.webp /var/www/languagevoicetutor/site/assets/images/landing/mobile.webp && sudo mv /tmp/landing-README.md /var/www/languagevoicetutor/site/assets/images/landing/README.md"
```

Do not upload or move Windows installer files with these website commands. Windows installer release files stay in `/var/www/languagevoicetutor/releases/windows/direct`.

### Cleanup if files were accidentally uploaded to the wrong root

Only run this cleanup if diagnostics confirm accidental public website files were uploaded directly under `/var/www/languagevoicetutor/`. Do not remove `/var/www/languagevoicetutor/site` or `/var/www/languagevoicetutor/releases`.

```powershell
ssh lvt-server "sudo rm -f /var/www/languagevoicetutor/index.html /var/www/languagevoicetutor/download.html /var/www/languagevoicetutor/styles.css /var/www/languagevoicetutor/download.js && sudo rm -rf /var/www/languagevoicetutor/assets"
```

### Public verification

Verify the website and landing assets over HTTPS after upload:

```powershell
Invoke-WebRequest https://languagevoicetutor.com/ -UseBasicParsing
Invoke-WebRequest https://languagevoicetutor.com/download.html -UseBasicParsing
Invoke-WebRequest https://languagevoicetutor.com/assets/images/landing/windows-desktop.webp -UseBasicParsing
Invoke-WebRequest https://languagevoicetutor.com/assets/images/landing/mobile.webp -UseBasicParsing
Invoke-RestMethod https://languagevoicetutor.com/releases/windows/direct/latest.json
```

The first four checks must return `200 OK`. The `latest.json` check must remain valid and should still show the intended Windows release metadata. For the resolved landing page incident, the verified manifest remained `version=0.1.36-tester.16`, `installerFileName=LanguageVoiceTutorSetup-0.1.36-tester.16.exe`, `backendBaseUrl=https://api.languagevoicetutor.com`, `minimumSupportedVersion=0.1.36-tester.16`, and `updateMode=manual-confirmation`.

### Rollback public website files

Rollback uses the timestamped backup directory printed by the backup command. Replace `<backup-dir>` with that exact path:

```powershell
ssh lvt-server "sudo rsync -a --delete <backup-dir>/ /var/www/languagevoicetutor/site/"
```

Re-run the public verification commands after rollback.

### Resolved landing page deployment incident note

A landing page update was initially uploaded to `/var/www/languagevoicetutor/`, but nginx serves the public website from `/var/www/languagevoicetutor/site`. Because the files were in the wrong parent directory, the live homepage did not update and public requests for `download.html` plus landing assets returned 404. Diagnostics confirmed the real nginx root, confirmed `/releases/windows/direct/` is a separate alias to `/var/www/languagevoicetutor/releases/windows/direct/`, and confirmed that Windows release files should not be mixed with website files. The accidental files were removed from the wrong parent directory, then `index.html`, `download.html`, `styles.css`, `download.js`, the landing images, and the landing README were uploaded to `/var/www/languagevoicetutor/site`. Public verification then returned `200 OK` for the homepage, `download.html`, and both landing images, while `latest.json` remained valid.

## Windows direct release upload commands

Canonical Windows direct release flow is separate from backend deployment:

```powershell
$ReleaseVersion = "<next-tester-version>"
powershell -ExecutionPolicy Bypass -File .\scripts\package-windows-inno-release.ps1 -Version $ReleaseVersion
powershell -ExecutionPolicy Bypass -File .\scripts\validate-windows-direct-release.ps1
powershell -ExecutionPolicy Bypass -File .\scripts\upload-windows-direct-release.ps1 -Version $ReleaseVersion
```

Upload Windows direct release files only to `/var/www/languagevoicetutor/releases/windows/direct`. Do not upload them to the public website root `/var/www/languagevoicetutor/site`, and do not mix this flow with backend deployment. Generated release outputs must remain uncommitted.

## Backend-only deployment commands


```powershell
$BackendVersion = "0.1.35-backend.57"
powershell -ExecutionPolicy Bypass -File .\scripts\package-backend-linux-release.ps1 -Version $BackendVersion
```

Example upload/restart command for a reviewed backend-only deploy:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\upload-backend-linux-release.ps1 `
  -Version $BackendVersion `
  -PackageFirst
```


```powershell
```

Expected output path:

```text
```


When a manual SQL migration creates a new table, include a runtime DB role grant check after the reviewed SQL is applied. For the current production setup, the runtime app DB role is `lvt_app`. After creating a new table through `postgres`-owned reviewed SQL, inspect privileges without pasting database passwords or connection strings into docs or commands:

```sql
\dp public.<table_name>
```

Grant expected runtime privileges intentionally when required, for example:

```sql
GRANT SELECT, INSERT, UPDATE, DELETE ON TABLE public.<table_name> TO lvt_app;
```

This grant check does not replace SQL review. Review migration SQL first, then verify and grant runtime table privileges as an explicit rollout step.

## Downloaded update installer cleanup

The desktop app stores verified installers downloaded by **Check for updates** under the current user's local update cache:

```text
%LOCALAPPDATA%\LanguageVoiceTutor\Updates\LanguageVoiceTutorSetup-{version}.exe
```

Cleanup old downloaded update installers from a tester machine with:

```powershell
Remove-Item "$env:LOCALAPPDATA\LanguageVoiceTutor\Updates\LanguageVoiceTutorSetup-*.exe" -Force -ErrorAction SilentlyContinue
Remove-Item "$env:LOCALAPPDATA\LanguageVoiceTutor\Updates\LanguageVoiceTutorSetup-*.exe.download" -Force -ErrorAction SilentlyContinue
```

Release/tester installed builds are server-only and use `https://api.languagevoicetutor.com`; Local backend URLs are DEBUG/developer-only.

## Admin CMS browser verification checks

Use these checks after a backend deploy that changes `/admin` static assets. They do not replace manual browser verification, but they confirm the deployed shell references the expected cache-busted assets and readable Validation & Preview renderers.

Fetch `/admin/` with a cache-busting query:

```powershell
Invoke-WebRequest "https://api.languagevoicetutor.com/admin/?v=admin-cms-20260613-raw-json-fix" -UseBasicParsing
```

Verify the `admin.js` and `admin.css` version token is present in the Admin shell:

```powershell
(Invoke-WebRequest "https://api.languagevoicetutor.com/admin/?v=admin-cms-20260613-raw-json-fix" -UseBasicParsing).Content | Select-String "admin-cms-20260613-raw-json-fix"
```

Fetch `admin.js` with a cache-busting query:

```powershell
Invoke-WebRequest "https://api.languagevoicetutor.com/admin/admin.js?v=admin-cms-20260613-raw-json-fix" -UseBasicParsing
```

Verify `renderCmsValidationResult` exists:

```powershell
(Invoke-WebRequest "https://api.languagevoicetutor.com/admin/admin.js?v=admin-cms-20260613-raw-json-fix" -UseBasicParsing).Content | Select-String "renderCmsValidationResult"
```

Verify `renderCmsPreviewSummary` exists:

```powershell
(Invoke-WebRequest "https://api.languagevoicetutor.com/admin/admin.js?v=admin-cms-20260613-raw-json-fix" -UseBasicParsing).Content | Select-String "renderCmsPreviewSummary"
```

Verify the collapsed raw validation JSON label exists:

```powershell
(Invoke-WebRequest "https://api.languagevoicetutor.com/admin/admin.js?v=admin-cms-20260613-raw-json-fix" -UseBasicParsing).Content | Select-String "Show raw validation JSON"
```

Verify the collapsed raw preview JSON label exists:

```powershell
(Invoke-WebRequest "https://api.languagevoicetutor.com/admin/admin.js?v=admin-cms-20260613-raw-json-fix" -UseBasicParsing).Content | Select-String "Show raw preview JSON"
```

Manual browser check:

1. Login to `/admin`.
2. Open **CMS Content**.
3. Open `static-json-v1`.
4. Run Validation.
5. Load Preview summary.
6. Confirm the UI is readable.
7. Confirm raw JSON appears only inside collapsed details blocks.

Current state: backend `0.1.35-backend.24` is the latest active backend example for these Admin CMS checks. Previous backend release for rollback reference remains `/opt/languagevoicetutor/backend/releases/0.1.35-backend.23`.

Current milestone: CMS published-snapshot runtime is active for controlled tester lessons. These checks must confirm the active CMS source and clean fallback state rather than enabling broad public release.

## Admin CMS runtime status diagnostic

Server release/backend verification runtime status check. The smoke script now defaults to the server backend, not localhost:

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\smoke_cms_runtime_status.ps1
```

Optional explicit server form:

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\smoke_cms_runtime_status.ps1 -BaseUrl "https://api.languagevoicetutor.com"
```

If the endpoint requires admin auth, provide an admin bearer token or approved admin auth method without printing or hardcoding token values. Localhost is not the default for release/backend verification and should be used only when explicitly passed for approved local backend development runs.

Manual endpoint check after signing in as a bootstrap admin:

```powershell
Invoke-RestMethod "https://api.languagevoicetutor.com/api/admin/dev/cms/runtime-status" -Headers @{ Authorization = "Bearer <admin-bearer-token>" }
```


CMS runtime tutor profile validation policy check:

```powershell
python .\tools\test_cms_runtime_tutor_profile_validation_policy.py
```

The runtime status endpoint is read-only and does not change CMS runtime content. CMS published snapshot is the intended primary source when `CmsContent:UsePublishedSnapshotForRuntime=true`, `CmsContent:ReadPublishedSnapshotEnabled=true`, `CmsContent:ContentPackSlug=static-json-v1`, and `CmsContent:FallbackToStaticJson=true` are configured and the snapshot validates. Emergency rollback can disable CMS runtime flags so the effective source returns to static JSON fallback, which must be treated as an attention state.

## Controlled CMS published-snapshot runtime validation

Use `tools/validate_cms_published_snapshot_runtime.ps1` for the next runtime validation step. Its default mode is read-only and targets `https://api.languagevoicetutor.com/api/admin/dev/cms/runtime-status`; provide an admin bearer token with `-AccessToken` or set `EVT_ADMIN_BEARER_TOKEN` after using an approved admin auth method. The token must never be printed, committed, or hardcoded. Without admin auth, 401/403 is an expected safe failure.

Default read-only check:

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\validate_cms_published_snapshot_runtime.ps1
```

The read-only check must confirm `effectiveSource=StaticJson`, `validationSuccess=true`, `usePublishedSnapshotForRuntime=false`, and that learner runtime is not using the CMS snapshot. It does not change server configuration, restart services, or enable CMS runtime.

Generate the explicit operator plan:

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\validate_cms_published_snapshot_runtime.ps1 -GenerateServerValidationPlan
```

The plan mode is offline and print-only: it must complete without admin auth, must not call backend endpoints, must not change configuration, and must not restart services. It prints the temporary flags for an explicitly approved controlled server validation window only:

```text
CmsContent__UsePublishedSnapshotForRuntime=true
CmsContent__ReadPublishedSnapshotEnabled=true
CmsContent__ContentPackSlug=static-json-v1
CmsContent__FallbackToStaticJson=true
```

For the current controlled tester phase, confirm the backend release, health, database health, and that Admin CMS has a published version. Runtime status must show `effectiveSource=CmsPublishedSnapshot`, `validationSuccess=true`, `fallbackUsed=false`, and counts of 6 topics, 26 scenarios, 3 prompt templates, and 3 tutor behavior profiles. Run a short installed-app lesson smoke before handoff.

Rollback remains disabling or removing the CMS runtime flags and restarting the backend, then rerunning the read-only status check and confirming `effectiveSource=StaticJson`. CMS runtime is active for the controlled tester phase; do not expand this into broad public release without a separate decision. This process has no billing, Paddle, subscription, entitlement, installer, desktop runtime, lesson JSON, public `latest.json`, deployment-script, or EF migration involvement.

## CMS-managed level profiles (A1-B2)

- CMS now manages A1, A2, B1, and B2 level behavior profiles through the CMS Content **Levels** tab.
- Level profiles include stable level keys, display names, active flags, sort order, wrap-up turn, final-message turn, language complexity guidance, correction guidance, answer-length guidance, and admin notes.
- Lesson length defaults come from the selected level profile: A1 is configured for a shorter lesson around 15 learner turns, while B2 supports a longer dialogue.
- Scenario-specific lesson length values remain optional overrides when explicitly set and valid. Priority is: scenario override, then CMS level profile, then safe backend constants.
- Backend runtime content remains the source of truth for lesson behavior. Desktop may keep its current level labels for display, but desktop and future mobile should use backend runtime behavior from the CMS published snapshot.
- Static JSON fallback remains available; fallback runtime also receives safe default level profiles.

## Current controlled tester handoff checks after CMS runtime milestone

Use these checks after confirming the server `current` symlink points to backend `0.1.35-backend.24` and the live public direct Windows manifest points to `version=0.1.36-tester.16`, `installerFileName=LanguageVoiceTutorSetup-0.1.36-tester.16.exe`, `backendBaseUrl=https://api.languagevoicetutor.com`, `minimumSupportedVersion=0.1.36-tester.16`, and `updateMode=manual-confirmation`. For future tester handoffs, replace these values with the live `latest.json` and server symlink values instead of hardcoding a new example here.

Verify the public tester manifest before handoff:

```powershell
Invoke-RestMethod https://languagevoicetutor.com/releases/windows/direct/latest.json
```

Verify CMS runtime status through the Admin runtime-status diagnostic. Normal runtime status should show `effectiveSource=CmsPublishedSnapshot`, `validationSuccess=Yes`, `fallbackUsed=No`, no errors, and no warnings. Static JSON fallback remains available for rollback/safety, but fallback should not be active in normal status.

When checking CMS content changes, remember: **Save draft** alone does not affect the app, **Publish current draft** is required, and existing active lessons may keep old content until a new lesson starts. Start a new lesson before confirming scenario edits or A1/A2/B1/B2 behavior changes in the desktop app.

Next commands/checks are tester-handoff oriented only: verify the installed tester build from the public site, perform a short smoke test, prepare tester handoff, and collect feedback on lesson quality, level behavior, voice, UI, and CMS-controlled content. Do not touch billing/Paddle in this phase and do not start broad public release yet.

## Planned Microsoft Store / MSIX commands — not yet implemented

This section is intentionally a placeholder for future Store work. Microsoft Store distribution is planned as a separate MSIX channel, while the existing direct Windows Inno installer commands above remain the current working direct release flow.

No Store/MSIX package command, Windows App Certification Kit command, Partner Center submission command, Store package identity, Store version mapping, Store channel build flag, or Store local-data migration process is confirmed yet. Do not invent or run final Store commands from this playbook until a real local MSIX prototype confirms them and `docs/WINDOWS_STORE_RELEASE_PLAN.md` is updated.

Store builds must not use the direct `/releases/windows/direct/latest.json` installer update flow. Direct installer upload, backend deployment, database migrations, Website CMS/static site publish, and future Store/MSIX submission remain separate flows.
