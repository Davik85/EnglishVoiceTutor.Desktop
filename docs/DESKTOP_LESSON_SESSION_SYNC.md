# Desktop lesson session sync

Review date: 2026-06-01.

## Scope

Normal Lesson Chat is connected to backend lesson session, lesson message, summary, history, and active-lesson guard APIs. The backend remains the source of truth for lesson history and active lesson state.

## Backend-required behavior

The packaged desktop app requires a reachable backend for lesson start, backend lesson history, lesson-bound message persistence, AI bot replies, voice transcription/STT, TTS, translation, hints, feedback, summary, subscription/access checks, active lesson guard, and remote active lesson release.

Backend-unavailable testing is resilience-only. A stopped or unreachable backend should not crash the app, but users should not expect login, lesson start, Send, Hint, Translate, Play voice/TTS, transcription, Conversation Mode, Finish lesson, Summary generation, lesson history, active lesson guard, or remote release to succeed without backend APIs.

## Session creation and finish

- Desktop attempts to create a backend lesson session when normal Lesson Chat opens.
- Normal signed-in lesson flows use the authenticated `/api/me` lesson session routes.
- Development fallback routes remain for local diagnostics where explicitly supported.
- Desktop attempts to finish the backend lesson session when the lesson is finished through the normal lesson flow.
- Closing the app or leaving during an active lesson is not treated as normal completion; shutdown cleanup attempts best-effort active lesson release and heartbeat timeout remains the fallback.

## Active lesson guard

- Backend enforces one active lesson per account.
- Desktop and future mobile clients must follow the same backend rule.
- Lesson Chat sends heartbeat calls for the active backend lesson session about every 30 seconds.
- Backend treats an active lesson as blocking only while its heartbeat is fresh; current freshness window is 2 minutes.
- Stale active lessons no longer block forever; after the freshness window, a new lesson can start and the old session is preserved as `Abandoned` history.
- The user can end an active lesson on another device and continue.
- Remote release marks the old active session `Abandoned`.
- Old devices/sessions cannot continue after remote release.
- Old heartbeat and old lesson-bound message creation are rejected with `lesson_session_ended_elsewhere`.
- UI wording for this flow must stay neutral and must not use fraud language.

## Current payload notes

- `ModeUsed` is sent as `text`, `normal_voice`, or `conversation_mode` according to the active lesson flow where available.
- `ValidTurnCount` is sent from the lesson turn count tracked by the desktop ViewModel.
- `StudyLanguage` uses the current study language canonical English name.
- `LessonContentId` uses scenario id when available, otherwise a stable fallback id from topic/subtopic/level.
- At lesson open time, selected context may not yet be finalized; if context is not selected yet, `SelectedContextId` and `SelectedContextTitle` are sent as `null`.

## Lesson messages persistence

Desktop normal Lesson Chat sends lesson messages to backend `lesson_messages` for the active backend lesson session.

- User lesson answers are saved with `role=user`.
- Assistant bot replies are saved with `role=assistant`.
- User typed messages are saved with `source=typed`.
- User voice-transcribed messages are saved with `source=voice_transcript` when distinguishable.
- Assistant replies are saved with `source=bot_reply`.
- Setup/context selection messages are not saved as valid lesson turns.

## Lesson summary persistence

Desktop normal Lesson Chat saves the visible lesson summary to backend `lesson_summaries` for the active backend lesson session.

- Summary save is best-effort and non-blocking.
- Summary persistence is attempted only when a backend lesson session id is available.
- A remotely released or abandoned lesson is not treated as a normally completed lesson and does not create a normal completion summary.

## Validation

Relevant validation scripts/docs:

- `tools/run_desktop_release_gate.ps1`
- `tools/smoke_single_active_lesson_guard.ps1`
- `docs/desktop-release-smoke-gate.md`
- `docs/TESTER_RELEASE.md`
