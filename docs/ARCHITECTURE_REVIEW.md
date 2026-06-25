# Architecture Review

Review date: 2026-05-16.

This review documents current architecture boundaries after the stabilization fixes. It recommends small future extractions only after behavior is pinned by smoke tests; it does not recommend a rewrite.

## Current desktop architecture

The desktop app is a WPF/MVVM application using CommunityToolkit.Mvvm. `MainViewModel` constructs long-lived services, owns current screen selection, and passes navigation callbacks into child ViewModels. Views are thin XAML/code-behind controls.

Important desktop directories:

- `Constants/`: app, audio, backend, avatar, content, and storage constants.
- `Content/Lessons/`: JSON lesson catalog and scenario/methodology metadata.
- `Content/Tutors/`: tutor profile JSON such as Lana.
- `Models/`: desktop DTOs and UI/domain models.
- `Models/LessonContent/`: strongly typed lesson JSON schema models.
- `Services/`: local storage, content loading, HTTP backend, audio recording/playback.
- `Services/Voice/`: realtime voice desktop engine, microphone capture, PCM playback.
- `ViewModels/`: screen state and command logic.
- `Views/`: WPF user controls.
- `tools/`: lesson content and policy regression scripts.

## Current backend architecture

The backend is an ASP.NET Core minimal API. `Program.cs` maps endpoints directly and delegates behavior to services. OpenAI model/route constants are centralized in `Constants/`. DTOs live in `Models/`, including `Models/RealtimeVoice/` for realtime session startup.

Important backend directories:

- `backend/EnglishVoiceTutor.Api/Constants/`: route, OpenAI model, timeout, and content-type constants.
- `backend/EnglishVoiceTutor.Api/Models/`: request/response DTOs for chat, feedback, audio, translation, config, and realtime.
- `backend/EnglishVoiceTutor.Api/Services/`: OpenAI chat/hint/audio/realtime integrations, prompt construction, tutor profiles, and fallback mock services.
- `backend/EnglishVoiceTutor.Api/Program.cs`: dependency registration and endpoint mapping.

## Dependency map

High-level runtime dependency flow:

```text
WPF View -> ViewModel -> Desktop Service -> Backend endpoint -> Backend Service -> OpenAI API
```

Normal typed/chained Lesson Chat:

```text
LessonChatViewModel -> LessonChatBackendService -> /api/lesson-chat/reply
AudioRecordingService -> LessonChatBackendService -> /api/audio/transcribe
LessonChatViewModel -> LessonChatBackendService -> /api/audio/speech -> AudioPlaybackService
```

Realtime Conversation Mode:

```text
LessonChatViewModel -> RealtimeVoiceConversationEngine -> /api/realtime-voice
backend RealtimeVoiceSessionService -> OpenAI Realtime WebSocket (gpt-realtime)
RealtimeVoiceConversationEngine -> RealtimeAudioPlaybackService
RealtimeMicrophoneCaptureService -> RealtimeVoiceConversationEngine
```

## Current ownership boundaries

