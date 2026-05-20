# Dev Lesson History Endpoints

## Overview
This document describes backend-only development endpoints for reading persisted lesson history.

Current behavior is intentionally scoped to the temporary dev user identity provided by `DevUserProvider`.

## Endpoints

### GET `/api/dev/lesson-history`
Returns recent lesson sessions for the temporary dev user.

Response shape (`LessonHistoryListResponse`):
- `items`: up to 50 recent sessions ordered by `startedAt` descending.
- each item includes session metadata, `hasSummary`, `summaryPreview`, and `messageCount`.

Possible responses:
- `200 OK`: history list payload.
- `503 Service Unavailable`: storage unavailable with safe short `ErrorResponse` body.

### GET `/api/dev/lesson-history/{sessionId}`
Returns one lesson session detail for the temporary dev user.

Response shape (`LessonHistoryDetailResponse`):
- full session metadata.
- `messages`: full session messages in conversation display order: `turnNumber` ascending, then role order (`user`, `assistant`, `system`, unknown), then `createdAt` ascending.
- `summary`: optional summary block when one exists.

Possible responses:
- `200 OK`: detail payload.
- `404 Not Found`: session does not exist for dev user.
- `503 Service Unavailable`: storage unavailable with safe short `ErrorResponse` body.

## Temporary dev user behavior
Both endpoints only return records for the current temporary dev user ID. This is deliberate for current development and can be swapped later with authenticated user identity.

## Known limitations
- Desktop Lesson History screen is not connected to these backend endpoints yet.
- `feedback_results` and `usage_events` are not included yet.
- CMS/admin panel is not implemented yet.
