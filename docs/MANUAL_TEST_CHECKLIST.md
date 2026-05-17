# Manual Test Checklist

Review date: 2026-05-17.

Use this checklist for the short regression smoke-test after the documentation update. Record tester notes, failures, screenshots, and backend log excerpts in a separate dated test run note if needed.

## Environment

- Windows desktop test machine.
- Backend running from `backend\EnglishVoiceTutor.Api` with `OPENAI_API_KEY` set.
- Desktop app built in Debug or Release.
- Microphone and speakers/headphones available.

## Pre-test validation commands

From the repository root:

```powershell
powershell -ExecutionPolicy Bypass -File tools\audit_lesson_content.ps1
dotnet restore
dotnet build
dotnet build -c Release
```

From `backend\EnglishVoiceTutor.Api`:

```powershell
dotnet restore
dotnet build
```

## App navigation smoke-test

- [ ] App starts without a startup crash.
- [ ] Welcome screen appears.
- [ ] Level selection works.
- [ ] Topic selection works.
- [ ] Subtopic/situation selection works.
- [ ] Lesson Chat opens for the selected lesson.

## Normal Lesson Chat smoke-test

- [ ] Typed Lesson Chat message sends successfully.
- [ ] Enter-to-send sends a valid typed message.
- [ ] Normal Send button sends a valid typed message.
- [ ] Bot reply appears after typed input.
- [ ] Normal voice recording starts and stops.
- [ ] Normal voice recording is transcribed.
- [ ] Valid voice transcript sends into Lesson Chat.
- [ ] Bot reply appears after valid voice transcript.
- [ ] Hint works in normal Lesson Chat.
- [ ] Translate works on an existing message.
- [ ] Play voice works on an existing bot message.
- [ ] View feedback works on an active roleplay learner message.
- [ ] Feedback panel shows readable section cards.
- [ ] Clicking the feedback card closes it.

## Context-selection feedback smoke-test

- [ ] Select or type a setup/context phrase before active roleplay.
- [ ] View feedback on the context-selection message.
- [ ] Feedback is phrase-level.
- [ ] Feedback is tied to the clicked context-selection message.
- [ ] Feedback does not treat the context-selection phrase as an active roleplay answer.
- [ ] The context-selection message does not incorrectly increment active roleplay learner-turn state.

## Conversation Mode smoke-test

- [ ] Conversation Mode opens from Lesson Chat.
- [ ] Full avatar overlay appears.
- [ ] Red record button appears.
- [ ] Exit/back button appears.
- [ ] Latest bot phrase bubble appears.
- [ ] First bot message is spoken in Conversation Mode.
- [ ] Bottom-left Hint button appears.
- [ ] Conversation Mode Hint opens the semi-transparent hint overlay.
- [ ] Conversation Mode recording starts and stops.
- [ ] User audio is transcribed.
- [ ] Latest user phrase bubble appears.
- [ ] Bot reply is generated through the lesson chat flow.
- [ ] Latest bot phrase bubble appears.
- [ ] Conversation Mode voice playback speaks the visible bot text exactly.
- [ ] Multiple turns work.
- [ ] Exit Conversation Mode returns to Lesson Chat.
- [ ] Conversation Mode transcript messages remain visible in Lesson Chat after return.
- [ ] Feedback works for a Conversation Mode transcript message after returning to Lesson Chat.

## Summary smoke-test

- [ ] Complete enough valid learner turns to reach the lesson finish state.
- [ ] Finish lesson remains available at completion.
- [ ] Lesson summary opens.
- [ ] Summary includes valid lesson turns.
- [ ] Summary excludes invalid retry/status messages.

## Backend log checks

Confirm backend/developer logs show the MVP voice routing:

- [ ] Normal Lesson Chat TTS uses `tts-1`.
- [ ] Normal Lesson Chat TTS uses `purpose=lesson_chat_tts`.
- [ ] Conversation Mode TTS uses `gpt-4o-mini-tts`.
- [ ] Conversation Mode TTS uses `purpose=conversation_mode_tts`.
- [ ] Conversation Mode TTS uses voice `coral`.
- [ ] Conversation Mode TTS uses speed `1.0`.
- [ ] Conversation Mode TTS has `HasInstructions=True`.
- [ ] No Realtime WebSocket opens by default.
- [ ] No default MVP Conversation Mode request uses Realtime-generated replies.

## Documentation-relevant policy tests

Run when Python is available from the repository root:

```powershell
python tools\test_conversation_mode_tts_provider_policy.py
python tools\test_conversation_mode_tts_instructions_policy.py
python tools\test_realtime_conversation_overlay_policy.py
python tools\test_feedback_target_binding_policy.py
python tools\test_ui_polish_regression_policy.py
python tools\test_usage_cost_policy.py
```

## Pass/fail notes

- Overall result: Pending.
- Tester:
- Date:
- Lesson(s):
- Failures:
- Backend log excerpts:
- Screenshots/video:
