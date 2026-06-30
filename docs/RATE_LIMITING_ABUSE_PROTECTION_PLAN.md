# Phase 3: Rate Limiting / Abuse Protection Completion Record

Review date: 2026-06-23.

This document started as the Phase 3 implementation plan and now records the completed post-Phase-3 production state. Phase 3 rate limiting / abuse protection is implemented and production-verified on backend `0.1.35-backend.39` with `RateLimiting__Enabled=true`. Do not change Admin RBAC behavior as part of Phase 3. Current Production Admin RBAC final state: after the successful 2026-06-22 controlled rehearsal, the later permanent fallback disable also passed on 2026-06-22. BootstrapAdmin fallback for `AdminPermission:*` policies is explicitly disabled through production `backend.env`, persistent role authorization is enabled and verified, two persistent `super_admin` accounts are verified, and both approved accounts passed validation after permanent fallback disable. Phase 3 rate limiting work must not change that Admin RBAC behavior; rollback remains an operational fallback action by setting `AdminAuthorization__EnableBootstrapAdminFallbackForAdminPermissionPolicies=true` and restarting the backend.

## Production completion summary — 2026-06-23

Phase 3 is complete at the single-instance/in-memory limiter level. The verified active backend is `/opt/languagevoicetutor/backend/releases/0.1.35-backend.39`, `/opt/languagevoicetutor/backend/current` points to `0.1.35-backend.39`, and the previous rollback reference is `/opt/languagevoicetutor/backend/releases/0.1.35-backend.38`. Production has `RateLimiting__Enabled=true` and `AdminAuthorization__EnableBootstrapAdminFallbackForAdminPermissionPolicies=false`.

Production checks passed: `/health` returned `200 OK`, `/api/health/database` returned `200 OK`, Admin RBAC smoke passed with `fallbackEnabled=False`, `persistentRoleAuthorizationEnabled=True`, and `actorMappingFound=True`, Admin UI manual check passed, Desktop app manual check passed, and Billing UI manual check passed. Paid users still see the payment action inactive; non-premium users still see payment action active.

Implemented slices cover auth login/register/password reset; audio transcription/STT, TTS speech and speech stream, translation, realtime voice start-rate protection; Admin read/write/role-management throttling; billing checkout/cancel-renewal, Paddle checkout launch, Paddle webhook throttling; and final-slice auth refresh/revoke/current-user/password-change, authenticated lesson start, lesson hint, lesson feedback, authenticated persisted lesson messages, and learner/subscription/status/trial/access-style endpoints where implemented.

Phase 3 did not change Admin RBAC authorization behavior, product/free usage semantics, Premium entitlement semantics, billing/Paddle semantics, Paddle webhook signature verification, provider-event handling, Desktop behavior, Admin UI behavior, CMS runtime behavior, package/upload scripts, deployment scripts, or EF migrations. No database migrations were added or run for Phase 3.

Deferred after Phase 3: true distributed/shared rate limiter storage, true concurrent realtime voice WebSocket connection caps if not implemented, backups/restore/migration rollback drills as Phase 4, monitoring/logging/privacy hardening, Paddle live readiness/legal/support blockers, and broad public-production readiness. Microsoft Store/MSIX remains discontinued for now.


## 1. Current state

### What protection already exists

- Authentication endpoints validate required email/password fields, enforce minimum password length on registration and password reset confirmation, and return simple `{ error = ... }` or password-reset response messages for known failures.
- Auth login returns `401 Unauthorized` for invalid credentials without exposing which part failed.
- Password reset request returns an accepted-style message when delivery is available, which helps avoid direct account enumeration.
- Auth refresh uses persisted refresh tokens and returns `401 Unauthorized` when refresh is rejected.
- Lesson session start is already protected by product access logic. Free/trial/Premium access decisions can return `403 Forbidden` for lesson access denial.
- Only one active lesson session is allowed per resolved user. A second lesson start can return `409 Conflict` with safe active-session metadata.
- Lesson chat reply, hint, transcription, and TTS call existing free-usage guard checks and can return `429 Too Many Requests` when product free usage limits are exceeded.
- Lesson chat, hint, feedback, transcription, translation, and TTS can verify that a supplied backend lesson session is still active.
- Lesson length/turn behavior exists through lesson soft/hard turn limits and CMS/runtime lesson behavior. This constrains normal lesson shape but is not request throttling.
- Admin endpoints require Admin permission policies. Role-management endpoints require the role-management permission policy. This is authorization, not request throttling.
- Paddle webhook handling requires the webhook feature to be enabled, requires a configured secret key, requires the `Paddle-Signature` header, verifies the signature, records duplicate events, and logs provider event IDs rather than raw payloads.
- Existing logging mostly uses structured summaries: route names, operation names, result labels, IDs, counts, lengths, status, and safe exception metadata.

