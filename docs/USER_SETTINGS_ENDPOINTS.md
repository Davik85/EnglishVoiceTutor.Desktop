# User Settings Endpoints

Review date: 2026-07-06.

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
- Authenticated request/response payloads include the separated `nativeLanguage`, `studyLanguage`, `explanationLanguage`, `speechVoice`, `speechSpeed`, `conversationModeEnabled`, and backend-persisted `selectedTutorId` concepts.

## Auth behavior

- `GET /api/me/settings` and `PUT /api/me/settings` return `401` without a valid token.
- Dev settings endpoint remains available in Development for local product testing.
- Desktop stores the signed-in session in a local `auth-session.json` file, but current Windows storage writes a DPAPI-protected Base64 payload, not raw plaintext token JSON.

## Language ownership and desktop mapping

- `UserProfileEntity.NativeLanguage` is the backend source for native language.
- `UserSettingsEntity.StudyLanguage` is the selected supported study language.
- `UserSettingsEntity.ExplanationLanguage` is the explanation/interface language preference.
- `UserProfileEntity.SelectedTutorId` is backend-owned persisted account state for the selected tutor and is returned as `selectedTutorId` from `GET /api/me/settings` and `/api/dev/user-settings`.
- Available tutor options still come from `GET /api/tutor-options`; `PUT /api/me/settings` persists the selected tutor when `selectedTutorId` is supplied, validates it against approved tutor IDs, canonicalizes valid IDs, and rejects invalid values.
- `speechVoice` remains a separate setting and is not automatically overwritten when `selectedTutorId` changes.
- Desktop sends `SelectedNativeLanguageOption.Id` as `NativeLanguage`.
- Desktop sends `SelectedInterfaceLanguageOption.Id` as `ExplanationLanguage`, or the current intended interface/explanation source if that UI is refactored later.
- Desktop sends the selected supported study language as `StudyLanguage`.
- Desktop must not send the native language as `ExplanationLanguage`.
- Existing `UserProfile.NativeLanguage = "unknown"` production values are not blindly backfilled. They are corrected when users save/sync settings from a fixed desktop client unless a reliable backend-side source is identified later.


## Selected tutor support

- Backend release `0.1.35-backend.109` deployed the completed selected tutor settings API change from commit `268681e`.
- `selectedTutorId` is no longer only a known API gap; it is persisted backend account state through `/api/me/settings`.
- `GET /api/me/settings` returns `selectedTutorId`.
- `PUT /api/me/settings` accepts optional `selectedTutorId`; when supplied with a valid tutor ID, the backend canonicalizes and persists it to `UserProfileEntity.SelectedTutorId`.
- Omitted or `null` `selectedTutorId` values preserve the existing selected tutor for backward compatibility.
- Invalid `selectedTutorId` values are rejected by backend validation; clients should read valid options from `GET /api/tutor-options`.
- No database migration was needed because `UserProfileEntity.SelectedTutorId` already existed in the EF model and migrations.

## Validation (applies to settings writes)

`400 Bad Request` for invalid payloads, including:

- unsupported `studyLanguage`
- empty `explanationLanguage`
- empty `speechVoice`
- `speechSpeed` outside allowed range
- unsupported `selectedTutorId` (valid tutor IDs come from `/api/tutor-options`)

## Current language boundaries

- Study languages remain exactly English, French, German, Portuguese, Spanish, and Italian.
- Release-ready Interface languages remain exactly `en`, `es`, `fr`, `de`, `it`, `pt`, `ru`, `pl`, `ar`, `ja`, `ko`, `sr`, `hr`, and `bg`.
- Native/Explanation languages remain the broad catalog from the localization foundation.

## Known limitations / future hardening

- Production billing is not ready.
- Public release is not declared ready.
- Full CMS/Admin production operations are not implemented. Development/admin Admin CMS Content exists, CMS draft-save audit logging is implemented for successful Save draft operations, smoke/test audit entries are hidden by default with a debug checkbox, and production RBAC plus critical-change approval remain future work.
