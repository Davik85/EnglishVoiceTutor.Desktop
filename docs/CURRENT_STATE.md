# Current State

Review date: 2026-05-17.

This document records the current MVP state after the recent stabilization work. It describes the validated behavior that documentation should reflect; it is not a request to change runtime behavior.

## Current MVP summary

The MVP core lesson flow works:

1. App start and navigation through level, topic, subtopic, and lesson chat are implemented.
2. Lesson Chat works by typed input and normal voice input.
3. Enter-to-send and the normal Send button both work.
4. Normal voice recording, transcription, lesson chat replies, Play voice, Translate, Hint, View feedback, and lesson summary work.
5. Feedback and hint UI are readable and use the warm card style.
6. Conversation Mode works by using the stable TTS provider by default, not the Realtime provider.
7. Realtime remains in the repository for future testing and provider-switch work, but it is not the default MVP Conversation Mode path.

## Latest Windows validation

The latest confirmed Windows validation is:

- `powershell -ExecutionPolicy Bypass -File tools\audit_lesson_content.ps1` passed and reported 26 lesson JSON files.
- Desktop `dotnet build` passed in Debug.
- Desktop `dotnet build -c Release` passed in Release.
- Backend `dotnet build` passed from `backend\EnglishVoiceTutor.Api`.

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

## Lesson content

Lesson content lives under `Content/Lessons/<TopicFolder>/<lesson>.json` and is loaded by `LessonContentService`. The current audit passes with 26 lesson JSON files:

- Daily Life: asking for help, introductions, making plans, small talk with a neighbor, talking about your day. Internal lesson IDs and the legacy `Content/Lessons/EverydayEnglish` folder name are preserved for compatibility.
- Free Conversation: open conversation.
- Job Interview: asking questions at the end, strengths and weaknesses, tell me about yourself, why do you want this job, work experience.
- Restaurant & Cafe: asking about ingredients, booking a table, handling a wrong order, ordering food, paying the bill.
- Travel: airport check-in, asking for directions, hotel check-in, lost luggage, ordering transport.
- Work & Business: asking for clarification, daily standup, discussing deadlines, first meeting, phone call with a client.

Keep methodology layers separate:

1. Lesson scenario: topic, subtopic, learning goal, roles, target language, scenario flow, context variants, roleplay beats, wrap-up/final behavior.
2. Context variation: setting, reason for meeting, and safe small details inside the same lesson goal.
3. Level adapter / level rules: A1/A2/B1/B2 complexity, sentence length, grammar depth, conversation depth, and feedback strictness.
4. Tutor profile: tutor name, personality, background, voice/tone, and safe identity details.

Tutor identity comes from `Content/Tutors/*.json` and runtime tutor profile data, not from lesson scenario JSON.

## Normal Lesson Chat

Normal Lesson Chat currently works with:

- typed input;
- Enter-to-send;
- normal Send button;
- normal voice recording;
- transcription through `gpt-4o-mini-transcribe`;
- lesson chat reply generation through the backend lesson chat model;
- Play voice;
- Translate;
- Hint;
- View feedback;
- lesson summary.

Normal Lesson Chat TTS remains `tts-1` with `purpose=lesson_chat_tts`.

## Feedback

Feedback currently works with:

- global bottom feedback panel;
- feedback tied to the clicked message through `sourceMessageId` and `sourceMessageKind`;
- phrase-level feedback for `ContextSelection` messages;
- no treatment of context-selection phrases as active roleplay answers;
- readable section cards inside the feedback panel;
- click-to-close behavior on the feedback card.

Feedback should remain available for valid existing learner messages during review states, including after returning from Conversation Mode when transcript messages are visible in chat.

## Hint

Hint currently works with:

- normal Lesson Chat Hint button;
- Conversation Mode bottom-left Hint button;
- Conversation Mode semi-transparent hint overlay;
- the same warm visual style used by feedback cards.

## Translate, Play voice, and summary

- Translate works for existing messages.
- Play voice works for bot messages outside active Conversation Mode.
- Lesson summary uses valid lesson turns and excludes invalid retry/status messages.
- Awaiting Finish is a review state: new lesson input is disabled while existing feedback, translation, and Play voice actions remain available where appropriate.

## Conversation Mode default pipeline

Current MVP voice decision:

Conversation Mode uses the stable TTS provider by default:

`microphone recording -> audio transcription -> lesson chat reply -> gpt-4o-mini-tts playback`

Realtime remains in the codebase for future testing, but it is not the default MVP path. The learner must hear exactly the same text that is displayed, so Conversation Mode does not shorten, summarize, rewrite, or chunk spoken text.

Conversation Mode currently works with:

- full avatar overlay;
- red record button;
- exit/back button;
- latest user phrase bubble;
- latest bot phrase bubble;
- bottom-left Hint button;
- semi-transparent hint overlay;
- user audio recording;
- transcription;
- bot reply generation through the same lesson chat reply flow as normal Lesson Chat;
- voice playback;
- multiple turns.

Conversation Mode TTS uses:

- model: `gpt-4o-mini-tts`;
- voice: `coral`;
- purpose: `conversation_mode_tts`;
- speed: `1.0`;
- calm speech instructions.

Conversation Mode speech must match the visible bot text exactly. Do not document or introduce shortened, summarized, rewritten, or chunked spoken-only text for Conversation Mode.

## Realtime status

Realtime remains implemented/partially stabilized in the repository for future testing. It is kept behind provider-switch/future-option work and should be considered non-default for the MVP. The default MVP Conversation Mode path should not open a Realtime WebSocket.

Realtime was not selected as the default MVP lifecycle because it was too unstable for MVP validation compared with the chained TTS provider path. Realtime diagnostics, schema work, and policy coverage remain useful for future review.

## Usage and cost logging

Usage/cost logging exists for lesson chat, transcription, speech, and realtime-related paths. Exact pricing fields remain approximate or missing where pricing constants are not configured. Monthly and unit economics should be recalculated later from real usage logs.

## UI polish

The app currently reflects the Soft Learning Desktop style:

- light blue frame;
- rounded cards, buttons, and inputs;
- level colors;
- topic colors;
- warm hint and feedback cards.

## Current focus after this documentation update

1. Run a short regression smoke-test.
2. Continue toward MVP infrastructure:
   - local/user data;
   - accounts;
   - usage limits;
   - payment/subscription planning;
   - packaging/installer;
   - release preparation;
   - support diagnostics such as error reporting and log export.

## Study language layer

The MVP now supports a study-language layer for English, French, German, Portuguese, Spanish, and Italian. English remains the default study language.

The same 26 lesson JSON scenarios are reused for every study language. Lesson JSON remains semantic scenario metadata; it is not duplicated or translated per language. The selected study language is passed to lesson chat, hint, feedback, summary input, translation source context, audio transcription, and Conversation Mode speech requests.

Study language is selected in Settings and is separate from UI/interface language. UI localization is not complete for the new study languages. Translation still targets the learner/native language setting and can be refined later as a separate data-storage/product task.

Conversation Mode continues to use the stable TTS provider by default (`gpt-4o-mini-tts`) with target-language speech instructions. Realtime remains present for future experimentation and is not the default MVP path.
