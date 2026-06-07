# Next Steps

Review date: 2026-06-07.


## After latest Admin CMS Content step

Step 5D-6d completed Admin CMS refresh resilience and unsaved-change protection. Step 5D-6e is also complete for Admin CMS scenario editor usability refinement, while draft-save publish discoverability, required publish summaries with clearer validation errors, smoke/test audit filtering, and confirmed local CMS runtime published-snapshot reads remain in place. Admin refresh no longer logs out the admin because refresh auth uses the existing admin-only HTTP-only cookie, while the admin JWT remains memory-only in JavaScript. Browser Web Storage is not used: no `sessionStorage`, no `localStorage`, and no IndexedDB. The URL hash stores only safe workspace identifiers (`adminTab`, `cmsSubTab`, `selectedUserId`, `contentPackSlug`, `topicKey`, `scenarioKey`, `promptTemplateKey`, and `tutorId`). Selected user details restore through an admin-only lookup by `selectedUserId`, and selected CMS entities restore by stable keys.

Admin CMS Content now supports content pack overview, topic editing, scenario editing, structured scenario editing, full scenario JSON editing, prompt template editing, tutor behavior profile editing, validation/preview summary, and versions/publish/restore flows under the existing `/admin/` shell. Step 5D-6e is complete: the Scenarios editor now includes compact local **Jump to** navigation, collapsible/visually separated Basic fields, Lesson setup, Context selection / choices, Conversation flow / response guidance, Wrap-up / summary guidance, and Advanced JSON sections, and helper text for normal content editors. Structured fields remain the recommended normal editing path; Advanced JSON remains a visually separated technical fallback for rare full-JSON edits. `Format JSON` only pretty-prints JSON for easier editing. `Validate JSON` checks syntax and required scenario fields. Neither action saves or publishes; `Save draft` is required to persist CMS edits and remains draft-only. Unsaved CMS dirty state is tracked in memory against the last loaded/saved baseline, unsaved content is not stored in browser storage or the URL hash, and refresh/navigation/entity switching/logout warns before discarding edits. After successful Save draft operations, the editor shows **Go to Publish**; publishing changed content still happens only from **Versions & Publish**, requires a short change summary, and publish failures display backend validation details. Published versions remain immutable; restore creates a new published version rather than mutating old history. Runtime reads only published snapshots when CMS runtime mode is explicitly enabled; static JSON remains default. A local run confirmed `Source=CmsPublishedSnapshot`, `ContentPackSlug=static-json-v1`, `VersionNumber=34`, `FallbackUsed=False`, `ValidationPassed=True`, 6 topics, 26 scenarios, 3 prompt templates, and 2 tutor behavior profiles with `CmsContent__ReadPublishedSnapshotEnabled=true`, `CmsContent__UsePublishedSnapshotForRuntime=true`, `CmsContent__ContentPackSlug=static-json-v1`, and `CmsContent__FallbackToStaticJson=true`.

Runtime learner behavior remains unchanged by default. CMS reads remain controlled by configuration and disabled by default, with static JSON fallback still available. The CMS/Admin Content MVP is advanced enough to pause CMS feature work and continue test deployment preparation. Backend deployment and static HTTPS download setup are next; production RBAC, role-based content approval, production billing operations, and full external tester handoff are still not production-ready.

CMS draft-save audit logging is implemented for successful Admin CMS Save draft operations, and the Admin CMS Audit subtab now exposes recent CMS changes as read-only rows filtered by selected content pack, entity type, stable key text, and limit. Smoke/test audit entries are hidden by default, a **Show smoke/test entries** checkbox exists for debugging, and normal manual Admin CMS UI changes remain visible. Audit rows show metadata and shortened before/after hashes; full edited content bodies are not stored or displayed in audit rows. The later CMS governance step is a critical-change approval workflow, but it should wait until production roles/RBAC exist.

## Recommended next product order

1. Server setup and test deployment preparation.
   - This is the next stage after this documentation update.
   - Prepare the backend deployment environment and a static HTTPS direct-download location without committing secrets, IP addresses, usernames, passwords, API keys, tokens, SSH keys, database passwords, provider keys, generated artifacts, or environment-specific values.
   - Backend deployment is not done yet. Keep backend as the source of truth for auth, lessons, active lesson state, usage, billing/access, CMS runtime selection, and AI/TTS/STT calls.
   - Desktop must continue to store no OpenAI API keys and must not call OpenAI directly.
   - External tester handoff remains blocked until server/static HTTPS download exists, clean-machine install passes, and the controlled tester checklist passes.
