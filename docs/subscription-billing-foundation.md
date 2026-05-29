# Subscription & Billing Foundation (Current State)

This document describes the current implemented foundation for account, trial, subscription, entitlement, free-limit enforcement, development test accounts, provider-agnostic checkout, Paddle billing/webhook ingestion, Paddle subscription/payment snapshots, entitlement activation/extension, canceled/paused expiry policy, and local Development CMS/admin support.

English Voice Tutor is a global, cross-platform, provider-agnostic product for desktop now and future mobile clients later. The backend is the source of truth for account, trial, subscription, entitlement, Premium/free status, daily free allowance, lesson history, usage, limits, payments, and billing state. Desktop and future mobile clients must rely on backend access/status decisions, not local payment assumptions.

## Account requirement for normal lesson start

- Normal lesson start requires sign-in.
- Desktop blocks signed-out users from starting a normal lesson and asks them to sign in or create an account.
- Development signed-out fallback paths still exist for diagnostics/development flows, but they are not the normal desktop UX path.

## Trial behavior

- A **7-day Premium trial** starts automatically after successful registration.
- Trial is **account-level** and intended to be shared by this account across desktop and future mobile clients.
- Login does **not** create or extend trial.
- `POST /api/me/trial/claim` still exists as a fallback/manual endpoint and remains one-trial-per-account.

## Free plan behavior

- Free plan currently allows **1 free lesson per day**.
- A free lesson is consumed only after both conditions are true in the same lesson session:
  1. Lesson session has started.
  2. Learner sends at least 3 valid user messages.
- Current lesson continuation is not blocked by this daily free-limit rule.
- Starting another new lesson can be blocked when subscription enforcement is enabled.

## Premium behavior

- Active Premium entitlement allows lesson start.
- Premium access bypasses free-lesson daily limits.
- `EntitlementEntity` remains the source of Premium access.
- Premium can come from trial, Development test-account grants, local admin grants, or validated provider billing events.
- Valid provider billing activation creates or extends `provider_event` Premium `EntitlementEntity` rows from validated `reconciliation_pending` `transaction.completed` `billing_events` only.
- Later valid period ends extend existing provider-event access; duplicate or older events do not duplicate or shorten entitlement.
- Actual `subscription.canceled` and `subscription.paused` events expire only active `provider_event` Premium entitlement for the resolved internal user/provider subscription context.
- Scheduled cancellation and past-due snapshots do not revoke Premium early.
- Manual/admin/trial/development/future-mobile entitlements are not touched by the provider-event canceled/paused expiry path.
- In local Development, a configured development test account can simulate unlimited Premium entitlement.

## Enforcement model

- Config key: `SubscriptionEnforcement:Enabled`
- Committed default value remains: `false`
- Local override example:
  - `SubscriptionEnforcement__Enabled=true`
- Backend is the enforcement authority for lesson-start access.
- Desktop preflight checks improve UX, but the backend remains the source of truth.
- Existing/current lesson continuation is not blocked by this enforcement; enforcement is about starting new lessons.

## Development unlimited Premium test accounts

- Config section: `DevelopmentTestAccounts`
- Local env var example (placeholder only):
  - `DevelopmentTestAccounts__UnlimitedPremiumEmails__0="test@example.com"`
- Development-only behavior.
- When matched, backend creates or updates a Premium entitlement using source `development_test_account`.
- Do not commit personal test emails.

## Billing checkout skeleton (provider-agnostic foundation)

Endpoint:
- `POST /api/me/billing/checkout-session`

Current disabled/default behavior:
- Requires authorization; unauthenticated requests return `401`.
- Expected invalid checkout requests return `400` with safe validation payloads and are handled at the endpoint layer, not as unhandled server exceptions.
- When no provider is configured, the endpoint returns a disabled/provider-not-configured response:
  - `created=false`
  - `checkoutEnabled=false`
  - `provider=none`
  - `errorCode=billing_provider_not_configured`
  - `message="Billing checkout is not configured yet."`
