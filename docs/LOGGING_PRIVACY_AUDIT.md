# Phase 5A Logging and Privacy Audit

Review date: 2026-06-25.

Scope: lightweight documentation/source audit only. No backend runtime behavior, Desktop behavior, billing/Paddle semantics, EF migrations, deployment scripts, external services, or heavy monitoring infrastructure were changed.

## Current production context

- Production backend at last verification: `/opt/languagevoicetutor/backend/releases/0.1.35-backend.95` through `/opt/languagevoicetutor/backend/current`; verify `/opt/languagevoicetutor/backend/previous` before rollback.
- `/health` and `/api/health/database` are healthy at last verification.
- Phase 4 backup/restore/migration rollback drills are complete for the current release-readiness level.
- Broad public production readiness is not claimed.
- Controlled Paddle live payment/webhook/Premium activation and desktop cancel-renewal validation were completed on 2026-07-02 with safe evidence only; do not expose raw Paddle payloads, webhook signatures, tokens, cookies, SQL dumps, backup contents, credentials, or full raw user data. Refund, chargeback, customer portal, and broad paid launch remain pending.

## Reviewed areas

- Backend logging configuration and tracked `appsettings` files.
- Backend application logging in auth/session/password reset, Paddle webhook, OpenAI lesson chat, STT/TTS/realtime voice, health, billing, usage, Admin, and CMS areas at a source-review level.
- Production-safe logging assumptions in release-readiness and security docs.
- Admin/CMS audit logging posture at a high level.

## Findings

### 1. Secret and raw-payload exposure risk

No obvious small source-code logging bug requiring an immediate code change was found in this pass. The reviewed logs generally use operational metadata such as ids, status codes, counts, model names, voice names, request/result categories, lengths, token counts, and error categories rather than passwords, bearer tokens, refresh tokens, JWTs, API keys, connection strings, private keys, Paddle signatures, Paddle raw payloads, OpenAI request bodies, STT/TTS raw content, or raw lesson messages.

Important boundary: Paddle webhook ingestion stores the raw provider payload and signature header server-side for ingestion/audit needs. That storage is not the same as logging, but operators must treat those database fields as sensitive support evidence. Raw Paddle payloads and signatures must not be pasted into chat, GitHub, broad admin views, docs, or support tickets.

### 2. EF/SQL production logging

Tracked default logging does not enable EF sensitive-data logging or SQL parameter logging. Development config keeps Microsoft Entity Framework Core and database command logs at `Warning`. Production must keep `EnableSensitiveDataLogging`, SQL parameter logging, and broad EF command `Information` logging disabled unless there is a short, owner-approved incident/debug window with sanitized retention and no transcript sharing.

### 3. Health check logging

Health endpoints are intentionally coarse and safe: they should report service/database health without secrets or raw data. Health checks may be noisy in access logs, but the expected payload and normal operational log shape are safe enough for controlled production use.

### 4. Admin/CMS audit logs

Admin/CMS audit logs are useful when they record actor, action, target id/type, status, timestamps, reason/change summary, and bounded before/after metadata or hashes. They must not record secrets, Authorization headers, access/refresh tokens, password reset codes, API keys, webhook secrets, connection strings, full provider payloads, or raw sensitive request bodies. CMS content audit views should keep large edited values summarized or hashed rather than broadly displaying raw payloads.

### 5. Operator paste/redaction rule

Operators may paste only bounded, non-secret operational evidence: command names, exit status, service active/healthy status, HTTP status codes, release paths, migration ids, table names, counts, timestamps, and redacted correlation/request ids.

Operators must redact or avoid pasting: `.env` contents, connection strings, passwords, JWT signing keys, bearer/access/refresh tokens, password reset codes or links, SMTP credentials, OpenAI keys, Paddle API keys/client tokens/webhook secrets/signatures/raw payloads, private keys, SQL dumps, backup contents, raw user lesson messages, raw microphone/STT/TTS content, raw OpenAI request/response bodies, and long unfiltered terminal transcripts.