### What remains after Phase 3

- True distributed/shared rate-limit storage remains future work for multi-instance deployments.
- True concurrent realtime voice WebSocket connection caps remain future work if not implemented; Phase 3 verified start-rate protection.
- Backups/restore/migration rollback drills are Phase 4 operational work, not Phase 3.
- Monitoring/logging/privacy hardening, Paddle live readiness/legal/support blockers, and broad public-production readiness remain deferred. Microsoft Store/MSIX remains discontinued for now.
- Request-size abuse policy beyond existing request/form validation and hosting defaults remains a future hardening topic.

### Product limits are not technical rate limits

Product limits and technical abuse protection must stay separate:

- Product limits decide what a learner is entitled to use, such as free lesson daily usage, free chat replies, hints, transcription, TTS, Premium access, and lesson length.
- Technical rate limiting protects the service from bursts, credential attacks, scraping, runaway clients, expensive AI loops, provider abuse, and accidental retry storms.
- Existing product limits such as free lesson daily usage and lesson length are not the same as request rate limiting. A Premium user may have broader product access but should still have safety caps to prevent runaway or abusive request volume.

## 2. Protected surfaces

The first implementation should classify endpoints by surface, then apply named policies. Suggested surfaces:

- **Auth login:** `POST /api/auth/login`.
- **Registration:** `POST /api/auth/register`.
- **Password reset/account recovery:** `POST /api/auth/password-reset/request` and `POST /api/auth/password-reset/confirm`.
- **Auth refresh/current user:** `POST /api/auth/refresh`, `POST /api/auth/revoke`, `GET /api/auth/me`, and `POST /api/auth/password/change`.
- **Lesson start:** authenticated `POST /api/me/lesson-sessions`; development `POST /api/dev/lesson-sessions` should stay development-only/operator-safe.
- **Lesson chat/reply:** `POST /api/lesson-chat/reply` and `POST /api/lesson-chat/mock-reply`.
- **Lesson hint/feedback:** `POST /api/lesson-chat/hint` and `POST /api/lesson-chat/feedback`.
- **Persisted lesson messages:** authenticated `POST /api/me/lesson-sessions/{sessionId}/messages`; development `POST /api/dev/lesson-sessions/{sessionId}/messages`.
- **Voice recording upload / STT:** `POST /api/audio/transcribe`.
- **TTS / bot voice:** `POST /api/audio/speech` and `POST /api/audio/speech-stream`.
- **Realtime voice:** WebSocket route `/api/realtime-voice`.
- **Translation:** `POST /api/translate`.
- **Free usage consumption:** free limit checks called by lesson chat reply, hint, transcription, and TTS; lesson start access decisions; daily usage counters.
- **Premium abuse safety caps:** authenticated learner AI and audio endpoints even when Premium is active.
- **Admin endpoints:** all `/api/admin/*` endpoints, with stricter treatment for write actions.
- **Admin role-management endpoints:** `/api/admin/role-assignments/*`, especially assign/revoke/enable/disable/provision/bootstrap.
- **Billing endpoints:** `POST /api/me/billing/checkout-session`, `POST /api/me/billing/subscription/cancel`, and `GET /checkout/paddle`.
- **Paddle/webhook/provider endpoints:** `POST /api/billing/webhooks/paddle`.
- **Health/config/status endpoints:** `/health`, `/api/health`, `/api/health/database`, `/api/backend/config-status`, CMS runtime status, subscription status, and lesson access/status endpoints. These should have light global/IP protection if exposed, but should not interfere with uptime checks.

## 3. Proposed first implementation slice

Start with the smallest safe slice:

1. Auth technical throttling for `POST /api/auth/login`, `POST /api/auth/register`, `POST /api/auth/password-reset/request`, and `POST /api/auth/password-reset/confirm`.
2. Learner AI request throttling for `POST /api/lesson-chat/reply`.

Why this slice first:

- Login, registration, and password reset are the highest-risk public surfaces for credential stuffing, account enumeration pressure, and email-delivery abuse.
- Lesson chat/reply is a high-cost AI surface and is likely to be used frequently in normal lessons, so it proves the policy can protect expensive calls without blocking real learners.
- This slice avoids Admin RBAC behavior changes, billing behavior changes, migrations, Desktop changes, and server configuration changes.
- It can be implemented with named ASP.NET Core rate-limiter policies and local in-memory counters first.
- It creates the reusable response/logging/config pattern for later audio, TTS, admin, and billing policies.

## 4. Suggested limit policy

Use `429 Too Many Requests` for technical throttling. Include `Retry-After` when possible. Use a simple user-facing response shape aligned with existing API style:

```json
{
  "error": "RateLimitExceeded",
  "message": "Too many requests. Please wait a moment and try again.",
  "retryAfterSeconds": 60
}
```

Do not expose internal bucket names, exact counter state, account existence, token validity, provider secrets, or raw request details.

