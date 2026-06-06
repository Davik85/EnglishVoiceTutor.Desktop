# EnglishVoiceTutor.Desktop

EnglishVoiceTutor.Desktop is a WPF desktop MVP for guided English speaking practice. The current MVP lesson flow is stabilized for local Windows testing: learners choose a level, topic, subtopic, and scenario, then practice in Lesson Chat by typing or recording speech.

## Run the desktop app

1. Restore dependencies:
   `dotnet restore`
2. Build the desktop app:
   `dotnet build`
3. Run from your IDE or use your preferred `dotnet` run/publish workflow.

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
- Realtime code remains in the repository for future testing, but it is not the default MVP Conversation Mode path.

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

## Security rule

OpenAI API keys are backend-only. `OPENAI_API_KEY` is needed only for real AI/TTS/STT testing, must never be stored in the desktop app, must never be committed to source control, and must never be sent to testers. The desktop app only needs a Backend URL and must call backend APIs only.

## Current MVP voice decision

Conversation Mode uses the stable TTS provider by default:

`microphone recording -> audio transcription -> lesson chat reply -> gpt-4o-mini-tts playback`

Realtime remains in the codebase for future testing, but it is not the default MVP path. The learner must hear exactly the same text that is displayed, so Conversation Mode does not shorten, summarize, rewrite, or chunk spoken text.

## Current MVP status

The current MVP baseline is documentation-first and behavior-stable:

- Lesson content audit passes with 26 lesson JSON files.
- Desktop builds successfully in Debug and Release on Windows.
- Backend builds successfully on Windows.
- Normal Lesson Chat works with typed input, Enter-to-send, Send button, normal voice recording, transcription, bot replies, Play voice, Translate, Hint, View feedback, and lesson summary.
- Feedback uses the global bottom feedback panel and is bound to the clicked message through `sourceMessageId` / `sourceMessageKind`.
- Context-selection feedback is phrase-level and does not treat the phrase as an active roleplay answer.
- Hint works in normal Lesson Chat and Conversation Mode, including the semi-transparent Conversation Mode overlay.
- Conversation Mode works with the TTS provider: full avatar overlay, red record button, exit/back button, latest user and bot phrase bubbles, recording, transcription, bot reply generation, voice playback, and multiple turns.
- Normal Lesson Chat TTS remains `tts-1` with `purpose=lesson_chat_tts`.
- Conversation Mode TTS uses `gpt-4o-mini-tts`, voice `coral`, `purpose=conversation_mode_tts`, speed `1.0`, and calm speech instructions.
- Usage/cost logging exists, but exact pricing fields are still approximate or missing where pricing constants are not configured.
- UI has the Soft Learning Desktop style: light blue frame, rounded cards/buttons/inputs, level colors, topic colors, and warm hint/feedback cards.
- Step 5B-2 adds a centralized native/interface/explanation language foundation for global language preferences, with English UI fallback for languages that do not have localized UI text yet. Study languages were not expanded and remain English, French, German, Portuguese, Spanish, and Italian.
- Step 5B-3 adds interface localization v1 for the supported interface language catalog. English fallback remains the default safety behavior for unknown languages or any missing UI text, and study languages were not expanded.
- Step 5B-3b limits the Interface language selector to release-ready UI localizations that passed the desktop coverage audit. Native/explanation languages remain the broad Step 5B-2 catalog, study languages were not expanded, and new Interface languages should be added only after UI localization QA passes.
- Step 5B-3c completes missing core UI localization for the release-ready Interface languages. English fallback remains a runtime safety mechanism, Native/Explanation languages remain broad, and study languages were not expanded.

Detailed review docs live in `docs/`:

- `docs/CURRENT_STATE.md`
- `docs/ARCHITECTURE_REVIEW.md`
- `docs/VOICE_AND_REALTIME_REVIEW.md`
- `docs/LESSON_FLOW_REVIEW.md`
- `docs/COST_MODEL.md`
- `docs/MANUAL_TEST_CHECKLIST.md`
- `docs/NEXT_STEPS.md`
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

Recommended next work: continue the CMS/Admin content MVP before external tester handoff. CMS draft-save audit logging is implemented for Admin CMS Save draft operations; production billing readiness, remaining billing operations, production RBAC, critical-change approval workflow, and full production CMS/Admin operational readiness remain deferred. Prompt/scenario/bot-behavior polishing stays in CMS/Admin rather than code edits.


## Local admin shell and Admin CMS Content

