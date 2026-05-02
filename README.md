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
- The desktop app uses `POST /api/lesson-chat/reply`.
- `POST /api/lesson-chat/mock-reply` stays available for local compatibility and testing.

## Backend OpenAI configuration
Run backend with OpenAI enabled (PowerShell):

```powershell
$env:OPENAI_API_KEY="your_api_key_here"
dotnet run
```

- If `OPENAI_API_KEY` is missing, backend returns mock replies.
- If OpenAI call fails or returns invalid output, backend safely falls back to mock replies.
- Desktop app still calls only the backend.

## Security rule
OpenAI API keys must never be stored in the desktop app and must never be committed to source control.
