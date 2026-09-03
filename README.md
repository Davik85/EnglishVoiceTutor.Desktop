# Language Voice Tutor Desktop

## 2026-09-03 Production closeout

Android `0.1.0+8` / Google Play versionCode `8` is publicly available as **Orralen - Language Voice Tutor**. The existing v8 artifact was selected and promoted as the Production candidate after owner approval and review submission; this does not claim a new AAB upload. Package `com.languagevoicetutor.mobile` and the existing Language Voice Tutor account, entitlement, and billing identities continue unchanged. Production backend is `0.1.35-backend.151` with `.150` rollback; health and database health returned HTTP 200, while Google Play Billing, RTDN, and reconciliation remain enabled.

The public root homepage is independent static `index.html`, `mobile.html` is a `noindex,follow` redirect to `/`, and `styles.css` is also independent. Website CMS owns download, pricing, support, legal, status, crawler, and consent resources only; it must not overwrite the independent files. See `docs/CURRENT_STATE.md` and `docs/COMMAND_PLAYBOOK.md` for the verified publication record and safe deployment order.

Language Voice Tutor Desktop is a WPF desktop product for guided English speaking practice. The current product lesson flow is stabilized for local Windows testing: learners choose a level, topic, subtopic, and scenario, then practice in Lesson Chat by typing or recording speech.

## Built with Codex and GPT-5.6

Language Voice Tutor Desktop was developed with a human-led, AI-assisted engineering workflow. Codex was used as an engineering agent to inspect the existing repository, implement scoped changes, refactor code, add and update automated tests, update documentation, and run repository verification commands. GPT-5.6 was used for technical planning, architecture review, debugging guidance, product and UX reasoning, task decomposition, review of Codex results, and preparation of precise implementation instructions.

Human developers remained responsible for product decisions, approving scope, reviewing changes, running or reviewing verification, testing the Windows application, handling production deployment, and deciding what was committed and released. AI tools did not autonomously publish production releases, handle secrets, approve payments, or make final product decisions.

The application runtime remains separate from the development workflow: all OpenAI API calls made by the product go through the backend, and API keys are not stored in the Windows client. The project workflow combines AI-assisted implementation with automated tests, builds, policy checks, manual smoke testing, and human review.

For a detailed description of the AI-assisted development workflow, see [docs/CODEX_AND_GPT_5_6_DEVELOPMENT.md](docs/CODEX_AND_GPT_5_6_DEVELOPMENT.md).

## Windows client functionality

For the current Windows desktop client feature source of truth, customer-facing functional overview, and future mobile planning reference, see [`docs/WINDOWS_CLIENT_FUNCTIONALITY_OVERVIEW.md`](docs/WINDOWS_CLIENT_FUNCTIONALITY_OVERVIEW.md).

## Run the desktop app

1. Restore dependencies:
   `dotnet restore`
2. Build the desktop app:
   `dotnet build`
3. Run from your IDE or use your preferred `dotnet` run/publish workflow.

Local development builds default the desktop Backend URL to `http://localhost:5000`. Installed release builds are server-only and must use `https://api.languagevoicetutor.com`; localhost/backend switching is DEBUG/developer-only and is not normal release behavior.

## Run the local backend proxy

1. Stop any old backend `dotnet` process or close old backend terminal windows before starting a fresh backend.
2. Go to the backend project folder:
   `cd backend/EnglishVoiceTutor.Api`
3. Restore dependencies:
   `dotnet restore`
4. Build the backend:
   `dotnet build`
5. Start the API:
   `dotnet run`

## Lesson chat endpoints

- The desktop app uses `POST /api/lesson-chat/reply` for normal lesson replies and for the default Conversation Mode reply flow.
- `POST /api/lesson-chat/mock-reply` stays available for local compatibility and testing.
- Normal audio transcription uses `POST /api/audio/transcribe`.
- Speech playback uses `POST /api/audio/speech`.
- Realtime code remains in the repository for future testing, but it is not the default product Conversation Mode path.

## Backend OpenAI configuration

Run backend with OpenAI enabled (PowerShell):

