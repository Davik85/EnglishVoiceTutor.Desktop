# Desktop backend settings sync

The desktop app remains local-first for user settings. It still stores settings in the existing local settings file and lessons continue to read the study language through that local settings flow.

Backend settings sync is currently limited to the temporary development user settings endpoints. It does not use authentication yet and should be replaced when real auth is implemented.

## Current flow

- `GET /api/dev/user-settings` loads the temporary dev user's backend settings when Settings opens or diagnostics refreshes successfully.
- If backend settings are loaded successfully, the desktop Study Language selection is updated from the backend value and the local settings file is kept consistent.
- `PUT /api/dev/user-settings` updates backend settings when Settings is saved.
- The desktop sends the current study language, explanation language, speech voice, speech speed, and conversation mode enabled values required by the backend contract.
- Supported study languages are the existing desktop study languages: English, French, German, Portuguese, Spanish, and Italian.

## Local fallback

Backend failure must not break local app usage. If the backend is unavailable, Settings continues using local settings and the user can still save local settings. Backend sync status is included in copied diagnostics as `available`, `unavailable`, or `not checked`.

## Temporary limitation

The `/api/dev/user-settings` endpoints are development-only integration points. This sync path is temporary until real authentication and per-user production settings are implemented.
