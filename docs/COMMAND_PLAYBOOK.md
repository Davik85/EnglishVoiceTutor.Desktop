# Command Playbook

Review date: 2026-09-04.

## CMS setup-localization draft import

Current production baseline: backend `0.1.35-backend.154` is active, with `.153` retained as rollback; the live `current` and `previous` symlinks remain authoritative. Historical `.151` established the static-homepage/CMS-ownership architecture, which remains in force. `.152`, `.153`, and `.154` required no EF migration. CMS published version `51` remains active at runtime. The import procedure below remains for a future older draft only; it is not a pending production operation.

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

## Independent homepage deployment

The independent homepage deployment unit is `site/public/index.html` together with every required `site/public/assets/homepage/**` resource. Install those files together whenever `index.html` references them; a missing homepage asset is a failed deployment even if `/` returns HTTP 200. `mobile.html` remains the legacy `noindex,follow` redirect to `/`, and the still-valid independent `styles.css` remains preserved; neither is removed merely because the homepage now uses `assets/homepage/`.

Website CMS Publish must not overwrite or delete `index.html`, `mobile.html`, `styles.css`, or `assets/homepage/**`. CMS ownership remains `download.html`, `pricing.html`, `support.html`, legal pages, `status.html`, `robots.txt`, `sitemap.xml`, optional `llms.txt`, and `marketing-consent.js`. Do not manually upload those CMS-owned files during the independent-homepage operation, broadly copy `site/public`, or use `scripts/upload-static-site.ps1` for this rollout. Keep the accepted sharing logo `/assets/brand/lvt-logo.png` separate and unchanged, preserve existing flag assets, and keep Windows release files under `/var/www/languagevoicetutor/releases/windows/direct` separate.

Use this section for the independent homepage only. It is separate from backend deployment, Website CMS Publish, and Windows installer/release upload.

Critical paths:

- Public website nginx root: `/var/www/languagevoicetutor/site`
- Do not upload website files to `/var/www/languagevoicetutor/`; that parent directory is not the nginx root for the public website.
- Windows release files are separate from website files and are served from `/var/www/languagevoicetutor/releases/windows/direct` through an nginx alias for `/releases/windows/direct/`.

Public website source files in this repository live under `site/public/`. Upload public website files only to `/var/www/languagevoicetutor/site` unless nginx diagnostics prove the root changed.

### Pre-deployment diagnostics

Confirm the public website root and the separate Windows release alias before any homepage deployment:

```powershell
ssh lvt-server "sudo nginx -T 2>/dev/null | sed -n '/server_name languagevoicetutor.com/,/}/p' | grep -E 'root /var/www/languagevoicetutor/site|alias /var/www/languagevoicetutor/releases/windows/direct'"
ssh lvt-server "find /var/www/languagevoicetutor/site -maxdepth 3 -type f | sort"
ssh lvt-server "find /var/www/languagevoicetutor/releases/windows/direct -maxdepth 1 -type f | sort"
```

The first command is read-only confirmation that nginx serves the public site from `/var/www/languagevoicetutor/site` and maps `/releases/windows/direct/` separately to `/var/www/languagevoicetutor/releases/windows/direct`. The next commands list the current public-site and Windows-release files without changing either surface. Optional public Windows manifest verification remains available:

```powershell
Invoke-WebRequest https://languagevoicetutor.com/releases/windows/direct/latest.json -UseBasicParsing
```

### Backup the current public website root

Create a timestamped backup from the correct website root before upload:

```powershell
ssh lvt-server "backup=/var/www/languagevoicetutor/site.backup.$(date -u +%Y%m%dT%H%M%SZ); sudo cp -a /var/www/languagevoicetutor/site \"$backup\"; echo \"$backup\""
```

Keep the printed backup path for rollback.

### Install the homepage unit

Copy `index.html` and the complete `assets/homepage/**` tree to a staged location, then install both into `/var/www/languagevoicetutor/site` as one unit. Do not modify nginx during an ordinary homepage content deployment: the accepted canonical redirects already exist as production nginx configuration and are not Website CMS output or repository-managed deployment content.

### Cleanup after a confirmed wrong-root upload

