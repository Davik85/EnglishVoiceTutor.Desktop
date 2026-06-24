# Release readiness review

Review date: 2026-06-20.

Scope: controlled tester/direct Windows release readiness and broader public-release remaining work. This review is documentation-only and does not change product behavior, billing logic, entitlement logic, Paddle integration, database schema, migrations, deployment scripts, generated artifacts, or secrets.

## 2026-06-21 Admin RBAC and roadmap update

Admin RBAC fallback disable is production-complete for the owner-equivalent path. Backend `0.1.35-backend.40` is deployed, production migration `20260620165657_AddAdminRoleAssignmentPersistence` is applied, persistent `super_admin` mappings exist, and production explicitly sets `AdminAuthorization__EnableBootstrapAdminFallbackForAdminPermissionPolicies=false`. Admin RBAC smoke passed with `fallbackEnabled=False`, `persistentRoleAuthorizationEnabled=True`, and `actorMappingFound=True`.

Public release still requires remaining operational readiness work: the completed Phase 4A backup/readability/separate-drill-restore plus completed local backup schedule activation plus completed Phase 4 backup/restore/migration rollback drills plus optional off-server backup hardening, monitoring/logging/privacy hardening, Paddle live readiness plus legal/support blockers, Microsoft Store/MSIX readiness, and validation of non-owner roles/critical-change approval. Rate limiting/abuse protection Phase 3 is implemented at the single-instance/in-memory level with distributed/shared limiter storage deferred.

## Current verified state recorded for release planning

- Backend `0.1.35-backend.40` is deployed at `/opt/languagevoicetutor/backend/releases/0.1.35-backend.40` and production `/health` plus `/api/health/database` return `200 OK`.
- Windows direct tester `0.1.36-tester.24` is live as `LanguageVoiceTutorSetup-0.1.36-tester.24.exe` with `backendBaseUrl=https://api.languagevoicetutor.com` and `updateMode=manual-confirmation`.
- Trial reference plan is seeded/required. Trial is displayed as a first-class tariff/reference plan, while Trial access remains entitlement-owned.
- Learner Account subscription UI is simplified to Current tariff, Free lessons remaining, Premium, and Auto-renewal.
- Premium continuous coverage display is backend-computed and can include queued paid Premium periods; `PremiumActive` remains based only on active started entitlements.
- Paddle sandbox checkout and sandbox cancel-renewal work through backend-owned flows. Production/live Paddle readiness remains deferred.
- The release remains a controlled tester/direct Windows release, not broad public production launch.
- Current controlled tester/direct Windows releases continue to use the existing Inno Setup installer flow; the preferred eventual full public release direction is Microsoft Store + MSIX after the project is fully release-ready. This review does not change packaging scripts, upload scripts, `latest.json`, release validation, or installer behavior.


## 1. Release blockers for controlled external tester handoff

No new critical blockers were found in this documentation/source review, assuming the following handoff checks are performed immediately before inviting testers:

- Verify live Windows `latest.json` over HTTPS still points to `0.1.36-tester.24`, `LanguageVoiceTutorSetup-0.1.36-tester.24.exe`, `https://api.languagevoicetutor.com`, and `manual-confirmation`.
- Verify backend symlink still resolves to `/opt/languagevoicetutor/backend/releases/0.1.35-backend.40` and `/health` plus `/api/health/database` are green.
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
- Before public release, define content approval ownership, runtime validation thresholds, rollback procedure, and post-publish monitoring.

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

Phase 4C improved operator preparation and is now followed by completed Phase 4D permission-fidelity evidence. Phase 4A remains completed, Phase 4B local PostgreSQL backup scheduling is active on production and must continue to be verified operationally, Phase 4C dry-run rehearsal is complete, and Phase 4D permission-fidelity restore drill is complete. Contabo VPS Auto Backup is an additional provider-level safety layer rather than a substitute for PostgreSQL `pg_dump`/`pg_restore` validation. Off-server encrypted backups remain optional future infrastructure hardening. Production/live Paddle readiness remains deferred, and broad public production readiness is not claimed.

