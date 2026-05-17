# Voice and Realtime Review

Review date: 2026-05-17.

This document records the current MVP voice architecture. It intentionally reflects the stable MVP path after recent Conversation Mode stabilization.

## Current MVP voice decision

Conversation Mode uses the stable TTS provider by default:

`microphone recording -> audio transcription -> lesson chat reply -> gpt-4o-mini-tts playback`

Realtime remains in the codebase for future testing, but it is not the default MVP path. The learner must hear exactly the same text that is displayed, so Conversation Mode does not shorten, summarize, rewrite, or chunk spoken text.

## Normal Lesson Chat voice path

Normal Lesson Chat uses a chained backend path:

1. The learner types or records a message.
2. Recorded audio is transcribed with `gpt-4o-mini-transcribe`.
3. Valid learner text is sent to the lesson chat reply endpoint.
4. Bot text is displayed in chat.
5. Play voice / normal auto-play uses `/api/audio/speech`.

Normal Lesson Chat TTS settings:

- model: `tts-1`;
- purpose: `lesson_chat_tts`;
- voice: backend/default voice configuration;
- speech instructions: not used for normal `tts-1` Lesson Chat playback.

Normal Lesson Chat TTS should continue to speak the visible bot message text.

## Conversation Mode voice path

Default MVP Conversation Mode uses the stable TTS provider, not Realtime:

1. The learner enters Conversation Mode from Lesson Chat.
2. The overlay shows the full avatar mode with the red record button, exit/back button, latest user phrase bubble, latest bot phrase bubble, and bottom-left Hint button.
3. The learner records audio.
4. Audio is transcribed through the normal transcription endpoint.
5. The transcript is sent through the same lesson chat reply flow as normal Lesson Chat.
6. The bot reply is displayed in the Conversation Mode bot bubble and persisted into the lesson transcript.
7. The displayed bot reply is sent to `/api/audio/speech` with Conversation Mode TTS settings.
8. The returned audio is played back.
9. Multiple turns repeat the same flow.

Conversation Mode TTS settings:

- model: `gpt-4o-mini-tts`;
- purpose: `conversation_mode_tts`;
- voice: `coral`;
- speed: `1.0`;
- instructions: calm speech instructions for natural, friendly learner-facing delivery.

The visible text and spoken text must match exactly. Conversation Mode must not use spoken-only shortening, summarization, rewriting, or chunking.

## Why the MVP uses the TTS provider

Realtime was too unstable for the MVP lifecycle. The TTS provider path is more stable because it reuses the already-working transcription, lesson chat reply, and speech playback endpoints. Switching Conversation Mode speech to `gpt-4o-mini-tts` also improved speech calmness because the request can include speech instructions.

This decision prioritizes predictable MVP testing over lower-latency future experiments.

## Realtime status

Realtime is implemented/partially stabilized and remains in the repository for future work. It should be treated as a provider-switch/future option, not the default MVP Conversation Mode provider.

Realtime assets that remain useful for future testing include:

- desktop WebSocket client/coordinator code;
- backend Realtime gateway;
- GA schema work;
- logging and stop-reason diagnostics;
- fallback/recovery policy tests;
- overlay policy coverage.

Default MVP Conversation Mode should not open `/api/realtime-voice` or create an OpenAI Realtime session.

## Feedback, hint, and transcript behavior in voice flows

- Feedback is tied to the clicked message through `sourceMessageId` and `sourceMessageKind`.
- Context-selection feedback is phrase-level and does not treat the phrase as an active roleplay answer.
- Conversation Mode transcript messages should be reviewable after returning to Lesson Chat.
- Hint works in normal Lesson Chat and in the Conversation Mode overlay.
- Invalid retry/status messages should not count as learner turns and should stay excluded from summary input.

## Cost and logging expectations

Backend logs should make the current voice routing visible:

- normal Lesson Chat speech requests use `Model=tts-1` and `Purpose=lesson_chat_tts`;
- Conversation Mode speech requests use `Model=gpt-4o-mini-tts` and `Purpose=conversation_mode_tts`;
- Conversation Mode speech requests include `HasInstructions=True`;
- no Realtime WebSocket opens by default in the MVP path.

Exact pricing remains approximate until real usage logs are collected and pricing constants are completed.
