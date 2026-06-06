# User Settings Endpoints

Review date: 2026-06-06.

## Status

- **Implemented + Validated:** development settings endpoints and authenticated settings endpoints.
- **Development-only:** `/api/dev/user-settings` remains available for local diagnostics/fallback.
- **Current desktop behavior:** desktop Settings switches source by auth state.

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
- Backend-backed settings remain the source of truth for signed-in account preferences.

## Auth behavior

- `GET /api/me/settings` and `PUT /api/me/settings` return `401` without a valid token.
- Dev settings endpoint remains available in Development for local MVP testing.
- Desktop stores the signed-in session in a local `auth-session.json` file, but current Windows storage writes a DPAPI-protected Base64 payload, not raw plaintext token JSON.

## Validation (applies to settings writes)

`400 Bad Request` for invalid payloads, including:

- unsupported `studyLanguage`
- empty `explanationLanguage`
- empty `speechVoice`
- `speechSpeed` outside allowed range

## Current language boundaries

- Study languages remain exactly English, French, German, Portuguese, Spanish, and Italian.
- Release-ready Interface languages remain exactly `en`, `es`, `fr`, `de`, `it`, `pt`, `ru`, `pl`, `ar`, `ja`, `ko`, `sr`, `hr`, and `bg`.
- Native/Explanation languages remain the broad catalog from the localization foundation.

## Known limitations / future hardening

- Production billing is not ready.
- Public release is not declared ready.
- Full CMS/Admin production operations are not implemented. Development/admin Admin CMS Content exists, CMS draft-save audit logging is implemented for successful Save draft operations, smoke/test audit entries are hidden by default with a debug checkbox, and production RBAC plus critical-change approval remain future work.
