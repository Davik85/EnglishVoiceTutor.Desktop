# Subscription & Billing Foundation (Current State)

This document describes the current implemented foundation for account, trial, subscription, entitlement, free-limit enforcement, development test accounts, provider-agnostic checkout, Paddle billing/webhook ingestion, entitlement activation, and local Development CMS/admin support.

English Voice Tutor is an international product for desktop now and future mobile clients later. The backend is the backend source of truth for account, trial, subscription, entitlement, free allowance, lesson history, usage, limits, and billing state. Desktop and future mobile clients must rely on backend state, not local payment assumptions.

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
- Premium can come from trial, Development test-account grants, local admin grants, or validated provider billing events.
- Provider billing activation currently creates `EntitlementEntity` rows from validated `reconciliation_pending` `billing_events` only.
- In local Development, a configured development test account can simulate unlimited Premium entitlement.

## Enforcement model

- Config key: `SubscriptionEnforcement:Enabled`
- Committed default value remains: `false`
- Local override example:
  - `SubscriptionEnforcement__Enabled=true`
- Backend is the enforcement authority for lesson-start access.
- Desktop preflight checks improve UX, but the backend remains the backend source of truth.
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
- Paddle is the current web/desktop checkout provider foundation, but the integration stays adapter-based.
- Future entitlement/billing sources are expected to include:
  - Paddle for web/desktop checkout.
  - Apple App Store for future iOS.
  - Google Play for future Android.
  - Manual admin grants via CMS/admin tooling.
- The backend remains the backend source of truth for entitlement state.

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
- Entitlement activation foundation v1 is separate and can activate Premium only after webhook ingestion, normalization, reconciliation decision processing, and strict activation validation.
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
- After durable raw ingestion, the request flow runs normalization, reconciliation decision processing, and entitlement activation foundation v1 for the current provider event only.
- Premium entitlement activation is allowed only from validated provider-agnostic `billing_events`; raw webhook payloads are not used directly for business-state mutation.
- Subscription and payment mutation remain deferred.
- No internal payment records are created from webhooks in this step.
- Full subscription reconciliation is deferred.
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
- Normalization does not mutate `SubscriptionEntity`.
- Normalization now carries safe transaction snapshot metadata used by downstream payment persistence.
- Normalization does not directly mutate `PaymentEntity`.
- Normalization does not directly mutate `EntitlementEntity`.


## Payment persistence snapshot foundation v1

- Paddle `transaction.completed` and `transaction.payment_failed` events can now upsert a minimal provider-agnostic `PaymentEntity` diagnostic snapshot for an existing internal user when safe metadata maps to the Premium plan.
- Payment snapshots are idempotent by billing provider + provider transaction id.
- `transaction.completed` stores a `completed` payment snapshot and still relies on the existing entitlement activation flow for Premium access.
- `transaction.payment_failed` stores a `failed` payment snapshot and does not activate, revoke, or otherwise mutate Premium access.
- `PaymentEntity` is diagnostic payment history only and is not used as an access source.

## Subscription lifecycle snapshot foundation v1

- Paddle `subscription.created` and `subscription.updated` events can now upsert a provider-agnostic `SubscriptionEntity` snapshot for an existing internal user.
- Snapshot persistence is idempotent by provider + provider subscription id and ignores older provider events when a newer lifecycle event was already applied.
- This snapshot stores subscription lifecycle data only; it does not grant, revoke, pause, resume, renew, expire, or otherwise change Premium entitlement/access behavior.
- `PaymentEntity` is not mutated by this flow.

## Entitlement reconciliation decision foundation v1

- Normalized `billing_events` are inspected for entitlement reconciliation eligibility.
- Paddle `transaction.completed` events with an existing internal user and safe metadata containing `internalPlanId=premium` become `reconciliation_pending`.
- Unsupported billing event types are marked `ignored`.
- Malformed, invalid, or missing required safe metadata is blocked and does not become eligible for activation.
- This is provider-agnostic processing over `billing_events`.
- This step does not activate Premium by itself.
- This step does not mutate `SubscriptionEntity`.
- This step does not use `PaymentEntity` as an access source.
- This step does not call Paddle.
- Raw webhook events remain in `paddle_webhook_events`.

## Entitlement activation foundation v1

- Valid `reconciliation_pending` Paddle `transaction.completed` events can create Premium `EntitlementEntity` rows.
- Activation currently requires:
  - `provider=paddle`;
  - `eventType=transaction.completed`;
  - a valid `internalUserId` in safe metadata;
  - an existing internal user for that id;
  - `internalPlanId=premium`;
  - `billingPeriodEndsAtUtc` present, parseable, and in the future;
  - `source=provider_event`.
