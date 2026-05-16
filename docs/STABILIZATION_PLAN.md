# Stabilization Plan

Review date: 2026-05-16.

This plan reflects the current post-fix stabilization status. It favors documentation, regression checks, and targeted smoke testing before new feature work.

## Stage 0: baseline audit/build

- Status: currently passing on Windows based on developer-provided validation.
- Reported baseline: lesson content audit 0 errors/0 warnings; root restore/build/Release build passed; backend restore/build passed.
- Goal: keep a clean baseline before every behavior change.
- Acceptance criteria: audit has 0 errors/0 warnings; Debug/Release desktop builds pass; backend build passes; no conflict markers/secrets.
- Commands:
  - `powershell -ExecutionPolicy Bypass -File tools\audit_lesson_content.ps1`
  - `dotnet restore`
  - `dotnet build`
  - `dotnet build -c Release`
  - `cd backend\EnglishVoiceTutor.Api; dotnet restore; dotnet build`
  - `rg -n "^(<<<<<<<|=======|>>>>>>>)" -S .`

## Stage 1: lesson phase/control state

- Status: improved, but still needs broader smoke testing across topics.
- Current behavior: setup/context selection does not count; valid turns count after active roleplay starts; final message leads to Awaiting Finish.
- Remaining work: verify across all five MVP topics and both typed/chained/realtime paths.
- Acceptance criteria: documented state table matches behavior; final state disables new lesson input while preserving review actions.

## Stage 2: exact manual Play voice

- Status: stabilized but should remain regression-tested.
- Current behavior: manual Play uses visible bot text, with only harmless trim/newline normalization, and routes through `/api/audio/speech` using `tts-1`.
- Remaining work: keep checklist/log checks for setup and roleplay bot messages.
- Acceptance criteria: spoken text matches visible text; logs show exact-text request checks.

## Stage 3: Realtime lifecycle

- Status: improved; expected disconnects are handled, but long-session testing is still needed.
- Current behavior: Realtime uses `/api/realtime-voice`, `gpt-realtime`, same-response assistant transcript/audio, and transcript-gated `response.create`.
- Remaining work: test start/stop/toggle/back/final/finish/close-app sequences and long sessions.
- Acceptance criteria: normal disconnects log as expected; no stuck microphone/playback/socket state; invalid transcripts do not create assistant responses.

## Stage 4: Realtime latency

- Status: not optimized yet; measure before tuning.
- Goal: capture first-audio-delta and playback-start timing for guided and free conversation sessions.
- Remaining work: produce a latency report before changing buffers, models, or streaming strategy.
- Acceptance criteria: agreed targets and measurements exist before any tuning PR.

## Stage 5: service extraction

- Status: future work, not now.
- Goal: reduce `LessonChatViewModel` risk through small behavior-preserving extractions.
- Candidate extractions: `LessonPhaseStateMachine` / `LessonTurnPolicy`, `LessonCommandStateService`, `BotVoicePlaybackCoordinator`, `ChainedVoiceInputCoordinator`, `RealtimeConversationCoordinator`, `LessonBackendRequestFactory`, and possibly `LessonPromptPolicy` / `LevelRulePolicy`.
- Acceptance criteria: one extraction at a time; no behavior changes; smoke checklist passes after each extraction.

## Stage 6: manual test coverage

- Status: should be strengthened.
- Goal: make `docs/MANUAL_TEST_CHECKLIST.md` the required Windows smoke record before feature work.
- Remaining work: record pass/fail for normal Lesson Chat, Realtime, invalid transcripts, whole-lesson summary, and Awaiting Finish review actions.
- Acceptance criteria: checklist can be executed by a developer/tester without relying on memory of recent fixes.

## Stage 7: product features

- Status: resume only after smoke checklist passes.
- Examples to defer: payments/subscriptions, avatar expansion, broad UI polish, major lesson JSON migration, and major Realtime redesign.
- Acceptance criteria: Stage 0 commands pass, manual checklist passes or accepted issues are recorded, and high-severity regressions are fixed.

## Recommended next direction

- Priority A — Documentation and regression checklist.
- Priority B — Broader scenario QA across all topics/subtopics.
- Priority C — Prompt/methodology polishing for lesson usefulness.
- Priority D — Small architecture extractions after behavior is pinned.
- Priority E — UI polish only after lesson behavior is stable.

## Current priority: Realtime recovery and usage instrumentation

Conversation Mode now has explicit startup/record recovery work: failed starts, unexpected disconnects, microphone failures, quick toggles, and final lesson state must all reset command state without requiring an app restart. Developer-only usage/cost logs are required before deciding whether normal TTS, chained voice, or Realtime should change pricing or model strategy.

## 2026-05-15 stabilization addendum

- Realtime is migrated to the GA `/v1/realtime` WebSocket interface; beta headers and beta session fields are regression risks and must not return. The GA `session.update` audio format must include `audio.input.format.rate` and `audio.output.format.rate` with the centralized 24 kHz PCM constants.
- Conversation Mode startup is recoverable: startup errors, including missing required Realtime schema parameters, produce a user-facing fallback message, close the stale socket, stop microphone/playback, and reset command state so retry does not require app restart. Startup is ready only after upstream `session.updated` and backend `session.ready`.
- Duplicate/stale Realtime events are ignored by session id on the desktop.
- English-only tutor output is a product invariant for normal Lesson Chat and Realtime. Translation remains available only through the app's Translate button.

## 2026-05-16 stabilization pass outcome

This pass is a baseline-hardening checkpoint, not a methodology rewrite. The current working assumptions are:

- 26 lesson JSON files pass the lesson content audit.
- Normal Lesson Chat, normal TTS with `tts-1`, normal voice transcription with `gpt-4o-mini-transcribe`, and Realtime Conversation Mode with `gpt-realtime` remain the intended routes.
- Realtime stays on the GA `/v1/realtime` schema and keeps pre-start opening playback through `tts-1` with `purpose=realtime_pre_start_opening`.
- Realtime assistant replies remain on Realtime audio rather than `/api/audio/speech`.
- Usage/cost instrumentation, transcript validation, English-only output locking, and lightweight hang diagnostics are protected behavior.

Next stabilization work should focus on manual smoke coverage, scenario QA, methodology/prompt polish, feedback/summary quality, and real usage/cost measurements before architecture extraction.
