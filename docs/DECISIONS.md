# Decisions

Review date: 2026-05-17.

## Current product decisions

- Normal Lesson Chat uses the canonical teaching policy from `LessonPromptBuilder` and the backend lesson chat flow.
- Conversation Mode now uses the same lesson methodology and lesson chat reply flow as normal Lesson Chat.
- Normal Lesson Chat TTS remains `tts-1` with `purpose=lesson_chat_tts`.
- Normal voice transcription remains `gpt-4o-mini-transcribe`.
- Conversation Mode uses the stable TTS provider by default: microphone recording -> audio transcription -> lesson chat reply -> `gpt-4o-mini-tts` playback.
- Conversation Mode TTS uses `model=gpt-4o-mini-tts`, `voice=coral`, `purpose=conversation_mode_tts`, speed `1.0`, and calm speech instructions.
- Conversation Mode spoken text must match the visible bot text exactly; do not shorten, summarize, rewrite, or chunk spoken text.
- Realtime remains implemented/partially stabilized in the repository for future provider-switch testing, but it is not the default product Conversation Mode provider.
- Default product Conversation Mode should not open a Realtime WebSocket.
- Scenario JSON remains avatar-neutral and must not hardcode Lana or another tutor identity.
- Tutor identity comes from `TutorProfile` / tutor avatar profile data.
- A1/A2/B1/B2 complexity belongs to level rules/policy such as `levelProfiles` and prompt policy metadata.
- Lesson scenario, context variation, level adapter/rules, and tutor profile are combined at runtime and should stay separate in documentation and future tasks.
- Awaiting Finish disables new lesson input but not message review: feedback, translation, and Play voice for existing messages remain available until Finish lesson is clicked.
- Pricing constants remain approximate placeholders until real pricing and measured sessions are reviewed.
