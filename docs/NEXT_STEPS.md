# Next Steps

Review date: 2026-06-04.


## After Step 5D-6d

Step 5D-6d completed Admin CMS refresh resilience and unsaved-change protection. Admin refresh no longer logs out the admin because refresh auth uses the existing admin-only HTTP-only cookie, while the admin JWT remains memory-only in JavaScript. Browser Web Storage is not used: no `sessionStorage`, no `localStorage`, and no IndexedDB. The URL hash stores only safe workspace identifiers (`adminTab`, `cmsSubTab`, `selectedUserId`, `contentPackSlug`, `topicKey`, `scenarioKey`, `promptTemplateKey`, and `tutorId`). Selected user details restore through an admin-only lookup by `selectedUserId`, and selected CMS entities restore by stable keys.

Admin CMS Content now supports content pack overview, topic editing, scenario editing, structured scenario editing, full scenario JSON editing, prompt template editing, tutor behavior profile editing, validation/preview summary, and versions/publish/restore flows under the existing `/admin/` shell. `Format JSON` only pretty-prints JSON for easier editing. `Validate JSON` checks syntax and required scenario fields. Neither action saves or publishes; `Save draft` is required to persist CMS edits. Unsaved CMS dirty state is tracked in memory against the last loaded/saved baseline, unsaved content is not stored in browser storage or the URL hash, and refresh/navigation/entity switching/logout warns before discarding edits.

Runtime learner behavior remains unchanged. CMS reads remain controlled by configuration and disabled by default, with static JSON fallback still available. External tester handoff remains paused until the CMS/Admin content MVP is ready enough for practical content changes without code edits. Production RBAC, role-based content approval, production billing operations, and full external tester handoff are still not production-ready.

CMS draft-save audit logging is implemented for successful Admin CMS Save draft operations, and the Admin CMS Audit subtab now exposes recent CMS changes as read-only rows filtered by selected content pack, entity type, stable key text, and limit. Audit rows show metadata and shortened before/after hashes; full edited content bodies are not stored or displayed in audit rows. The later CMS governance step is a critical-change approval workflow, but it should wait until production roles/RBAC exist.

## Recommended next product order

1. CMS/Admin content MVP foundation (Phase 5D-0 through Step 5D-6d).
   - Planning baseline: `docs/CMS_ADMIN_PLANNING.md`.
   - Detailed content MVP plan: `docs/cms-content-mvp-plan.md`.
   - Step 5D-0 planning, Step 5D-1 backend schema foundation, Step 5D-2 static JSON import/seed foundation, Step 5D-3 backend published-snapshot read/status path, Step 5D-4 backend Admin CMS content API draft read/update plus validation/preview skeleton, Step 5D-5 backend publish/version/rollback endpoints, Step 5D-6 Admin CMS Content UI shell, Step 5D-6a internal Admin CMS sub-tabs, Step 5D-6b table selection UX/governance documentation, Step 5D-6c full scenario JSON editing foundation, Step 5D-6d refresh resilience/unsaved-change protection, and Step 5D-6e structured scenario editing are complete.
   - The new CMS tables and imported `static-json-v1` / `Static JSON Baseline` snapshot are still not used by runtime lesson loading by default; static JSON/content behavior remains unchanged.
   - `CmsContent:ReadPublishedSnapshotEnabled` defaults to `false`, `CmsContent:ContentPackSlug` defaults to `static-json-v1`, and `CmsContent:FallbackToStaticJson` defaults to `true`.
   - Keep this content-focused. Do not include production billing controls, Paddle management, payment editing, entitlement editing, broad user management, mobile-specific CMS, public production Admin, secrets, direct OpenAI key handling, or study-language editing.
   - CMS draft-save audit logging now contains actor identity, timestamp UTC, content pack, entity type/id and stable key, changed fields, before/after hashes, reason, source, status, and request/correlation id when available; future CMS production governance still needs production RBAC and approval workflow.
   - Future critical CMS changes should require approval after production roles exist; planned roles may include Content Editor, Content Reviewer, and Admin / Owner, with draft editing separated from approval.
