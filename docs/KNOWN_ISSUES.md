# Known Issues

Review date: 2026-05-13.

## ISSUE-001: Realtime WebSocket close handshake exception

- Severity: high (reduced by this pass; needs manual verification).
- Affected files: `backend/EnglishVoiceTutor.Api/Services/RealtimeVoiceSessionService.cs`, `Services/Voice/RealtimeVoiceConversationEngine.cs`, `backend/EnglishVoiceTutor.Api/Program.cs`.
- Observed behavior: Kestrel can log an unhandled `WebSocketException` when the desktop closes without a full close handshake.
- Expected behavior: expected desktop disconnect logs at Information and does not escape `RunGatewayAsync`; unexpected exceptions still log as Error.
- Likely cause: desktop receive loop did not treat `ConnectionClosedPrematurely` as a normal client disconnect.
- Recommended fix: catch expected `WebSocketException`/cancellation around desktop `ReceiveAsync`, log Information, return, and keep OpenAI socket cleanup.
- Test needed: start realtime, close Conversation Mode/Back, verify backend logs normal disconnect and no Kestrel fail.

## ISSUE-002: Realtime first assistant audio delta too slow

- Severity: high.
- Affected files: `backend/EnglishVoiceTutor.Api/Services/RealtimeVoiceSessionService.cs`, `Services/Voice/RealtimeVoiceConversationEngine.cs`, `Services/Voice/RealtimeAudioPlaybackService.cs`.
- Observed behavior: logs have shown high first-audio latency.
- Expected behavior: first assistant audio should start quickly enough for natural conversation.
- Likely cause: combined model latency, manual turn handling, buffering threshold, backend relay, and desktop playback startup.
- Recommended fix: Stage 4 measurement first; separate backend first-delta, desktop receive, buffer fill, playback start, and underrun metrics before tuning.
- Test needed: record first-delta/playback metrics for 10 guided and 10 free-conversation turns.

## ISSUE-003: Conversation Mode state machine instability

- Severity: high.
- Affected files: `ViewModels/LessonChatViewModel.cs`, `Services/Voice/RealtimeVoiceConversationEngine.cs`, `Services/Voice/RealtimeMicrophoneCaptureService.cs`, `Services/Voice/RealtimeAudioPlaybackService.cs`.
- Observed behavior: Conversation Mode can behave inconsistently around setup, after context selection, and cleanup.
- Expected behavior: guided setup may enable mode but defers realtime until context; active roleplay starts realtime; toggle/back/finish cleanly stop all resources.
- Likely cause: Conversation Mode flags, lesson phase, realtime started flags, and command invalidation are distributed through a very large ViewModel.
- Recommended fix: first document and test state transitions; later extract a `RealtimeConversationCoordinator`.
- Test needed: guided before-context, guided after-context, free conversation, unavailable backend, and disconnect smoke tests.

## ISSUE-004: Finish lesson / button state regressions

- Severity: high.
- Affected files: `ViewModels/LessonChatViewModel.cs`, `Views/LessonChatView.xaml`.
- Observed behavior: repeated regressions in Finish lesson and other command states.
- Expected behavior: setup, active, final-limit, awaiting-finish, and finished states should have deterministic enabled buttons.
- Likely cause: command state is computed from many mutable properties and both attribute-driven and manual invalidation.
- Recommended fix: Stage 1 formal state table and command-state assertions/manual checklist before refactor.
- Test needed: manual checklist for all phase/busy states plus future ViewModel unit tests.

## ISSUE-005: `LessonChatViewModel` too large / mixed responsibilities

- Severity: high.
- Affected files: `ViewModels/LessonChatViewModel.cs`.
- Observed behavior: one file owns lesson state, backend DTOs, recording, TTS playback, realtime, command state, cache cleanup, and avatar/status.
- Expected behavior: smaller coordinators/services with clear ownership.
- Likely cause: rapid feature development accumulated in the main chat ViewModel.
- Recommended fix: Stage 5 extraction after behavior stabilization; do not perform a broad rewrite now.
- Test needed: regression suite around lesson phase, voice exactness, realtime lifecycle, and final limits before extraction.

