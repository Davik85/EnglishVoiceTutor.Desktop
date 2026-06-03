# Next Steps

Review date: 2026-06-03.

This roadmap starts from the current confirmed state: the desktop MVP core lesson/voice/TTS flow is accepted, the tester ZIP flow is accepted, Welcome screen polish and Lesson Chat window auto-sizing are accepted, backend-enforced single active lesson protection is accepted, localization for the current Interface language set is closed for this phase, and the desktop hardening block is stable enough to pause external tester handoff. The product priority is now CMS/Admin content MVP before external testers. Production billing and public release remain deferred/not ready.

## Recommended next product order

1. CMS/Admin content MVP schema foundation (Phase 5D-0/5D-1).
   - Planning baseline: `docs/CMS_ADMIN_PLANNING.md`.
   - Detailed content MVP plan: `docs/cms-content-mvp-plan.md`.
   - Step 5D-0 planning and Step 5D-1 backend schema foundation are complete.
   - The new CMS tables are not used by runtime lesson loading yet; static JSON/content behavior remains unchanged.
   - Keep this content-focused. Do not include production billing controls, Paddle management, payment editing, entitlement editing, broad user management, mobile-specific CMS, public production Admin, secrets, direct OpenAI key handling, or study-language editing.
2. CMS JSON import/seed planning and importer implementation.
   - Next implementation should design and add the current JSON content import/seed path into CMS draft/published data without rewriting current lesson JSON.
   - Do this before Admin UI work so the schema has real imported content to validate and compare.
   - Keep runtime lesson loading on static JSON until a later explicit published-content read-path step adds fallback-safe CMS reads.
   - Desktop must continue to call backend APIs only; backend remains the source of truth.
3. Controlled external tester handoff.
   - Tester handoff is paused until CMS/Admin content MVP foundation is ready enough that content/prompt/scenario fixes can be handled through CMS.
   - Before actual delivery, re-run the release gate and clean-machine checklist from `docs/desktop-release-work-plan.md`.
   - Keep the canonical tester handoff flow as:

     ```powershell
     cd C:\dev\EnglishVoiceTutor.Desktop
     powershell -ExecutionPolicy Bypass -File .\scripts\package-tester-release.ps1
     ```

   - Default tester artifact: `artifacts\packages\EnglishVoiceTutor.Desktop-win-x64-self-contained.zip`.
   - `dotnet publish` remains a lower-level troubleshooting/developer path, not the main tester flow.
4. Production billing readiness later.
   - Production-readiness checklist: `docs/paddle-production-readiness-checklist.md`.
   - Production webhook setup checklist: `docs/paddle-production-webhook-setup.md`.
   - Safe local config guard: `tools/smoke_paddle_production_config_guard.ps1`.
   - Keep production billing marked incomplete until production webhook delivery, checkout configuration, provider credentials, product/price mapping, environment separation, and manual smoke verification are completed safely outside tracked files and without committing secrets.
5. Remaining billing operations planning/implementation after CMS content MVP, controlled tester handoff, and production-billing readiness decisions.
   - Planning document: `docs/billing-remaining-operations-plan.md`.
   - Refund and chargeback handling policy.
   - Manual revocation automation policy.
   - Optional bounded refresh/polling decision later; manual Refresh status exists now and automatic polling is not implemented.
   - Future Apple App Store / Google Play mobile entitlement bridge plan.
   - Optional background subscription reconciliation job.

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
- Welcome screen polish accepted for the current desktop hardening phase.
- Lesson Chat window auto-sizing accepted for the current desktop hardening phase.
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

## After accepted Welcome screen and Lesson Chat sizing hardening

- Treat Welcome screen polish and Lesson Chat window auto-sizing as done for the current desktop hardening phase.
- Keep controlled tester handoff paused until the CMS/Admin content MVP foundation is ready enough for controlled content fixes.
- Continue next with CMS JSON import/seed planning and importer work before Admin UI.
- Keep production billing/Paddle rollout work deferred while CMS/Admin content MVP remains the priority.
- Set up a domain email/provider later before enabling password reset delivery. Password reset remains disabled/not exposed as a working tester flow until that setup exists.
