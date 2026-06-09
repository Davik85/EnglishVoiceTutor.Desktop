# Current State

Review date: 2026-06-09.


## Manual desktop update UI

A simple manual update check is now implemented near the top of normal Settings for the Windows desktop app; it is user-facing and not Diagnostics-only. The app reads `https://languagevoicetutor.com/releases/windows/direct/latest.json` only when the user clicks **Check for updates**. It validates `productName`, `appId`, `platform`, and `architecture`, compares the installed app version with the manifest version using SemVer-style tester prerelease rules, and shows simple dialogs for up-to-date, update-available, newer-than-manifest, and connectivity/error cases.

Downloads are manual only. If a newer version is available, the app asks before downloading and installing, stores the installer in a user-writable LocalAppData updates cache, verifies SHA-256 against `installerSha256`, deletes failed verification downloads, and only then asks whether to start the installer. It does not perform a silent auto-update, does not run an installer before hash verification, and does not store secrets or tokens in update files. Progress and Lesson History remain account-scoped local data and must be preserved across app restart, Windows restart, reinstall, and update.

External tester handoff remains blocked until the clean-machine smoke test and controlled tester checklist pass.


## Internal smoke readiness for v0.1.17-tester.1

Version `0.1.17-tester.1` passed the basic internal end-to-end smoke test against the real production-like server setup. This is **internal smoke readiness only**. It is not approval for external tester handoff and does not mean the app is publicly released.

Confirmed working in the internal smoke test:

- Desktop app starts.
- User registration works.
- Trial entitlement is issued.
- Lessons start successfully.
- Normal lesson chat works.
- Conversation Mode works.
- TTS works in normal chat and Conversation Mode.
- Translation works.
- Feedback works.
- Hints work.
- Lesson history is saved.
- Active/restored session behavior works after closing and reopening the app.
- Backend is reachable at `https://api.languagevoicetutor.com`.
- Backend health endpoint is healthy.
- Backend database health endpoint is healthy.
- PostgreSQL migrations have been applied on the server.
- Static Windows direct release hosting works.
- `latest.json` for `0.1.17-tester.1` is available from the production domain.
- The Windows installer was generated, validated, uploaded, and server-side release files were verified.

External tester handoff remains blocked until all of these are completed:

1. CMS/Admin content flow connected and verified on the server.
2. Clean-machine tester release smoke and controlled tester checklist completion.

Password reset/change flows, the static tester download page, CMS/Admin draft initialization, and the manual update UI are now working, but they do not remove the clean-machine smoke-test blocker.


## Current production/server CMS/Admin verification step

The current step is to connect and verify the existing CMS/Admin implementation on the production server. The backend is deployed at `https://api.languagevoicetutor.com`, PostgreSQL is healthy, the static tester download site is deployed at `https://languagevoicetutor.com`, and password reset/change flows are working. CMS/Admin server verification remains in progress. Runtime content must continue to default to static JSON unless `CmsContent__UsePublishedSnapshotForRuntime=true` is intentionally enabled after published snapshot validation. Public release and external tester handoff remain blocked until CMS/Admin server verification and the update/version-check system are completed.

## Accepted MVP/source-of-truth pause point

Language Voice Tutor Desktop MVP works, the backend works locally, the production-like backend has passed the `0.1.17-tester.1` internal smoke baseline, and the PostgreSQL + EF Core persistence foundation exists. Desktop auth session storage is protected with Windows DPAPI, the single active lesson guard is accepted, and the backend remains the source of truth. The desktop must not store OpenAI API keys or call OpenAI directly.

Desktop backend profile state: local development builds default to `http://localhost:5000`, while Inno tester/release packages default to `https://api.languagevoicetutor.com` via the `DesktopBackendBaseUrl` build property passed by `scripts/package-windows-inno-release.ps1`. Empty saved Backend URL settings use the current build default. Saved legacy localhost settings migrate to the deployed API only when the current build default is the deployed API; custom tester/developer values are preserved and still normalized without trailing slashes. Settings/Diagnostics continue to show the active Backend URL for tester support. The backend remains the source of truth, the desktop still does not contain OpenAI keys or call OpenAI directly, production billing remains deferred, and public release/external tester handoff remain blocked until server-connected CMS/Admin verification, manual update check verification, clean-machine install, and the controlled tester checklist pass.

The CMS/Admin Content MVP is advanced enough to pause and continue test deployment preparation later: `/admin/` works; the CMS Content workspace has Overview, Topics, Scenarios, Prompts, Tutors, Validation & Preview, Versions & Publish, and Audit tabs; `Save draft` does not publish; publishing requires **Versions & Publish** plus a publish summary; published versions are immutable; restore creates a new published version; CMS draft-save audit logging works. The runtime published-snapshot path was checked locally, but runtime still defaults to static JSON unless explicitly switched.

Public release is not ready. The deployed backend API is available at `https://api.languagevoicetutor.com`, and static HTTPS site hosting is available at `https://languagevoicetutor.com`. Version `0.1.17-tester.1` passed internal smoke against the production-like setup, but external tester handoff is still blocked by server-connected CMS/Admin verification, manual update check verification, clean-machine tester release smoke, and the controlled tester checklist. Production billing, production RBAC, critical-change approval workflow, Microsoft Store, App Store, Google Play, and Mac version are deferred. Language Voice Tutor remains a global/international product; do not introduce YooKassa or Russia-only billing assumptions.

