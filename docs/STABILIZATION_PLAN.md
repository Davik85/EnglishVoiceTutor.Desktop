# Stabilization Plan

Review date: 2026-05-13.

## Stage 0: freeze features and make builds/audit clean

- Goal: stop feature churn and establish a clean baseline.
- Files likely touched: docs, README, tiny critical fixes only.
- Risks: discovering existing build/audit failures that require triage.
- Acceptance criteria: audit has 0 errors/0 warnings; Debug/Release desktop builds pass; backend build passes; no conflict markers/secrets.
- Commands to run:
  - `powershell -ExecutionPolicy Bypass -File tools\audit_lesson_content.ps1`
  - `dotnet restore`
  - `dotnet build`
  - `dotnet build -c Release`
  - `cd backend\EnglishVoiceTutor.Api; dotnet restore; dotnet build`
  - `rg -n "^(<<<<<<<|=======|>>>>>>>)" -S .`
- Manual tests: none beyond startup smoke if builds pass.

## Stage 1: stabilize control state and lesson phase state machine

- Goal: make setup, active roleplay, final, awaiting-finish, and finished states deterministic.
- Files likely touched: `ViewModels/LessonChatViewModel.cs`, possibly `Views/LessonChatView.xaml`.
- Risks: button enablement regressions and accidental lesson methodology changes.
- Acceptance criteria: documented state table matches behavior; user turns start only after roleplay; final state leaves Finish lesson available.
- Commands to run: Stage 0 commands plus targeted ViewModel tests when available.
- Manual tests: setup buttons, Finish lesson early, guided context selection, active roleplay, final limit, awaiting Finish lesson, summary.

## Stage 2: stabilize exact manual Play voice

- Goal: guarantee manual Play uses exact visible bot text.
- Files likely touched: `ViewModels/LessonChatViewModel.cs`, `Services/LessonChatBackendService.cs`, tests.
- Risks: changing normalization could affect audio requests or cache keys.
- Acceptance criteria: manual Play request text equals visible message text except trim/newline normalization; logs/tests prove `IsExactText=True`.
- Commands to run: Stage 0 commands plus future unit test for exact text.
- Manual tests: Play setup and roleplay bot messages; compare visible text and logs.

## Stage 3: stabilize Realtime connection lifecycle and close handling

- Goal: clean start/stop/back/finish/toggle-off behavior without unhandled WebSocket exceptions.
- Files likely touched: `ViewModels/LessonChatViewModel.cs`, `Services/Voice/RealtimeVoiceConversationEngine.cs`, `backend/EnglishVoiceTutor.Api/Services/RealtimeVoiceSessionService.cs`.
- Risks: swallowing real errors or leaving OpenAI sockets open.
- Acceptance criteria: expected disconnect logs Information; unexpected errors log Error; microphone/playback/socket cleanup is idempotent.
- Commands to run: Stage 0 commands; backend run with logs visible.
- Manual tests: start realtime, speak, toggle off, Back, Finish, close app, verify logs.

## Stage 4: stabilize Realtime audio/transcript latency

- Goal: reduce first-audio and playback-start latency based on measurements.
- Files likely touched: `RealtimeVoiceSessionService.cs`, `RealtimeVoiceConversationEngine.cs`, `RealtimeAudioPlaybackService.cs`, `RealtimeMicrophoneCaptureService.cs`.
- Risks: buffer tuning can cause underruns; model/session changes can alter behavior.
- Acceptance criteria: latency metrics meet agreed target without transcript/audio mismatch.
- Commands to run: Stage 0 commands plus manual metric capture.
- Manual tests: 10 guided realtime turns and 10 free conversation realtime turns with first-delta/playback metrics.

## Stage 5: extract smaller services from `LessonChatViewModel`

- Goal: reduce regression risk by moving coherent responsibilities into coordinators.
- Files likely touched: `ViewModels/LessonChatViewModel.cs`, new coordinator/service files, tests.
- Risks: large extraction can change behavior if done before tests.
- Acceptance criteria: one extraction at a time; no UI behavior changes; smoke tests pass after each extraction.
- Commands to run: Stage 0 commands plus targeted tests.
- Manual tests: full checklist after each extraction.

## Stage 6: expand manual test coverage

- Goal: make the Windows smoke checklist mandatory before feature work.
- Files likely touched: `docs/MANUAL_TEST_CHECKLIST.md`, possible test scripts.
- Risks: checklist drift if not maintained.
- Acceptance criteria: tester can reproduce exact state/voice/realtime checks and record pass/fail.
- Commands to run: Stage 0 commands.
- Manual tests: every item in `docs/MANUAL_TEST_CHECKLIST.md`.

## Stage 7: continue product features only after smoke tests pass

- Goal: resume features from a stable baseline.
- Files likely touched: feature-dependent.
- Risks: returning to fast changes without regression gates.
- Acceptance criteria: Stage 0 commands pass, manual checklist passes, known high-severity issues are resolved or explicitly accepted.
- Commands to run: Stage 0 commands and feature-specific tests.
- Manual tests: focused feature tests plus regression checklist sections for lesson flow and voice.
