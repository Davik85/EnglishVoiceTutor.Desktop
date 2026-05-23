# Lesson Session Endpoints

Review date: 2026-05-23.

## Status

- **Implemented + Validated:** create/finish/read session endpoints.
- **Development-only:** current routes are under `/api/dev/*`.
- **Transitional MVP behavior:** request identity is auth-aware in Development.

## Routes

- `POST /api/dev/lesson-sessions`
- `PUT /api/dev/lesson-sessions/{sessionId}/finish`
- `GET /api/dev/lesson-sessions`
- `GET /api/dev/lesson-sessions/{sessionId}`

## Runtime identity resolution (Development)

- With valid Bearer token: resolve authenticated user.
- Without token: use dev-user fallback.
- Authenticated and dev session history/counters remain isolated.

## Request/response summary

- `POST` creates an active session.
- `PUT .../finish` marks a session finished.
- `GET` list returns recent sessions (newest first, capped).
- `GET` detail returns one session for resolved user.

Common responses:
- `200 OK` / `201 Created` on success.
- `400 Bad Request` for validation errors.
- `404 Not Found` when session is not visible to resolved user.
- `503 Service Unavailable` when storage is unavailable.

## Known limitations / future hardening

- Dev endpoints remain available for local diagnostics.
- Login exists but is not required for Lesson Chat in MVP.
- Production auth enforcement is not enabled for all runtime endpoints yet.
- Future production API naming should move away from `/api/dev` for authenticated user-facing session APIs.
- Subscription/payment enforcement is not implemented.
