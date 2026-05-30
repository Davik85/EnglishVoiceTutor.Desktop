# Billing Remaining Operations Plan

Review date: 2026-05-30.

Status: planning document only; not implementation.

This document records the remaining billing operations plan after the completed sandbox billing/paywall loop and the Paddle production readiness checklist. Production billing is not yet verified. This task does not implement backend, desktop, Admin UI, database, configuration, smoke-script, or test changes.

## 1. Current completed baseline

The current completed billing baseline includes:

- provider-agnostic checkout foundation;
- Paddle checkout transaction creation;
- backend-hosted checkout launch page;
- desktop Upgrade panel;
- desktop manual Refresh status;
- sandbox payment loop validated end-to-end: Upgrade -> backend checkout session -> backend-hosted Paddle checkout launch page -> Paddle Checkout -> sandbox payment -> `transaction.completed` webhook -> backend Premium activation -> desktop Refresh status -> Premium lesson access;
- `transaction.completed` activates or extends Premium through `EntitlementEntity`;
- subscription snapshots exist;
- payment diagnostic snapshots exist;
- canceled/paused expiry policy exists;
- resumed/activated events are snapshot-only;
- transient PostgreSQL serializable transaction conflicts are retried;
- fake Paddle webhook smokes pass;
- Step 4F documentation update is complete;
- Step 4G Paddle production readiness checklist exists at `docs/paddle-production-readiness-checklist.md`.

## 2. Non-negotiable boundaries

The following boundaries must remain true for every future billing operation:

- Backend remains the source of truth for Premium, free, trial, usage, and access decisions.
- `EntitlementEntity` is the only Premium access source.
- `SubscriptionEntity` does not grant Premium.
- `PaymentEntity` does not grant Premium.
- Desktop does not decide Premium.
- Desktop does not activate Premium locally.
- Checkout creation does not activate Premium.
- Paddle stays behind the backend/provider adapter.
- Production and sandbox configs must stay separate.
- Future mobile entitlement bridge must use the same backend account/subscription/entitlement model.
- Do not introduce YooKassa, Russia-only, or provider-specific access assumptions.
- Do not add real Paddle API keys, client-side tokens, webhook secrets, price IDs, customer IDs, transaction IDs, OpenAI keys, or secret-bearing URLs to docs, code, tests, or log examples.

## 3. Remaining operation: refunds

Plan only. Refund handling is not implemented by this document.

Paddle event types that likely matter include adjustment/refund-related events such as `adjustment.created` and `adjustment.updated`, but the exact event names, payload fields, statuses, and action values must be verified against current Paddle documentation before implementation.

Refunds must not be treated as normal cancellation automatically without an explicit product policy decision. A refund is a financial adjustment, while cancellation is a subscription lifecycle state; those concepts may overlap in some product decisions but should not be conflated by default.

Product decisions needed before implementation:

- Should a full refund revoke an existing `provider_event` Premium entitlement, shorten it to a specific timestamp, or leave access unchanged until manual review?
- Should a partial refund do nothing to access, shorten access proportionally, or mark the account/payment for admin review?
- What should happen if a refund is issued after the paid period was already used?
- Should refund behavior differ for first purchase, renewal, upgrade, downgrade, or goodwill refund scenarios?

Recommended first implementation after policy approval:

- persist the refund event safely and idempotently;
- do not immediately revoke access until the refund access policy is approved;
- add admin/audit visibility for refund diagnostics and decisions;
- add smoke tests after policy approval, including full refund, partial refund, duplicate event, and out-of-order event cases.

## 4. Remaining operation: chargebacks / disputes

Plan only. Chargeback/dispute handling is not implemented by this document.

Chargebacks and disputes are higher risk than refunds because they can indicate payment fraud, account takeover, provider risk, or payment network action. Paddle event types that likely matter may include dispute, chargeback, or adjustment-related events, but exact event names and payload semantics must be verified against current Paddle documentation before implementation.

Policy decisions needed before implementation:

- Should a confirmed chargeback immediately revoke the relevant `provider_event` Premium entitlement?
- Should a dispute merely mark the account for review until the dispute is resolved?
- Should accounts with unresolved or lost disputes be blocked from future checkout?
- Should a reversed/won dispute restore access automatically, require a new valid payment, or require manual/admin action?

Recommended first implementation after policy approval:

- persist dispute/chargeback events safely and idempotently;
- add audit/admin visibility for the event, linked account, linked provider subscription, linked provider transaction/payment, and current entitlement state;
- only then decide whether any automatic entitlement action is allowed.

