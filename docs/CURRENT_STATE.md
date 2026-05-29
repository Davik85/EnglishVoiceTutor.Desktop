# Current State

Review date: 2026-05-29.

## Short summary

EnglishVoiceTutor currently has a working Windows desktop MVP backed by a working backend, PostgreSQL, and EF Core persistence foundation. Lesson Chat, account login, trial entitlement, free lesson access checks, local Development admin support, and the provider-agnostic billing lifecycle foundation through Step 3C are implemented and validated. Paddle is the current desktop/web provider adapter, but backend account, subscription, entitlement, usage, limits, lesson history, payment, and Premium/free status remain the source of truth.

## Product architecture principle

- Product context remains global, cross-platform, and provider-agnostic.
- The backend is the source of truth for account, trial, subscription, Premium/free status, daily free allowance, usage, limits, lesson history, payments, and entitlements.
- Desktop and future mobile clients must rely on backend account/subscription/entitlement state, not local payment assumptions.
- Paddle is the current desktop/web billing provider adapter.
- Core backend subscription, entitlement, and access logic must remain provider-agnostic.
- `EntitlementEntity` remains the source of Premium access.
- `SubscriptionEntity` is a provider-agnostic subscription snapshot and does not grant Premium access by itself.
- `PaymentEntity` is diagnostic payment history only and is not an access source.
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
- Valid Paddle `transaction.completed` events can create or extend `provider_event` Premium entitlement.
- Older Paddle `transaction.completed` events do not shorten entitlement.
- Duplicate Paddle `transaction.completed` events do not duplicate entitlement.
- Actual Paddle `subscription.canceled` and `subscription.paused` events expire only active `provider_event` Premium entitlement for the resolved internal user/provider subscription context.
- Manual/admin/trial/development/future-mobile entitlement sources are not touched by the provider-event canceled/paused expiry path.
- Desktop and future mobile clients must use backend status/access state instead of local payment assumptions.

## Paddle billing lifecycle foundation status

**Implemented and validated through Step 3C**

1. Provider-agnostic checkout foundation:
   - `POST /api/me/billing/checkout-session` exists.
   - Default `provider=none` path returns a safe disabled response.
   - Paddle checkout transaction creation v1 works behind explicit config.
   - Checkout itself does not activate Premium.
2. Paddle webhook foundation:
   - `POST /api/billing/webhooks/paddle` exists.
   - The endpoint is protected by `Paddle-Signature`, not JWT.
   - Raw body is verified before JSON parsing.
   - Raw Paddle events are stored in `paddle_webhook_events`.
   - Duplicate Paddle event ids are accepted idempotently.
   - Accepted Paddle webhooks normalize into provider-agnostic `billing_events`.
   - Raw payload remains in `paddle_webhook_events`.
   - `billing_events` stores safe metadata only.
   - Webhook request processing is event-scoped and processes only the current provider event id.
3. Subscription snapshot persistence:
   - `subscription.created` and `subscription.updated` upsert `SubscriptionEntity` snapshot.
   - Duplicate `subscription.created` is idempotent.
   - `subscription.updated` updates provider snapshot/current period.
   - Older out-of-order `subscription.updated` does not regress `SubscriptionEntity` state.
   - `subscription.created` and `subscription.updated` do not activate Premium by themselves.
4. Payment snapshot persistence:
   - `transaction.completed` and `transaction.payment_failed` upsert `PaymentEntity` diagnostic snapshots.
   - `PaymentEntity` stores provider-agnostic payment/transaction trail.
   - `PaymentEntity` is not used as an access source.
   - Duplicate `transaction.completed` and `transaction.payment_failed` are idempotent and do not duplicate `PaymentEntity`.
   - `transaction.payment_failed` does not activate Premium.
5. Entitlement activation and extension:
   - Valid `transaction.completed` can create `provider_event` Premium entitlement.
   - Valid later `transaction.completed` can extend an existing `provider_event` Premium entitlement.
   - Duplicate `transaction.completed` does not duplicate entitlement.
   - Older `transaction.completed` does not shorten entitlement.
   - `EntitlementEntity` remains the source of Premium access.
6. Scheduled cancellation policy:
   - `subscription.updated` with scheduled cancellation records cancellation metadata in `SubscriptionEntity`.
   - `cancelAtPeriodEnd`, `scheduledChangeAction`, and `scheduledChangeEffectiveAtUtc` are exposed safely where needed for diagnostics/status.
   - Scheduled cancellation does not revoke Premium early.
   - Scheduled cancellation does not shorten existing `provider_event` entitlement.
7. Past due policy:
   - `subscription.past_due` is recorded as `SubscriptionEntity` snapshot/status.
   - `subscription.past_due` does not create entitlement.
   - `subscription.past_due` does not extend entitlement.
   - `subscription.past_due` does not revoke already active entitlement.
   - A user without active entitlement does not become Premium from `subscription.past_due`.
