# User Settings Endpoints

The backend now has temporary database-backed endpoints for reading and updating user settings during local development.

These endpoints are intentionally scoped to a stable development user until real authentication is implemented. The desktop app is not connected to these endpoints yet.

## Temporary dev user behavior

- The backend uses one stable development user id for every request.
- If that user does not exist, the backend creates it in PostgreSQL.
- If the user profile does not exist, the backend creates a minimal profile.
- If the user settings row does not exist, the backend creates default settings.
- This is temporary and should be replaced when registration, login, JWT, and authenticated user identity are added.

## Supported study languages

`StudyLanguage` means the language the learner studies in lessons. It is not the UI language.

Supported study languages are:

- English
- French
- German
- Portuguese
- Spanish
- Italian

The default study language is English.

## GET /api/dev/user-settings

Loads the temporary development user's settings from the database. Missing dev user, profile, or settings records are created with local development defaults.

Example response:

```json
{
  "userId": "7a0f6073-09a0-47c2-b1f2-91f2a727f5e9",
  "studyLanguage": "English",
  "explanationLanguage": "Russian",
  "speechVoice": "coral",
  "speechSpeed": 1.0,
  "conversationModeEnabled": true,
  "createdAt": "2026-05-18T12:00:00Z",
  "updatedAt": "2026-05-18T12:00:00Z"
}
```

## PUT /api/dev/user-settings

Updates the temporary development user's settings in the database and returns the updated settings.

Example request:

```json
{
  "studyLanguage": "French",
  "explanationLanguage": "Russian",
  "speechVoice": "alloy",
  "speechSpeed": 1.0,
  "conversationModeEnabled": true
}
```

Example response:

```json
{
  "userId": "7a0f6073-09a0-47c2-b1f2-91f2a727f5e9",
  "studyLanguage": "French",
  "explanationLanguage": "Russian",
  "speechVoice": "alloy",
  "speechSpeed": 1.0,
  "conversationModeEnabled": true,
  "createdAt": "2026-05-18T12:00:00Z",
  "updatedAt": "2026-05-18T12:05:00Z"
}
```

## Validation

The endpoint returns `400 Bad Request` when:

- `studyLanguage` is empty or not one of the supported study languages.
- `explanationLanguage` is empty.
- `speechVoice` is empty.
- `speechSpeed` is outside the `0.5` to `2.0` range.

Example invalid language response:

```json
{
  "error": "Study language must be one of: English, French, German, Portuguese, Spanish, Italian."
}
```

## Current limitations

- Authentication is not implemented for these endpoints yet.
- The desktop app is not connected to these endpoints yet.
- These endpoints do not change Lesson Chat, Conversation Mode, TTS, STT, prompts, or lesson JSON loading behavior.
- No billing or subscription runtime logic is added here.
