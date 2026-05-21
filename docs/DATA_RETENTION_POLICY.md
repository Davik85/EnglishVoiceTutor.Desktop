# Data Retention and Storage Policy (Draft)

## Purpose

This document captures the current technical retention posture for backend persistence in EnglishVoiceTutor.

It explains:
- what is stored now
- what is explicitly not stored
- what is implemented vs deferred
- how current storage supports desktop and future sync

This is a product/technical draft, not a legal policy.

## Implemented persistence foundation

The PostgreSQL + EF Core storage foundation is implemented for:
- `users`
- `user_profiles`
- `user_settings`
- `lesson_sessions`
- `lesson_messages`
- `lesson_summaries`
- `usage_events`
- `daily_usage_counters`

Also implemented:
- backend health and database health endpoints
- backend lesson history endpoints
- desktop lesson history backend read with local JSON fallback

## Implemented usage/counter behavior

- `usage_events` persistence is implemented.
- `daily_usage_counters` runtime aggregation is implemented.
- Counters aggregate **successful** usage events by `(user, UTC date, study language)`.
- `lesson_chat_reply` increments `chatReplyCount`.
- `lessonsStarted` and `lessonsCompleted` are reserved for future lesson lifecycle counters.
- Daily limits are **not enforced** yet.

## Not implemented yet

- auth/JWT
- production user accounts
- subscription/billing runtime enforcement
- CMS/admin UI
- content versioning workflow
- mobile sync
- Contabo server deployment
- `feedback_results` runtime persistence wiring (if still disconnected)

## Data minimization rules

By default, do not store:
- raw audio files
- API keys/secrets
- connection strings/passwords in source control
- full prompts/provider payloads unless explicitly required
- stack traces/secrets in API responses

Current usage-event policy:
- store aggregate metadata only (operation/model/studyLanguage/status/duration/cost)
- do not store raw audio, full prompts, provider secrets, or full provider payloads

## Tooling clarification

- DBeaver is for local developer DB inspection only.
- DBeaver is not a CMS/admin panel.
- CMS/admin should come later, after auth/roles/content versioning.
