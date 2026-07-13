# Backend server deployment

Review date: 2026-07-13.

## Current production backend

Production backend is deployed and healthy.

- Current release: `0.1.35-backend.115`
- Production URL: `https://api.languagevoicetutor.com`
- Health: `https://api.languagevoicetutor.com/health`
- Database health: `https://api.languagevoicetutor.com/api/health/database`

## Current backend pre-check

Run these checks before announcing a backend state, packaging a replacement, deploying, or rolling back:

```powershell
ssh lvt-server "readlink -f /opt/languagevoicetutor/backend/current"
Invoke-WebRequest https://api.languagevoicetutor.com/health -UseBasicParsing
Invoke-WebRequest https://api.languagevoicetutor.com/api/health/database -UseBasicParsing
```

Expected baseline for the current deployment is release `0.1.35-backend.115` with previous release `0.1.35-backend.114`. The live server symlink is the source of truth; generated local files under `artifacts/` are not proof that a backend version is live.

Previous backend rollback reference must be verified from `/opt/languagevoicetutor/backend/previous`. `0.1.35-backend.49` remains a documented older rollback reference, not a substitute for checking the live `previous` symlink.

## Package backend release

The production server does not need a git checkout, a `dotnet` SDK, or a `dotnet` runtime. Backend packaging uses the repository PowerShell helper and creates the linux-x64 backend archive under `artifacts/packages/backend/`:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\package-backend-linux-release.ps1 -Version 0.1.35-backend.115
```

The package command does not upload, restart, run EF migrations, publish website files, upload Windows installers, or enable Paddle live.

## Dry-run upload

Use `-PackageFirst -DryRun` to print the upload, generated deploy-helper, symlink, and restart/status commands without changing the server; the script does not run the sudo restart or sudo status commands in dry-run mode, and restart/status commands are printed but not executed:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\upload-backend-linux-release.ps1 -Version 0.1.35-backend.115 -PackageFirst -DryRun
```

The upload helper creates a temporary `deploy-backend-release.sh` helper and uses that helper for release extraction and symlink switching. It uses `ssh -tt` for sudo restart/status when restart is enabled. Do not document old fragile inline bash deployment paths as the current backend deployment flow.

## Real upload

After the pre-check and dry run are reviewed, run the backend upload helper without `-DryRun`:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\upload-backend-linux-release.ps1 -Version 0.1.35-backend.115 -PackageFirst
```

By default, the helper uploads to `/opt/languagevoicetutor/backend`, switches `current`, updates `previous` when an older current release exists, restarts `languagevoicetutor-backend.service`, and prints service status. Use script parameters only for an intentionally reviewed non-default host, SSH port, user, or remote path.

## Post-deploy checks

Verify the live server state and public health endpoints after upload:

```powershell
ssh lvt-server "readlink -f /opt/languagevoicetutor/backend/current"
Invoke-WebRequest https://api.languagevoicetutor.com/health -UseBasicParsing
Invoke-WebRequest https://api.languagevoicetutor.com/api/health/database -UseBasicParsing
ssh -t lvt-server "sudo systemctl status languagevoicetutor-backend.service --no-pager"
```

If the service status or health checks need investigation, inspect recent backend logs without printing secrets:

```powershell
ssh -t lvt-server "sudo journalctl -u languagevoicetutor-backend.service -n 100 --no-pager"
```

Do not paste production environment values, database connection strings, API keys, or provider secrets into documentation, tickets, chat, commits, or pull requests. Do not echo `ConnectionStrings__DefaultConnection`, `PGPASSWORD`, or database URLs.


## 2026-07-11 backend `0.1.35-backend.112` verification

Backend `0.1.35-backend.112` is deployed and verified in production for the authenticated lesson-summary provider-output extraction fix. The live `current` symlink resolves from `/opt/languagevoicetutor/backend/current` to `/opt/languagevoicetutor/backend/releases/0.1.35-backend.112`; the rollback release is `/opt/languagevoicetutor/backend/releases/0.1.35-backend.111`; `languagevoicetutor-backend.service` is active; `https://api.languagevoicetutor.com/health` returned `200 Healthy`; and `https://api.languagevoicetutor.com/api/health/database` returned `200 Healthy` with `canConnect=true`. The release was packaged and uploaded with the normal Linux backend release scripts. No EF migration was added or executed, no database schema changed, and Windows installer/release files were unchanged.

