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
- Latest confirmed EF migration remains `20260524061817_AddSubscriptionFoundationV1`.
- Paddle checkout/webhooks are still not implemented.

## Deferred / Not implemented yet

- No production admin roles/RBAC yet.
- No production admin deployment/security hardening yet.
- No Paddle checkout integration yet.
- No Paddle webhook ingestion yet.
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
- Latest migration remains `20260524061817_AddSubscriptionFoundationV1`.

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
- Latest confirmed EF migration remains `20260524061817_AddSubscriptionFoundationV1`.

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
- Latest confirmed EF migration remains `20260524061817_AddSubscriptionFoundationV1`.
- No Paddle checkout/webhooks were added.
- It writes an audit action via `AdminAuditService` / `admin_actions`.
- It does not delete lesson sessions or lesson messages.
- It does not reset `DailyUsageCounters` or old per-operation usage counters.
- It does not change Premium/trial/subscription/payment records.
- It does not add CMS UI.
- It does not add Paddle checkout/webhooks.
- It does not require a database migration.
- Latest confirmed EF migration remains `20260524061817_AddSubscriptionFoundationV1`.

## Admin Foundation v9 (local smoke test script)

- Admin Foundation v9 adds a local PowerShell smoke test script.
- Script: `tools/smoke_admin_foundation.ps1`.
- It verifies existing Admin Foundation v1-v8 behavior against a running Development backend.
- It does not start the backend automatically.
- It does not add backend behavior.
- It does not add CMS UI.
- It does not add Paddle checkout/webhooks.
- It does not require a database migration.
- Latest confirmed EF migration remains `20260524061817_AddSubscriptionFoundationV1`.

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
- Latest confirmed EF migration remains `20260524061817_AddSubscriptionFoundationV1`.


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
- Latest confirmed EF migration remains `20260524061817_AddSubscriptionFoundationV1`.

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
- Latest confirmed EF migration remains `20260524061817_AddSubscriptionFoundationV1`.

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
- Latest confirmed EF migration remains `20260524061817_AddSubscriptionFoundationV1`.


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
- Latest confirmed EF migration remains `20260524061817_AddSubscriptionFoundationV1`.

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
- Latest confirmed EF migration remains `20260524061817_AddSubscriptionFoundationV1`.

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
- Latest confirmed EF migration remains `20260524061817_AddSubscriptionFoundationV1`.

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
- Latest confirmed EF migration remains `20260524061817_AddSubscriptionFoundationV1`.
- CMS UI regression guard: static admin shell audit covers tab IDs, user search forms, premium controls, free lesson reset controls, audit controls, endpoint constants, and memory-only JWT guard.
- This guard is UI/testing only; it does not change backend endpoints, migrations, Paddle/webhooks, or desktop UI.

## Billing Provider Adapter Foundation v1

- Added a provider-agnostic checkout adapter interface foundation in backend billing services.
- Existing endpoint remains `POST /api/me/billing/checkout-session`.
- Existing public API behavior remains disabled/provider-not-configured by default.
- No real checkout session is created.
- No external provider call is made.
- No Paddle checkout integration was added.
- No Paddle webhook ingestion was added.
- No subscription/payment/entitlement/billing event mutation was added.
- No database migration was required.
- Latest confirmed EF migration remains `20260524061817_AddSubscriptionFoundationV1`.
- This prepares the backend for a future Paddle adapter without coupling the core checkout endpoint to Paddle.

## Billing checkout smoke test

- `tools/smoke_billing_checkout.ps1` verifies the current safe billing checkout skeleton.
- It checks unauthenticated 401, invalid plan 400, unsupported plan 400, and premium disabled/provider-not-configured response.
- It does not create a real checkout session.
- It does not call Paddle or any external provider.
- It does not mutate subscriptions, entitlements, payments, or billing events.
- It requires a running local backend.
- No database migration is required.
- Latest confirmed EF migration remains `20260524061817_AddSubscriptionFoundationV1`.