Verified Phase 4C evidence: backend current `/opt/languagevoicetutor/backend/releases/0.1.35-backend.39`, previous `/opt/languagevoicetutor/backend/releases/0.1.35-backend.38`, health/database health `200 OK`, latest readable backup `/var/backups/languagevoicetutor/postgres/lvt_app_db_20260623_153008Z.dump`, `pg_restore --list` line count `245`, latest EF migration `20260620165657_AddAdminRoleAssignmentPersistence`, required key tables `OK`, backend service active/enabled, backup timer enabled/active with next observed run `2026-06-24 03:15 CEST`, and Contabo VPS Auto Backup enabled as a provider/VPS-level layer rather than a replacement for PostgreSQL validation.

## 2026-06-23 Phase 4D completion update

Phase 4D permission-fidelity restore drill completed successfully on 2026-06-23. The owner/ACL-aware backup `/var/backups/languagevoicetutor/postgres/lvt_app_db_owner_acl_20260623_161611Z.dump` for production database `lvt_app_db` was non-empty (`3.4M`), passed `pg_restore --list` readability with `245` lines, and restored into separate drill database `lvt_app_db_owner_acl_drill_20260623_161611Z`. Key table owners and `lvt_app` grants matched the production baseline, key tables returned `OK`, latest migration was `20260620165657_AddAdminRoleAssignmentPersistence`, the drill database was cleaned up, and production backend remained healthy on `0.1.35-backend.39`.

Phase 4 is complete for the current release-readiness level: Phase 4A backup/readability/separate restore drill, Phase 4B local scheduled PostgreSQL backups, Phase 4C migration rollback/remediation dry-run, and Phase 4D permission-fidelity restore drill are complete. Off-server encrypted backups remain optional future infrastructure hardening rather than an immediate release blocker. Production/live Paddle readiness remains deferred, Microsoft Store/MSIX remains later release-channel work, and broad public production readiness is not claimed.

## 2026-06-23 Phase 5A logging/privacy audit update

Phase 5A lightweight production logging/privacy audit is complete and documented in `docs/LOGGING_PRIVACY_AUDIT.md`. This was documentation/audit only: no code, backend runtime behavior, Desktop behavior, billing/Paddle semantics, EF migrations, deployment scripts, external services, or heavy monitoring infrastructure were changed. No obvious dangerous source-code logging issue requiring immediate fix was found.

The current logging/privacy posture remains controlled-tester appropriate when operators follow the redaction rule: paste only bounded non-secret operational evidence, and never paste secrets, tokens, connection strings, `.env` contents, raw Paddle signatures or payloads, raw OpenAI/STT/TTS/lesson content, SQL dumps, backup contents, or full unfiltered terminal transcripts. The smallest next hardening step is a bounded production log sampling/redaction checklist before introducing any heavy monitoring stack. Broad public production readiness is still not claimed, and production/live Paddle readiness remains deferred.

## 2026-06-24 Phase 5C production logging hardening note

Phase 5B bounded production log sampling found over-verbose EF Core `Microsoft.EntityFrameworkCore.Database.Command[20101]` entries at `Information` level with SQL command text. The sampled output redacted parameter values as `?` and did not show raw passwords, bearer tokens, refresh-token values, connection strings, OpenAI API keys, raw Paddle payload contents, raw SQL dumps, or raw secrets, so this is not treated as a data breach. It is a release-readiness issue because SQL text can expose sensitive schema/field names and unnecessary health-check/CMS noise.

Phase 5C production logging hardening is deployed and production-verified on backend `0.1.35-backend.40`. `/opt/languagevoicetutor/backend/current` points to `/opt/languagevoicetutor/backend/releases/0.1.35-backend.40`, `/opt/languagevoicetutor/backend/previous` points to `/opt/languagevoicetutor/backend/releases/0.1.35-backend.39`, `/health` returned `200 OK`, `/api/health/database` returned `200 OK`, and a repeat database-health check also returned `200 OK`. `languagevoicetutor-backend.service` is active and enabled. Post-deploy journal sampling over the recent verification window returned 0 lines for the bounded sensitive/EF SQL grep set: `Microsoft.EntityFrameworkCore.Database.Command`, `SELECT`, `INSERT`, `UPDATE`, `PasswordHash`, `TokenHash`, `RawPayload`, and `SignatureHeader`. No EF migrations were run for this config-only backend release, and no production database schema or data changed. Production/live Paddle readiness remains deferred, and broad public production readiness is still not claimed.
