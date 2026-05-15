# Cost and Usage Instrumentation Model

Review date: 2026-05-14.

This document describes the developer-only instrumentation used to compare normal Lesson Chat, chained voice fallback, normal `tts-1` playback, and Realtime Conversation Mode with `gpt-realtime`.

## What is measured exactly

- Normal typed Lesson Chat logs the Responses API model, response id, input tokens, output tokens, total tokens, cached input tokens, and audio token fields when OpenAI returns them.
- Feedback requests use the same Responses API path and log the same fields with `Operation=feedback`.
- Normal transcription logs uploaded audio bytes, transcription model, language, transcript length, and an estimated WAV duration.
- Normal TTS logs model `tts-1`, voice, output format, input character count, output byte count, and an estimated duration where possible.
- Realtime logs session id, model `gpt-realtime`, voice, input transcription model, language, input audio bytes, committed audio bytes, commit count, user transcript characters, assistant transcript characters, assistant audio bytes, first-audio timing, response-complete timing, disconnect reason, and any Realtime usage tokens present on `response.done` events.

## What is estimated

- Audio duration is estimated from PCM/WAV byte counts and the configured sample rates.
- TTS duration is approximate because compressed or containerized formats may not map perfectly to bytes.
- Realtime audio duration is approximate because it is based on relayed PCM byte counts.
- Cost is marked approximate/incomplete while pricing constants are zero.

## Pricing constants

Pricing values live in `PricingConstants.OpenAi` in the backend usage metrics model. They are intentionally `0` placeholders. Update them manually from the OpenAI pricing page before treating cost estimates as exact.

Fields:

- `TranscriptionPerMinuteUsd`
- `Tts1PerMillionCharactersUsd`
- `RealtimeTextInputPerMillionTokensUsd`
- `RealtimeTextOutputPerMillionTokensUsd`
- `RealtimeAudioInputPerMillionTokensUsd`
- `RealtimeAudioOutputPerMillionTokensUsd`
- `ChatTextInputPerMillionTokensUsd`
- `ChatTextOutputPerMillionTokensUsd`

## Comparing normal chat versus Realtime

For a normal lesson, add:

1. typed Lesson Chat Responses API usage;
2. feedback Responses API usage;
3. chained transcription usage for voice turns;
4. `tts-1` usage for manual or auto Play voice.

For Realtime Conversation Mode, add:

1. `gpt-realtime` session audio/text usage when exact usage is returned;
2. raw input/output audio byte duration estimates when exact usage is missing;
3. any final feedback or summary calls made outside the Realtime session.

Realtime is expected to cost more than normal typed chat because it can include bidirectional audio processing, lower-latency streaming, and audio output tokens/bytes. It can still be a better user experience because it removes the separate record/transcribe/send/play chain.

## Why normal Lesson Chat TTS remains `tts-1`

This stabilization task does not change runtime model choices. Normal Lesson Chat TTS remains `tts-1` so cost comparisons isolate instrumentation and state recovery changes from model changes.

## Developer-only output

Usage summaries are emitted as structured backend logs with `Developer usage summary`. They are not shown to end users and do not contain API keys, authorization headers, raw audio, or full sensitive payloads.

## 2026-05-15 Realtime GA usage note

Realtime usage logging remains attached to `gpt-realtime` responses and sessions after the GA `/v1/realtime` migration. The backend still records session id, model, voice, English transcription model/language, input/output audio byte estimates, transcript characters, disconnect reason, and exact response usage fields when the GA `response.done.response.usage` payload provides them. Normal Lesson Chat TTS remains `tts-1`.


## 2026-05-15 Realtime GA schema and recovery note

Realtime usage instrumentation remains unchanged while the GA session schema is corrected. The configured session update must include `audio.input.format: { type: "audio/pcm", rate: 24000 }` and `audio.output.format: { type: "audio/pcm", rate: 24000 }`; `audio.output.format.rate` is required before OpenAI accepts the session. A missing required schema parameter is treated as a failed startup, not a billable successful lesson turn, and cleanup still emits the `Developer usage summary: Operation=realtime_session` line with approximate byte/duration counters and centralized placeholder pricing fields.

## 2026-05-15 fallback transcription cost note

Realtime transcript recovery can add a one-shot fallback transcription call only after Realtime user transcription fails or times out and buffered committed audio exists. The fallback uses the same normal transcription endpoint/model (`gpt-4o-mini-transcribe`, English) as Lesson Chat voice input. It is not used on successful Realtime transcripts, does not generate assistant speech through `tts-1`, and does not create duplicate learner turns or duplicate Realtime assistant responses. Fallback attempts log usage-oriented metadata (audio chunks, bytes, estimated duration, model, language, and result validity) so extra cost can be measured separately from successful Realtime turns.
