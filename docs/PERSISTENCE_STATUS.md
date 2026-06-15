# Persistence Status Checklist

Review date: 2026-06-15.

## Implemented and validated

- PostgreSQL + EF Core persistence foundation is active.
- `lesson_sessions`, `lesson_messages`, and `lesson_summaries` persist correctly.
- `feedback_results` persist correctly.
- `usage_events` and `daily_usage_counters` persist correctly (including `chat_reply_count`).
- Lesson history detail includes messages, summary, and `feedbackResults` where available.

## Runtime identity behavior (auth-aware)

**Implemented + Validated**
- Runtime persistence is auth-aware:
  - without token in Development -> dev-user fallback
  - with Bearer token -> authenticated user
- Authenticated-user and dev-user counters/history are isolated.
- Request user resolution order:
  1. authenticated JWT user
  2. Development dev-user fallback when no token

## Dev read endpoints (current MVP)

**Implemented + Development-only**
- `GET /api/dev/lesson-history`
- `GET /api/dev/lesson-history/{sessionId}`
- `GET /api/dev/usage-events`
- `GET /api/dev/daily-usage-counters`
- `GET /api/dev/free-limit-status`
- `GET /api/dev/feedback-results`

Dev endpoints remain available for local diagnostics.

## Free-limit mode

**Development-only + Validated**
- Diagnostics-only mode in Development: `FreeLimits:EnforcementEnabled=false`.
- Counters and diagnostics remain active.
- Lesson Chat / Hint / STT / TTS are not blocked in this mode.

## Device tracking

**Implemented + Validated**
- Device tracking is privacy-safe and authenticated.
- It counts coarse backend `DeviceEntity` rows, not installer downloads or raw install events.
- Tracked-device identity is same user + platform + coarse device name; `AppVersion` is stored as latest seen app version and is not part of identity.
- Repeated app updates for the same user/platform/coarse device should update `LastSeenAt` and latest `AppVersion` rather than create a new row.
- No raw hardware identifiers, machine fingerprints, serial numbers, MAC addresses, Windows usernames, IP addresses, or personal device IDs are collected.

## Data safety constraints

**Implemented + Validated**
- Raw audio is not persisted.
- Full prompts are not persisted.
- Provider payloads are not persisted.
- Secrets/API keys are not persisted.
- JWT tokens are not persisted in backend tables.
- Passwords are not persisted as plain text.
- `auth-session.json` is desktop-local MVP token storage and is not backend persistence.

## Future work

- Enable production-grade auth enforcement for runtime endpoints.
- Add billing/subscription enforcement later.
- Add CMS/admin later (after roles and content workflow hardening).