- No external payment provider call is made in the disabled/default path.
- No billing event is written in the disabled/default path.
- No `SubscriptionEntity`, `PaymentEntity`, or `EntitlementEntity` state is mutated in the disabled/default path.

Current config section:

```json
"Billing": {
  "CheckoutEnabled": false,
  "Provider": "none",
  "SuccessUrl": "",
  "CancelUrl": ""
}
```

## Provider-agnostic billing direction

- Internal plan ids stay independent from provider-specific plan/price ids.
- Paddle is the current web/desktop checkout provider adapter, but the integration stays adapter-based.
- Paddle must not be hardcoded into lesson/business access logic.
- Future entitlement/billing sources may include:
  - Paddle for web/desktop checkout.
  - Apple App Store for future iOS.
  - Google Play for future Android.
  - Manual admin grants via CMS/admin tooling.
- The Apple/Google mobile entitlement bridge is not implemented yet.
- The backend remains the source of truth for entitlement state.

## Paddle checkout transaction creation v1

Endpoint:
- `POST /api/me/billing/checkout-session`

Current completed behavior:
- Backend can create a Paddle sandbox/live checkout transaction only when explicitly configured.
- Paddle checkout is controlled by all of these settings being supplied outside client code:
  - `Billing__CheckoutEnabled=true`
  - `Billing__Provider=paddle`
  - `PaddleBilling__CheckoutAdapterEnabled=true`
  - `PaddleBilling__Environment=sandbox` or `PaddleBilling__Environment=live`
  - `PaddleBilling__ApiKey=<secret>`
  - `PaddleBilling__PremiumPriceId=<price id>`
- The backend calls the Paddle sandbox/live API and returns `checkoutUrl`.
- Paddle API keys and price ids must be stored in environment variables, user secrets, or secure deployment configuration; never store real values in tracked files or client code.
- Checkout transaction creation does **not** itself activate Premium.
- Checkout transaction creation does **not** create internal `billing_events`.
- Checkout transaction creation does **not** mutate `SubscriptionEntity`.
- Checkout transaction creation does **not** mutate `PaymentEntity`.
- Checkout transaction creation does **not** mutate `EntitlementEntity`.
- Entitlement activation is separate and can activate Premium only after webhook ingestion, normalization, reconciliation decision processing, and strict activation validation.
- Optional real sandbox smoke script: `tools/smoke_paddle_checkout_live_sandbox.ps1`.
- The optional real sandbox smoke requires `-AllowRealPaddleCall` and creates a real Paddle sandbox transaction only; it does not complete payment, call webhooks, or activate internal entitlement state.

## Paddle webhook ingestion foundation v1

Endpoint:
- `POST /api/billing/webhooks/paddle`

Current completed behavior:
- The endpoint exists for Paddle webhook ingestion.
- The endpoint is protected by Paddle `Paddle-Signature` verification, not JWT authentication.
- The endpoint uses the raw request body before JSON parsing because Paddle signs `<timestamp>:<raw_body>`.
- Signature verification uses `ts` and `h1` values from `Paddle-Signature`, HMAC-SHA256 with the configured notification destination secret, timing-safe comparison, and timestamp tolerance.
- If `PaddleWebhook:Enabled=false`, the endpoint returns `404` to hide the disabled webhook endpoint.
- If the endpoint is enabled but the secret is blank, it returns `503` with the safe message `Paddle webhook is not configured.`
- Missing, invalid, or stale signatures return `401`.
- Invalid JSON after valid signature verification returns `400`.
- Raw Paddle webhook events are stored in `paddle_webhook_events`.
- Duplicate Paddle event ids are accepted idempotently and return `200` with `duplicate=true` without inserting a second raw event row.
- New valid events return `200` with `accepted=true` and `duplicate=false`.
- After durable raw ingestion, the request flow runs normalization, subscription/payment snapshot processing, reconciliation decision processing, and entitlement activation/expiry policy for the current provider event only.
- Premium entitlement activation is allowed only from validated provider-agnostic `billing_events`; raw webhook payloads are not used directly for access-state mutation.
- The Paddle webhook secret must be stored in environment variables, user secrets, or secure deployment configuration. Do not put real webhook secrets in tracked files or client code.

