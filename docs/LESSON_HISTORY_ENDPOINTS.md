# Lesson History Endpoints

Review date: 2026-07-19.

## Status and client boundary

Authenticated Lesson History is implemented in commit `37d8c4d8` (`Add authenticated lesson history endpoints`), production-verified, and deployed in backend release `0.1.35-backend.123`. This completed the backend API prerequisite; Mobile UI integration remains separate future client work. The backend release did not change Desktop behavior.

Production clients must use the authenticated `/api/me/...` routes below. `/api/dev/lesson-history` routes remain development diagnostics and are not Mobile contracts. Mobile must never read Desktop-local JSON history.

## Authentication and ownership

- Both production routes require authentication; an unauthenticated request returns `401 Unauthorized`.
- Ownership comes only from the authenticated request identity. Clients do not send, choose, or trust a user ID to select history.
- List and detail queries return only the authenticated user's sessions.
- Detail returns `404 Not Found` both when the session does not exist and when it is not available to the authenticated user, without revealing another user's lesson.

## GET `/api/me/lesson-history`

Returns the authenticated learner's recent lesson sessions, newest `startedAt` first. The backend currently returns at most **50** items and owns the ordering; clients should display the returned order rather than reconstructing it.

Each list item contains:

- `sessionId`, `lessonContentId`, `studyLanguage`, `topicTitle`, `subtopicTitle`, and `level`;
- optional `selectedContextTitle`, plus `modeUsed` and `status`;
- `startedAt`, optional `finishedAt`, and `updatedAt`;
- `validTurnCount` and `messageCount`;
- `hasSummary` and optional `summaryPreview` summary-availability indicators;
- `estimatedCost` as part of the current response contract.

The response envelope is `{ "items": [...] }`.

## GET `/api/me/lesson-history/{sessionId:guid}`

Returns an owned lesson's full history detail. The response can include:

- lesson and session metadata, status, timestamps, valid-turn count, and current contract metadata;
- an optional summary with strengths, improvements, vocabulary, grammar, and next steps;
- transcript messages with role, text, source, turn order, valid-turn indicator, and applicable audio/transcript metadata;
- feedback results, including the available correction, explanation, tip, and praise fields.

Messages are returned in backend-defined transcript order. `summary` can be `null`; message and feedback arrays can be empty.

## Response and storage behavior

- `200 OK` on success.
- `401 Unauthorized` when a production `/api/me/...` request is not authenticated.
- `404 Not Found` for an unknown or non-owned session.
- `503 Service Unavailable` with a safe short error body when lesson storage is unavailable.

The authenticated routes reuse the existing lesson-session, message, summary, and feedback persistence model. No database schema change or EF migration was required for this feature.

## History is not Progress

The recent maximum-50 History list is not an all-time Progress API. Clients must not derive official totals, streaks, aggregate learning statistics, or long-term progress from it, and must not invent official Progress locally. A future Progress feature requires a separate backend-owned aggregate endpoint or contract. Pagination is future work only if later product requirements require more than recent history.