Use this only when diagnostics confirm that files were accidentally uploaded directly to `/var/www/languagevoicetutor/`, rather than its `/site` child. Do not remove `/var/www/languagevoicetutor/site` or `/var/www/languagevoicetutor/releases`; do not use this as a homepage deployment method. Remove only the confirmed misplaced files, for example:

```powershell
ssh lvt-server "sudo rm -f /var/www/languagevoicetutor/index.html /var/www/languagevoicetutor/download.html /var/www/languagevoicetutor/styles.css /var/www/languagevoicetutor/download.js"
```

### Public verification

Verify the root response and every required homepage resource over HTTPS. A compact PowerShell example is:

```powershell
$homepageAssets = Get-ChildItem .\site\public\assets\homepage -Recurse -File |
  ForEach-Object { '/assets/homepage/' + $_.FullName.Substring((Resolve-Path .\site\public\assets\homepage).Path.Length + 1).Replace('\', '/') }

(Invoke-WebRequest https://languagevoicetutor.com/ -UseBasicParsing).StatusCode
$homepageAssets | ForEach-Object { (Invoke-WebRequest "https://languagevoicetutor.com$_" -UseBasicParsing).StatusCode }
```

The root and every listed homepage asset must return `200`. Compare the public `index.html` to the staged file when byte-for-byte release verification is required. Verify current canonical routing read-only; these checks are verification only, not instructions to edit nginx:

```powershell
Invoke-WebRequest https://languagevoicetutor.com/ -UseBasicParsing
Invoke-WebRequest https://languagevoicetutor.com/index.html -MaximumRedirection 0 -UseBasicParsing
Invoke-WebRequest https://languagevoicetutor.com/ai-language-tutor -MaximumRedirection 0 -UseBasicParsing
Invoke-WebRequest https://languagevoicetutor.com/ai-language-tutor/ -MaximumRedirection 0 -UseBasicParsing
Invoke-WebRequest https://www.languagevoicetutor.com/example?source=check -MaximumRedirection 0 -UseBasicParsing
```

Require `/` to return `200`; `/index.html`, `/ai-language-tutor`, and `/ai-language-tutor/` to redirect to `/`; and `www` to redirect to the equivalent non-`www` HTTPS URL while preserving path and query.

### Rollback the homepage unit

Rollback uses the timestamped backup directory printed by the backup command. Restore `index.html` and `assets/homepage/` together from the same backup; do not restore one without the other. Replace `<backup-dir>` with that exact path:

```powershell
ssh -t lvt-server "set -eu; sudo cp -a '<backup-dir>/index.html' '/var/www/languagevoicetutor/site/index.html'; sudo rm -rf '/var/www/languagevoicetutor/site/assets/homepage'; if [ -d '<backup-dir>/assets/homepage' ]; then sudo cp -a '<backup-dir>/assets/homepage' '/var/www/languagevoicetutor/site/assets/homepage'; fi; echo HOMEPAGE_ROLLBACK=PASS"
```

Re-run the public verification commands after rollback.

### Historical wrong-root incident

An earlier landing-page upload to `/var/www/languagevoicetutor/` did not update the public site because nginx serves `/var/www/languagevoicetutor/site`. Windows release files remained separately served from `/var/www/languagevoicetutor/releases/windows/direct`. This is operational history retained to prevent repeating the wrong-root upload; it does not authorize a broad static-site upload for the current independent homepage.


## Microsoft Store/MSIX prototype commands discontinued

Do not run MSIX prototype commands. The local Store/MSIX prototype was evaluated and discontinued, and the repository no longer keeps active Store/MSIX packaging projects, asset generators, or policy tests.

Use the Direct EXE/Inno installer commands in this playbook for Windows distribution. The direct `latest.json` update flow remains the active update path. Future Windows trust/signing work should focus on a code signing certificate for the direct EXE/Inno installer. Backend deploy, Website CMS/static site publish, Windows direct installer upload, and database migrations remain separate processes.

## Windows direct release upload commands

Canonical Windows direct release flow is separate from backend deployment:

