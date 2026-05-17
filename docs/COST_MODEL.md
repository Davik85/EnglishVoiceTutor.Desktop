# Cost and Usage Instrumentation Model

Review date: 2026-05-17.

This document describes the current MVP model usage and the developer-only usage/cost instrumentation. Pricing and cost estimates remain approximate where pricing constants are missing or incomplete.

## Current model usage

- Lesson chat reply: the current chat model configured and used by the backend lesson chat service.
- Feedback, hint, and summary: backend lesson-related OpenAI calls as configured by the current backend services.
- Transcription: `gpt-4o-mini-transcribe`.
- Normal Lesson Chat TTS: `tts-1` with `purpose=lesson_chat_tts`.
- Conversation Mode TTS: `gpt-4o-mini-tts` with `purpose=conversation_mode_tts`.
- Realtime: `gpt-realtime` is not default for MVP; keep for future cost review if/when Realtime is re-enabled as a provider option.

## Current MVP voice decision

Conversation Mode uses the stable TTS provider by default:

`microphone recording -> audio transcription -> lesson chat reply -> gpt-4o-mini-tts playback`

Realtime remains in the codebase for future testing, but it is not the default MVP path. The learner must hear exactly the same text that is displayed, so Conversation Mode does not shorten, summarize, rewrite, or chunk spoken text.

## What is measured

Developer logs and usage records are intended to capture:

- lesson chat operation/model identifiers;
- input/output/total token counts when returned by the provider;
- cached input token counts when returned by the provider;
- transcription model, language, uploaded audio bytes, transcript length, and estimated audio duration;
- speech model, voice, purpose, input character count, output byte count, estimated duration, speed, and instruction presence;
- `gpt-realtime` session metrics when Realtime is explicitly used in future testing.

## What remains approximate

- Exact pricing is approximate or missing where pricing constants are not configured.
- Audio duration may be estimated from byte counts and sample rates.
- TTS duration may be approximate because compressed/container formats do not always map cleanly to duration.
- Realtime cost comparison is deferred because Realtime is not the default MVP path.
- Monthly and unit economics should be recalculated later from real usage logs.

## Conversation Mode cost note

Conversation Mode may cost more after switching from `tts-1` to `gpt-4o-mini-tts`, but quality improved because `gpt-4o-mini-tts` supports calmer instruction-based speech. Exact monthly and unit economics should be recalculated later from real usage logs instead of estimates alone.

## Log checks for smoke testing

During the regression smoke-test, confirm logs show:

- normal Lesson Chat speech uses `Model=tts-1` and `Purpose=lesson_chat_tts`;
- Conversation Mode speech uses `Model=gpt-4o-mini-tts` and `Purpose=conversation_mode_tts`;
- Conversation Mode speech uses `Voice=coral`, `SpeechSpeed=1.0`, and `HasInstructions=True`;
- no Realtime WebSocket opens by default.

## Future cost work

Before pricing, subscriptions, or usage limits are finalized:

1. Run representative test lessons across levels and topics.
2. Export or collect real usage logs.
3. Recalculate per-lesson, per-minute, and per-month costs.
4. Separate normal Lesson Chat, transcription, normal TTS, Conversation Mode TTS, and any future Realtime experiment.
5. Add missing pricing constants where appropriate.
6. Revisit usage limits and subscription tiers with measured data.
