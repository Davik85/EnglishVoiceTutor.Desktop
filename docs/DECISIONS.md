# Decisions

Review date: 2026-05-14.

## Current stabilization decisions

- Normal Lesson Chat and Realtime share the canonical teaching policy from `LessonPromptBuilder`.
- Audio transport differs by mode: normal Lesson Chat uses `/api/audio/speech` for manual Play, auto-play, and chained TTS fallback; Realtime uses `/api/realtime-voice` and OpenAI Realtime.
- Realtime generated assistant turns must not use `/api/audio/speech`.
- Scenario JSON remains avatar-neutral and must not hardcode Elena or another tutor identity.
- Tutor identity comes from `TutorProfile` / tutor avatar profile data.
- A1/A2/B1/B2 complexity belongs to level rules/policy such as `levelProfiles` and prompt policy metadata.
- Lesson scenario, context variation, level adapter/rules, and tutor profile are combined at runtime and should stay separate in documentation and future tasks.
- Awaiting Finish disables new lesson input but not message review: feedback, translation, and Play voice for existing messages remain available until Finish lesson is clicked.
