# Paddle Live Readiness Review

Review date: 2026-06-25.

Scope: Language Voice Tutor / English Voice Tutor Desktop release path. This is review and planning only. It does not enable live Paddle, change server environment variables, run production Paddle transactions, add secrets, change entitlement semantics, change Desktop behavior, add EF migrations, or modify deployment scripts.

## Current verified production state used for this review

- Backend current: `/opt/languagevoicetutor/backend/releases/0.1.35-backend.74`.
- Previous rollback reference: `/opt/languagevoicetutor/backend/releases/0.1.35-backend.49`.
- `/health` is `200 OK`.
- `/api/health/database` is `200 OK`.
- Admin/Product Statistics shows tracked signed-in app/device records, successful payments total, and successful payments current month.
- Successful payment metrics are internal payment/billing-event metrics, separate from active Premium entitlement state.
- Production/live Paddle readiness remains deferred. Broad public production readiness is still not claimed.

## Reviewed files and areas

- Production planning docs: `docs/paddle-production-readiness-checklist.md`, `docs/paddle-production-webhook-setup.md`, `docs/subscription-billing-foundation.md`, `docs/billing-remaining-operations-plan.md`, `docs/CURRENT_STATE.md`, `docs/NEXT_STEPS.md`, `docs/RELEASE_READINESS_REVIEW.md`, `docs/SECURITY_RELEASE_REVIEW.md`, and `docs/BACKEND_SERVER_DEPLOYMENT.md`.
- Backend billing/Paddle code: checkout-session endpoint, Paddle checkout adapter, backend-hosted checkout launch page, Paddle webhook endpoint/signature verification/ingestion/normalization, billing-event reconciliation, payment persistence, subscription snapshot processing, entitlement activation, cancellation services, and billing/admin statistics services/tests.
- Desktop billing flow only for understanding: Account/Upgrade/Refresh/Cancel UI paths and backend checkout-session client paths.
- Policy/smoke coverage: Paddle smoke scripts, billing policy tests, logging/privacy policy tests, deployment policy tests, documentation source-of-truth policy, and Admin product statistics policy.

## Readiness matrix

| Area | Status | Notes |
| --- | --- | --- |
| Sandbox checkout flow | Ready | Validated path is Desktop Buy Premium -> backend checkout-session -> backend-hosted Paddle checkout -> sandbox payment -> webhook -> Premium -> Refresh status -> lesson allowed. |
| Backend `checkout-session` | Ready | Requires auth, validates Premium plan id, stays backend-owned, and does not activate Premium by checkout creation alone. |
| Backend-hosted checkout launch page | Ready | Uses backend-provided transaction id and configured Paddle client-side token; Desktop receives a backend URL rather than calling Paddle directly. |
| `transaction.completed` webhook handling | Ready for sandbox / needs live destination | Signature verification, ingestion, normalization, payment persistence, reconciliation decision, and entitlement activation exist; live Paddle notification destination still needs manual dashboard setup and secret configuration. |
| Premium entitlement activation | Ready | Valid provider billing events activate/extend provider-event Premium entitlements while preserving entitlement as the access source of truth. |
| Desktop Refresh status | Ready | Refresh remains backend-status driven and is part of the validated sandbox loop. |
| Lesson access after Premium | Ready | Backend lesson access honors active Premium/trial entitlements; Desktop does not decide Premium locally. |
| Payment statistics | Ready | Admin metrics now show successful payment totals from internal payment/billing-event records, separate from active Premium entitlement state. |
| Live Paddle API key | Missing / blocker | Must be created/stored in secure production server configuration only. |
| Live Premium price id | Missing / blocker | Must map the live Paddle price to the internal Premium plan through backend configuration only. |
| Live client-side token | Missing / blocker | Required for the backend-hosted checkout launch page in live mode. |
| Live webhook/notification destination secret | Missing / blocker | Required before live webhook payloads can be accepted. |
| Live notification destination URL | Missing / blocker | Must be configured in the live Paddle dashboard as `https://YOUR_PRODUCTION_DOMAIN/api/billing/webhooks/paddle`. |
| Production backend env flags | Missing / blocker | Live flags and values must be prepared, reviewed, and applied only when approved. |
| Production webhook route sanity checks | Risk / needs manual owner action | Route exists. Before live transaction testing, operators must confirm enabled route returns `401` for missing signature, not `404`; enabled-but-missing-secret must return safe failure and not accept payloads. |
| Refunds | Missing / deferred | No complete production refund operation/runbook is ready. |
| Chargebacks | Missing / deferred | No complete chargeback handling/reconciliation operation is ready. |
| Manual revocation | Ready for controlled support / risk | Admin manual Premium grant/revoke exists with audit expectations, but it is not a complete broad production finance/reconciliation process. |
| Customer portal/support path | Missing / blocker for broad paid launch | A customer-facing support/contact and portal handoff path must be finalized before public paid launch. |
| Cancellation support | Partially ready / risk | Current-user and admin cancel-renewal paths exist for cancel-at-period-end semantics; production support copy and operational process still need owner review. |
| Reconciliation | Missing / deferred | Provider event processing and payment persistence exist, but full finance reconciliation/background operations remain deferred. |
| Legal/support materials | Risk / needs manual owner action | Terms, privacy policy, refund policy, subscription cancellation disclosure, support contact path, and billing copy require owner/legal review before live paid launch. |
| Logs/privacy for live testing | Ready with operator discipline | Source logging avoids printing raw webhook payloads/signatures/tokens/API keys in operational logs, but raw webhook payload/signature are stored server-side for ingestion/audit and must never be pasted into docs/chat/tickets. |

## Required live Paddle server environment variable names

