# Voice and Realtime Review

Review date: 2026-10-05.

## Current chained product decision

Normal Lesson Chat and Conversation Mode share the existing transcription and lesson-reply flow:

`learner audio -> gpt-transcribe -> validated learner text -> gpt-5.6-luna lesson reply -> exact final visible tutor text -> gpt-realtime-2.1-mini speech rendering`

`gpt-realtime-2.1-mini` is a speech renderer only. It does not generate lesson content, learner corrections, or conversation decisions. The learner hears the exact final text displayed in chat; do not shorten, summarize, rewrite, or chunk it. The project decision is not to return full Realtime Conversation Mode.

## Current production models and ownership

Production backend `0.1.35-backend.166` is current at `/opt/languagevoicetutor/backend/releases/0.1.35-backend.166`; `/opt/languagevoicetutor/backend/releases/0.1.35-backend.165` is the verified rollback release. Accepted/deployed source commit is `d49a8eb039f3ec556057226d957853bda77daf2f`. The verified 2026-10-05 production checkpoint confirmed `languagevoicetutor-backend.service` active/running, the deployed process running from the `.166` release directory, public `/health` HTTP 200, and public `/api/health/database` HTTP 200. No EF migration or database schema/data migration was required for `.165` or `.166`.

Lesson Tutor Chat, Feedback / correction, Lesson Hint, and Translation use `gpt-5.6-luna`, with all four omit-temperature flags enabled; `SpeechToTextModel=gpt-transcribe`; `LessonChatTextToSpeechModel=gpt-realtime-2.1-mini`; `ConversationModeTextToSpeechModel=gpt-realtime-2.1-mini`; `RealtimeVoiceModel=gpt-realtime` belongs to the dormant old full-Realtime path.

Read-only verification on 2026-10-02 found persistent Active and Draft model IDs and all four omit-temperature flags identical at revision `39`. The verified 2026-10-05 `.165`/`.166` production checkpoint retained that configuration and revision unchanged.

The Admin AI Models TTS publication occurred on 2026-10-02 and is separate from backend `.164` deployment and Windows 1.8 publication. Desktop and Mobile call backend endpoints; provider model selection and OpenAI credentials remain backend-owned.

## Lesson Chat and Conversation Mode voice paths

1. The learner types or records a message. Recorded audio uses the normal backend transcription endpoint with `gpt-transcribe`.
2. Validated learner text enters the same lesson chat reply flow in both modes; `gpt-5.6-luna` generates the final tutor reply.
3. The reply is displayed in Lesson Chat or the Conversation Mode bubble and retained in the lesson transcript.
4. The exact displayed text reaches backend speech with `purpose=lesson_chat_tts` or `purpose=conversation_mode_tts`.
5. The backend renders speech with `gpt-realtime-2.1-mini`, and the client plays the returned audio.

Conversation Mode retains its avatar overlay, record/exit controls, latest user and tutor bubbles, and Hint. Optional calm, friendly study-language speech instructions affect delivery only. Normal Lesson Chat does not add those Conversation Mode speech-style instructions. Neither mode sends learner microphone audio to the speech renderer.

## Current Realtime-mini speech renderer

The backend opens a short-lived server-to-server Realtime WebSocket to render already-final tutor text. It sends no learner microphone stream or lesson conversation history to generate an answer; instructions require exact-text speech without additions, omissions, or paraphrasing. Internal output is PCM 24 kHz; the non-streaming backend contract remains WAV and the streaming contract remains PCM. No numeric speed property is sent in `session.update` or `response.create`; product speed is normal/default `1.0`, with `RequestedSpeed=1.0` and `NumericSpeedApplied=False`. The renderer does not generate lesson content, learner corrections, or conversation decisions. The project decision is not to return full Realtime Conversation Mode; the dormant `/api/realtime-voice` architecture remains separate.

The exact model ID selects the dedicated renderer. Legacy non-Realtime speech models retain `/v1/audio/speech` HTTP transport and speed `1.0`; that compatibility path does not describe the current active production TTS roles.

## Current `.166` bounded non-streaming retry contract — 2026-10-05

Non-streaming Realtime speech allows at most 3 total WebSocket attempts inside the same original 20-second overall speech budget; the budget is never reset. Attempt 1 retains an 8-second first-audio startup deadline. A zero-audio startup timeout may open Attempt 2, and a `WebSocketException` may open another attempt only when the request is non-streaming, another attempt remains, no first audio has been received, `PcmBytes=0`, client cancellation has not occurred, and the original overall timeout has not expired. Attempt 2 and Attempt 3 use only the remaining overall budget, with no independent 8-second startup deadline. No retry is allowed after audio/PCM starts, after client cancellation, or after overall timeout; no fourth attempt is possible. Streaming remains exactly one attempt with its existing timeout behavior. Socket cleanup completes before the next attempt. Structured retry reasons are `first_audio_startup_timeout` and `websocket_transport_failure`; retry logs do not expose transcript/provider response text or exception messages.

The exact final visible tutor text, model, voice, purpose, study language, speed behavior, WAV contract, transcript fidelity, and one application-level usage record remain unchanged. The chained lesson architecture and exact-text speech-rendering decision remain in force; full Realtime Conversation Mode remains dormant.

### Historical `.165` startup retry and production failure

