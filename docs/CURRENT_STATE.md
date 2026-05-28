# Current State

Review date: 2026-05-28.

## Short summary

EnglishVoiceTutor currently has a working Windows desktop MVP backed by a working backend, PostgreSQL, and EF Core persistence foundation. Lesson Chat, account login, trial entitlement, free lesson access checks, local Development admin support, and the provider-agnostic billing foundation are implemented and validated. Paddle is the current desktop/web provider adapter, but backend account, subscription, entitlement, usage, limits, lesson history, payment, and Premium/free status remain the source of truth.

## Product architecture principle

- Product context remains global, cross-platform, and provider-agnostic.
- The backend is the source of truth for account, trial, subscription, Premium/free status, usage, limits, lesson history, payments, and entitlements.
- Desktop and future mobile clients must rely on backend account/subscription/entitlement state, not local payment assumptions.
- Paddle is the current desktop/web billing provider adapter.
- Core backend subscription and entitlement logic must remain provider-agnostic.
- Realtime code remains in the repository as future capability, but it is not the default MVP Conversation Mode path.

## Desktop/backend MVP status

**Implemented and validated**

- Windows desktop app builds.
- Backend builds.
- PostgreSQL + EF Core persistence foundation works.
- Lesson content audit passes.
- Lesson Chat works.
- Text input works.
- Send by Enter works.
- Voice recording works.
- Transcription works.
- Bot replies work.
- Hint works.
- Feedback works.
- Summary works.
- Translate works.
- Conversation Mode works through the stable TTS pipeline.

## Study language status

Supported study languages:

- English
- French
- German
- Portuguese
- Spanish
- Italian

Study language is the language of lessons, hints, feedback, summary, transcription, and TTS. It is not the UI language.

## Auth/account/trial status

**Implemented and validated**

- Auth/JWT backend foundation is implemented and validated.
- Optional desktop Account UI exists in Settings.
- Register/Login/Logout work.
- `auth-session.json` is created after login/register and removed after logout.
- Settings is auth-aware:
  - signed in -> `/api/me/settings`
  - signed out -> dev settings fallback.
- Normal lesson start requires sign-in.
- Signed-out users cannot start normal lessons from the desktop UI.
- Registration grants a 7-day Premium trial automatically.
- Login does not create or extend trial.
- Trial is account-level and shared across desktop and future mobile.
- `/api/me/trial/claim` remains as a fallback/manual one-trial-per-account endpoint.

## Lesson persistence and access status

**Implemented and validated**

- Desktop signed-in lesson persistence uses `/api/me` lesson routes.
- Backend free lesson consumption works after:
  1. a lesson session has started;
  2. the learner sends at least 3 valid user messages in that session.
- Free plan allows 1 free lesson per day.
- Current lesson continuation is not blocked.
- Starting another new lesson can be blocked only when `SubscriptionEnforcement:Enabled=true`.
- Committed `SubscriptionEnforcement` default remains `false`.
- Backend lesson access endpoints exist:
  - `GET /api/me/lesson-access`
  - `GET /api/dev/lesson-access`
- Backend can enforce lesson start denial behind the config flag.
- Desktop preflight guard checks lesson access before navigation.
- Desktop handles backend `403 lesson_access_denied` fallback calmly.

## Subscription/free/Premium entitlement status

**Implemented and validated**

- Premium entitlement bypass works.
- Trial bypass works.
- Development-only unlimited Premium test accounts work via environment variable:
  - `DevelopmentTestAccounts__UnlimitedPremiumEmails__0="test@example.com"`
- Provider-event Premium entitlements are visible through existing backend access/status endpoints.
- Desktop and future mobile clients must use backend status/access state instead of local payment assumptions.

## Paddle billing foundation status

**Implemented and validated**

1. Provider-agnostic checkout skeleton exists:
   - `POST /api/me/billing/checkout-session`
   - default `provider=none` path returns a safe disabled response.
2. Paddle checkout transaction creation v1 works:
   - behind explicit config only;
   - backend calls Paddle sandbox/live API;
   - returns `checkoutUrl`;
   - does not itself activate Premium;
   - does not mutate `SubscriptionEntity`;
   - does not mutate `PaymentEntity`;
   - does not mutate `EntitlementEntity`.
3. Paddle webhook ingestion foundation v1 works:
   - `POST /api/billing/webhooks/paddle`
   - protected by `Paddle-Signature`, not JWT;
   - verifies the raw request body before JSON parsing;
   - stores raw events in `paddle_webhook_events`;
   - duplicate Paddle event ids are accepted idempotently.
4. Billing event normalization foundation v1 works:
   - accepted Paddle webhooks normalize into provider-agnostic `billing_events`;
   - raw payload remains in `paddle_webhook_events`;
   - `billing_events` stores safe metadata only.
5. Entitlement reconciliation decision foundation v1 works:
   - `transaction.completed` with internal user and `internalPlanId=premium` becomes `reconciliation_pending`;
   - unsupported events are ignored;
   - malformed/missing metadata is blocked.
