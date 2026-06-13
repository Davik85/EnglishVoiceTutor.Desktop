# Command Playbook

Review date: 2026-06-13.

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

## Backend-only deployment commands

Example package command for the current backend snapshot:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\package-backend-linux-release.ps1 -Version 0.1.35-backend.7
```

Example upload/restart command for a reviewed backend-only deploy:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\upload-backend-linux-release.ps1 `
  -Version 0.1.35-backend.7 `
  -PackageFirst
```

Backend deploys are separate from EF migrations and Windows release upload. The backend upload flow does not run `dotnet ef database update`, does not apply SQL, does not upload Windows installer files, and does not change the public Windows `latest.json`. For `0.1.35-backend.7`, no EF migration was needed and no Windows installer upload was performed. Backend deploys do not upload Windows installer files and do not change `latest.json`.

Previous backend release for rollback reference: `/opt/languagevoicetutor/backend/releases/0.1.35-backend.5`.

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

Current state: backend `0.1.35-backend.8` is the latest active backend example for these Admin CMS checks. Previous backend release for rollback reference remains `/opt/languagevoicetutor/backend/releases/0.1.35-backend.7`.

Do not enable by default: these checks do not enable CMS published-snapshot runtime for learners. Learners still use packaged static JSON by default unless a separate controlled runtime-read validation and enablement decision is made.

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

The runtime status endpoint is read-only and does not enable CMS runtime content. Static JSON remains default unless the controlled runtime validation environment explicitly sets `CmsContent:UsePublishedSnapshotForRuntime=true`, `CmsContent:ReadPublishedSnapshotEnabled=true`, `CmsContent:ContentPackSlug=static-json-v1`, and `CmsContent:FallbackToStaticJson=true`. Rollback is to remove/disable those explicit CMS runtime flags so the effective source returns to static JSON.

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

During the controlled window, confirm the backend release, health, database health, and that Admin CMS has a published version before applying temporary flags. After restart, runtime status must show `effectiveSource=CmsPublishedSnapshot`, `validationSuccess=true`, and counts of 6 topics, 26 scenarios, 3 prompt templates, and 3 tutor behavior profiles. Run a short installed-app lesson smoke only if explicitly approved.

Rollback is to disable or remove the temporary CMS runtime flags and restart the backend, then rerun the read-only status check and confirm `effectiveSource=StaticJson`. CMS runtime must not become the learner default until after this controlled validation passes and a separate enablement decision is approved. This process has no billing, Paddle, subscription, entitlement, installer, desktop runtime, lesson JSON, public `latest.json`, deployment-script, or EF migration involvement.

## CMS-managed level profiles (A1-B2)

- CMS now manages A1, A2, B1, and B2 level behavior profiles through the CMS Content **Levels** tab.
- Level profiles include stable level keys, display names, active flags, sort order, wrap-up turn, final-message turn, language complexity guidance, correction guidance, answer-length guidance, and admin notes.
- Lesson length defaults come from the selected level profile: A1 is configured for a shorter lesson around 15 learner turns, while B2 supports a longer dialogue.
- Scenario-specific lesson length values remain optional overrides when explicitly set and valid. Priority is: scenario override, then CMS level profile, then safe backend constants.
- Backend runtime content remains the source of truth for lesson behavior. Desktop may keep its current level labels for display, but desktop and future mobile should use backend runtime behavior from the CMS published snapshot.
- Static JSON fallback remains available; fallback runtime also receives safe default level profiles.
