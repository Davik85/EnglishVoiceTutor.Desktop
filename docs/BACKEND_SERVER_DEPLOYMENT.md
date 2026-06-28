# Backend server deployment

Review date: 2026-06-28.

## Current production backend

Production backend is deployed and healthy.

- Current release: `0.1.35-backend.74`
- Production URL: `https://api.languagevoicetutor.com`
- Health: `https://api.languagevoicetutor.com/health`
- Database health: `https://api.languagevoicetutor.com/api/health/database`

Verify before announcing or rolling back:

```powershell
ssh lvt-server "readlink -f /opt/languagevoicetutor/backend/current"
Invoke-WebRequest https://api.languagevoicetutor.com/health -UseBasicParsing
Invoke-WebRequest https://api.languagevoicetutor.com/api/health/database -UseBasicParsing
```

Previous backend rollback reference should be verified from `/opt/languagevoicetutor/backend/previous`; `0.1.35-backend.49` remains a documented older rollback reference, not a substitute for checking the live symlink.

## Deployment commands

Backend deployment uses the repository PowerShell helpers:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\package-backend-linux-release.ps1 -Version 0.1.35-backend.74
powershell -ExecutionPolicy Bypass -File .\scripts\upload-backend-linux-release.ps1 -Version 0.1.35-backend.74
```

The upload script creates/uploads a `deploy-backend-release.sh` helper and uses that helper for release extraction and symlink switching. It uses `ssh -tt` for sudo restart/status when needed. Do not document old fragile inline bash deployment paths as the current backend deployment flow.

## Scope boundaries

Backend deploy is backend-only. It does not:

- upload Windows installer files;
- publish static website HTML/CSS/JS;
- run EF migrations;
- apply reviewed SQL;
- change production Paddle environment values;
- enable live Paddle;
- change Desktop app behavior.

Backend upload/package scripts do not apply EF migrations automatically. Database migrations remain a separate reviewed SQL/operator process only when schema changes exist and after backups/review/verification are complete.

Generated local files under `artifacts/` are not proof that a version is live on the public site. A locally built installer becomes public only after the Windows direct release files are uploaded to the website release folder and `latest.json` is verified over HTTPS.

## Release-readiness status

- Backend: production healthy, current release `0.1.35-backend.74`.
- Website: generated public pages and Paddle-review polish are completed separately from backend deployment.
- Download: current Windows tester release is visible without JavaScript and manifest-driven with JavaScript.
- Windows installer: current public tester release is `0.1.36-tester.30`, installer `LanguageVoiceTutorSetup-0.1.36-tester.30.exe`.
- Billing: Paddle live is not enabled yet. Production/live Paddle readiness remains deferred.
- Legal: website legal/support/seller/AI/status pages are ready for owner/legal final review as drafts, not final legal advice.

Do not state that the product is fully public production-ready. The direct Windows release remains a controlled tester/direct Windows release, not a broad public production launch, and not broad public production readiness.

## Operational context retained

Phase 3 rate limiting / abuse protection is completed and production-verified with `RateLimiting__Enabled=true`. Admin persistent role authorization remains enabled with `AdminAuthorization__EnableBootstrapAdminFallbackForAdminPermissionPolicies=false`. Admin Product Statistics uses `Tracked signed-in app/device records` for backend `DeviceEntity` records and this is not raw installer downloads. `Successful payments current month` is an internal billing-event metric and is not Premium access.

Phase 4 is complete for the current release-readiness level: Phase 4A backup/readability/separate-drill-restore completed, Phase 4B local PostgreSQL backup scheduling active, Phase 4C migration rollback/remediation dry-run rehearsal completed, and Phase 4D permission-fidelity restore drill completed. Off-server encrypted backups remain optional future infrastructure hardening.

Production/live Paddle readiness remains deferred, and broad public production readiness is still not claimed. Code signing remains deferred. CMS published-snapshot runtime is active for controlled tester lessons. Backend deployment, database migrations, the download website, and update UI remain separate work.