- `billingPeriodStartsAtUtc` is used as the entitlement start when present and valid; otherwise activation uses the current UTC processing time.
- Activation creates a Premium `EntitlementEntity` with source `provider_event`, status `active`, `SubscriptionId = null`, and a safe reason referencing the provider event.
- Existing events without a future `billingPeriodEndsAtUtc` are blocked instead of granting open-ended Premium.
- Duplicate webhook events do not create duplicate `billing_events` and therefore do not create duplicate entitlements.
- `SubscriptionEntity` is not mutated by this flow.
- Payment persistence may already have upserted a diagnostic `PaymentEntity` for the same provider event, but activation does not read `PaymentEntity` and does not use it as an access source.
- No cancellation, expiry, revocation, or renewal handling is implemented yet.
- No full subscription reconciliation is implemented yet.
- Activation does not call Paddle.
- Raw Paddle webhook events remain in `paddle_webhook_events`.
- Provider-agnostic `billing_events` remain the processing source for reconciliation decisions and entitlement activation.

## Event-scoped webhook request processing

- A Paddle webhook request processes only the current Paddle provider event id.
- The request normalizes only the current Paddle event.
- The request processes subscription lifecycle snapshot logic only for the current provider event.
- The request processes reconciliation decision logic only for the current provider event.
- The request activates entitlement only for the current provider event when strict activation validation passes.
- The request must not process old unrelated `received` or `reconciliation_pending` billing events.
- Broad/batch processing is reserved for future worker/backfill tooling.

## Backend access/status verification

- The Paddle webhook smoke verifies that a `provider_event` Premium entitlement is visible to existing backend access/status endpoints.
- Backend access/status endpoints recognize provider-event Premium entitlement as backend state.
- Desktop and future mobile clients must rely on backend state, not local payment assumptions.
- Desktop UI was not changed for this foundation step.
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
- Admin UI was not changed for Paddle entitlement activation/access smoke verification.

## Current smoke scripts

- `tools/smoke_billing_checkout.ps1`
- `tools/smoke_paddle_checkout_adapter.ps1`
- `tools/smoke_paddle_checkout_live_sandbox.ps1`
- `tools/smoke_paddle_webhook_ingestion.ps1`
- `tools/smoke_admin_foundation.ps1`

### Paddle webhook ingestion smoke coverage

`tools/smoke_paddle_webhook_ingestion.ps1` verifies:

- signed webhook accepted;
- normalization into `billing_events`;
- reconciliation decision for the current provider event;
- entitlement activation from validated `reconciliation_pending` billing events;
- backend access/status sees Premium from a `provider_event` entitlement;
- duplicate webhook does not duplicate entitlement;
- unsigned webhook returns `401`;
- invalid signature returns `401`.

Required local backend configuration for the webhook smoke:

```powershell
$env:PaddleWebhook__Enabled = "true"
$env:PaddleWebhook__SecretKey = "test_webhook_secret"
$env:PaddleWebhook__TimestampToleranceSeconds = "300"
```

`test_webhook_secret` is a local smoke-test placeholder only. Do not use or commit real Paddle API keys, webhook secrets, or price ids.

## Current latest EF migration

- Latest confirmed EF migration remains `20260528000000_AddPaddleWebhookEvents`.
- No new migration was created after entitlement activation/access smoke verification.
- No schema change is required by the current entitlement activation/access verification.

## Explicit non-goals and deferred scope

Current Paddle billing and entitlement work does **not** complete the full subscription lifecycle.

Deferred scope / next roadmap:
- cancellation, expiry, and revocation handling;
- renewal handling;
- subscription status reconciliation;
- full subscription reconciliation;
- production Paddle webhook configuration;
- production billing rollout hardening;
- desktop upgrade/paywall UI;
- future Apple/Google mobile entitlement bridge.

## Current mutation boundaries

- Checkout transaction creation returns `checkoutUrl` only and does not activate Premium.
- Raw webhook ingestion writes `paddle_webhook_events`.
- Normalization writes provider-agnostic `billing_events` with safe metadata only.
- Reconciliation decision updates only the current normalized provider event decision state.
- Entitlement activation can create Premium `EntitlementEntity` rows from validated `reconciliation_pending` billing events.
- `SubscriptionEntity` is mutated only by subscription lifecycle snapshot processing.
- `PaymentEntity` is mutated only by payment persistence snapshot processing and is not used for access decisions.
- Admin UI was not changed.
- Desktop UI was not changed.
- Latest payment persistence schema migration is `20260529000000_AddPaddlePaymentPersistenceV1`.