Root cause corrected: backend `0.1.35-backend.111` generated summaries during authenticated Finish but read only top-level Responses API `output_text`; real provider responses could contain structured JSON text under `output[].content[].text`. Missing top-level text led to blank deserialization input and a safely isolated `JsonException`, leaving the persisted summary unavailable while preserving successful lesson completion. Backend `.112` prefers nonblank top-level `output_text`, falls back to nonblank nested `output[].content[].text`, rejects blank provider output before JSON deserialization, and still isolates summary-generation failures from lesson completion.

Production client verification on 2026-07-11 used a real authenticated Flutter mobile lesson: session start and message persistence succeeded, `PUT /api/me/lesson-sessions/{sessionId}/finish` completed the lesson, and `GET /api/me/lesson-sessions/{sessionId}/summary` returned a ready backend-owned learner-safe summary that mobile displayed. Authenticated desktop Finish continues to use the shared completion path, with unchanged desktop UI and Finish response contracts.

## 2026-07-10 backend `0.1.35-backend.111` verification

Backend `0.1.35-backend.111` is deployed and verified in production for backend-owned authenticated lesson completion and summaries. The live `current` path is `/opt/languagevoicetutor/backend/releases/0.1.35-backend.111`; the previous rollback release is `/opt/languagevoicetutor/backend/releases/0.1.35-backend.110`; `languagevoicetutor-backend.service` is active and listening on `127.0.0.1:5001`; `https://api.languagevoicetutor.com/health` returned `200 Healthy`; and `https://api.languagevoicetutor.com/api/health/database` returned `200 Healthy` with `canConnect=true`. No EF migration or database schema change was required. The existing Windows desktop app was manually verified after deployment, including lesson start, chat, finish, and summary/history behavior, and no new Windows desktop release was required for this backend change.

## 2026-07-08 backend `0.1.35-backend.110` verification

Backend `0.1.35-backend.110` is deployed and verified in production for the mobile lesson-session reply placeholder release. The live `current` symlink resolves to `/opt/languagevoicetutor/backend/releases/0.1.35-backend.110`; `https://api.languagevoicetutor.com/health` returned `200 OK` / `Healthy` / `Production`; `https://api.languagevoicetutor.com/api/health/database` returned `200 OK` / `Healthy` / `canConnect true`; and `languagevoicetutor-backend.service` was active/running. No EF migrations were run for this release.

Pre-deploy checks that passed before deployment:

```powershell
dotnet test backend\EnglishVoiceTutor.Api.Tests\EnglishVoiceTutor.Api.Tests.csproj
python .\tools\test_backend_linux_deployment_policy.py
python .\tools\test_backend_refresh_token_migration_policy.py
python .\tools\test_backend_refresh_token_policy.py
python .\tools\test_desktop_release_backend_lock_policy.py
```

Results: `dotnet test` passed `89/89`; all listed Python policy checks passed. This deployment did not change database schema, deployment scripts, desktop code, mobile repo code, billing, OpenAI/provider configuration, Website CMS/static site content, voice, TTS, realtime, analytics, history, store setup, or installer release artifacts.

## Persistent AI Models CMS settings

AI Models CMS active/draft runtime settings are persistent server data/config, not release artifacts. The configured `AiModelSettings:StorageJsonPath` defaults to `site/content/ai-model-settings.json` and is resolved outside the versioned release content root, so production stores it under the persistent backend data tree (`/opt/languagevoicetutor/backend/site/content/ai-model-settings.json`) rather than `/opt/languagevoicetutor/backend/current/site/content/` or `/opt/languagevoicetutor/backend/releases/<version>/site/content/`. Backend startup/deploy must not overwrite an existing active settings file with packaged defaults, and future backend deploys must not rely on release-folder AI Models JSON as the source of truth. If the persistent file is missing but a legacy release-content file exists, the backend imports that file once; otherwise defaults seed the in-memory draft/active values until an admin saves or publishes.

