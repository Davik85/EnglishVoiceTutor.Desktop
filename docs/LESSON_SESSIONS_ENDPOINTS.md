# Lesson Session Endpoints

Review date: 2026-07-13.

## Status

- **Implemented + Validated:** authenticated create/finish/read session endpoints, backend-owned lesson summaries, persisted messages, the authenticated mobile lesson-session reply placeholder route, and backend-owned initial voice scenario semantic resolution.
- **Development-only:** legacy diagnostic routes remain under `/api/dev/*`.
- **Production mobile contract:** `POST /api/me/lesson-sessions/{sessionId}/reply` is authenticated and backend-owned, but is intentionally not AI-enabled yet.
- **Transitional product behavior:** request identity is auth-aware in Development for dev routes.

## Routes

### Authenticated user-facing routes

- `POST /api/me/lesson-sessions`
- `PUT /api/me/lesson-sessions/{sessionId}/finish`
- `GET /api/me/lesson-sessions/{sessionId}/summary`
- `POST /api/me/lesson-sessions/{sessionId}/messages`
- `POST /api/me/lesson-sessions/{sessionId}/reply`
- `POST /api/me/lesson-sessions/{sessionId}/voice-scenario-resolution`

`PUT /api/me/lesson-sessions/{sessionId}/finish` marks the owned session complete first, then makes a best-effort backend-owned summary-generation attempt using persisted lesson messages and safe runtime metadata. It is idempotent. `GET /api/me/lesson-sessions/{sessionId}/summary` is read-only: it returns the learner-safe persisted result when ready, or a stable safe unavailable status when generation is not ready or unavailable; it does not regenerate a missing summary. Authenticated production clients do not upload or author `summary`, `strengths`, `improvements`, `vocabulary`, `grammar`, or `nextSteps`; those fields are generated and owned by the backend.

### Development / diagnostic routes

- `POST /api/dev/lesson-sessions`
- `PUT /api/dev/lesson-sessions/{sessionId}/finish`
- `GET /api/dev/lesson-sessions`
- `GET /api/dev/lesson-sessions/{sessionId}`
- `PUT /api/dev/lesson-sessions/{sessionId}/summary`
- `GET /api/dev/lesson-sessions/{sessionId}/summary`

The development-only summary routes are diagnostic/development boundaries, not the production mobile flow or production mobile contracts.

## Mobile lesson-session text reply placeholder

`POST /api/me/lesson-sessions/{sessionId}/reply` is the new backend-owned mobile text reply contract. Mobile clients should send only the route `sessionId` plus this request body:

```json
{
  "messageText": "..."
}
```

Current production behavior is deliberately safe placeholder mode. The endpoint authenticates the caller, verifies that the session exists and belongs to the user, verifies that the session is still active, checks the existing lesson/chat reply limits where applicable, and then returns a controlled `409 Conflict` for otherwise valid active sessions instead of calling AI:

```json
{
  "error": "mobile_lesson_reply_not_implemented",
  "errorCode": "mobile_lesson_reply_not_implemented",
  "code": "mobile_lesson_reply_not_implemented",
  "message": "Mobile lesson text replies are not available yet. Please continue this lesson in a supported client.",
  "sessionId": "..."
}
```

Other response behavior:

- `400 Bad Request` for blank `messageText`.
- `404 Not Found` when the session is missing or not owned by the authenticated user.
- `409 Conflict` with the existing session-ended payload for inactive/ended sessions.
- `429 Too Many Requests` with the existing free/rate-limit payload if the chat reply limit is exceeded.
- `503 Service Unavailable` with the existing lesson-session storage unavailable payload.

Architecture boundary:

- `POST /api/lesson-chat/reply` remains the existing real Windows desktop lesson runtime path and still expects the large desktop-built `LessonChatRequest`. Mobile must **not** call it directly.
- `POST /api/me/lesson-sessions/{sessionId}/messages` remains the persisted lesson-message path used for server-owned transcript/history.
- Mobile must **not** send desktop prompt, runtime, scenario, or turn-management payloads.
- Mobile must **not** call OpenAI directly. Provider access remains backend-only.
- This endpoint is not the real production lesson runtime yet; it is a safe placeholder contract and must not be treated as the production cross-platform chat implementation.
- The next implementation direction is for the backend to hydrate lesson runtime/server-side context before enabling AI replies. Mobile should continue to send only `sessionId` and `messageText`, and should not duplicate desktop prompt/scenario/turn logic.

Production deployment note: backend `0.1.35-backend.110` deployed this placeholder contract without adding or running an EF/database migration. Backend `0.1.35-backend.111` later deployed backend-owned authenticated lesson completion and summaries without adding or running an EF/database migration. Backend `0.1.35-backend.113` added authenticated initial voice scenario semantic resolution without adding or running an EF/database migration. Backend `0.1.35-backend.115` historically fixed the resolver structured-output validation mismatch without adding or running an EF/database migration. Backend `0.1.35-backend.116` is the current production backend; previous release is `0.1.35-backend.115`. The old `POST /api/lesson-chat/reply` desktop endpoint was not changed, and `POST /api/me/lesson-sessions/{sessionId}/messages` remains unchanged.


## Initial voice scenario semantic resolution

