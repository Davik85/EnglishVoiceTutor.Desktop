# Windows Client Functionality Overview

Review date: 2026-07-06.

## Product summary

Language Voice Tutor Desktop is a Windows desktop client for guided language speaking practice. The current app helps learners choose a study language, CEFR level, topic, and practical scenario, then practice with an AI tutor in a lesson chat using typed text or microphone recording.

The document is intended to serve as:

- an internal source of truth for the current Windows client functionality;
- a customer/client presentation preparation reference;
- a planning reference for a future mobile client that should mirror the same core product model.

This document describes current product behavior only. It does not claim that broad public production readiness, production billing operations, mobile apps, or production CMS/Admin operations are complete beyond the cautious status already recorded in the release-readiness documents.

## Current supported platforms

- Current client platform: Windows desktop app.
- Current public distribution path: Windows direct-download release, with the live manifest as the release source of truth.
- Future platforms: Android and iOS mobile clients are planning targets only; mobile apps are not currently available.
- The desktop client is backend-driven for account, lesson, subscription/Premium, AI, transcription, translation, TTS, progress/history, and settings sync behavior where those features require server state.

## Main user flow

1. The learner opens the desktop app landing/home experience.
2. The learner can choose **Start lesson** or open **Settings**.
3. Settings are organized into **Learning**, **Account**, **Audio**, **Progress**, and **Contacts** sections.
4. For lesson onboarding, the learner chooses a CEFR level.
5. The learner chooses a main topic.
6. The learner chooses a practical situation/subtopic inside that topic.
7. The app starts Lesson Chat with an AI tutor introduction and a header showing the selected topic, situation, and level.
8. The learner practices by typing or recording voice.
9. The learner can ask for hints, show translations, play tutor voice, use auto-send/auto-play options, switch into Conversation Mode, and finish the lesson.
10. After finishing, the app records and displays lesson/progress state through the backend/local settings flow currently implemented for the desktop client.

## Language configuration

The Windows client currently separates three language concepts:

- **Study language**: the language the learner practices in lessons.
- **Native / explanation language**: the learner-facing native language catalog used for explanation/native-language preferences.
- **Interface language**: the localized desktop UI language options that are release-ready for the current phase.

Current language counts verified from code:

- Study languages: exactly **6** — English, French, German, Portuguese, Spanish, and Italian.
- Native / Explanation languages: currently **55** languages in `NativeLanguageCatalog`.
- Release-ready interface languages: exactly **14** — `en`, `es`, `fr`, `de`, `it`, `pt`, `ru`, `pl`, `ar`, `ja`, `ko`, `sr`, `hr`, and `bg`.

Important boundary: the broad native/explanation language catalog is not the same as full UI localization coverage. The UI selector is intentionally limited to the release-ready interface language list. Unknown or missing UI text can fall back to English as a safety mechanism.

## Tutor and voice configuration

The Learning settings section lets the learner configure tutor and voice behavior:

- Choose a tutor avatar.
- Choose the tutor speech voice.
- View the selected avatar profile, including age, location, role/background, interests, personality, and speaking style.

Current tutor content includes JSON tutor profiles for David, Lana, and Nelli. Tutor profiles provide stable identity/background rules and level-specific speaking guidance so the AI tutor can remain consistent while adapting to the selected lesson scenario.

## Account and subscription functionality

The Account settings section currently exposes account and subscription controls:

- Register.
- Login.
- Logout.
- Forgot password.
- Change password.
- Current account display.
- Settings source display.
- Subscription/Premium status display.
- Buy Premium / upgrade entry point.
- Refresh status.

Billing and subscription wording must remain cautious. Controlled Paddle live payment/webhook/Premium activation and selected subscription validation have been documented elsewhere, but broad paid launch and expanded customer portal/subscription management remain follow-up work. Do not present billing as fully complete broad production operations unless a current release-readiness source explicitly says so.

## Audio and microphone functionality

The Audio settings section supports:

- Microphone/input-device selection.
- Refreshing available microphone devices.
- Microphone test.
- Saving the selected microphone for lesson recording.

In Lesson Chat, the learner can record voice, stop recording, and send recorded speech through the backend transcription and lesson-reply flow. OpenAI/API credentials are backend-only and must never be stored in the desktop client.

## Progress tracking

The Progress settings section currently summarizes learner activity:

- Total completed lessons.
- Lessons completed today.
- Current streak.
- Last completed lesson.

Progress/history behavior is backend-driven where available, with the desktop client presenting the learner-facing state. Future clients should reuse the same backend-owned progress model rather than creating client-specific progress counters.

## Level selection

The onboarding flow currently offers four CEFR-aligned levels:

- A1 Beginner.
- A2 Elementary.
- B1 Intermediate.
- B2 Upper-Intermediate.

Lesson content contains level-specific guidance so the AI tutor can adjust sentence length, vocabulary, correction depth, and scenario complexity.

## Topics, subtopics, and practical scenarios

The current main topic list includes:

- Daily Life.
- Travel.
- Work & Business.
- Job Interview.
- Restaurant & Cafe.
- Free Conversation.

Current scenario content is stored under `Content/Lessons` and includes practical situations such as:

- Daily Life: introductions, asking for help, making plans, small talk with a neighbor, talking about your day.
- Travel: airport check-in, hotel check-in, asking for directions, ordering transport, lost luggage.
- Work & Business: first meeting, daily standup, phone call with a client, asking for clarification, discussing deadlines.
- Job Interview: tell me about yourself, work experience, strengths and weaknesses, why do you want this job, asking questions at the end.
- Restaurant & Cafe: booking a table, ordering food, asking about ingredients, handling a wrong order, paying the bill.
- Free Conversation: open conversation with safety and respectful-boundary constraints.

