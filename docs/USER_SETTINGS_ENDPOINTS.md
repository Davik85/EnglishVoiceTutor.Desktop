# User Settings Endpoints

Review date: 2026-05-23.

## Status

- **Implemented + Validated:** dev settings endpoints and authenticated settings endpoints.
- **Development-only:** `/api/dev/user-settings` remains available for local diagnostics/fallback.
- **Transitional MVP behavior:** desktop Settings switches source by auth state.

## Endpoint map

### Development endpoint
- `GET /api/dev/user-settings`
- `PUT /api/dev/user-settings`

### Authenticated endpoint
- `GET /api/me/settings` (requires Bearer token)
- `PUT /api/me/settings` (requires Bearer token)

## Desktop Settings behavior

- Signed out -> uses `/api/dev/user-settings`.
- Signed in -> uses `/api/me/settings`.
- Logout -> returns to `/api/dev/user-settings`.

## Auth behavior

- `GET /api/me/settings` and `PUT /api/me/settings` return `401` without valid token.
- Dev settings endpoint remains available in Development for local MVP testing.

## Validation (applies to settings writes)

`400 Bad Request` for invalid payloads, including:
- unsupported `studyLanguage`
- empty `explanationLanguage`
- empty `speechVoice`
- `speechSpeed` outside allowed range

## Known limitations / future hardening

- Login exists but is optional for MVP.
- Production-wide auth enforcement for all runtime routes is not enabled yet.
- Local desktop token storage (`auth-session.json`) is MVP-only and must be hardened/replaced before production.
- Roles, subscription/payment enforcement, and CMS/admin are not implemented.
