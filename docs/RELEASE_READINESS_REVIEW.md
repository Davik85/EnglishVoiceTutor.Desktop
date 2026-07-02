# Release readiness review

Review date: 2026-06-30.

Scope: controlled direct Windows release readiness and broader public-release remaining work. This review is documentation-only and does not change backend runtime code, desktop runtime code, product behavior, billing logic, entitlement logic, Paddle/OpenAI runtime behavior, database schema, migrations, Inno installer behavior, deployment scripts, generated artifacts, signing keys, or secrets.

## 2026-06-21 Admin RBAC and roadmap update

Admin RBAC fallback disable is production-complete for the owner-equivalent path. Backend `0.1.35-backend.82` is deployed, production migration `20260620165657_AddAdminRoleAssignmentPersistence` is applied, persistent `super_admin` mappings exist, and production explicitly sets `AdminAuthorization__EnableBootstrapAdminFallbackForAdminPermissionPolicies=false`. Admin RBAC smoke passed with `fallbackEnabled=False`, `persistentRoleAuthorizationEnabled=True`, and `actorMappingFound=True`.

Public release still requires remaining operational readiness work: the completed Phase 4A backup/readability/separate-drill-restore plus completed local backup schedule activation plus completed Phase 4 backup/restore/migration rollback drills plus optional off-server backup hardening, monitoring/logging/privacy hardening, Paddle live readiness plus legal/support blockers, validation of non-owner roles/critical-change approval. Rate limiting/abuse protection Phase 3 is implemented at the single-instance/in-memory level with distributed/shared limiter storage deferred.


## Admin/CMS statistics boundary fix (2026-06-25)

- Admin/CMS release analytics now treat Premium and Trial users as access categories, not installs/devices.
- The current live card label is `Tracked signed-in app/device records`. The app/device metric is a count of signed-in backend `DeviceEntity` app/device records only; it is not raw installer downloads and must not include Premium entitlements, Trial grants, subscription snapshots, billing events, or users solely because they currently have Premium access.
- Registered users remain derived from backend `UserEntity` rows, active trials from active Trial grants, active Premium users from active Premium entitlements, active/free user categories from recent activity and current access state, and language statistics from user settings/profile or lesson/usage activity as appropriate.
- This is a release analytics correctness fix before Paddle live readiness. Controlled live Paddle validation is complete; broad public production readiness is still not claimed.

## Current verified state recorded for release planning

- Backend `0.1.35-backend.82` is deployed at `/opt/languagevoicetutor/backend/releases/0.1.35-backend.82` and production `/health` plus `/api/health/database` return `200 OK`.
- Windows direct public `1.0` is live as `LanguageVoiceTutorSetup-1.0.exe` with `backendBaseUrl=https://api.languagevoicetutor.com` and `updateMode=manual-confirmation`.
- AI Models persistent production storage is verified at `/opt/languagevoicetutor/backend/site/content/ai-model-settings.json`; it contains the known-good lesson tutor chat `gpt-5.5`, feedback/correction `gpt-5.2`, lesson hint `gpt-5.2`, and translation `gpt-5.2` setup; matched the release copy by SHA-256 `94f84fc07551d821bfa9dc0682bb4ee60108d11d74987b84ebb39fce96f825f1`; and survived a backend service restart with health/database health still green. This was a production data/config persistence correction, not a backend deploy, DB migration, Website CMS publish, or Windows installer upload.
- Trial reference plan is seeded/required. Trial is displayed as a first-class tariff/reference plan, while Trial access remains entitlement-owned.
- Learner Account subscription UI is simplified to Current tariff, Free lessons remaining, Premium, and Auto-renewal.
- Premium continuous coverage display is backend-computed and can include queued paid Premium periods; `PremiumActive` remains based only on active started entitlements.
- Paddle sandbox checkout and sandbox cancel-renewal work through backend-owned flows. Controlled live Paddle validation is complete; broader launch readiness remains pending.
- The release remains a controlled direct Windows release, not broad public production launch.
- Current controlled direct Windows releases continue to use the existing Inno Setup installer flow; Microsoft Store/MSIX was evaluated and discontinued for now; future trust work should focus on direct installer code signing. This review does not change packaging scripts, upload scripts, `latest.json`, release validation, or installer behavior.


## 1. Release blockers for controlled external tester handoff