### 6. Smallest practical next hardening step

Add a small production log sampling/redaction runbook/checklist: collect a short bounded journal/API sample around one health check, one login failure, one lesson request, one password-reset request, and one Paddle sandbox webhook attempt; confirm only safe metadata appears; record pass/fail without copying raw logs. Do this before adding any heavy monitoring stack.

## Phase 5 status

Phase 5A logging/privacy audit is complete, Phase 5B bounded production log sampling is complete, and Phase 5C Production logging hardening is deployed/verified and retained in backend `0.1.35-backend.49`.

Phase 5A was documentation/audit only. No code changes were made because this pass did not find an obvious dangerous logging issue that was small and safe to fix immediately.

## Intentionally deferred

- Heavy monitoring dashboards, alerting infrastructure, log shipping, SIEM, or external services.
- Production/live Paddle readiness and live billing operations.
- Log retention policy implementation beyond the operator guidance above.
- Any backend runtime behavior change, Desktop behavior change, billing/Paddle semantic change, or EF migration.

## 2026-06-24 Phase 5B sampling and Phase 5C production logging hardening

Phase 5B bounded production log sampling found a real release-readiness logging issue: normal production journal output included `Microsoft.EntityFrameworkCore.Database.Command[20101]` entries at `Information` level with full SQL command text. Sampled SQL parameters were redacted as `?`, and no raw passwords, bearer tokens, refresh-token values, connection strings, OpenAI API keys, raw Paddle payload contents, raw SQL dumps, or raw Paddle secrets were observed. This is therefore not a data breach based on the sampled evidence, but SQL command text exposed sensitive schema/field names such as password/token hash columns, webhook payload/signature columns, large CMS JSON selections, and repeated health-check `SELECT 1` noise.

Phase 5C hardens tracked Production logging configuration by setting `Microsoft.EntityFrameworkCore.Database.Command`, `Microsoft.EntityFrameworkCore.Infrastructure`, and `System.Net.Http.HttpClient` to `Warning` in `backend/EnglishVoiceTutor.Api/appsettings.Production.json`. This is a configuration-level hardening only: it does not change runtime behavior, billing/Paddle semantics, database schema, Desktop behavior, Admin UI behavior, CMS behavior, deployment scripts, package scripts, or EF migrations.

Backend `0.1.35-backend.49` retains the Phase 5C Production logging hardening first deployed in `0.1.35-backend.40` and was packaged, uploaded, deployed, restarted, and production-verified with that hardening still in place. `/opt/languagevoicetutor/backend/current` points to `/opt/languagevoicetutor/backend/releases/0.1.35-backend.49`, and `/opt/languagevoicetutor/backend/previous` points to `/opt/languagevoicetutor/backend/releases/0.1.35-backend.48`. `/health`, `/api/health/database`, and a repeat `/api/health/database` check returned `200 OK`; `languagevoicetutor-backend.service` is active and enabled. Post-deploy journal sampling over the recent verification window returned 0 lines for the bounded sensitive/EF SQL grep set: `Microsoft.EntityFrameworkCore.Database.Command`, `SELECT`, `INSERT`, `UPDATE`, `PasswordHash`, `TokenHash`, `RawPayload`, and `SignatureHeader`. This confirms ordinary production logs no longer show normal EF SQL command text after the Production logging config hardening. No EF migrations were run for this config-only backend release; no production database schema or data changed; and no business logic, Desktop, Admin UI, CMS, billing/Paddle semantics, package script, or deployment script behavior changed. Application-level warnings and errors, including billing/Paddle warnings or failures, should remain visible. Controlled Paddle live payment/Premium activation and desktop cancel-renewal validation are now completed as of 2026-07-02; refund, chargeback, customer portal, and broad public production readiness are still not claimed.

## Admin Activity privacy note