Use secure production server configuration or a secrets manager. Names only are listed here; values must use private placeholders during planning and real values only on the server.

```text
ASPNETCORE_ENVIRONMENT=<Production>
DOTNET_ENVIRONMENT=<Production>
ASPNETCORE_URLS=<production bind URL>
SubscriptionEnforcement__Enabled=<true>
PaddleWebhook__Enabled=<true>
PaddleWebhook__SecretKey=<production notification destination secret>
PaddleWebhook__TimestampToleranceSeconds=<positive integer, normally 300>
Billing__CheckoutEnabled=<true>
Billing__Provider=<paddle>
PaddleBilling__CheckoutAdapterEnabled=<true>
PaddleBilling__Environment=<live>
PaddleBilling__ApiKey=<production Paddle API key>
PaddleBilling__PremiumPriceId=<production Premium price id>
PaddleBilling__ClientSideToken=<production client-side token>
```

Do not commit, paste, screenshot, or print real values for these variables.

## Config guard and smoke coverage

A production config guard exists:

```powershell
pwsh -NoProfile -ExecutionPolicy Bypass -File tools/smoke_paddle_production_config_guard.ps1 -Strict -AssumeProduction
```

What it checks:

- Required Paddle webhook, checkout, provider, environment, API key, Premium price id, and client-side token variables are present when strict/production checks are requested.
- Secret-like values are reported only as `set` or `missing`; values are not printed.
- Live mode does not use the local test webhook secret placeholder.
- Timestamp tolerance is a positive integer.
- Checkout-enabled configuration uses provider `paddle`.
- Paddle checkout adapter environment is `sandbox` or `live`, and live-grade checks apply when `PaddleBilling__Environment=live` or `-AssumeProduction` is supplied.

This script does not call Paddle, prove webhook delivery, prove DNS/TLS, or prove that a live transaction succeeds.

## Safe webhook sanity checks

- Route: `POST /api/billing/webhooks/paddle`.
- If `PaddleWebhook__Enabled=false`, the endpoint intentionally returns `404`; this is suitable for disabled environments but is not proof of live readiness.
- If `PaddleWebhook__Enabled=true` and `PaddleWebhook__SecretKey` is missing, the endpoint returns `503` with a safe message and does not accept payloads.
- If `PaddleWebhook__Enabled=true` and a secret is configured, an unsigned request must return `401`, not `404`. This proves the production route is enabled and signature enforcement is active without accepting a payload.
- Do not send real Paddle payloads, signatures, secrets, customer data, or transaction ids in documentation or chat.

Example operator check after approved live-readiness configuration is staged on the server, using a harmless unsigned body:

```bash
curl -i -X POST https://YOUR_PRODUCTION_DOMAIN/api/billing/webhooks/paddle \
  -H 'Content-Type: application/json' \
  --data '{}'
```

Expected readiness result with webhook enabled and secret configured: HTTP `401`. A `404` means route disabled/wrong URL; a `503` means enabled but missing secret/config.

## Legal/support blockers

These are not legal advice and require owner/legal review before public paid launch:

| Item | Status | Required owner action |
| --- | --- | --- |
| Terms | Risk / needs manual owner action | Review and publish production terms appropriate for paid subscriptions. |
| Privacy policy | Risk / needs manual owner action | Review Paddle/payment data handling, account data, support logs, retention, and deletion process. |
| Refund policy | Missing / blocker | Define and publish refund policy and operational handling. |
| Subscription cancellation disclosure | Risk / needs manual owner action | Ensure billing UI/site copy explains renewal and cancellation-at-period-end behavior clearly. |
| Support email/contact path | Missing / blocker | Publish a working support contact path for billing issues. |
| App store/public website billing copy | Risk / needs manual owner action | Review any paid-plan, trial, cancellation, refund, and renewal claims before public traffic. |

## Proposed safe sequence for live readiness

1. Review live Paddle dashboard configuration: product, price, client-side token, notification destination, selected events, allowed domains, and support/business settings.
2. Prepare server environment placeholders in the approved secure configuration channel; do not paste real values into docs/chat/commits.
3. Run the config guard against the staged environment shape.
4. Enable live flags only after owner approval and legal/support blockers are acceptable for the intended test scope.
5. Run the unsigned webhook sanity check and confirm `401`, not `404`; confirm enabled-but-missing-secret is not used for transaction testing.
6. Run one low-risk live transaction only after legal/support blockers are acceptable and the owner approves the test.
7. Verify Paddle delivery logs show success for the live event without copying raw payloads/signatures.
8. Verify Admin payment stats, Premium entitlement, Desktop Refresh status, and Premium lesson access.
9. Verify production logs contain no secrets, raw webhook payloads, signatures, tokens, API keys, connection strings, SQL dumps, or raw customer personal data in shared evidence.
10. Record bounded non-secret evidence only: release path, endpoint status codes, aggregate counts, timestamps, and safe request/correlation ids if needed.

## Smallest safe next step

Do not turn on live Paddle immediately. The smallest safe next step is an owner-led live Paddle dashboard and legal/support readiness pass that produces only placeholders and decisions: confirm live product/price/client-side token/webhook destination can be prepared, choose the public support contact/refund/cancellation disclosure path, then run `tools/smoke_paddle_production_config_guard.ps1 -Strict -AssumeProduction` against a secret-safe staged environment.

## Intentionally deferred

- Enabling live Paddle flags on production.
- Running live Paddle transactions.
- Changing entitlement semantics, billing provider behavior, Desktop behavior, deployment scripts, EF migrations, or legal text.
- Full refunds, chargebacks, finance reconciliation automation, customer portal/support automation, mobile app store entitlement bridging, and broad public production readiness.
