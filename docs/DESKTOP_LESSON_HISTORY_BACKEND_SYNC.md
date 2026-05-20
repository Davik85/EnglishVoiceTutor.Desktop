# Desktop lesson history backend sync

## Current behavior

- The desktop **Lesson History** screen now prefers backend history from `GET /api/dev/lesson-history` when the backend is reachable.
- If backend history is unavailable (timeout, network failure, invalid response, or HTTP error), desktop silently falls back to the existing local JSON lesson history file.
- Local JSON history is still used for writes from lesson summaries in the current phase.

## Data source priority

1. Backend list endpoint (`/api/dev/lesson-history`) for read.
2. Local JSON history (`LessonHistoryService`) as fallback.

This keeps the screen usable even when backend is down.

## Detail endpoint status

- Backend detail endpoint exists: `GET /api/dev/lesson-history/{sessionId}`.
- Desktop history UI currently remains list-focused.
- Detail endpoint integration is deferred until a dedicated desktop detail UX is implemented.

## Out-of-scope in this phase

- No local-to-backend migration.
- No backend-to-local caching layer.
- No backend code changes.
- No database schema changes or migrations.
- No auth/JWT integration yet (dev endpoints are used).
- No CMS/admin panel implementation in this phase.
