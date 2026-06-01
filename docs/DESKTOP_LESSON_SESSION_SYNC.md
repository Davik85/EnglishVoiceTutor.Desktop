# Desktop lesson session sync (dev endpoints)

## Scope

This implementation connects **normal Lesson Chat** in the desktop app to backend dev `lesson_sessions` endpoints.

- Included: create and finish lesson session rows.
- Not included: lesson messages, feedback, summary persistence, usage/cost logs.
- Conversation Mode is not directly connected in this task unless it flows through normal Lesson Chat finish behavior.

## Behavior

- Desktop attempts to create a backend lesson session when normal Lesson Chat opens.
- Desktop attempts to finish the backend lesson session when lesson is finished or when user leaves via back navigation.
- Session-tracking calls are best-effort and non-blocking for the UI once a backend-authorized lesson flow is active.
- Backend-unavailable testing is resilience-only. A stopped or unreachable backend should not crash the app, but users should not expect login, lesson start, Send, Hint, Translate, Play voice/TTS, transcription, Conversation Mode, Finish lesson, or Summary generation to succeed without backend APIs.
- Lesson Chat status near the lesson title refers to **lesson history sync/session tracking** state (for example: `History sync: active`), not overall backend health.
- Backend/Database/AI health should be checked in **Settings Diagnostics**.
- If history sync is unavailable while the backend-running lesson flow is otherwise available, history sync should fail gracefully without breaking the current UI state.

## Current payload notes

- `ModeUsed` is sent as `"text"` for normal Lesson Chat tracking.
- `ValidTurnCount` is sent from `LearnerTurnCount` in `LessonChatViewModel`.
- `StudyLanguage` uses the current study language canonical English name.
- `LessonContentId` uses scenario id when available, otherwise a stable fallback id from topic/subtopic/level.

## Context selection limitation

At lesson open time, selected context may not yet be finalized.

- If context is not selected yet, `SelectedContextId` and `SelectedContextTitle` are sent as `null`.
- No update-context endpoint is added in this task.

## Auth/user note

Dev lesson session endpoints currently use temporary dev user behavior on backend side until auth is introduced.

## Lesson messages persistence (Normal Lesson Chat)

Desktop normal Lesson Chat now sends lesson messages to backend `lesson_messages` for the active backend lesson session.

- User lesson answers are saved with `role=user`.
- Assistant bot replies are saved with `role=assistant`.
- User typed messages are saved with `source=typed`.
- User voice-transcribed messages are saved with `source=voice_transcript` when the message source is distinguishable in the ViewModel.
- Assistant replies are saved with `source=bot_reply`.
- Setup/context selection messages are not saved as valid lesson turns in this task.
- Feedback, summary, and usage/cost logs are still not saved in this task.

## Lesson summary persistence (Normal Lesson Chat)

Desktop normal Lesson Chat now saves the visible lesson summary to backend `lesson_summaries` for the active backend lesson session.

- Summary save is best-effort and non-blocking.
- If the current desktop summary is plain text, only `summary` is persisted and optional semantic fields (`strengths`, `improvements`, `vocabulary`, `grammar`, `next_steps`) are sent as `null`.
- Summary persistence is attempted only when a backend lesson session id is available.
- Feedback results and usage/cost events are still not saved in this task.
- Conversation Mode summary is not connected unless it shares this exact normal Lesson Chat summary lifecycle.
