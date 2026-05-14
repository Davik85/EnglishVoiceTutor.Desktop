# Architecture Review

Review date: 2026-05-14.

This review documents current architecture boundaries after the stabilization fixes. It recommends small future extractions only after behavior is pinned by smoke tests; it does not recommend a rewrite.

## Current desktop architecture

The desktop app is a WPF/MVVM application using CommunityToolkit.Mvvm. `MainViewModel` constructs long-lived services, owns current screen selection, and passes navigation callbacks into child ViewModels. Views are thin XAML/code-behind controls.

Important desktop directories:

- `Constants/`: app, audio, backend, avatar, content, and storage constants.
- `Content/Lessons/`: JSON lesson catalog and scenario/methodology metadata.
- `Content/Tutors/`: tutor profile JSON such as Elena.
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
- Lesson catalog/content loading: `LessonContentService` owns JSON loading; audit rules live in `tools/`.
- Lesson scenario data: lesson JSON owns topic, subtopic, goal, target language, controlled context variants, scenario flow, roleplay beats, wrap-up, and final behavior.
- Level rules: lesson `levelProfiles` and prompt/turn metadata own A1/A2/B1/B2 complexity, sentence length, depth, and feedback strictness.
- Tutor identity: `TutorProfile`/`TutorAvatarProfileProvider` owns tutor name, personality, background, voice/tone, and identity details. Lesson JSON must remain avatar-neutral.
- Prompt policy: `LessonPromptBuilder` owns the shared canonical tutor policy for normal Lesson Chat and Realtime.
- Lesson state: mostly `LessonChatViewModel`, including phase, turn counts, setup context selection, completion, and button state.
- Audio recording: `AudioRecordingService` owns file-based recording for chained voice; `RealtimeMicrophoneCaptureService` owns realtime PCM capture.
- Bot voice playback: `LessonChatViewModel` orchestrates manual/auto-play; `LessonChatBackendService` requests speech; `AudioPlaybackService` saves/plays files; `AudioSpeechService` generates backend speech.
- Realtime: `LessonChatViewModel` decides when to start/stop; `RealtimeVoiceConversationEngine` owns desktop WebSocket; `RealtimeVoiceSessionService` owns backend gateway and OpenAI Realtime session; `RealtimeAudioPlaybackService` owns PCM playback.

## Teaching policy vs audio transport

Normal Lesson Chat and Realtime share teaching behavior through the canonical policy in `LessonPromptBuilder`. They differ in audio transport:

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
- `BotVoicePlaybackCoordinator`: own manual Play, auto-play, exact-text validation, segmentation, prefetch, cancellation, and playback logs.
- `ChainedVoiceInputCoordinator`: own record/transcribe/validate/auto-send flow.
- `RealtimeConversationCoordinator`: own Conversation Mode state, deferred guided start, transcript replacement, realtime event handlers, and cleanup.
- `LessonBackendRequestFactory`: build chat, hint, feedback, summary/recent-message, and realtime request DTOs from lesson state.
- `LessonPromptPolicy` / `LevelRulePolicy`: only if the current prompt/level boundaries become hard to maintain in `LessonPromptBuilder`; do not duplicate policy in lesson JSON.

## What should NOT be changed immediately

- Do not rewrite the whole `LessonChatViewModel`.
- Do not redesign Realtime transport before long-session and latency measurements.
- Do not change OpenAI model choices during stabilization.
- Do not route generated Realtime turns through `/api/audio/speech`.
- Do not hardcode Elena or any tutor identity in lesson JSON.
- Do not put A1-only rules directly into each scenario JSON unless documenting current `levelProfiles` behavior.
- Do not add subscriptions, avatar expansion, broad UI polish, or all-lesson JSON migration until smoke tests are reliable.

## Current architectural risks

- Single large ViewModel creates high regression risk.
- Command can-execute attributes and manual `RefreshAllCommandStates()` are easy to miss.
- Lesson phase and button state are coupled but not fully represented as a formal state machine.
- Realtime and chained voice share UI flags but use different lifecycles.
- Exact spoken text is protected by code/logging, but normalization and segmentation remain in the ViewModel.
- Realtime expected disconnects are improved, but long-session behavior still needs testing.
- Mock fallback services are present and useful for degraded operation, but must be clearly distinguished from production OpenAI paths.