Current production verification: the persistent file exists at `/opt/languagevoicetutor/backend/site/content/ai-model-settings.json`, was seeded from `/opt/languagevoicetutor/backend/current/site/content/ai-model-settings.json` only as a one-time data/config correction, has mode `644`, contains lesson tutor chat `gpt-5.5` plus feedback/correction, lesson hint, and translation `gpt-5.2`, and matched the current release file by SHA-256 `94f84fc07551d821bfa9dc0682bb4ee60108d11d74987b84ebb39fce96f825f1`. After restarting `languagevoicetutor-backend.service`, `/health` and `/api/health/database` returned `200 Healthy`, the persistent file still existed, and `gpt-5.5` plus `gpt-5.2` remained present. This correction was not a backend deploy, not a database migration, not a Website CMS publish, and not a Windows installer upload.

After backend deploy, Super Admin should verify **Admin CMS → System → AI Models → Load AI Models**: lesson tutor chat remains `gpt-5.5`; feedback/correction, lesson hint, and translation remain `gpt-5.2`; then run **Validate format**. Test provider access only if settings changed, and do not publish unless changes are intentional. API keys remain environment secrets and are never stored in AI Models CMS JSON.

## Rollback

Rollback is controlled by the server `previous` symlink. Verify it first and do not assume an older documented version is still the rollback target:

```powershell
ssh lvt-server "readlink -f /opt/languagevoicetutor/backend/previous"
```

If `previous` resolves to the reviewed rollback release directory and the operator intentionally approves rollback, switch `current` back to that target and restart the backend service:

```powershell
ssh -t lvt-server "set -e; previous=\$(readlink -f /opt/languagevoicetutor/backend/previous); test -n \"\$previous\"; test -d \"\$previous\"; current=\$(readlink -f /opt/languagevoicetutor/backend/current); sudo ln -sfn \"\$current\" /opt/languagevoicetutor/backend/rollback-from; sudo ln -sfn \"\$previous\" /opt/languagevoicetutor/backend/current"
ssh -t lvt-server "sudo systemctl restart languagevoicetutor-backend.service"
ssh lvt-server "readlink -f /opt/languagevoicetutor/backend/current"
Invoke-WebRequest https://api.languagevoicetutor.com/health -UseBasicParsing
Invoke-WebRequest https://api.languagevoicetutor.com/api/health/database -UseBasicParsing
ssh -t lvt-server "sudo systemctl status languagevoicetutor-backend.service --no-pager"
```

If rollback health checks fail or the service does not stabilize, capture the last 100 backend log lines and follow the accepted incident procedure for the server:

```powershell
ssh -t lvt-server "sudo journalctl -u languagevoicetutor-backend.service -n 100 --no-pager"
```

## Migration reference

If a reviewed database change is required, generate and review SQL separately. The current refresh-token migration reference is `20260611000000_AddUserRefreshTokens`; apply reviewed SQL with `psql` only through the approved database procedure, never from the backend upload helper.

## Scope boundaries

Backend deploy is backend-only. It does not:

- run EF migrations;
- apply reviewed SQL;
- publish static website HTML/CSS/JS;
- publish Website CMS content;
- seed or replace persistent AI Models server data/config from release folders as the source of truth;
- upload Windows installer files;
- change production Paddle environment values;
- enable live Paddle;
- change Desktop app behavior.

Backend upload/package scripts do not apply EF migrations automatically. Database migrations remain a separate reviewed SQL/operator process only when schema changes exist and after backups/review/verification are complete.

Generated local files under `artifacts/` are not proof that a version is live on the public site. A locally built installer becomes public only after the Windows direct release files are uploaded to the website release folder and `latest.json` is verified over HTTPS.

## Release-readiness status

- Backend: production healthy, current release `0.1.35-backend.115`, previous release `0.1.35-backend.114`.
- Website: generated public pages and Paddle-review polish are completed separately from backend deployment.
- Download: current Windows tester release is visible without JavaScript and manifest-driven with JavaScript.
- Windows installer: current public direct release is `1.1`, installer `LanguageVoiceTutorSetup-1.1.exe`.
- AI Models: persistent production storage is verified and survived restart with known-good `gpt-5.5` / `gpt-5.2` values.
- Billing: controlled Paddle live payment/webhook/Premium activation and desktop cancel-renewal validation are completed for the 2026-07-02 owner-led test; full-refund Premium revocation is production-verified; chargeback remains implemented/test-covered but not live-chargeback-tested; expanded customer portal/subscription management is deferred; broad public paid launch remains pending final release-readiness review.
- Legal: website legal/support/seller/AI/status pages are ready for owner/legal final review as drafts, not final legal advice.