## ISSUE-006: Manual Play exact text risk

- Severity: medium.
- Affected files: `ViewModels/LessonChatViewModel.cs`, `Services/LessonChatBackendService.cs`, `backend/EnglishVoiceTutor.Api/Services/AudioSpeechService.cs`.
- Observed behavior: current code uses visible text with trim/newline normalization and logs exactness, but logic is embedded with segmentation/prefetch.
- Expected behavior: manual Play spoken text exactly matches visible bot text except harmless trim/newline normalization.
- Likely cause: future changes to normalization/segmentation could alter text before speech.
- Recommended fix: add a focused test/guard for manual Play request text; avoid changing `GetExactBotVoiceText` without tests.
- Test needed: visible setup/roleplay messages with punctuation/newlines; confirm `RawTextLength == VoiceTextLength` except trim and `IsExactText=True`.

## ISSUE-007: Final turn limit enforcement risk

- Severity: high.
- Affected files: `ViewModels/LessonChatViewModel.cs`, `backend/EnglishVoiceTutor.Api/Services/LessonLimitHelper.cs`, `backend/EnglishVoiceTutor.Api/Services/RealtimeVoiceSessionService.cs`, lesson JSON files.
- Observed behavior: repeated regressions around final limits and awaiting Finish lesson.
- Expected behavior: learner turn counting starts only in active roleplay; final message shown once; only Finish lesson remains active.
- Likely cause: turn counting and final state are implemented in multiple paths: text, chained voice, and realtime.
- Recommended fix: Stage 1 state machine stabilization and Stage 6 manual coverage.
- Test needed: guided lesson to final turn and Free Conversation to turn 30.

## ISSUE-008: Chained TTS latency/reliability risk

- Severity: medium.
- Affected files: `ViewModels/LessonChatViewModel.cs`, `Services/LessonChatBackendService.cs`, `Services/AudioPlaybackService.cs`, `backend/EnglishVoiceTutor.Api/Services/AudioSpeechService.cs`.
- Observed behavior: chained fallback has multiple network/model/file/playback steps and can be slow or fail.
- Expected behavior: failure should not break chat; status should recover; fallback should remain available.
- Likely cause: inherent multi-step flow plus timeouts and temp-file playback.
- Recommended fix: keep diagnostics and avoid further behavior changes until exactness/control state are stable.
- Test needed: record/transcribe/send/TTS playback with backend configured; with backend unavailable, verify these actions do not crash and show friendly backend-required or backend-unavailable messages.

## ISSUE-009: Old backend process / stale runtime testing risk

- Severity: medium.
- Affected files: `README.md`, `docs/MANUAL_TEST_CHECKLIST.md`, backend run workflow.
- Observed behavior: test results can be misleading if an old backend process is still serving endpoints.
- Expected behavior: testers verify the running backend build/path before manual testing.
- Likely cause: local Windows processes survive rebuilds or multiple terminals.
- Recommended fix: add stale process check to manual checklist.
- Test needed: confirm process ID/start time and backend logs after each rebuild.

## ISSUE-010: Mock fallback text appears in production code paths

- Severity: low.
- Affected files: `backend/EnglishVoiceTutor.Api/Constants/ApiConstants.cs`, `backend/EnglishVoiceTutor.Api/Services/MockLessonChatService.cs`, `Localization/AppLocalization.cs`.
- Observed behavior: search finds mock fallback text and mock services.
- Expected behavior: mock output should only be used for explicit mock route or degraded fallback, never confused with real OpenAI output.
- Likely cause: intentional fallback services remain registered for robustness.
- Recommended fix: document route/fallback ownership; do not delete during stabilization.
- Test needed: configured backend should use real `/api/lesson-chat/reply`; `/api/lesson-chat/mock-reply` should be the only deliberate mock endpoint.

## Search review notes

Repository-wide searches on 2026-05-13 covered TODO/FIXME/HACK/mock, prepared voice text terms, endpoint strings, conversation/finish/final state terms, WebSocket receive/close terms, and cancellation tokens. Findings are reflected in the issues above and in the architecture/voice reviews.
