# Decisions

Review date: 2026-05-16.

## Current stabilization decisions

- Normal Lesson Chat and Realtime share the canonical teaching policy from `LessonPromptBuilder`.
- Audio transport differs by mode: normal Lesson Chat uses `/api/audio/speech` for manual Play, auto-play, and chained TTS fallback; Realtime uses `/api/realtime-voice` and OpenAI Realtime.
- Realtime generated assistant turns must not use `/api/audio/speech`.
- Scenario JSON remains avatar-neutral and must not hardcode Elena or another tutor identity.
- Tutor identity comes from `TutorProfile` / tutor avatar profile data.
- A1/A2/B1/B2 complexity belongs to level rules/policy such as `levelProfiles` and prompt policy metadata.
- Lesson scenario, context variation, level adapter/rules, and tutor profile are combined at runtime and should stay separate in documentation and future tasks.
- Awaiting Finish disables new lesson input but not message review: feedback, translation, and Play voice for existing messages remain available until Finish lesson is clicked.

## 2026-05-16 stabilization decisions

- The cleanup baseline intentionally does not change user-facing lesson behavior or lesson methodology.
- Normal Lesson Chat TTS remains `tts-1`.
- Normal voice transcription remains `gpt-4o-mini-transcribe`.
- Realtime Conversation Mode remains `gpt-realtime` on the GA `/v1/realtime` schema.
- Realtime pre-start opening playback remains routed through `/api/audio/speech` with `purpose=realtime_pre_start_opening`.
- Realtime-generated assistant replies remain on Realtime audio and must not route through `/api/audio/speech`.
- Pricing constants remain approximate placeholders until real pricing and measured sessions are reviewed.