```powershell
Set-Item -Path Env:OPENAI_API_KEY -Value (Read-Host "Enter your local OpenAI API key")
dotnet run
```

- If `OPENAI_API_KEY` is missing, the real lesson chat endpoint returns an error instead of mock lesson text.
- If an OpenAI call fails or returns invalid output, the real lesson chat endpoint returns an error instead of mock lesson text.
- Desktop app still calls only the real backend lesson chat endpoint during normal lesson flow.

## Backend availability testing note

The desktop app is backend-driven. Backend-unavailable checks are resilience-only: the app should not crash, Settings/Account should remain usable, and backend-required lesson or AI actions should show a friendly localized message instead of a raw exception. Full lesson functionality, including Send, Hint, Translate, Play voice/TTS, Finish, Summary, and Account Login/Logout, must be tested with the backend running.


## Source of truth for current versions

These docs are a snapshot of the last known verified state. They can become stale and must not be used as the only source of truth for live versions. Always verify the live/public state before telling any user or stakeholder that a version is current.

Check the public Windows direct release from the live website manifest:

```powershell
Invoke-RestMethod https://languagevoicetutor.com/releases/windows/direct/latest.json
```

If a PowerShell path reads raw manifest text and `ConvertFrom-Json` fails because a UTF-8 BOM is present at the start of `latest.json`, strip the BOM before parsing:

```powershell
($raw -replace "^\uFEFF", "") | ConvertFrom-Json
```

Check the production backend release from the server `current` symlink:

```powershell
ssh lvt-server "readlink -f /opt/languagevoicetutor/backend/current"
```

Check production backend health and database health:

```powershell
Invoke-WebRequest https://api.languagevoicetutor.com/health -UseBasicParsing
Invoke-WebRequest https://api.languagevoicetutor.com/api/health/database -UseBasicParsing
```

Generated local files under `artifacts/` are not proof that a version is live on the public site. A locally built installer becomes public only after the Windows direct release files are uploaded to the website release folder and `latest.json` is verified over HTTPS.

## Windows direct-download release

Inno Setup is the primary Windows direct-download installer flow. Build it with:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\package-windows-inno-release.ps1 -Version 1.6
```

Last verified public Windows direct manifest baseline: the live website `latest.json` points to channel `direct-public`, `LanguageVoiceTutorSetup-1.6.exe`, `version` and `minimumSupportedVersion` set to `1.6`, `backendBaseUrl` set to `https://api.languagevoicetutor.com`, and `updateMode` set to `manual-confirmation`. The published manifest records installer SHA-256 `9eaac1ffa1ead6c3590f2cf072ff6dcabb7edba912c38a6cd1d6875ad5ac1aa3` and size `188959874` bytes; no independent second public-download SHA verification is claimed for 1.6. The public manifest remains the source of truth and must be verified over HTTPS before release handoff. The release package script locks packaged release builds to that backend; custom/local backend URLs are DEBUG/developer-only and must not be used for installed release builds. The primary installer is written to `artifacts\installers\windows\LanguageVoiceTutorSetup-{version}.exe`. Server-ready direct-download files are generated under `artifacts\releases\windows\direct`, including `latest.json`, `changelog.json`, `known-issues.json`, and `checksums.sha256`. Generated `artifacts\` files and installer `.exe` files must not be committed. Code signing is still deferred.

Validate the generated direct-release folder with `powershell -ExecutionPolicy Bypass -File .\scripts\validate-windows-direct-release.ps1`. Static release upload is documented in [`docs/WINDOWS_RELEASE_SERVER_UPLOAD.md`](docs/WINDOWS_RELEASE_SERVER_UPLOAD.md); it publishes release files only and does not deploy the backend or run migrations. The desktop release Settings UI includes a single user-facing **Check for updates** button that checks `latest.json`, validates manifest identity, asks before download/install, verifies SHA-256 before launching the installer, and never silently auto-updates.

The Settings screen footer displays the installed app version, for example `Version: v1.6`. Users should include that version when reporting bugs.

## Backend Linux deployment foundation

