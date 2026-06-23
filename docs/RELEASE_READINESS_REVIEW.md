# Release readiness review

Review date: 2026-06-20.

Scope: controlled tester/direct Windows release readiness and broader public-release remaining work. This review is documentation-only and does not change product behavior, billing logic, entitlement logic, Paddle integration, database schema, migrations, deployment scripts, generated artifacts, or secrets.

## 2026-06-21 Admin RBAC and roadmap update

Admin RBAC fallback disable is production-complete for the owner-equivalent path. Backend `0.1.35-backend.39` is deployed, production migration `20260620165657_AddAdminRoleAssignmentPersistence` is applied, persistent `super_admin` mappings exist, and production explicitly sets `AdminAuthorization__EnableBootstrapAdminFallbackForAdminPermissionPolicies=false`. Admin RBAC smoke passed with `fallbackEnabled=False`, `persistentRoleAuthorizationEnabled=True`, and `actorMappingFound=True`.

Public release still requires remaining operational readiness work: backups/restore and migration rollback drills, monitoring/logging/privacy hardening, Paddle live readiness plus legal/support blockers, Microsoft Store/MSIX readiness, and validation of non-owner roles/critical-change approval. Rate limiting/abuse protection Phase 3 is implemented at the single-instance/in-memory level with distributed/shared limiter storage deferred.

## Current verified state recorded for release planning

- Backend `0.1.35-backend.39` is deployed at `/opt/languagevoicetutor/backend/releases/0.1.35-backend.39` and production `/health` plus `/api/health/database` return `200 OK`.
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
- Verify backend symlink still resolves to `/opt/languagevoicetutor/backend/releases/0.1.35-backend.39` and `/health` plus `/api/health/database` are green.
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

- Resolve high-priority items from `docs/SECURITY_RELEASE_REVIEW.md`: code signing, production RBAC, Paddle live readiness, rate limiting, backups/restore, and migration rollback drills.

### Monitoring/logging/backups

- Verify production metrics/log review, uptime alerts, DB backup schedule, restore drills, retention policy, and incident response contacts.
- Ensure logs/audit records avoid tokens, secrets, raw provider payloads in broad views, connection strings, password reset codes, and full provider ids.

### Legal/compliance/policies

- Review privacy policy, terms, subscription/trial/refund/cancellation policy, support contact process, data deletion/retention, and jurisdiction-specific subscription disclosures separately before public launch.

### Rollback plan

- Maintain separate rollback plans for Windows direct release, backend release symlink, CMS runtime fallback/static JSON, and EF migrations/data changes.
- Do not assume code rollback reverses migrations or reference data changes.

### Tester feedback process

- Use a small tester cohort, known issue list, structured feedback form, severity triage, reproducibility notes, safe log collection instructions, and release decision meeting before expanding scope.

## 3. Deferred / post-MVP work

- Production/live Paddle launch and full billing operations.
- Refunds, chargebacks, customer portal, referral/promo logic, and broader finance automation.
- Mobile releases and Apple/Google entitlement bridge.
- Full production Admin role management/RBAC and critical-change approval if not completed before MVP.
- Additional content polishing for short/repeating scenarios and avatar dialogue quality.
- Advanced monitoring dashboards and support automation beyond the controlled tester needs.
