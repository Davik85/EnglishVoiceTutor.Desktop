# Security and release-readiness review

Review date: 2026-06-20.

Scope: documentation and source review only. No application behavior, billing logic, entitlement logic, Paddle integration behavior, database schema, migrations, deployment scripts, generated artifacts, or secrets were changed by this review.

## 2026-06-21 Admin RBAC and roadmap update

Admin RBAC fallback disable is production-complete for the owner-equivalent path. Backend `0.1.35-backend.50` is deployed, production migration `20260620165657_AddAdminRoleAssignmentPersistence` is applied, persistent `super_admin` mappings exist, and production explicitly sets `AdminAuthorization__EnableBootstrapAdminFallbackForAdminPermissionPolicies=false`. Admin RBAC smoke passed with `fallbackEnabled=False`, `persistentRoleAuthorizationEnabled=True`, and `actorMappingFound=True`.

Public release still requires remaining operational readiness work: the completed Phase 4A backup/readability/separate-drill-restore plus completed local backup schedule activation plus completed Phase 4 backup/restore/migration rollback drills plus optional off-server backup hardening, monitoring/logging/privacy hardening, Paddle live readiness plus legal/support blockers, Microsoft Store/MSIX readiness, and validation of non-owner roles/critical-change approval. Rate limiting/abuse protection Phase 3 is implemented at the single-instance/in-memory level with distributed/shared limiter storage deferred.


## Admin/CMS statistics boundary fix (2026-06-25)

- Admin/CMS release analytics now treat Premium and Trial users as access categories, not installs/devices.
- The current live card label is `Tracked signed-in app/device records`. The app/device metric is a count of signed-in backend `DeviceEntity` app/device records only; it is not raw installer downloads and must not include Premium entitlements, Trial grants, subscription snapshots, billing events, or users solely because they currently have Premium access.
- Registered users remain derived from backend `UserEntity` rows, active trials from active Trial grants, active Premium users from active Premium entitlements, active/free user categories from recent activity and current access state, and language statistics from user settings/profile or lesson/usage activity as appropriate.
- This is a release analytics correctness fix before Paddle live readiness. Production/live Paddle readiness remains deferred, and broad public production readiness is still not claimed.

## Current verified release context

- Production backend: `0.1.35-backend.50` at `/opt/languagevoicetutor/backend/releases/0.1.35-backend.50` through the `/opt/languagevoicetutor/backend/current` symlink.
- Production backend health: `/health` returns `200 OK` and `/api/health/database` returns `200 OK`.
- Windows direct tester release: `0.1.36-tester.24`, installer `LanguageVoiceTutorSetup-0.1.36-tester.24.exe`, `backendBaseUrl=https://api.languagevoicetutor.com`, `updateMode=manual-confirmation`.
- This remains a controlled tester/direct Windows release, not broad public production readiness.
- Billing remains controlled sandbox/tester validation. Paddle production/live readiness remains deferred.

## A. Desktop security

### Confirmed safeguards

- Auth/session persistence uses a local `auth-session.json` payload protected with Windows DPAPI for the current user. Logout clears persisted session data.
- The desktop stores access/refresh session state, not raw passwords.
- Packaged non-Debug Windows builds are server-only and use `https://api.languagevoicetutor.com`; normal release Settings do not expose a backend URL override.
- Desktop Account billing uses backend endpoints only. The desktop does not call Paddle directly and does not store Paddle API keys, webhook secrets, provider price ids, provider subscription ids, or raw provider payloads.
- The manual update flow validates manifest identity (`productName`, `appId`, platform, architecture), compares the version, requires user confirmation before download, verifies SHA-256 against `installerSha256`, and asks again before launching the installer.
- There is no silent auto-update, no background update service, and no installer launch before SHA-256 verification.
- Installer naming uses `LanguageVoiceTutorSetup-{version}.exe`; installed application files have moved to `LanguageVoiceTutor.Desktop.*`, with legacy `EnglishVoiceTutor.Desktop.*` cleanup in the install folder while preserving AppData.

### Remaining risks before broader release

- Code signing remains deferred, so Windows SmartScreen and tamper-trust friction remain likely for broad distribution.
- Update-over-existing-install from older `EnglishVoiceTutor.Desktop.*` builds still needs explicit controlled validation for auth/session, settings, Lesson History, and Progress preservation.
- Logs and support bundles must continue to avoid bearer tokens, refresh tokens, raw auth/session files, Paddle secrets, raw provider payloads, connection strings, and full provider ids.

## B. Backend API security

### Confirmed safeguards

