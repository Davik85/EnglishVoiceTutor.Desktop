# Security and release-readiness review

Review date: 2026-06-20.

Scope: documentation and source review only. No application behavior, billing logic, entitlement logic, Paddle integration behavior, database schema, migrations, deployment scripts, generated artifacts, or secrets were changed by this review.

## 2026-06-21 Admin RBAC and roadmap update

Admin RBAC is advanced but not fully production-cutover. Backend `0.1.35-backend.34` is deployed, production migration `20260620165657_AddAdminRoleAssignmentPersistence` is applied, the persistent owner-equivalent mapping exists, and the active persistent production admin role is `super_admin`. Cutover smoke passes with BootstrapAdmin fallback enabled by default; no explicit production fallback override is present and no production fallback-disabling cutover has been performed.

Public release still requires a controlled fallback cutover rehearsal and rollback drill, or an explicit owner-approved temporary exception. Rate limiting/abuse protection, backups/restore and migration rollback drills, monitoring/logging/privacy hardening, Paddle live readiness plus legal/support blockers, and Microsoft Store/MSIX readiness remain blockers or pending readiness tracks.

## Current verified release context

- Production backend: `0.1.35-backend.34` at `/opt/languagevoicetutor/backend/releases/0.1.35-backend.34` through the `/opt/languagevoicetutor/backend/current` symlink.
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

- Production role management/RBAC is not enabled. Bootstrap admins currently receive the full admin permission set; role/permission UI is awareness-only and does not replace endpoint authorization.
- Rate limiting / abuse protection was not found as a completed production control in this review. Add or verify rate limiting for auth, password reset, checkout creation, admin login/admin actions, and webhook endpoints before broad public launch.
- CORS/reverse-proxy hardening must be verified in the production host configuration, including allowed origins, forwarded headers, HTTPS enforcement, request size limits, and secure headers.
- Public health/database health endpoints should remain coarse and should be monitored for abuse/noise.

## C. Admin/CMS security

### Confirmed safeguards

- Admin shell access is BootstrapAdmin-based in the current controlled tester foundation.
- `/api/admin/me` and `/api/admin/capabilities` expose roles/permissions for UI awareness; `productionRolesAvailable=false` remains the current state.
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
- Production DB ownership, least-privilege permissions, backup/restore drills, and migration rollback drills must be verified outside this documentation-only review.

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
- Production backup/restore and migration rollback drills.

### Medium priority

- Validate legacy update/reinstall and AppData migration from old executable names.
- Harden Admin raw JSON/content preview for production editor use.
- Document and test production CORS/reverse-proxy/security-header assumptions.
- Review log retention and user lesson-content privacy posture.

### Low priority / deferred

- Referral/promo logic.
- Mobile entitlement bridges for Apple/Google.
- Broader production support tooling and automation beyond controlled tester support.