6. Entitlement activation foundation v1 works:
   - valid `reconciliation_pending` Paddle `transaction.completed` events can create Premium `EntitlementEntity` rows;
   - activation requires an existing internal user, `internalPlanId=premium`, and future `billingPeriodEndsAtUtc`;
   - `source=provider_event`;
   - `SubscriptionId` remains null for now;
   - duplicate webhook does not create duplicate entitlement.
7. Event-scoped webhook request processing works:
   - webhook request processes only the current Paddle event id;
   - it does not process old unrelated `received`/`reconciliation_pending` billing events.
8. Backend access/status recognition works:
   - `provider_event` Premium entitlement is visible through existing backend access/status endpoints.
   - Desktop and future mobile must rely on backend state, not local payment assumptions.

## Admin/support foundation status

**Implemented for local Development support**

- Local Development admin foundation exists.
- Backend-hosted local admin shell exists at `/admin/`.
- Admin access uses Development/config bootstrap admin access.
- This is not production RBAC yet.
- Admin shell tabs:
  - Overview
  - User Lookup
  - Premium
  - Free Lesson
  - Audit Log
  - System
- Admin shell JWT remains memory-only.
- Admin supports:
  - exact user lookup by email;
  - read-only user diagnostics;
  - Premium entitlement schedule inspection;
  - manual Premium grant;
  - manual Premium revoke;
  - free lesson allowance reset;
  - read-only audit log;
  - capabilities view.
- Admin mutations require a reason and write audit actions.
- Static admin shell audit exists: `tools/audit_admin_shell.ps1`.
- Admin smoke exists: `tools/smoke_admin_foundation.ps1`.

## EF migrations

Current confirmed EF migrations:

- `20260518000000_InitialProductStorageSchema`
- `20260520120000_AddLessonSummaryContentFields`
- `20260520132002_AddUsageEventStatusAndStudyLanguage`
- `20260520150000_AddDailyUsageChatReplyCount`
- `20260524061817_AddSubscriptionFoundationV1`
- `20260528000000_AddPaddleWebhookEvents`

Latest confirmed EF migration:

- `20260528000000_AddPaddleWebhookEvents`

## Smoke scripts and latest confirmed validation

Current smoke scripts:

- `tools/smoke_billing_checkout.ps1`
- `tools/smoke_paddle_checkout_adapter.ps1`
- `tools/smoke_paddle_checkout_live_sandbox.ps1`
- `tools/smoke_paddle_webhook_ingestion.ps1`
- `tools/smoke_admin_foundation.ps1`

Latest confirmed validation:

- `tools/audit_admin_shell.ps1` passed.
- Lesson content audit passed.
- Desktop Debug build passed.
- Desktop Release build passed.
- Backend build passed.
- `dotnet ef migrations list` shows latest migration `20260528000000_AddPaddleWebhookEvents`.
- `dotnet ef database update` reports the database is already up to date.
- Paddle webhook smoke passed:
  - signed webhook accepted;
  - normalization into `billing_events`;
  - reconciliation decision for current provider event;
  - entitlement activation from validated `reconciliation_pending` billing event;
  - backend access/status sees Premium from `provider_event` entitlement;
  - duplicate webhook does not duplicate entitlement;
  - unsigned webhook returns 401;
  - invalid signature returns 401.
- Paddle adapter smoke passed.
- Admin foundation smoke passed.

## Current mutation boundaries

- This current-state update is documentation-only.
- No backend code should be changed for this update.
- No desktop code should be changed for this update.
- No admin UI should be changed for this update.
- No database entities or migrations should be changed for this update.
- No smoke scripts should be changed for this update.
- No new EF migration should be created for this update.
- Paddle checkout transaction creation does not mutate `SubscriptionEntity`, `PaymentEntity`, or `EntitlementEntity`.
- Paddle webhook entitlement activation can create Premium `EntitlementEntity` rows only after validated provider event reconciliation.
- `SubscriptionEntity` and `PaymentEntity` are not mutated by the Paddle flow yet.

## Known limitations / deferred scope

- `SubscriptionEntity` is not mutated by the Paddle flow yet.
- `PaymentEntity` is not mutated by the Paddle flow yet.
- Cancellation handling is not implemented yet.
- Expiry handling is not implemented yet.
- Revocation handling is not implemented yet.
- Renewal handling is not implemented yet.
- Full subscription reconciliation is not implemented yet.
- Payment record persistence is not implemented yet, unless later deemed necessary.
- Production Paddle webhook configuration is not completed yet.
- Desktop upgrade/paywall UI is not implemented yet.
- Future Apple/Google mobile entitlement bridge is not implemented yet.
- Production RBAC/admin system is not implemented yet.
- Contabo deployment is not part of this task.

## Next recommended phase

The next phase is subscription/payment lifecycle planning only. Do not start cancellation, renewal, revocation, full subscription reconciliation, or payment persistence implementation before a plan is approved.
