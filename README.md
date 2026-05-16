# EnglishVoiceTutor.Desktop

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
- The desktop app uses `POST /api/lesson-chat/reply`.
- `POST /api/lesson-chat/mock-reply` stays available for local compatibility and testing.

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

## Current stabilization status

Feature development is paused while Lesson Chat, bot voice playback, and Realtime Conversation Mode are stabilized. Detailed review docs live in `docs/`:

- `docs/CURRENT_STATE.md`
- `docs/ARCHITECTURE_REVIEW.md`
- `docs/VOICE_AND_REALTIME_REVIEW.md`
- `docs/LESSON_FLOW_REVIEW.md`
- `docs/KNOWN_ISSUES.md`
- `docs/STABILIZATION_PLAN.md`
- `docs/MANUAL_TEST_CHECKLIST.md`

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

## 2026-05-16 stabilization baseline

The current stabilization baseline keeps runtime behavior unchanged while documenting the working routes:

- Lesson audit covers 26 JSON lessons.
- Normal Lesson Chat TTS remains `tts-1` through `/api/audio/speech`.
- Normal voice transcription remains `gpt-4o-mini-transcribe` through `/api/audio/transcribe`.
- Realtime Conversation Mode uses `gpt-realtime` on the GA `/v1/realtime` schema.
- Realtime pre-start opening playback remains enabled through `tts-1` with `purpose=realtime_pre_start_opening`.
- Realtime-generated assistant replies stay on Realtime audio and are not routed through `/api/audio/speech`.
- Usage/cost instrumentation, transcript validation, English-only tutor output, lesson content audit, avatar tooltip cleanup, and lightweight hang diagnostics are protected stabilization behavior.

Recommended next work: full MVP smoke-test, all-lesson scenario QA, methodology/prompt polish by level, feedback/summary quality polish, and real usage/cost measurement before architecture extraction.
