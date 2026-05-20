# Lesson Summaries Backend Endpoints (Dev)

These endpoints persist and read lesson summaries for existing **dev lesson sessions**.

Summaries are the primary long-term learning artifact because they capture practice outcomes, improvement areas, and next actions that are reused across later lessons.

## Temporary dev-user behavior

All endpoints use the current temporary backend dev user provider. A summary is only accessible if the underlying lesson session belongs to that dev user.

## Endpoints

### PUT `/api/dev/lesson-sessions/{sessionId}/summary`
Upserts the summary for an existing dev lesson session.

- If the session has no summary row yet, one is created.
- If the session already has a summary row, that row is updated.
- Does **not** create a lesson session.

Responses:
- `200 OK` with summary payload.
- `400 Bad Request` when `summary` is empty.
- `404 Not Found` when the session does not exist for the dev user.
- `503 Service Unavailable` when database storage is unavailable.

### GET `/api/dev/lesson-sessions/{sessionId}/summary`
Reads the summary for an existing dev lesson session.

Responses:
- `200 OK` with summary payload.
- `404 Not Found` when the session is missing for the dev user or no summary exists.
- `503 Service Unavailable` when database storage is unavailable.

### GET `/api/dev/lesson-summaries`
Returns recent summaries for the dev user.

- Ordered by newest first.
- Limited to recent items.

Responses:
- `200 OK` with list payload.
- `503 Service Unavailable` when database storage is unavailable.

## Database unavailable behavior

On storage outages, endpoints return a safe short `503 ServiceUnavailable` body and do not expose stack traces, connection strings, provider internals, or host details.

## Current limitations

- Desktop Summary button/flow is **not connected** to these endpoints yet.
- Summary generation behavior is **unchanged** in this step.
- Feedback persistence is **not connected** yet.
- Usage/cost persistence is **not connected** yet.
