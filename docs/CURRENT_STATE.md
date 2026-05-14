# Current State

Review date: 2026-05-14.

This document records the current stabilized baseline after the Realtime Conversation Mode, guided methodology, feedback, summary, TTS, and final-review fixes. It is a current-state note, not a claim that every manual test has permanently passed.

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

Lesson content lives under `Content/Lessons/<TopicFolder>/<lesson>.json` and is loaded by `LessonContentService`. There are currently 26 lesson JSON files:

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

Keep the methodology layers separate:

1. Lesson scenario: topic, subtopic, learning goal, roles, target language, scenario flow, context variants, roleplay beats, wrap-up/final behavior.
2. Context variation: setting, reason for meeting, and safe small details inside the same lesson goal.
3. Level adapter / level rules: A1/A2/B1/B2 complexity, sentence length, grammar depth, conversation depth, and feedback strictness.
4. Tutor profile: tutor name, personality, background, voice/tone, and safe identity details.

Tutor identity comes from `Content/Tutors/*.json` and runtime tutor profile data, not from lesson scenario JSON. Scenario JSON should remain avatar-neutral; for example, Introductions uses `{tutorName}` rather than hardcoding Elena.

## Current backend endpoints

`backend/EnglishVoiceTutor.Api/Program.cs` maps:

- `GET /health`.
- `GET /api/health`.
- `GET /api/backend/config-status`.
- `POST /api/lesson-chat/reply`.
- `POST /api/lesson-chat/mock-reply`.
- `POST /api/lesson-chat/hint`.
- `POST /api/lesson-chat/feedback`.
- `POST /api/audio/transcribe`.
- `POST /api/translate`.
- `POST /api/audio/speech`.
- `POST /api/audio/speech-stream`.
- `MAP /api/realtime-voice` for the desktop-to-backend WebSocket gateway.

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
- `LessonPromptBuilder` and `LessonLimitHelper` for shared tutor prompt policy and turn metadata.
- `AudioTranscriptionService` for `/api/audio/transcribe`.
- `AudioSpeechService` for `/api/audio/speech` and `/api/audio/speech-stream`.
- `TranslationService` for `/api/translate`.
- `RealtimeVoiceSessionService` for the realtime WebSocket gateway.
- `OpenAiOptionsProvider` and `TutorAvatarProfileProvider` for runtime configuration/profile data.

## Current voice modes

1. Typed Lesson Chat: user types; desktop calls `/api/lesson-chat/reply`.
2. Chained voice fallback: user records audio; desktop sends STT to `/api/audio/transcribe`, sends valid resulting text to `/api/lesson-chat/reply`, then uses `/api/audio/speech` for bot playback when playback is requested/enabled.
3. Manual Play voice: user clicks Play on a visible bot message; desktop sends that exact visible message text, after trim/newline normalization only, to `/api/audio/speech`.
4. Auto-play bot voice: desktop optionally plays newly-created bot messages through `/api/audio/speech`; setup auto-play is intentionally skipped.
5. Realtime Conversation Mode: desktop opens a WebSocket to `/api/realtime-voice`; backend opens OpenAI Realtime and relays same-response assistant transcript/audio deltas.

Normal Lesson Chat TTS currently uses `tts-1`. Realtime Conversation Mode currently uses `gpt-realtime`.

## Current normal Lesson Chat path

Typed and chained fallback turns share the normal Lesson Chat path:

1. Validate the learner transcript/text.
2. Reject empty, placeholder, or invalid/non-English transcripts with a retry prompt.
3. Count valid learner turns only after active roleplay has started.
4. Send the turn to `/api/lesson-chat/reply` with scenario, level, tutor, recent conversation, and turn-limit metadata.
5. Attach feedback to valid learner messages when feedback is available.
6. Keep manual Play and auto-play on `/api/audio/speech` using `tts-1`.

Manual Play must speak exactly the visible bot message, apart from harmless trim/newline normalization.

## Current Realtime Conversation Mode path

Realtime uses a separate audio transport but the same canonical tutor teaching policy as normal Lesson Chat.

- `/api/realtime-voice` is only for Conversation Mode.
- Realtime generated assistant turns stay on Realtime transport and must not use `/api/audio/speech`.
- Assistant audio and visible assistant transcript must come from the same Realtime response.
- For audio input, `response.create` is transcript-gated: the backend commits user audio, waits for a valid user transcript, then creates the assistant response.
- The pending `[Voice message]` user placeholder is replaced by the validated Realtime transcript.
- Invalid/empty/non-English Realtime transcripts are marked as retry/invalid messages, do not count as learner turns, do not create a normal assistant response, and remain excluded from feedback and summary.

## Current feedback behavior

- Typed learner messages and valid chained voice messages are feedback-eligible.
- Valid Realtime user transcripts are shown as normal learner messages and are feedback-eligible after returning to chat/review.
- Invalid transcript retry messages are technical/non-feedback messages and must stay excluded from feedback.
- Feedback strictness is driven by level/profile policy metadata, not by hardcoded tutor identity in lesson JSON.

## Current summary behavior

Lesson Summary should use the whole valid lesson conversation, not only the final exchange. Invalid transcript retry messages and technical placeholders must remain excluded. Finish lesson navigates to summary/history after the completed lesson is confirmed.

## Current final / Awaiting Finish behavior

At the final learner turn, the app shows the final tutor message once, sets the lesson to Completed/Awaiting Finish, and disables new lesson input. Awaiting Finish disables Send, Start recording, Hint, Back, and Conversation Mode, but keeps review actions available until Finish lesson is clicked:

- View feedback on valid learner messages.
- Translate existing messages.
- Play voice for existing bot messages.
- Finish lesson remains enabled and navigates to summary/history.

`CanReviewExistingMessages` is intentionally separate from lesson input state.

## Current known working baseline

Latest Windows smoke validation was developer-provided and reported:

- `tools/audit_lesson_content.ps1`: 0 errors, 0 warnings.
- `dotnet restore` from repository root: passed.
- `dotnet build` from repository root: passed.
- `dotnet build -c Release` from repository root: passed.
- `backend/EnglishVoiceTutor.Api` `dotnet restore`: passed.
- `backend/EnglishVoiceTutor.Api` `dotnet build`: passed.
- Manual testing: current behavior is generally acceptable for now.

This review container also ran the repository checks listed in the final task report for this documentation update.

## Known remaining risks

- `LessonChatViewModel` still owns many responsibilities and remains the main regression risk.
- Realtime needs broader long-session testing and latency measurement before tuning.
- Lesson flow should be smoke-tested across all five MVP topics, not only A1 Introductions.
- Scenario methodology and prompts are acceptable for stabilization but still need a quality polish pass.
- Manual test coverage should be strengthened and recorded after each future behavior change.
- Do not add subscriptions, avatar expansion, broad UI polish, or all-lesson JSON migration until the smoke checklist is reliable.