Windows direct-download release state: Inno Setup is the primary installer track with stable AppId `LanguageVoiceTutor.Desktop`; Velopack is rejected/deprecated; ZIP packaging is only an emergency/developer fallback. Expected installer output is `artifacts\installers\windows\LanguageVoiceTutorSetup-{version}.exe`; server-ready direct-download files are under `artifacts\releases\windows\direct` and include `latest.json`, `changelog.json`, `known-issues.json`, `checksums.sha256`, and `LanguageVoiceTutorSetup-{version}.exe`. The current app displays `Version: v{version}` in Settings and includes a simple Settings **Check for updates** action that reads `latest.json` only on explicit user action, validates the manifest, asks before downloading/installing, verifies SHA-256 before offering to start the installer, and does not silently auto-update. The public tester download page also derives its **Download for Windows** link only from `latest.json`; it must not hardcode old installer filenames, and after every Windows release upload the displayed page version and the actual downloaded installer filename must both be verified. `scripts/validate-windows-direct-release.ps1` validates local release artifacts; `scripts/upload-windows-direct-release.ps1` supports dry-run/future SCP without hardcoded server secrets. Code signing remains deferred, so SmartScreen warnings are expected. Generated `artifacts/` files must not be committed. External tester handoff is still blocked until server-connected CMS/Admin verification, manual update check verification, clean-machine install, and the controlled tester checklist pass.

## Step 5D-6d current state — Admin CMS refresh resilience and unsaved-change protection

Latest Admin CMS Content step completed: Step 5D-6e Admin CMS scenario editor usability refinement, including compact local **Jump to** navigation, collapsible/visually separated scenario sections, helper text for normal content editors, and Advanced JSON as a visually separated technical fallback while structured fields remain the recommended normal editing path. Admin CMS publish discoverability update: `Save draft` remains draft-only and never publishes. After a successful draft save, admins now see “Draft saved. To apply this content to runtime, publish the current draft.” plus a **Go to Publish** action that opens **Versions & Publish** without bypassing the existing confirmation-based **Publish current draft** flow. Publishing changed content requires a publish change summary; the Admin CMS blocks blank summaries after draft saves and now shows backend publish validation errors/warnings instead of only a generic invalid-request message. Runtime reads only published snapshots when CMS runtime mode is explicitly enabled; static JSON remains the default and the CMS runtime path remains disabled by default.

Step 5D-6d is complete. The bootstrap-admin Admin shell under `/admin/` now restores safe workspace selection state across browser refresh without using browser Web Storage. Admin authentication survives refresh through the existing admin-only HTTP-only cookie, while the admin JWT remains memory-only in JavaScript. The Admin shell does not use `sessionStorage`, `localStorage`, or IndexedDB for auth, selected workspace state, or unsaved content.

After a valid admin session is verified, the Admin shell parses only safe URL hash identifiers: `adminTab`, `cmsSubTab`, `selectedUserId`, `contentPackSlug`, `topicKey`, `scenarioKey`, `promptTemplateKey`, and `tutorId`. Selected user details are restored by `selectedUserId` through an admin-only user lookup. Selected CMS topic, scenario, prompt template, and tutor behavior profile details are restored by stable keys/IDs. Passwords, tokens, prompt bodies, full scenario JSON, tutor profile JSON, and unsaved draft field values are intentionally not stored in the URL hash or browser storage.

Unsaved CMS changes are tracked in memory by comparing the current editor values with the last loaded or successfully saved baseline. Topic, scenario, prompt template, and tutor behavior profile editors show a visible Unsaved changes indicator when dirty. The warning is shown before browser refresh, tab close, top-level admin tab switching, CMS sub-tab switching, selecting another CMS entity, publish/restore reload flows, or logout would discard edits. `Save draft` is the explicit persistence action: a successful save clears the dirty indicator, and a failed save keeps it. Unsaved content is not persisted in browser storage or the URL hash.

## Current Admin CMS Content capabilities

The backend Admin shell exposes a single main `CMS Content` sidebar tab with internal sub-tabs for Overview, Topics, Scenarios, Prompts, Tutors, Validation & Preview, Versions & Publish, and Audit. The workspace supports content pack overview, topic editing, scenario editing through recommended structured fields or a visually separated Advanced JSON technical fallback, prompt template editing, tutor behavior profile editing, validation and preview summary, version listing/detail, publish, and restore/rollback flow. Topics, scenarios, prompt templates, and tutor behavior profiles can be selected conveniently by clicking table rows; compact visible Select buttons remain as fallbacks and the selected row is highlighted.

Scenario editing includes bounded fields, the recommended structured scenario sections, compact local **Jump to** navigation, helper text for normal content editors, and a visually separated Advanced JSON technical fallback. Structured sections cover Basic fields, Lesson setup, Context selection / choices, Conversation flow / response guidance, Wrap-up / summary guidance, and Advanced JSON. `Format JSON` only pretty-prints/re-indents JSON in the editor to make it easier to read and edit. `Validate JSON` checks syntax and required scenario fields before saving. Neither `Format JSON` nor `Validate JSON` saves, publishes, or persists changes; admins must still click `Save draft` to persist CMS edits, and draft saves do not publish runtime content.

