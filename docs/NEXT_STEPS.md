# Next Steps

Review date: 2026-06-09.

## Immediate priority: production/server CMS/Admin verification

The current step is CMS/Admin server verification against `https://api.languagevoicetutor.com`. Password reset/change flows are working, PostgreSQL is healthy, and the static tester download page is deployed at `https://languagevoicetutor.com`, but external tester handoff remains blocked until CMS/Admin verification and the update/version-check system are complete. Public release is still not ready.

Use `docs/CMS_ADMIN_SERVER_VERIFICATION.md` as the runbook. Keep static JSON as the default runtime source with `CmsContent__UsePublishedSnapshotForRuntime=false` unless the published CMS snapshot has been explicitly validated and the runtime switch is intentionally enabled. No EF migration is expected for this verification step unless `dotnet ef migrations has-pending-model-changes` reports a real model change.

## Update/version-check system planned after CMS verification

The Windows installer installed-version check foundation is now implemented before the future in-app update UI/system:

- same installed version: asks the user to confirm reinstall;
- older installed version: allows the guided installer update flow;
- newer installed version: warns and blocks by default;
- running app replacement is guarded by Inno Setup close-application behavior.

The current app displays its version but does not implement update checking, update UI, or automatic update behavior yet. Future in-app update UI still must avoid prompting or installing during an active lesson.

## After latest Admin CMS Content step

Step 5D-6d completed Admin CMS refresh resilience and unsaved-change protection. Step 5D-6e is also complete for Admin CMS scenario editor usability refinement, while draft-save publish discoverability, required publish summaries with clearer validation errors, smoke/test audit filtering, and confirmed local CMS runtime published-snapshot reads remain in place. Admin refresh no longer logs out the admin because refresh auth uses the existing admin-only HTTP-only cookie, while the admin JWT remains memory-only in JavaScript. Browser Web Storage is not used: no `sessionStorage`, no `localStorage`, and no IndexedDB. The URL hash stores only safe workspace identifiers (`adminTab`, `cmsSubTab`, `selectedUserId`, `contentPackSlug`, `topicKey`, `scenarioKey`, `promptTemplateKey`, and `tutorId`). Selected user details restore through an admin-only lookup by `selectedUserId`, and selected CMS entities restore by stable keys.

Admin CMS Content now supports content pack overview, topic editing, scenario editing, structured scenario editing, full scenario JSON editing, prompt template editing, tutor behavior profile editing, validation/preview summary, and versions/publish/restore flows under the existing `/admin/` shell. Step 5D-6e is complete: the Scenarios editor now includes compact local **Jump to** navigation, collapsible/visually separated Basic fields, Lesson setup, Context selection / choices, Conversation flow / response guidance, Wrap-up / summary guidance, and Advanced JSON sections, and helper text for normal content editors. Structured fields remain the recommended normal editing path; Advanced JSON remains a visually separated technical fallback for rare full-JSON edits. `Format JSON` only pretty-prints JSON for easier editing. `Validate JSON` checks syntax and required scenario fields. Neither action saves or publishes; `Save draft` is required to persist CMS edits and remains draft-only. Unsaved CMS dirty state is tracked in memory against the last loaded/saved baseline, unsaved content is not stored in browser storage or the URL hash, and refresh/navigation/entity switching/logout warns before discarding edits. After successful Save draft operations, the editor shows **Go to Publish**; publishing changed content still happens only from **Versions & Publish**, requires a short change summary, and publish failures display backend validation details. Published versions remain immutable; restore creates a new published version rather than mutating old history. Runtime reads only published snapshots when CMS runtime mode is explicitly enabled; static JSON remains default. A local run confirmed `Source=CmsPublishedSnapshot`, `ContentPackSlug=static-json-v1`, `VersionNumber=34`, `FallbackUsed=False`, `ValidationPassed=True`, 6 topics, 26 scenarios, 3 prompt templates, and 2 tutor behavior profiles with `CmsContent__ReadPublishedSnapshotEnabled=true`, `CmsContent__UsePublishedSnapshotForRuntime=true`, `CmsContent__ContentPackSlug=static-json-v1`, and `CmsContent__FallbackToStaticJson=true`.

Runtime learner behavior remains unchanged by default. CMS reads remain controlled by configuration and disabled by default, with static JSON fallback still available. The CMS/Admin Content MVP is advanced enough to pause CMS feature work and continue test deployment preparation. Version `0.1.8-tester.1` passed internal smoke against the production-like setup, but external tester handoff is not approved. The next work must stay focused on tester handoff blockers before any new product features. Production RBAC, role-based content approval, production billing operations, and full external tester handoff are still not production-ready.