A safe Ubuntu 24.04 backend deployment foundation is documented in [`docs/BACKEND_SERVER_DEPLOYMENT.md`](docs/BACKEND_SERVER_DEPLOYMENT.md). It packages the backend as a self-contained `linux-x64` archive from the local Windows development machine, uploads it to a versioned server release folder under `/opt/languagevoicetutor/backend`, and documents a systemd service plus nginx reverse proxy for `api.languagevoicetutor.com`. Current production backend snapshot: `.151` is deployed and `.150` is the rollback release; backend/database health passed and Google Play Billing, RTDN, and reconciliation remain enabled. `.151` required no EF migration. Historical `.141`/`.140` release details remain historical only. The deployed backend API is available at `https://api.languagevoicetutor.com`, and Android v8 is publicly available in Google Play Production. AI Models persistent production storage remains `/opt/languagevoicetutor/backend/site/content/ai-model-settings.json`; release-folder AI Models JSON is not the production source of truth.

Generated backend publish/package files are written under `artifacts/` and must not be committed. Real server configuration must live outside git, for example `/etc/languagevoicetutor/backend.env`, and must never include committed database passwords, OpenAI keys, provider keys, JWT signing keys, SSH keys, tokens, or other secrets. Broad paid launch remains pending final readiness/legal/support/ops review.

## Security rule

OpenAI API keys are backend-only. `OPENAI_API_KEY` is needed only for real AI/TTS/STT testing, must never be stored in the desktop app, must never be committed to source control, and must never be sent to users. The desktop app must call backend APIs only; installed release builds use the fixed production backend and do not expose Backend URL editing to normal users.

## Current product voice decision

Conversation Mode uses the stable TTS provider by default:

`microphone recording -> audio transcription -> lesson chat reply -> gpt-4o-mini-tts playback`

Realtime remains in the codebase for future testing, but it is not the default product path. The learner must hear exactly the same text that is displayed, so Conversation Mode does not shorten, summarize, rewrite, or chunk spoken text.

## Current product status

The current product baseline is documentation-first and behavior-stable:

- Lesson content audit passes with 26 lesson JSON files.
- Desktop builds successfully in Debug and Release on Windows.
- Backend builds successfully on Windows.
- Normal Lesson Chat works with typed input, Enter-to-send, Send button, normal voice recording, transcription, bot replies, Play voice, Translate, Hint, View feedback, and lesson summary.
- Feedback uses the global bottom feedback panel and is bound to the clicked message through `sourceMessageId` / `sourceMessageKind`.
- Context-selection feedback is phrase-level and does not treat the phrase as an active roleplay answer.
- Hint works in normal Lesson Chat and Conversation Mode, including the semi-transparent Conversation Mode overlay.
- Conversation Mode works with the TTS provider: full avatar overlay, red record button, exit/back button, latest user and bot phrase bubbles, recording, transcription, bot reply generation, voice playback, and multiple turns.
- Normal Lesson Chat TTS remains `tts-1` with `purpose=lesson_chat_tts`.
- Conversation Mode TTS uses `gpt-4o-mini-tts`, the selected tutor voice (`coral` by default, `onyx` for David), `purpose=conversation_mode_tts`, speed `1.0`, and calm speech instructions.
- Usage/cost logging exists, but exact pricing fields are still approximate or missing where pricing constants are not configured.
- UI has the Soft Learning Desktop style: light blue frame, rounded cards/buttons/inputs, level colors, topic colors, and warm hint/feedback cards.
- Step 5B-2 adds a centralized native/interface/explanation language foundation for global language preferences, with English UI fallback for languages that do not have localized UI text yet. Study languages were not expanded and remain English, French, German, Portuguese, Spanish, and Italian.
- Step 5B-3 adds interface localization v1 for the supported interface language catalog. English fallback remains the default safety behavior for unknown languages or any missing UI text, and study languages were not expanded.
- Step 5B-3b limits the Interface language selector to release-ready UI localizations that passed the desktop coverage audit. Native/explanation languages remain the broad Step 5B-2 catalog, study languages were not expanded, and new Interface languages should be added only after UI localization QA passes.
- Step 5B-3c completes missing core UI localization for the release-ready Interface languages. English fallback remains a runtime safety mechanism, Native/Explanation languages remain broad, and study languages were not expanded.