```powershell
$ReleaseVersion = "<next-tester-version>"
powershell -ExecutionPolicy Bypass -File .\scripts\package-windows-inno-release.ps1 -Version $ReleaseVersion
powershell -ExecutionPolicy Bypass -File .\scripts\validate-windows-direct-release.ps1
powershell -ExecutionPolicy Bypass -File .\scripts\upload-windows-direct-release.ps1 -ServerHost lvt-server -ServerUser deploy -RemotePath /var/www/languagevoicetutor/releases/windows/direct -DryRun
powershell -ExecutionPolicy Bypass -File .\scripts\upload-windows-direct-release.ps1 -ServerHost lvt-server -ServerUser deploy -RemotePath /var/www/languagevoicetutor/releases/windows/direct
```

Upload Windows direct release files only to `/var/www/languagevoicetutor/releases/windows/direct`. Do not upload them to the public website root `/var/www/languagevoicetutor/site`, and do not mix this flow with backend deployment. Generated release outputs must remain uncommitted.

## Backend-only deployment commands

This file is the repository command-playbook source required by the backend deployment-policy check. Use `scripts/upload-backend-linux-release.ps1` for backend package/upload (normally with `-PackageFirst`); do not replace it with an invented manual deployment path. Migrations are applied separately through reviewed operator SQL. Never pass passwords in command arguments, and never commit generated files under `artifacts/`.


```powershell
$BackendVersion = "<next-backend-version>"
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


When a manual SQL migration creates a new table, include a runtime DB role grant check and owner check after the reviewed SQL is applied. For the current production setup, the runtime app DB role is `lvt_app`. If reviewed SQL is applied under the `postgres` role and creates a new application table, verify the new object's owner and required grants before considering the migration complete. Do not introduce a blanket ownership change for unrelated existing tables. In the interactive `psql` verification session, inspect the bounded object with `\dp public.<table_name>`; do not place a password in the command arguments.

For the feedback-report table, the documented expected owner is `lvt_app`. Inspect ownership and privileges without pasting database passwords or connection strings into docs or commands:

```sql
SELECT
    schemaname,
    tablename,
    tableowner
FROM pg_tables
WHERE tablename = 'user_feedback_reports';

SELECT
    grantee,
    privilege_type
FROM information_schema.role_table_grants
WHERE table_schema = 'public'
  AND table_name = 'user_feedback_reports'
ORDER BY grantee, privilege_type;
```

Grant expected runtime privileges intentionally when required, for example:

```sql
GRANT SELECT, INSERT, UPDATE, DELETE ON TABLE public.<table_name> TO lvt_app;
```

This owner/grant check does not replace SQL review. Review migration SQL first, then verify ownership and grant runtime table privileges as explicit rollout steps.

For sensitive provider-ownership tables, review automatically inherited analytics-reader privileges explicitly after creation and revoke access when the table is outside the approved analytics surface. This is a table-by-table data-classification decision, not a blanket rule for every new table.

## Mandatory billing-adapter regression gate

Before accepting Paddle, Google Play, Apple App Store, another provider adapter, or shared billing/status work, follow the detailed [mandatory billing-adapter regression gate](subscription-billing-foundation.md#mandatory-billing-adapter-regression-gate). Provider-specific tests or sandbox checks alone are insufficient.

At minimum, run focused `AdminPremiumGrantService`, `SubscriptionStatusService`, affected provider verification/persistence, Paddle cross-provider, Google Play cross-provider, and affected client parsing/display coverage. Manually verify a controlled trial plus scheduled/active manual Premium account: final expiry later than trial, correct tariff/provider/Auto-renew state, unchanged approved client layout, and correct **Premium Entitlement Schedule** versus **Active Entitlements** behavior.

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

When the Admin login form, Admin authentication JavaScript, or Admin URL/session initialization changes, validate the real script before backend packaging or deployment with `node --check backend/EnglishVoiceTutor.Api/wwwroot/admin/admin.js` and the existing backend regression test `AdminFeedbackReportsUiStaticTests.AdminScriptParsesSoLoginAndFeedbackHandlersCanInitialize`.

For changes to `wwwroot/admin/admin.js`, `wwwroot/admin/index.html`, Admin authentication/endpoints, middleware order, Admin rate limiting, or Admin feedback services, health checks alone are insufficient. The release is successful only after a private-window manual smoke: authorized login by both the Sign in button and Enter; dashboard load; feedback/support queue open; ordinary reports load; account-deletion filter works; one report opens; reply controls render; logout; and repeat login. Verify legacy URL cleanup with a clearly fake URL such as `/admin/?Email=example.invalid&PASSWORD=not-a-real-password&view=feedback#feedback-reports`: only case-insensitive credential parameters may be removed, while unrelated query state and hash are preserved. Also verify fail-closed fallback with JavaScript unavailable: browser-native submission cannot serialize credentials into either the URL or native POST body. Verify the active server symlink, backend and database health, service status, recent logs, and any other relevant product smoke. `/health` and database health cannot prove browser JavaScript parsing or login-handler registration.

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