| Surface | Key | Rough limit | Window | Response | User-facing message | Logging/audit behavior | Premium behavior |
| --- | --- | ---: | --- | --- | --- | --- | --- |
| Auth login | Per IP + per normalized email | 10 per IP and 5 per email | 5 minutes | `429` | `Too many login attempts. Please wait a few minutes and try again.` | Log safe summary: route, result, IP hash/prefix if available, normalized email hash, retry-after. Do not log password. | No Premium difference. |
| Registration | Per IP + per normalized email | 5 per IP and 3 per email | 15 minutes | `429` | `Too many registration attempts. Please wait and try again.` | Log safe summary and duplicate/created result only. Do not log password. | No Premium difference. |
| Password reset request | Per IP + per normalized email | 5 per IP and 3 per email | 15 minutes | `429` | `Too many password reset requests. Please wait before trying again.` | Log accepted/throttled/delivery-unavailable summary. Do not reveal account existence. | No Premium difference. |
| Password reset confirm | Per IP + per normalized email if present | 10 per IP and 5 per email | 15 minutes | `429` | `Too many reset attempts. Please wait and try again.` | Log result only. Do not log reset token or password. | No Premium difference. |
| Auth refresh/revoke | Per user when authenticated/known token subject is available; otherwise per IP | 60 per user or 30 per IP | 5 minutes | `429` | `Too many session requests. Please wait and try again.` | Log route and result. Do not log tokens. | No Premium difference. |
| Auth me/current user | Per user + light per IP | 120 per user | 5 minutes | `429` | `Too many account status requests. Please wait and try again.` | Low-noise log only when throttled. | No Premium difference. |
| Lesson start | Per user + per IP fallback | 10 per user | 10 minutes | `429` | `Too many lesson start attempts. Please wait a moment and try again.` | Log user id, source, decision, active-session conflicts separately. | Premium can start more lessons by product entitlement, but same technical burst cap. |
| Lesson chat/reply | Per user + per lesson session + per IP fallback | 30 per user and 20 per session | 10 minutes | `429` | `You are sending messages too quickly. Please wait a moment and continue the lesson.` | Log route, user id, session id, lesson scenario id, target language id, tutor profile id, retry-after. Do not log message body. | Premium gets product access, not unlimited request bursts. Consider higher cap later only if normal lessons need it. |
| Lesson hint | Per user + per lesson session | 20 per user | 10 minutes | `429` | `Too many hint requests. Please wait a moment and try again.` | Log safe route/session summary. Do not log lesson content. | Product limits may differ, but keep a technical safety cap. |
| Lesson feedback | Per user + per lesson session | 20 per user | 10 minutes | `429` | `Too many feedback requests. Please wait a moment and try again.` | Log source kind and text length only. Do not log corrected content as part of throttling. | Same safety cap at first. |
| Persisted lesson messages | Per user + per session | 40 per session | 10 minutes | `429` | `Too many lesson messages. Please wait a moment and try again.` | Log session id and role only. Do not log text. | Same safety cap. |
| Audio upload / transcription | Per user + per IP fallback | 20 per user | 10 minutes | `429` | `Too many recordings. Please wait a moment before recording again.` | Log file length, language, duration if known, and result. Do not log audio content or transcript in throttle logs. | Premium can have product access, but keep safety cap; tune after observing normal voice lessons. |
| TTS / bot voice | Per user + per session | 60 per user | 10 minutes | `429` | `Voice playback is being requested too quickly. Please wait a moment and try again.` | Log input length, voice, purpose, bytes/duration metrics. Do not log text content in throttle logs. | Same safety cap initially. |
| Realtime voice WebSocket | Per IP + per user when authenticated/session known | 3 concurrent connections and 10 starts | 10 minutes | `429` or close before accept | `Too many voice sessions. Please close another session or wait a moment.` | Log connection accepted/rejected, route, user/session if known, duration. Do not log audio. | Same safety cap. |
| Translation | Per user + per IP fallback | 30 per user | 10 minutes | `429` | `Too many translation requests. Please wait a moment and try again.` | Log input length and language only. Do not log text. | Same safety cap. |
| Free usage consumption | Existing product limit key by user/date/study language | Keep existing product limits | Daily/product-defined | Existing product responses, often `429` for free usage exhaustion | Existing free-limit copy | Keep existing usage-event/counter logging. Do not merge this with technical throttling counters. | Premium bypasses free product limits but not technical safety caps. |
| Premium abuse safety caps | Per user + per endpoint family | Higher than normal lesson need; start with chat 120/hour, audio 120/hour | 1 hour | `429` | `Usage is temporarily too high. Please wait and try again.` | Log safe aggregate summary; consider future audit event only for repeated abuse. | Applies to Premium as safety cap. Do not market as product limit. |
| Admin read endpoints | Per admin user + per IP | 120 per admin | 5 minutes | `429` | `Too many admin requests. Please wait and try again.` | Log admin user id, endpoint group, permission policy, result. Do not log cookies/tokens. | Not applicable. |
| Admin write endpoints | Per admin user + endpoint group | 30 per admin | 10 minutes | `429` | `Too many admin changes. Please wait and try again.` | Log admin user id, action, target id if safe, result. Keep existing audit behavior unchanged. | Not applicable. |
| Admin role-management | Per admin user + per IP | 10 per admin | 10 minutes | `429` | `Too many role-management attempts. Please wait and try again.` | Log and audit safe action metadata. Do not change authorization semantics. | Not applicable. |
| Billing checkout/cancel | Per user + per IP | 10 per user | 10 minutes | `429` | `Too many billing requests. Please wait and try again.` | Log request type and provider result. Do not log provider secrets or full provider payloads. | No Premium difference. |
| Paddle checkout launch | Per IP + transaction id if available | 30 per IP | 10 minutes | `429` | `Too many checkout launch requests. Please wait and try again.` | Log transaction id if safe and result. Do not log client-side token. | No Premium difference. |
| Paddle webhook | Provider event id for idempotency + per IP/provider source safety | 300 per IP/source | 5 minutes | `429` only for clear floods; otherwise preserve valid provider delivery | `Too many provider requests.` | Prefer signature verification and idempotency. Log provider event id, verification result, duplicate flag, counts. Do not log raw payload. | Not applicable. |
| Health/status/config | Per IP | 300 per IP | 5 minutes | `429` | `Too many status requests. Please wait and try again.` | Low-noise throttle-only logging. Avoid breaking uptime checks. | Not applicable. |

## 5. Storage approach

First implementation can use in-memory rate-limit storage because the first goal is safe single-instance protection and behavior validation.

Recommended first step:

- Use built-in ASP.NET Core rate limiting with named policies.
- Store counters in process memory.
- Keep policy names and defaults configurable.
- Add no packages unless built-in framework support is unavailable in the current target framework.
- Add no migrations.

Known limitations of in-memory storage:

- Counters reset on backend restart.
- Counters are not shared across multiple backend instances.
- A multi-instance deployment can allow the limit once per instance unless traffic is sticky or a shared store is added.

Later multi-instance/server scale change:

