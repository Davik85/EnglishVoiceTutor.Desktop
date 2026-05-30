# Paddle Production Webhook Setup

Review date: 2026-05-29.

Status: setup checklist / not a secret store / not production deployment proof.

## Purpose

This document describes how to configure a Paddle notification destination safely for sandbox and production, and how to validate local backend readiness without committing secrets. It is an operational setup checklist only; it does not prove that production delivery is working and it must not contain real Paddle credentials, secrets, identifiers, or secret-bearing URLs.

## Architecture boundaries

- English Voice Tutor is global, cross-platform, and provider-agnostic.
- Paddle is the current desktop/web provider adapter.
- Backend remains the source of truth for account, trial, subscription, Premium/free status, usage, limits, lesson history, payments, and entitlements.
- `EntitlementEntity` remains the access source for Premium.
- `SubscriptionEntity` is a provider-agnostic snapshot only and must not grant Premium by itself.
- `PaymentEntity` is diagnostic payment history only and must not grant Premium by itself.
- Desktop and future mobile clients must rely on backend access/status endpoints; they do not decide Premium locally.
- Do not put secrets in the repository, chat, screenshots, logs, documentation, committed configuration, or committed scripts.

## Required backend endpoint

Configure Paddle to send webhook notifications to:

```text
POST /api/billing/webhooks/paddle
```

Endpoint requirements and expected safety behavior:

- The endpoint is protected by `Paddle-Signature`, not JWT.
- The raw request body must be verified before JSON parsing.
- A disabled webhook endpoint returns `404`.
- A missing configured webhook secret returns `503`.
- Missing, invalid, or stale signatures return `401`.
- Valid Paddle events are stored in `paddle_webhook_events` and then normalized to provider-agnostic `billing_events`.

## Required Paddle notification destination event types

Subscribe the Paddle notification destination to these current event types:

- `transaction.completed`
- `transaction.payment_failed`
- `subscription.created`
- `subscription.updated`
- `subscription.past_due`
- `subscription.canceled`
- `subscription.paused`
- `subscription.resumed`
- `subscription.activated`

Not required yet:

- `adjustment.created` and `adjustment.updated` are not required yet because refund and chargeback handling is deferred to a later approved plan.
- Customer events are not required for the current access lifecycle unless a future support or Admin UI requirement adds them.

## Sandbox-first procedure

1. Configure a Paddle sandbox notification destination first.
2. Use an HTTPS public backend URL ending in `/api/billing/webhooks/paddle`.
3. Subscribe only the required event types listed above unless a later approved plan expands the lifecycle.
4. Copy the Paddle notification destination secret into secure environment configuration only.
5. Keep `PaddleBilling__Environment=sandbox` for sandbox validation.
6. Verify delivery with the Paddle simulator or sandbox transactions.
7. Run local/backend smoke scripts with a test secret and non-production data.
8. Run the local config guard before promoting configuration shape to production.
9. Never paste a real notification destination secret into chat and never commit it.

## Production procedure

1. Repeat the same notification destination setup in Paddle production only after sandbox success.
2. Use the production backend HTTPS endpoint ending in `/api/billing/webhooks/paddle`.
3. Store the production notification destination secret only in secure deployment configuration.
4. Store the production Paddle API key and Premium price id only in secure deployment configuration.
5. Verify disabled-webhook, missing-secret, and invalid-signature behavior in safe environments before relying on production traffic.
6. Run the config guard in strict production-readiness mode against the deployment environment shape without printing secrets.
7. Verify a real signed production event only after the backend deployment is ready and monitored.
8. Keep a rollback and secret-rotation procedure ready before enabling live customer traffic.

## Environment variables / config names

Use placeholders only in documentation and committed examples:

```text
PaddleWebhook__Enabled=true
PaddleWebhook__SecretKey=<secure notification destination secret>
PaddleWebhook__TimestampToleranceSeconds=300
Billing__CheckoutEnabled=true
Billing__Provider=paddle
PaddleBilling__CheckoutAdapterEnabled=true
PaddleBilling__Environment=sandbox|live
PaddleBilling__ApiKey=<secure Paddle API key>
PaddleBilling__PremiumPriceId=<secure Paddle price id>
PaddleBilling__ClientSideToken=<public Paddle client-side token>
```

Warnings:

- Do not put real values in `appsettings.json`, README files, documentation, screenshots, chat, or committed scripts.
- Do not print real Paddle API keys, webhook secrets, price ids, customer ids, transaction ids, or production URLs that include secrets.
- The desktop app opens the backend-hosted `/checkout/paddle` launch page returned by `POST /api/me/billing/checkout-session`; it does not call Paddle directly.
- The launch page uses Paddle.js and `PaddleBilling__ClientSideToken` to open checkout for the Paddle transaction id.
- Premium still activates only after a valid `transaction.completed` webhook is ingested and reconciled.
- Use secure environment/deployment configuration supplied outside source control.

## Local config guard

Run the safe local guard script before sandbox or production setup checks. The script does not call Paddle and does not prove production delivery.

```powershell
powershell -ExecutionPolicy Bypass -File tools\smoke_paddle_production_config_guard.ps1
powershell -ExecutionPolicy Bypass -File tools\smoke_paddle_production_config_guard.ps1 -Strict
powershell -ExecutionPolicy Bypass -File tools\smoke_paddle_production_config_guard.ps1 -Strict -AssumeProduction
```

Expected behavior:

- Secret-like values are reported only as `set` or `missing`.
- Non-secret booleans may print their actual values.
- `PaddleBilling__Environment` may print `sandbox` or `live`.
- Live/production mode fails on missing secret-like values or the local test webhook secret placeholder.
- Non-strict local development mode prints guidance instead of failing when Paddle variables are absent.

## Secret rotation and incident response

If a webhook secret or API key is exposed:

1. Rotate or revoke the exposed value immediately in Paddle.
2. Update secure environment/deployment configuration with the replacement value.
3. Restart the backend or redeploy so the replacement configuration is loaded.
4. Rerun the config guard and local webhook smoke scripts using safe test data where appropriate.
5. Audit repository history if a secret was committed.
6. Treat screenshots, logs, chat messages, and support transcripts as possible disclosure channels if they included the secret.

## Validation checklist

Before considering a production Paddle webhook setup ready, verify:

- Backend health/startup.
- EF migrations list.
- Database update.
- Pending model changes check.
- `audit_lesson_content`.
- Desktop Debug build.
- Desktop Release build.
- Backend build.
- Local Paddle webhook smoke scripts with a test secret.
- Optional Paddle sandbox simulator or manual sandbox transaction.
- `git status` is clean after committed setup documentation/tooling changes.

## Production readiness limitations

- Production Paddle webhook setup is not proven by local smokes alone.
- Refunds and chargebacks are not implemented yet.
- Desktop paywall UI is not implemented yet.
- Apple/Google bridge is not implemented yet.
- Background reconciliation job is not implemented yet.
- This checklist and guard do not change billing lifecycle behavior.
