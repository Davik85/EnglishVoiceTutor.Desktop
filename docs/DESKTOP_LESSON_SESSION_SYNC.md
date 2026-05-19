# Desktop lesson session sync (dev endpoints)

## Scope

This implementation connects **normal Lesson Chat** in the desktop app to backend dev `lesson_sessions` endpoints.

- Included: create and finish lesson session rows.
- Not included: lesson messages, feedback, summary persistence, usage/cost logs.
- Conversation Mode is not directly connected in this task unless it flows through normal Lesson Chat finish behavior.

## Behavior

- Desktop attempts to create a backend lesson session when normal Lesson Chat opens.
- Desktop attempts to finish the backend lesson session when lesson is finished or when user leaves via back navigation.
- Calls are best-effort and non-blocking for local lesson flow.
- If backend is unavailable, lesson still opens and works locally.

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