Safe local configuration example:

```powershell
$env:PaddleWebhook__Enabled = "true"
$env:PaddleWebhook__SecretKey = "test_webhook_secret"
$env:PaddleWebhook__TimestampToleranceSeconds = "300"
```

## Billing event normalization foundation v1

- Accepted Paddle webhooks are normalized into provider-agnostic `billing_events` rows after the raw webhook event is safely stored in `paddle_webhook_events`.
- `paddle_webhook_events` remains the raw signed event ingestion table and stores the original webhook payload separately from the normalized billing stream.
- `billing_events` is the provider-agnostic event stream for reconciliation decisions and stores safe metadata only, not raw payloads, signatures, API keys, webhook secrets, or other secrets.
- Normalization is idempotent through the existing unique `billing_events` constraint on provider + provider event id.
- Normalization does not call Paddle.
- Normalization carries safe subscription/payment/entitlement metadata used by downstream provider-agnostic processing.
- Normalization does not directly grant Premium access.

## Subscription lifecycle snapshot foundation v1

- Paddle `subscription.created` and `subscription.updated` events upsert a provider-agnostic `SubscriptionEntity` snapshot for an existing internal user.
- Duplicate `subscription.created` is idempotent.
- `subscription.updated` updates provider snapshot/current period data.
- Older out-of-order `subscription.updated` events do not regress `SubscriptionEntity` state.
- `subscription.created` and `subscription.updated` do not activate Premium by themselves.
- Scheduled cancellation metadata from `subscription.updated` is recorded in `SubscriptionEntity`.
- `cancelAtPeriodEnd`, `scheduledChangeAction`, and `scheduledChangeEffectiveAtUtc` are exposed safely where needed for diagnostics/status.
- Scheduled cancellation does not revoke Premium early.
- Scheduled cancellation does not shorten existing `provider_event` entitlement.
- Paddle `subscription.past_due` is recorded as `SubscriptionEntity` snapshot/status.
- `subscription.past_due` does not create entitlement.
- `subscription.past_due` does not extend entitlement.
- `subscription.past_due` does not revoke already active entitlement.
- A user without active entitlement does not become Premium from `subscription.past_due`.
- Actual `subscription.canceled` updates `SubscriptionEntity.Status = Canceled`.
- Actual `subscription.paused` updates `SubscriptionEntity.Status = Paused`.
- `SubscriptionEntity` stores subscription lifecycle data only and does not grant Premium access by itself.
- `subscription.resumed` and `subscription.activated` update only `SubscriptionEntity` snapshot/status to active and do not restore Premium by themselves.
- Premium restoration still requires valid `transaction.completed` through the existing entitlement activation/extension path.

## Payment persistence snapshot foundation v1

- Paddle `transaction.completed` and `transaction.payment_failed` events upsert a minimal provider-agnostic `PaymentEntity` diagnostic snapshot for an existing internal user when safe metadata maps to the Premium plan.
- `PaymentEntity` stores provider-agnostic payment/transaction trail.
- Payment snapshots are idempotent by billing provider + provider transaction id.
- Duplicate `transaction.completed` and `transaction.payment_failed` events do not duplicate `PaymentEntity`.
- `transaction.completed` stores a `completed` payment snapshot and still relies on the existing entitlement activation flow for Premium access.
- `transaction.payment_failed` stores a `failed` payment snapshot and does not activate Premium.
- `PaymentEntity` is diagnostic payment history only and is not used as an access source.

## Entitlement reconciliation decision foundation v1

