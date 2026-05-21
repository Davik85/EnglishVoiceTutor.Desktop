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
- `200 OK` with list payload (`{ items: [...] }`).
- `503 Service Unavailable` when database storage is unavailable.

## Database unavailable behavior

On storage outages, endpoints return a safe short `503 ServiceUnavailable` body and do not expose stack traces, connection strings, provider internals, or host details.

## Data schema

`lesson_summaries` stores dedicated semantic fields for summary content:

- `summary` (required)
- `strengths` (nullable)
- `improvements` (nullable)
- `vocabulary` (nullable)
- `grammar` (nullable)
- `next_steps` (nullable)
- `created_at` (required)
- `updated_at` (required, updated on each upsert)

No semantic workaround mapping is used in endpoint persistence.

## Current status and limitations

- Backend summary persistence is implemented.
- Desktop normal Lesson Chat summary persistence is implemented as best-effort when a backend session id exists.
- Daily limits, auth/JWT, and billing/subscription enforcement are not implemented.
- `feedback_results` persistence is not connected yet.