- Auth endpoints validate required email/password fields and enforce minimum password length. Passwords are stored as password hashes, not raw passwords.
- Refresh tokens are persisted server-side as token hashes with finite expiration and revocation support.
- Admin endpoints are protected by the BootstrapAdmin authorization policy in the current foundation.
- Public health endpoints intentionally expose coarse health/database status and no secrets.
- Sensitive configuration placeholders in tracked config are blank/default; real SMTP, database, Paddle, OpenAI, JWT, and webhook values must come from secure environment/deployment configuration.
- Paddle webhooks use `Paddle-Signature` verification over the raw request body with timestamp tolerance and timing-safe HMAC comparison.

### Gaps / risks

- Production Admin RBAC fallback disable is complete for the owner-equivalent path, with persistent `super_admin` mappings verified and BootstrapAdmin fallback disabled for `AdminPermission:*` policies. Non-owner role validation and critical-change approval remain future work.
- Phase 3 rate limiting / abuse protection is a completed production control at the single-instance/in-memory level for auth, learner, Admin, billing, and webhook surfaces; distributed/shared limiter storage remains future work before multi-instance scale-out.
- CORS/reverse-proxy hardening must be verified in the production host configuration, including allowed origins, forwarded headers, HTTPS enforcement, request size limits, and secure headers.
- Public health/database health endpoints should remain coarse and should be monitored for abuse/noise.

## C. Admin/CMS security

### Confirmed safeguards

- Admin shell access now relies on persistent Admin RBAC for `AdminPermission:*` policies in production because BootstrapAdmin fallback is explicitly disabled; BootstrapAdmin remains only a rollback mechanism if the setting is changed intentionally.
- `/api/admin/me` and `/api/admin/capabilities` expose roles/permissions for UI awareness; persistent role authorization is enabled in production.
- Admin action audit logging exists for support operations such as Premium grants/revokes, free lesson allowance reset, and billing cancel-renewal actions.
- CMS supports Save draft, Publish current draft, Restore, and runtime published-snapshot reads. Draft edits do not affect learner runtime until published.
- Runtime CMS diagnostics expose safe metadata and bounded validation details, not lesson bodies, prompt bodies, tutor instruction bodies, secrets, tokens, API keys, connection strings, or auth headers.
- Admin support cancel-renewal exists and requires a non-secret reason.

### Gaps / risks

- Production RBAC, admin user/role management, endpoint-level per-role enforcement, and critical-change approval remain incomplete.
- Raw JSON editing/display and content preview are acceptable for controlled admin testing but should be reviewed for XSS/content-injection handling, accidental prompt disclosure, and editor permissions before production operations.
- Manual Premium grant/revoke, free lesson reset, and admin cancel-renewal are safe only for tightly controlled tester support with audit review; they are not yet a complete production operations model.

## D. Billing/Paddle security

### Confirmed safeguards

- Desktop does not call Paddle directly.
- Admin UI does not call Paddle directly.
- Checkout is backend-hosted. The backend creates a Paddle transaction and returns a backend-hosted `/checkout/paddle` launch URL when explicitly configured.
- Webhook validation uses Paddle signature verification; provider event ingestion is idempotent by provider event id.
- Entitlements remain the source of truth for Premium/free status. Checkout creation alone does not activate Premium.
- Cancel-renewal means cancel at period end / next billing period. It must not immediately revoke paid Premium entitlement.
- Failed provider cancellation must not mark renewal as canceled or revoke Premium.
- Learner UI does not expose secrets or raw provider payloads. Admin diagnostics should expose only safe provider diagnostics such as safe error code/message, HTTP status, correlation id, and last-four/hash provider id views.
- Trial is a first-class reference tariff for display, but Trial access remains entitlement-owned.
- Premium continuous coverage display uses backend-computed entitlement coverage and may include queued paid Premium periods; `PremiumActive` remains based on active started entitlements only.

### Deferred / not ready

- Production/live Paddle readiness remains deferred.
- Refunds, chargebacks, customer portal handoff, production subscription operations, production webhook delivery monitoring, and live finance reconciliation remain deferred.

## E. Database / migrations / deployment security

- EF migrations are explicit operator actions and separate from backend packaging/upload/deploy scripts.
- Backend upload scripts must not run `dotnet ef database update`, apply SQL, upload Windows installers, change public `latest.json`, or upload secrets.
- The required `free`, `trial`, and `premium` plan reference rows are data/reference prerequisites. Trial is required for current learner tariff display and trial entitlement reference behavior.
- Generated release artifacts under `artifacts/`, installers, backend ZIPs, generated release folders, temporary deployment scripts, SQL outputs, `.env` files, and secrets must not be committed.
- Rollback after schema/data migrations requires migration-specific planning; code rollback alone may not undo data/reference changes.
- Production DB ownership and least-privilege permissions now have Phase 4D permission-fidelity drill evidence for the current release-readiness level. The earlier Phase 4A drill used `pg_restore --no-owner --no-acl`, while Phase 4D restored an owner/ACL-aware backup into a separate drill database and confirmed checked owners/grants matched the production baseline.