CMS draft-save audit logging is implemented for successful Admin CMS Save draft operations, and the Admin CMS Audit subtab now exposes recent CMS changes as read-only rows filtered by selected content pack, entity type, stable key text, and limit. Smoke/test audit entries are hidden by default, a **Show smoke/test entries** checkbox exists for debugging, and normal manual Admin CMS UI changes remain visible. Audit rows show metadata and shortened before/after hashes; full edited content bodies are not stored or displayed in audit rows. The later CMS governance step is a critical-change approval workflow, but it should wait until production roles/RBAC exist.

## Desktop backend profile checkpoint

Local desktop development keeps the default Backend URL `http://localhost:5000`. Inno tester/release packages default to `https://api.languagevoicetutor.com` through `scripts/package-windows-inno-release.ps1`, which passes `DesktopBackendBaseUrl` to `dotnet publish` and prints the selected Backend URL. Existing saved localhost settings may migrate to the deployed API only in tester/release builds where that deployed API is the build default; custom values must remain untouched. Confirm the generated `latest.json` non-secret `backendBaseUrl` with `scripts/validate-windows-direct-release.ps1` before handoff. Backend APIs remain server-side source of truth, the desktop must never contain OpenAI keys, production billing remains deferred, and public release/external tester handoff are not ready until server-connected CMS/Admin verification and update UI/system plus installed-version check verification, clean-machine install, and the controlled tester checklist pass.

## Recommended next product order

Do not propose or start new product features before the readiness blockers below. Version `0.1.8-tester.1` has internal smoke readiness only, not tester-release readiness.

1. Password recovery / password reset.
   - Implement and verify the complete password recovery flow.
   - Keep secrets, provider credentials, reset tokens, and copied environment values out of git.
2. Password change for signed-in users.
   - Implement and verify a signed-in password change flow.
   - Confirm existing sessions and error states behave predictably.
3. Connect and verify CMS/Admin content flow on the server.
   - Verify the server-connected CMS/Admin content workflow end to end.
   - Confirm draft, publish, runtime published-snapshot selection, fallback behavior, and audit expectations on the server before tester handoff.
4. Verify the already-deployed static tester download page points at the intended installer during final handoff smoke.
   - Provide a simple public page for the Language Voice Tutor Windows installer.
   - Keep static release hosting separate from backend API hosting.
   - Do not commit generated installer artifacts from `artifacts/`.
5. Implement a basic update system / update UI.
   - Use the existing direct-release manifest foundation.
   - Require manual confirmation and avoid update prompts during active lessons.
   - Do not introduce silent updates.
6. Run clean-machine tester release smoke.
   - Verify install, launch, registration/login/session restore, trial entitlement, lesson start, normal chat, Conversation Mode, TTS, translation, feedback, hints, lesson history, active/restored session behavior, update guidance, and uninstall/upgrade expectations on a clean or representative Windows machine.
7. Only then hand off to first controlled testers.
   - Handoff is not approved until Steps 1-6 pass.
   - Keep code signing deferred for now and document expected SmartScreen warnings.
   - Keep public release, production billing, production RBAC, app stores, and Mac/mobile work deferred.

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
- No production CMS/Admin readiness yet: production RBAC and approval workflow are not implemented, although bootstrap-admin CMS draft-save audit logging now exists.
- No code-side dialogue/prompt quality polishing; use CMS/Admin content workflows for future prompt/scenario/bot-behavior polishing.
- No mobile app-store bridge work before the desktop and billing gates are ready.
- No expansion of Study languages.
- No expansion of the release-ready Interface language list.
- No narrowing of the Native/Explanation language catalog.
- No lesson JSON rewrite.
- No public release declaration yet.
- No automatic update-check or update UI yet; the direct-download `latest.json` manifest for `0.1.8-tester.1` has been validated/uploaded as a hosting foundation only. A basic update UI/system remains a blocker before external tester handoff and must require manual confirmation without interrupting active lessons.

## After accepted Welcome screen and Lesson Chat sizing hardening

- Treat Welcome screen polish and Lesson Chat window auto-sizing as done for the current desktop hardening phase.
- CMS/Admin content MVP is advanced enough to pause feature work for server setup and test deployment preparation.
- Continue next with password recovery/reset, signed-in password change, server-connected CMS verification, a public download page, basic update UI/system, clean-machine tester smoke, and only then controlled tester handoff; keep production billing deferred and public release not ready.
- Keep production billing/Paddle rollout work deferred during server setup/test deployment preparation.
- Set up a domain email/provider later before enabling password reset delivery. Password reset remains disabled/not exposed as a working tester flow until that setup exists.


