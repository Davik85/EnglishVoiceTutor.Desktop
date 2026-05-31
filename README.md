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

## Security rule

OpenAI API keys must never be stored in the desktop app and must never be committed to source control.

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

Recommended next work: plan remaining billing operations only: `subscription.resumed` / `subscription.activated` restore policy, refund/chargeback policy, manual revocation automation, production Paddle webhook setup, desktop upgrade/paywall UI, future Apple/Google mobile entitlement bridge, and optional background reconciliation.


## Local admin shell

- Local admin shell: http://localhost:5000/admin/
- Requires running backend and a configured Development bootstrap admin.
- The local admin shell currently supports capabilities view, read-only user lookup, read-only per-user audit log, manual Premium grant/revoke, and free lesson allowance reset for selected users.
- The local admin shell is organized into tabs: Overview, User Lookup, Premium, Free Lesson, Audit Log, and System.
- User lookup also shows a Premium entitlement schedule (current + future active Premium grants) in addition to currently active entitlements.
- It does not require a database migration.
- JWT remains in memory only for this phase.
- Static admin shell audit script: `powershell -ExecutionPolicy Bypass -File tools\audit_admin_shell.ps1`.
- The existing smoke script (`tools/smoke_admin_foundation.ps1`) now runs this admin shell audit before backend HTTP smoke checks.
- Latest confirmed EF migration is `20260529000000_AddPaddlePaymentPersistenceV1`.

## Interface localization

Step 5B-3d completed a full learner-facing desktop UI localization pass for the release-ready Interface languages (`en`, `es`, `fr`, `de`, `it`, `pt`, `ru`, `pl`, `ar`, `ja`, `ko`, `sr`, `hr`, `bg`). Runtime English fallback remains a safety mechanism for unexpected missing text, not the expected path for release-ready interface languages. Native/Explanation languages remain broad, and Study languages were not expanded.

Step 5B-3e completed Subtopics/Situations display localization for those release-ready Interface languages. Lesson JSON remains unchanged; runtime English fallback remains a safety mechanism only, Native/Explanation languages remain broad, and Study languages were not expanded.

Step 5B-4 added a desktop release smoke gate in `docs/desktop-release-smoke-gate.md` and the safe local helper `tools/run_desktop_release_gate.ps1`. Localization is considered closed for the current phase. Future Interface languages should be added only 1-2 at a time after full localization QA, and production billing remains deferred until desktop hardening is complete.
