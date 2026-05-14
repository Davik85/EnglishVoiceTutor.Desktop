# Voice and Realtime Review

Review date: 2026-05-14.

This document records current voice contracts that should be regression-tested before future voice or realtime changes.

## Endpoint ownership by path

- `/api/audio/speech`: normal Lesson Chat manual Play, auto-play bot voice, and chained TTS fallback.
- `/api/audio/speech-stream`: backend streaming TTS endpoint; currently separate from the main manual Play path.
- `/api/audio/transcribe`: chained/non-realtime user voice transcription.
- `/api/lesson-chat/reply`: normal typed and chained-fallback text reply.
- `/api/realtime-voice`: Conversation Mode WebSocket only.

Normal Lesson Chat TTS currently uses `tts-1`. Realtime Conversation Mode currently uses `gpt-realtime`.

## Normal chained voice fallback

```text
user voice -> AudioRecordingService -> /api/audio/transcribe -> transcript validation -> /api/lesson-chat/reply -> optional /api/audio/speech -> AudioPlaybackService
```

This path uses separate STT, text generation, TTS, and playback operations. It remains the fallback/non-realtime voice path and should not be removed while Realtime is still being tested.

Invalid/empty/non-English transcripts show a retry message, do not count as lesson turns, and must stay excluded from feedback and summary.

## Manual Play voice

Manual Play starts from a visible `ChatMessageViewModel.Text` value in `LessonChatViewModel`. Current contract:

- Manual Play must speak exactly the visible bot message, except harmless trim/newline normalization.
- The desktop sends that exact normalized text to `/api/audio/speech`.
- `/api/audio/speech` uses the normal chat TTS model `tts-1`.
- Diagnostic logs should show exact-text checks such as `RawTextLength`, `VoiceTextLength`, and `IsExactText`.

Manual Play is a review action. Existing bot messages remain playable in Awaiting Finish until Finish lesson is clicked.

## Auto-play bot voice

Auto-play also uses visible bot message text and `/api/audio/speech` with `tts-1`, but may skip playback when setup auto-play is disabled, the newest message changed, playback is already busy, or the text exceeds auto-play limits. Setup messages are intentionally skipped for auto-play.

Auto-play skipping is separate from the manual Play exact-visible-text requirement.

## Realtime Conversation Mode

```text
LessonChatViewModel -> RealtimeVoiceConversationEngine -> /api/realtime-voice
/api/realtime-voice -> RealtimeVoiceSessionService -> OpenAI Realtime WebSocket (gpt-realtime)
OpenAI response audio delta -> backend -> desktop -> RealtimeAudioPlaybackService
OpenAI response transcript delta -> backend -> desktop -> LessonChatViewModel message text
RealtimeMicrophoneCaptureService -> RealtimeVoiceConversationEngine -> backend -> OpenAI input_audio_buffer
```

Realtime generated assistant turns must not use `/api/audio/speech`. Assistant transcript and audio must come from the same OpenAI Realtime response.

Current Realtime transcript behavior:

- Desktop creates a pending user placeholder such as `[Voice message]` when the learner speaks.
- Backend commits user audio and waits for transcription.
- `response.create` is gated by a valid transcript.
- A valid Realtime user transcript replaces the pending placeholder and counts as a learner turn only when active roleplay is underway.
- Invalid/empty/non-English transcripts mark the placeholder as an invalid retry message, do not count as lesson turns, do not generate a normal assistant response, and remain excluded from feedback and summary.

## Shared teaching policy

Normal Lesson Chat and Realtime share the canonical tutor teaching policy from `LessonPromptBuilder`. Realtime changes only the transport and voice-first formatting. Tutor identity comes from `TutorProfile`; scenario JSON remains avatar-neutral; level complexity belongs to level rules/policy.

## Awaiting Finish voice behavior

After the final tutor message, the lesson enters Awaiting Finish:

- Send, Start recording, Hint, Back, and Conversation Mode are disabled for new lesson input.
- Finish lesson remains enabled.
- View feedback remains enabled on valid learner messages.
- Translate remains enabled on existing messages.
- Play voice remains enabled on existing bot messages through `/api/audio/speech` until Finish lesson is clicked.

## Known risks and regression checks

- Realtime lifecycle is improved, including expected disconnect handling, but still needs long-session testing.
- Realtime latency has not been optimized; measure first-audio and playback timing before tuning.
- Transcript/audio mismatch is a high-severity regression: Realtime assistant transcript and audio must always come from the same response.
- Manual Play exact-visible-text behavior should remain covered by checklist and logs.
- Chained fallback should remain usable when Realtime is unavailable.