Runtime learner behavior for published Windows direct lessons may use the CMS published snapshot according to the current backend release-readiness state, with static JSON remaining available for initialization/emergency rollback. Save draft + Publish is required before newly started desktop lessons see CMS changes.

## Lesson chat capabilities

Lesson Chat currently includes:

- AI tutor introduction/setup message.
- Header context showing topic, situation/subtopic, and level.
- Typed learner input.
- Voice recording input.
- Send.
- Start/stop recording.
- Hint.
- Show/hide translation.
- Play voice / tutor TTS playback.
- Auto-send voice.
- Auto-play bot voice.
- Conversation Mode toggle.
- Finish lesson.
- Lesson summary/feedback flow after finishing.

The normal lesson flow calls the backend lesson-chat endpoint. If the backend or required AI services are unavailable, user-facing error handling should be friendly and localized rather than exposing raw exceptions.

## Conversation Mode

Conversation Mode is a lesson-chat mode designed for a more immersive speaking experience with the tutor/avatar. It keeps the same selected lesson context and backend-owned lesson behavior while changing the interaction feel and layout for conversation practice.

Conversation Mode should not be treated as a separate product, separate account model, or separate lesson engine. It is part of the same Windows client lesson flow and should be mirrored thoughtfully in future mobile UX if mobile voice ergonomics support it.

## Translation, voice playback, hints, and corrections

Lesson Chat supports learner assistance features:

- **Hints** provide suggested wording or a practical next step when the learner needs help.
- **Show translation** displays translated support text using the learner/interface explanation language path currently implemented in the app.
- **Play voice** uses backend text-to-speech for tutor/bot voice playback.
- **Corrections and feedback** are level-aware and scenario-aware, with correction depth increasing from A1 through B2.
- **Lesson summary** captures what went well, what to improve, useful phrases, and next steps after the learner finishes.

These features depend on backend AI/transcription/translation/TTS services for full functionality. They should be tested with the backend running and configured.

## Data/settings source of truth

Current source-of-truth boundaries:

- Study language catalog: `Shared/StudyLanguages/StudyLanguageCatalog.cs` and `Content/StudyLanguages/study_languages.json`.
- Native/explanation language catalog: `Shared/NativeLanguages/NativeLanguageCatalog.cs`.
- Release-ready interface language list: `Models/InterfaceLanguageOptions.cs`.
- Settings UI and visible settings sections: `Views/SettingsView.xaml`, `Views/SettingsView.xaml.cs`, and `ViewModels/SettingsViewModel.cs`.
- Landing/home and lesson onboarding: `Views/WelcomeView.xaml`, `Views/HomeView.xaml`, `ViewModels/LevelSelectionViewModel.cs`, `ViewModels/HomeViewModel.cs`, and `ViewModels/SubtopicsViewModel.cs`.
- Lesson chat UI/capabilities: `Views/LessonChatView.xaml` and `ViewModels/LessonChatViewModel.cs`.
- Static packaged lesson/tutor content: `Content/Lessons`, `Content/Prompts`, and `Content/Tutors`.
- Published runtime lesson behavior for current public Windows direct lessons: backend CMS published snapshot when healthy, with static JSON fallback reserved for initialization/emergency rollback according to current-state documentation.
- User account, Premium/subscription status, backend-required lesson actions, AI, transcription, translation, TTS, and synced progress/history are backend-owned.

## Current limitations / not yet public-release-ready areas

Keep external/customer-facing wording honest:

- Do not claim Android or iOS apps exist. They are future planning targets only.
- Do not claim Microsoft Store, Google Play, or App Store availability.
- Do not claim broad public production readiness. Current docs describe a public Windows direct release and a healthy production backend, but broader paid-launch/public-readiness work remains cautious and follow-up driven.
- Do not claim production billing operations or expanded customer portal/subscription management are fully complete; broad paid launch remains pending final review.
- Do not claim the full native/explanation catalog has complete UI localization. Only the 14 release-ready interface languages are exposed as interface languages for this phase.
- Code signing / SmartScreen mitigation remains deferred for the Windows direct installer path.
- CMS/Admin should be described using the existing cautious status: CMS/Admin foundations exist, runtime CMS published snapshot is active for published Windows direct lessons, but broader production CMS/Admin operational readiness and critical-change approval remain follow-up areas unless newer source-of-truth documents say otherwise.

## Mobile client notes: which functionality should be reused or mirrored in the future mobile client

A future mobile client should be another client for the same product, not a separate product. It should reuse or mirror:

- The same user account and backend session model.
- The same backend-owned Premium/subscription entitlement state.
- The same study-language, native/explanation-language, and interface-language concepts, adapted to mobile UI constraints.
- The same four CEFR levels.
- The same topic/subtopic/scenario model.
- The same tutor/avatar and voice identity concepts, adapted to mobile presentation.
- The same typed and voice lesson practice model.
- The same Hint, Translation, Play voice, Auto-send voice, Auto-play bot voice, Conversation Mode, Finish lesson, and summary/feedback concepts where mobile ergonomics support them.
- The same backend-owned progress/history model.
- The same security boundary: no client-side OpenAI calls, no client-side Premium decisions, and no provider secrets in the mobile app.

Mobile planning should adapt layout, touch ergonomics, audio permissions, backgrounding behavior, and store billing surfaces, but should not introduce a separate account system, backend, database, entitlement model, or lesson behavior model.