No new critical blockers were found in this documentation/source review. The earlier AI Models persistent storage risk is resolved. Perform the following handoff checks immediately before inviting testers:

- Verify live Windows `latest.json` over HTTPS still points to `1.0`, `LanguageVoiceTutorSetup-1.0.exe`, `https://api.languagevoicetutor.com`, and `manual-confirmation`.
- Verify backend symlink still resolves to `/opt/languagevoicetutor/backend/releases/0.1.35-backend.82`, `/health` plus `/api/health/database` are green, and the persistent AI Models file still exists under `/opt/languagevoicetutor/backend/site/content/` with the known-good model IDs.
- Perform one installed-build smoke: registration/login, auth restore, lesson start, at least one lesson completion path, TTS/bot voice, Conversation Mode, Lesson History, Progress, Account view, Buy Premium sandbox path, Refresh status, and Cancel subscription sandbox path where applicable.
- Confirm generated artifacts, installers, backend ZIPs, generated release folders, temp deploy scripts, SQL outputs, `.env` files, and secrets are not committed.
- Prepare tester feedback intake: tester group, feedback template, severity labels, known-issue list, and rollback/contact instructions.

## 2. Strongly recommended before wider public release

### Install/update flow

- Add code signing for Windows installers before a public release candidate or broad public distribution. Controlled tester/direct release can remain unsigned for now if accepted knowingly; public release candidate should require signing or a documented owner-approved exception, and signing verification must be added before broad public distribution.
- Validate update/reinstall from older `EnglishVoiceTutor.Desktop.*` installed builds and confirm auth/session, settings, Lesson History, and Progress are preserved.
- Keep manifest identity validation, SHA-256 verification, and user-confirmation-only update behavior.

### Auth/session persistence

- Keep DPAPI-protected local auth session storage and no raw password storage.
- Verify refresh-token expiration/revocation behavior under production support scenarios.
- Keep Phase 3 rate limiting enabled and monitor/tune login, registration, password reset, refresh, learner, admin-sensitive, billing, and webhook throttles without changing product or entitlement semantics.

### Lesson start and completion

- Continue smoke coverage for lesson start, active lesson continuation, free lesson consumption, Finish lesson confirmation, Lesson History, and Progress.
- Triage occasional server-error reports only when reproducible with safe logs/correlation ids.

### Conversation Mode

- Keep Conversation Mode in controlled tester validation.
- Before public release, verify voice capture, transcript quality, interrupt/retry behavior, and user expectations across supported locales/devices.

### Voice/TTS

- Continue testing bot voice autoplay and TTS failures.
- Ensure logs do not capture sensitive microphone/audio content beyond intentional product telemetry/support boundaries.

### Lesson History and Progress

- Verify history/progress migration and preservation across reinstall/update.
- Confirm backend and local data recovery/support expectations.

### CMS runtime content source

- CMS published snapshot is active for controlled tester lessons; static JSON fallback remains rollback/safety.
- Before public release, define content approval ownership, runtime validation thresholds, rollback procedure, and post-publish monitoring. AI Models persistent storage is already verified; future AI Models changes remain Super Admin CMS operations using persistent server data/config, not release-folder JSON.

### Admin CMS Save draft / Publish / Restore

- Current flow is usable for controlled operators: Save draft is draft-only, Publish affects newly started learner lessons, Restore is available.
- Before production operations, add production RBAC, endpoint-level permissions, critical-change approval, and editor training/process.

### Localization for release-ready languages

- Keep the 14 release-ready interface languages under audit: `en`, `es`, `fr`, `de`, `it`, `pt`, `ru`, `pl`, `ar`, `ja`, `ko`, `sr`, `hr`, `bg`.
- Run localization audits before each public release and avoid adding languages without full UI coverage.

### Billing/trial/Premium sandbox

- Keep trial/reference plan and entitlement-owned access behavior.
- Continue sandbox validation for Premium stacking, queued paid Premium after trial, continuous coverage display, cancel-renewal, and safe failure handling.

### Paddle production readiness

- Production/live Paddle remains deferred. Complete live credentials, live product/price, live webhook destination, webhook monitoring, reconciliation, refunds, chargebacks, customer portal, finance operations, and legal policy review before broad paid launch.

### Support/admin operations

- Controlled tester support actions are available: manual Premium grant/revoke, free lesson reset, billing diagnostics, and admin cancel-renewal with reason.
- BootstrapAdmin is acceptable for controlled testing only. A public release candidate requires production Admin RBAC or a documented owner-approved exception.
- Endpoint-level permission enforcement is required before exposing support, content, or billing admin actions broadly; Admin UI awareness is not enough.
- Audit logging must remain mandatory for dangerous actions such as manual Premium grant/revoke, free lesson reset, cancel-renewal, CMS publish, CMS restore/rollback, and role/permission changes.
- Before public operations, add production RBAC, least-privilege roles, approval workflow for risky actions, support runbooks, and audit review process. See `docs/PRODUCTION_ADMIN_RBAC_READINESS.md`.

### Security review findings

- Resolve high-priority items from `docs/SECURITY_RELEASE_REVIEW.md`: code signing, production RBAC, Paddle live readiness, rate limiting, completed Phase 4A backup/readability/separate-drill-restore, completed Phase 4B local backup timer activation, completed Phase 4C/4D drills and optional off-server encrypted backup hardening.

### Monitoring/logging/backups

- Production local DB backup schedule is active as of 2026-06-23: `languagevoicetutor-postgres-backup.timer` is enabled and `active (waiting)`, next observed trigger `2026-06-24 03:15 CEST`, one-off service run `Result=success`/`ExecMainStatus=0`, and latest backup readability verified with `pg_restore --list` at `245` lines for `/var/backups/languagevoicetutor/postgres/lvt_app_db_20260623_150541Z.dump`. Continue to verify production metrics/log review, uptime alerts, retention monitoring, optional off-server encrypted backups, backup retention monitoring, and incident response contacts.
- Ensure logs/audit records avoid tokens, secrets, raw provider payloads in broad views, connection strings, password reset codes, and full provider ids.

### Legal/compliance/policies

- Review privacy policy, terms, subscription/trial/refund/cancellation policy, support contact process, data deletion/retention, and jurisdiction-specific subscription disclosures separately before public launch.

### Rollback plan

- Maintain separate rollback plans for Windows direct release, backend release symlink, CMS runtime fallback/static JSON, and EF migrations/data changes.
- Do not assume code rollback reverses migrations or reference data changes.

### Tester feedback process

- Use a small tester cohort, known issue list, structured feedback form, severity triage, reproducibility notes, safe log collection instructions, and release decision meeting before expanding scope.

## 3. Deferred / post-release work

- Production/live Paddle launch and full billing operations.
- Refunds, chargebacks, customer portal, referral/promo logic, and broader finance automation.
- Mobile releases and Apple/Google entitlement bridge.
- Full production Admin role management/RBAC and critical-change approval if not completed before product.
- Additional content polishing for short/repeating scenarios and avatar dialogue quality.
- Advanced monitoring dashboards and support automation beyond the controlled tester needs.


## 2026-06-23 Phase 4C documentation/tooling update

Phase 4C migration rollback/remediation dry-run rehearsal was completed successfully on 2026-06-23, and the rehearsal assets exist as documentation and a dry-run operator command printer: `docs/MIGRATION_ROLLBACK_REMEDIATION_RUNBOOK.md` and `tools/migration_rollback_remediation_commands.ps1`. The completed rehearsal was read-only and did not mutate production database state, did not run EF migrations, did not apply SQL, did not restore over production, and did not change backend runtime, Desktop, Admin UI, CMS, billing/Paddle, deployment, package, or upload behavior.

Phase 4C improved operator preparation and is now followed by completed Phase 4D permission-fidelity evidence. Phase 4A remains completed, Phase 4B local PostgreSQL backup scheduling is active on production and must continue to be verified operationally, Phase 4C dry-run rehearsal is complete, and Phase 4D permission-fidelity restore drill is complete. Contabo VPS Auto Backup is an additional provider-level safety layer rather than a substitute for PostgreSQL `pg_dump`/`pg_restore` validation. Off-server encrypted backups remain optional future infrastructure hardening. Controlled live Paddle validation is complete; broad public production readiness is not claimed.

Verified Phase 4C evidence: backend current `/opt/languagevoicetutor/backend/releases/0.1.35-backend.39`, previous `/opt/languagevoicetutor/backend/releases/0.1.35-backend.38`, health/database health `200 OK`, latest readable backup `/var/backups/languagevoicetutor/postgres/lvt_app_db_20260623_153008Z.dump`, `pg_restore --list` line count `245`, latest EF migration `20260620165657_AddAdminRoleAssignmentPersistence`, required key tables `OK`, backend service active/enabled, backup timer enabled/active with next observed run `2026-06-24 03:15 CEST`, and Contabo VPS Auto Backup enabled as a provider/VPS-level layer rather than a replacement for PostgreSQL validation.

## 2026-06-23 Phase 4D completion update

Phase 4D permission-fidelity restore drill completed successfully on 2026-06-23. The owner/ACL-aware backup `/var/backups/languagevoicetutor/postgres/lvt_app_db_owner_acl_20260623_161611Z.dump` for production database `lvt_app_db` was non-empty (`3.4M`), passed `pg_restore --list` readability with `245` lines, and restored into separate drill database `lvt_app_db_owner_acl_drill_20260623_161611Z`. Key table owners and `lvt_app` grants matched the production baseline, key tables returned `OK`, latest migration was `20260620165657_AddAdminRoleAssignmentPersistence`, the drill database was cleaned up, and production backend remained healthy on `0.1.35-backend.39`.

Phase 4 is complete for the current release-readiness level: Phase 4A backup/readability/separate restore drill, Phase 4B local scheduled PostgreSQL backups, Phase 4C migration rollback/remediation dry-run, and Phase 4D permission-fidelity restore drill are complete. Off-server encrypted backups remain optional future infrastructure hardening rather than an immediate release blocker. Controlled live Paddle validation is complete, Microsoft Store/MSIX is discontinued for now, and broad public production readiness is not claimed.

## 2026-06-23 Phase 5A logging/privacy audit update

Phase 5A lightweight production logging/privacy audit is complete and documented in `docs/LOGGING_PRIVACY_AUDIT.md`. This was documentation/audit only: no code, backend runtime behavior, Desktop behavior, billing/Paddle semantics, EF migrations, deployment scripts, external services, or heavy monitoring infrastructure were changed. No obvious dangerous source-code logging issue requiring immediate fix was found.

The current logging/privacy posture remains controlled-tester appropriate when operators follow the redaction rule: paste only bounded non-secret operational evidence, and never paste secrets, tokens, connection strings, `.env` contents, raw Paddle signatures or payloads, raw OpenAI/STT/TTS/lesson content, SQL dumps, backup contents, or full unfiltered terminal transcripts. The smallest next hardening step is a bounded production log sampling/redaction checklist before introducing any heavy monitoring stack. Broad public production readiness is still not claimed, and production/live Paddle readiness remains deferred.

## 2026-06-24 Phase 5C production logging hardening note

Phase 5B bounded production log sampling found over-verbose EF Core `Microsoft.EntityFrameworkCore.Database.Command[20101]` entries at `Information` level with SQL command text. The sampled output redacted parameter values as `?` and did not show raw passwords, bearer tokens, refresh-token values, connection strings, OpenAI API keys, raw Paddle payload contents, raw SQL dumps, or raw secrets, so this is not treated as a data breach. It is a release-readiness issue because SQL text can expose sensitive schema/field names and unnecessary health-check/CMS noise.

Phase 5C production logging hardening was first deployed on backend `0.1.35-backend.40` and is retained, deployed, and production-verified on current backend `0.1.35-backend.82`. `/opt/languagevoicetutor/backend/current` points to `/opt/languagevoicetutor/backend/releases/0.1.35-backend.82`, `/opt/languagevoicetutor/backend/previous` points to `/opt/languagevoicetutor/backend/releases/0.1.35-backend.49`, `/health` returned `200 OK`, `/api/health/database` returned `200 OK`, and a repeat database-health check also returned `200 OK`. `languagevoicetutor-backend.service` is active and enabled. Post-deploy journal sampling over the recent verification window returned 0 lines for the bounded sensitive/EF SQL grep set: `Microsoft.EntityFrameworkCore.Database.Command`, `SELECT`, `INSERT`, `UPDATE`, `PasswordHash`, `TokenHash`, `RawPayload`, and `SignatureHeader`. No EF migrations were run for this config-only backend release, and no production database schema or data changed. Controlled live Paddle validation is complete; broad public production readiness is still not claimed.

## 2026-06-25 Admin payment-event statistics note

