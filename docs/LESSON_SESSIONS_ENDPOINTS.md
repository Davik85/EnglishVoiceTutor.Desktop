# Lesson Session Endpoints

Review date: 2026-07-08.

## Status

- **Implemented + Validated:** create/finish/read session endpoints and the authenticated mobile lesson-session reply placeholder route.
- **Development-only:** legacy diagnostic routes remain under `/api/dev/*`.
- **Production mobile contract:** `POST /api/me/lesson-sessions/{sessionId}/reply` is authenticated and backend-owned, but is intentionally not AI-enabled yet.
- **Transitional product behavior:** request identity is auth-aware in Development for dev routes.

## Routes

### Authenticated user-facing routes

- `POST /api/me/lesson-sessions/{sessionId}/reply`

### Development / diagnostic routes

- `POST /api/dev/lesson-sessions`
- `PUT /api/dev/lesson-sessions/{sessionId}/finish`
- `GET /api/dev/lesson-sessions`
- `GET /api/dev/lesson-sessions/{sessionId}`

## Mobile lesson-session text reply placeholder

`POST /api/me/lesson-sessions/{sessionId}/reply` is the new backend-owned mobile text reply contract. Mobile clients should send only the route `sessionId` plus this request body:

```json
{
  "messageText": "..."
}
```

Current production behavior is deliberately safe placeholder mode. The endpoint authenticates the caller, verifies that the session exists and belongs to the user, verifies that the session is still active, checks the existing lesson/chat reply limits where applicable, and then returns a controlled `409 Conflict` for otherwise valid active sessions instead of calling AI:

```json
{
  "error": "mobile_lesson_reply_not_implemented",
  "errorCode": "mobile_lesson_reply_not_implemented",
  "code": "mobile_lesson_reply_not_implemented",
  "message": "Mobile lesson text replies are not available yet. Please continue this lesson in a supported client.",
  "sessionId": "..."
}
```

Other response behavior:

- `400 Bad Request` for blank `messageText`.
- `404 Not Found` when the session is missing or not owned by the authenticated user.
- `409 Conflict` with the existing session-ended payload for inactive/ended sessions.
- `429 Too Many Requests` with the existing free/rate-limit payload if the chat reply limit is exceeded.
- `503 Service Unavailable` with the existing lesson-session storage unavailable payload.

Architecture boundary:

- Mobile must **not** call `POST /api/lesson-chat/reply` directly. That endpoint remains desktop-owned and still expects the large desktop-built `LessonChatRequest`.
- Mobile must **not** send desktop prompt, runtime, scenario, or turn-management payloads.
- Mobile must **not** call OpenAI directly. Provider access remains backend-only.
- This endpoint is not real mobile AI chat yet; it is the future mobile text-reply contract in safe placeholder mode.
- The next implementation direction is for the backend to hydrate lesson runtime/server-side context before enabling AI replies. Mobile should continue to send only `sessionId` and `messageText`, and should not duplicate desktop prompt/scenario/turn logic.

Production deployment note: backend `0.1.35-backend.110` deployed this placeholder contract without adding or running an EF/database migration. The old `POST /api/lesson-chat/reply` desktop endpoint was not changed.

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
- Login exists but is not required for Lesson Chat in product.
- Production auth enforcement is not enabled for all runtime endpoints yet.
- Future production API naming should move away from `/api/dev` for authenticated user-facing session APIs.
- Subscription/payment enforcement is not implemented.