- Move counters to a shared store such as Redis or another production-approved distributed cache.
- Keep the same policy names and response shape.
- Keep product usage counters in the database separate from technical rate-limit counters.
- Consider edge/proxy limits for coarse global IP floods only after app-level behavior is proven.

## 6. Configuration

Configuration now lives under the `RateLimiting` section. Keep all values adjustable through `appsettings.json` and environment variables. Do not include secrets.

Suggested names and defaults:

```json
{
  "RateLimiting": {
    "Enabled": false,
    "LogThrottledRequests": true,
    "DefaultRetryAfterSeconds": 60,
    "Auth": {
      "LoginPerIpLimit": 10,
      "LoginPerEmailLimit": 5,
      "LoginWindowMinutes": 5,
      "RegisterPerIpLimit": 5,
      "RegisterPerEmailLimit": 3,
      "RegisterWindowMinutes": 15,
      "PasswordResetPerIpLimit": 5,
      "PasswordResetPerEmailLimit": 3,
      "PasswordResetWindowMinutes": 15
    },
    "Lessons": {
      "StartPerUserLimit": 10,
      "StartWindowMinutes": 10,
      "ChatReplyPerUserLimit": 30,
      "ChatReplyPerSessionLimit": 20,
      "ChatReplyWindowMinutes": 10
    },
    "Audio": {
      "TranscriptionPerUserLimit": 20,
      "TtsPerUserLimit": 60,
      "AudioWindowMinutes": 10,
      "RealtimeVoiceConcurrentPerIpLimit": 3
    },
    "Admin": {
      "ReadPerAdminLimit": 120,
      "WritePerAdminLimit": 30,
      "RoleManagementPerAdminLimit": 10,
      "WindowMinutes": 10
    },
    "Billing": {
      "CheckoutPerUserLimit": 10,
      "WebhookPerIpLimit": 300,
      "WindowMinutes": 10
    }
  }
}
```

Production note: production now intentionally has `RateLimiting__Enabled=true` after Phase 3 verification. Local/development defaults may remain disabled unless a developer intentionally enables throttle testing.

Environment variable examples for later deployment:

- `RateLimiting__Enabled=true`
- `RateLimiting__Auth__LoginPerIpLimit=10`
- `RateLimiting__Lessons__ChatReplyPerUserLimit=30`

## 7. User experience

Rate limiting must not block legitimate learners during normal lessons.

Guidelines:

- Use generous chat windows for normal lesson pace. A learner sending 10 to 15 messages over a lesson should never hit the technical limiter.
- Use per-session chat limits to stop loops, but keep them above normal lesson length. Current lesson hard limits can reach extended lesson sizes, so technical chat caps must be higher than expected lesson turns.
- Avoid throttling normal TTS playback too aggressively because a single learner message can trigger bot text and voice playback.
- Avoid counting failed network retries too harshly for audio upload if the client retries after timeout/cancel.
- Return clear wait-and-retry messages, not generic server errors.
- Include `Retry-After` where possible so Desktop can later show friendlier wait states without guessing.
- Do not change Desktop behavior in the first documentation or backend implementation task.
- Premium should not bypass technical safety caps entirely. If Premium needs different behavior later, raise caps carefully based on observed normal usage rather than making it unlimited.

## 8. Security/privacy

Do not log:

- Passwords.
- Password reset tokens.
- Access tokens or refresh tokens.
- Cookies.
- Raw provider payloads.
- Raw connection strings.
- Private keys or signing keys.
- Full request bodies.
- Raw audio.
- Sensitive lesson content where avoidable.

Safe logging examples:

- Route or endpoint group.
- User id when authenticated.
- Admin user id for admin actions.
- Provider event id after signature verification/ingestion.
- Lesson session id.
- Lesson scenario id.
- Target language id/code.
- Tutor profile id.
- Request text length, not text.
- Audio file length/duration, not audio.
- Result code, throttle policy name, retry-after seconds.
- Hash or coarse prefix of IP/email only if needed for diagnostics and allowed by privacy policy.

Avoid turning rate-limit logs into a source of sensitive lesson or account data. Use warning-level logs only for throttled or suspicious events; keep normal accepted requests low-noise.

## 9. Tests and smoke checks

