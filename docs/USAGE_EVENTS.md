# Usage Events

`usage_events` stores backend usage telemetry for OpenAI-related operations.

## Logged operations
- `lesson_chat_reply`
- `lesson_chat_hint`
- `lesson_chat_feedback`
- `translation`
- `audio_transcription`
- `tts`

## Behavior
- Logging is **best effort**.
- Backend requests are **not blocked** if usage persistence fails.
- Successful usage events update `daily_usage_counters` via best-effort runtime aggregation keyed by user/date/study language.
- Daily limits are **not enforced** yet.
- `status` is stored as one of: `success`, `failed`, or `skipped`.
- `studyLanguage` is stored when a safe request/session language value is available.

## Aggregation mapping to daily counters
- `lesson_chat_reply` -> `chatReplyCount`
- `lesson_chat_hint` -> `hintsUsed`
- `lesson_chat_feedback` -> `feedbackRequests`
- `audio_transcription` -> `transcriptionSeconds`
- `tts` -> `ttsSeconds`

`lessonsStarted` and `lessonsCompleted` are reserved for session lifecycle counters and are not incremented from chat reply usage events.

## Data safety
Usage events do not store:
- API keys or secrets
- connection strings
- raw audio
- full prompts
- full provider payloads
- stack traces

## Dev verification endpoint
`GET /api/dev/usage-events`
- returns the latest 50 usage events for the dev user.
- response contract uses `{ "items": [...] }`.
- intended for local development verification only.

## Out of scope (current)
- Limit enforcement remains deferred.
- CMS/admin panel
- subscription/billing enforcement
