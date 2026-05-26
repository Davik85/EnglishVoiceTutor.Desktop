# Subscription & Billing Foundation (Current State)

This document describes the **current implemented foundation** for account, trial, subscription, entitlement, free-limit enforcement, development test accounts, and billing checkout skeleton behavior.

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
  1. lesson session has started;
  2. learner sends at least 3 valid user messages.
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

## Roadmap (recommended order)

1. CMS/admin foundation:
   - roles
   - audit trail
   - user lookup
   - manual Premium grant/revoke
   - free allowance reset
   - entitlement inspection
2. Provider-agnostic billing provider adapter interface.
3. Paddle checkout adapter.
4. Paddle webhook ingestion.
5. Billing event persistence and idempotency checks.
6. Entitlement reconciliation.
7. Desktop upgrade/paywall UI.
8. Future Apple/Google mobile entitlement bridge.

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

Do not use real tokens or secrets in shared docs/scripts.
