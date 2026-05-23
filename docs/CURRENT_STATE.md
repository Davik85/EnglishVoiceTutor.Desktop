# Current State

Review date: 2026-05-23.

This document records the current confirmed MVP status after auth foundation, optional desktop account UX, and auth-aware runtime persistence updates.

## MVP core flow

**Implemented + Validated**
- Desktop build succeeds.
- Desktop Release build succeeds.
- Backend build succeeds.
- Lesson content audit passes.
- App starts without login.
- Lesson Chat opens and works without login.

## Auth and account status

**Implemented + Validated**
- Backend Auth/JWT foundation is implemented:
  - `POST /api/auth/register`
  - `POST /api/auth/login`
  - `GET /api/auth/me` (Bearer token)
- Optional desktop Account UI is implemented inside Settings:
  - register/login/logout
  - `auth-session.json` created after register/login
  - `auth-session.json` removed after logout
  - password box clears after successful register/login/logout
  - token is not shown in UI
  - password is not shown as plain text
- Login remains optional for MVP.

## Settings status

**Implemented + Validated**
- Settings is auth-aware:
  - signed out -> `GET/PUT /api/dev/user-settings`
  - signed in -> `GET/PUT /api/me/settings`
- `GET /api/me/settings` returns `401` without token.
- Logout returns Settings to dev settings source (`/api/dev/user-settings`).

## Lesson runtime and persistence status

**Implemented + Validated**
- PostgreSQL + EF Core persistence foundation works.
- Lesson persistence works for sessions/messages/summaries.
- Lesson runtime persistence is auth-aware:
  - signed out -> Development dev-user fallback
  - signed in -> authenticated JWT user persistence
- Lesson Chat does not require login.
- Dev-user and authenticated-user counters/history are isolated.
- Lesson history detail includes messages, summary, and feedback results where available.

## Free-limit status

**Development-only + Validated**
- Development uses diagnostics-only mode: `FreeLimits:EnforcementEnabled=false`.
- Counters still increment.
- Lesson Chat / Hint / STT / TTS are not blocked in Development diagnostics-only mode.

## Data handling status

**Implemented + Validated**
- `feedback_results` persistence works.
- Raw audio is not stored.
- Full prompts are not stored.
- Provider payloads are not stored.
- Secrets/API keys/JWT tokens/passwords are not stored in persistence tables.

## Existing EF migrations

**Implemented (unchanged)**
- `20260518000000_InitialProductStorageSchema`
- `20260520120000_AddLessonSummaryContentFields`
- `20260520132002_AddUsageEventStatusAndStudyLanguage`
- `20260520150000_AddDailyUsageChatReplyCount`
- No new EF migration was created for recent auth-aware runtime work.

## Known limitations and future production hardening

**Future work**
- Production auth enforcement for all runtime endpoints is not enabled yet.
- Local token storage (`auth-session.json`) is MVP-only and must be hardened/replaced before production.
- Roles are not implemented.
- Billing/subscription enforcement is not implemented.
- CMS/admin panel is not implemented.
- Contabo deployment has not been done.
