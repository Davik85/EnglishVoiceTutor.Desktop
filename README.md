# EnglishVoiceTutor.Desktop

## Run the desktop app
1. Restore dependencies:
   `dotnet restore`
2. Build the desktop app:
   `dotnet build`
3. Run from your IDE or use your preferred `dotnet` run/publish workflow.

## Run the local backend proxy
1. Go to the backend project folder:
   `cd backend/EnglishVoiceTutor.Api`
2. Restore dependencies:
   `dotnet restore`
3. Build the backend:
   `dotnet build`
4. Start the API:
   `dotnet run`

## Lesson chat endpoints
- The desktop app now uses `POST /api/lesson-chat/reply`.
- `POST /api/lesson-chat/mock-reply` is kept temporarily for local compatibility and testing.

## Backend OpenAI configuration (safe skeleton)
- Configure the OpenAI key only on the backend environment.
- For local Windows PowerShell testing:
  `$env:OPENAI_API_KEY="your_api_key_here"`
- Do not store the key in the desktop app.
- Do not commit the key to git.
- The backend currently still uses mock lesson replies by default.

## Security rule
OpenAI API keys must never be stored in the desktop app. The desktop app should call only the backend proxy for future AI features.