8. Actual canceled / paused policy:
   - Actual `subscription.canceled` updates `SubscriptionEntity.Status = Canceled`.
   - Actual `subscription.canceled` expires only active `provider_event` Premium entitlement for the resolved internal user/provider subscription context.
   - Actual `subscription.paused` updates `SubscriptionEntity.Status = Paused`.
   - Actual `subscription.paused` expires only active `provider_event` Premium entitlement for the resolved internal user/provider subscription context.
   - Manual/admin/trial/development/future-mobile entitlements are not touched by this provider-event expiry path.
   - `subscription.resumed` / `subscription.activated` restore behavior is not implemented yet.

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
- `20260528010000_AddPaddleSubscriptionLifecycleSnapshotV1`
- `20260529000000_AddPaddlePaymentPersistenceV1`

Latest confirmed EF migration:

- `20260529000000_AddPaddlePaymentPersistenceV1`

## Smoke scripts and latest confirmed validation

Current smoke scripts:

- `tools/smoke_billing_checkout.ps1`
- `tools/smoke_paddle_checkout_adapter.ps1`
- `tools/smoke_paddle_checkout_live_sandbox.ps1`
- `tools/smoke_paddle_webhook_ingestion.ps1`
- `tools/smoke_admin_foundation.ps1`
- `tools/smoke_paddle_subscription_lifecycle.ps1`
- `tools/smoke_paddle_payment_persistence.ps1`
- `tools/smoke_paddle_entitlement_extension.ps1`
- `tools/smoke_paddle_cancellation_past_due_policy.ps1`
- `tools/smoke_paddle_canceled_paused_expiry_policy.ps1`

Latest confirmed validation:

- `tools/audit_lesson_content.ps1` passed.
- Desktop Debug build passed.
- Desktop Release build passed.
- Backend build passed.
- `dotnet ef migrations list` shows latest migration `20260529000000_AddPaddlePaymentPersistenceV1`.
- `dotnet ef database update` reports the database is already up to date.
- `dotnet ef migrations has-pending-model-changes` reports no model changes.
- `tools/smoke_paddle_canceled_paused_expiry_policy.ps1` passed.
- `tools/smoke_paddle_cancellation_past_due_policy.ps1` passed.
- `tools/smoke_paddle_entitlement_extension.ps1` passed.
- `tools/smoke_paddle_payment_persistence.ps1` passed.
- `tools/smoke_paddle_subscription_lifecycle.ps1` passed.
- `tools/smoke_paddle_webhook_ingestion.ps1` passed.

## Current mutation boundaries

- This current-state update is documentation-only.
- No backend code should be changed for this update.
- No desktop code should be changed for this update.
- No admin UI should be changed for this update.
- No database entities or migrations should be changed for this update.
- No smoke scripts should be changed for this update.
- No new EF migration should be created for this update.
- Checkout transaction creation returns `checkoutUrl` only and does not activate Premium.
- Raw webhook ingestion writes `paddle_webhook_events`.
- Normalization writes provider-agnostic `billing_events` with safe metadata only.
- Reconciliation decision updates only the current normalized provider event decision state.
- Entitlement activation can create or extend Premium `EntitlementEntity` rows from validated `reconciliation_pending` billing events.
- `SubscriptionEntity` is mutated only by subscription lifecycle snapshot processing and does not grant Premium access by itself.
- Actual `subscription.canceled` and `subscription.paused` events can shorten only active `provider_event` Premium `EntitlementEntity` rows for the resolved internal user/provider subscription context.
- `PaymentEntity` is mutated only by payment persistence snapshot processing and is not used for access decisions.

## Known limitations / deferred scope

- Production Paddle webhook configuration is not completed yet.
- Desktop upgrade/paywall UI is not implemented yet.
- `subscription.resumed` behavior is not implemented yet.
- `subscription.activated` restore behavior is not implemented yet.
- Refund handling is not implemented yet.
- Chargeback handling is not implemented yet.
- Manual revocation automation is not implemented yet.
- Full subscription reconciliation / background reconciliation job is not implemented yet.
- Future Apple App Store / Google Play mobile entitlement bridge is not implemented yet.
- Production RBAC/admin system is not implemented yet.
- Contabo deployment is not part of this task.

## Next recommended phase

The next phase is planning remaining billing operations only. Do not implement remaining lifecycle behavior before a plan is approved. Recommended planning topics:

- `subscription.resumed` / `subscription.activated` restore policy;
- refunds / chargebacks policy;
- manual revocation automation policy;
- production Paddle webhook setup checklist;
- desktop upgrade/paywall UI plan;
- future Apple App Store / Google Play mobile entitlement bridge plan;
- optional background reconciliation job.
