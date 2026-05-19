# Dev Lesson Message Endpoints

This document describes backend-only dev endpoints for storing and reading lesson messages for an existing lesson session.

## Scope

- Backend-only change.
- Desktop Lesson Chat is **not** connected to these endpoints yet.
- Feedback, lesson summary, and usage/cost logs are still **not saved** by this feature.

## Temporary dev user behavior

These endpoints use the existing temporary `DevUserProvider` behavior.

- A message can be saved only when the target lesson session exists and belongs to the dev user.
- If the session does not exist for the dev user, the API returns `404 Not Found`.

## Endpoints

## POST `/api/dev/lesson-sessions/{sessionId}/messages`

Creates one message for an existing dev-user lesson session.

### Request body

```json
{
  "role": "user",
  "text": "Hello, my name is David.",
  "source": "typed",
  "turnNumber": 1,
  "isValidLessonTurn": true,
  "studyLanguage": "English",
  "transcriptConfidence": null,
  "audioDurationMs": null
}
```

### Success response

- `201 Created` with the created `LessonMessageResponse`.

### Validation rules (`400 Bad Request`)

- `role` is required and must be supported.
- `text` is required.
- `source` is required and must be supported.
- `turnNumber` must be `0` or greater.
- `studyLanguage` is required and must be supported.
- `transcriptConfidence` (if provided) must be between `0` and `1`.
- `audioDurationMs` (if provided) must be `0` or greater.

## GET `/api/dev/lesson-sessions/{sessionId}/messages`

Lists messages for an existing dev-user lesson session.

### Success response

- `200 OK` with:

```json
{
  "items": [
    {
      "id": "...",
      "sessionId": "...",
      "role": "user",
      "text": "Hello, my name is David.",
      "source": "typed",
      "turnNumber": 1,
      "isValidLessonTurn": true,
      "studyLanguage": "English",
      "transcriptConfidence": null,
      "audioDurationMs": null,
      "createdAt": "2026-05-19T00:00:00Z"
    }
  ]
}
```

Results are ordered by `turnNumber`, then `createdAt`.

## Supported values

### role

- `user`
- `assistant`
- `system`

### source

- `typed`
- `voice_transcript`
- `bot_reply`
- `hint`
- `setup`
- `context_selection`
- `summary`

## Valid turn behavior

- Minimum turn number is `0`.
- `isValidLessonTurn` is accepted as provided by the caller.
- The shared default constant for `isValidLessonTurn` is `false`.

## Storage unavailable behavior

If the database is unavailable, endpoints return:

- `503 Service Unavailable`
- A safe short `ErrorResponse` payload
- No stack trace or database internals