Current release-readiness status: the public direct Windows manifest must be verified from live `latest.json`; the current verified public direct build is `1.6` on channel `direct-public` with installer `LanguageVoiceTutorSetup-1.6.exe` and production backend `0.1.35-backend.151` (`.150` rollback). Windows 1.6 removes obsolete Desktop Free-limit behavior and presents technical HTTP 429 throttling as a localized temporary wait-and-retry condition; it does not change the one-free-lesson-per-day product policy. The successful 1.5 -> 1.6 manual-confirmation update is verified. The Language Voice Tutor product name, stable AppId, accounts, update continuity, and existing ORRALEN icon/shortcut behavior remain unchanged. Broader Desktop and Mobile product-facing rebranding remains future audited work. Code signing / SmartScreen mitigation, monitoring, customer feedback, and broader quality work remain follow-up items. See `docs/CURRENT_STATE.md`, `docs/NEXT_STEPS.md`, and `docs/TESTER_RELEASE.md`.

Detailed review docs live in `docs/`:

- `docs/CURRENT_STATE.md`
- `docs/ARCHITECTURE_REVIEW.md`
- `docs/VOICE_AND_REALTIME_REVIEW.md`
- `docs/LESSON_FLOW_REVIEW.md`
- `docs/COST_MODEL.md`
- `docs/MANUAL_TEST_CHECKLIST.md`
- `docs/NEXT_STEPS.md`
- `docs/PRE_MOBILE_READINESS.md`
- `docs/subscription-billing-foundation.md`

Development admin smoke test (requires a running Development backend at `http://localhost:5000`):

```powershell
powershell -ExecutionPolicy Bypass -File tools\smoke_admin_foundation.ps1
```

Billing checkout smoke tests require a running backend:

- Default billing smoke:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools\smoke_billing_checkout.ps1
```

- Paddle adapter smoke (start backend with safe disabled Paddle env overrides first; no real Paddle credentials are required):

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools\smoke_paddle_checkout_adapter.ps1
```

- Paddle client-side token guard smoke (start backend with fake API/price values and `PaddleBilling__ClientSideToken` empty; it should stop before any Paddle API call):

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools\smoke_paddle_checkout_client_token_guard.ps1
```

- Optional real Paddle sandbox checkout smoke (start backend first with Paddle environment variables or user secrets, including `PaddleBilling__ClientSideToken`; do not put real API keys in tracked files):

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools\smoke_paddle_checkout_live_sandbox.ps1 -AllowRealPaddleCall
```

This optional smoke creates a real Paddle sandbox transaction and prints the backend-hosted checkout launch URL, but it does not complete payment, call webhooks, or activate internal entitlement state.

Paddle lifecycle smoke tests verify signed ingestion, normalization, subscription snapshots, payment snapshots, entitlement activation/extension, scheduled-cancellation and past-due policy, actual canceled/paused expiry policy, duplicate idempotency, unsigned/invalid-signature rejection, and backend access/status recognition of `provider_event` Premium entitlement. Start the backend with local placeholder webhook settings only; do not use real secrets in tracked files:

```powershell
$env:PaddleWebhook__Enabled = "true"
$env:PaddleWebhook__SecretKey = "test_webhook_secret"
$env:PaddleWebhook__TimestampToleranceSeconds = "300"
powershell -NoProfile -ExecutionPolicy Bypass -File tools\smoke_paddle_webhook_ingestion.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File tools\smoke_paddle_subscription_lifecycle.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File tools\smoke_paddle_payment_persistence.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File tools\smoke_paddle_entitlement_extension.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File tools\smoke_paddle_cancellation_past_due_policy.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File tools\smoke_paddle_canceled_paused_expiry_policy.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File tools\smoke_paddle_resumed_activated_snapshot_policy.ps1
```

Detailed billing architecture, provider-agnostic access boundaries, and deferred scope are documented in `docs/subscription-billing-foundation.md`.

