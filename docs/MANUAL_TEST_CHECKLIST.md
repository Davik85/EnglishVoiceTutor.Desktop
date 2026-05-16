# Manual Test Checklist

Review date: 2026-05-16.

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

## 2026-05-15 Realtime GA recovery and English-only checks

- [ ] Start Conversation Mode and verify backend logs do not contain `Realtime Beta API is no longer supported`.
- [ ] Verify Realtime connects through GA `/v1/realtime`, sends no `OpenAI-Beta` header, and logs a sanitized `session.update` shape containing `audio.input.format.rate` and `audio.output.format.rate`.
- [ ] Force a Realtime startup failure and verify the user sees a clear failure message, Conversation Mode becomes clickable again, Record is not shown as ready, and text Lesson Chat still works.
- [ ] Start/stop Conversation Mode three times and verify no overlapping sessions or stale events corrupt the active state.
- [ ] In normal Lesson Chat, send `Speak Finnish.` and verify the tutor answers in English, for example `Let's practice in English. What's your name?`.
- [ ] In Realtime, say `Speak Finnish.` and verify the tutor replies in English and does not speak Finnish.
- [ ] Click Translate on an existing message and verify translation works without changing tutor lesson language.

- [ ] Verify a broken Realtime schema is fatal but recoverable: backend sends `session.startup_failed`, desktop returns Conversation Mode to NotStarted/Faulted retry state, the Conversation Mode button is clickable, the Record button remains disabled until a later `session.ready`, and normal text Lesson Chat still works.

## 2026-05-15 Realtime GA content-part regression checks

- [ ] Start Conversation Mode after recent assistant messages exist and verify backend logs show `ContentPartTypes=output_text` for assistant seed items and never `ContentPartTypes=text`.
- [ ] Verify `response.create` logs `OutputModalities=audio` and does not include any content item with type `text`.
- [ ] Verify no backend log contains `Invalid value: 'text'. Value must be 'output_text'.`.
- [ ] Force a post-ready Realtime upstream error and verify desktop receives a recoverable runtime failure, stops microphone/playback, clears busy state, and allows Conversation Mode re-entry without app restart.
- [ ] Start Conversation Mode while normal bot voice playback or auto-play is pending and verify normal TTS is canceled/suppressed during Realtime startup; manual Play voice still works after leaving Realtime.

## 2026-05-15 Conversation Mode opening playback and fallback checks

- [ ] A1 Beginner → Everyday English → Introductions → Meeting a new neighbor: click Conversation Mode and verify the visible prompt is spoken before recording is enabled.
- [ ] Verify backend `/api/audio/speech` logs show `Model=tts-1` and `Purpose=realtime_pre_start_opening` for the opening prompt.
- [ ] Verify the opening prompt is not duplicated in chat and no Realtime `response.create` is sent for that opening.
- [ ] Verify the record button is disabled during `OpeningPlayback` and enabled after playback completes or after a simulated playback failure.
- [ ] Record `My name is David.` and verify the placeholder becomes the transcript, feedback is available, and the tutor reply arrives through Realtime audio rather than `/api/audio/speech`.
- [ ] Record short A1 answers such as `Russia.`, `Moscow.`, `Yes.`, and `No.` and verify recognized transcripts count as valid learner turns.
- [ ] Record silence/noise and verify `[Voice message]` is replaced with a retry/status message, no feedback button appears, the learner turn count does not increment, and no assistant response is created.
- [ ] Force a Realtime transcript timeout/failure with committed audio and verify exactly one fallback transcription attempt is logged; a valid fallback transcript is used once with no duplicate user messages or assistant responses.
- [ ] Leave Conversation Mode and verify normal Lesson Chat recording still uses `/api/audio/transcribe` and normal Play voice still uses `tts-1`.

## 2026-05-15 Avatar tooltip and freeze diagnostics checks

- [ ] Hover over the tutor avatar in Lesson Chat.
- [ ] Expected: no technical asset path, file path, image file name, or avatar asset metadata is shown.
- [ ] Expected: the avatar image/animation still renders normally.
- [ ] If the desktop app freezes or hangs, collect recent frontend Debug logs and backend logs around Conversation Mode enter/exit, `realtime_pre_start_opening` TTS playback, Play voice/auto-play, and normal/Realtime recording start/stop.
- [ ] Check frontend logs for `UI operation start`, `UI operation end`, and `UI operation warning` entries with operation names and durations.

## Short post-cleanup smoke checklist

Run this after documentation or low-risk cleanup changes before starting feature work:

1. Normal Lesson Chat: open A1 Introductions, send `My name is David.`, verify the tutor response, click Play voice, and click View feedback.
2. Normal voice: use Start recording outside Conversation Mode, verify the transcript appears, and verify the lesson response is generated.
3. Realtime: enter Conversation Mode, verify the opening prompt plays with `purpose=realtime_pre_start_opening`, record one answer, verify the transcript appears, verify Realtime assistant audio plays, then exit and re-enter Conversation Mode.
4. Finish lesson: click Finish lesson after multiple valid learner turns and verify the summary is based on the lesson conversation.
5. UI: hover over the avatar and verify no technical asset path appears.
