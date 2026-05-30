# Paddle Production Readiness Checklist

Review date: 2026-05-30

Status: production planning/checklist only; production billing is not yet verified.

## Purpose

This checklist describes what must be prepared, configured, and verified before production Paddle billing can be considered ready for English Voice Tutor. It is a production-readiness planning document only. It does not change backend behavior, desktop behavior, Admin UI behavior, database schema, smoke scripts, configuration defaults, or billing entitlement rules.

Production readiness must preserve the current architecture:

- Backend remains the source of truth for Premium/free/trial/usage/access.
- `EntitlementEntity` remains the source of Premium access.
- `SubscriptionEntity` is a provider-agnostic subscription snapshot and is not an access source.
- `PaymentEntity` is diagnostic payment history only.
- Desktop never decides Premium locally.
- Desktop never activates Premium locally.
- Paddle stays behind the backend/provider adapter.
- Future mobile clients must rely on the same backend account/subscription/entitlement state.

## What is already validated in sandbox

The following sandbox loop has already been validated:

- Desktop Upgrade panel works.
- Backend `checkout-session` works.
- Backend-hosted checkout launch page works.
- Paddle Checkout opens.
- Sandbox `transaction.completed` webhook activates Premium.
- Desktop **Refresh status** shows Premium active.
- Lesson starts as Premium after backend reports Premium active.

## What production readiness does NOT mean yet

Production billing is not ready just because the sandbox loop works. The following remain true until explicitly completed and verified in live production:

- Production billing is not complete just because sandbox works.
- Production webhook destination still needs live setup.
- Production checkout settings still need live setup.
- Production secrets must not be committed.
- Refunds/chargebacks are not implemented yet.
- Apple/Google mobile entitlement bridge is not implemented yet.

## Required live Paddle items

Use placeholders only in documentation, tickets, screenshots, logs, and examples. Do not paste real values into the repository.

| Item | Source | Placeholder only |
| --- | --- | --- |
| Live Paddle API key | Paddle Dashboard live environment API credentials | `<production Paddle API key>` |
| Live Premium Price ID | Paddle Dashboard live product/price configuration | `<production Premium price id>` |
| Live Client-side token | Paddle Dashboard live client-side token configuration | `<production client-side token>` |
| Live Notification Destination secret | Paddle Dashboard live notification destination endpoint secret | `<production notification destination secret>` |
| Live Paddle notification destination URL | Production backend public HTTPS route configured in Paddle Notifications | `https://YOUR_PRODUCTION_DOMAIN/api/billing/webhooks/paddle` |
| Approved production checkout/payment domain, if required by Paddle setup | Paddle Dashboard domain/checkout/payment settings | `<approved production checkout/payment domain>` |
| Production backend public HTTPS URL | Production hosting/DNS/TLS setup | `https://YOUR_PRODUCTION_DOMAIN` |

Do not include real Paddle API keys, client-side tokens, webhook secrets, price ids, customer ids, transaction ids, OpenAI keys, or secret-bearing URLs in docs/code/tests/log examples.

## Required production environment variables

Store production values only in secure server environment configuration or a production secrets manager. Do not commit production values to tracked files.

```text
ASPNETCORE_ENVIRONMENT=Production
DOTNET_ENVIRONMENT=Production
ASPNETCORE_URLS=<production ASP.NET Core bind URL>
SubscriptionEnforcement__Enabled=true
PaddleWebhook__Enabled=true
PaddleWebhook__SecretKey=<production notification destination secret>
PaddleWebhook__TimestampToleranceSeconds=300
Billing__CheckoutEnabled=true
Billing__Provider=paddle
PaddleBilling__CheckoutAdapterEnabled=true
PaddleBilling__Environment=live
PaddleBilling__ApiKey=<production Paddle API key>
PaddleBilling__PremiumPriceId=<production Premium price id>
PaddleBilling__ClientSideToken=<production client-side token>
```

Production value rules:

- Do not use `test_webhook_secret` in production.
- Do not use a sandbox API key in production.
- Do not use a sandbox price id in production.
- Do not use a sandbox client-side token in production.

## Production Paddle notification destination checklist

1. Open Paddle Dashboard -> Developer tools -> Notifications.
2. Create a live webhook destination.
3. Use the production backend HTTPS URL:

   ```text
   https://YOUR_PRODUCTION_DOMAIN/api/billing/webhooks/paddle
   ```

4. Select the required event types:
   - `transaction.completed`
   - `transaction.payment_failed`
   - `subscription.created`
   - `subscription.updated`
   - `subscription.past_due`
   - `subscription.canceled`
   - `subscription.paused`
   - `subscription.resumed`
   - `subscription.activated`
5. Save the endpoint secret immediately.
6. Store the secret only in secure server environment configuration.
7. Do not paste the secret into chat, docs, screenshots, logs, or commits.

## Production checkout checklist

- Paddle live product exists.
- Paddle live price exists.
- Price ID matches the Premium plan.
- Backend uses internal plan id `premium` and provider price id only in backend configuration.
- Desktop does not contain Paddle price id.
- Backend-hosted checkout launch page uses production client-side token.
- Backend uses `PaddleBilling__Environment=live`.
- Checkout page uses backend-provided transaction id.
- Checkout creation does not activate Premium.

## Safe production verification sequence

1. Deploy backend.
2. Set secure environment variables.
3. Run config guard if applicable.
4. Confirm backend starts.
5. Confirm webhook endpoint behavior: missing signature should return `401`, not `404`.
6. Create a low-risk production test transaction only when ready.
7. Confirm Paddle delivery logs show `200 OK` for `transaction.completed`.
8. Confirm backend logs show `transaction.completed` accepted and Premium entitlement activated.
9. Confirm desktop **Refresh status** shows Premium active.
10. Confirm lesson starts as Premium.
11. Confirm no secrets appear in logs.

## Failure diagnosis table

| Symptom | Simple explanation |
| --- | --- |
| `404` from webhook | Endpoint disabled, wrong URL, or wrong route. |
| `401` from webhook | Missing/invalid signature or wrong secret. |
| `503` from webhook | Webhook enabled but secret/config missing. |
| Checkout unavailable | Missing API key, price id, client-side token, or checkout adapter disabled. |
| Premium not active after payment | `transaction.completed` not delivered, blocked, or not mapped to internal user/plan. |
| Lesson still blocked after Premium | Backend status/access issue; check `/api/me/lesson-access`. |

## Required pre-production checks

Use environment-appropriate commands. The commands below are placeholders/check categories and must be adapted to the production release environment without adding secrets to shell history, logs, docs, or commits.

```bash
# Repository state
git status

# EF migrations list
<run EF migrations list command for the backend project/startup project>

# Database update
<run EF database update command against the intended production database only when approved>

# Pending model changes
<run EF pending model changes command for the backend project/startup project>

# Lesson content audit
<run audit_lesson_content script>

# Desktop Debug build
<run desktop Debug build command>

# Desktop Release build
<run desktop Release build command>

# Backend build
<run backend build command>

# Production config guard
<run production Paddle config guard, if applicable>

# Webhook endpoint sanity check
<send unsigned request to production webhook endpoint and confirm 401, not 404>

# No secrets scan
<run repository/log/config scan for accidental real secrets before release>
```

## Security rules

- Rotate any secret exposed in chat/screenshots/commit.
- Never commit `appsettings` files with real Paddle/OpenAI secrets.
- Keep production and sandbox values separate.
- Do not mix fake smoke webhook secret with real Paddle webhook secret.
- Do not use production keys in fake local smoke scripts.

## Deferred production scope

The following production billing/operations scope is deferred and not implemented by this checklist:

- Refunds/chargebacks not implemented.
- Manual revocation automation not implemented.
- Full reconciliation/background job not implemented.
- Automatic polling after checkout not implemented.
- Mobile Apple/Google entitlement bridge not implemented.
- Production RBAC/admin not implemented.