Admin Product Statistics now distinguishes entitlement/access-state metrics from payment-event metrics. `Active Premium users now` remains based on currently active Premium access. `Successful payments total` and `Successful payments current month` are aggregate payment-event metrics based on internal provider-agnostic completed Premium payment records, with the current month calculated in UTC from the first day inclusive to the next month start exclusive. The Admin UI displays only aggregate counts and explanatory wording; it does not expose emails, user IDs, Paddle customer IDs, transaction IDs, raw provider payloads, signatures, or personal data. No entitlement activation semantics, Paddle webhook processing semantics, subscription lifecycle semantics, production database data, or Desktop behavior changed. Controlled live Paddle validation is complete; broad public production readiness is not claimed.
## 2026-06-29 Website CMS Marketing / SEO documentation note

Website CMS now includes Marketing / SEO fields for the consent banner, analytics enablement, GA4 Measurement ID, ads tracking enablement, Google Ads ID, download conversion label, Search Console verification token, and `llms.txt` enablement. These values remain JSON/file-based Website CMS content, not database schema, backend secrets, env values, or committed example configuration. Real Google IDs and conversion labels must be entered only in Admin Website CMS when available; placeholders such as `G-XXXXXXXXXX` and `AW-123456789` must not be published as live values.

Public website publish now emits or maintains public HTML pages plus crawler/consent artifacts including `robots.txt`, `sitemap.xml`, `llms.txt` when enabled, and `marketing-consent.js`. Generated pages are expected to include canonical URLs, meta descriptions, Open Graph/Twitter metadata, JSON-LD where appropriate, and SoftwareApplication JSON-LD for the Windows desktop only. Consent mode defaults to denied for analytics and ads storage/user data/personalization before user choice, the banner is controlled by Website CMS, Privacy Policy includes optional analytics/advertising/cookie disclosure, and GA/Ads scripts must not be emitted while IDs are empty or tracking is disabled. This note does not enable live Paddle, publish Website CMS, deploy the backend, upload installers, run EF migrations, add secrets, or change application behavior.

## 2026-06-30 Microsoft Store / MSIX discontinued note

The local Microsoft Store/MSIX prototype was evaluated and discontinued. The active Windows distribution path is Direct EXE/Inno installer plus direct `latest.json` update behavior. No Store submission is planned, Store/MSIX packaging is not active, and future Windows trust/signing work should focus on a code signing certificate for the direct EXE/Inno installer.

Backend deployment, database migrations, Website CMS/static site publish, and Windows direct installer upload remain separate processes. This decision does not change Paddle/payment logic, OpenAI provider logic, backend runtime behavior, database schema, deployment scripts, or Inno installer behavior.

## 2026-06-30 full release-readiness audit after Store/MSIX discontinuation

### Current Active Release Strategy

- **Windows:** Direct EXE/Inno installer is the active Windows distribution path.
- **Updates:** Direct `latest.json` update checks remain active through `site/public/releases/windows/direct/latest.json` and the public URL `https://languagevoicetutor.com/releases/windows/direct/latest.json`.
- **Signing:** Future Windows trust work is buying and integrating a code signing certificate for the direct EXE/Inno installer. Store/MSIX signing/submission is not active.
- **Backend:** Production backend is served at `https://api.languagevoicetutor.com` and uses the backend package/upload helper plus health and database-health checks.
- **Website:** Public site is `https://languagevoicetutor.com`; Website CMS/static-site publish is separate from backend deployment.
- **Billing:** Billing remains Paddle/global provider-agnostic; live Paddle readiness is still to verify before paid launch.
- **Store/MSIX:** Microsoft Store/MSIX was evaluated and discontinued for now. It must not appear as an active release path, active next step, Store-channel runtime behavior, or Store submission plan.

### Verified current release point from tracked repository state