`POST /api/me/lesson-sessions/{sessionId}/voice-scenario-resolution` is an authenticated, backend-owned helper for the initial voice scenario selection turn only. It was added by source commit `c850f4b` (`feat: add voice scenario semantic resolution`) and first deployed in production backend `0.1.35-backend.113`. Backend `0.1.35-backend.115` historically stabilized this contract; backend `0.1.35-backend.116` is the current production backend. The endpoint verifies that the authenticated user owns the active lesson session before resolving anything. It does not replace the normal lesson reply endpoint and must not be used for ongoing lesson dialogue.

The endpoint compares the learner's recognized text against the current CMS scenario candidates supplied by the client for that initial selection turn. It uses the backend-configured OpenAI Responses structured-output infrastructure; clients never receive or provide a model ID, API key, provider prompt, or secret. The backend validates model output against the supplied candidate list and rejects any matched or clarification candidate IDs that are not present in that list, so it never invents a CMS context ID. Production code contains no lesson-specific aliases, scenario phrases, topic keywords, or hardcoded provider model IDs for this resolver.


Backend `0.1.35-backend.115` fixes an HTTP 502 failure path where the provider could return a structured-output shape permitted by the old provider schema but rejected by backend validation. The provider schema now uses one explicit result shape for each supported decision: `published_context`, `free_context`, `clarify`, and `unsafe`. The backend converts that nested provider result back into the existing flat public endpoint response shown below, so the route, request body, response body, and Mobile contract did not change. `free_context` remains a first-class result, and runtime candidate IDs are still validated against the current CMS candidates for the lesson. This fix did not add scenario titles, transcript phrases, CMS scenario IDs, or language-specific production examples; did not change production credential validation; and automated tests did not use a live OpenAI call.

Request body contract:

```json
{
  "studyLanguage": "English",
  "learnerLevel": "A2",
  "topicId": "travel",
  "subtopicId": "hotel_check_in",
  "runtimeScenarioId": "travel_hotel_check_in",
  "runtimeVersion": "...",
  "recognizedText": "I'd like to check in at a hotel",
  "isInitialScenarioSelectionTurn": true,
  "candidates": [
    {
      "id": "hotel_front_desk",
      "title": "Check in at a hotel",
      "description": "Practice arriving at a hotel and speaking with reception."
    }
  ]
}
```

Response body contract:

```json
{
  "decision": "published_context",
  "matchedContextId": "hotel_front_desk",
  "confidence": 0.92,
  "candidateContextIds": ["hotel_front_desk"],
  "normalizedFreeContext": null,
  "clarificationText": null
}
```

Supported `decision` values:

- `published_context`: selects one real supplied CMS context.
- `free_context`: preserves the learner's specific custom scenario instead of mapping it to a CMS candidate.
- `clarify`: starts no scenario and returns likely supplied candidates plus clarification text.
- `unsafe`: starts no scenario.

Desktop boundary: no Desktop client source code changed in commit `c850f4b`; the endpoint is additive; existing Desktop API usage remains compatible; no new Windows installer was required; and these docs must not claim that Desktop already uses this endpoint. A Desktop voice-scenario parity review may be considered later, but it is only a possible future task, not a completed audit or confirmed defect. A physical Android retest is still required to confirm that the first clean voice scenario selection no longer returns HTTP 502; initial mixed-script transcription rejection, keyboard overflow and lesson-screen UI work, and missing lesson-chat avatar assets remain separate Mobile issues. Do not mark the complete Mobile voice flow as fully stabilized yet.

## Runtime identity resolution (Development)

- With valid Bearer token: resolve authenticated user.
- Without token: use dev-user fallback.
- Authenticated and dev session history/counters remain isolated.

## Request/response summary

- `POST` creates an active session.
- `PUT .../finish` marks a session finished.
- `GET` list returns recent sessions (newest first, capped).
- `GET` detail returns one session for resolved user.

Common responses:
- `200 OK` / `201 Created` on success.
- `400 Bad Request` for validation errors.
- `404 Not Found` when session is not visible to resolved user.
- `503 Service Unavailable` when storage is unavailable.

## Known limitations / future hardening

- Dev endpoints remain available for local diagnostics.
- Login exists but is not required for Lesson Chat in product.
- Production auth enforcement is not enabled for all runtime endpoints yet.
- Future production API naming should move away from `/api/dev` for authenticated user-facing session APIs.
- Subscription/payment enforcement is not implemented.


## 2026-07-11 authenticated Finish + Summary production verification

Production backend `0.1.35-backend.112` was verified with a real authenticated Flutter mobile lesson: session start succeeded, lesson messages persisted, `PUT /api/me/lesson-sessions/{sessionId}/finish` completed the lesson, and `GET /api/me/lesson-sessions/{sessionId}/summary` returned a ready backend-owned learner-safe summary displayed by mobile. The `.112` fix preserves support for top-level Responses API `output_text`, adds fallback support for nested `output[].content[].text`, rejects blank provider output before deserialization, and keeps summary failure isolated from lesson completion. No local/client summary generation, endpoint contract change, schema change, migration, desktop UI change, or Windows installer change was introduced.

Desktop authenticated Finish uses the shared completion path and keeps its existing desktop-compatible response behavior. Desktop currently displays its existing local desktop summary flow; mobile is the first verified client displaying the authenticated backend-owned GET summary result.
