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

## Security rule
OpenAI API keys must never be stored in the desktop app. The desktop app should call only the backend proxy for future AI features.