Implemented CMS draft-save audit logging details: successful Topic, Scenario (bounded fields, structured scenario fields, and full scenario JSON), Prompt Template, and Tutor Behavior Profile Save draft operations write `DraftSaved` rows to `cms_content_audit_logs`. Rows capture audit id, `createdAtUtc`, actor user id, actor email when available, content pack id and slug, entity type, entity id, stable key (`stableTopicKey`, `stableScenarioKey`, `templateKey`, or `tutorId`), changed field names, before/after SHA-256 hashes, source `AdminCms`, status, and request id when available. Audit rows intentionally do not store full before/after JSON snapshots, prompt/tutor source text snapshots, passwords, tokens, provider secrets, OpenAI API keys, Paddle API keys/webhook secrets, or admin bearer tokens. Large edited values are represented by hashes. No-op Save draft requests avoid noisy draft-save audit rows. Admins can read recent CMS audit entries through bootstrap-admin-protected audit endpoints and the CMS Content Audit subtab. Runtime learner behavior is unchanged: CMS read path remains disabled by default and static JSON fallback remains available. Production RBAC and critical-change approval remain future work.

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

## Password recovery/change follow-up (2026-06-08)

- Password recovery/reset and signed-in password change are implemented for backend and desktop and should be included in the next internal smoke.
- Configure password reset delivery on the server by adding SMTP settings to `/etc/languagevoicetutor/backend.env`; do not commit SMTP credentials or other secrets. Use `support@languagevoicetutor.com` as the production sender identity.
- Verify on the production backend that a registered user can request a reset, receive the code, reset the password, log in with the new password, and fail login with the old password. Verify signed-in Change password the same way.
- External tester handoff remains blocked until CMS/Admin server verification, update UI/system plus installed-version check verification, clean-machine smoke, and checklist completion are finished.

## Account recovery/change tester-readiness follow-up (2026-06-08)

- Verify password reset email delivery on the production server with SMTP values stored only in `/etc/languagevoicetutor/backend.env`; do not commit or paste real SMTP credentials into docs, logs, or release notes.
- Smoke test login failure, wrong-current-password change, short-password validation, reset-code failure, and successful reset/change against `https://api.languagevoicetutor.com` before external tester handoff.
- Confirm the hardened backend upload script leaves `/opt/languagevoicetutor/backend/releases/<version>/EnglishVoiceTutor.Api` executable and fails loudly if it cannot.
- External tester handoff remains blocked until CMS/Admin server verification, update UI/system plus installed-version check verification, clean-machine smoke, and checklist completion.

## Static tester download page follow-up

A basic public download page foundation is now prepared under `site/public/`. The page is static and reads `latest.json` from the existing Windows direct release folder at `/releases/windows/direct/latest.json`; it does not implement auto-update and does not replace the future update UI. It is only a tester download page for invited testers.

Before external tester handoff, deploy the static page only after reviewing `scripts/upload-static-site.ps1` with `-DryRun`, verifying the public HTTPS page loads the manifest, and confirming the fallback link remains available if the manifest request fails. External tester handoff is still blocked until the update UI/system and the clean-machine smoke checklist pass.
## CMS/Admin first-production initialization next step

Before external tester handoff, verify the Admin CMS selected content pack `static-json-v1`. If the overview reports that the pack has not been initialized, sign in as a bootstrap admin and click **Initialize from static JSON** (or POST `/api/admin/dev/cms/content-packs/static-json-v1/initialize-from-static-json`). This admin-only step imports the current packaged static JSON content into CMS draft/admin storage where supported.

The initialization action does not publish automatically and does not switch runtime. Keep `CmsContent__UsePublishedSnapshotForRuntime=false` until a separate publish/validation decision intentionally enables `CmsContent__UsePublishedSnapshotForRuntime=true`. Public release / external tester handoff remains blocked until this CMS/Admin initialization/verification, installed-version check verification, future update UI/system, and clean-machine smoke are complete.


## Windows installer version-check foundation status

Installed-version checking is now part of the Windows installer foundation. Before the future in-app update system is implemented, the Inno Setup installer now checks the installed Language Voice Tutor version from the existing installer identity.

- Same-version install asks for reinstall confirmation and only continues when the user confirms.
- Older installed version is treated as an update after a clear update message.
- Newer installed version warns and blocks by default rather than silently downgrading.
- Running app replacement is guarded with Inno Setup close-application behavior.

This is not the future in-app update UI. Future update UI still needs to check `latest.json`, verify SHA-256, avoid updates during active lessons, and guide the user through download/install. Active-lesson detection belongs to that future in-app update UI because the installer can only conservatively handle a running executable.

External tester handoff is still blocked until update/version-check verification and clean-machine smoke pass.
