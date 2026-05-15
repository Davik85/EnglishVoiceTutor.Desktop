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

## 2026-05-14 update: recovery and cost instrumentation

Realtime Conversation Mode startup and record-button recovery are treated as stabilization priorities. The desktop state model must allow retry after backend/OpenAI startup failure, unexpected socket disconnect, and microphone start failure. Backend developer logs now collect raw usage data for typed Lesson Chat, chained transcription, `tts-1` speech, and `gpt-realtime` sessions so future cost comparisons use measured usage instead of theory alone.

## 2026-05-15 update: Realtime GA and English-only output

Realtime Conversation Mode now uses the GA Realtime WebSocket path `wss://api.openai.com/v1/realtime?model=gpt-realtime` with only the normal `Authorization: Bearer ...` header. The deprecated `OpenAI-Beta: realtime=v1` header is not used. Session configuration uses GA-shaped `session.update` data: `type: realtime`, `model: gpt-realtime`, `output_modalities: ["audio"]`, nested `audio.input.format` and `audio.output.format` objects, output voice `coral`, and English input transcription. Both audio format objects must include `type: "audio/pcm"` and `rate: 24000`; the explicit `audio.output.format.rate` field fixes the Windows startup blocker `Missing required parameter: 'session.audio.output.format.rate'`.

Startup is strict: desktop does not treat Conversation Mode as ready until the backend receives a successful upstream `session.updated` after `session.update`, seeds recent conversation, and sends `session.ready`. Missing required Realtime schema parameters are fatal for that startup attempt, classified as startup failure/upstream realtime error, relayed as `session.startup_failed`, and cleaned up so the Conversation Mode button can be clicked again without restarting the app.

Tutor output language is locked to English in both normal Lesson Chat and Realtime. The tutor must refuse requests such as “Speak Finnish” or “Can you speak Russian?” in English and continue the selected lesson. The Translate button remains a separate review feature and does not permit the tutor to change lesson language.

## 2026-05-15 GA content-part correction

The remaining GA runtime crash `Invalid value: 'text'. Value must be 'output_text'.` was traced to recent conversation seeding for assistant messages. Realtime `conversation.item.create` events no longer use a generic content part type of `text`: user/system-style text is mapped to `input_text`, and assistant seed text is mapped to `output_text`. Realtime `response.create` remains an audio-generation event with `output_modalities: ["audio"]`; it does not create fake text content items and does not route generated Realtime assistant turns through `/api/audio/speech`.

Runtime upstream errors after `session.ready` are recoverable. The backend emits `session.runtime_failed`, logs the shutdown as an upstream/runtime Realtime error, and closes stale upstream state. The desktop cleans microphone, playback, transcript buffers, and active-response flags, returns to a retryable state, and ignores stale events from old session IDs.

## 2026-05-15 update: Conversation Mode opening playback and transcript recovery

Conversation Mode now has an explicit `OpeningPlayback` startup state. After the Realtime session is configured but before recording is enabled, the desktop selects the current visible bot prompt that is awaiting the learner and plays that exact visible text through the normal `/api/audio/speech` path with `tts-1` and purpose `realtime_pre_start_opening`. This is pre-existing lesson text only; generated Realtime assistant turns still come from Realtime audio events and are not routed through `/api/audio/speech`.

During `OpeningPlayback`, the red record button is disabled because recording remains allowed only in `Ready`. If opening playback fails or is canceled, Conversation Mode transitions to `Ready` so the learner can still record. Spoken opening message ids are remembered by the view model so leaving and re-entering Conversation Mode during the same active lesson does not replay the same prompt automatically.

Realtime voice placeholders now resolve to a final state: accepted transcript text, the retry/status message, or a technical retry status after microphone/network failure. Invalid retry messages remain non-turn technical user messages: they are not feedback-eligible, do not increment learner turns, do not push the conversation forward, and are excluded from summaries.

Realtime transcript failures now log compact diagnostics with session id, realtime user turn id/item id, learner turn number, audio chunk count, buffered bytes, estimated audio duration, transcript length, validation reason, and retry flags. When Realtime transcription fails or times out and buffered PCM audio exists, the desktop makes one fallback `/api/audio/transcribe` attempt using `gpt-4o-mini-transcribe` with English transcription. A valid fallback transcript replaces the placeholder and is sent into Realtime as a text user turn so the assistant response still uses Realtime audio; an invalid fallback keeps the retry message.
