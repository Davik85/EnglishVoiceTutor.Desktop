# Next Steps

Review date: 2026-05-29.

This roadmap starts from the current confirmed MVP state where:
- desktop/backend builds pass,
- lesson content audit passes,
- auth/JWT foundation is implemented,
- optional desktop Account UI is implemented,
- Settings and Lesson Chat runtime persistence are auth-aware,
- Development free-limit mode is diagnostics-only,
- Paddle checkout, webhook ingestion, subscription snapshots, payment snapshots, entitlement activation/extension, scheduled-cancellation policy, past-due policy, actual canceled/paused expiry policy, and resumed/activated snapshot-only policy are implemented through Step 4B.

## Recommended next backend/product order

1. Small auth/runtime cleanup if needed
   - reduce noisy duplicate-email logs if needed
   - review expired-token fallback behavior
   - keep dev fallback safe for local testing
2. Plan remaining billing operations before implementation
   - next planning document: `docs/billing-remaining-operations-plan.md`
   - refund and chargeback policy
   - manual revocation automation policy
   - production Paddle webhook setup checklist
   - desktop upgrade/paywall UI plan
   - future Apple App Store / Google Play mobile entitlement bridge plan
   - optional background subscription reconciliation job
3. Add broader production admin/RBAC/content-management work later, only after:
   - roles
   - content versioning
   - draft/published workflow
   - audit trail
   - rollback
   - safe prompt/scenario editing

## Already completed (do not relist as future work)

- backend Auth/JWT foundation
- optional desktop Account UI
- authenticated user settings endpoints
- auth-aware Settings source switching (`/api/dev/user-settings` <-> `/api/me/settings`)
- auth-aware Lesson Chat runtime persistence
- read-only free-limit diagnostics
- Development diagnostics-only mode
- Paddle checkout transaction creation v1 behind explicit configuration
- Paddle webhook ingestion, normalization, reconciliation decision, and event-scoped processing foundation v1
- Paddle subscription lifecycle snapshot foundation v1 for `subscription.created`, `subscription.updated`, and `subscription.past_due`
- Paddle transaction payment persistence snapshot foundation v1 for `transaction.completed` and `transaction.payment_failed`
- Paddle entitlement activation and extension from valid `transaction.completed` provider events
- Scheduled cancellation metadata recording without early Premium revocation
- Past-due snapshot recording without entitlement creation, extension, or revocation
- Actual `subscription.canceled` / `subscription.paused` policy that expires only active `provider_event` Premium entitlement for the resolved internal user/provider subscription context
- Backend access/status recognition of `provider_event` Premium entitlement
- local Development CMS/admin support foundation v1

## Billing boundaries to preserve

- English Voice Tutor remains global, cross-platform, and provider-agnostic.
- Paddle is the current desktop/web billing provider adapter only.
- Backend remains the only source of truth for account, trial, subscription, Premium/free status, daily free allowance, usage, lesson history, limits, payments, and entitlements.
- Desktop and future mobile clients must continue relying on backend access/status decisions.
- `EntitlementEntity` remains the source of Premium access.
- `SubscriptionEntity` is a provider-agnostic subscription snapshot and must not grant Premium access by itself.
- `PaymentEntity` is diagnostic payment history only and must not be used as an access source.