Historical example: backend `.24` was the active release when these older asset checks were first recorded, with `.23` as its rollback reference. Always use the live `current` and `previous` symlinks now; the documented current production release is `.154` with `.153` as rollback.

Current milestone: CMS published-snapshot runtime is active for published Windows direct lessons. These checks must confirm the active CMS source and clean fallback state without changing release scope.

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

For the current Windows Direct Release 1.6, confirm the backend release, health, database health, and that Admin CMS has published version `51`. Runtime status must show `effectiveSource=CmsPublishedSnapshot`, `validationSuccess=true`, `fallbackUsed=false`, 26 scenarios, 130 localized setup-message templates, and 625 localized context titles. The prior installed-app localized setup smoke remains historical evidence; the verified 1.5 -> 1.6 manual-confirmation update does not claim broader installed-app functional smoke.

Rollback remains disabling or removing the CMS runtime flags and restarting the backend, then rerunning the read-only status check and confirming `effectiveSource=StaticJson`. CMS runtime is active for the Windows Direct Release 1.6 phase; do not expand this into broad public release without a separate decision. This process has no billing, Paddle, subscription, entitlement, installer, desktop runtime, lesson JSON, public `latest.json`, deployment-script, or EF migration involvement.

## CMS-managed level profiles (A1-B2)

- CMS now manages A1, A2, B1, and B2 level behavior profiles through the CMS Content **Levels** tab.
- Level profiles include stable level keys, display names, active flags, sort order, wrap-up turn, final-message turn, language complexity guidance, correction guidance, answer-length guidance, and admin notes.
- Lesson length defaults come from the selected level profile: A1 is configured for a shorter lesson around 15 learner turns, while B2 supports a longer dialogue.
- Scenario-specific lesson length values remain optional overrides when explicitly set and valid. Priority is: scenario override, then CMS level profile, then safe backend constants.
- Backend runtime content remains the source of truth for lesson behavior. Desktop may keep its current level labels for display, but desktop and future mobile should use backend runtime behavior from the CMS published snapshot.
- Static JSON fallback remains available; fallback runtime also receives safe default level profiles.

## Current controlled tester handoff checks after CMS runtime milestone

Use these checks after confirming the server `current` symlink points to backend `0.1.35-backend.154`, the `previous` symlink points to `.153`, and the live public direct Windows manifest points to `version=1.6`, `installerFileName=LanguageVoiceTutorSetup-1.6.exe`, `backendBaseUrl=https://api.languagevoicetutor.com`, `minimumSupportedVersion=1.6`, and `updateMode=manual-confirmation`. The live manifest is verified; no independent second public-download SHA verification is claimed for 1.6. For future handoffs, replace these values with the live `latest.json` and server symlink values instead of hardcoding a new example here.

Verify the public direct release manifest before handoff:

```powershell
Invoke-RestMethod https://languagevoicetutor.com/releases/windows/direct/latest.json
```

Verify CMS runtime status through the Admin runtime-status diagnostic. Normal runtime status should show `effectiveSource=CmsPublishedSnapshot`, `validationSuccess=Yes`, `fallbackUsed=No`, no errors, and no warnings. Static JSON fallback remains available for rollback/safety, but fallback should not be active in normal status.