Do not implement tests in this planning task. When implementing the first slice, add focused coverage for auth and lesson chat/reply throttling.

Suggested automated tests for first implementation slice:

- Login below limit returns existing success/unauthorized behavior.
- Login over per-IP limit returns `429` with the standard rate-limit response.
- Login over per-email limit returns `429` without revealing account existence.
- Registration below limit keeps existing validation/created/conflict behavior.
- Registration over limit returns `429`.
- Password reset request over limit returns `429` and does not reveal whether the email exists.
- Password reset confirm over limit returns `429` and never logs token/password values.
- Lesson chat/reply below limit keeps existing `200`, free-limit `429`, session-ended `409`, and provider-error behavior.
- Lesson chat/reply over technical limit returns `429` before the expensive provider call.
- Existing product free-limit exhaustion remains separate from technical throttling.
- Response includes `Retry-After` or `retryAfterSeconds` when available.
- Rate-limited logs contain safe metadata only.

Suggested manual smoke checklist after first implementation slice:

1. Start backend with rate limiting disabled and confirm existing login/register/password-reset/chat behavior is unchanged.
2. Enable rate limiting locally with low test-only limits.
3. Call `POST /api/auth/login` repeatedly with a test email and confirm `429` after the configured limit.
4. Confirm the login response does not expose whether the test email exists.
5. Call `POST /api/auth/password-reset/request` repeatedly and confirm `429` without account enumeration.
6. Start a normal authenticated lesson and send several learner chat messages below the configured normal limit.
7. Lower chat limit in local config and confirm `POST /api/lesson-chat/reply` returns `429` before provider work.
8. Confirm free usage/product-limit responses are still distinct from technical rate-limit responses.
9. Review logs and confirm no passwords, tokens, cookies, raw lesson messages, raw transcripts, or raw request bodies appear.
10. Disable rate limiting and confirm existing behavior returns.

Useful existing documentation to reference while implementing:

- `docs/LESSON_SESSIONS_ENDPOINTS.md` for lesson session behavior.
- `docs/LESSON_MESSAGES_ENDPOINTS.md` for persisted lesson message behavior.
- `docs/DAILY_USAGE_COUNTERS.md` and `docs/USAGE_EVENTS.md` for product usage counters.
- `docs/DESKTOP_BACKEND_ROUTE_SMOKE.md` and `docs/MANUAL_TEST_CHECKLIST.md` for smoke patterns.
- `docs/PRODUCTION_ADMIN_RBAC_READINESS.md` and `docs/ADMIN_RBAC_CUTOVER_RUNBOOK.md` for Admin RBAC context that must not be changed by this phase.

## 10. Post-Phase-3 next work

Phase 3 endpoint coverage is complete and production-verified for the current single-instance deployment. Do not open more Phase 3 endpoint-coverage tasks unless a new uncovered route is explicitly identified.

Next work is Phase 4 operational readiness:

1. Run and record a production-safe backup/readability/restore drill using `docs/BACKUP_RESTORE_DRILL_RUNBOOK.md` and `tools/db_backup_restore_drill_commands.ps1`.
2. Keep generated backup files, SQL dumps, connection strings, `.env` contents, secrets, provider payloads, Paddle signatures, and raw user data out of git and shared transcripts.
3. Design distributed/shared rate limiter storage before any multi-instance backend scale-out.
4. Add true concurrent realtime voice WebSocket connection caps if the current backend only protects start rate.
5. Continue monitoring/logging/privacy hardening, Paddle live readiness/legal/support work, and keep Microsoft Store/MSIX discontinued unless a separate future product decision reopens it before any broad public-production readiness claim.

## Historical implementation note

This document preserves the original planning tables above for policy context, but the current state is no longer “first slice only.” Backend `0.1.35-backend.39` is active in production with `RateLimiting__Enabled=true`, all Phase 3 slices listed in the completion summary are implemented, and no Phase 3 database migrations were added or run. Admin RBAC authorization behavior, product/free usage semantics, Premium entitlement semantics, billing/Paddle semantics, Paddle webhook signature verification, provider-event handling, Desktop behavior, Admin UI behavior, CMS runtime behavior, package/upload scripts, and deployment scripts remain unchanged by Phase 3.
