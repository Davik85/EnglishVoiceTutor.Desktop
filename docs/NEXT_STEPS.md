# Next Steps

Review date: 2026-06-01.

This roadmap starts from the current confirmed state: the desktop MVP core lesson/voice/TTS flow is accepted, the tester ZIP flow is accepted, backend-enforced single active lesson protection is accepted, localization for the current Interface language set is closed for this phase, and production billing / CMS/Admin remain deferred.

## Recommended next product order

1. Continue remaining desktop release hardening first (Phase 5B).
   - Primary plan: `docs/desktop-release-work-plan.md`.
   - Audit baseline: `docs/desktop-release-readiness-audit.md`.
   - Release smoke gate: `docs/desktop-release-smoke-gate.md` and `tools/run_desktop_release_gate.ps1`.
   - Keep the canonical tester handoff flow as:

     ```powershell
     cd C:\dev\EnglishVoiceTutor.Desktop
     powershell -ExecutionPolicy Bypass -File .\scripts\package-tester-release.ps1
     ```

   - Default tester artifact: `artifacts\packages\EnglishVoiceTutor.Desktop-win-x64-self-contained.zip`.
   - `dotnet publish` may remain documented only as a lower-level troubleshooting/developer path, not the main tester flow.
   - Do not expand Study languages, Interface languages, or lesson JSON during this documentation/hardening track.
   - Do not continue prompt/dialogue/scenario/bot-behavior quality polishing in code now; defer it to CMS/Admin.
2. Production billing readiness (Phase 5C), only after desktop hardening.
   - Production-readiness checklist: `docs/paddle-production-readiness-checklist.md`.
   - Production webhook setup checklist: `docs/paddle-production-webhook-setup.md`.
   - Safe local config guard: `tools/smoke_paddle_production_config_guard.ps1`.
   - Keep production billing marked incomplete until production webhook delivery, checkout configuration, provider credentials, product/price mapping, environment separation, and manual smoke verification are completed safely.
3. Remaining billing operations planning/implementation after the desktop release gate and production-billing readiness decision.
   - Planning document: `docs/billing-remaining-operations-plan.md`.
   - Refund and chargeback handling policy.
   - Manual revocation automation policy.
   - Optional bounded refresh/polling decision later; manual Refresh status exists now and automatic polling is not implemented.
   - Future Apple App Store / Google Play mobile entitlement bridge plan.
   - Optional background subscription reconciliation job.
4. CMS/Admin operational readiness (Phase 5D), after desktop hardening.
   - Planning baseline: `docs/CMS_ADMIN_PLANNING.md`.
   - Start with support/admin operational needs before full content management.
   - Later CMS/Admin work may include safe prompt/scenario/bot-behavior editing, validation, preview, versioning, rollback, roles, audit trail, and draft/published workflow.

## Already completed or accepted (do not relist as future work)

- Backend Auth/JWT foundation.
- Optional desktop Account UI.
- Authenticated user settings endpoints and auth-aware Settings source switching.
- Auth-aware Lesson Chat runtime persistence and backend lesson history.
- Protected desktop auth session storage using Windows DPAPI-protected `auth-session.json` payloads.
- Desktop Step 4D backend-driven upgrade/paywall flow for sandbox validation: backend-state mapping, simple access/paywall panel, backend-only checkout launch, and manual Refresh status after checkout.
- Paddle webhook ingestion, normalization, subscription lifecycle snapshots, payment snapshots, entitlement activation/extension, scheduled cancellation policy, past-due policy, canceled/paused expiry policy, and resumed/activated snapshot-only policy.
- Local Development CMS/admin support foundation v1.
- Step 5A desktop release readiness audit.
- Step 5B Settings/Diagnostics release gate: packaged Release hides Diagnostics by default and enables it only with local `EVT_DESKTOP_DIAGNOSTICS=1`.
- Step 5B native/interface/explanation language foundation.
- Step 5B interface localization current phase closed for the release-ready list.
- Backend-unavailable/account UX hardening for non-crash resilience and localized errors.
- Single active lesson guard.
- Heartbeat stale protection.
- Remote active lesson release.
- Old-session invalidation after remote release, including old heartbeat and old lesson-bound message rejection.
- Lesson Chat / Voice / TTS acceptance gate.
- Tester ZIP package acceptance on another Windows device.

## Billing and platform boundaries to preserve

- English Voice Tutor remains global, cross-platform, and provider-agnostic.
- Do not introduce YooKassa, Russian payment flows, or Russia-only billing assumptions.
- Do not change Paddle, billing, subscription, entitlement, or Admin UI logic during desktop documentation/hardening work.
- Paddle is the current desktop/web billing provider adapter only.
- Backend remains the only source of truth for account, trial, subscription, Premium/free status, daily free allowance, usage, lesson history, active lesson state, limits, payments, entitlements, and user settings.
- Desktop and future mobile clients must continue relying on backend access/status/active-lesson decisions.
- `EntitlementEntity` remains the source of Premium access.
- `SubscriptionEntity` is a provider-agnostic subscription snapshot and must not grant Premium access by itself.
- `PaymentEntity` is diagnostic payment history only and must not be used as an access source.
- Desktop must not store real secrets, payment secrets, provider API keys, OpenAI API keys, or make direct OpenAI calls.

## Current non-goals

- No production Paddle rollout before Phase 5B desktop release hardening is complete.
- No production billing enablement from documentation alone.
- No full CMS/Admin implementation before desktop readiness and minimum support requirements are clear.
- No code-side dialogue/prompt quality polishing before CMS/Admin prompt/scenario/bot-behavior editing is ready.
- No mobile app-store bridge work before the desktop and billing gates are ready.
- No expansion of Study languages.
- No expansion of the release-ready Interface language list.
- No narrowing of the Native/Explanation language catalog.
- No lesson JSON rewrite.
- No public release declaration yet.

## After Step 5B-9

- Continue with controlled tester handoff and the final clean-machine checklist.
- Keep production billing/Paddle rollout work deferred until tester validation is complete.
- Keep CMS/Admin expansion deferred.
- Set up a domain email/provider later before enabling password reset delivery. Password reset remains disabled/not exposed as a working tester flow until that setup exists.
