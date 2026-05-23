# Data Retention and Storage Policy (Draft)

Review date: 2026-05-23.

This is a technical MVP retention policy draft (not a legal policy).

## Implemented persistence scope

Backend persistence foundation (PostgreSQL + EF Core) is implemented for:
- `users`
- `user_profiles`
- `user_settings`
- `lesson_sessions`
- `lesson_messages`
- `lesson_summaries`
- `usage_events`
- `daily_usage_counters`
- `feedback_results`

## Stored now (MVP)

- Lesson messages/transcript text may be stored as learning history.
- Lesson summaries may be stored as learning history.
- Feedback results may be stored as learning history.
- Usage event metadata and daily aggregated counters may be stored.

## Sensitive/auth data handling

- Passwords are stored only as backend password hashes.
- JWT tokens are not stored in backend persistence tables.
- Desktop `auth-session.json` is temporary MVP local token storage and must be hardened/replaced before production.

## Not stored now (MVP)

- Raw audio is not persisted.
- Full prompts are not persisted.
- Provider payloads are not persisted.
- Secrets/API keys are not persisted.

## Development free-limit note

- Development can run diagnostics-only free-limit mode (`FreeLimits:EnforcementEnabled=false`).
- In this mode, counters still increment while lesson actions are not blocked.

## Future work (not implemented)

- Production-wide auth enforcement for all runtime endpoints
- Roles/authorization layers
- Subscription/payment enforcement
- CMS/admin workflow
- Contabo deployment
