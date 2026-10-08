# Voice and Realtime Review

Review date: 2026-10-07.

## Current chained product decision

Normal Lesson Chat and Conversation Mode share the existing transcription and lesson-reply flow:

`learner audio -> gpt-transcribe -> validated learner text -> gpt-5.6-luna lesson reply -> exact final visible tutor text -> gpt-realtime-2.1-mini speech rendering`

`gpt-realtime-2.1-mini` is a speech renderer only. It does not generate lesson content, learner corrections, or conversation decisions. The required behavior is to speak the exact final text displayed in chat without shortening, summarizing, rewriting, or chunking it; the observed transcript mismatch remains an unresolved fidelity defect below. The project decision is not to return full Realtime Conversation Mode.

## Current production models and ownership

Production backend `0.1.35-backend.167` is current at `/opt/languagevoicetutor/backend/releases/0.1.35-backend.167`; `/opt/languagevoicetutor/backend/releases/0.1.35-backend.166` is the verified rollback release. Accepted/deployed source commit is `959a3dffe2fde97c2b92717665291b6afbc909ea` (`Improve cold-start AI recovery`). After deployment, `languagevoicetutor-backend.service` was active/running from the `.167` release directory; public `/health` returned HTTP 200 `Healthy`, and public `/api/health/database` returned HTTP 200 `Healthy` with `canConnect=true`. `.166` remains the rollback target.

The reviewed `.167` package SHA-256 is `D2CFAD52A8D6DA0D0161409CA69E9C1A2E37E6B4E7D55123A69B9A0942646CB3`; the uploaded package hash matched the reviewed local package. No EF migration, database schema/data migration, AI model publication, production secret/configuration change, Mobile artifact, or Desktop artifact was required by `.167`.

Lesson Tutor Chat, Feedback / correction, Lesson Hint, and Translation use `gpt-5.6-luna`, with all four omit-temperature flags enabled; `SpeechToTextModel=gpt-transcribe`; `LessonChatTextToSpeechModel=gpt-realtime-2.1-mini`; `ConversationModeTextToSpeechModel=gpt-realtime-2.1-mini`; `RealtimeVoiceModel=gpt-realtime` belongs to the dormant old full-Realtime path.

Read-only verification on 2026-10-02 found persistent Active and Draft model IDs and all four omit-temperature flags identical at revision `39`. The `.167` production checkpoint retains that configuration and revision unchanged.

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

## Current `.167` bounded non-streaming retry contract — 2026-10-07

Non-streaming Realtime speech retains one shared 20-second overall budget and at most 3 total WebSocket attempts; the budget is never reset. Attempt 1 has an 8-second zero-audio startup deadline; Attempt 2 has a 6-second zero-audio startup deadline. Attempt 3 has no independent startup deadline or new budget and can use only the time remaining from the original shared 20-second budget. Once first audio/PCM is received, that attempt's startup deadline is disabled and the response may continue under the original overall budget. Startup-timeout and existing safe `WebSocketException` transport recovery require zero audio/PCM, another attempt remaining, no client cancellation, and unexpired overall time. No retry occurs after audio starts, no fourth attempt is possible, and client cancellation and the original overall timeout remain authoritative. Streaming remains single-attempt behavior and was not changed. Socket cleanup completes before the next attempt; structured retry reasons remain `first_audio_startup_timeout` and `websocket_transport_failure`, without logging user content or secrets.

The requested final visible tutor text, model, voice, purpose, study language, speed behavior, WAV contract, and one application-level usage record remain unchanged. `.167` does not resolve the transcript-fidelity mismatch. The chained lesson architecture and exact-text speech-rendering decision remain in force; full Realtime Conversation Mode remains dormant.

## Current `.167` Lesson Hint provider deadline

OpenAI Lesson Hint provider work now has a backend-owned 7-second deadline. When that provider deadline is reached, the backend returns the existing deterministic language-aware `MockLessonHintService` fallback instead of waiting tens of seconds. Normal provider responses before the deadline remain unchanged, and ordinary provider failures still use the existing fallback. Client cancellation propagates separately and is not a provider-timeout fallback. The public Lesson Hint route and response contract, model selection, prompt construction, schema, temperature policy, and language-aware fallback content remain unchanged. Safe diagnostics distinguish provider success, internal provider timeout/fallback, and other provider failure/fallback without logging user content or secrets.

## Accepted `.167` verification and bounded post-idle Android evidence

Accepted `.167` automated release verification: the focused command covering `RealtimeSpeechSynthesisServiceTests` and Lesson Hint tests passed 112 tests; the backend Release build passed with 0 warnings and 0 errors; `git diff --check`, backend Linux deployment policy, package build, and upload dry-run passed before deployment. These are accepted release results, not application checks rerun by this documentation update.

Immediate post-deploy Android smoke succeeded across multiple study languages. After a genuine several-hour idle period, the user opened the application/lesson again and the first flow started normally; the previously observed cold/idle startup failure did not reproduce in that post-idle test. This is bounded real production evidence, not a guarantee against another external-provider latency or outage incident; no broader external-user or Play-installed v13 smoke is inferred.

## Open transcript-fidelity follow-up

Transcript fidelity remains unresolved and is the next separate bounded backend voice reliability follow-up. Previously captured production evidence showed a completed speech request with `InputCharacters=318`, `TranscriptCharacters=318`, and `TranscriptMatches=False`. Current behavior can still log that mismatch and return the WAV successfully; `.167` does not fix it. No fix design or future release number is selected.

## Historical cold/idle evidence motivating `.167`

Under `.166`, after several hours of low/no use, Attempt 1 could reach its 8-second TTS startup deadline and Attempt 2 could consume essentially all remaining shared time, producing a 504 before Attempt 3 had a meaningful opportunity. During the same idle period, the first Lesson Hint provider request took roughly 39 seconds against Mobile's normal API budget of about 10 seconds; subsequent provider calls were fast. The backend service had not restarted. The exact external-provider cause was not isolated; this does not establish an OpenAI model cold start, DNS, TLS, or network root cause.

Under `.166`, only Attempt 1 had an 8-second startup deadline; Attempts 2 and 3 used the remaining original overall budget without independent startup deadlines. `.167` adds the Attempt 2 6-second deadline while retaining safe zero-audio transport recovery.

### Historical `.165` startup retry and production failure

`.165` (source commit `0725c5265e300c7da5a3dbb09db3f034dc4bf208`) added one bounded non-streaming retry for a first-audio startup stall: Attempt 1 had an 8-second deadline, and Attempt 2 was allowed only with zero PCM, no client cancellation, and time remaining in the original shared overall timeout. The retry reused that remaining budget rather than resetting it; streaming was unchanged. Accepted release verification recorded 62 focused `RealtimeSpeechSynthesisService` tests passed and a passing Release backend build.

On 2026-10-05, a real `lesson_chat_tts` request with 289 characters started Attempt 1 under `.165`. It produced zero PCM and reached `first_audio_startup_timeout` after approximately 8.13 seconds. `.165` correctly opened Attempt 2, but that attempt failed almost immediately with `WebSocketException` and zero PCM, so the request failed while time remained in the original overall budget. A fresh request for the same 289-character text shortly afterward succeeded on Attempt 1, with first audio in approximately 2.042 seconds and total completion in approximately 3.833 seconds. This sequence motivated the `.166` zero-audio transport retry.

### Historical accepted `.166` verification and bounded Android evidence

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
