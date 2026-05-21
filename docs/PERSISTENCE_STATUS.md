# Persistence Status Checklist

## Implemented tables/features

- [x] PostgreSQL + EF Core persistence foundation is active.
- [x] `users`
- [x] `user_settings`
- [x] `lesson_sessions`
- [x] `lesson_messages`
- [x] `lesson_summaries`
- [x] `usage_events`
- [x] `daily_usage_counters`
- [x] `daily_usage_counters.chat_reply_count` (EF migration `20260520150000_AddDailyUsageChatReplyCount`)
- [x] Backend aggregates successful usage events into daily counters by `(user, usageDate, studyLanguage)`.

## Implemented dev endpoints

- [x] Health endpoint: `GET /health`
- [x] Database health endpoint: `GET /api/health/database`
- [x] User settings endpoints (`/api/dev/user-settings`)
- [x] Lesson sessions endpoints (`/api/dev/lesson-sessions`)
- [x] Lesson summaries endpoints (`/api/dev/lesson-sessions/{sessionId}/summary`, `/api/dev/lesson-summaries`)
- [x] Lesson history endpoints (`/api/dev/lesson-history`, `/api/dev/lesson-history/{sessionId}`)
- [x] Dev diagnostics endpoints for usage/counters (`/api/dev/usage-events`, `/api/dev/daily-usage-counters`)

## Desktop integrations already connected

- [x] Desktop diagnostics checks backend + database health endpoints.
- [x] Desktop lesson session create/finish sync to backend (best effort).
- [x] Desktop lesson message persistence to backend (best effort).
- [x] Desktop lesson summary persistence to backend (best effort).
- [x] Desktop Lesson History reads backend list first, with local JSON fallback.

## Data handling clarifications

- [x] `usage_events` stores aggregate metadata (operation/model/studyLanguage/status/timing/cost), not raw audio.
- [x] `usage_events` does not store full prompts, full provider payloads, API keys, or secrets.
- [x] `lesson_chat_reply` increments `chatReplyCount` in `daily_usage_counters`.
- [x] `lessonsStarted` / `lessonsCompleted` are reserved for future lesson lifecycle counters.

## Pending backend features

- [ ] Daily limit enforcement (free-tier runtime enforcement).
- [ ] Subscription/billing runtime enforcement.
- [ ] Auth/JWT and production user identities.
- [ ] `feedback_results` persistence wiring (if still disconnected in runtime flow).
- [ ] Content versioning workflow.
- [ ] CMS/admin backend role workflows.

## Pending desktop features

- [ ] Mobile sync.
- [ ] Broader history/detail UX integration beyond current list-first backend read.

## Pending release/security features

- [ ] Server deployment to Contabo.
- [ ] Production auth/roles hardening for admin/content management.
- [ ] CMS/admin panel (after auth + roles + content versioning).

## Recommended next steps

1. Verify `daily_usage_counters` endpoint after `ChatReplyCount` migration.
2. Add read-only free-limit diagnostics endpoint.
3. Add soft free-limit enforcement later.
4. Add `feedback_results` persistence.
5. Add auth/JWT and real accounts.
6. Add subscription/payment enforcement.
7. Add CMS/admin panel only after roles/content versioning.

## Notes

- DBeaver is for local developer inspection only; it is not a CMS/admin surface.
