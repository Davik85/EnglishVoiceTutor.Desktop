# Current State

Review date: 2026-05-13.

## Implemented desktop screens

The WPF desktop app currently uses ViewModel-first navigation with these implemented screens:

- Welcome (`WelcomeViewModel`, `WelcomeView`).
- Level selection (`LevelSelectionViewModel`, `LevelSelectionView`).
- Home/topic selection (`HomeViewModel`, `HomeView`).
- Subtopic/situation selection (`SubtopicsViewModel`, `SubtopicsView`).
- Lesson chat (`LessonChatViewModel`, `LessonChatView`).
- Lesson summary (`LessonSummaryViewModel`, `LessonSummaryView`).
- Lesson history (`LessonHistoryViewModel`, `LessonHistoryView`).
- Settings (`SettingsViewModel`, `SettingsView`).

## Implemented topics and lesson content structure

Lesson content lives under `Content/Lessons/<TopicFolder>/<lesson>.json` and is loaded by `LessonContentService`. Current content files are:

- Everyday English: asking for help, introductions, making plans, small talk with a neighbor, talking about your day.
- Free Conversation: open conversation.
- Job Interview: asking questions at the end, strengths and weaknesses, tell me about yourself, why do you want this job, work experience.
- Restaurant & Cafe: asking about ingredients, booking a table, handling a wrong order, ordering food, paying the bill.
- Travel: airport check-in, asking for directions, hotel check-in, lost luggage, ordering transport.
- Work & Business: asking for clarification, daily standup, discussing deadlines, first meeting, phone call with a client.

Each JSON lesson generally contains:

- `id` and `metadata` (`topic`, `subtopic`, `lessonType`).
- `learningGoal`.
- default turn limits and per-level `levelProfiles`.
- setup, target language, hint, repetition, feedback, controlled variation, and conversation flow sections.
- guided lessons use `lessonType: guided_roleplay`; Free Conversation uses `lessonType: free_conversation`.

## Current backend endpoints

`backend/EnglishVoiceTutor.Api/Program.cs` maps:

- `GET /health`.
- `GET /api/health`.
- `GET /api/backend/config-status`.
- `POST /api/lesson-chat/reply`.
- `POST /api/lesson-chat/mock-reply`.
- `POST /api/lesson-chat/hint`.
- `POST /api/audio/transcribe`.
- `POST /api/translate`.
- `POST /api/audio/speech`.
- `POST /api/audio/speech-stream`.
- `MAP /api/realtime-voice` for desktop-to-backend WebSocket gateway.

## Current desktop services

- `LessonContentService`: reads lesson JSON content and builds topic/subtopic lists.
- `LessonChatBackendService`: owns HTTP/WebSocket backend calls from desktop.
- `AudioRecordingService`: records learner microphone input for chained STT.
- `AudioInputDeviceService`: enumerates audio input devices for Settings.
- `AudioPlaybackService`: saves and plays bot TTS audio files.
- `BotVoiceTempFileCleanupService`: removes temporary bot voice files.
- `LessonHistoryService`: stores lesson completion history.
- `UserSettingsService`: stores local app settings.
- `RealtimeVoiceConversationEngine`: desktop WebSocket client for `/api/realtime-voice`.
- `RealtimeAudioPlaybackService`: buffers and plays PCM realtime assistant audio.
- `RealtimeMicrophoneCaptureService`: captures PCM microphone audio for Realtime.

## Current backend services

- `OpenAiLessonChatService` / `MockLessonChatService` through `ILessonChatService`.
- `OpenAiLessonHintService` / `MockLessonHintService` through `ILessonHintService`.
- `LessonPromptBuilder` and `LessonLimitHelper` for chat prompt/turn metadata.
- `AudioTranscriptionService` for `/api/audio/transcribe`.
- `AudioSpeechService` for `/api/audio/speech` and `/api/audio/speech-stream`.
- `TranslationService` for `/api/translate`.
- `RealtimeVoiceSessionService` for the realtime WebSocket gateway.
- `OpenAiOptionsProvider` and `TutorAvatarProfileProvider` for runtime configuration/profile data.

## Current voice modes

1. Text chat only: user types; desktop calls `/api/lesson-chat/reply`.
2. Chained voice fallback: user records audio; desktop sends STT to `/api/audio/transcribe`, sends resulting text to `/api/lesson-chat/reply`, then uses `/api/audio/speech` for bot playback.
3. Manual Play voice: user clicks Play on a visible bot message; desktop sends that message text to `/api/audio/speech`.
4. Auto-play bot voice: desktop optionally plays newly-created bot messages, but setup auto-play is intentionally disabled/skipped.
5. Realtime Conversation Mode: desktop opens a WebSocket to `/api/realtime-voice`; backend opens OpenAI Realtime and relays same-response assistant transcript/audio deltas.

## Current Realtime status

Realtime Conversation Mode is implemented but should be treated as unstable. It can start, relay transcript/audio events, and play PCM audio, but known risks remain around state transitions, first-audio latency, disconnect lifecycle, guided setup deferral, and final turn enforcement.

## Current chained STT/text/TTS fallback status

The fallback path remains implemented and is important. It uses separate calls for transcription, text response, and TTS. That makes it more robust as a fallback but higher latency and more fragile than direct realtime voice. It must not be deleted until Realtime is stable.

## Current known working commands

Intended commands from repository root:

```powershell
powershell -ExecutionPolicy Bypass -File tools\audit_lesson_content.ps1
dotnet restore
dotnet build
dotnet build -c Release
cd backend\EnglishVoiceTutor.Api
dotnet restore
dotnet build
```

This Linux review container did not have `dotnet` or PowerShell installed, so build/audit execution could not be completed here. Run the commands above on the Windows development/test machine.
