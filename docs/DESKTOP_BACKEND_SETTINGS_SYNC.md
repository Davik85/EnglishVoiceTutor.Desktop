# Desktop backend settings sync

Review date: 2026-07-06.

The desktop app remains local-first for user settings while authenticated users also sync account preferences with the backend. Lessons continue to read the selected study language through the normal desktop settings flow, but signed-in saves also send the separated settings fields expected by the backend contract.

## Current authenticated flow

- Signed-in desktop clients use `GET /api/me/settings` and `PUT /api/me/settings` with the authenticated user token.
- Signed-out/development fallback can still use `/api/dev/user-settings` for local diagnostics, but that endpoint is not the production account settings source.
- Backend-backed settings remain the signed-in source of truth after a successful sync.
- Backend failure must not break local app usage: Settings continues using local values, and backend sync status remains diagnostic-only.

## Language ownership and sync contract

The backend and desktop now treat native language, selected study language, explanation/interface language, and selected tutor as separate concepts:

- `UserProfileEntity.NativeLanguage` is the source for the user's native language.
- `UserSettingsEntity.StudyLanguage` is the selected supported study language for lessons.
- `UserSettingsEntity.ExplanationLanguage` is the explanation/interface language preference, separate from native language.
- `UserProfileEntity.SelectedTutorId` is the backend source for persisted selected tutor account state. `GET /api/me/settings` returns `selectedTutorId`, `PUT /api/me/settings` persists it when a valid `selectedTutorId` is supplied, and `GET /api/tutor-options` remains the source for available tutor IDs.
- `speechVoice` remains separate from `selectedTutorId`; saving a selected tutor must not automatically overwrite the selected speech voice. Omitted or `null` `selectedTutorId` values preserve the existing backend-selected tutor for backward compatibility, and invalid IDs are rejected by backend validation.

Desktop settings writes should map UI selections as follows:

- `NativeLanguage` comes from `SelectedNativeLanguageOption.Id`.
- `ExplanationLanguage` comes from `SelectedInterfaceLanguageOption.Id`, or the current intended interface/explanation language source if that UI is refactored later.
- `StudyLanguage` comes from the selected supported study language option.

Desktop must not send native language as `ExplanationLanguage`. Existing production users whose `UserProfile.NativeLanguage` is `unknown` are not blindly backfilled by the backend; those values are expected to be corrected when the user saves/syncs settings from a fixed desktop client, unless a reliable backend-side source is identified later.

## Language boundaries

- Supported study languages remain English, French, German, Portuguese, Spanish, and Italian.
- Release-ready interface languages remain `en`, `es`, `fr`, `de`, `it`, `pt`, `ru`, `pl`, `ar`, `ja`, `ko`, `sr`, `hr`, and `bg`.
- Future interface languages should be added only after full UI coverage and localization audit coverage.

## Local fallback

Backend failure must not break local app usage. If the backend is unavailable, Settings continues using local settings and the user can still save local settings. Backend sync status is included in copied diagnostics as `available`, `unavailable`, or `not checked`.
