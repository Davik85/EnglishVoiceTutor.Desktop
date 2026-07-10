# Lesson Summaries Endpoints

Lesson summaries are backend-owned learner feedback. Authenticated desktop and mobile clients finish through `PUT /api/me/lesson-sessions/{sessionId}/finish` and read the learner-safe result through `GET /api/me/lesson-sessions/{sessionId}/summary`. Clients do not author or upload summary content. The `/api/dev/.../summary` routes below remain development diagnostics and are not production/mobile contracts.

Review date: 2026-05-23.

## Status

- **Implemented + Validated:** summary upsert/read/list persistence.
- **Development-only:** current route namespace is `/api/dev/*`.
- **Transitional product behavior:** runtime identity is auth-aware in Development.

## Endpoints

- `PUT /api/dev/lesson-sessions/{sessionId}/summary`
- `GET /api/dev/lesson-sessions/{sessionId}/summary`
- `GET /api/dev/lesson-summaries`

## Runtime identity resolution (Development)

- With valid Bearer token: resolve authenticated user.
- Without token: use dev-user fallback.
- Authenticated and dev summary history remain isolated.

## Behavior summary

- PUT upserts summary for existing lesson session.
- GET by session returns summary for that session when visible to resolved user.
- GET list returns recent summaries for resolved user.

Common responses:
- `200 OK` on success.
- `400 Bad Request` when summary content is invalid/empty (PUT).
- `404 Not Found` when session/summary is not visible to resolved user.
- `503 Service Unavailable` when storage is unavailable.

## Known limitations / future hardening

- Dev endpoints remain available for local diagnostics.
- Production auth enforcement for all runtime endpoints is not enabled yet.
- Future production API naming should move away from `/api/dev` for authenticated user-facing summary APIs.
- Subscription/payment enforcement is not implemented.
