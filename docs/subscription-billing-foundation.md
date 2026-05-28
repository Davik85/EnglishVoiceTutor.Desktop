# Subscription & Billing Foundation (Current State)

This document describes the **current implemented foundation** for account/trial/subscription/entitlement/free-limit enforcement, development test accounts, provider-agnostic billing checkout skeleton behavior, completed local Development CMS/admin support foundation v1, and deferred global billing provider work.

English Voice Tutor is designed as an international product for desktop now and mobile apps later. The backend is the single source of truth for account, trial, subscription, entitlement, free allowance, lesson history, usage, limits, and billing state.

## Account requirement for normal lesson start

- Normal lesson start requires sign-in.
- Desktop now blocks signed-out users from starting a normal lesson and asks them to sign in or create an account.
- Development signed-out fallback paths still exist for diagnostics/development flows, but they are not the normal desktop UX path.

## Trial behavior

- A **7-day Premium trial** starts automatically after successful registration.
- Trial is **account-level** and intended to be shared by this account across desktop and future mobile apps.
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
- In local Development, a configured development test account can simulate unlimited Premium entitlement.

## Enforcement model

- Config key: `SubscriptionEnforcement:Enabled`
- Committed default value remains: `false`
- Local override example:
  - `SubscriptionEnforcement__Enabled=true`
- Backend is the enforcement authority for lesson-start access.
- Desktop preflight checks improve UX, but backend remains source of truth.
- Existing/current lesson continuation is not blocked by this enforcement; enforcement is about starting new lessons.

## Development unlimited Premium test accounts

- Config section: `DevelopmentTestAccounts`
- Local env var example (placeholder only):
  - `DevelopmentTestAccounts__UnlimitedPremiumEmails__0="22222@gmail.com"`
- Development-only behavior.
- When matched, backend creates or updates a Premium entitlement using source `development_test_account`.
- Do not commit personal test emails.

## Billing checkout skeleton (provider-agnostic foundation)

Endpoint:
- `POST /api/me/billing/checkout-session`

Current behavior:
- Requires authorization (unauthenticated request returns `401`).
- Expected invalid checkout requests return `400` with safe validation payloads and are handled at the endpoint layer (not as unhandled server exceptions).
- Returns disabled/provider-not-configured response now:
  - `created=false`
  - `checkoutEnabled=false`
  - `provider=none`
  - `errorCode=billing_provider_not_configured`
  - `message="Billing checkout is not configured yet."`
- No real checkout session is created.
- No external payment provider call is made.
- No billing event is written.
- No subscription or entitlement state is mutated.
- Paddle checkout transaction creation v1 is implemented separately; Paddle webhook behavior is now limited to ingestion, signature verification, raw event persistence, and idempotency readiness.
- No database migration was required.
- Latest confirmed EF migration becomes `AddPaddleWebhookEvents`.

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
- Paddle is likely first web/desktop checkout provider, but integration must be adapter-based.
- Future entitlement/billing sources are expected to include:
  - Paddle (web/desktop checkout)
  - Apple App Store (future iOS)
  - Google Play (future Android)
  - Manual admin grant via future CMS/admin tooling
- Backend remains the only source of truth for entitlement state.

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
- No database migration was required.
- Latest confirmed EF migration becomes `AddPaddleWebhookEvents`.
- Paddle checkout transaction creation v1 exists; Paddle webhook ingestion foundation v1 stores signed raw events for idempotency, and entitlement activation foundation v1 can activate Premium from validated provider-agnostic billing events.

## Paddle webhook ingestion foundation v1

Endpoint:
- `POST /api/billing/webhooks/paddle`

