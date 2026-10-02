# Decisions

Review date: 2026-10-02.

- Admin/tester behavior feedback is triaged through the CMS behavior tuning playbook: choose one CMS area, make a draft edit with paste-ready wording, validate/preview, publish, start a new lesson, and restore the previous published version if worse.
- `static-json-v1` / `Static JSON Baseline` is a CMS pack/seed identity; the decisive active runtime fields are `Actual learner runtime source` and `Currently using static JSON fallback`.

## Current product decisions

- Normal Lesson Chat uses CMS published prompt/scenario/tutor/level content for editable teaching behavior, with `LessonPromptBuilder` responsible for assembly and backend-owned guardrails in the backend lesson chat flow.
- Conversation Mode now uses the same lesson methodology and lesson chat reply flow as normal Lesson Chat.
- Normal Lesson Chat TTS uses `gpt-realtime-2.1-mini` with `purpose=lesson_chat_tts`.
- Normal voice transcription uses the active production `gpt-transcribe` model; legacy configured transcription models remain compatible.
- Conversation Mode uses the same chained product flow: `learner audio -> gpt-transcribe -> validated learner text -> gpt-5.6-luna lesson reply -> exact final visible tutor text -> gpt-realtime-2.1-mini speech rendering`.
- Conversation Mode TTS uses `model=gpt-realtime-2.1-mini`, the selected canonical voice (David fallback `cedar`, other tutors `coral`), `purpose=conversation_mode_tts`, speed `1.0`, and calm speech instructions.
- Speech speed is fixed at `1.0` at settings and both backend speech boundaries; legacy `ConversationModeEnabled` settings stays true. See [Voice and Realtime Review](VOICE_AND_REALTIME_REVIEW.md) for canonical catalog and old-client compatibility.
- Conversation Mode spoken text must match the visible bot text exactly; do not shorten, summarize, rewrite, or chunk spoken text.
- Full Realtime Conversation Mode remains dormant; the project decision is not to return it. Realtime-mini is only a backend renderer of exact already-final visible text and does not generate lesson content or conversation decisions.
- Current speech rendering opens a short-lived server-to-server Realtime WebSocket. The client product flow does not open `/api/realtime-voice` or stream the learner microphone to that renderer.
- Scenario JSON remains avatar-neutral and must not hardcode Lana or another tutor identity.
- Tutor identity comes from `TutorProfile` / tutor avatar profile data.
- A1/A2/B1/B2 complexity, strictness guidance, and wrap/final timing belong to CMS level profiles when CMS runtime is active; prompt templates must not define numeric wrap/final timing.
- Lesson scenario, context variation, level adapter/rules, and tutor profile are combined at runtime and should stay separate in documentation and future tasks.
- Awaiting Finish disables new lesson input but not message review: feedback, translation, and Play voice for existing messages remain available until Finish lesson is clicked.
- Pricing constants remain approximate placeholders until real pricing and measured sessions are reviewed.