## F. Logging and privacy

- Backend logging reviewed in this pass is mostly operational/audit logging such as user ids, result codes, safe status values, and aggregate product statistics.
- Product statistics device tracking is coarse authenticated app/device metadata and explicitly avoids raw hardware identifiers, machine fingerprints, serial numbers, MAC addresses, Windows usernames, IP addresses, and personal device IDs.
- EF command logging with sensitive parameter values must remain disabled in production unless there is a tightly controlled incident/debug window with sanitized retention.
- Raw Paddle webhook payloads are stored server-side for ingestion/audit needs, but raw provider payloads must not be shown in learner UI, copied into docs, pasted into support tickets, or exposed through broad Admin views.
- Audit logs must avoid secrets, Authorization headers, refresh tokens, access tokens, password reset codes, API keys, webhook secrets, connection strings, full provider ids, and raw provider payloads.
- Before broad production, reduce any noisy operational logs that include user-entered lesson content unless there is a documented retention/privacy basis.

## G. Legal/compliance readiness notes

This is not legal advice. Before broad public launch, separately review and publish the legal/compliance set: privacy policy, terms of use, subscription terms, refund/cancellation policy, trial disclosures, data retention/deletion process, support contact process, and any jurisdiction-specific consumer/subscription notices.

## Findings by severity

### Critical blockers

- None identified for controlled tester/direct Windows handoff from this documentation/source review.

### High priority before broader public release

- Code signing for Windows installers.
- Production RBAC/admin role management with endpoint-level authorization beyond BootstrapAdmin.
- Production/live Paddle readiness, including live credentials, live webhook destination verification, refund/chargeback/customer portal policy, monitoring, and reconciliation.
- Rate limiting/abuse protection verification or implementation for auth, password reset, checkout, admin, and webhook surfaces.
- Local backup schedule/retention automation is active as of 2026-06-23 via `languagevoicetutor-postgres-backup.timer`; Phase 4C migration rollback/remediation dry-run and Phase 4D permission-fidelity restore drill completed on 2026-06-23; off-server encrypted backups remain optional future hardening.
- The initial production-safe Phase 4A backup/readability/separate-drill-restore was completed on 2026-06-23 without restoring over production, and Phase 4B latest backup readability was verified with `pg_restore --list` at `245` lines.

### Medium priority

- Validate legacy update/reinstall and AppData migration from old executable names.
- Harden Admin raw JSON/content preview for production editor use.
- Document and test production CORS/reverse-proxy/security-header assumptions.
- Review log retention and user lesson-content privacy posture.

### Low priority / deferred

- Referral/promo logic.
- Mobile entitlement bridges for Apple/Google.
- Broader production support tooling and automation beyond controlled tester support.


## 2026-06-23 Phase 4C migration rollback/remediation readiness note

Phase 4C migration rollback/remediation dry-run rehearsal completed successfully on 2026-06-23, supported by the runbook and dry-run command-printer helper. The rehearsal and assets are intentionally non-mutating: they do not read backend environment secrets, do not print connection strings/passwords, do not dump raw table data, do not print SQL dumps or backup contents, do not call provider APIs, do not apply SQL, do not run EF migrations, do not restore over production, and do not change backend runtime behavior.

Security posture remains that SQL remediation must be targeted and separately reviewed, broad unreviewed SQL is forbidden, and production restore-over is not part of rehearsal. Contabo VPS Auto Backup is an additional provider-level recovery layer only; it does not replace PostgreSQL custom-format backups and `pg_restore` readability validation. Off-server encrypted backups remain optional future infrastructure hardening, not an immediate release blocker. Production/live Paddle readiness remains deferred, and broad public production readiness is not claimed.

Phase 4C verified backend current `/opt/languagevoicetutor/backend/releases/0.1.35-backend.39`, previous `/opt/languagevoicetutor/backend/releases/0.1.35-backend.38`, health/database health `200 OK`, latest readable backup `/var/backups/languagevoicetutor/postgres/lvt_app_db_20260623_153008Z.dump` with `245` `pg_restore --list` lines, latest EF migration `20260620165657_AddAdminRoleAssignmentPersistence`, required key tables `OK`, backend service active/enabled, backup timer enabled/active with next observed run `2026-06-24 03:15 CEST`, and Contabo VPS Auto Backup enabled as provider/VPS-level protection rather than a PostgreSQL validation substitute. No production DB mutation, EF migration, SQL remediation, restore-over-production, or backend runtime change occurred.