When checking CMS content changes, remember: **Save draft** alone does not affect the app, **Publish current draft** is required, and existing active lessons may keep old content until a new lesson starts. Start a new lesson before confirming scenario edits or A1/A2/B1/B2 behavior changes in the desktop app.

Next commands/checks are tester-handoff oriented only: verify the installed tester build from the public site, perform a short smoke test, prepare tester handoff, and collect feedback on lesson quality, level behavior, voice, UI, and CMS-controlled content. Do not touch billing/Paddle in this phase and do not start broad public release yet.

## Microsoft Store/MSIX commands

No Microsoft Store/MSIX commands are active or planned. Do not add Partner Center submission commands, WACK commands, MSIX package commands, or Store-channel build flags unless the product decision changes in a separate future effort.

The active Windows release flow is Direct EXE/Inno plus the direct `latest.json` update manifest.

## AI Models CMS post-deploy verification

After a backend deploy, open **Admin CMS → System → AI Models → Load AI Models** as Super Admin. Confirm Lesson Tutor Chat, Feedback / correction, Lesson Hint, and Translation each remain `gpt-5.6-luna`; confirm all four **Omit temperature parameter** flags remain enabled; run **Validate format**; run **Test provider access** only if model settings changed; and do not publish unless the changes are intentional. AI Models CMS JSON is persistent server data/config at `/opt/languagevoicetutor/backend/site/content/ai-model-settings.json` resolved outside versioned backend release folders, not a packaged release artifact. Historical persistence verification confirmed the file survived backend restart and matched its then-current release copy by SHA-256 `94f84fc07551d821bfa9dc0682bb4ee60108d11d74987b84ebb39fce96f825f1`; future deploys must not use `/opt/languagevoicetutor/backend/current/site/content/ai-model-settings.json` or `/opt/languagevoicetutor/backend/releases/<version>/site/content/ai-model-settings.json` as the source of truth.

## Do not mix release operations

- Backend deploy commands package/upload the backend only; they do not upload Windows installers, publish Website CMS/static site content, run EF migrations, enable Paddle live, change `latest.json`, or replace persistent AI Models server data/config from release-folder JSON.
- Windows direct upload commands publish only the Direct EXE/Inno release files for `latest.json`; they do not deploy backend code, run migrations, publish Website CMS, or create Store/MSIX packages.
- Website CMS publish is a separate website/content operation; it is not backend deploy and not Windows installer upload.
- DB migrations are separate reviewed operator work; do not imply backend upload scripts apply migrations automatically.
- Paddle live account/provider changes are manual/provider configuration unless an approved backend configuration/code change is explicitly required.
- AI Models persistence correction is server data/config work; it is separate from backend deploy, Website CMS publish, Windows direct installer upload, DB migrations, and provider/Paddle live changes.

## Paddle live checkout verification commands

- Static checkout readiness: `pytest -q tests/test_paddle_live_checkout_readiness.py`.
- Backend compile/test in an environment with the .NET SDK installed: `dotnet test backend/EnglishVoiceTutor.Api.Tests/EnglishVoiceTutor.Api.Tests.csproj --no-restore` or the repository's current backend test command.
- After backend deployment and restart: `curl -fsS https://api.languagevoicetutor.com/health` and `curl -fsS https://api.languagevoicetutor.com/api/health/database`.
- Do not run live checkout or live webhook smoke tests until the owner explicitly approves the controlled live test.

## 2026-06-30 Paddle live checkout/Admin readiness update

Current production facts after backend `0.1.35-backend.108` and the 2026-07-02 controlled live payment/cancel-renewal validation:

- Backend health and database health are `200 Healthy`.
- Backend server-side Paddle configuration is in the existing env file `/etc/languagevoicetutor/backend.env`; do not invent a second env file and do not create Paddle live systemd drop-ins for this configuration.
- Backend current symlink is `/opt/languagevoicetutor/backend/current`; backend releases are under `/opt/languagevoicetutor/backend/releases/<version>`.
- At that historical checkpoint, AI Models persistent server data remained `/opt/languagevoicetutor/backend/site/content/ai-model-settings.json`; the then-known-good models were `gpt-5.5` for lesson tutor chat and `gpt-5.2` for feedback correction, lesson hints, and translation.
- Static website nginx root is `/var/www/languagevoicetutor/site`. The parent `/var/www/languagevoicetutor` is not the nginx static-site root and must not be used as the static website upload target.
- Public Paddle config is `/var/www/languagevoicetutor/site/paddle.public.json`; public Paddle checkout page is `/var/www/languagevoicetutor/site/pay.html`.
- Direct Windows release files are separate at `/var/www/languagevoicetutor/releases/windows/direct` and are not touched by static website upload.
- Active Windows delivery remains Direct EXE/Inno. Store/MSIX is discontinued and must not be reintroduced. Current direct public release is `1.2`; direct `latest.json` remains active with manual-confirmation update mode.
- Paddle website review is approved, `/pay.html` and `/paddle.public.json` are deployed/reachable, backend live Paddle env is configured, and a real transaction URL opened Paddle checkout with `Language Voice Tutor Pro`, `Pro Monthly`, `14.99 EUR`.
- 2026-07-02 controlled validation completed: real live payment Complete for Language Voice Tutor Pro at 14.99 EUR via Google Pay; live checkout transaction creation, `subscription.created`, `subscription.activated`, `transaction.completed`, payment persistence, subscription snapshot processing, reconciliation, entitlement activation (`ActivatedCount=1`, `BlockedCount=0`, `FailedCount=0`), and desktop Premium visibility were verified without exposing raw provider payloads or secrets. Earlier failed payment attempts were processed without Premium activation (`ActivatedCount=0` / `AlreadySkippedCount=1`). One PostgreSQL serialization conflict during subscription snapshot processing retried successfully and ended with `FailedCount=0`. Desktop cancel-renewal was verified: auto-renewal became inactive while Premium remained active until `8/2/2026`. Full-refund Premium revocation is production-verified; chargeback remains implemented/test-covered but not live-chargeback-tested; expanded customer portal/subscription management is deferred and not a current blocker.
- Controlled live payment, webhook delivery, payment persistence, subscription snapshot processing, entitlement activation, desktop Premium visibility, and desktop cancel-renewal behavior were completed and documented on 2026-07-02. Paddle full-refund Premium revocation is production-verified on backend `0.1.35-backend.108` using the already stored live `adjustment.updated` event; automatic future handling should use delivered `adjustment.created` / `adjustment.updated` notifications, with the operator reprocess command reserved for already-stored/legacy events only. Chargeback remains implemented/test-covered but not live-chargeback-tested; expanded customer portal/subscription management is deferred and not a current blocker; broad public paid launch remains pending final release-readiness review and remaining release blockers.

Historical broad static-site upload command (not for the current independent homepage, which must deploy `index.html` with `assets/homepage/**`) targeted the real nginx root:

```powershell
pwsh -NoProfile -ExecutionPolicy Bypass -File .\scripts\upload-static-site.ps1 `
  -ServerHost "lvt-server" `
  -ServerUser "deploy" `
  -RemotePath "/var/www/languagevoicetutor/site"