- Local admin shell: http://localhost:5000/admin/
- Requires running backend and a configured Development bootstrap admin.
- The local admin shell supports capabilities view, read-only user lookup, read-only per-user audit log, manual Premium grant/revoke, free lesson allowance reset for selected users, and the development/admin-only `CMS Content` workspace with a read-only Recent CMS changes audit surface.
- The local admin shell is organized into main tabs: Overview, User Lookup, Premium, Free Lesson, Audit Log, CMS Content, and System. User lookup also shows a Premium entitlement schedule (current + future active Premium grants) in addition to currently active entitlements.
- The `CMS Content` workspace exists under `/admin/` and contains sub-tabs for Overview, Topics, Scenarios, Prompts, Tutors, Validation & Preview, Versions & Publish, and Audit. It supports content pack overview, topic editing, scenario editing, full scenario JSON editing, prompt template editing, tutor behavior profile editing, validation/preview summary, and versions/publish/restore flows. Topics, scenarios, prompt templates, and tutor behavior profiles can be selected by table row click or compact Select buttons.
- Scenario editing includes bounded fields, a structured form-based scenario editor for common content text, and an advanced Full scenario JSON editor. `Format JSON` only pretty-prints/re-indents the JSON in the editor for readability. `Validate JSON` checks JSON syntax and required scenario fields before saving. Neither action saves, publishes, or persists changes; `Save draft` is still required to persist CMS edits.
- `Save draft` persists draft rows and audit entries only; it does not publish and does not change runtime-visible content. After a successful draft save, the Admin CMS now shows “Draft saved. To apply this content to runtime, publish the current draft.” with a **Go to Publish** action that opens the existing **Versions & Publish** subtab while preserving the selected content pack, selected CMS entity keys, selected user hash state, and the URL hash. The **Versions & Publish** subtab now explains that draft changes are not runtime-visible until publish, labels the publish change summary as required when publishing changed content, blocks likely changed publishes with a blank summary in the browser, and renders backend publish validation errors/warnings in a readable list. The only actual publish path remains the confirmed **Publish current draft** flow in **Versions & Publish**.
- Admin refresh no longer logs out the admin. Admin authentication survives refresh through the existing admin-only HTTP-only cookie, while the admin JWT remains memory-only in JavaScript. Browser Web Storage is not used: no `sessionStorage`, no `localStorage`, and no IndexedDB.
- The admin workspace restores only safe identifiers from the URL hash after a valid admin session is verified: `adminTab`, `cmsSubTab`, `selectedUserId`, `contentPackSlug`, `topicKey`, `scenarioKey`, `promptTemplateKey`, and `tutorId`. Selected user details are restored through an admin-only user lookup by `selectedUserId`; selected CMS entities are restored by stable keys. Passwords, tokens, prompts, full scenario JSON, tutor profile JSON, and unsaved draft field values are not stored in the hash or browser storage.
- CMS dirty state is tracked in memory by comparing current form values against the last loaded/saved baseline. Unsaved CMS changes show a visible indicator and warn before browser refresh, tab close, top-level admin tab switching, CMS sub-tab switching, selecting another CMS entity, publish/restore reload flows, or logout would discard edits. `Save draft` clears the dirty indicator after a successful save; failed saves keep it. Unsaved content is never persisted in browser storage or the URL hash.
- Runtime learner behavior remains unchanged. The CMS read path remains controlled by configuration, remains disabled by default for learners, and static JSON fallback remains available.
- This remains development/admin-only and is not production CMS readiness. Production RBAC, role-based content approval, production billing operations, and full external tester handoff remain future work. CMS draft-save audit logging is implemented for topic, scenario (including structured scenario fields and full scenario JSON), prompt template, and tutor behavior profile Save draft operations; critical-change approval should wait until production roles exist.


CMS draft-save audit logging records successful Admin CMS Save draft operations in `cms_content_audit_logs`. Entries capture audit id, UTC timestamp, actor user id, actor email when available, content pack id/slug, entity type (`Topic`, `Scenario`, `PromptTemplate`, `TutorBehaviorProfile`), entity id, stable key, operation `DraftSaved`, changed field names, before/after SHA-256 hashes, reason, request id when available, source `AdminCms`, and status. The Admin CMS Audit subtab now exposes recent changes as read-only rows with filters for entity type, stable key text, and limit, aligned to the selected content pack (`static-json-v1` by default). Full before/after JSON snapshots and edited prompt/tutor/scenario bodies are intentionally not stored or displayed in audit rows; large values are represented by hashes. Secrets, passwords, tokens, provider secrets, OpenAI API keys, Paddle keys, webhook secrets, and admin bearer tokens are not logged. Runtime learner behavior remains unchanged: CMS reads are still disabled by default and static JSON fallback remains available.

