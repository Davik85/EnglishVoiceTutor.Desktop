# Billing Remaining Operations Plan

Review date: 2026-05-29.

Status: planning only, not implemented.

The current Paddle billing lifecycle foundation is completed through Step 3C. This document plans the remaining billing operations and intentionally does not implement backend, desktop, Admin UI, database, configuration, smoke-script, or test changes.

## 1. Title and status

- Title: Billing Remaining Operations Plan.
- Review date: 2026-05-29.
- Status: planning only, not implemented.
- Current lifecycle foundation: completed through Step 3C.

## 2. Non-negotiable architecture boundaries

- English Voice Tutor is global, cross-platform, and provider-agnostic.
- Paddle is the current desktop/web provider adapter.
- Backend is the source of truth for billing state, access state, account state, subscription status, payments, usage, and entitlements.
- `EntitlementEntity` remains the source of Premium access.
- `SubscriptionEntity` is a snapshot only and must not grant Premium access by itself.
- `PaymentEntity` is diagnostic payment history only and must not grant Premium access by itself.
- Desktop and future mobile clients must rely on backend access/status endpoints.
- Do not introduce YooKassa, Russia-only, or provider-specific business logic assumptions.

## 3. Current completed baseline

The completed Step 3C baseline includes:

- checkout skeleton and Paddle checkout adapter behind explicit configuration;
- webhook ingestion that verifies `Paddle-Signature`, stores raw Paddle events, and handles disabled/missing-secret/signature-failure cases safely;
- provider-agnostic billing event normalization into `billing_events` after durable raw event ingestion;
- subscription snapshots for `subscription.created`, `subscription.updated`, and `subscription.past_due`;
- payment snapshots for `transaction.completed` and `transaction.payment_failed`;
- entitlement activation/extension from valid `transaction.completed` events mapped to Premium;
- scheduled cancellation policy that records cancellation-at-period-end metadata without early Premium revocation;
- `past_due` policy that records subscription status without entitlement creation, extension, or revocation;
- actual `subscription.canceled` and `subscription.paused` policy that expires only active `provider_event` Premium entitlements for the resolved internal user/provider subscription context;
- current Paddle smoke scripts covering checkout/webhook ingestion, event normalization, subscription snapshots, payment snapshots, entitlement activation/extension, scheduled cancellation, past-due behavior, and actual canceled/paused expiry behavior.

## 4. Remaining operation A: subscription.resumed / subscription.activated restore policy

Plan:

- `subscription.resumed` should update the `SubscriptionEntity` snapshot/status to active/resumed when received.
- `subscription.activated` should update the `SubscriptionEntity` snapshot/status to active when received.
- Neither event should grant Premium by itself in the first implementation slice.
- Premium restoration should happen only through valid `transaction.completed` because Paddle resume/activation flows include transaction events when billing is collected.
- If a future business rule needs grace access on resume before `transaction.completed`, it must be a separate explicit product decision.

Future smoke coverage should verify:

- resumed updates the subscription snapshot;
- activated updates the subscription snapshot;
- resumed/activated alone do not create Premium;
- a following valid `transaction.completed` restores or extends Premium.

## 5. Remaining operation B: refunds and chargebacks policy

Plan:

- Paddle financial adjustments should be handled via `adjustment.created` and `adjustment.updated` events.
- Adjustment action values may include `refund`, `chargeback`, `chargeback_reverse`, `credit`, and `chargeback_warning`.
- Do not immediately revoke Premium on every adjustment.
- Full refund of the current paid period may expire `provider_event` Premium entitlement, but only after careful mapping to the related provider transaction/subscription and approved/completed adjustment status.
- Partial refund should usually not revoke Premium automatically unless a product policy explicitly says so.
- `chargeback` should likely expire `provider_event` Premium entitlement and create a support/audit flag, but must not touch trial/manual/admin/development/future-mobile entitlements.
- `chargeback_reverse` should not automatically restore Premium until a policy is approved; restoration may require a new valid `transaction.completed` or manual/admin action.
- Store adjustment diagnostics separately or extend `PaymentEntity` only if the data model supports it cleanly.
- Do not implement this before a separate adjustment data model/smoke plan is approved.

## 6. Remaining operation C: manual revocation automation policy

Plan:

- Local admin manual Premium revoke currently exists.
- Future automation should define:
  - who/what can revoke;
  - reason required;
  - audit log required;
  - only the targeted entitlement source should be affected;
  - never revoke trial/manual/admin/provider/future-mobile entitlements accidentally.
- Manual admin revocation should remain separate from provider webhook automation.

## 7. Remaining operation D: production Paddle webhook setup checklist

Plan:

- Use the real Paddle notification destination secret only in secure environment/deployment configuration.
- Never commit real Paddle API keys, webhook secrets, price ids, customer ids, or transaction ids.
- Configure the notification destination for required event types.
- Confirm sandbox first, then production.
- Verify signature failures return `401`.
- Verify disabled webhook returns `404`.
- Verify missing secret returns `503`.
- Define a rotation procedure for webhook secret/API key.
- Define an incident procedure if a secret is exposed.

## 8. Remaining operation E: desktop upgrade/paywall UI plan

Plan:

- Desktop should not decide Premium locally.
- Desktop should call backend access/status endpoints.
- Paywall should be driven by backend denial/status.
- Checkout session should be requested from backend.
- Desktop should open `checkoutUrl` but not activate Premium locally.
- Desktop should refresh backend subscription/access status after checkout completion.

UX states:

- Free with allowance remaining;
- Free allowance used;
- Trial active;
- Premium active;
- Past due;
- Canceled/paused;
- Checkout unavailable.

## 9. Remaining operation F: future Apple/Google mobile entitlement bridge

Plan:

- Future mobile purchases must map to the same backend account and entitlement model.
- Apple/Google should be provider adapters, not separate access systems.
- Backend should normalize app store purchase/renewal/cancellation events into provider-agnostic entitlements.
- Mobile clients must not decide Premium locally.
- Need a separate future design for receipt validation / server notifications / entitlement reconciliation.
- Do not implement now.

## 10. Remaining operation G: optional background reconciliation job

Plan:

- Current webhook flow is event-scoped.
- Future reconciliation job could verify local state against provider state for missed/out-of-order events.
- Keep it provider-adapter-based.
- Do not make it required for normal lesson access.
- Add idempotent repair behavior only after plan approval.

## 11. Recommended implementation order

- Step 4A: this planning document only.
- Step 4B: `subscription.resumed` / `subscription.activated` snapshot-only handling.
- Step 4C: production Paddle webhook setup checklist and dry-run validation.
- Step 4D: desktop upgrade/paywall UI plan.
- Step 4E: refund/chargeback adjustment model design.
- Step 4F: background reconciliation job design.
- Step 4G: Apple/Google mobile entitlement bridge design.

## 12. Explicit non-goals

- No code implementation in this task.
- No production Paddle credentials.
- No refund/chargeback automation yet.
- No app store entitlement bridge yet.
- No desktop paywall implementation yet.
- No full reconciliation job yet.

## 13. Verification checklist

Future implementation phases should include a short verification checklist with:

- EF migrations list;
- database update;
- pending model changes;
- lesson content audit;
- desktop Debug/Release build;
- backend build;
- relevant Paddle smoke scripts;
- git status.