```

`site/public/.well-known/assetlinks.json` is infrastructure-owned static content and the repository source for the Android Digital Asset Links association. Website CMS Publish owns only its explicit generated files and must preserve this file unchanged. The full `scripts/upload-static-site.ps1` command uploads the broader tracked static-site surface, so it must not be used merely to publish `assetlinks.json` when the live CMS-generated pages may be newer than the repository copies. The assetlinks-only publication is complete and live verification has confirmed the association; any future update must use the established scoped per-file static-infrastructure deployment pattern to `/var/www/languagevoicetutor/site/.well-known/assetlinks.json`. Restore Credentials is enabled in production.

Safe backend env verification must redact secrets and must use the existing env file, for example:

```bash
sudo awk -F= '/^(Billing__|PaddleBilling__|PaddleWebhook__)/ { v=$2; if ($1 ~ /(ApiKey|SecretKey|Token)/) v=(length($2)>0 ? "SET" : "EMPTY"); print $1 "=" v }' /etc/languagevoicetutor/backend.env
```

Admin capabilities should distinguish completed controlled validation from launch completion: live checkout/webhooks, the 2026-07-02 live payment/Premium activation/cancel-renewal path, failed-payment non-activation, and full-refund Premium revocation can be reported as completed; chargeback remains implemented/test-covered but not live-chargeback-tested, expanded customer portal/subscription management is deferred, and `billingPaidLaunchReleaseComplete=false` remains until final release-readiness review and remaining blockers are closed.

Admin RBAC note: Production Admin RBAC / persistent role management, Admin Activity first production slice, and `super_admin` emergency Premium Revoke are completed. Admin Activity shows existing `admin_actions` and `admin_role_assignment_events`, including Manual Premium Grant/Revoke and stored Admin notes/reasons where present; `safeMetadataJson` remains separate from Admin note. Manual Premium Revoke is an emergency access-control action and does not mutate Paddle provider/payment history or fake webhook events. `productionRolesAvailable` means persistent Admin role authorization is active with an explicit fallback cutover (`AdminAuthorization__EnableBootstrapAdminFallbackForAdminPermissionPolicies=false`). It is not a broad public-launch flag and does not override remaining paid-launch blockers. Production diagnostics show two active `super_admin` AdminUsers and fallback disabled; if this flag is false, check the explicit fallback configuration and cutover status before changing role assignments.

## 2026-07-01 production CMS capability/runtime verification

Backend `0.1.35-backend.108` fixed the stale `cmsUiAvailable` capability state. In production, **System → Capabilities Check** shows `cmsUiAvailable` as AVAILABLE, the Admin Shell **CMS Content** tab opens, and the CMS Content workspace loads. This verification did not save, publish, restore, initialize, import, or otherwise mutate CMS content.

The learner runtime is production-verified as `CmsPublishedSnapshot`, with the published snapshot active and valid. The current runtime snapshot reports content pack slug `static-json-v1`, published version number `46`, 6 topics, 26 scenarios, 4 prompt templates, 3 tutor behavior profiles, validation success `Yes`, and currently using static JSON fallback `No`. Static JSON remains an emergency fallback only and is not active in the verified production runtime state.

## Operator-only Paddle adjustment reprocess command

Use this only as a one-off recovery path for an already-stored Paddle `adjustment.created` or `adjustment.updated` provider event that was previously normalized but skipped/blocked before entitlement revocation. This command is not part of normal deployment, must not run automatically, and must not be used to create a new payment, create a new refund, delete provider history, or fabricate a webhook.

After deploying a backend build that contains the recovery path, run it from the backend release directory with the specific existing Paddle provider event id:

```bash
cd /opt/languagevoicetutor/backend/current
sudo -u languagevoicetutor dotnet EnglishVoiceTutor.Api.dll --reprocess-paddle-adjustment --provider-event-id evt_01kwhgmvh1v9k8ve70gvnfeskm
```

Expected safe behavior after backend `.99`: the command refuses to run without `--provider-event-id`; refuses event types other than `adjustment.created` / `adjustment.updated`; reuses the existing billing event row; does not depend on old blocked/skipped reconciliation state; directly invokes the same safe full-refund/chargeback Premium revocation logic after safety checks pass; skips partial refunds conservatively; preserves `PaymentEntity` and `SubscriptionEntity` history; does not create a fake Paddle webhook event; and reports `Revoked`, `AlreadyRevoked`, `NotFound`, `RefusedEventType`, or `Blocked` with safe metadata only. Backend `.98` ran this command for `evt_01kwhgmvh1v9k8ve70gvnfeskm` but returned `Result=Blocked` / `BlockReason=reconciliation_blocked`; root cause was that the explicit reprocess path still routed through the old reconciliation gate for an event already blocked by earlier code. No more live payment, refund, or Paddle replay is required; after `.99` deploy, run the same provider event id and require `Revoked` or `AlreadyRevoked` plus Desktop/Admin inactive Premium before broad public paid launch.

Post-run verification should use bounded checks only: confirm logs show safe result metadata for the provider event id, confirm Admin/Desktop no longer show active paid provider-event Premium for the refunded user, and do not paste raw Paddle payloads, signatures, cookies, tokens, secrets, API keys, full request bodies, full user data, or card/payment data into tickets or chats.
