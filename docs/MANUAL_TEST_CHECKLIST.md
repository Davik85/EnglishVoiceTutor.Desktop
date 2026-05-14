# Manual Test Checklist

Review date: 2026-05-14.

Use this checklist on a Windows machine with the desktop app and backend available. Record pass/fail notes when possible. The latest Windows smoke commands were reported passing by the developer, but this checklist should still be rerun before new feature work.

## Pre-flight commands

- [ ] Audit: `powershell -ExecutionPolicy Bypass -File tools\audit_lesson_content.ps1` reports 0 errors and 0 warnings.
- [ ] Desktop Debug build: `dotnet restore` then `dotnet build` passes from repo root.
- [ ] Desktop Release build: `dotnet build -c Release` passes from repo root.
- [ ] Backend build: from `backend\EnglishVoiceTutor.Api`, run `dotnet restore` then `dotnet build`.
- [ ] Backend run: start the backend from the freshly built checkout and confirm `/api/health` returns OK.
- [ ] Stale backend process check: verify no old `EnglishVoiceTutor.Api`/`dotnet` process is serving the port; confirm PID/start time/log path.
- [ ] Secret check: verify no API keys or local `.env` values were committed.

## Normal Lesson Chat A1 Introductions

- [ ] Select `A1 Beginner -> Everyday English -> Introductions`.
- [ ] Verify setup/context selection is displayed.
- [ ] Select `Meeting a new neighbor` or another controlled variant.
- [ ] Verify context confirmation appears and the opening line uses the active tutor profile name through `{tutorName}` resolution.
- [ ] Verify learner turn count starts only after context confirmation/opening.
- [ ] Type `My name is David.` and send.
- [ ] Expected: one tutor reply appears, stays in the introductions scenario, and asks a simple next question.
- [ ] Verify feedback is available for the valid learner message.
- [ ] Continue until soft wrap-up/final to verify the final message appears once.

## Reciprocal question test

- [ ] In A1 Introductions active roleplay, type `Do you study or work?`.
- [ ] Expected: tutor answers from the active tutor profile and asks one simple question back.
- [ ] Expected: response should not hardcode tutor identity from lesson JSON and should not restart onboarding/topic selection.

## Normal chained voice fallback

- [ ] With Conversation Mode off, record learner audio.
- [ ] Stop recording.
- [ ] Verify `/api/audio/transcribe` returns text or a graceful retry.
- [ ] With auto-send off, verify a valid transcript fills `UserInput` without sending.
- [ ] With auto-send on, verify a valid transcript is sent and feedback is available.
- [ ] Verify invalid/empty/non-English transcript retry messages do not count as learner turns.

## Normal Play voice exact text

- [ ] Click Play on a visible setup bot message.
- [ ] Verify `/api/audio/speech` succeeds and playback finishes.
- [ ] Verify spoken text matches the visible bot text exactly, except harmless trim/newline normalization.
- [ ] Check Debug logs for `RawTextLength`, `VoiceTextLength`, and `IsExactText=True`.
- [ ] Click Play on a visible roleplay bot response and repeat the exact-text check.
- [ ] Verify normal TTS endpoint logs show `Model=tts-1`.

## Auto-play bot voice

- [ ] Enable bot voice auto-play.
- [ ] Open a guided lesson setup screen.
- [ ] Expected: setup prompt does not auto-play.
- [ ] After context selection, verify roleplay bot message auto-play follows current settings and limits.
- [ ] Verify auto-play uses `/api/audio/speech` and `tts-1` when it plays.

## Realtime Conversation Mode A1 Introductions

- [ ] Select `A1 Beginner -> Everyday English -> Introductions`.
- [ ] Enable Conversation Mode before context selection if supported by the UI.
- [ ] Expected: guided realtime session does not start until context selection/opening.
- [ ] Select `Meeting a new neighbor`.
- [ ] Verify Realtime starts through `/api/realtime-voice`.
- [ ] Verify backend/session logs show `Model=gpt-realtime`.
- [ ] Verify the scripted tutor opening appears and is spoken as the visible text.
- [ ] Speak `My name is David.`.
- [ ] Verify the Realtime user placeholder is replaced by `My name is David.` or the validated transcript, with no duplicate user message.
- [ ] Verify Realtime `response.create` occurs only after a valid transcript is available.
- [ ] Verify generated assistant transcript and audio come from the same Realtime response.
- [ ] Verify no `/api/audio/speech` request is made for generated Realtime assistant turns.

## Realtime transcript replacement and feedback

- [ ] During Realtime, speak one valid active-roleplay turn.
- [ ] Verify `[Voice message]` changes to the actual transcript.
- [ ] Stop/leave Conversation Mode and return to chat review if needed.
- [ ] Verify the Realtime user message is visible as a normal learner message.
- [ ] Verify View feedback is enabled/available for the valid Realtime user message.

## Invalid transcript test

- [ ] In chained voice or Realtime, produce silence, unclear audio, or non-English speech.
- [ ] Expected: retry guidance appears.
- [ ] Expected: invalid transcript message is not feedback-eligible.
- [ ] Expected: invalid transcript does not count as a learner turn.
- [ ] Expected: invalid transcript is excluded from summary.
- [ ] Realtime-specific expected: invalid transcript does not trigger `response.create` or a normal assistant response.

## Awaiting Finish review actions

Reach the final tutor message in A1 Introductions or another guided lesson.

- [ ] Verify final tutor message is shown once.
- [ ] Verify Send disabled.
- [ ] Verify Start recording disabled.
- [ ] Verify Hint disabled.
- [ ] Verify Back disabled.
- [ ] Verify Conversation Mode disabled/stopped.
- [ ] Verify Finish lesson enabled.
- [ ] Verify View feedback enabled on valid user messages.
- [ ] Verify Translate enabled on existing messages.
- [ ] Verify Play voice enabled on existing bot messages.
- [ ] Verify existing bot message Play still uses `/api/audio/speech` and exact visible text.
- [ ] Click Finish lesson.
- [ ] Expected: navigates to summary/history.

## Whole-lesson summary

- [ ] Finish a lesson after several valid learner turns.
- [ ] Verify summary screen appears.
- [ ] Verify summary reflects multiple valid lesson exchanges, not only the last exchange.
- [ ] Verify invalid transcript retry messages are absent from the summary.
- [ ] Verify lesson history is saved.
- [ ] Verify navigation back to topics/home works.

## Free Conversation smoke

- [ ] Open Free Conversation.
- [ ] Verify no context selection is required.
- [ ] Enable Conversation Mode.
- [ ] Expected: realtime starts immediately when backend/config are available.
- [ ] Verify final limit is 30 learner turns.
- [ ] Verify text/chained fallback remains usable when Conversation Mode is off.

## Realtime unavailable and disconnects

- [ ] Run app with backend unavailable or missing OpenAI key.
- [ ] Enable Conversation Mode.
- [ ] Expected: clear unavailable/fallback message; buttons do not get stuck; text/chained fallback remains usable.
- [ ] Start Realtime Conversation Mode with backend available.
- [ ] Toggle Conversation Mode off, use final/Finish, and close the app in separate runs.
- [ ] Expected: normal disconnects log as expected and do not produce unhandled Kestrel close-handshake failures.

## Voice/realtime log checks

- [ ] Normal TTS endpoint logs show `Model=tts-1`.
- [ ] Realtime logs show `Model=gpt-realtime`.
- [ ] Realtime user audio commit logs indicate waiting for transcript before `response.create`.
- [ ] Realtime invalid transcript logs indicate no normal assistant response was created.
- [ ] Manual Play logs indicate exact visible text was used.
