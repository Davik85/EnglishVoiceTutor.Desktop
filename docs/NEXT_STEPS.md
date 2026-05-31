# Next Steps

Review date: 2026-05-31.

This roadmap starts from the current confirmed MVP state where:
- desktop/backend builds pass,
- lesson content audit passes,
- auth/JWT foundation is implemented,
- optional desktop Account UI is implemented,
- Settings and Lesson Chat runtime persistence are auth-aware,
- Development free-limit mode is diagnostics-only,
- Paddle checkout, webhook ingestion, subscription snapshots, payment snapshots, entitlement activation/extension, scheduled-cancellation policy, past-due policy, actual canceled/paused expiry policy, and resumed/activated snapshot-only policy are implemented through Step 4B.
- Step 4C production Paddle webhook setup checklist/config guard is documentation/tooling only.
- Desktop Step 4D-1 through 4D-4 are implemented for backend-state mapping, paywall display, backend-only checkout launch, and manual refresh after checkout.
- The manual sandbox payment loop has been validated: Upgrade -> Paddle Checkout -> transaction.completed webhook -> Premium active -> lesson allowed.
- Production billing setup is not complete.
- Step 5A desktop release readiness audit is tracked in `docs/desktop-release-readiness-audit.md`.
- The consolidated desktop release work plan is tracked in `docs/desktop-release-work-plan.md`.
- Desktop release readiness is the first active priority before production billing rollout or broader CMS/Admin work.

## Recommended next product order

1. Desktop release readiness and hardening: Phase 5B
   - primary plan: `docs/desktop-release-work-plan.md`
   - audit baseline: `docs/desktop-release-readiness-audit.md`
   - start with Settings final acceptance and Diagnostics Release gate
   - include Step 5B-2 native languages and localization foundation:
     - expand native/interface/explanation language options;
     - keep Study language options separate;
     - keep backend-backed user settings as the source of truth;
     - keep desktop AI features routed through backend APIs only;
     - do not store OpenAI keys in desktop;
     - do not change lesson JSON content as part of language-list planning
   - continue through backend-unavailable/account UX, auth-session storage decision, lesson selection QA, Lesson Chat polish, voice/TTS/Conversation Mode acceptance, release diagnostics/config cleanup, release packaging, security/privacy, manual release checklist, and final P0/P1 triage
2. Production billing readiness: Phase 5C, after desktop hardening
   - production-readiness checklist: `docs/paddle-production-readiness-checklist.md`
   - production webhook setup checklist: `docs/paddle-production-webhook-setup.md`
   - safe local config guard: `tools/smoke_paddle_production_config_guard.ps1`
   - configure production checkout settings outside tracked files and client code
   - keep production billing marked incomplete until production webhook delivery, checkout configuration, provider credentials, product/price mapping, environment separation, and manual smoke verification are completed safely
3. Remaining billing operations planning/implementation after the desktop release gate and production-billing readiness decision
   - planning document: `docs/billing-remaining-operations-plan.md`
   - refund and chargeback handling policy
   - manual revocation automation policy
   - optional bounded refresh/polling decision later; manual Refresh status exists now and automatic polling is not implemented
   - future Apple App Store / Google Play mobile entitlement bridge plan
   - optional background subscription reconciliation job
4. CMS/Admin operational readiness: Phase 5D, after desktop hardening
   - planning baseline: `docs/CMS_ADMIN_PLANNING.md`
   - start with read-only support/admin needs before full CMS
   - defer broad production RBAC/content-management work until desktop readiness and minimum operational support requirements are clear
   - later work may include roles, content versioning, draft/published workflow, audit trail, rollback, and safe prompt/scenario editing

## Already completed (do not relist as future work)

- backend Auth/JWT foundation
- optional desktop Account UI
- authenticated user settings endpoints
- auth-aware Settings source switching (`/api/dev/user-settings` <-> `/api/me/settings`)
- auth-aware Lesson Chat runtime persistence
- read-only free-limit diagnostics
- Development diagnostics-only mode
- Paddle checkout transaction creation v1 behind explicit configuration
- Paddle production webhook setup checklist and safe local config guard as documentation/tooling only
- Desktop Step 4D-1 through Step 4D-4 backend-driven upgrade/paywall flow (`docs/desktop-upgrade-paywall-ui-plan.md`): backend-state mapping, simple access/paywall panel, backend-only checkout launch, and manual Refresh status after checkout
- Paddle webhook ingestion, normalization, reconciliation decision, and event-scoped processing foundation v1
- Paddle subscription lifecycle snapshot foundation v1 for `subscription.created`, `subscription.updated`, and `subscription.past_due`
- Paddle transaction payment persistence snapshot foundation v1 for `transaction.completed` and `transaction.payment_failed`
- Paddle entitlement activation and extension from valid `transaction.completed` provider events
- Scheduled cancellation metadata recording without early Premium revocation
- Past-due snapshot recording without entitlement creation, extension, or revocation
- Actual `subscription.canceled` / `subscription.paused` policy that expires only active `provider_event` Premium entitlement for the resolved internal user/provider subscription context
- Backend access/status recognition of `provider_event` Premium entitlement
- local Development CMS/admin support foundation v1
- Manual Paddle sandbox payment loop validation: Upgrade -> Paddle Checkout -> transaction.completed webhook -> Premium active -> lesson allowed
- Step 5A desktop release readiness audit

## Billing boundaries to preserve

- English Voice Tutor remains global, cross-platform, and provider-agnostic.
- Paddle is the current desktop/web billing provider adapter only.
- Backend remains the only source of truth for account, trial, subscription, Premium/free status, daily free allowance, usage, lesson history, limits, payments, entitlements, and user settings.
- Desktop and future mobile clients must continue relying on backend access/status decisions.
- `EntitlementEntity` remains the source of Premium access.
- `SubscriptionEntity` is a provider-agnostic subscription snapshot and must not grant Premium access by itself.
- `PaymentEntity` is diagnostic payment history only and must not be used as an access source.
- Desktop must not store real secrets, payment secrets, provider API keys, OpenAI API keys, or make direct OpenAI calls.

## Current non-goals

- No production Paddle rollout before Phase 5B desktop release hardening is complete.
- No production billing enablement from documentation alone.
- No full CMS/Admin implementation before desktop readiness and minimum support requirements are clear.
- No mobile app-store bridge work before the desktop and billing gates are ready.
- No expansion of Study languages as part of Step 5B-2 unless a later approved task explicitly requests it.
- No lesson JSON rewrite as part of native/interface/explanation language planning.