- `GET /api/admin/activity` is read-only and normalizes audit rows from `admin_actions`, `admin_role_assignment_events`, `cms_content_audit_logs`, and the production-applied `admin_auth_audit_events` source.
- Current visible Admin Activity sources include `admin_actions`, `admin_role_assignment_events`, `cms_content_audit_logs`, and `admin_auth_audit_events`; no raw provider payload table is exposed through this view.
- Visible action coverage includes `manual_premium_grant`, `manual_premium_revoke`, `free_lesson_reset` where present, `billing_cancel_renewal` where present, role assignment/revocation/admin disable/enable events from role-assignment audit rows, and CMS events where already present in `cms_content_audit_logs`.
- Admin-entered reasons/notes are shown when they are already present in those existing audit rows; the Admin note/reason column is separate from safe metadata such as `safeMetadataJson`, and no raw payload inference is used.
- The unified DTO intentionally exposes safe fields only and does not add password, cookie, JWT, API key, Authorization header, webhook raw payload, provider raw payload, raw provider event bodies, secrets, or full request-body fields.
- Migration `20260701000000_AddAdminAuthAuditEvents` was applied in production before backend `0.1.35-backend.95`, after a fresh backup and SQL review. Production Admin Activity shows verified `admin_login_success`, `admin_logout`, `admin_login_failed`, and `disabled_admin_login_denied` rows from `admin_auth_audit_events`. Session expiration persistence remains pending.
- Website/AI publish audit may still be partial where existing audit tables do not already contain those events. Controlled Paddle live payment/webhook/Premium activation and desktop cancel-renewal validation were completed on 2026-07-02; failed payment attempts did not grant Premium; automatic refund/chargeback revocation requires production verification, customer portal verification remains pending, and broad public paid launch remains pending.

## 2026-07-01 Admin actions visibility and Premium revoke audit note

- Admin Activity now includes and filters existing `admin_actions` rows with normalized actor fields where a linked persistent `admin_users` row exists for the stored actor app-user id.
- Admin Activity is visible and usable in production for `admin_actions` and `admin_role_assignment_events`, including `manual_premium_grant` and `manual_premium_revoke`; the Admin note/reason is visible where stored.
- Manual Premium Revoke remains audited with an admin-entered reason and safe metadata only. The emergency revoke action changes backend entitlement/access state only and does not alter Paddle provider history, delete payment records, or fake Paddle webhook events.
- Secrets, raw provider payloads, webhook signatures, and unredacted provider event bodies must not be exposed in Admin Activity, docs, tickets, screenshots, or pasted operational evidence.
- Admin auth audit migration `20260701000000_AddAdminAuthAuditEvents` is production-applied and visible in Admin Activity for verified `admin_login_success`, `admin_logout`, `admin_login_failed`, and `disabled_admin_login_denied` events. Session expiration audit persistence and the controlled live Paddle payment validation remain pending.

## 2026-07-01 Admin login/logout/failure audit persistence design and production status

Inspection result: do **not** persist admin login/logout/failure events into the pre-existing visible Admin Activity source tables. The approved follow-up was the dedicated `admin_auth_audit_events` schema, which is now production-applied for verified `admin_login_success` and `admin_logout` events.

Current admin login/session flow:

- App-user credential login happens in `AuthEndpoints.LoginAsync` through `IAuthService.LoginAsync` after the request email/password presence check. Missing credentials return `400`; invalid credentials return `401` and only write an application log message without a persistent audit row.
- The Admin shell cookie is created in `AuthEndpoints.LoginAsync` only after app login succeeds and either bootstrap admin access is detected or persistent Admin RBAC grants `admin.self.read`. The cookie uses the `AdminShellCookie` scheme, is non-persistent, cannot refresh, and expires at the app auth response expiry.
- Persistent Admin shell access is checked first by linked app-user id and then, if the linked AdminUser is not disabled and the email is present, by normalized email. Disabled AdminUsers are rejected by the role-assignment read service when `DisabledAtUtc` is set or status is not `active`; that returns no Admin shell access and `AuthEndpoints.LoginAsync` signs out the Admin shell cookie instead of creating one.
- Admin logout/session deletion happens in `AdminEndpoints.DeleteAdminSessionAsync`, which signs out the `AdminShellCookie` scheme and returns `204`. The Admin UI calls `DELETE /api/admin/session` from `logoutAdminSession` and then clears local Admin UI state.
- Session expiration is currently cookie/auth-ticket expiration only; it is not a persistent event, and the Admin UI treats invalid/expired session responses as local session reset.

