# Lesson History Endpoints

Review date: 2026-05-23.

## Status

- **Implemented + Validated:** lesson history list/detail endpoints.
- **Development-only:** current route namespace remains `/api/dev/*` for local diagnostics.
- **Transitional product behavior:** runtime identity is auth-aware even on dev endpoints.

## Endpoints

### GET `/api/dev/lesson-history`
Returns recent lesson sessions for the resolved runtime user.

### GET `/api/dev/lesson-history/{sessionId}`
Returns lesson session detail for the resolved runtime user, including:
- session metadata
- messages
- optional summary
- `feedbackResults` (when available)

## Runtime identity resolution (Development)

- If Bearer token is present and valid, endpoints resolve the authenticated user.
- Without token, endpoints use Development dev-user fallback.
- Authenticated and dev histories remain isolated.

## Response behavior

- `200 OK` on success.
- `404 Not Found` when session is not visible to resolved user.
- `503 Service Unavailable` when storage is unavailable (safe short error body).

## Known limitations / future hardening

- Dev endpoints remain available for local diagnostics.
- Production auth enforcement for all runtime endpoints is not enabled yet.
- Future production API naming should move away from `/api/dev` for authenticated user-facing history.