Common validation commands from the repository root:

```powershell
powershell -ExecutionPolicy Bypass -File tools\audit_lesson_content.ps1
dotnet restore
dotnet build
dotnet build -c Release
cd backend\EnglishVoiceTutor.Api
dotnet restore
dotnet build
```

Recommended next work: pre-mobile planning should start from [`docs/PRE_MOBILE_READINESS.md`](docs/PRE_MOBILE_READINESS.md), then continue with final readiness/legal/support/ops review before any broader paid launch decision. CMS/Admin is connected, CMS published-snapshot runtime is active for published Windows direct lessons, and Save draft + Publish is required before newly started desktop lessons see CMS changes. Static JSON fallback remains available for rollback. The deployed backend API is available at `https://api.languagevoicetutor.com`. Customer portal work, remaining billing operations, critical-change approval workflow, Microsoft Store/MSIX (discontinued for now), App Store, Google Play, Mac version, broader production-readiness follow-ups, and full production CMS/Admin operational readiness remain deferred. Prompt/scenario/bot-behavior polishing stays in CMS/Admin rather than code edits.


## Local admin shell and Admin CMS Content

For non-developer prompt and tutor behavior editing, start with [`docs/CMS_PROMPT_MANAGEMENT_ADMIN_GUIDE.md`](docs/CMS_PROMPT_MANAGEMENT_ADMIN_GUIDE.md) and the field-level [`docs/CMS_BEHAVIOR_TUNING_PLAYBOOK.md`](docs/CMS_BEHAVIOR_TUNING_PLAYBOOK.md). Normal prompt/scenario/tutor behavior tuning belongs in CMS draft + publish workflows, not backend code or static JSON edits.