- Navigation: `MainViewModel` owns screen navigation and constructs child ViewModels.
- Lesson catalog/content loading: runtime lesson content should come from the active coherent source. CMS published snapshot is the normal controlled tester learner source when active and valid; packaged static JSON is initialization/local-development/emergency fallback only. Audit rules live in `tools/`.
- Lesson scenario data: CMS scenario content owns editable topic/subtopic wording, controlled context variants, scenario flow, roleplay beats, scenario-specific behavior, and scenario-specific wrap-up/final wording when CMS runtime is active. Static lesson JSON remains seed/fallback content. Level profiles own lesson length and wrap-up/final turn timing.
- Level rules: CMS level profiles own A1/A2/B1/B2 complexity, sentence length, depth, feedback strictness, and wrap/final timing thresholds. Prompt templates must not define numeric wrap/final timing.
- Tutor identity: approved desktop tutor avatar profiles and backend guardrails own source identity. CMS tutor behavior profiles own editable personality/style wording without changing the selected tutor identity. Scenarios must remain avatar-neutral.
- Prompt ownership: CMS prompt templates and tutor/scenario/level content own normal editable tutor behavior. `LessonPromptBuilder` owns assembly order, runtime phase contract, source coherence, target-language lock, tutor identity guardrails, structured response format, final-state guardrails, diagnostics, and fallback/init mechanics.
- Lesson state: mostly `LessonChatViewModel`, including phase, turn counts, setup context selection, completion, and button state.
- Audio recording: `AudioRecordingService` owns file-based recording for chained voice; `RealtimeMicrophoneCaptureService` owns realtime PCM capture.
- Bot voice playback: `LessonChatViewModel` orchestrates manual/auto-play; `LessonChatBackendService` requests speech; `AudioPlaybackService` saves/plays files; `AudioSpeechService` generates backend speech.
- Realtime: `LessonChatViewModel` decides when to start/stop; `RealtimeVoiceConversationEngine` owns desktop WebSocket; `RealtimeVoiceSessionService` owns backend gateway and OpenAI Realtime session; `RealtimeAudioPlaybackService` owns PCM playback.

## Teaching policy vs audio transport

Normal Lesson Chat and Realtime share CMS-first assembled lesson behavior plus backend guardrails. The backend assembles the active runtime content source and enforces non-editable protections; normal wording/style changes should be made in CMS. They differ in audio transport:

- Normal Lesson Chat uses `/api/lesson-chat/reply` for text and `/api/audio/speech` for manual Play, auto-play, and chained TTS fallback. Normal TTS currently uses `tts-1`.
- Realtime uses `/api/realtime-voice` and OpenAI Realtime with `gpt-realtime`. Realtime assistant audio and transcript must come from the same Realtime response and generated Realtime turns must not use `/api/audio/speech`.

## Message review vs lesson input state

Awaiting Finish deliberately separates message review from new lesson input. After the final tutor message:

- New lesson input is disabled: Send, Start recording, Hint, Back, and Conversation Mode should not continue the lesson.
- Existing-message review remains enabled until Finish lesson: View feedback for valid learner messages, Translate existing messages, and Play voice for existing bot messages.

This separation is represented by review-oriented command checks such as `CanReviewExistingMessages`, not by reopening lesson input.

## `LessonChatViewModel.cs` responsibilities

`ViewModels/LessonChatViewModel.cs` remains the center of the lesson experience. It handles:

- chat message collection and selected feedback.
- setup/context selection and active roleplay transitions.
- learner turn counting, soft wrap-up, final message, Awaiting Finish, and finish navigation.
- text send and backend request DTO construction.
- audio recording, transcription, validation, and auto-send.
- manual bot voice, auto-play, segment cache, prefetch, cancellation, and playback state.
- Realtime Conversation Mode start/stop, event handlers, transcript replacement, microphone capture, and playback.
- command can-execute logic for Send, Record, Hint, Finish, Back, Play Voice, Translate, View Feedback, and Conversation Mode.
- avatar/status state.

This file still owns many responsibilities and remains a future extraction candidate.

## Why `LessonChatViewModel.cs` is risky

The file is large and mixes product state, UI command state, fallback voice, realtime voice, backend DTO construction, and cache/cancellation mechanics. Small changes can unintentionally affect multiple behaviors, especially:

- Finish lesson button enablement.
- Conversation Mode button state.
- setup-stage vs active-roleplay phase checks.
- exact visible text vs spoken text.
- transcript validation and invalid retry-message exclusion.
- realtime cleanup vs fallback playback cancellation.
- final learner-turn enforcement and Awaiting Finish review actions.
- whole-lesson summary input.

## Recommended future extractions

Extract one boundary at a time after the manual smoke checklist and policy tests are passing:

- `LessonPhaseStateMachine` / `LessonTurnPolicy`: own setup, active roleplay, wrap-up, final, Awaiting Finish transitions and learner turn counting.
- `LessonCommandStateService`: compute Send, Record, Hint, Back, Finish, Conversation Mode, View Feedback, Translate, and Play Voice enablement from phase and busy flags.
- `BotVoicePlaybackCoordinator`: own manual Play, auto-play, exact-text validation, prefetch, cancellation, and playback logs.
- `ChainedVoiceInputCoordinator`: own record/transcribe/validate/auto-send flow.
- `ConversationModeVoiceCoordinator`: own default TTS-provider Conversation Mode state, transcript handling, exact visible-text playback, and cleanup; keep Realtime-specific coordination separate for future provider-switch work.
- `LessonBackendRequestFactory`: build chat, hint, feedback, summary/recent-message, and realtime request DTOs from lesson state.
- `LessonPromptPolicy` / `LevelRulePolicy`: only if backend-owned guardrails or assembly boundaries become hard to maintain in `LessonPromptBuilder`; do not move normal editable tutor behavior out of CMS or duplicate numeric timing in prompt templates.

## What should NOT be changed immediately

- Do not rewrite the whole `LessonChatViewModel`.
- Do not redesign Realtime transport before long-session and latency measurements.
- Do not change OpenAI model choices during the documentation update.
- Do not make Realtime the default product Conversation Mode provider.
- Do not hardcode Lana or any tutor identity in lesson JSON.
- Do not put A1-only rules directly into each scenario JSON unless documenting current `levelProfiles` behavior.
- Do not add subscriptions, avatar expansion, broad UI polish, or all-lesson JSON migration until smoke tests are reliable.

## Current architectural risks

- Single large ViewModel creates high regression risk.
- Command can-execute attributes and manual `RefreshAllCommandStates()` are easy to miss.
- Lesson phase and button state are coupled but not fully represented as a formal state machine.
- Realtime and default TTS-provider Conversation Mode share some UI flags but use different lifecycles.
- Exact spoken text is protected by code/logging, but voice playback coordination remains in the ViewModel.
- Realtime is non-default for product and still needs future long-session/provider-switch testing before it can be reconsidered.
- Mock fallback services are present and useful for degraded operation, but must be clearly distinguished from production OpenAI paths.

## 2026-05-16 codebase inventory

- `Views/`: WPF user controls and light code-behind. Stable, no obvious Realtime/TTS obsolete artifacts found; avatar tooltip cleanup remains in place.
- `ViewModels/`: screen state and commands. Stable but highest-risk area because `LessonChatViewModel` still owns chat, phase, feedback, translation, normal voice, Realtime, summary, and navigation lifecycle logic. No safe wholesale extraction was made.
- `Services/`: desktop content, backend, recording, playback, settings, cleanup, and history services. Stable; temporary-file cleanup and TTS playback are current behavior, not obsolete artifacts.
- `Services/Voice/`: Realtime desktop WebSocket, microphone capture, and PCM playback services. Stable after GA schema work; no beta transport artifact was identified in runtime code.
- `Shared/LessonPolicies/`: shared turn/transcript validation policy. Stable and protected by policy tests; transcript validation must not be weakened.
- `Models/`: desktop DTOs, UI state, and lesson content schema models. Stable; no unused request/response model was removed because current compiler coverage is unavailable in this Linux container.
- `Content/Lessons/`: 26 lesson JSON files. Stable by audit, but scenario QA is still a product/content task.
- `Content/Tutors/`: tutor profile JSON. Stable; tutor identity remains separate from lesson JSON.
- `backend/EnglishVoiceTutor.Api/`: minimal API backend, OpenAI services, Realtime gateway, usage/cost models, and endpoint DTOs. Stable; mock endpoint remains intentionally available for compatibility/testing and is separate from normal lesson flow.
- `tools/`: static policy tests and lesson audit scripts. Stable; tests assert current GA Realtime, routing, language-lock, feedback/summary, usage/cost, avatar, and diagnostics policies.
- `docs/`: stabilization, architecture, flow, voice, cost, release, and checklist documentation. Refreshed to describe the current working baseline and remaining limitations.

Larger extraction opportunities should remain queued until the full manual smoke-test matrix passes across product topics.
