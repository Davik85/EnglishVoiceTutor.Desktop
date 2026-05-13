# Architecture Review

Review date: 2026-05-13.

## Current desktop architecture

The desktop app is a WPF/MVVM application using CommunityToolkit.Mvvm. `MainViewModel` constructs long-lived services, owns current screen selection, and passes navigation callbacks into child ViewModels. Views are thin XAML/code-behind controls. Most product behavior is in ViewModels and services.

Important desktop directories:

- `Constants/`: app, audio, backend, avatar, content, and storage constants.
- `Content/Lessons/`: JSON lesson catalog and lesson methodology metadata.
- `Models/`: desktop DTOs and UI/domain models.
- `Models/LessonContent/`: strongly typed lesson JSON schema models.
- `Services/`: local storage, content loading, HTTP backend, audio recording/playback.
- `Services/Voice/`: realtime voice desktop engine, microphone capture, PCM playback.
- `ViewModels/`: screen state and command logic.
- `Views/`: WPF user controls.
- `tools/`: lesson content audit scripts.

## Current backend architecture

The backend is an ASP.NET Core minimal API. `Program.cs` maps endpoints directly and delegates behavior to scoped/singleton services. OpenAI model/route constants are centralized in `Constants/`. DTOs live in `Models/`, including `Models/RealtimeVoice/` for realtime session startup.

Important backend directories:

- `backend/EnglishVoiceTutor.Api/Constants/`: route, OpenAI model, timeout, content-type constants.
- `backend/EnglishVoiceTutor.Api/Models/`: request/response DTOs for chat, audio, translation, config, and realtime.
- `backend/EnglishVoiceTutor.Api/Services/`: OpenAI chat/hint/audio/realtime integrations and fallback mock services.
- `backend/EnglishVoiceTutor.Api/Program.cs`: dependency registration and endpoint mapping.

## Dependency map

High-level runtime dependency flow:

```text
WPF View -> ViewModel -> Desktop Service -> Backend endpoint -> Backend Service -> OpenAI API
```

Voice-specific flows:

```text
Chained fallback:
AudioRecordingService -> LessonChatBackendService -> /api/audio/transcribe
LessonChatViewModel -> LessonChatBackendService -> /api/lesson-chat/reply
LessonChatViewModel -> LessonChatBackendService -> /api/audio/speech -> AudioPlaybackService

Realtime:
LessonChatViewModel -> RealtimeVoiceConversationEngine -> /api/realtime-voice
backend RealtimeVoiceSessionService -> OpenAI Realtime WebSocket
RealtimeVoiceConversationEngine -> RealtimeAudioPlaybackService
RealtimeMicrophoneCaptureService -> RealtimeVoiceConversationEngine
```

## Ownership today

- Navigation: `MainViewModel` owns screen navigation and constructs child ViewModels.
- Lesson catalog/content loading: `LessonContentService` owns JSON loading; audit rules live in `tools/`.
- Lesson state: mostly `LessonChatViewModel`, including phase, turn counts, setup context selection, completion, and button state.
- Audio recording: `AudioRecordingService` owns file-based recording for chained voice; `RealtimeMicrophoneCaptureService` owns realtime PCM capture.
- Bot voice playback: `LessonChatViewModel` orchestrates manual/auto-play; `LessonChatBackendService` requests speech; `AudioPlaybackService` saves/plays files; `AudioSpeechService` generates backend speech.
- Realtime: `LessonChatViewModel` decides when to start/stop; `RealtimeVoiceConversationEngine` owns desktop WebSocket; `RealtimeVoiceSessionService` owns backend gateway and OpenAI Realtime session; `RealtimeAudioPlaybackService` owns PCM playback.

## `LessonChatViewModel.cs` responsibilities

`ViewModels/LessonChatViewModel.cs` is currently the center of the lesson experience. It handles:

- chat message collection and selected feedback.
- setup/context selection and active roleplay transitions.
- learner turn counting, soft wrap-up, final message, and finish state.
- text send and backend request DTO construction.
- audio recording, transcription, and auto-send.
- manual bot voice, auto-play, segment cache, prefetch, cancellation, and playback state.
- Realtime Conversation Mode start/stop, event handlers, transcript accumulation, microphone capture, and playback.
- command can-execute logic for Send, Record, Hint, Finish, Back, Play Voice, and Conversation Mode.
- avatar/status state.

## Why `LessonChatViewModel.cs` is risky

The file is more than 2,500 lines and mixes product state, UI command state, fallback voice, realtime voice, backend DTO construction, and cache/cancellation mechanics. Small changes can unintentionally affect multiple behaviors, especially:

- Finish lesson button enablement.
- Conversation Mode button state.
- setup-stage vs active-roleplay phase checks.
- exact visible text vs spoken text.
- realtime cleanup vs fallback playback cancellation.
- final learner-turn enforcement.

## What should eventually be extracted

Recommended future boundaries:

- `LessonPhaseStateMachine`: owns setup/active/wrap-up/final/awaiting-finish transitions and turn counting.
- `LessonCommandStateService` or explicit state model: computes button enablement from phase and busy flags.
- `BotVoicePlaybackCoordinator`: owns manual play, auto-play, exact-text validation, segmentation, prefetch, and cancellation.
- `ChainedVoiceInputCoordinator`: owns record/transcribe/auto-send flow.
- `RealtimeConversationCoordinator`: owns Conversation Mode state, deferred guided start, realtime event handlers, and cleanup.
- `LessonBackendRequestFactory`: builds chat/hint/realtime request DTOs from lesson state.

## What should NOT be changed immediately

Do not immediately rewrite the whole `LessonChatViewModel` or Realtime architecture. The safest next step is to lock down behavior with manual smoke tests and then extract one boundary at a time while preserving public command/state behavior. Do not change lesson JSON, prompt methodology, OpenAI model choices, or fallback endpoints during stabilization.

## Current architectural risks

- Single large ViewModel creates high regression risk.
- Command can-execute attributes and manual `RefreshAllCommandStates()` are easy to miss.
- Lesson phase and button state are coupled but not represented as one formal state machine.
- Realtime and chained voice share UI flags but use different lifecycles.
- Exact spoken text is protected by code/logging, but normalization and segmentation remain in the ViewModel.
- Backend realtime receive loop historically let expected desktop disconnect exceptions escape to Kestrel.
- Mock fallback services are present and useful for degraded operation, but must be clearly distinguished from production OpenAI paths.
