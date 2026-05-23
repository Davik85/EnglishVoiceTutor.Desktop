# Persistence Status Checklist

## Implemented and validated now

- PostgreSQL + EF Core persistence foundation is implemented and active.
- Runtime persistence is implemented and validated for:
  - `lesson_sessions`
  - `lesson_messages`
  - `lesson_summaries`
  - `usage_events`
  - `daily_usage_counters` (including `chat_reply_count`)
  - `feedback_results`
- `feedback_results` runtime records are linked to `sessionId`/`messageId` after View feedback flow and are validated through dev read endpoints.

## Implemented read endpoints

- `GET /api/dev/lesson-history`
- `GET /api/dev/lesson-history/{sessionId}`
  - session metadata
  - messages
  - summary (optional)
  - feedback results list (`feedbackResults`) with `messageId` references
- `GET /api/dev/usage-events`
- `GET /api/dev/daily-usage-counters`
- `GET /api/dev/free-limit-status`
- `GET /api/dev/feedback-results` (dev diagnostics, safe fields only)

## Free-limit status (MVP)

- Free-limit diagnostics are implemented and active.
- Soft enforcement wiring is implemented and configurable via `FreeLimits:EnforcementEnabled`.
- Development is intentionally diagnostics-only: `FreeLimits:EnforcementEnabled=false`.
- In diagnostics-only mode, counters and diagnostics stay active:
  - `usage_events` still persist.
  - `daily_usage_counters` still increment.
  - `GET /api/dev/free-limit-status` remains usable.
- In diagnostics-only Development mode, lesson chat/hint/STT/TTS are not blocked by HTTP 429.
- Existing HTTP 429 enforcement behavior remains available when `FreeLimits:EnforcementEnabled=true`.

## Data safety constraints (current)

- No raw audio is stored in persistence tables.
- No full prompts are stored.
- No full provider payloads are stored.
- No API keys or secrets are stored.

## Recommended next backend/product order

1. Final small stabilization pass
   - monitor STT quality with real short learner phrases
   - harden TutorIdentityGuard / tutor identity behavior if warnings continue
2. Auth/JWT and real accounts
3. Subscription/payment enforcement
4. CMS/admin panel only after auth, roles, content versioning, draft/published workflow, audit trail, and rollback


## Authenticated settings endpoints

- Auth/JWT foundation is implemented.
- Authenticated user settings endpoints are implemented: `GET /api/me/settings`, `PUT /api/me/settings`.
- Existing dev endpoints remain available for local MVP testing (`/api/dev/user-settings`, `/api/dev/free-limit-status`).
- Desktop login UI is still not implemented.
- Runtime lesson persistence now resolves user from JWT when available and falls back to Development dev-user identity when unauthenticated.