- Normalized `billing_events` are inspected for entitlement reconciliation eligibility.
- Paddle `transaction.completed` events with an existing internal user and safe metadata containing `internalPlanId=premium` become `reconciliation_pending`.
- Unsupported billing event types are marked `ignored`.
- Malformed, invalid, or missing required safe metadata is blocked and does not become eligible for activation.
- This is provider-agnostic processing over `billing_events`.
- This step does not activate Premium by itself.
- This step does not use `SubscriptionEntity` or `PaymentEntity` as an access source.
- This step does not call Paddle.
- Raw webhook events remain in `paddle_webhook_events`.

## Entitlement activation and extension foundation v1

- Valid `reconciliation_pending` Paddle `transaction.completed` events can create Premium `EntitlementEntity` rows.
- Valid later `transaction.completed` events can extend an existing `provider_event` Premium entitlement.
- Duplicate `transaction.completed` does not duplicate entitlement.
- Older `transaction.completed` does not shorten entitlement.
- Activation currently requires:
  - `provider=paddle`;
  - `eventType=transaction.completed`;
  - a valid `internalUserId` in safe metadata;
  - an existing internal user for that id;
  - `internalPlanId=premium`;
  - `billingPeriodEndsAtUtc` present, parseable, and in the future;
  - `source=provider_event`.
- `billingPeriodStartsAtUtc` is used as the entitlement start when present and valid; otherwise activation uses the current UTC processing time.
- Activation creates or extends a Premium `EntitlementEntity` with source `provider_event`, status `active`, and a safe reason referencing the provider event.
- Existing events without a future `billingPeriodEndsAtUtc` are blocked instead of granting open-ended Premium.
- Activation does not read `PaymentEntity` and does not use it as an access source.
- Activation does not call Paddle.
- Raw Paddle webhook events remain in `paddle_webhook_events`.
- Provider-agnostic `billing_events` remain the processing source for reconciliation decisions and entitlement activation.

## Actual canceled / paused expiry policy foundation v1

- Actual `subscription.canceled` expires only active `provider_event` Premium entitlement for the resolved internal user/provider subscription context.
- Actual `subscription.paused` expires only active `provider_event` Premium entitlement for the resolved internal user/provider subscription context.
- Manual/admin/trial/development/future-mobile entitlements are not touched by this provider-event expiry path.
- `subscription.resumed` and `subscription.activated` are snapshot-only active-status events and do not reverse provider-event entitlement expiry by themselves.

## Event-scoped webhook request processing

- A Paddle webhook request processes only the current Paddle provider event id.
- The request normalizes only the current Paddle event.
- The request processes subscription lifecycle snapshot logic only for the current provider event.
- The request processes payment snapshot logic only for the current provider event.
- The request processes reconciliation decision logic only for the current provider event.
- The request activates, extends, or expires entitlement only for the current provider event when strict validation passes.
- The request must not process old unrelated `received` or `reconciliation_pending` billing events.
- Broad/batch processing is reserved for future worker/backfill tooling.

## Backend access/status verification

- Backend access/status endpoints recognize provider-event Premium entitlement as backend state.
- `EntitlementEntity` remains the source of Premium access.
- `SubscriptionEntity` alone must not grant Premium access.
- `PaymentEntity` must not grant Premium access.
- Desktop and future mobile clients must rely on backend state, not local payment assumptions.
- Desktop upgrade/paywall UI was not implemented for this foundation step.
- Future mobile UI was not added for this foundation step.

## CMS/admin support foundation v1 checkpoint

- CMS/admin support foundation v1 is completed for local Development support workflows.
- It is not a production RBAC/admin system.
- It uses Development/config bootstrap admin access.
- The local admin shell is backend-hosted at `/admin/`.
- Tabs: Overview, User Lookup, Premium, Free Lesson, Audit Log, System.
- Admin shell JWT remains memory-only.
- It supports:
  - Exact user lookup by email.
  - Read-only user diagnostics.
  - Premium entitlement schedule inspection.
  - Manual Premium grant.
  - Manual Premium revoke.
  - Free lesson allowance reset.
  - Read-only audit log.
  - Capabilities view.