2. Windows direct-download release preparation.
   - Keep Inno Setup as the primary Windows direct-download installer path. Stable installer AppId: `LanguageVoiceTutor.Desktop`. Expected installer artifact: `artifacts\installers\windows\LanguageVoiceTutorSetup-{version}.exe`.
   - Server-ready direct-download files are generated under `artifacts\releases\windows\direct`: `latest.json`, `changelog.json`, `known-issues.json`, `checksums.sha256`, and `LanguageVoiceTutorSetup-{version}.exe`.
   - Validate local release artifacts with `scripts/validate-windows-direct-release.ps1`. The upload helper supports dry-run/future SCP only and must not hardcode server secrets.
   - `latest.json` is only for the future download page and future in-app manual update-check. The app does not automatically check it yet. Future update UX must require manual confirmation and must not run during an active lesson.
   - Code signing remains deferred, so Windows SmartScreen warnings are expected for now. Generated `artifacts/` files must not be committed.
   - ZIP packaging remains only an emergency/developer fallback through `scripts/package-tester-release.ps1`; do not present ZIP as the primary external tester handoff once the Inno installer smoke passes.
   - Velopack is rejected/deprecated and must not be reintroduced.
3. CMS/Admin content MVP pause point.
   - Planning baseline: `docs/CMS_ADMIN_PLANNING.md`. Detailed content MVP plan: `docs/cms-content-mvp-plan.md`.
   - Step 5D-0 through Step 5D-6e are complete enough to pause and continue deployment preparation. `/admin/` works, the CMS Content workspace exists, and Overview, Topics, Scenarios, Prompts, Tutors, Validation & Preview, Versions & Publish, and Audit tabs exist.
   - `Save draft` is explicit and draft-only. Publishing happens only from **Versions & Publish**, requires a publish summary for changed content, keeps old published versions immutable, and restore creates a new published version.
   - CMS draft-save audit logging works for successful Save draft operations. Runtime published-snapshot loading was checked locally, but runtime still defaults to static JSON unless explicitly switched with the CMS runtime settings.
   - Production RBAC and critical-change approval workflow remain deferred.
4. Production billing readiness later.
   - Production billing is deferred. Do not change Paddle, billing, checkout, subscriptions, entitlements, payment code, or provider configuration during server setup documentation/preparation.
   - Language Voice Tutor is a global/international product. Do not introduce YooKassa, Russia-only, or provider-specific regional billing assumptions.
   - Keep production billing marked incomplete until production webhook delivery, checkout configuration, provider credentials, product/price mapping, environment separation, and manual smoke verification are completed safely outside tracked files and without committing secrets.
5. Remaining platform/store work later.
   - Microsoft Store, Apple App Store, Google Play, and Mac version are deferred.
   - Mobile entitlement bridge, remaining billing operations, production CMS/Admin governance, and public release readiness remain future work.

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
- Earlier tester ZIP package acceptance on another Windows device; ZIP is now only an emergency/developer fallback.

## Billing and platform boundaries to preserve

- Language Voice Tutor remains global, cross-platform, and provider-agnostic.
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

- No production Paddle rollout during server setup/test deployment preparation.
- No production billing enablement from documentation alone.
- No production CMS/Admin readiness yet: production RBAC and approval workflow are not implemented, although development/admin-only CMS draft-save audit logging now exists.
- No code-side dialogue/prompt quality polishing; use CMS/Admin content workflows for future prompt/scenario/bot-behavior polishing.
- No mobile app-store bridge work before the desktop and billing gates are ready.
- No expansion of Study languages.
- No expansion of the release-ready Interface language list.
- No narrowing of the Native/Explanation language catalog.
- No lesson JSON rewrite.
- No public release declaration yet.
- No automatic update-check or update UI yet; the direct-download `latest.json` manifest is only a foundation for a future download page and future manual-confirmation update-check that must not run during active lessons.

## After accepted Welcome screen and Lesson Chat sizing hardening

- Treat Welcome screen polish and Lesson Chat window auto-sizing as done for the current desktop hardening phase.
- CMS/Admin content MVP is advanced enough to pause feature work for server setup and test deployment preparation.
- Continue next with backend deployment environment setup and static HTTPS direct-download preparation; keep production billing deferred and public release not ready.
- Keep production billing/Paddle rollout work deferred during server setup/test deployment preparation.
- Set up a domain email/provider later before enabling password reset delivery. Password reset remains disabled/not exposed as a working tester flow until that setup exists.