Do not state that the product is fully public production-ready. The direct Windows release remains a controlled direct Windows release, not a broad public production launch, and not broad public production readiness.

## Operational context retained

Phase 3 rate limiting / abuse protection is completed and production-verified with `RateLimiting__Enabled=true`. Admin persistent role authorization remains enabled with `AdminAuthorization__EnableBootstrapAdminFallbackForAdminPermissionPolicies=false`. Admin Product Statistics uses `Tracked signed-in app/device records` for backend `DeviceEntity` records and this is not raw installer downloads. `Successful payments current month` is an internal billing-event metric and is not Premium access.

Phase 4 is complete for the current release-readiness level: Phase 4A backup/readability/separate-drill-restore completed, Phase 4B local PostgreSQL backup scheduling active, Phase 4C migration rollback/remediation dry-run rehearsal completed, and Phase 4D permission-fidelity restore drill completed. Off-server encrypted backups remain optional future infrastructure hardening.

Controlled production Paddle live payment/Premium activation and desktop cancel-renewal validation are completed for the 2026-07-02 owner-led test, and failed payment attempts did not grant Premium. Paddle full-refund Premium revocation is production-verified; chargeback remains implemented/test-covered but not live-chargeback-tested. Expanded customer portal/subscription management is deferred and is not a current blocker, broad public paid launch remains pending, and Direct installer code signing remains pending. CMS published-snapshot runtime is active for controlled tester lessons. Backend deployment, database migrations, the download website, and update UI remain separate work.

## 2026-06-30 Paddle live checkout/Admin readiness update

Current production facts after backend `0.1.35-backend.108` and the 2026-07-02 controlled live payment/cancel-renewal validation:

- Backend health and database health are `200 Healthy`.
- Backend server-side Paddle configuration is in the existing env file `/etc/languagevoicetutor/backend.env`; do not invent a second env file and do not create Paddle live systemd drop-ins for this configuration.
- Backend current symlink is `/opt/languagevoicetutor/backend/current`; backend releases are under `/opt/languagevoicetutor/backend/releases/<version>`.
- AI Models persistent server data remains `/opt/languagevoicetutor/backend/site/content/ai-model-settings.json`; known-good models remain `gpt-5.5` for lesson tutor chat and `gpt-5.2` for feedback correction, lesson hints, and translation.
- Static website nginx root is `/var/www/languagevoicetutor/site`. The parent `/var/www/languagevoicetutor` is not the nginx static-site root and must not be used as the static website upload target.
- Public Paddle config is `/var/www/languagevoicetutor/site/paddle.public.json`; public Paddle checkout page is `/var/www/languagevoicetutor/site/pay.html`.
- Direct Windows release files are separate at `/var/www/languagevoicetutor/releases/windows/direct` and are not touched by static website upload.
- Active Windows delivery remains Direct EXE/Inno. Store/MSIX is discontinued and must not be reintroduced. Current direct public release is `1.1`; direct `latest.json` remains active with manual-confirmation update mode.
- Paddle website review is approved, `/pay.html` and `/paddle.public.json` are deployed/reachable, backend live Paddle env is configured, and a real transaction URL opened Paddle checkout with `Language Voice Tutor Pro`, `Pro Monthly`, `14.99 EUR`.
- 2026-07-02 controlled validation completed: real live payment Complete for Language Voice Tutor Pro at 14.99 EUR via Google Pay; live checkout transaction creation, `subscription.created`, `subscription.activated`, `transaction.completed`, payment persistence, subscription snapshot processing, reconciliation, entitlement activation (`ActivatedCount=1`, `BlockedCount=0`, `FailedCount=0`), and desktop Premium visibility were verified without exposing raw provider payloads or secrets. Earlier failed payment attempts were processed without Premium activation (`ActivatedCount=0` / `AlreadySkippedCount=1`). One PostgreSQL serialization conflict during subscription snapshot processing retried successfully and ended with `FailedCount=0`. Desktop cancel-renewal was verified: auto-renewal became inactive while Premium remained active until `8/2/2026`. Full-refund Premium revocation is production-verified; chargeback remains implemented/test-covered but not live-chargeback-tested; expanded customer portal/subscription management is deferred and not a current blocker.
- Controlled live payment, webhook delivery, payment persistence, subscription snapshot processing, entitlement activation, desktop Premium visibility, and desktop cancel-renewal behavior were completed and documented on 2026-07-02. Paddle full-refund Premium revocation is production-verified on backend `0.1.35-backend.108` using the already stored live `adjustment.updated` event; automatic future handling should use delivered `adjustment.created` / `adjustment.updated` notifications, with the operator reprocess command reserved for already-stored/legacy events only. Chargeback remains implemented/test-covered but not live-chargeback-tested; expanded customer portal/subscription management is deferred and not a current blocker; broad public paid launch remains pending final release-readiness review and remaining release blockers.

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