Current behavior:
- The endpoint is protected by Paddle `Paddle-Signature` verification, not JWT authentication.
- The endpoint reads the raw request body and verifies it before JSON parsing because Paddle signs `<timestamp>:<raw_body>`.
- Signature verification uses the `ts` and `h1` values from `Paddle-Signature`, HMAC-SHA256 with the configured notification destination secret, timing-safe comparison, and timestamp tolerance.
- If `PaddleWebhook:Enabled=false`, the endpoint returns `404` to hide the disabled webhook endpoint.
- If the endpoint is enabled but the secret is blank, it returns `503` with the safe message `Paddle webhook is not configured.`
- Missing, invalid, or stale signatures return `401`.
- Invalid JSON after valid signature verification returns `400`.
- Events are stored in `paddle_webhook_events` for idempotency and future processing.
- Duplicate Paddle event ids return `200` with `duplicate=true` and do not insert a second row.
- New valid events return `200` with `accepted=true` and `duplicate=false`.
- After durable raw ingestion, the endpoint runs normalization, reconciliation decision processing, and entitlement activation foundation v1.
- Premium entitlement activation is allowed only from validated provider-agnostic `billing_events`; raw webhook payloads are not used directly for business-state mutation.
- Subscription and payment mutation remain deferred.
- No internal payment records are created from webhooks in this step.
- Full subscription reconciliation is deferred.
- The Paddle webhook secret must be stored in environment variables, user secrets, or secure deployment configuration. Do not put real webhook secrets in appsettings or client code.
- The new EF migration `AddPaddleWebhookEvents` is required because webhook idempotency and future reconciliation need the `paddle_webhook_events` persistence table.
- Latest confirmed EF migration becomes `AddPaddleWebhookEvents` after this update.
- Local webhook ingestion smoke script: `tools/smoke_paddle_webhook_ingestion.ps1`.

Safe local configuration example:

```powershell
$env:PaddleWebhook__Enabled = "true"
$env:PaddleWebhook__SecretKey = "test_webhook_secret"
$env:PaddleWebhook__TimestampToleranceSeconds = "300"
```

## Entitlement activation foundation v1

- `reconciliation_pending` provider-agnostic `billing_events` can now create Premium `EntitlementEntity` rows when all strict validation passes.
- Activation currently requires:
  - billing provider `paddle`;
  - event type `transaction.completed`;
  - a valid `internalUserId` in `SafeMetadataJson`;
  - an existing backend user for that id;
  - `internalPlanId` equal to `premium`;
  - `billingPeriodEndsAtUtc` present, parseable, and in the future.
- `billingPeriodStartsAtUtc` is used as the entitlement start when present and valid; otherwise activation uses the current UTC processing time.
- Activation creates a Premium `EntitlementEntity` with source `provider_event`, status `active`, `SubscriptionId = null`, and a safe reason referencing the provider event.
- Existing events without `billingPeriodEndsAtUtc` are blocked instead of granting open-ended Premium.
- Duplicate webhook events do not create duplicate billing events and therefore do not create duplicate entitlements.
- This step does **not** update `SubscriptionEntity`.
- This step does **not** create or update `PaymentEntity`.
- This step does **not** implement cancellation, expiry, revocation, or renewal handling.
- This step does **not** implement full subscription reconciliation.
- This step does **not** call Paddle.
- Raw Paddle webhook events remain in `paddle_webhook_events`.
- Provider-agnostic `billing_events` remain the processing source for reconciliation decisions and entitlement activation.
- No database migration was required because the current schema already had the required billing event metadata and entitlement columns.
- Latest confirmed EF migration remains `20260528000000_AddPaddleWebhookEvents`.
- Webhook smoke now registers a real local test user, extracts the `evt_user_id` claim from the JWT locally, sends a signed `transaction.completed` payload with a future billing period, and verifies entitlement activation.
- Paddle webhook request processing is event-scoped: a signed `POST /api/billing/webhooks/paddle` normalizes, reconciles, and activates/blocks/fails only the current Paddle event identified by the incoming provider event id.
- Broad batch processing methods remain reserved for future worker, backfill, and reconciliation tooling rather than the synchronous webhook request flow.
- This prevents one incoming webhook from mutating unrelated old `received` or `reconciliation_pending` billing events.
- No migration was required for this event-scoped webhook flow change.
- Latest confirmed migration remains `20260528000000_AddPaddleWebhookEvents`.

## Deferred / Not implemented yet

- No production admin roles/RBAC yet.
- No production admin deployment/security hardening yet.
- No further Paddle checkout work beyond transaction creation v1 and webhook ingestion foundation v1 yet.
- No Apple App Store / Google Play integration yet.
- No real payment acceptance yet.
- No provider reconciliation job yet.
- No desktop paywall/upgrade UI yet.

## Roadmap (recommended order)

- ✅ Completed: CMS/admin support foundation v1 (local Development support/admin workflows).

