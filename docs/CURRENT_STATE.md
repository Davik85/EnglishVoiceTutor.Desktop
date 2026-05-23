# Current State

Review date: 2026-05-22.

This document records the current MVP state after the recent stabilization work. It describes the validated behavior that documentation should reflect; it is not a request to change runtime behavior.

## Current MVP summary

The MVP core lesson flow works:

1. App start and navigation through level, topic, subtopic, and lesson chat are implemented.
2. Lesson Chat works by typed input and normal voice input.
3. Enter-to-send and the normal Send button both work.
4. Normal voice recording, transcription, lesson chat replies, Play voice, Translate, Hint, View feedback, and lesson summary work.
5. Feedback and hint UI are readable and use the warm card style.
6. Conversation Mode works by using the stable TTS provider by default, not the Realtime provider.
7. Realtime remains in the repository for future testing and provider-switch work, but it is not the default MVP Conversation Mode path.

## Latest confirmed validation state

- Desktop builds successfully.
- Backend builds successfully.
- Lesson content audit passes.
- PostgreSQL + EF Core persistence foundation works.
- lesson sessions/messages/summaries persistence works.
- usage events and daily usage counters persistence works.
- feedback_results persistence works and is linked to session/message after View feedback.
- `GET /api/dev/feedback-results` works.
- `GET /api/dev/lesson-history/{sessionId}` includes messages, summary, and feedbackResults.
- `GET /api/dev/free-limit-status` works.

## Free-limit mode (current MVP)

- Free-limit diagnostics remain active.
- Free-limit enforcement is configurable.
- Development uses diagnostics-only mode: `FreeLimits:EnforcementEnabled=false`.
- In diagnostics-only mode, usage counters still increment, but Lesson Chat / Hint / STT / TTS are not blocked by HTTP 429.
- Existing HTTP 429 enforcement behavior remains available when `FreeLimits:EnforcementEnabled=true`.
- Billing/subscription enforcement is future work and should not be confused with current diagnostics mode.

## Lesson Chat UI stabilization state

Lesson Chat UI has been stabilized for MVP testing:

- right chat card is visually clearer;
- inner chat scroll area exists;
- feedback/hint are inside the chat scroll area;
- avatar panel is bounded and no longer stretches badly.

## Voice/STT stabilization state

Voice/STT flow has been stabilized after recent regressions:

- no TranslationService normalization in AudioTranscriptionService;
- no post-transcription rewriting;
- backend returns direct provider transcript text with `Trim()` only;
- usage tracking for `audio_transcription` remains active;
- development transcript preview logging exists for local debugging.

STT quality is improved after rollback/stabilization, but remains an MVP known-risk area that should continue to be monitored.

## Known stabilization items (concise)

- STT quality should continue to be monitored with real short learner phrases.
- Development transcript preview logging is useful for local debugging.
- TutorIdentityGuard may still log warnings when model output confuses learner name and tutor self-introduction.
- If these warnings continue, the next stabilization step before larger product work should be tutor identity hardening.

## Not implemented yet

- Contabo deployment
- desktop login UI
- production rollout of authenticated user flows in desktop
- billing/subscription runtime enforcement
- CMS/admin panel

## Recommended next backend/product order

1. Final small stabilization pass
   - monitor STT quality
   - harden TutorIdentityGuard / tutor identity behavior if warnings continue
2. Auth/JWT and real accounts
3. Subscription/payment enforcement
4. CMS/admin panel only after auth, roles, content versioning, draft/published workflow, audit trail, and rollback


## Auth and user settings status

- Auth/JWT backend foundation is implemented (`/api/auth/register`, `/api/auth/login`, `/api/auth/me`).
- Authenticated user settings endpoints are implemented: `GET /api/me/settings` and `PUT /api/me/settings`.
- Existing dev endpoint `GET/PUT /api/dev/user-settings` remains available for local MVP testing.
- Desktop login UI is still not implemented in this repository state.

- Desktop auth client/storage foundation is implemented (auth models, auth backend client, and local MVP token session storage).
- Desktop login UI is still not implemented.
- Lesson Chat still works without login in Development.
- Token storage is temporary MVP local JSON storage and should be hardened before production.
