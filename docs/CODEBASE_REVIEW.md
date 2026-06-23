# Codebase Review

## Date

2026-05-16.

## Baseline

- Lesson content audit parses 26 lesson JSON files with 0 errors and 0 warnings in this container via the Python audit entry point.
- Static policy tests pass for tutor prompts, lesson turns, feedback/summary routing, tutor profile policy, command state, voice state, usage/cost, language lock, Realtime GA session/content schema, Realtime record button behavior, Realtime logging, Realtime opening playback, Realtime transcription recovery, avatar UI, and desktop hang diagnostics.
- Windows-specific PowerShell and .NET build commands could not run in this Linux container because `powershell`/`pwsh` and `dotnet` are not installed here; they remain the required validation commands on a Windows developer machine.

## Areas reviewed

- `Views/`: WPF views and light code-behind; stable, no technical avatar tooltip regression found by policy test.
- `ViewModels/`: screen state and command orchestration; stable but `LessonChatViewModel` remains the main technical-debt hotspot.
- `Services/`: desktop content, backend, recording, playback, settings, cleanup, and history services; stable.
- `Services/Voice/`: Realtime desktop WebSocket, microphone, and PCM playback services; stable after GA Realtime migration.
- `Shared/LessonPolicies/`: turn/transcript validation policy; stable and protected by tests.
- `Models/`: DTOs, UI models, and lesson content schema models; stable.
- `Content/Lessons/`: 26 lesson JSON files; audit-clean, but scenario QA remains needed.
- `Content/Tutors/`: tutor profile content; stable and separate from lesson JSON.
- `backend/EnglishVoiceTutor.Api/`: minimal API backend, OpenAI services, Realtime gateway, and usage/cost models; stable.
- `tools/`: audit and static policy tests; stable and current.
- `docs/`: refreshed to capture the current working baseline and next priorities.

## Cleanups made

- Replaced an obsolete TODO comment in the mock lesson chat service with a current explanation of the explicit compatibility/testing mock endpoint.
- Refreshed stabilization documentation dates and added current baseline notes across the main review docs.
- Added this concise codebase review document for the next development phase.

## Safe refactors made

- No runtime behavior refactor was made. The only runtime-file change is a comment clarification in `MockLessonChatService`.

## Things intentionally not changed

- Lesson methodology, prompts, and lesson JSON content.
- Realtime GA schema, Realtime pre-start opening playback, and Realtime assistant audio routing.
- Normal Lesson Chat TTS model (`tts-1`) and normal transcription model (`gpt-4o-mini-transcribe`).
- Usage/cost instrumentation, transcript validation, English-only output locking, lesson audit safeguards, and hang diagnostics.
- `LessonChatViewModel` structure, async lifecycle semantics, UI layout, topics, avatars, payments, or subscriptions.

## Risks / technical debt

- `LessonChatViewModel` remains large and should be extracted gradually only after smoke-test coverage is reliable.
- Realtime retry/fallback tuning still needs real-session measurements.
- Scenario QA is still needed across all 26 lessons.
- Cost estimates remain approximate until pricing constants and measured test sessions are updated.
- UI polish and multi-avatar product work are future tasks, not stabilization tasks.

## Recommended next tasks

1. Run the full smoke-test across all product topics.
2. Create a scenario QA report for all 26 lesson JSON files.
3. Polish methodology and prompt behavior by level.
4. Improve feedback and summary quality from real lesson transcripts.
5. Measure usage/cost across 10-20 representative test lessons.