- Admin mutations require a reason and write audit actions.
- Static admin shell audit script (`tools/audit_admin_shell.ps1`) guards required tabs/forms/controls/endpoints and forbids `localStorage`/`sessionStorage` usage in `admin.js`.
- Smoke script (`tools/smoke_admin_foundation.ps1`) runs the admin shell audit first.
- Admin UI was not changed for Paddle lifecycle documentation.

## Current smoke scripts

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
- `tools/smoke_paddle_resumed_activated_snapshot_policy.ps1`

## Latest confirmed validation

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

### Paddle lifecycle smoke coverage

The Paddle lifecycle smoke scripts verify:

- signed webhook accepted;
- normalization into `billing_events`;
- event-scoped processing for the current provider event;
- subscription snapshot idempotency and out-of-order protection;
- payment snapshot idempotency;
- entitlement activation and extension from validated `reconciliation_pending` billing events;
- scheduled cancellation metadata recording without early revocation;
- past-due snapshot recording without entitlement creation, extension, or revocation;
- actual canceled/paused expiry of only active `provider_event` Premium entitlement for the resolved internal user/provider subscription context;
- backend access/status sees Premium from a `provider_event` entitlement;
- unsigned webhook returns `401`;
- invalid signature returns `401`.

Required local backend configuration for webhook smokes:

```powershell
$env:PaddleWebhook__Enabled = "true"
$env:PaddleWebhook__SecretKey = "test_webhook_secret"
$env:PaddleWebhook__TimestampToleranceSeconds = "300"
```

`test_webhook_secret` is a local smoke-test placeholder only. Do not use or commit real Paddle API keys, webhook secrets, price ids, customer ids, or transaction ids.

## Current latest EF migration

- Latest confirmed EF migration is `20260529000000_AddPaddlePaymentPersistenceV1`.
- `dotnet ef database update` reports the database is already up to date.
- `dotnet ef migrations has-pending-model-changes` reports no model changes.

## Explicit non-goals and deferred scope

Current Paddle billing and entitlement work does **not** complete all production billing operations.

Deferred scope / next roadmap:
- Production Paddle webhook configuration is not completed yet.
- Desktop upgrade/paywall UI is not implemented yet.
- `subscription.resumed` / `subscription.activated` grace-access restoration before `transaction.completed` is not implemented.
- Refund handling is not implemented yet.
- Chargeback handling is not implemented yet.
- Manual revocation automation is not implemented yet.
- Full subscription reconciliation / background reconciliation job is not implemented yet.
- Future Apple App Store / Google Play mobile entitlement bridge is not implemented yet.
- Production RBAC/admin system is not implemented yet.
- Contabo deployment is not part of this task.

## Current mutation boundaries

- Checkout transaction creation returns `checkoutUrl` only and does not activate Premium.
- Raw webhook ingestion writes `paddle_webhook_events`.
- Normalization writes provider-agnostic `billing_events` with safe metadata only.
- Subscription lifecycle processing updates `SubscriptionEntity` snapshots only; `SubscriptionEntity` does not grant Premium access by itself.
- Payment persistence updates `PaymentEntity` diagnostic snapshots only; `PaymentEntity` is not used for access decisions.
- Reconciliation decision updates only the current normalized provider event decision state.
- Entitlement activation can create or extend Premium `EntitlementEntity` rows from validated `reconciliation_pending` billing events.
- Actual `subscription.canceled` and `subscription.paused` events can shorten only active `provider_event` Premium `EntitlementEntity` rows for the resolved internal user/provider subscription context.
- Trial/manual/admin/development/future-mobile entitlements are not touched by the provider-event canceled/paused expiry path.
- Admin UI was not changed.
- Desktop UI was not changed.
- Latest payment persistence schema migration is `20260529000000_AddPaddlePaymentPersistenceV1`.
