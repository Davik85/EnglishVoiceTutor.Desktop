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
- Current implementation persists `usage_events` only; daily counter runtime updates are deferred.
- Daily limits are **not enforced** yet.

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
- intended for local development verification only.

## Out of scope (current)
- CMS/admin panel
- subscription/billing enforcement
- daily limit enforcement