Current persistent logging coverage after production verification:

| Event | Persisted in Admin Activity? | Current behavior |
| --- | --- | --- |
| successful admin login | Yes, production-verified | `admin_auth_audit_events` appears in Admin Activity with `admin_login_success`. |
| failed app credential login | Yes, production-verified | `admin_auth_audit_events` appears in Admin Activity with `admin_login_failed`. |
| disabled AdminUser login attempt | Yes, production-verified | `admin_auth_audit_events` appears in Admin Activity with `disabled_admin_login_denied`. |
| explicit admin logout | Yes, production-verified | `admin_auth_audit_events` appears in Admin Activity with `admin_logout`. |
| session expiration | No | Cookie expiry / invalid-session handling only; no durable audit row completion is claimed. |

Existing table fit:

- `admin_actions` is not a safe fit. It is target-app-user/action audit with required `TargetUserId`, required non-empty `Reason`, and foreign keys to app users. Login failures can involve no known user/admin identity, and logout is actor/session activity rather than an action taken against a target user.
- `admin_role_assignment_events` is not a safe fit. It is role-management audit with required `TargetAdminUserId`, role-change fields, and role-assignment semantics. Login/logout/failure events are not role assignment events; forcing them here would pollute RBAC audit and still would not represent unknown failed attempts safely.
- `cms_content_audit_logs` is not a safe fit. It is CMS content audit with entity/content fields and CMS action/status semantics, not authentication/session audit.

Migration decision: the approved durable, queryable, privacy-safe Admin Activity coverage uses the dedicated authentication/session audit table `admin_auth_audit_events`, with only safe fields:

- `id` GUID primary key.
- `occurred_at_utc` timestamp.
- `event_type` string: `admin_login_success`, `admin_logout`, `admin_login_failed`, `disabled_admin_login_denied`.
- `result` string: `succeeded`, `failed`, or `denied`.
- Nullable `actor_user_id` for the linked app user when app authentication succeeded.
- Nullable `actor_admin_user_id` for the persistent AdminUser when resolved safely.
- Nullable normalized `actor_email` or `attempted_normalized_email`, stored only as the email string submitted/known for the login attempt after trimming/normalization; do not store passwords or raw request bodies.
- Nullable safe role context such as `role_ids_json` only after app authentication succeeds and roles are resolved from existing Admin RBAC data; do not store cookies, JWTs, authorization headers, raw claims, or full request bodies.
- Nullable `safe_metadata_json` for bounded non-secret context such as `admin_shell_cookie_issued`, `denial_reason`, or `auth_stage`; never store cookies, JWTs, Authorization headers, Paddle secrets, OpenAI keys, raw provider payloads, raw request bodies, or full provider payloads.

Implemented/remaining safe slice status:

1. Dedicated table and EF entity/configuration/migration: production-applied as `20260701000000_AddAdminAuthAuditEvents`.
2. `admin_login_success`: production-verified in Admin Activity.
3. `disabled_admin_login_denied`: production-verified in Admin Activity.
4. `admin_login_failed`: production-verified in Admin Activity.
5. `admin_logout`: production-verified in Admin Activity.
6. Include the new source in Admin Activity as read-only, with filters for actor user/admin user/action/result/time and no secret-bearing fields.
7. Add tests for success, failed credential attempt, disabled-admin denial, explicit logout, Admin Activity projection/filtering, and privacy assertions that password/cookie/JWT/Authorization/request-body/provider secrets are not persisted.