## 2026-06-23 Phase 4D permission-fidelity restore security note

Phase 4D completed on 2026-06-23 without production mutation. An owner/ACL-aware backup of `lvt_app_db` restored into separate drill database `lvt_app_db_owner_acl_drill_20260623_161611Z`; key table ownership and `lvt_app` grants matched the production baseline; the drill database was dropped; and production backend `0.1.35-backend.39` stayed healthy with `/health` and `/api/health/database` returning `200 OK`. No EF migrations, SQL remediation, restore-over-production, runtime behavior, Desktop, Admin UI, CMS, billing, Paddle, package, upload, or deployment changes occurred.

Phase 4 backup/restore/migration rollback drills are complete for the current release-readiness level. Off-server encrypted backups remain optional future infrastructure hardening. Production/live Paddle readiness remains deferred, Microsoft Store/MSIX remains later release-channel work, and broad public production readiness is not claimed.

## 2026-06-23 Phase 5A logging/privacy audit security note

Phase 5A lightweight production logging/privacy audit is complete and documented in `docs/LOGGING_PRIVACY_AUDIT.md`. This was documentation/audit only: no code, backend runtime behavior, Desktop behavior, billing/Paddle semantics, EF migrations, deployment scripts, external services, or heavy monitoring infrastructure were changed. No obvious dangerous source-code logging issue requiring immediate fix was found.

The current logging/privacy posture remains controlled-tester appropriate when operators follow the redaction rule: paste only bounded non-secret operational evidence, and never paste secrets, tokens, connection strings, `.env` contents, raw Paddle signatures or payloads, raw OpenAI/STT/TTS/lesson content, SQL dumps, backup contents, or full unfiltered terminal transcripts. The smallest next hardening step is a bounded production log sampling/redaction checklist before introducing any heavy monitoring stack. Broad public production readiness is still not claimed, and production/live Paddle readiness remains deferred.

## 2026-06-24 Phase 5C logging/privacy hardening note

Phase 5B bounded production log sampling found over-verbose EF Core SQL command text in normal production logs via `Microsoft.EntityFrameworkCore.Database.Command[20101]` at `Information` level. Sampled parameter values were redacted as `?`, and no raw passwords, bearer tokens, refresh-token values, connection strings, OpenAI API keys, raw Paddle payload contents, raw SQL dumps, or raw secrets were observed. This is not classified as a data breach from the sampled evidence, but it is too verbose for release-ready production logging because SQL text can expose sensitive schema and field names.

Phase 5C production logging hardening was first deployed on backend `0.1.35-backend.40` and is retained, deployed, and production-verified on current backend `0.1.35-backend.50`. `/opt/languagevoicetutor/backend/current` points to `/opt/languagevoicetutor/backend/releases/0.1.35-backend.50`, `/opt/languagevoicetutor/backend/previous` points to `/opt/languagevoicetutor/backend/releases/0.1.35-backend.49`, `/health` returned `200 OK`, `/api/health/database` returned `200 OK`, and a repeat database-health check also returned `200 OK`. `languagevoicetutor-backend.service` is active and enabled. Post-deploy journal sampling over the recent verification window returned 0 lines for the bounded sensitive/EF SQL grep set: `Microsoft.EntityFrameworkCore.Database.Command`, `SELECT`, `INSERT`, `UPDATE`, `PasswordHash`, `TokenHash`, `RawPayload`, and `SignatureHeader`. No EF migrations were run for this config-only backend release, and no production database schema or data changed. No secrets, tokens, connection strings, SQL dumps, Paddle payloads, signatures, private keys, or raw user data were added to tracked docs. Production/live Paddle readiness remains deferred, and broad public production readiness is still not claimed.

## 2026-06-25 Admin payment-event statistics security note

Admin Product Statistics now includes aggregate successful payment total/current-month counts while preserving the separation between access-state metrics and payment-event metrics. The payment metrics use internal normalized payment records rather than raw Paddle webhook payloads and remain aggregate-only in the Admin UI, with no emails, user IDs, Paddle customer IDs, transaction IDs, payloads, signatures, or raw user data exposed. The device metric remains separate from payment/billing metrics and continues to count signed-in app/device records only. Production/live Paddle readiness remains deferred, and broad public production readiness is not claimed.
