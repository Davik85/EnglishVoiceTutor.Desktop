# Backend database foundation

## Architecture

Language Voice Tutor uses a backend-first storage architecture. PostgreSQL is the main product database and EF Core is the backend data access layer.

The desktop application must not connect directly to PostgreSQL. Desktop features should continue calling backend APIs, and future storage work should happen server-side behind those APIs.

Lesson JSON files remain file-based content for now. The `lessons` table is only a lightweight reference/snapshot layer for future backend features and does not replace the existing lesson content files.

## Connection string

The backend reads the PostgreSQL connection string from:

```text
ConnectionStrings:DefaultConnection
```

Local development placeholder:

```text
Host=localhost;Port=5432;Database=english_voice_tutor_dev;Username=postgres;Password=postgres
```

Do not store production secrets in source control. Use environment variables, user secrets, or a deployment secret store for real environments.

## Local PostgreSQL setup

Create a local database with an existing PostgreSQL installation:

```bash
createdb english_voice_tutor_dev
```

Or run PostgreSQL in Docker:

```bash
docker run --name english-voice-tutor-postgres -e POSTGRES_PASSWORD=postgres -e POSTGRES_DB=english_voice_tutor_dev -p 5432:5432 -d postgres:16
```

## Migration commands

From the repository root, restore tools and list migrations:

```bash
dotnet tool restore
dotnet ef migrations list --project backend/EnglishVoiceTutor.Api/EnglishVoiceTutor.Api.csproj --startup-project backend/EnglishVoiceTutor.Api/EnglishVoiceTutor.Api.csproj
```

Add future migrations from the repository root with:

```bash
dotnet ef migrations add <MigrationName> --project backend/EnglishVoiceTutor.Api/EnglishVoiceTutor.Api.csproj --startup-project backend/EnglishVoiceTutor.Api/EnglishVoiceTutor.Api.csproj
```

Apply migrations to the configured local database:

```bash
dotnet ef database update --project backend/EnglishVoiceTutor.Api/EnglishVoiceTutor.Api.csproj --startup-project backend/EnglishVoiceTutor.Api/EnglishVoiceTutor.Api.csproj
```

## Build verification

Restore and build from the repository root:

```bash
dotnet restore
dotnet build EnglishVoiceTutor.Desktop.slnx
```

## Future lesson-session storage note

Setup messages and context-selection messages should be stored as normal `lesson_messages` records when server-side lesson persistence is added, but they must use `IsValidLessonTurn=false` so they do not automatically count toward valid learner lesson turns.

## Production deployment note

Production deployment is intentionally skipped. Before server deployment, configure PostgreSQL on the server, set `DefaultConnection` through environment variables, run migrations against production carefully, and verify backups.
