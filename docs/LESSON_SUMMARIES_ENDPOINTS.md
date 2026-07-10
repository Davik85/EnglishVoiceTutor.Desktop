# Lesson Summaries Endpoints

Lesson summaries are backend-owned learner feedback. Authenticated desktop and mobile clients finish through `PUT /api/me/lesson-sessions/{sessionId}/finish` and read the learner-safe result through `GET /api/me/lesson-sessions/{sessionId}/summary`. Clients do not author or upload summary content. The `/api/dev/.../summary` routes below remain development diagnostics and are not production/mobile contracts.

Review date: 2026-07-10.

## Status

- **Implemented + Validated:** authenticated backend-owned finish and learner-safe summary read flow.
- **Development-only:** `/api/dev/*` summary upsert/read/list persistence remains diagnostic only.
- **Transitional product behavior:** runtime identity is auth-aware in Development.

## Production endpoints

- `PUT /api/me/lesson-sessions/{sessionId}/finish`
- `GET /api/me/lesson-sessions/{sessionId}/summary`

The finish request remains backward compatible, for example:

```json
{
  "validTurnCount": 1
}
```

Finish is idempotent. The backend generates and persists summaries from persisted lesson messages and safe lesson/session metadata. Summary generation failure does not undo a successfully completed lesson. Summary GET may return `ready` with learner-safe fields or `unavailable`. Production clients, including desktop and mobile, must not upload `summary`, `strengths`, `improvements`, `vocabulary`, `grammar`, or `nextSteps`.

## Development / diagnostic endpoints

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
- Subscription/payment enforcement is not implemented.
