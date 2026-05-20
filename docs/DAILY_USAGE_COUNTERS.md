# Daily Usage Counters

## Purpose

`daily_usage_counters` aggregates `usage_events` by:
- user
- UTC usage date
- study language

These counters are designed to support future:
- free-tier daily limits
- usage analytics
- cost controls

Daily limits are **not enforced yet**.

## Runtime behavior

- `chatReplyCount` requires EF migration `AddDailyUsageChatReplyCount` (`20260520150000_AddDailyUsageChatReplyCount`) to be applied in the database.
- Usage event logging is **implemented** and remains best-effort.
- Daily counter aggregation is **implemented** and remains best-effort.
- If daily counter update fails, the main user request still succeeds.
- Only usage events with `status = success` increment operation counters.
- `estimatedCost` is aggregated into daily estimated cost.

## Counter dimensions and fallback behavior

- Date key uses UTC date derived from usage event `createdAt`.
- Study language uses usage event `studyLanguage` when available.
- If study language is missing, fallback value is `unknown`.

## Current operation mapping

- `lesson_chat_reply` increments `chatReplyCount`
- `lesson_chat_hint` increments `hintsUsed`
- `lesson_chat_feedback` increments `feedbackRequests`
- `audio_transcription` adds to `transcriptionSeconds`
- `tts` adds to `ttsSeconds`

`lessonsStarted` and `lessonsCompleted` are reserved for lesson session lifecycle counters (session start/finish) and are not incremented by chat replies.

## Dev verification endpoint

`GET /api/dev/daily-usage-counters`

- Returns latest 50 rows for the dev user.
- For local development verification only.
- Returns safe structured data only.
- Does not include secrets, raw audio, full prompts, or full provider payloads.

## Out of scope

- Runtime enforcement of daily limits
- CMS/admin UI
- Subscription or billing runtime enforcement logic
