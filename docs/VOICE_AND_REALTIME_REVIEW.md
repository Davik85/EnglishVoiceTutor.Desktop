# Voice and Realtime Review

Review date: 2026-05-13.

## Current chained voice path

```text
user voice -> AudioRecordingService -> /api/audio/transcribe -> LessonChatBackendService -> /api/lesson-chat/reply -> /api/audio/speech -> AudioPlaybackService
```

This path uses separate STT, text generation, TTS, and playback operations. It is the fallback/non-realtime path and should remain available while Realtime is unstable.

## Current manual Play voice path

Manual Play starts from a visible `ChatMessageViewModel.Text` value in `LessonChatViewModel`. The current code:

- trims/normalizes visible message text through `GetExactBotVoiceText(message)`.
- for manual Play, sends one full segment containing the exact normalized text.
- calls `lessonChatBackendService.CreateBotSpeechAsync(...)`.
- `LessonChatBackendService` posts to `/api/audio/speech`.
- `AudioPlaybackService` saves and plays the returned audio.

Diagnostic logs include `RawTextLength`, `VoiceTextLength`, and `IsExactText` before requests. A second segment-level log also records the same exactness check.

## Current auto-play path

Auto-play also starts from visible bot message text, but may skip playback when the newest message changed, setup auto-play is disabled, or the text exceeds auto-play limits. Current setup messages are intentionally skipped for auto-play. Auto-play still uses `/api/audio/speech` when it plays.

## Current Realtime Conversation Mode path

```text
LessonChatViewModel -> RealtimeVoiceConversationEngine -> /api/realtime-voice
/api/realtime-voice -> RealtimeVoiceSessionService -> OpenAI Realtime WebSocket
OpenAI response audio delta -> backend -> desktop -> RealtimeAudioPlaybackService
OpenAI response transcript delta -> backend -> desktop -> LessonChatViewModel message text
RealtimeMicrophoneCaptureService -> RealtimeVoiceConversationEngine -> backend -> OpenAI input_audio_buffer
```

Realtime must use transcript and audio from the same OpenAI Realtime assistant response. It must not create visible text through one backend text response and audio through separate TTS for Conversation Mode.

## Endpoint ownership by path

- `/api/audio/speech`: manual Play and chained/auto bot TTS fallback.
- `/api/audio/speech-stream`: backend streaming TTS endpoint; currently separate from the main documented manual Play path.
- `/api/realtime-voice`: Conversation Mode WebSocket only.
- `/api/audio/transcribe`: chained user voice transcription.
- `/api/lesson-chat/reply`: text reply for typed/chained fallback.

## Exact text vs spoken text rule

Required rule: visible bot text and spoken bot text must be identical for manual Play voice, except harmless newline normalization and trimming.

Current implementation status:

- The source is `message.Text` from the visible bot message.
- `GetExactBotVoiceText` normalizes line endings and trims.
- Manual Play passes the complete normalized text as a single segment.
- The request path logs raw and voice text lengths and `IsExactText`.

Remaining risk:

- The same ViewModel owns exact-text logic, auto-play segmentation, prefetch, cancellation, and UI state.
- Any future change to `NormalizeVoiceWhitespace`, segment selection, or prefetch could reintroduce mismatches.
- Auto-play can intentionally skip or limit playback; this is separate from the manual Play exact-text requirement.

## Realtime known issues

- State transitions between guided setup, deferred realtime start, active realtime, and stop cleanup remain fragile.
- First assistant audio delta/playback can be too slow in logs and needs latency instrumentation review before tuning.
- Desktop disconnect without close handshake previously could surface as an unhandled Kestrel failure; this pass adds safe expected-disconnect handling in `RealtimeVoiceSessionService`.
- Realtime turn limit enforcement is separate from normal lesson text path and needs smoke testing.
- Realtime unavailable path should clearly fall back to text/chained voice without leaving buttons stuck.

## WebSocket disconnect handling issue

Observed failure: `System.Net.WebSockets.WebSocketException: The remote party closed the WebSocket connection without completing the close handshake.`

Expected behavior: normal desktop close/back/finish should be logged at Information and should not escape `RunGatewayAsync` as an unhandled exception. Unexpected receive-loop errors should still be logged as errors.

Safe fix applied in this pass: expected premature desktop disconnect and cancellation are handled in `RunGatewayAsync`/desktop receive loop, then OpenAI socket cleanup still runs.

## Audio latency observations

Current logs include first-segment/first-playback metrics for chained TTS and first-audio-delta/playback metrics for Realtime. Known risk remains that Realtime first assistant audio delta and/or playback-start timing can miss usability targets. Do not tune models or rewrite streaming until Stage 4 of the stabilization plan.

## Recommended stabilization order

1. Freeze features and keep audit/build clean.
2. Lock down lesson/control state and final-turn behavior.
3. Add explicit tests/checklists for manual Play exact text.
4. Stabilize Realtime disconnect lifecycle.
5. Measure and then improve Realtime latency.
6. Extract voice/realtime coordinators from `LessonChatViewModel` only after behavior is pinned.