`.165` (source commit `0725c5265e300c7da5a3dbb09db3f034dc4bf208`) added one bounded non-streaming retry for a first-audio startup stall: Attempt 1 had an 8-second deadline, and Attempt 2 was allowed only with zero PCM, no client cancellation, and time remaining in the original shared overall timeout. The retry reused that remaining budget rather than resetting it; streaming was unchanged. Accepted release verification recorded 62 focused `RealtimeSpeechSynthesisService` tests passed and a passing Release backend build.

On 2026-10-05, a real `lesson_chat_tts` request with 289 characters started Attempt 1 under `.165`. It produced zero PCM and reached `first_audio_startup_timeout` after approximately 8.13 seconds. `.165` correctly opened Attempt 2, but that attempt failed almost immediately with `WebSocketException` and zero PCM, so the request failed while time remained in the original overall budget. A fresh request for the same 289-character text shortly afterward succeeded on Attempt 1, with first audio in approximately 2.042 seconds and total completion in approximately 3.833 seconds. This sequence motivated the `.166` zero-audio transport retry.

### Accepted `.166` verification and bounded Android evidence

`.166` (source commit `d49a8eb039f3ec556057226d957853bda77daf2f`) extended this bounded non-streaming recovery to zero-audio WebSocket transport failures, with at most 3 attempts. Accepted release verification recorded 77/77 focused `RealtimeSpeechSynthesisService` tests passed, a passing Release backend build with 0 warnings/errors, and `git diff --check` passed. These are release-verification results, not checks rerun by this documentation update.

After `.166` deployment, physical Android lesson testing reported that the previously failing first-message voice flow worked correctly. This is bounded production evidence; it does not guarantee every future request or exclude provider failures.

## Voice catalog and legacy settings compatibility

The canonical selectable voice catalog is exactly `alloy`, `ash`, `ballad`, `coral`, `echo`, `sage`, `shimmer`, `verse`, `marin`, and `cedar`. `nova`, `onyx`, and `fable` are trimmed, case-insensitive legacy settings-input compatibility IDs only, never selectable canonical voices. Canonical IDs are stored lowercase; legacy input uses the effective tutor after any same-request tutor change: David -> `cedar`, every other tutor -> `coral`. Blank and arbitrary unknown settings-input voices remain rejected. UserSettings persist/return speech speed `1.0` and `ConversationModeEnabled=true`; legacy API/storage fields remain. Historical incoming speeds in the existing 0.5–2.0 range remain accepted but normalize to `1.0`. Load repairs voice/speed/flag together, preserves `CreatedAt`, updates `UpdatedAt` only when repair is needed, and leaves a subsequent unchanged load untouched. Both `/api/audio/speech` and `/api/audio/speech-stream` enforce request/provider speed `1.0` regardless of stale caller values.

The permanently true legacy settings field does not restore any runtime dependency on stored false values or change Lesson Chat/Conversation Mode navigation.

## Dormant old full-Realtime architecture

The old desktop WebSocket engine, microphone capture, backend `/api/realtime-voice` gateway, GA schema, recovery tests, and diagnostics remain in the repository. Its `RealtimeVoiceModel=gpt-realtime` field belongs to that dormant product path. It historically handled learner audio and assistant conversation generation within one Realtime session. It is separate from the current short-lived server-to-server speech renderer and is not the current lesson/conversation architecture. The current client flow should not open `/api/realtime-voice`.

## Historical bounded production smoke — 2026-10-02

The bounded 2026-10-02 production smoke after the Admin AI Models publication recorded 7 Realtime-mini speech starts and 7 completions, 0 failed/canceled requests, and 0 transcript-fidelity mismatches (`TranscriptMatches=True` for the verified samples). It included 5 `lesson_chat_tts` and 2 `conversation_mode_tts` completions, voices `coral`, `cedar`, and `marin`, and approximately 1.2–2.2 seconds to first audio. Backend remained `.164`, the service active, health Healthy, and database Healthy with `canConnect=true`. This sample does not guarantee future request success or every voice/language combination.

## Feedback, hint, transcript, and study-language behavior

- Feedback remains tied to the clicked message through `sourceMessageId` and `sourceMessageKind`; context-selection feedback remains phrase-level.
- Conversation Mode transcript messages remain reviewable after returning to Lesson Chat; Hint works in both views.
- Invalid retry/status messages remain excluded from learner-turn counts and summary input.
- Study languages remain English, French, German, Portuguese, Spanish, and Italian. Transcription uses the selected language code; safe logs record language IDs without audio content or secrets.
- Translation remains a separate review feature; the speech renderer does not make translation or lesson decisions.

## Deprecation and cost references

OpenAI announced on 2026-10-01 that the listed deprecated text-to-speech models (`tts-1`, `tts-1-hd`, and the listed `gpt-4o-mini-tts` snapshots) are scheduled for removal on 2027-01-06 and recommends `gpt-realtime-2.1-mini`. Sources checked on 2026-10-02: [OpenAI deprecations](https://developers.openai.com/api/docs/deprecations) and [GPT-Realtime-2.1 Mini model documentation](https://developers.openai.com/api/docs/models/gpt-realtime-2.1-mini).

Current token pricing and measurement limits are recorded in [Cost Model](COST_MODEL.md); no precise per-minute rendering cost is claimed from this smoke.

## Historical 2026-09-29 TTS decision

At that checkpoint both TTS roles used `gpt-4o-mini-tts`; transcription, lesson reply generation, and speech playback were already chained, while full Realtime Conversation Mode was outside the default product path. The earlier `tts-1` -> `gpt-4o-mini-tts` switch supported calmer instruction-based delivery. The 2026-10-02 change replaces only the final speech renderer and preserves that chained product decision.
