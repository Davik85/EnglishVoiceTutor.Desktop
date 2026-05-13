# Manual Test Checklist

Review date: 2026-05-13.

Use this on a Windows machine with the desktop app and backend available.

## Pre-flight commands

- [ ] Audit: `powershell -ExecutionPolicy Bypass -File tools\audit_lesson_content.ps1` reports 0 errors and 0 warnings.
- [ ] Desktop Debug build: `dotnet restore` then `dotnet build` passes from repo root.
- [ ] Desktop Release build: `dotnet build -c Release` passes from repo root.
- [ ] Backend build: from `backend\EnglishVoiceTutor.Api`, run `dotnet restore` then `dotnet build`.
- [ ] Backend run: start the backend from the freshly built checkout and confirm `/api/health` returns OK.
- [ ] Stale backend process check: verify no old `EnglishVoiceTutor.Api`/`dotnet` process is serving the port; confirm PID/start time/log path.

## Setup screen buttons

- [ ] Open a guided lesson.
- [ ] Verify setup/context selection is displayed.
- [ ] Verify Conversation Mode button state matches expected setup behavior.
- [ ] Verify Finish lesson button does not allow an accidental completed lesson before meaningful practice.
- [ ] Verify setup bot message does not auto-play.

## Finish lesson early

- [ ] On setup screen, click Finish lesson if enabled.
- [ ] Expected: behavior is deliberate and does not corrupt navigation/history.
- [ ] Return to a fresh guided lesson.

## Guided context selection

- [ ] Choose a context variant such as neighbor in Small talk with a neighbor.
- [ ] Verify context confirmation appears.
- [ ] Verify active roleplay begins.
- [ ] Verify learner turn count starts only after context selection, not during setup.

## Active roleplay

- [ ] Send a typed learner reply.
- [ ] Verify one bot reply appears.
- [ ] Verify feedback is attached/available for learner message as expected.
- [ ] Verify buttons recover after send.

## Final limit

- [ ] Continue a guided lesson until the final learner turn.
- [ ] Verify final message is shown once.
- [ ] Verify normal input/record/hint/conversation do not continue the lesson.
- [ ] Verify Finish lesson remains active and navigates to summary.

## Manual Play exact text

- [ ] Click Play on a setup/roleplay bot message that is visible.
- [ ] Verify spoken text matches visible text exactly, except harmless trim/newline normalization.
- [ ] Check Debug logs: `RawTextLength`, `VoiceTextLength`, and `IsExactText`.
- [ ] Expected: `RawTextLength == VoiceTextLength` except trim/newline normalization and `IsExactText=True` for manual Play.

## Auto-play disabled for setup

- [ ] Enable bot voice auto-play.
- [ ] Open a guided lesson setup screen.
- [ ] Expected: setup prompt does not auto-play.
- [ ] After context selection, verify roleplay bot message auto-play behavior follows current setting and limits.

## Conversation Mode before context

- [ ] On guided setup screen, enable Conversation Mode.
- [ ] Expected: UI state is clear; realtime session should not start until context selection.
- [ ] Select context.
- [ ] Expected: realtime starts after context selection or fails cleanly with fallback message.

## Conversation Mode after context

- [ ] Start guided roleplay normally.
- [ ] Enable Conversation Mode.
- [ ] Verify realtime starts and backend logs session start.
- [ ] Speak one turn.
- [ ] Verify visible assistant transcript and audio are from the same realtime response.

## Free Conversation

- [ ] Open Free Conversation.
- [ ] Verify no context selection is required.
- [ ] Enable Conversation Mode.
- [ ] Expected: realtime starts immediately when backend/config are available.
- [ ] Verify final limit is 30 learner turns.

## Realtime unavailable

- [ ] Run app with backend unavailable or missing OpenAI key.
- [ ] Enable Conversation Mode.
- [ ] Expected: clear unavailable/fallback message; buttons do not get stuck; text/chained fallback remains usable.

## WebSocket disconnect

- [ ] Start Realtime Conversation Mode.
- [ ] Toggle Conversation Mode off, use Back, and close the app in separate runs.
- [ ] Expected backend logs Information for normal disconnect and no Kestrel fail/unhandled close-handshake exception.

## Voice recording and transcription

- [ ] Record learner audio with chained voice mode.
- [ ] Stop recording.
- [ ] Verify `/api/audio/transcribe` returns text or graceful fallback.
- [ ] Verify auto-send setting behavior.

## Bot TTS fallback

- [ ] With Realtime off, send a typed message.
- [ ] Click Play on bot response.
- [ ] Verify `/api/audio/speech` succeeds and playback finishes.
- [ ] Disable backend/network and verify failure message recovers without crashing.

## Lesson summary

- [ ] Finish a lesson after active roleplay.
- [ ] Verify summary screen appears.
- [ ] Verify lesson history is saved.
- [ ] Verify navigation back to topics/home works.