- Local admin shell: http://localhost:5000/admin/
- Requires running backend and a configured Development bootstrap admin.
- The local admin shell supports capabilities view, read-only user lookup, read-only per-user audit log, manual Premium grant/revoke, free lesson allowance reset for selected users, and the development/admin-only `CMS Content` workspace with a read-only Recent CMS changes audit surface.
- The local admin shell is organized into main tabs: Overview, User Lookup, Premium, Free Lesson, Audit Log, CMS Content, and System. User lookup also shows a Premium entitlement schedule (current + future active Premium grants) in addition to currently active entitlements.
- The `CMS Content` workspace exists under `/admin/` and contains sub-tabs for Overview, Topics, Scenarios, Prompts, Levels, Tutors, Validation & Preview, Versions & Publish, and Audit. It supports content pack overview, topic editing, scenario editing, full scenario JSON editing, prompt template editing, tutor behavior profile editing, validation/preview summary, and versions/publish/restore flows. Topics, scenarios, prompt templates, and tutor behavior profiles can be selected by table row click or compact Select buttons.
- Scenario editing includes bounded fields, a structured form-based scenario editor for common content text, compact local **Jump to** navigation, collapsible/visually separated sections for Basic fields, Lesson setup, Context selection / choices, Conversation flow / response guidance, Wrap-up / summary guidance, and Advanced JSON, plus helper text for normal content editors. Structured fields remain the recommended normal editing path. Advanced JSON is still available as a visually separated technical fallback for rare full-JSON edits. `Format JSON` only pretty-prints/re-indents the JSON in the editor for readability. `Validate JSON` checks JSON syntax and required scenario fields before saving. Neither action saves, publishes, or persists changes; `Save draft` is still required to persist CMS edits.
- `Save draft` persists draft rows and audit entries only; it does not publish and does not change runtime-visible content. After a successful draft save, the Admin CMS shows “Draft saved. To apply this content to runtime, publish the current draft.” with a **Go to Publish** action that opens the existing **Versions & Publish** subtab while preserving the selected content pack, selected CMS entity keys, selected user hash state, and the URL hash. The **Versions & Publish** subtab explains that draft changes are not runtime-visible until publish, labels the publish change summary as required when publishing changed content, blocks likely changed publishes with a blank summary in the browser, and renders backend publish validation errors/warnings in a readable list. The only actual publish path remains the confirmed **Publish current draft** flow in **Versions & Publish**. Published versions are immutable; restore copies an old published version into a new published version rather than changing prior version history.
- Admin refresh no longer logs out the admin. Admin authentication survives refresh through the existing admin-only HTTP-only cookie, while the admin JWT remains memory-only in JavaScript. Browser Web Storage is not used: no `sessionStorage`, no `localStorage`, and no IndexedDB.
- The admin workspace restores only safe identifiers from the URL hash after a valid admin session is verified: `adminTab`, `cmsSubTab`, `selectedUserId`, `contentPackSlug`, `topicKey`, `scenarioKey`, `promptTemplateKey`, and `tutorId`. Selected user details are restored through an admin-only user lookup by `selectedUserId`; selected CMS entities are restored by stable keys. Passwords, tokens, prompts, full scenario JSON, tutor profile JSON, and unsaved draft field values are not stored in the hash or browser storage.
- CMS dirty state is tracked in memory by comparing current form values against the last loaded/saved baseline. Unsaved CMS changes show a visible indicator and warn before browser refresh, tab close, top-level admin tab switching, CMS sub-tab switching, selecting another CMS entity, publish/restore reload flows, or logout would discard edits. `Save draft` clears the dirty indicator after a successful save; failed saves keep it. Unsaved content is never persisted in browser storage or the URL hash.
- Runtime learner behavior now uses the CMS published snapshot for published Windows direct lessons. Historical CMS-first/runtime behavior was verified on backend `0.1.35-backend.48`, and the historical Windows direct `1.1` handoff was published and verified. Save draft remains draft-only; Save draft + Publish is required before newly started desktop lessons see CMS changes. The clarified Overview separates CMS content pack/seed identity from actual learner runtime source: `static-json-v1` / `Static JSON Baseline` is the CMS pack identity, while the decisive healthy fields are `Actual learner runtime source = CmsPublishedSnapshot`, `Validation success = Yes`, `Currently using static JSON fallback = No`, and a published version exists. Static JSON fallback remains available for initialization/emergency rollback only.
- This remains development/admin-only and is not production CMS readiness. Production Admin RBAC controlled cutover, role-based content approval, production billing operations, and broad paid launch remain future work. CMS draft-save audit logging is implemented for topic, scenario (including structured scenario fields and full scenario JSON), prompt template, and tutor behavior profile Save draft operations; critical-change approval should wait until production roles exist.


CMS draft-save audit logging records successful Admin CMS Save draft operations in `cms_content_audit_logs`. Entries capture audit id, UTC timestamp, actor user id, actor email when available, content pack id/slug, entity type (`Topic`, `Scenario`, `PromptTemplate`, `TutorBehaviorProfile`), entity id, stable key, operation `DraftSaved`, changed field names, before/after SHA-256 hashes, reason, request id when available, source `AdminCms`, and status. The Admin CMS Audit subtab now exposes recent changes as read-only rows with filters for entity type, stable key text, and limit, aligned to the selected content pack (`static-json-v1` by default). Smoke/test entries are hidden by default, a **Show smoke/test entries** checkbox is available for debugging, and normal manual Admin CMS UI changes remain visible. Full before/after JSON snapshots and edited prompt/tutor/scenario bodies are intentionally not stored or displayed in audit rows; large values are represented by hashes. Secrets, passwords, tokens, provider secrets, OpenAI API keys, Paddle keys, webhook secrets, and admin bearer tokens are not logged. Runtime learner behavior now uses the CMS published snapshot for published Windows direct lessons, and static JSON fallback remains available for rollback.

- Static admin shell audit script: `powershell -ExecutionPolicy Bypass -File tools\audit_admin_shell.ps1`.
- The existing smoke script (`tools/smoke_admin_foundation.ps1`) runs this admin shell audit before backend HTTP smoke checks.
- Latest confirmed EF migration is `20260604121000_AddCmsDraftSaveAuditMetadata`.

## Interface localization

Step 5B-3d completed a full learner-facing desktop UI localization pass for the release-ready Interface languages (`en`, `es`, `fr`, `de`, `it`, `pt`, `ru`, `pl`, `ar`, `ja`, `ko`, `sr`, `hr`, `bg`). Runtime English fallback remains a safety mechanism for unexpected missing text, not the expected path for release-ready interface languages. Native/Explanation languages remain broad, and Study languages were not expanded.

Step 5B-3e completed Subtopics/Situations display localization for those release-ready Interface languages. Lesson JSON remains unchanged; runtime English fallback remains a safety mechanism only, Native/Explanation languages remain broad, and Study languages were not expanded.

Step 5B-4 added a desktop release smoke gate in `docs/desktop-release-smoke-gate.md` and the safe local helper `tools/run_desktop_release_gate.ps1`. The reusable Windows direct-download installer flow is Inno Setup: set `$ReleaseVersion = "<release-version>"`, then run `powershell -ExecutionPolicy Bypass -File .\scripts\package-windows-inno-release.ps1 -Version $ReleaseVersion`; local artifacts are not public/live until upload and HTTPS manifest verification. Windows Direct Release 1.6 is public: live manifest values and the 1.5 -> 1.6 manual-confirmation update were verified. The app does not silently auto-update. Current production backend is `0.1.35-backend.141` with `.140` rollback; neither release required a migration or schema change. Code signing / SmartScreen mitigation, monitoring, customer feedback, and broader production-readiness follow-ups remain.

Structured scenario editor update: the Admin CMS Scenarios subtab now includes a safer structured editor for common scenario content (title/subtopic, description, setup message, first bot message guidance, context option titles, valid context keywords, custom context rules, invalid context redirect, goal text, can-do statements, opening/first-user-task/follow-up guidance, AI tutor instructions, wrap-up/final message guidance, hint example, and level-profile-owned lesson length guidance (scenario turn counters are not normal controls)). `DefinitionJson` remains the canonical stored scenario definition; no per-field scenario database columns or EF migration were added. Structured edits parse the current `DefinitionJson`, update only known JSON paths, and write the merged valid JSON back to `DefinitionJson`, preserving unknown fields and advanced configuration in place. Advanced JSON remains available with `Format JSON` and `Validate JSON` for rare technical changes. Save draft remains explicit; invalid Advanced JSON or invalid structured numeric/required data is rejected before saving, and backend scenario validation still rejects invalid JSON, missing required fields, or accidental stable id/title/setup mismatches. CMS draft-save audit logging still records successful scenario saves with changed field names and before/after hashes without storing full scenario JSON bodies. Runtime learner behavior now uses the CMS published snapshot for published Windows direct lessons; Save draft + Publish is required for app-visible changes, and static JSON fallback remains available.


### Controlled CMS runtime lesson-content read path

Runtime lesson content now uses the CMS published snapshot for published Windows direct lessons. Runtime status should remain `effectiveSource=CmsPublishedSnapshot`, `validationSuccess=true`, `fallbackUsed=false`, with no errors and no warnings. Save draft alone does not affect the app; Save draft + Publish is required before newly started desktop lessons see CMS changes. The backend now has a development/admin-safe runtime content diagnostic at `/api/admin/dev/cms/runtime-content/status` that reports whether runtime content was loaded from packaged static JSON or from the currently published CMS snapshot. CMS runtime reads require explicit configuration: `CmsContent:UsePublishedSnapshotForRuntime=true` and `CmsContent:ReadPublishedSnapshotEnabled=true`; `CmsContent:ContentPackSlug` defaults to `static-json-v1`, and `CmsContent:FallbackToStaticJson` defaults to `true`. Runtime CMS mode reads only immutable published snapshots and never draft rows. If the selected published snapshot is missing or invalid, static JSON fallback is used only when `CmsContent:FallbackToStaticJson=true`; otherwise the diagnostic returns a clear server-side unavailable result. The diagnostic reports source, slug, published version, snapshot hash, counts, fallback state, validation state, errors, and warnings without logging full prompt, scenario, or tutor bodies. Static JSON fallback remains available for rollback; production Admin RBAC controlled cutover and approval workflow remain future work.