- Static admin shell audit script: `powershell -ExecutionPolicy Bypass -File tools\audit_admin_shell.ps1`.
- The existing smoke script (`tools/smoke_admin_foundation.ps1`) runs this admin shell audit before backend HTTP smoke checks.
- Latest confirmed EF migration is `20260604120000_AddCmsDraftSaveAuditMetadata`.

## Interface localization

Step 5B-3d completed a full learner-facing desktop UI localization pass for the release-ready Interface languages (`en`, `es`, `fr`, `de`, `it`, `pt`, `ru`, `pl`, `ar`, `ja`, `ko`, `sr`, `hr`, `bg`). Runtime English fallback remains a safety mechanism for unexpected missing text, not the expected path for release-ready interface languages. Native/Explanation languages remain broad, and Study languages were not expanded.

Step 5B-3e completed Subtopics/Situations display localization for those release-ready Interface languages. Lesson JSON remains unchanged; runtime English fallback remains a safety mechanism only, Native/Explanation languages remain broad, and Study languages were not expanded.

Step 5B-4 added a desktop release smoke gate in `docs/desktop-release-smoke-gate.md` and the safe local helper `tools/run_desktop_release_gate.ps1`. Step 5B-8b documents the canonical current desktop tester distribution flow: run the smoke gate, then create the tester zip with `powershell -ExecutionPolicy Bypass -File .\scripts\package-tester-release.ps1`; the default handoff artifact is `artifacts\packages\EnglishVoiceTutor.Desktop-win-x64-self-contained.zip`. The tester ZIP has been verified on another Windows device after extraction: the app starts, Diagnostics is hidden by default, backend connection/account login/backend history work, core Lesson Chat / Conversation Mode / TTS / transcription / translation / hints / feedback / summary are accepted, and the single active lesson guard plus remote active lesson release stop the old device/session. `tools/smoke_single_active_lesson_guard.ps1` covers heartbeat stale protection, remote release, old-session invalidation, and old heartbeat/message rejection. Localization is considered closed for the current phase. Future Interface languages should be added only 1-2 at a time after full localization QA, production billing remains deferred, CMS/Admin content MVP work continues before external tester handoff, and public release is not declared ready.

Structured scenario editor update: the Admin CMS Scenarios subtab now includes a safer structured editor for common scenario content (title/subtopic, description, setup message, first bot message guidance, context option titles, valid context keywords, custom context rules, invalid context redirect, goal text, can-do statements, opening/first-user-task/follow-up guidance, AI tutor instructions, wrap-up/final message guidance, hint example, and wrap/final turn counters). `DefinitionJson` remains the canonical stored scenario definition; no per-field scenario database columns or EF migration were added. Structured edits parse the current `DefinitionJson`, update only known JSON paths, and write the merged valid JSON back to `DefinitionJson`, preserving unknown fields and advanced configuration in place. Advanced JSON remains available with `Format JSON` and `Validate JSON` for rare technical changes. Save draft remains explicit; invalid Advanced JSON or invalid structured numeric/required data is rejected before saving, and backend scenario validation still rejects invalid JSON, missing required fields, or accidental stable id/title/setup mismatches. CMS draft-save audit logging still records successful scenario saves with changed field names and before/after hashes without storing full scenario JSON bodies. Runtime learner behavior remains unchanged by default: the CMS read path is still disabled unless explicitly enabled, and static JSON fallback remains available.


### Controlled CMS runtime lesson-content read path

Runtime lesson content remains static JSON by default. The backend now has a development/admin-safe runtime content diagnostic at `/api/admin/dev/cms/runtime-content/status` that reports whether runtime content was loaded from packaged static JSON or from the currently published CMS snapshot. CMS runtime reads require explicit configuration: `CmsContent:UsePublishedSnapshotForRuntime=true` and `CmsContent:ReadPublishedSnapshotEnabled=true`; `CmsContent:ContentPackSlug` defaults to `static-json-v1`, and `CmsContent:FallbackToStaticJson` defaults to `true`. Runtime CMS mode reads only immutable published snapshots and never draft rows. If the selected published snapshot is missing or invalid, static JSON fallback is used only when `CmsContent:FallbackToStaticJson=true`; otherwise the diagnostic returns a clear server-side unavailable result. The diagnostic reports source, slug, published version, snapshot hash, counts, fallback state, validation state, errors, and warnings without logging full prompt, scenario, or tutor bodies. External tester handoff remains paused until this controlled path is verified with smoke tests; production RBAC and approval workflow remain future work.