Until that schema is approved, keep Admin Activity read-only over the current source tables and do not force auth/session events into tables whose required keys and semantics do not fit.

## Approved Admin auth audit persistence slice (2026-07-01)

- The approved dedicated Admin auth audit source now exists locally as migration `20260701000000_AddAdminAuthAuditEvents`, creating `admin_auth_audit_events`.
- Production deployment/application of this migration is not complete until explicitly performed through the approved production migration process.
- Admin Activity will include `admin_auth_audit_events` after deployment/migration as a read-only source and supports the `source=admin_auth_audit_events` filter.
- Persisted event types are limited to `admin_login_success`, `admin_login_failed`, `disabled_admin_login_denied`, and `admin_logout` in this slice.
- The table intentionally stores only safe fields: ids, timestamps, event/result codes, normalized emails where applicable, source/role ids JSON, failure reason code, and safe metadata JSON. Passwords, cookies, JWTs, Authorization headers, Paddle/OpenAI secrets, raw request bodies, full provider payloads, and raw claims are not stored.
- Session expiration persistence remains pending because this slice did not add a clean, low-noise expiration event write path.
- Controlled Paddle live payment/webhook/Premium activation and desktop cancel-renewal validation were completed on 2026-07-02; failed payment attempts did not grant Premium; automatic refund/chargeback revocation requires production verification, customer portal verification remains pending, and broad public paid launch remains pending.

## 2026-07-02 refund and chargeback Premium protection

In candidate backend code that is not yet production-deployed, full Paddle refunds are treated as access-control events after `adjustment.created` or `adjustment.updated` webhook processing: the backend preserves Paddle/payment/subscription history, maps the adjustment back to the internal user by safe metadata or existing payment/subscription records, and immediately expires active provider-event Premium entitlements with reason `paddle_full_refund`. Chargebacks are implemented as stronger refund evidence and immediately expire active provider-event Premium entitlements with reason `paddle_chargeback` in that candidate code.

Normal cancel-renewal behavior is unchanged: scheduled cancellation keeps Premium through the paid period end. Partial refunds are conservative in this slice: the event is safely recorded/processed for review and Premium is left unchanged unless the adjustment is full or a chargeback. Provider history is preserved; payment and subscription records are not deleted, and refund processing does not fake Paddle webhook events or expose raw provider payloads, webhook signatures, tokens, cookies, secrets, API keys, or full card/payment data in Admin Activity evidence.

Automatic refund/chargeback revocation requires production verification after the candidate backend is deployed; current production backend is `0.1.35-backend.96`; production `.96` received `adjustment.created` and `adjustment.updated` but blocked them in reconciliation before automatic Premium revocation. Broad public paid launch remains pending. Customer portal verification remains pending. Direct installer code signing remains pending.


### 2026-07-02 production refund replay blocker and fix candidate

Production backend `0.1.35-backend.96` is deployed and healthy. Paddle has `adjustment.created` and `adjustment.updated` enabled and delivered. A controlled full-refund notification reached the backend with provider transaction/subscription ids but no `InternalUserId`; normalization and payment persistence completed, but reconciliation blocked the event before entitlement revocation. Therefore automatic refund/chargeback Premium revocation is not production-verified yet.

Fix candidate: for full-refund/chargeback adjustment events, use safe metadata first, then resolve the backend user from existing Paddle payment history by provider transaction id, then existing subscription history by provider subscription id, then active provider-event entitlement evidence where already linked. If no safe mapping exists, block with a safe reason. Manual Premium Revoke remains the operational stopgap until the next backend is deployed and Paddle Replay verification passes. No more live payment/refund tests should be run until fake-signed/local verification passes. Broad public paid launch remains pending. Customer portal remains pending unless separately verified. Direct installer code signing remains pending.

Production verification after deployment must not create another live payment or refund: replay the already delivered Paddle `adjustment.updated` notification from the Paddle notification log, then refresh Desktop/Admin subscription status and confirm Premium is removed.