1. Provider-agnostic billing provider adapter interface.
2. Paddle checkout adapter.
3. Paddle webhook ingestion.
4. Billing event persistence and idempotency checks.
5. Entitlement reconciliation.
6. Desktop upgrade/paywall UI.
7. Future Apple/Google mobile entitlement bridge.
8. Production admin roles/RBAC and hardening.

## Validation checkpoint

Commands:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools\audit_admin_shell.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File tools\audit_lesson_content.ps1
dotnet build
dotnet build -c Release
cd backend\EnglishVoiceTutor.Api
dotnet build
dotnet ef migrations list --project .\EnglishVoiceTutor.Api.csproj --startup-project .\EnglishVoiceTutor.Api.csproj
```

Expected results:

- Admin shell audit passes.
- Lesson content audit passes.
- Desktop Debug/Release builds pass.
- Backend build passes.
- Latest migration becomes `AddPaddleWebhookEvents`.

## Admin Foundation v1 (backend-only bootstrap)

- Admin Foundation v1 is backend-only.
- Access is Development/config bootstrap admin access (`AdminBootstrap:Enabled` with configured bootstrap emails).
- Endpoint surface currently includes only: `GET /api/admin/me`.
- `GET /api/admin/me` requires a valid Bearer token and bootstrap admin authorization and returns a read-only self-check payload.
- No CMS/admin UI is implemented.
- No user search is included in v1.
- No manual grant/revoke/reset actions are implemented.
- Paddle checkout transaction creation v1 and Paddle webhook ingestion foundation v1 exist; entitlement activation and reconciliation are deferred.
- `admin_actions` storage exists for future audited admin mutations, but `/api/admin/me` does **not** write audit actions.

## Admin Foundation v2 (backend-only exact user lookup)

- Admin Foundation v2 is backend-only and read-only.
- Added endpoint: `GET /api/admin/users/by-email?email=user@example.com`.
- Endpoint is protected by `AdminAuthorizationConstants.BootstrapAdminPolicyName`.
- Lookup is exact by normalized email (trim + lower-invariant), with no broad search or partial matching.
- Returns a safe response containing user overview, profile/settings snapshot, subscription status, and checked timestamp.
- No CMS/admin UI is implemented.
- No manual grant/revoke/reset actions are implemented.
- No additional Paddle checkout/webhook behavior is introduced by this diagnostics section.
- No audit writes are performed for this read-only lookup.

- Latest confirmed EF migration becomes `AddPaddleWebhookEvents`.
- Admin Foundation v1/v2 did not require a database migration.

## Safe local test commands (PowerShell)

Run backend with enforcement enabled:

```powershell
cd C:\dev\EnglishVoiceTutor.Desktop\backend\EnglishVoiceTutor.Api
$env:ASPNETCORE_ENVIRONMENT="Development"
$env:DOTNET_ENVIRONMENT="Development"
$env:SubscriptionEnforcement__Enabled="true"
dotnet run
```

Optional development unlimited Premium test account (placeholder example only):

```powershell
$env:DevelopmentTestAccounts__UnlimitedPremiumEmails__0="22222@gmail.com"
```

Useful API checks (authenticated unless noted):

- `GET /api/me/subscription-status`
- `GET /api/me/lesson-access`
- `POST /api/me/billing/checkout-session`
- `GET /api/admin/me`
- `GET /api/admin/users/by-email?email=user@example.com`

Do not use real tokens or secrets in shared docs/scripts.

## Admin Foundation v3 (backend-only read-only diagnostics)

- Admin Foundation v3 extends the exact user lookup response with read-only diagnostics.
- It includes recent lesson sessions, daily usage counters, active entitlements, and recent usage events.
- It is protected by the existing bootstrap admin policy.
- It does not add CMS UI.
- It does not add manual grant/revoke/reset.
- It does not write audit actions.
- It does not add Paddle checkout/webhooks.
- It does not require a database migration.
- Latest confirmed EF migration becomes `AddPaddleWebhookEvents`.

## Admin Foundation v4 (audit service foundation)

- Admin Foundation v4 adds a backend-only audit service foundation for future admin mutations.
- It uses the existing `admin_actions` storage.
- It does not add CMS UI.
- It does not add any new mutation endpoint.
- It does not add manual Premium grant/revoke.
- It does not add free allowance reset.
- It does not write audit actions for read-only diagnostics.
- It does not add Paddle checkout/webhooks.
- It does not require a database migration.
- Latest confirmed EF migration becomes `AddPaddleWebhookEvents`.
- Future admin mutations must call the audit service with a clear reason and safe metadata only.


## Admin Foundation v5 (manual Premium grant)

- Admin Foundation v5 adds a backend-only manual Premium grant endpoint.
- Endpoint: `POST /api/admin/users/{userId}/premium-grants`.
- It is protected by the existing bootstrap admin policy.
- It creates a manual active Premium entitlement with source `manual_admin`.
- It requires a clear reason and bounded `durationDays`.
- It writes an audit action via `AdminAuditService` into `admin_actions`.
- It does not add revoke.
- It does not add free allowance reset.
- It does not add CMS UI.
- It does not add Paddle checkout/webhooks.
- It does not create subscription/payment records.
- It does not require a database migration.
- Latest confirmed EF migration becomes `AddPaddleWebhookEvents`.

## Admin Foundation v6 (manual Premium revoke)

- Admin Foundation v6 adds a backend-only manual Premium revoke endpoint.
- Endpoint: `POST /api/admin/users/{userId}/premium-grants/{entitlementId}/revoke`.
- It is protected by the existing bootstrap admin policy.
- It can revoke only active `manual_admin` Premium entitlements.
- It does not revoke trial/provider/subscription/store entitlements.
- It requires a clear reason.
- It writes an audit action via `AdminAuditService` / `admin_actions`.
- It does not delete entitlement history.
- It does not add free allowance reset.
- It does not add CMS UI.
- It does not add Paddle checkout/webhooks.
- It does not create or mutate subscription/payment records.
- It does not require a database migration.
- Latest confirmed EF migration becomes `AddPaddleWebhookEvents`.

## Admin Foundation v7 (read-only audit log)

- Admin Foundation v7 adds a backend-only read-only audit log endpoint.
- Endpoint: `GET /api/admin/users/{userId}/audit-actions`.
- It is protected by the existing bootstrap admin policy.
- It returns recent audit actions for a target user.
- It supports a bounded `limit` query parameter.
- It does not write audit actions.
- It does not add CMS UI.
- It does not add grant/revoke/reset behavior.
- It does not add Paddle checkout/webhooks.
- It does not create or mutate subscription/payment records.
- It does not require a database migration.
- Latest confirmed EF migration becomes `AddPaddleWebhookEvents`.

## Admin Foundation v8 (free lesson allowance reset)

- Admin Foundation v8 adds a backend-only free lesson allowance reset endpoint.
- Endpoint: `POST /api/admin/users/{userId}/free-lesson-allowance/reset`.
- It is protected by the existing bootstrap admin policy.
- It resets the daily free lesson allowance by deleting the `DailyFreeLessonUsage` record for a target user and date.
- If `usageDate` is omitted, backend uses today in UTC.
- It requires a clear reason.

## Admin User Lookup Premium schedule visibility

- Manual Premium grants may now be stacked into the future.
- `ActiveEntitlements` shows only currently active entitlements.
- `PremiumEntitlementSchedule` shows current and future active Premium entitlements.
- CMS UI shows this schedule so support/admin can verify that a grant was issued even if it starts later.
- No database migration is required.
- Latest confirmed EF migration becomes `AddPaddleWebhookEvents`.
- No Paddle checkout/webhooks were added.
- It writes an audit action via `AdminAuditService` / `admin_actions`.
- It does not delete lesson sessions or lesson messages.
- It does not reset `DailyUsageCounters` or old per-operation usage counters.
- It does not change Premium/trial/subscription/payment records.
- It does not add CMS UI.
- It does not add Paddle checkout/webhooks.
- It does not require a database migration.
- Latest confirmed EF migration becomes `AddPaddleWebhookEvents`.

## Admin Foundation v9 (local smoke test script)

- Admin Foundation v9 adds a local PowerShell smoke test script.
- Script: `tools/smoke_admin_foundation.ps1`.
- It verifies existing Admin Foundation v1-v8 behavior against a running Development backend.
- It does not start the backend automatically.
- It does not add backend behavior.
- It does not add CMS UI.
- It does not add Paddle checkout/webhooks.
- It does not require a database migration.
- Latest confirmed EF migration becomes `AddPaddleWebhookEvents`.

Usage example:

```powershell
cd C:\dev\EnglishVoiceTutor.Desktop
powershell -ExecutionPolicy Bypass -File tools\smoke_admin_foundation.ps1
```

## Admin Foundation v10 (admin capabilities endpoint)

- Admin Foundation v10 adds a backend-only read-only capabilities endpoint for the future CMS/Admin UI.
- Endpoint: `GET /api/admin/capabilities`.
- It is protected by the existing bootstrap admin policy.
- It tells future UI which admin capabilities are currently available.
- It confirms CMS UI, production roles, billing provider, Paddle checkout/webhooks, and mobile store bridge are not available yet.
- It does not write audit actions.
- It does not add CMS UI.
- It does not add Paddle checkout/webhooks.
- It does not add new admin mutation behavior.
- It does not require a database migration.
- Latest confirmed EF migration becomes `AddPaddleWebhookEvents`.


## CMS UI Phase 1 (backend-hosted admin shell)

- CMS UI Phase 1 adds a backend-hosted static admin shell.
- URL: `/admin/`.
- It uses existing `/api/auth/login` and `/api/admin/capabilities`.
- The shell stores the JWT token only in memory for this phase.
- The shell displays capabilities and placeholder admin sections.
- It does not implement user lookup UI yet.
- It does not implement grant/revoke/reset UI yet.
- It does not add new admin mutation behavior.
- It does not add Paddle checkout/webhooks.
- It does not add production roles.
- It does not require a database migration.
- Latest confirmed EF migration becomes `AddPaddleWebhookEvents`.

## CMS UI Phase 2 (read-only user lookup)

- CMS UI Phase 2 adds a read-only User Lookup section to the backend-hosted admin shell.
- It uses the existing `GET /api/admin/users/by-email?email=...` endpoint.
- It displays user summary, subscription status, profile/settings snapshots, active entitlements, recent lesson sessions, daily usage counters, and recent usage events.
- It does not add new backend endpoints.
- It does not add grant/revoke/reset UI.
- It does not call admin mutation endpoints.
- It does not add Paddle checkout/webhooks.
- It does not add production roles.
- It does not require a database migration.
- JWT remains in memory only for this phase.
- Latest confirmed EF migration becomes `AddPaddleWebhookEvents`.

## CMS UI Phase 3 (read-only audit log)

- CMS UI Phase 3 adds a read-only Audit Log section for the currently looked-up user.
- It uses the existing `GET /api/admin/users/{userId}/audit-actions?limit=...` endpoint.
- It automatically loads audit actions after successful user lookup.
- It supports bounded limit selection.
- It displays action type, reason, admin user id, action id, timestamp, and safe metadata as plain text.
- It does not add new backend endpoints.
- It does not add grant/revoke/reset UI.
- It does not call admin mutation endpoints.
- It does not add Paddle checkout/webhooks.
- It does not add production roles.
- It does not require a database migration.
- JWT remains in memory only for this phase.
- Latest confirmed EF migration becomes `AddPaddleWebhookEvents`.


## CMS UI Phase 4 (manual Premium grant UI)

- CMS UI Phase 4 adds a Manual Premium Grant section for the currently looked-up user.
- It uses the existing `POST /api/admin/users/{userId}/premium-grants` endpoint.
- It requires `durationDays` and `reason`.
- It asks for confirmation before calling the backend.
- After successful grant, it refreshes user lookup and audit log.
- It does not add new backend endpoints.
- It does not add revoke UI.
- It does not add free lesson reset UI.
- It does not add Paddle checkout/webhooks.
- It does not add production roles.
- It does not require a database migration.
- JWT remains in memory only for this phase.
- Latest confirmed EF migration becomes `AddPaddleWebhookEvents`.

## CMS UI Phase 5 (manual Premium revoke UI)

- CMS UI Phase 5 adds a Manual Premium Revoke section for the currently looked-up user.
- It uses the existing `POST /api/admin/users/{userId}/premium-grants/{entitlementId}/revoke` endpoint.
- It allows revoking active `manual_admin` Premium entitlements from the user's PremiumEntitlementSchedule.
- It requires a reason.
- It asks for confirmation before calling the backend.
- After successful revoke, it refreshes user lookup and audit log.
- It does not add new backend endpoints.
- It does not add free lesson reset UI.
- It does not add Paddle checkout/webhooks.
- It does not add production roles.
- It does not require a database migration.
- JWT remains in memory only for this phase.
- Latest confirmed EF migration becomes `AddPaddleWebhookEvents`.

## CMS UI Phase 6 (free lesson allowance reset UI)

- CMS UI Phase 6 adds a Free Lesson Allowance Reset section for the currently looked-up user.
- It uses the existing `POST /api/admin/users/{userId}/free-lesson-allowance/reset` endpoint.
- It requires `usageDate` and `reason` in the UI.
- It asks for confirmation before calling the backend.
- After successful reset, it refreshes user lookup and audit log.
- It does not add new backend endpoints.
- It does not add Paddle checkout/webhooks.
- It does not add production roles.
- It does not require a database migration.
- JWT remains in memory only for this phase.
- Latest confirmed EF migration becomes `AddPaddleWebhookEvents`.

## CMS UI Phase 7 (tabbed admin layout)

- CMS UI Phase 7 reorganizes the existing backend-hosted admin shell into a left-navigation tabbed layout.
- Tabs: Overview, User Lookup, Premium, Free Lesson, Audit Log, System.
- This phase does not add new backend endpoints.
- This phase does not change admin API behavior.
- Existing grant/revoke/reset/audit/user lookup flows remain the same.
- This phase does not add Paddle checkout/webhooks.
- This phase does not add production roles.
- This phase does not require a database migration.
- JWT remains in memory only.
- Latest confirmed EF migration becomes `AddPaddleWebhookEvents`.
- CMS UI regression guard: static admin shell audit covers tab IDs, user search forms, premium controls, free lesson reset controls, audit controls, endpoint constants, and memory-only JWT guard.
- This guard is UI/testing only; it does not change backend endpoints, migrations, Paddle/webhooks, or desktop UI.

## Billing Provider Adapter Foundation v1

- Added a provider-agnostic checkout adapter interface foundation in backend billing services.
- Existing endpoint remains `POST /api/me/billing/checkout-session`.
- Existing public API behavior remains disabled/provider-not-configured by default.
- No real checkout session is created.
- No external provider call is made.
- No Paddle checkout integration was added.
- Paddle webhook ingestion foundation v1 is now added separately and only stores signed raw events for idempotency.
- No subscription/payment/entitlement/billing event mutation was added.
- No database migration was required.
- Latest confirmed EF migration becomes `AddPaddleWebhookEvents`.
- This prepares the backend for a future Paddle adapter without coupling the core checkout endpoint to Paddle.

## Billing checkout smoke test

- `tools/smoke_billing_checkout.ps1` verifies the current safe billing checkout skeleton.
- It checks unauthenticated 401, invalid plan 400, unsupported plan 400, and premium disabled/provider-not-configured response.
- It does not create a real checkout session.
- It does not call Paddle or any external provider.
- It does not mutate subscriptions, entitlements, payments, or billing events.
- It requires a running local backend.
- No database migration is required.
- Latest confirmed EF migration becomes `AddPaddleWebhookEvents`.

## Paddle checkout adapter smoke test

- `tools/smoke_paddle_checkout_adapter.ps1` verifies the safe `provider=paddle` adapter path.
- It requires a local backend started with `Billing__CheckoutEnabled=true` and `Billing__Provider=paddle`.
- It expects `PaddleBilling__CheckoutAdapterEnabled=false`.
- It verifies `provider=paddle`, `created=false`, `checkoutEnabled=false`, empty `checkoutUrl`, and `paddle_checkout_not_configured`.
- It does not create a real checkout session.
- It does not call Paddle or any external provider.
- It does not mutate subscriptions, entitlements, payments, or billing events.
- No database migration is required.
- Latest confirmed EF migration becomes `AddPaddleWebhookEvents`.

## Paddle Checkout Adapter Configuration Foundation

- Added `PaddleBilling` options with safe non-secret defaults (`CheckoutAdapterEnabled=false`, `Environment=sandbox`, empty `ApiKey`, empty `PremiumPriceId`).
- Added a provider-agnostic Paddle checkout adapter registration path so `IBillingProviderCheckoutAdapterResolver` can resolve `paddle` safely.
- The Paddle adapter now calls Paddle only when explicitly enabled and fully configured.
- The Paddle adapter can create real Paddle checkout transactions and return `checkoutUrl`; webhook ingestion foundation v1 stores signed raw events, while entitlement activation remains deferred.
- Paddle webhook ingestion foundation v1 is now added separately and only stores signed raw events for idempotency.
- No subscription/payment/entitlement/billing event mutation was added.
- No database migration was required.
- Latest confirmed EF migration becomes `AddPaddleWebhookEvents`.
- Webhook ingestion idempotency is now implemented in `paddle_webhook_events`; entitlement reconciliation remains deferred.
- Do not put Paddle secrets in appsettings; use user secrets, environment variables, or secure deployment configuration.

## Paddle checkout transaction creation v1

- Backend can create a Paddle sandbox/live checkout transaction when explicitly configured.
- Existing endpoint remains `POST /api/me/billing/checkout-session`.
- Real Paddle checkout is controlled by all of these settings being supplied outside client code:
  - `Billing__CheckoutEnabled=true`
  - `Billing__Provider=paddle`
  - `PaddleBilling__CheckoutAdapterEnabled=true`
  - `PaddleBilling__Environment=sandbox` or `PaddleBilling__Environment=live`
  - `PaddleBilling__ApiKey=<secret>`
  - `PaddleBilling__PremiumPriceId=<price id>`
- Paddle API keys must be stored in environment variables, user secrets, or secure deployment configuration; never store them in `appsettings.json` or client code.
- This step creates a Paddle transaction through the backend and returns `checkoutUrl` only.
- Paddle webhook ingestion foundation v1 is implemented separately and only stores signed raw events for idempotency.
- This step does not activate Premium.
- This step does not create internal billing events.
- This step does not mutate subscriptions, entitlements, payments, or billing event tables.
- No database migration was required.
- Latest confirmed EF migration becomes `AddPaddleWebhookEvents`.
- Entitlement activation foundation v1 is implemented separately from checkout transaction creation and can activate Premium only after webhook ingestion, normalization, and reconciliation decision processing.
- Optional real sandbox smoke script: `tools/smoke_paddle_checkout_live_sandbox.ps1`.
- The optional real sandbox smoke requires `-AllowRealPaddleCall` and creates a real Paddle sandbox transaction only; it does not complete payment, call webhooks, or check entitlement activation.

## Billing event normalization foundation v1

- Paddle webhook ingestion now normalizes accepted signed Paddle events into provider-agnostic `billing_events` rows immediately after the raw webhook event is safely stored.
- Normalization is idempotent through the existing unique `billing_events` constraint on provider + provider event ID (`BillingProvider` + `ProviderEventId`).
- `paddle_webhook_events` remains the raw signed event ingestion table and stores the original signed webhook payload separately from the normalized billing stream.
- `billing_events` is the provider-agnostic event stream for future reconciliation and stores only safe metadata, not raw payloads, signatures, or secrets.
- No Premium activation is performed in this step.
- No entitlement, subscription, or payment mutation is performed in this step.
- No final entitlement reconciliation is performed in this step.
- No external Paddle calls are made by normalization.
- No database migration was required because the existing `BillingEventEntity` and `PaddleWebhookEventEntity` schema was sufficient.
- Latest confirmed EF migration remains `20260528000000_AddPaddleWebhookEvents`.


## Entitlement reconciliation decision foundation v1

- Normalized `billing_events` are now inspected for future entitlement reconciliation eligibility.
- Paddle `transaction.completed` events with safe metadata containing `internalUserId` and `internalPlanId=premium` are marked `reconciliation_pending`.
- Unsupported billing event types are marked `ignored`.
- Invalid or missing safe metadata is marked `reconciliation_blocked`.
- This step does not activate Premium.
- This step does not create entitlements.
- This step does not mutate subscriptions or payments.
- This step does not call Paddle.
- This step does not add final reconciliation.
- Raw webhook events remain in `paddle_webhook_events`.
- Provider-agnostic event decisions live in `billing_events`.
- No database migration was required because no schema changed.
- Latest confirmed EF migration remains `20260528000000_AddPaddleWebhookEvents`.
