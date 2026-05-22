# Persistence Status Checklist

## Implemented now

- PostgreSQL + EF Core persistence foundation is active.
- Runtime persistence is implemented for:
  - `lesson_sessions`
  - `lesson_messages`
  - `lesson_summaries`
  - `usage_events`
  - `daily_usage_counters` (including `chat_reply_count`)
  - `feedback_results` (best-effort runtime save on successful `/api/lesson-chat/feedback` calls)
- Free-limit diagnostics and soft-enforcement wiring are complete for the current dev-user scope:
  - `GET /api/dev/free-limit-status`
  - study language normalization for usage counters
  - configurable backend free-limit enforcement (HTTP 429 before provider calls when enabled)
  - desktop user-friendly HTTP 429 free-limit UX
  - Development defaults to diagnostics-only mode (`FreeLimits:EnforcementEnabled=false`) so local MVP testing is not blocked

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

## Data safety constraints (current)

- No raw audio is stored in persistence tables.
- No full prompts are stored.
- No full provider payloads are stored.
- No API keys or secrets are stored.

## Next recommended backend focus

1. feedback_results persistence wiring validation/observability hardening
2. auth/JWT and real accounts
3. subscription/payment enforcement
4. CMS/admin panel only after auth/roles/content versioning

## Free-limit enforcement mode (MVP)

- Diagnostics are always active: `usage_events`, `daily_usage_counters`, and `GET /api/dev/free-limit-status` continue to track and expose limit status.
- Soft enforcement is controlled by `FreeLimits:EnforcementEnabled`.
- Local Development is configured for diagnostics-only mode (`false`) to prevent HTTP 429 blocks during lesson chat, hints, transcription, and TTS testing.
- Enforcement can be re-enabled later by setting `FreeLimits:EnforcementEnabled=true` for billing/subscription rollout work.
