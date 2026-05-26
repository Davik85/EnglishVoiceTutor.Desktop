# Subscription & Billing Foundation (Current State)

This document describes the **current implemented foundation** for account, trial, subscription, entitlement, free-limit enforcement, development test accounts, billing checkout skeleton behavior, and backend-only admin bootstrap diagnostics.

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

## Deferred / Not implemented yet

- No Paddle checkout integration yet.
- No Paddle webhook ingestion yet.
- No Apple App Store / Google Play integration yet.
- No real payment acceptance yet.
- No CMS/admin panel yet.
- No production admin roles yet.
- No provider reconciliation job yet.
- No desktop paywall/upgrade UI yet.
- No manual grant/revoke/reset actions yet.

## Roadmap (recommended order)

1. CMS/admin foundation:
   - Roles.
   - Audit trail.
   - User lookup.
   - Manual Premium grant/revoke.
   - Free allowance reset.
   - Entitlement inspection.
2. Provider-agnostic billing provider adapter interface.
3. Paddle checkout adapter.
4. Paddle webhook ingestion.
5. Billing event persistence and idempotency checks.
6. Entitlement reconciliation.
7. Desktop upgrade/paywall UI.
8. Future Apple/Google mobile entitlement bridge.

## Admin Foundation v1 (backend-only bootstrap)

- Admin Foundation v1 is backend-only.
- Access is Development/config bootstrap admin access (`AdminBootstrap:Enabled` with configured bootstrap emails).
- Endpoint surface currently includes only: `GET /api/admin/me`.
- `GET /api/admin/me` requires a valid Bearer token and bootstrap admin authorization and returns a read-only self-check payload.
- No CMS/admin UI is implemented.
- No user search is included in v1.
- No manual grant/revoke/reset actions are implemented.
- Paddle checkout and webhooks are deferred.
- `admin_actions` storage exists for future audited admin mutations, but `/api/admin/me` does **not** write audit actions.

## Admin Foundation v2 (backend-only exact user lookup)

- Admin Foundation v2 is backend-only and read-only.
- Added endpoint: `GET /api/admin/users/by-email?email=user@example.com`.
- Endpoint is protected by `AdminAuthorizationConstants.BootstrapAdminPolicyName`.
- Lookup is exact by normalized email (trim + lower-invariant), with no broad search or partial matching.
- Returns a safe response containing user overview, profile/settings snapshot, subscription status, and checked timestamp.
- No CMS/admin UI is implemented.
- No manual grant/revoke/reset actions are implemented.
- No Paddle checkout/webhook behavior is introduced.
- No audit writes are performed for this read-only lookup.

- Latest confirmed EF migration after Admin Foundation v2 remains `20260524061817_AddSubscriptionFoundationV1`.
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
- Latest confirmed EF migration remains `20260524061817_AddSubscriptionFoundationV1`.

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
- Latest confirmed EF migration remains `20260524061817_AddSubscriptionFoundationV1`.
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
- Latest confirmed EF migration remains `20260524061817_AddSubscriptionFoundationV1`.

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
- Latest confirmed EF migration remains `20260524061817_AddSubscriptionFoundationV1`.