Admin capabilities should distinguish completed controlled validation from launch completion: live checkout/webhooks, the 2026-07-02 live payment/Premium activation/cancel-renewal path, failed-payment non-activation, and full-refund Premium revocation can be reported as completed; chargeback remains implemented/test-covered but not live-chargeback-tested, expanded customer portal/subscription management is deferred, and `billingPaidLaunchReleaseComplete=false` remains until final release-readiness review and remaining blockers are closed.

Admin RBAC note: Production Admin RBAC / persistent role management is completed. `productionRolesAvailable` means persistent Admin role authorization is active with an explicit fallback cutover (`AdminAuthorization__EnableBootstrapAdminFallbackForAdminPermissionPolicies=false`). It is not a broad public-launch flag and does not override remaining paid-launch blockers. Production diagnostics show two active `super_admin` AdminUsers and fallback disabled; if this flag is false, check the explicit fallback configuration and cutover status before changing role assignments.


## 2026-07-13 backend voice scenario semantic resolution release

Backend `0.1.35-backend.113` was deployed successfully from source commit `c850f4b` (`feat: add voice scenario semantic resolution`), with rollback release `0.1.35-backend.112`. That historical note describes the original additive endpoint deployment only. The latest voice scenario structured-output validation fix is in backend `0.1.35-backend.115`, not `.113` or `.114`.

Backend `0.1.35-backend.115` is deployed and verified in production with previous release `0.1.35-backend.114`. The live `current` symlink resolves to `/opt/languagevoicetutor/backend/releases/0.1.35-backend.115`, and the live `previous` symlink resolves to `/opt/languagevoicetutor/backend/releases/0.1.35-backend.114`; these symlinks remain the source of truth. Backend `0.1.35-backend.114` was already active before the latest deployment and must not be described as containing the `.115` fix. `languagevoicetutor-backend.service` is active and running, public `/health` returned HTTP 200, and public `/api/health/database` returned HTTP 200 with `canConnect=true`. No EF migration or database schema change was required or run, and no website or Windows installer deployment was performed.

Backend `.115` fixes `POST /api/me/lesson-sessions/{sessionId}/voice-scenario-resolution` returning HTTP 502 when the provider returned a structured-output shape permitted by the old provider schema but rejected by backend validation. The provider schema now has explicit result shapes for `published_context`, `free_context`, `clarify`, and `unsafe`, and the nested provider result is converted back into the existing flat public endpoint response. The public endpoint route and Mobile request/response contract were not changed; `free_context` remains a first-class result; runtime candidate IDs are still validated against current CMS candidates for the lesson; production credential validation is unchanged; and automated tests did not use a live OpenAI call. Verification completed: `OpenAiVoiceScenarioResolutionServiceTests` passed `61 passed, 0 failed, 0 skipped`; the full backend test suite passed `162 passed, 0 failed, 0 skipped`; the Release backend build succeeded with `0 warnings` and `0 errors`; production deployment completed through the existing `upload-backend-linux-release.ps1` script; and no EF migrations were run.

Remaining validation boundary: a physical Android retest is still required to confirm that the first clean voice scenario selection no longer returns HTTP 502. Initial mixed-script transcription rejection, keyboard overflow and lesson-screen UI work, and missing lesson-chat avatar assets remain separate Mobile issues. Do not mark the complete Mobile voice flow as fully stabilized yet. See `docs/LESSON_SESSIONS_ENDPOINTS.md` for the authoritative `POST /api/me/lesson-sessions/{sessionId}/voice-scenario-resolution` API contract.