Runtime learner behavior remains unchanged by default. The CMS read path remains controlled by configuration and disabled by default for learner runtime; `CmsContent:ReadPublishedSnapshotEnabled` defaults to `false`, `CmsContent:ContentPackSlug` defaults to `static-json-v1`, and `CmsContent:FallbackToStaticJson` defaults to `true`. Static JSON fallback remains available when CMS reads are disabled, missing, invalid, corrupt, or fail. External tester handoff remains paused until the CMS/Admin content MVP is ready enough for practical content changes without code edits. Production billing remains deferred and public release remains not ready. No EF migration, lesson JSON edit, prompt/tutor source edit, desktop UI change, admin HTML/CSS/JS change, billing/Paddle/subscription/entitlement/payment change, schema change, or password reset behavior change is part of this documentation update.

The Admin CMS is still not production-ready. Production RBAC is not implemented. CMS draft-save audit logging is implemented for successful Admin CMS Save draft operations, and the Admin CMS Audit subtab exposes recent CMS changes as read-only rows filtered by selected content pack, entity type, stable key text, and limit. Smoke/test entries are hidden by default, a **Show smoke/test entries** checkbox exists for debugging, and normal manual Admin CMS UI changes remain visible. Audit rows show metadata and shortened hashes only; full edited content bodies are not stored or displayed in audit rows. Role-based critical-change approval remains future work and should wait until production roles exist.

## Short summary

EnglishVoiceTutor currently has a working Windows desktop MVP backed by a working backend, PostgreSQL, and EF Core persistence foundation. The recent desktop release-hardening block accepted the core lesson/voice/TTS flow and the backend-enforced single-active-lesson guard. The Windows direct-download installer foundation is now Inno Setup for the public `Language Voice Tutor` product name; ZIP packaging remains only an emergency/developer fallback. Public release is not declared ready. Desktop backend profile state: local development builds default to `http://localhost:5000`, while Inno tester/release packages default to `https://api.languagevoicetutor.com` via the `DesktopBackendBaseUrl` build property passed by `scripts/package-windows-inno-release.ps1`. Empty saved Backend URL settings use the current build default. Saved legacy localhost settings migrate to the deployed API only when the current build default is the deployed API; custom tester/developer values are preserved and still normalized without trailing slashes. Settings/Diagnostics continue to show the active Backend URL for tester support. The backend remains the source of truth, the desktop still does not contain OpenAI keys or call OpenAI directly, production billing remains deferred, and public release remains blocked until a clean-machine install and the controlled tester checklist pass.

The CMS/Admin Content MVP is advanced enough to pause for server setup/test deployment preparation. External tester handoff remains blocked until clean-machine install passes and the controlled tester checklist passes. Production billing remains deferred.

## Product architecture principle

- Product context remains global, cross-platform, and provider-agnostic.
- This is not a Russia-only product; do not introduce YooKassa, Russian payment flows, or Russia-only billing assumptions.
- The backend is the source of truth for account, trial, subscription, Premium/free status, daily free allowance, usage, limits, lesson history, active lesson state, payments, entitlements, and AI/TTS/STT calls.
- Desktop and future mobile clients must rely on backend account/subscription/entitlement/active-lesson state, not local payment or local session assumptions.
- Desktop must call backend APIs only, must not store an OpenAI API key, and must not call OpenAI directly.
- `OPENAI_API_KEY` is backend-only, needed only for real AI/TTS/STT testing, must never be committed, and must never be sent to testers.
- Paddle is the current desktop/web billing provider adapter, but core backend subscription, entitlement, and access logic must remain provider-agnostic.
- Do not change Paddle, billing, subscription, entitlement, lesson JSON, Study languages, Interface languages, or Native/Explanation language catalog without an explicit later task. Step 5D-1 added backend CMS schema foundation, Step 5D-2 added a bootstrap-admin CMS static JSON import foundation, Step 5D-3 added a safe backend published-snapshot read/status path, Step 5D-4 added backend draft read/update validation/preview APIs, Step 5D-5 added backend publish/version/restore APIs, and Step 5D-6 added a Admin CMS Content UI shell. Learner-facing runtime CMS rollout and production CMS operations still require explicit future tasks.


## Future CMS governance requirements

CMS draft-save audit logging is implemented before production CMS operations. Successful Admin CMS Save draft operations are audited with actor user id, actor email when available, timestamp UTC, content pack slug, entity type (`Topic`, `Scenario`, `PromptTemplate`, or `TutorBehaviorProfile`), entity id and stable key, changed fields, before/after hashes, reason, source `AdminCms`, status, and request/correlation id when available. The current Admin CMS Audit view is read-only and intentionally shows metadata plus shortened hashes rather than full prompt, scenario, tutor, or before/after JSON bodies.

Future critical CMS changes should require approval after production roles are implemented. Critical changes include prompt template changes, tutor behavior/safety changes, large scenario changes, disabling important content, and publish actions. Future roles may include Content Editor, Content Reviewer, and Admin / Owner. Draft editing and approval should be separated once those roles exist. For now, keep the existing development-only admin flow and the current confirmation dialogs for publish and restore.

## Accepted desktop MVP state

Implemented and accepted for the current controlled desktop MVP:

- Windows desktop app builds.
- Backend builds.
- PostgreSQL + EF Core persistence foundation works.
- Lesson content audit passes.
- Account register/login/logout and session restore validation work through backend APIs.
- Backend lesson history is visible and preserved for signed-in accounts.
- Normal Lesson Chat works.
- Conversation Mode works.
- TTS works.
- Voice recognition/transcription works and writes text correctly.
- Translation works.
- Hints work.
- Feedback works.
- Final lesson summary appears.
- Desktop upgrade/paywall UI exists for sandbox validation with manual Refresh status.
- Packaged Release hides Diagnostics by default.
- Welcome screen UI is accepted for the current desktop hardening phase: the hero message is neutral for a multi-language learning product, no longer positions the product as English-only, uses a large cover image, keeps text in a compact translucent top overlay, and keeps Start lesson / Settings actions in a translucent bottom overlay.
- Lesson Chat window sizing is accepted: entering Lesson Chat auto-expands the main window if it is too small, targets a preferred 1320 × 940 layout with a 1180 × 820 readability floor, does not force fullscreen or maximize, does not shrink a larger user-sized window, and keeps the expanded window within the visible monitor working area where possible.

Prompt, scenario, dialogue, and bot-behavior quality polishing is intentionally deferred to the CMS/Admin content MVP, which now starts before external tester handoff so edits can later be validated, previewed, versioned, and rolled back safely.

## Current CMS/Admin content MVP decision

External tester handoff is paused until the CMS/Admin content MVP is ready enough for practical content changes without code edits. Step 5D-1 added the backend CMS schema foundation, Step 5D-2 added a safe bootstrap-admin CMS static content import foundation, Step 5D-3 added a safe backend read/status path for the latest published CMS snapshot, Step 5D-4 added backend draft read/update validation/preview APIs, Step 5D-5 added publish/version/restore APIs, and Step 5D-6 added the bootstrap-admin Admin CMS Content workspace. The importer reads packaged lessons, file-backed prompt templates, and tutor behavior profiles into the stable `static-json-v1` / `Static JSON Baseline` content pack, validates the import, records import audit logs, and creates a published baseline snapshot when validation passes. The read path can verify the snapshot hash, deserialize the snapshot, validate required topics/scenarios/prompts/tutors, and return bootstrap-admin status at `GET /api/admin/dev/cms/published-content/status` without exposing prompt bodies. Runtime learner behavior still remains static JSON by default, and production CMS governance is not complete.

The CMS/Admin content MVP is content-focused and should cover lesson topics, subtopics/situations, starter/setup messages, prompt templates, tutor behavior instructions, hint/feedback/summary prompt configuration where applicable, validation, preview, versioning, rollback, and draft/published workflow. Production billing, Paddle management, payment editing, entitlement editing, broad user management, mobile-specific CMS, full production Admin, secrets, direct OpenAI key handling, study-language changes, Interface-language changes, and Native/Explanation-language changes remain out of scope.

Planning baseline: `docs/CMS_ADMIN_PLANNING.md`. Detailed plan: `docs/cms-content-mvp-plan.md`. Current runtime lesson loading remains unchanged by default and continues to use the static JSON/content behavior. The `CmsContent` configuration defaults keep `ReadPublishedSnapshotEnabled=false`, `ContentPackSlug=static-json-v1`, and `FallbackToStaticJson=true`; imported CMS rows and snapshots do not affect learners unless a later explicitly approved runtime integration enables CMS reads behind that disabled-by-default flag. Static JSON remains the fallback when CMS reads are disabled, missing, invalid, corrupt, or fail. The bootstrap-admin Admin CMS Content workspace can load `static-json-v1`, view content summaries and lists, edit topics, scenarios through the recommended structured sections or the technical Advanced JSON fallback, prompt templates, and tutor behavior profiles, run validation, load preview summaries, list versions, publish a changed draft with confirmation and required publish summary, and restore a previous version by creating a new published version with confirmation. Format/validation actions do not save; `Save draft` is required. It does not add a learner runtime switch, lesson JSON edits, prompt/tutor file edits, OpenAI calls, billing changes, study-language changes, interface-language changes, production CMS/RBAC, production approval workflow, or public release readiness; draft-save audit logging and read-only Admin CMS recent-change visibility are implemented for successful Save draft operations.

## Windows installer and ZIP fallback state

The recommended Windows direct-download installer flow is Inno Setup:

```powershell
cd C:\dev\EnglishVoiceTutor.Desktop
powershell -ExecutionPolicy Bypass -File .\scripts\package-windows-inno-release.ps1 -Version 0.1.0
```

Expected installer artifact:

```text
artifacts\installers\windows\LanguageVoiceTutorSetup-0.1.0.exe
```

The installer uses the public product name `Language Voice Tutor`, the stable AppId `LanguageVoiceTutor.Desktop`, Program Files by default, normal Windows uninstall integration, a Start Menu shortcut, an optional Desktop shortcut, and an optional launch-after-install action. Velopack is deprecated/rejected because its one-click installer does not match the desired release-like installer UX.