2. Controlled next CMS implementation step.
   - CMS draft-save audit logging and the read-only Recent CMS changes UI are implemented; next work is controlled Admin CMS regression and later governance planning.
   - Run controlled Admin CMS end-to-end UI/API regression against a local backend: load `static-json-v1`, select and save one bounded draft field per content type, exercise full scenario JSON format/validate/save, run validation, load preview summary, list versions, and verify publish/restore confirmation flows.
   - Critical-change approval workflow is a later CMS governance step and should wait until production roles/RBAC exist.
   - If learner runtime CMS integration is attempted later, keep it behind the disabled-by-default feature flag and retain static JSON fallback on every CMS failure.
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
- No production CMS/Admin readiness yet: production RBAC and approval workflow are not implemented, although development/admin-only CMS draft-save audit logging now exists.
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
- Continue next with Admin CMS UI/API hardening and controlled end-to-end CMS editor smoke/regression work; keep production billing deferred and public release not ready.
- Keep production billing/Paddle rollout work deferred while CMS/Admin content MVP remains the priority.
- Set up a domain email/provider later before enabling password reset delivery. Password reset remains disabled/not exposed as a working tester flow until that setup exists.


Implemented CMS draft-save audit logging details: successful Topic, Scenario (bounded fields, structured scenario fields, and full scenario JSON), Prompt Template, and Tutor Behavior Profile Save draft operations write `DraftSaved` rows to `cms_content_audit_logs`. Rows capture audit id, `createdAtUtc`, actor user id, actor email when available, content pack id and slug, entity type, entity id, stable key (`stableTopicKey`, `stableScenarioKey`, `templateKey`, or `tutorId`), changed field names, before/after SHA-256 hashes, source `AdminCms`, status, and request id when available. Audit rows intentionally do not store full before/after JSON snapshots, prompt/tutor source text snapshots, passwords, tokens, provider secrets, OpenAI API keys, Paddle API keys/webhook secrets, or admin bearer tokens. Large edited values are represented by hashes. No-op Save draft requests avoid noisy draft-save audit rows. Admins can read recent CMS audit entries through development/admin-only audit endpoints and the CMS Content Audit subtab. Runtime learner behavior is unchanged: CMS read path remains disabled by default and static JSON fallback remains available. Production RBAC and critical-change approval remain future work.

Structured scenario editor update: the Admin CMS Scenarios subtab now includes a safer structured editor for common scenario content (title/subtopic, description, setup message, first bot message guidance, context option titles, valid context keywords, custom context rules, invalid context redirect, goal text, can-do statements, opening/first-user-task/follow-up guidance, AI tutor instructions, wrap-up/final message guidance, hint example, and wrap/final turn counters). `DefinitionJson` remains the canonical stored scenario definition; no per-field scenario database columns or EF migration were added. Structured edits parse the current `DefinitionJson`, update only known JSON paths, and write the merged valid JSON back to `DefinitionJson`, preserving unknown fields and advanced configuration in place. Advanced JSON remains available with `Format JSON` and `Validate JSON` for rare technical changes. Save draft remains explicit; invalid Advanced JSON or invalid structured numeric/required data is rejected before saving, and backend scenario validation still rejects invalid JSON, missing required fields, or accidental stable id/title/setup mismatches. CMS draft-save audit logging still records successful scenario saves with changed field names and before/after hashes without storing full scenario JSON bodies. Runtime learner behavior remains unchanged by default: the CMS read path is still disabled unless explicitly enabled, and static JSON fallback remains available.


## Controlled CMS runtime path next checks

Before external tester handoff, verify the disabled-by-default CMS runtime read path with `CmsContent__UsePublishedSnapshotForRuntime=true`, `CmsContent__ReadPublishedSnapshotEnabled=true`, `CmsContent__ContentPackSlug=static-json-v1`, and the desired `CmsContent__FallbackToStaticJson` setting. Run the runtime diagnostic smoke (`tools/smoke_cms_runtime_content_read.ps1`) after importing/publishing CMS content. Confirm the diagnostic reports `CmsPublishedSnapshot`, 6 topics, 26 scenarios, 3 prompt templates, 2 tutor behavior profiles, a valid snapshot hash, and `fallbackUsed=false`. Also keep fallback verification in the manual/regression checklist: use a non-existent content pack slug with fallback enabled to confirm `StaticJson` plus `fallbackUsed=true`, and with fallback disabled to confirm a clear server-side unavailable result. Production RBAC and approval workflow remain future work.
