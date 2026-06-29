# Backend server deployment

Review date: 2026-06-28.

## Current production backend

Production backend is deployed and healthy.

- Current release: `0.1.35-backend.80`
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

Expected baseline for the current deployment is release `0.1.35-backend.80`. The live server symlink is the source of truth; generated local files under `artifacts/` are not proof that a backend version is live.

Previous backend rollback reference must be verified from `/opt/languagevoicetutor/backend/previous`. `0.1.35-backend.49` remains a documented older rollback reference, not a substitute for checking the live `previous` symlink.

## Package backend release

Backend packaging uses the repository PowerShell helper and creates the linux-x64 backend archive under `artifacts/packages/backend/`:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\package-backend-linux-release.ps1 -Version 0.1.35-backend.80
```

The package command does not upload, restart, run EF migrations, publish website files, upload Windows installers, or enable Paddle live.

## Dry-run upload

Use `-PackageFirst -DryRun` to print the upload, generated deploy-helper, symlink, and restart/status commands without changing the server:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\upload-backend-linux-release.ps1 -Version 0.1.35-backend.80 -PackageFirst -DryRun
```

The upload helper creates a temporary `deploy-backend-release.sh` helper and uses that helper for release extraction and symlink switching. It uses `ssh -tt` for sudo restart/status when restart is enabled. Do not document old fragile inline bash deployment paths as the current backend deployment flow.

## Real upload

After the pre-check and dry run are reviewed, run the backend upload helper without `-DryRun`:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\upload-backend-linux-release.ps1 -Version 0.1.35-backend.80 -PackageFirst
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

Do not paste production environment values, database connection strings, API keys, or provider secrets into documentation, tickets, chat, commits, or pull requests.

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

## Scope boundaries

Backend deploy is backend-only. It does not:

- run EF migrations;
- apply reviewed SQL;
- publish static website HTML/CSS/JS;
- publish Website CMS content;
- upload Windows installer files;
- change production Paddle environment values;
- enable live Paddle;
- change Desktop app behavior.

Backend upload/package scripts do not apply EF migrations automatically. Database migrations remain a separate reviewed SQL/operator process only when schema changes exist and after backups/review/verification are complete.

Generated local files under `artifacts/` are not proof that a version is live on the public site. A locally built installer becomes public only after the Windows direct release files are uploaded to the website release folder and `latest.json` is verified over HTTPS.

## Release-readiness status

- Backend: production healthy, current release `0.1.35-backend.80`.
- Website: generated public pages and Paddle-review polish are completed separately from backend deployment.
- Download: current Windows tester release is visible without JavaScript and manifest-driven with JavaScript.
- Windows installer: current public tester release is `0.1.36-tester.31`, installer `LanguageVoiceTutorSetup-0.1.36-tester.31.exe`.
- Billing: Paddle live is not enabled yet. Production/live Paddle readiness remains deferred.
- Legal: website legal/support/seller/AI/status pages are ready for owner/legal final review as drafts, not final legal advice.

Do not state that the product is fully public production-ready. The direct Windows release remains a controlled tester/direct Windows release, not a broad public production launch, and not broad public production readiness.

## Operational context retained

Phase 3 rate limiting / abuse protection is completed and production-verified with `RateLimiting__Enabled=true`. Admin persistent role authorization remains enabled with `AdminAuthorization__EnableBootstrapAdminFallbackForAdminPermissionPolicies=false`. Admin Product Statistics uses `Tracked signed-in app/device records` for backend `DeviceEntity` records and this is not raw installer downloads. `Successful payments current month` is an internal billing-event metric and is not Premium access.

Phase 4 is complete for the current release-readiness level: Phase 4A backup/readability/separate-drill-restore completed, Phase 4B local PostgreSQL backup scheduling active, Phase 4C migration rollback/remediation dry-run rehearsal completed, and Phase 4D permission-fidelity restore drill completed. Off-server encrypted backups remain optional future infrastructure hardening.

Production/live Paddle readiness remains deferred, and broad public production readiness is still not claimed. Code signing remains deferred. CMS published-snapshot runtime is active for controlled tester lessons. Backend deployment, database migrations, the download website, and update UI remain separate work.
