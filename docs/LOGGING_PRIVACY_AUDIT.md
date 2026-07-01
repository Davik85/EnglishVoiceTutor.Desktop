# Phase 5A Logging and Privacy Audit

Review date: 2026-06-25.

Scope: lightweight documentation/source audit only. No backend runtime behavior, Desktop behavior, billing/Paddle semantics, EF migrations, deployment scripts, external services, or heavy monitoring infrastructure were changed.

## Current production context

- Production backend at last verification: `/opt/languagevoicetutor/backend/releases/0.1.35-backend.49` through `/opt/languagevoicetutor/backend/current`, with rollback reference `/opt/languagevoicetutor/backend/releases/0.1.35-backend.48`.
- `/health` and `/api/health/database` are healthy at last verification.
- Phase 4 backup/restore/migration rollback drills are complete for the current release-readiness level.
- Broad public production readiness is not claimed.
- Production/live Paddle readiness remains deferred.

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

Backend `0.1.35-backend.49` retains the Phase 5C Production logging hardening first deployed in `0.1.35-backend.40` and was packaged, uploaded, deployed, restarted, and production-verified with that hardening still in place. `/opt/languagevoicetutor/backend/current` points to `/opt/languagevoicetutor/backend/releases/0.1.35-backend.49`, and `/opt/languagevoicetutor/backend/previous` points to `/opt/languagevoicetutor/backend/releases/0.1.35-backend.48`. `/health`, `/api/health/database`, and a repeat `/api/health/database` check returned `200 OK`; `languagevoicetutor-backend.service` is active and enabled. Post-deploy journal sampling over the recent verification window returned 0 lines for the bounded sensitive/EF SQL grep set: `Microsoft.EntityFrameworkCore.Database.Command`, `SELECT`, `INSERT`, `UPDATE`, `PasswordHash`, `TokenHash`, `RawPayload`, and `SignatureHeader`. This confirms ordinary production logs no longer show normal EF SQL command text after the Production logging config hardening. No EF migrations were run for this config-only backend release; no production database schema or data changed; and no business logic, Desktop, Admin UI, CMS, billing/Paddle semantics, package script, or deployment script behavior changed. Application-level warnings and errors, including billing/Paddle warnings or failures, should remain visible. Production/live Paddle readiness remains deferred, and broad public production readiness is still not claimed.

## Admin Activity privacy note

- `GET /api/admin/activity` is read-only and normalizes only existing audit rows from `admin_actions`, `admin_role_assignment_events`, and `cms_content_audit_logs`.
- Admin-entered reasons/notes are shown when they are already present in those existing audit rows; safe metadata remains separate and no raw payload inference is used.
- The unified DTO intentionally exposes safe fields only and does not add password, cookie, JWT, API key, Authorization header, webhook raw payload, provider raw payload, or full request-body fields.
- No migration was added. Login/logout/failure audit persistence remains pending until a unified audit table or explicit approved schema change is available.
- Website/AI publish audit may still be partial where existing audit tables do not already contain those events. Paddle live payment test remains pending.

## 2026-07-01 Admin actions visibility and Premium revoke audit note

- Admin Activity now includes and filters existing `admin_actions` rows with normalized actor fields where a linked persistent `admin_users` row exists for the stored actor app-user id.
- Manual Premium Revoke remains audited with an admin-entered reason and safe metadata only. The emergency revoke action changes backend entitlement/access state only and does not alter Paddle provider history or delete payment records.
- No migration was added. Login/logout/failure audit persistence and the live Paddle payment test remain pending.
