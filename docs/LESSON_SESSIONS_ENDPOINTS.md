# Dev Lesson Session Endpoints

These endpoints add the first backend-only session history layer for lesson runs.

## Scope

- Temporary dev user only (same identity source as `/api/dev/user-settings`).
- Backend-only in this phase.
- Desktop Lesson Chat is **not** connected to these endpoints yet.
- Lesson messages are **not** saved yet.
- Feedback is **not** saved yet.
- Summary is **not** saved yet.
- Usage/cost logs are **not** saved yet (session `estimatedCost` currently starts at `0`).

## Routes

- `POST /api/dev/lesson-sessions`
- `PUT /api/dev/lesson-sessions/{sessionId}/finish`
- `GET /api/dev/lesson-sessions`
- `GET /api/dev/lesson-sessions/{sessionId}`

## Temporary dev user behavior

- User id is resolved from `DevUserProvider`.
- If the dev user row does not exist, backend creates a minimal user row once:
  - `email = dev-user@local.test`
  - `status = active`

## Status values

- `Active`
- `Finished`

## modeUsed values

- `text`
- `normal_voice`
- `conversation_mode`
- `realtime_future`

## Request/response behavior

### `POST /api/dev/lesson-sessions`
Creates a new active lesson session.

Validation:
- `lessonContentId` required
- `studyLanguage` required and must be supported study language
- `topicId` required
- `topicTitle` required
- `subtopicId` required
- `subtopicTitle` required
- `level` required
- `modeUsed` required and must be one of supported mode values

Returns:
- `201 Created` + lesson session payload
- `400 Bad Request` for validation errors
- `503 Service Unavailable` when database storage is unavailable

### `PUT /api/dev/lesson-sessions/{sessionId}/finish`
Marks an existing dev-user session as finished.

Validation:
- `validTurnCount >= 0`

Returns:
- `200 OK` + updated lesson session payload
- `400 Bad Request` for validation errors
- `404 Not Found` if session does not exist for dev user
- `503 Service Unavailable` when database storage is unavailable

### `GET /api/dev/lesson-sessions`
Returns recent sessions for the dev user.

Returns:
- `200 OK` + `{ items: [...] }`
- `503 Service Unavailable` when database storage is unavailable

Notes:
- Results are sorted by newest first.
- Maximum recent sessions returned: `50`.

### `GET /api/dev/lesson-sessions/{sessionId}`
Returns one session by id for the dev user.

Returns:
- `200 OK` + lesson session payload
- `404 Not Found` if missing for dev user
- `503 Service Unavailable` when database storage is unavailable

## Database unavailable behavior

All lesson-session endpoints return a safe short `503` JSON error body when storage is unavailable.
No stack trace or provider internals are returned to clients.