- Windows direct release is tracked as `1.0` in `site/public/releases/windows/direct/latest.json`, with `LanguageVoiceTutorSetup-1.0.exe`, `backendBaseUrl=https://api.languagevoicetutor.com`, `minimumSupportedVersion=1.0`, and `updateMode=manual-confirmation`.
- Backend production release is tracked in release docs as `0.1.35-backend.82`. The live `/opt/languagevoicetutor/backend/current` symlink was manually verified with `ssh lvt-server "readlink -f /opt/languagevoicetutor/backend/current"` and resolved to `/opt/languagevoicetutor/backend/releases/0.1.35-backend.82`; production `/health` and `/api/health/database` were verified healthy. No backend deploy was performed by this documentation task.
- Admin CMS AI Models settings are persistent server data outside versioned backend release folders. Known-good model settings are: lesson tutor chat `gpt-5.5`; feedback/correction `gpt-5.2`; lesson hint `gpt-5.2`; translation `gpt-5.2`.
- For `gpt-5.5`, backend requests must omit `temperature`. API keys and provider secrets remain server environment secrets. The Desktop app must not call OpenAI directly and must not choose OpenAI model IDs.

### Store/MSIX rollback audit result

- No tracked `packaging/windows-msix` files were found in the working tree.
- No active Store/MSIX command should be used from the command playbook; the only Store/MSIX playbook section is a discontinued warning.
- Store/MSIX references retained in docs must be historical/discontinued context only.
- Direct `latest.json` update behavior remains the active and protected update path.
- If any future search finds `DesktopDistributionChannel`, Store-channel update guards, Store-specific update messaging, WACK commands, Partner Center submission commands, or active MSIX packaging commands, treat that as a regression unless it is explicitly marked discontinued historical context.

### Do not mix these operations

- Backend deploy is not Windows installer upload.
- Website CMS/static-site publish is not backend deploy.
- DB migration is separate, reviewed, backed up, and operator-approved; backend upload scripts do not apply migrations automatically.
- Direct Windows installer upload is not Store/MSIX packaging or Microsoft Store submission.
- Paddle live changes are provider/account/configuration work and are not code deploy unless a reviewed backend configuration/code change is intentionally required.

### Area-by-area readiness snapshot

| Area | Status | Notes |
| --- | --- | --- |
| Windows Direct EXE/Inno | Partially ready for controlled testers | `1.0` is documented and manifest-backed. Public release still needs code signing, update-over-existing-install evidence if not already current, clean-machine smoke before expansion, and controlled feedback. |
| Direct update flow | Ready for controlled testers | `latest.json`, manual confirmation, manifest identity checks, and SHA-256 verification remain the active path. |
| Backend production | Ready for controlled testers | Health/database health checks and deployment docs exist. The live current symlink verifies production backend `0.1.35-backend.82`; backend deploys, database migrations, Website CMS publish, and Windows installer uploads remain separate operations. |
| Database/migrations | Controlled/manual | Current docs say backend deploy does not run migrations. Any migration requires separate review, backup, SQL/operator procedure, and post-checks. |
| Admin CMS / AI Models | Partially ready | AI Models are persistent server data and known-good models are documented. CMS publish changes runtime content for newly started lessons. |
| Website/CMS/legal/support | Partially ready | Public site and legal/support pages are draft-ready for owner/legal review. Website CMS publish is separate. Do not claim mobile app stores or Microsoft Store availability. |
| Billing/Paddle | Blocked for paid public launch | Provider-agnostic architecture exists, but live Paddle credentials/prices/webhooks/reconciliation/refund/customer portal/finance operations still require verification and owner approval. |
| AI tutor / lessons | Partially ready | CMS owns prompt/scenario/tutor behavior tuning; product quality still needs controlled tester feedback and content approval ownership before broad launch. |
| Security/privacy/compliance | Partially ready | Secrets boundaries, desktop/backend boundary, log privacy, backups, and rate limiting are documented; code signing and live billing/legal final review remain blockers. |
| Release operations | Partially ready | Safe commands exist for checks/package/upload. Operations must remain separated and generated artifacts/secrets must not be committed. |

### Top 10 remaining release tasks in safe order

1. **Docs-only/operations hygiene:** Keep the backend release discrepancy resolved: production current is documented as `0.1.35-backend.82` after live symlink, health, and database-health verification; do not run backend deploys for docs-only work.
2. **Docs/manual:** Re-run final release-readiness checklist and confirm Store/MSIX appears only as discontinued historical context.
3. **Manual/provider:** Buy/select the Windows code signing certificate and document the signing integration plan for the direct Inno installer.
4. **Windows installer build/upload:** After signing integration is approved, build a new direct installer, validate the direct-release folder, upload with the direct upload helper, and verify HTTPS `latest.json` plus installer SHA-256.
5. **Manual QA:** Perform clean-machine install/update-over-existing-install smoke for auth/session, lesson flow, updates, history/progress, account/billing sandbox views, and uninstall/reinstall expectations.
6. **Website CMS publish:** Complete final owner/legal review of website, pricing, subscription, privacy, terms, refunds, cancellation, support, seller/company, AI/data, and status pages; publish only through Website CMS/static-site flow.
7. **Manual/provider:** Complete Paddle live readiness: live account, products/prices, client token, webhook destination/signing, reconciliation, refund/chargeback/customer portal policy, finance operations, and monitoring.
8. **Backend deploy only if needed:** Deploy backend only for an approved runtime/configuration change; otherwise do not deploy just for docs, Website CMS, installer upload, or Paddle account setup.
9. **DB migration only if needed:** Run a migration only if a reviewed schema/data change exists; this requires backup, SQL review, operator approval, privilege checks, and rollback/remediation plan.
10. **Manual/product:** Run a small external tester cohort, collect structured feedback, triage blockers, and hold a release decision before broad public launch.

### Deployment impact classification for this audit

- Documentation-only: yes.
- Backend runtime code changed: no.
- Desktop runtime code changed: no.
- Database schema changed: no.
- Inno installer behavior changed: no.
- Deployment scripts changed: no.
- Website CMS publish needed: no.
- Backend deploy needed: no.
- Windows direct installer upload needed: no.
- Store/MSIX path: discontinued, not active.

## 2026-06-30 Paddle live checkout/Admin readiness update

Current production facts after backend `0.1.35-backend.83` and before any real live payment test:

- Backend health and database health are `200 Healthy`.
- Backend server-side Paddle configuration is in the existing env file `/etc/languagevoicetutor/backend.env`; do not invent a second env file and do not create Paddle live systemd drop-ins for this configuration.
- Backend current symlink is `/opt/languagevoicetutor/backend/current`; backend releases are under `/opt/languagevoicetutor/backend/releases/<version>`.
- AI Models persistent server data remains `/opt/languagevoicetutor/backend/site/content/ai-model-settings.json`; known-good models remain `gpt-5.5` for lesson tutor chat and `gpt-5.2` for feedback correction, lesson hints, and translation.
- Static website nginx root is `/var/www/languagevoicetutor/site`. The parent `/var/www/languagevoicetutor` is not the nginx static-site root and must not be used as the static website upload target.
- Public Paddle config is `/var/www/languagevoicetutor/site/paddle.public.json`; public Paddle checkout page is `/var/www/languagevoicetutor/site/pay.html`.
- Direct Windows release files are separate at `/var/www/languagevoicetutor/releases/windows/direct` and are not touched by static website upload.
- Active Windows delivery remains Direct EXE/Inno. Store/MSIX is discontinued and must not be reintroduced. Current direct public release is `1.0`; direct `latest.json` remains active with manual-confirmation update mode.
- Paddle website review is approved, `/pay.html` and `/paddle.public.json` are deployed/reachable, backend live Paddle env is configured, and a real transaction URL opened Paddle checkout with `Language Voice Tutor Pro`, `Pro Monthly`, `14.99 EUR`.
- Controlled live payment, webhook delivery, Premium entitlement activation, failed-payment non-activation, cancel-renewal, and full-refund Premium revocation are completed. Paid-launch readiness remains incomplete until final release-readiness review and remaining non-billing blockers are closed; chargeback remains implemented/test-covered but not live-chargeback-tested, partial refund remains conservative/manual-review, and expanded customer portal/subscription management is deferred.

Static website upload command must target the real nginx root:

```powershell
scripts/upload-static-site.ps1 -ServerHost "lvt-server" -ServerUser "deploy" -RemotePath "/var/www/languagevoicetutor/site"
```

Safe backend env verification must redact secrets and must use the existing env file, for example:

```bash
sudo awk -F= '/^(Billing__|PaddleBilling__|PaddleWebhook__)/ { v=$2; if ($1 ~ /(ApiKey|SecretKey|Token)/) v=(length($2)>0 ? "SET" : "EMPTY"); print $1 "=" v }' /etc/languagevoicetutor/backend.env
```

Admin capabilities should now distinguish configuration from launch completion: configured live checkout/webhooks can be reported as available/configured, while `billingLivePaymentTestComplete=false` and `billingPaidLaunchReleaseComplete=false` continue to block paid launch until the controlled live payment path is documented.
