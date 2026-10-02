# Cost and Usage Instrumentation Model

Review date: 2026-10-02.

This document records current product model usage and developer-only cost instrumentation. Official token prices below are current as checked on this date; actual cost estimates remain approximate where runtime pricing/accounting is incomplete.

## Current model usage

Lesson Tutor Chat, Feedback / correction, Lesson Hint, and Translation use `gpt-5.6-luna`, with all four omit-temperature flags enabled; `SpeechToTextModel=gpt-transcribe`; `LessonChatTextToSpeechModel=gpt-realtime-2.1-mini`; `ConversationModeTextToSpeechModel=gpt-realtime-2.1-mini`; `RealtimeVoiceModel=gpt-realtime` belongs to the dormant old full-Realtime path.

Read-only verification on 2026-10-02 found persistent Active and Draft model IDs and all four omit-temperature flags identical at revision `39`.

Lesson Summary intentionally follows the Lesson Tutor Chat model/temperature policy. Both TTS purposes render already-final visible tutor text; neither generates lesson content, corrections, or conversation decisions.

## Current product voice decision

`learner audio -> gpt-transcribe -> validated learner text -> gpt-5.6-luna lesson reply -> exact final visible tutor text -> gpt-realtime-2.1-mini speech rendering`

The speech renderer uses a short-lived backend Realtime WebSocket, with no learner microphone stream or lesson history for answer generation. Internal PCM is 24 kHz, backend non-streaming output remains WAV, streaming remains PCM, and product speech speed is fixed `1.0` with no numeric provider speed configuration. Full Realtime Conversation Mode is dormant and the project decision is not to return it.

## Current official Realtime-mini token pricing

Prices checked on 2026-10-02 from [GPT-Realtime-2.1 Mini model documentation](https://developers.openai.com/api/docs/models/gpt-realtime-2.1-mini), in USD per 1 million tokens:

| Token type | Input | Cached input | Output |
| --- | ---: | ---: | ---: |
| Text | $0.60 | $0.06 | $2.40 |
| Audio | $10.00 | $0.30 | $20.00 |

The renderer sends text and receives audio. Listing audio-input pricing does not mean learner audio is sent to this renderer. Use provider-returned usage by modality and cache status for accounting; do not convert these token prices to a precise per-minute rate without sourced token/time measurements.

OpenAI announced on 2026-10-01 that the listed deprecated text-to-speech models (`tts-1`, `tts-1-hd`, and the listed `gpt-4o-mini-tts` snapshots) are scheduled for removal on 2027-01-06 and recommends `gpt-realtime-2.1-mini`. Sources checked on 2026-10-02: [OpenAI deprecations](https://developers.openai.com/api/docs/deprecations) and [GPT-Realtime-2.1 Mini model documentation](https://developers.openai.com/api/docs/models/gpt-realtime-2.1-mini).

## What is measured

Developer logs and usage records capture operation/model/purpose identifiers; available input/output/total/cached token counts; transcription language, audio bytes, and estimated duration; speech voice, input character count, output bytes, estimated duration, instruction presence, and first-audio timing. Realtime-mini diagnostics include `Transport=realtime_speech`, `RequestedSpeed=1.0`, `NumericSpeedApplied=False`, and transcript-fidelity results. Returned Realtime usage includes available text/audio token detail fields; the current renderer's cost diagnostics still mark `CostEstimateApproximate=True` and `MissingCostFields=realtime_pricing`. Existing streaming accounting remains diagnostic-only; official prices in this document do not complete runtime cost accounting.

## What remains approximate

- Provider usage may omit token detail fields; input text length is not a token count.
- Audio duration may be estimated from PCM bytes and sample rate; WAV/container byte counts require format-aware accounting.
- The bounded smoke latency is not a cost/time conversion or a future latency guarantee.
- Dormant full-Realtime session economics are separate from the current final-text renderer.
- Monthly, per-lesson, and per-minute economics require representative measured usage, durations, and completed runtime pricing accounting.

## Historical Conversation Mode cost/pricing context

Before 2026-10-02, both TTS roles used `gpt-4o-mini-tts`. The earlier move from `tts-1` may have increased Conversation Mode cost while improving calm instruction-based delivery; that comparison is historical and is not the current renderer's measured unit economics. No historical numerical price is silently relabeled as a current Realtime-mini price.

## Current smoke log checks

- Both `lesson_chat_tts` and `conversation_mode_tts` use `Model=gpt-realtime-2.1-mini` and `Transport=realtime_speech`.
- Selected canonical voice reaches the renderer; David fallback is `cedar`, others `coral`.
- Effective speed is `1.0`; no numeric speed is added to Realtime payloads.
- Optional Conversation Mode instructions affect delivery only; transcript fidelity checks compare against final visible text.
- Short-lived server-to-server rendering sockets are expected; client `/api/realtime-voice` sessions remain outside the current product flow.

The 2026-10-02 smoke had 7/7 completions, 0 failed/canceled requests, and 0 transcript mismatches; this is bounded evidence, not universal reliability or cost validation.

## Future cost work

Collect representative lessons across levels, languages, and voices; retain safe provider usage and duration measurements; reconcile non-streaming and streaming accounting; then calculate unit economics and review usage limits. Any pricing-constant implementation, billing change, or production operation is separate from this documentation synchronization.