Implemented CMS draft-save audit logging details: successful Topic, Scenario (bounded fields, structured scenario fields, and full scenario JSON), Prompt Template, and Tutor Behavior Profile Save draft operations write `DraftSaved` rows to `cms_content_audit_logs`. Rows capture audit id, `createdAtUtc`, actor user id, actor email when available, content pack id and slug, entity type, entity id, stable key (`stableTopicKey`, `stableScenarioKey`, `templateKey`, or `tutorId`), changed field names, before/after SHA-256 hashes, source `AdminCms`, status, and request id when available. Audit rows intentionally do not store full before/after JSON snapshots, prompt/tutor source text snapshots, passwords, tokens, provider secrets, OpenAI API keys, Paddle API keys/webhook secrets, or admin bearer tokens. Large edited values are represented by hashes. No-op Save draft requests avoid noisy draft-save audit rows. Admins can read recent CMS audit entries through development/admin-only audit endpoints and the CMS Content Audit subtab. Runtime learner behavior is unchanged: CMS read path remains disabled by default and static JSON fallback remains available. Production RBAC and critical-change approval remain future work.

Step 5D-6e scenario editor usability refinement update: the Admin CMS Scenarios subtab now includes a safer structured editor for common scenario content (title/subtopic, description, setup message, first bot message guidance, context option titles, valid context keywords, custom context rules, invalid context redirect, goal text, can-do statements, opening/first-user-task/follow-up guidance, AI tutor instructions, wrap-up/final message guidance, hint example, and wrap/final turn counters), with compact local **Jump to** navigation, collapsible/visually separated Basic fields, Lesson setup, Context selection / choices, Conversation flow / response guidance, Wrap-up / summary guidance, and Advanced JSON sections, and concise helper text for normal content editors. `DefinitionJson` remains the canonical stored scenario definition; no per-field scenario database columns or EF migration were added. Structured edits parse the current `DefinitionJson`, update only known JSON paths, and write the merged valid JSON back to `DefinitionJson`, preserving unknown fields and advanced configuration in place. Structured fields remain the recommended normal editing path. Advanced JSON remains available as a visually separated technical fallback with `Format JSON` and `Validate JSON` for rare full-JSON changes. Save draft remains explicit and draft-only; invalid Advanced JSON or invalid structured numeric/required data is rejected before saving, and backend scenario validation still rejects invalid JSON, missing required fields, or accidental stable id/title/setup mismatches. CMS draft-save audit logging still records successful scenario saves with changed field names and before/after hashes without storing full scenario JSON bodies. Runtime learner behavior remains unchanged by default: the CMS read path is still disabled unless explicitly enabled, and static JSON fallback remains available.


## Controlled CMS runtime path next checks


Admin CMS workflow note: draft edits must still be published through **Versions & Publish** before they can be visible to runtime CMS mode. The editor now exposes a post-save **Go to Publish** path, but it does not auto-publish and does not replace the confirmed **Publish current draft** action. Keep static JSON as the default runtime source unless CMS runtime settings are explicitly enabled and verified with `tools/smoke_cms_runtime_content_read.ps1`.
Before external tester handoff, keep re-verifying the disabled-by-default CMS runtime read path with `CmsContent__UsePublishedSnapshotForRuntime=true`, `CmsContent__ReadPublishedSnapshotEnabled=true`, `CmsContent__ContentPackSlug=static-json-v1`, and the desired `CmsContent__FallbackToStaticJson` setting. Run the runtime diagnostic smoke (`tools/smoke_cms_runtime_content_read.ps1`) after importing/publishing CMS content. The latest local run confirmed `CmsPublishedSnapshot`, content pack `static-json-v1`, version 34, 6 topics, 26 scenarios, 3 prompt templates, 2 tutor behavior profiles, validation passed, and `fallbackUsed=false`; continue confirming those expectations after CMS changes. Also keep fallback verification in the manual/regression checklist: use a non-existent content pack slug with fallback enabled to confirm `StaticJson` plus `fallbackUsed=true`, and with fallback disabled to confirm a clear server-side unavailable result. Production RBAC and approval workflow remain future work.

## Windows direct-download release follow-ups

- Use [`docs/WINDOWS_RELEASE_SERVER_UPLOAD.md`](WINDOWS_RELEASE_SERVER_UPLOAD.md) to validate `artifacts\releases\windows\direct`, dry-run future static-server upload, and perform a manual upload only after server SSH access exists; this must not deploy the backend or run automatically.
- When packaging tester builds, keep Inno Setup as the primary Windows direct-download path and keep ZIP packaging only as an emergency/developer fallback.
- Verify testers report the Settings footer version with every bug report.
- Do not commit generated files from `artifacts\installers\windows` or `artifacts\releases\windows\direct`.
- Add update-check UI only in a future task, with manual confirmation and active-lesson protection.
- Complete code signing later before broad public distribution.
