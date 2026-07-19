# Progress Endpoints

## GET `/api/me/progress`

This authenticated endpoint returns backend-owned learner Progress V1 for the current account. The user is resolved from the bearer token; the route accepts no user ID and never returns another user's data.

Progress is calculated on request from existing `lesson_sessions` records only. It does not use the bounded latest-50 Lesson History response, and Mobile remains a client of this backend-owned contract.

## Completion and calendar rules

A qualifying completed lesson has `status = Finished` and a non-null `finishedAt`. All completion totals, calendar windows, activity, streaks, and last-completed ordering use `finishedAt`.

- Calendar timezone: UTC (`calendarTimezone = "UTC"`).
- Last 7 days: the current UTC date and preceding six UTC dates.
- Last 30 days: the current UTC date and preceding 29 UTC dates.
- A streak day has at least one qualifying completed lesson. Multiple lessons on one date are one streak day.
- Current streak ends today when today has activity; otherwise it may end yesterday; otherwise it is zero.
- Longest streak is the longest all-time consecutive sequence of qualifying UTC dates.

`dailyActivity` always contains exactly 35 UTC dates, oldest first, ending on the generated current UTC date. Zero-lesson dates are included.

## Response behavior

The response exposes learner-facing totals, streaks, a nullable last completed lesson, language and level distributions, and daily completion activity. Empty accounts return `200 OK`, zero counts, empty distributions, null `lastCompletedLesson`, and 35 zero-valued activity rows.

Stored language and level values are trimmed. Blank legacy values still count in totals and activity but are omitted from their corresponding distributions. V1 intentionally excludes valid-turn totals, messages, duration, scores, and percentages.

## Storage and future work

Progress V1 adds no database, table, column, index, migration, model-snapshot change, stored aggregate, cache, job, or backfill. Future index work requires real production query-performance evidence and is not part of this endpoint.