ZIP packaging remains available only as an emergency/developer fallback:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\package-tester-release.ps1
```

Expected default ZIP:

```text
artifacts\packages\LanguageVoiceTutor.Desktop-win-x64-self-contained.zip
```

External tester handoff remains paused while the CMS/Admin content MVP foundation continues. Public release is not declared ready, code signing is not done, Microsoft Store/MSIX remains deferred, and update UX is not implemented.

## Backend-required packaged desktop scope

The packaged desktop app requires a reachable backend for:

- login/register/logout/session restore validation;
- backend lesson history;
- lesson start;
- AI bot replies;
- voice transcription/STT;
- TTS;
- translation;
- hints;
- feedback;
- summary;
- subscription/access checks;
- active lesson guard;
- remote active lesson release.

Backend-unavailable checks are resilience-only and must not be treated as full functional acceptance.

## Release Diagnostics state

- Packaged Release hides Diagnostics by default.
- Diagnostics is visible in Release only if `EVT_DESKTOP_DIAGNOSTICS=1` is set locally before app launch.
- Do not commit `EVT_DESKTOP_DIAGNOSTICS` in scripts, settings, shortcuts, or machine-specific docs.
- Diagnostics and copied diagnostics output must mask secrets, tokens, API keys, environment variables, lesson messages, audio paths, and lesson history content.

## Auth/session storage state

Desktop authenticated session persistence is now part of the tester-readiness foundation. The desktop does not store raw passwords; token/session data is stored under the current user app-data folder with Windows DPAPI protection. Logout clears persisted auth session data. Reinstall/update should preserve user app data and session storage. Same-version installer reinstall confirmation remains in place. The basic manual in-app update UI now checks `latest.json`, validates the manifest, verifies SHA-256 before offering to open the installer/folder, and does not silently auto-update. External tester handoff remains blocked until persisted-session verification and clean-machine smoke pass.

The desktop still uses a local file named `auth-session.json` under the app data folder, but the current implementation writes a Windows DPAPI-protected Base64 payload using `ProtectedData.Protect(..., DataProtectionScope.CurrentUser)`. The protected payload contains the serialized session fields after decryption by the same Windows user: access token, token type, expiry, and user DTO. Loading can migrate an old plaintext JSON payload by reading it once and saving it back protected. Documentation must not describe the current token as raw plaintext storage.

## Active lesson state

Single active lesson protection is implemented and accepted:

- Backend enforces one active lesson per account.
- Desktop and future mobile clients must follow the same backend rule.
- Lesson Chat sends a backend heartbeat for the active lesson session about every 30 seconds.
- The backend treats an active lesson as blocking only while its heartbeat is fresh; current freshness window is 2 minutes.
- A stale heartbeat no longer blocks the account forever; stale active sessions are marked `Abandoned` when a new lesson starts after the freshness window.
- The user can choose to end an active lesson on another device and continue.
- Remote release uses the backend active lesson release endpoint and the old session becomes `Abandoned`.
- The old device/session cannot continue.
- Old heartbeat and old lesson-bound message creation are rejected with `lesson_session_ended_elsewhere`.
- UI wording must stay neutral and must not frame this as fraud language.
- `tools/smoke_single_active_lesson_guard.ps1` passes in the accepted Windows/backend test environment.

## Study language status

Study languages remain exactly:

- English
- French
- German
- Portuguese
- Spanish
- Italian

Study language is the language the user practices or learns in lessons. It is separate from Native/Explanation/Interface language.

## Interface and Native/Explanation language status

Release-ready Interface languages remain exactly:

- `en`
- `es`
- `fr`
- `de`
- `it`
- `pt`
- `ru`
- `pl`
- `ar`
- `ja`
- `ko`
- `sr`
- `hr`
- `bg`

Native/Explanation languages remain the broad catalog from the localization foundation. The current interface localization phase is closed for release hardening; future Interface languages should be added only 1-2 at a time after full localization QA.

## EF migration status

Current confirmed migrations:

- `20260518000000_InitialProductStorageSchema`
- `20260520120000_AddLessonSummaryContentFields`
- `20260520132002_AddUsageEventStatusAndStudyLanguage`
- `20260520150000_AddDailyUsageChatReplyCount`
- `20260524061817_AddSubscriptionFoundationV1`
- `20260528000000_AddPaddleWebhookEvents`
- `20260528010000_AddPaddleSubscriptionLifecycleSnapshotV1`
- `20260529000000_AddPaddlePaymentPersistenceV1`
- `20260601090000_AddLessonSessionHeartbeat`
- `20260601120000_AddPasswordResetFoundation`
- `20260603120000_AddCmsContentFoundation`
- `20260604120000_AddCmsScenarioDefinitionJson`
- `20260604121000_AddCmsDraftSaveAuditMetadata`

Latest confirmed EF migration: `20260604121000_AddCmsDraftSaveAuditMetadata`.

## Backend Linux deployment foundation

The backend deployment foundation for Ubuntu 24.04 is prepared in documentation and scripts only. Local packaging uses `scripts/package-backend-linux-release.ps1` to publish `backend/EnglishVoiceTutor.Api/EnglishVoiceTutor.Api.csproj` as a self-contained `linux-x64` backend archive under `artifacts/packages/backend`. Manual upload support uses `scripts/upload-backend-linux-release.ps1` to place a zip on the VPS, extract it to `/opt/languagevoicetutor/backend/releases/{version}`, and update `/opt/languagevoicetutor/backend/current`. Server templates live under `docs/server/`, and the full operator guide is `docs/BACKEND_SERVER_DEPLOYMENT.md`. No backend deployment has been completed by this repository change, no EF migration was added, and generated `artifacts/` files must not be committed.

Production billing remains deferred for this test deployment path. Paddle/provider keys are not required unless checkout/billing tests are deliberately enabled later. External tester handoff remains blocked until backend deployment, clean-machine install, and controlled tester checklist validation all pass.

## Current smoke/audit scripts

Current documented smoke/audit scripts:

- `tools/run_desktop_release_gate.ps1`
- `tools/audit_lesson_content.ps1`
- `tools/audit_interface_localization.ps1`
- `tools/audit_desktop_backend_boundary.ps1`
- `tools/smoke_single_active_lesson_guard.ps1`
- `tools/smoke_admin_foundation.ps1`
- `tools/audit_admin_shell.ps1`
- `tools/audit_cms_content_foundation.ps1`
- `tools/smoke_cms_content_import.ps1`
- `tools/smoke_cms_published_content_read.ps1`
- `tools/smoke_cms_content_admin_api.ps1`
- `tools/smoke_cms_publish_rollback.ps1`
- `tools/smoke_cms_draft_save_audit.ps1`
- `tools/smoke_cms_structured_scenario_editor.ps1`
- `tools/smoke_cms_runtime_content_read.ps1`
- `tools/smoke_billing_checkout.ps1`
- `tools/smoke_paddle_checkout_adapter.ps1`
- `tools/smoke_paddle_checkout_client_token_guard.ps1`
- `tools/smoke_paddle_checkout_live_sandbox.ps1`
- `tools/smoke_paddle_webhook_ingestion.ps1`
- `tools/smoke_paddle_subscription_lifecycle.ps1`
- `tools/smoke_paddle_entitlement_extension.ps1`
- `tools/smoke_paddle_payment_persistence.ps1`
- `tools/smoke_paddle_cancellation_past_due_policy.ps1`
- `tools/smoke_paddle_canceled_paused_expiry_policy.ps1`
- `tools/smoke_paddle_resumed_activated_snapshot_policy.ps1`
- `tools/smoke_paddle_production_config_guard.ps1`

The desktop release gate (`tools/run_desktop_release_gate.ps1`) runs restore/build/release build/backend build plus lesson-content, interface-localization, and desktop-backend-boundary audits. Run EF checks with the gate only when backend schema validation is required. The single active lesson guard smoke is documented separately because it requires a running backend and accounts/test setup.

## Validation status

Accepted manual validation:

- Tester ZIP was copied to and verified on another Windows device.
- Extracted package launched successfully.
- Diagnostics was hidden by default in packaged Release.
- Backend connection, account login, and backend history were verified from the extracted package.
- Core Lesson Chat / Conversation Mode / TTS / transcription / translation / hints / feedback / summary flow was accepted.
- Single active lesson guard, heartbeat stale protection, remote active lesson release, and old-session invalidation were accepted.

Local automated validation expected before the next release handoff, after CMS/Admin content MVP is ready:

1. `powershell -ExecutionPolicy Bypass -File .\tools\run_desktop_release_gate.ps1`
2. `powershell -ExecutionPolicy Bypass -File .\tools\smoke_single_active_lesson_guard.ps1` with the required backend/test setup.
3. `powershell -ExecutionPolicy Bypass -File .\scripts\package-windows-inno-release.ps1 -Version 0.1.0`

## Deferred scope / not ready yet

- Public release is not declared ready.
- Production billing is not ready and remains deferred while CMS/Admin content MVP is prioritized before tester handoff.
- Paddle production webhook delivery, production checkout configuration, provider credentials, product/price mapping, environment separation, and manual production smoke verification are not complete.
- Full production CMS/Admin operational readiness remains deferred. Development/admin CMS content APIs and Admin CMS UI now include draft read/update, structured scenario editing, validation/preview, version list/detail, required-summary publish, restore/rollback, draft-save audit visibility with smoke/test filtering, and a locally verified runtime published-snapshot read path. External tester handoff remains paused.
- Prompt/scenario/bot-behavior quality polishing is deferred to the content-focused CMS/Admin MVP.
- Installer/signing and Microsoft Store packaging are not complete.
- Mobile app implementation and mobile app-store entitlement bridge are not complete.
- Refund handling, chargeback handling, manual revocation automation, and background subscription reconciliation are not complete.

## Step 5B-9 account UX hardening

- Packaged Release continues to hide Diagnostics by default and the tester package keeps the existing backend/ngrok URL behavior.
- Account sessions are stored outside the extracted app folder in the current Windows user's roaming app data and protected with Windows DPAPI. Login/register writes the protected `auth-session.json`; logout deletes it; a temporarily unavailable backend does not delete the stored session unless the backend clearly rejects the token.
- Settings localization was tightened for Account and Progress. Russian Progress text is Russian, Russian Account signed-out status is localized, and the Russian Save button has enough width for `Сохранить`.
- Password reset backend foundation exists, but real email delivery is not configured because there is no domain email/provider yet. Password reset is disabled by default, reset tokens are generated securely, only token hashes are stored, and no secrets are committed.


Implemented CMS draft-save audit logging details: successful Topic, Scenario (bounded fields, structured scenario fields, and full scenario JSON), Prompt Template, and Tutor Behavior Profile Save draft operations write `DraftSaved` rows to `cms_content_audit_logs`. Rows capture audit id, `createdAtUtc`, actor user id, actor email when available, content pack id and slug, entity type, entity id, stable key (`stableTopicKey`, `stableScenarioKey`, `templateKey`, or `tutorId`), changed field names, before/after SHA-256 hashes, source `AdminCms`, status, and request id when available. Audit rows intentionally do not store or display full before/after JSON snapshots, prompt/tutor source text snapshots, passwords, tokens, provider secrets, OpenAI API keys, Paddle API keys/webhook secrets, or admin bearer tokens. Large edited values are represented by hashes. No-op Save draft requests avoid noisy draft-save audit rows. Admins can read recent CMS audit entries through bootstrap-admin-protected audit endpoints and the CMS Content Audit subtab, which is aligned to the selected content pack (`static-json-v1` by default) and supports entity type, stable key text, limit, and Refresh audit controls. Runtime learner behavior is unchanged: CMS read path remains disabled by default and static JSON fallback remains available. Production RBAC and critical-change approval remain future work.

Structured scenario editor update: the Admin CMS Scenarios subtab now includes a safer structured editor for common scenario content (title/subtopic, description, setup message, first bot message guidance, context option titles, valid context keywords, custom context rules, invalid context redirect, goal text, can-do statements, opening/first-user-task/follow-up guidance, AI tutor instructions, wrap-up/final message guidance, hint example, and wrap/final turn counters). Those fields are grouped into collapsible Basic, Setup, Choices, Flow, and Wrap-up sections with concise helper text and local jump buttons for long scenarios. `DefinitionJson` remains the canonical stored scenario definition; no per-field scenario database columns or EF migration were added. Structured edits parse the current `DefinitionJson`, update only known JSON paths, and write the merged valid JSON back to `DefinitionJson`, preserving unknown fields and advanced configuration in place. Advanced JSON remains available as a visually separated technical section with `Format JSON` and `Validate JSON` for rare technical changes. Save draft remains explicit; invalid Advanced JSON or invalid structured numeric/required data is rejected before saving, and backend scenario validation still rejects invalid JSON, missing required fields, or accidental stable id/title/setup mismatches. CMS draft-save audit logging still records successful scenario saves with changed field names and before/after hashes without storing full scenario JSON bodies. Runtime learner behavior remains unchanged by default: the CMS read path is still disabled unless explicitly enabled, and static JSON fallback remains available.


## Controlled CMS runtime read path status

Confirmed local runtime read: with `CmsContent__ReadPublishedSnapshotEnabled=true`, `CmsContent__UsePublishedSnapshotForRuntime=true`, `CmsContent__ContentPackSlug=static-json-v1`, and `CmsContent__FallbackToStaticJson=true`, backend logs confirmed `Source=CmsPublishedSnapshot`, `ContentPackSlug=static-json-v1`, `VersionNumber=34`, `FallbackUsed=False`, `ValidationPassed=True`, `TopicCount=6`, `ScenarioCount=26`, `PromptTemplateCount=3`, and `TutorBehaviorProfileCount=2`. Static JSON remains the default runtime lesson-content source. A controlled backend runtime CMS mode now exists behind `CmsContent:UsePublishedSnapshotForRuntime`; it also requires the existing `CmsContent:ReadPublishedSnapshotEnabled` published snapshot read flag. The content pack slug defaults to `static-json-v1`, and `CmsContent:FallbackToStaticJson` remains the safe fallback switch. When enabled, runtime reads only the current immutable published snapshot for the configured pack, maps topics, scenarios including `DefinitionJson`, prompt templates, and tutor behavior profiles into the same lesson-content model family used by the static content baseline, validates required counts and fields, and logs only source/slug/version/hash/count/validation metadata. Draft saves are not runtime-visible until publish. The bootstrap-admin runtime diagnostic endpoint is `/api/admin/dev/cms/runtime-content/status`, and the public non-secret source flag endpoint is `/api/cms/runtime-content/source-status`. External tester handoff remains paused until server CMS/Admin verification and update/version-check work are complete; production RBAC and approval workflow remain future work.

## Windows direct-download release manifest foundation

Inno Setup remains the primary Windows direct-download installer track. `scripts/package-windows-inno-release.ps1` builds the primary installer in `artifacts\installers\windows` and now also prepares server-ready direct-download files in `artifacts\releases\windows\direct`: the copied installer, `latest.json`, `changelog.json`, `known-issues.json`, and `checksums.sha256`. `latest.json` is used by the manual in-app update-check; the current desktop app does not fetch it automatically. The update UI uses manual user confirmation and must not run during an active lesson. Code signing remains deferred, and generated `artifacts\` files must not be committed. Settings now displays the installed app version in the footer so testers and support can identify exact builds in bug reports. Local validation and optional manual SCP upload are documented in [`docs/WINDOWS_RELEASE_SERVER_UPLOAD.md`](WINDOWS_RELEASE_SERVER_UPLOAD.md); upload is not automatic and backend deployment remains separate later work, and external tester handoff remains blocked until server-connected CMS/Admin verification, manual update check verification, clean-machine install, and the controlled tester checklist pass.


## Password recovery and signed-in password change status (2026-06-08)

- Password recovery/reset is implemented on the existing backend auth foundation. The existing `password_reset_tokens` table and `20260601120000_AddPasswordResetFoundation` migration are reused; no new EF Core migration was created.
- Backend routes now support generic password reset requests, one-time reset confirmation with token expiration/revocation, and authenticated signed-in password changes. The desktop Account settings UI exposes a tester-friendly Forgot password flow and a signed-in Change password flow.
- SMTP delivery is environment-driven. Production SMTP credentials must be configured on the server through `/etc/languagevoicetutor/backend.env`; credentials, tokens, private keys, and provider secrets must not be committed. Expected sender identity is `support@languagevoicetutor.com`.
- External tester handoff is still blocked until CMS/Admin server verification, manual update check verification and clean-machine smoke, and checklist completion are done.

## Account recovery/change polish and backend deploy hardening (2026-06-08)

- Account password recovery/change is being polished for tester readiness: the Account screen keeps account/subscription status and sign-in fields visible, while Forgot password and Change password forms are collapsed by default and clear sensitive fields on close/success.
- Desktop validation now keeps normal 400/401 auth validation failures user-facing instead of presenting them as server outages. Wrong login credentials map to `Email or password is incorrect.`, wrong current password maps to `Current password is incorrect.`, short new passwords map to `Password must be at least 8 characters.`, and invalid reset codes map to `Password reset code is invalid or expired.`
- SMTP credentials remain server-only in `/etc/languagevoicetutor/backend.env`; no SMTP password, reset token, or other secret is committed. The backend now accepts the production-style `SmtpEmail__Username` and `SmtpEmail__UseSsl` environment names as aliases for the SMTP sender settings.
- Backend upload hardening now packages Linux archive entries with forward slashes and verifies `/opt/languagevoicetutor/backend/releases/<version>/EnglishVoiceTutor.Api` exists and is executable before reporting success.
- External tester handoff is still blocked until CMS/Admin server verification, manual update check verification and clean-machine smoke, and checklist completion.

## Static tester download page foundation status

A basic public download page foundation is now prepared under `site/public/`. The page is static, uses plain HTML/CSS/JS, and reads `latest.json` from the existing Windows direct release folder at `/releases/windows/direct/latest.json`. It uses `installerRelativeUrl` from the manifest for the primary Windows installer link and shows version, channel, installer size, and SHA-256 when the manifest loads.

The static page does not implement auto-update, does not replace the in-app manual update UI, and does not change backend API logic or lesson behavior. It is only a tester download page for invited testers. External tester handoff is still blocked until the manual update check verification and the clean-machine smoke checklist pass.

## CMS/Admin static-json-v1 initialization status

CMS/Admin login works for the configured bootstrap admin account, and protected Admin CMS APIs remain behind authentication/authorization. The current production-safe default is still static JSON runtime: `CmsContent__UsePublishedSnapshotForRuntime=false`, `CmsContent__ContentPackSlug=static-json-v1`, and `CmsContent__FallbackToStaticJson=true`.

On first production setup, `static-json-v1` may need the admin-only **Initialize from static JSON** action before the CMS Content overview can show topics, scenarios, prompt templates, tutor profiles, and available study-language metadata references. This initialization creates or prepares CMS draft/admin content only, preserves any existing draft content, does not publish automatically, and does not switch runtime. Runtime remains `StaticJson` until `CmsContent__UsePublishedSnapshotForRuntime=true` is intentionally enabled after verification.

Public release / external tester handoff remains blocked until CMS initialization/verification and update/version-check work are complete.


## Windows installer installed-version check foundation

Installed-version checking is now part of the Windows installer foundation. The Windows Inno Setup installer keeps the existing installer identity and checks the installed Language Voice Tutor version before continuing.

- Same-version install asks for reinstall confirmation instead of silently reinstalling.
- Older installed version is treated as an update with a clear installer message.
- Newer installed version warns and blocks by default to avoid accidental downgrades.
- The installer uses Inno Setup close-application handling for the desktop executable so it does not silently replace files while the app is running.

This is not the future in-app update UI. The desktop app still does not check `latest.json`, verify SHA-256, or guide users through an in-app download/install flow. Future update UI still needs to check `latest.json`, verify SHA-256, avoid updates during active lessons, and guide the user through download/install. Active-lesson detection remains a future in-app update UI responsibility because the installer cannot reliably inspect lesson state.

External tester handoff is still blocked until update/version-check verification and clean-machine smoke pass.


## Desktop manual update check and local progress/history

Settings now includes a simple user-facing **Check for updates** action near the top of the normal Settings experience; update checking is not diagnostics-only. The action reads the Windows direct-release `latest.json`, validates the Language Voice Tutor product identity, app id, Windows platform, and x64 architecture, compares the installed version with the manifest version, and uses manual confirmations before any download or installer launch. The desktop downloads only after the user agrees, verifies the installer SHA-256 before offering to start it, starts the installer through the normal shell when the user agrees, and does not silently auto-update.

Progress and Lesson History remain local, account-scoped learner data. Existing completed lessons for the signed-in backend account are preserved and should remain visible across app restart, Windows restart, reinstall, and update. Signed-in users do not see ownerless legacy records or records from another account. Clean-machine smoke remains required before external tester handoff, including sign-in restore plus Progress/Lesson History verification.