## 5. Remaining operation: manual revocation automation

Plan only. Manual revocation automation is not implemented by this document.

Admin can already manually grant/revoke Premium. Future automation needs a policy for when provider events should trigger automatic revocation, shortening, hold, or review.

Rules for any future automation:

- Keep manual/admin/trial/development/future-mobile entitlements separate from `provider_event` entitlements.
- Do not let a Paddle provider event revoke non-provider entitlements.
- Any automatic provider-event revocation must target only the entitlement source and provider context that the policy explicitly allows.
- Every automatic revocation or shortening decision must be auditable with event id, provider, reason, old entitlement state, new entitlement state, and actor/system source.
- Manual/admin override behavior must be explicitly defined before automation is enabled.

## 6. Remaining operation: background reconciliation job

Plan only. A background reconciliation job is not implemented by this document.

A backend reconciliation job may be needed for:

- missed webhooks;
- delayed webhooks;
- out-of-order events;
- production incident recovery;
- validating local snapshots against provider state after operational problems.

Requirements for any future reconciliation job:

- It must be backend-only.
- It must not use desktop state.
- It must not directly grant Premium without strict validation of provider, account mapping, subscription/transaction identity, payment state, and product/price mapping.
- It must be safe, idempotent, and auditable.
- It should avoid broad destructive updates.
- It should record what was checked, what changed, and why.
- It can be deferred until after production webhook stability is proven.

## 7. Remaining operation: production setup sequence

Plan only. Production billing is not complete or verified by this document.

Use `docs/paddle-production-readiness-checklist.md` as the production readiness checklist before enabling production billing. The production setup sequence must include:

- production webhook destination verification;
- production checkout configuration;
- production environment separation from sandbox;
- production config guard;
- webhook endpoint sanity check;
- real production smoke/manual verification only when ready.

Production and sandbox configuration must stay separated. Documentation alone must not be treated as production enablement. Production billing remains incomplete until production webhook delivery, checkout configuration, provider credentials, product/price mapping, and manual smoke verification are completed safely outside tracked files and without committing secrets.

## 8. Remaining operation: optional bounded polling

Plan only. Automatic desktop polling is not implemented by this document.

Manual Refresh status exists now and is the current supported post-checkout desktop status update path.

If automatic polling is implemented later, it must be:

- bounded only;
- short duration;
- backend-driven;
- based on backend access/status responses;
- never based on local Premium activation;
- stopped after success, failure, user cancellation, or timeout;
- safe when webhooks are delayed or missing;
- optional UX improvement rather than an access authority.

## 9. Remaining operation: future mobile entitlement bridge

Plan only. The mobile entitlement bridge is not designed or implemented by this document.

Future Apple App Store and Google Play purchases must map into the same backend account model used by desktop/web billing. Mobile clients must not decide Premium locally. The backend must normalize mobile store events into backend entitlement state using the same account/subscription/entitlement model and must keep access decisions server-side.

This task does not design the full mobile bridge. Future design must cover account linking, receipt/server notification validation, renewal/cancellation/refund/dispute equivalents, entitlement source separation, idempotency, audit logs, and provider-specific edge cases without changing the core rule that `EntitlementEntity` is the Premium access source.

## 10. Suggested implementation order

Recommended order for remaining billing operations:

1. Production Paddle readiness verification and configuration.
2. Refund/chargeback policy document.
3. Safe event persistence for refund/chargeback/dispute events.
4. Admin/audit visibility for those events.
5. Automatic `provider_event` entitlement action only after policy approval.
6. Optional reconciliation job.
7. Optional bounded desktop polling.
8. Future mobile entitlement bridge.

## 11. Explicit non-goals

This planning update explicitly does not include:

- no code implementation in this task;
- no backend changes;
- no desktop changes;
- no EF migration;
- no Admin UI changes;
- no smoke script changes;
- no configuration default changes;
- no real secrets;
- no production enablement by documentation alone;
- no claim that refunds are implemented;
- no claim that chargebacks/disputes are implemented;
- no claim that reconciliation is implemented;
- no claim that automatic desktop polling is implemented;
- no claim that a mobile entitlement bridge is implemented.

## 12. Verification checklist for this docs task

Use this checklist to verify the Step 4H documentation update:

- docs-only;
- no code changes;
- no backend changes;
- no desktop changes;
- no Admin UI changes;
- no database entity changes;
- no EF migration;
- no smoke script changes;
- no real secrets;
- production billing still marked incomplete;
- sandbox baseline preserved;
- remaining operations clearly separated from implemented features.
