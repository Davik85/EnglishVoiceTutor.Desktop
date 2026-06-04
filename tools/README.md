# Lesson content audit tools

## Run the lesson content audit on Windows

From the repository root, use the supported Windows PowerShell audit command:

```powershell
powershell -ExecutionPolicy Bypass -File tools\audit_lesson_content.ps1
```

If your PowerShell execution policy already allows local scripts, you can use the shorter form:

```powershell
.\tools\audit_lesson_content.ps1
```

Python is not required for the Windows audit workflow. The older Python audit remains optional duplicate tooling only if Python is already installed:

```powershell
python tools\audit_lesson_content.py
```

## Run before commit

From the repository root:

```powershell
dotnet restore
dotnet build
dotnet build -c Release
powershell -ExecutionPolicy Bypass -File tools\audit_lesson_content.ps1
```

The PowerShell audit uses only built-in PowerShell/.NET functionality. It validates lesson JSON, expected lesson folders and files, taxonomy metadata, level profile fields, level-specific turn limits, Cyrillic-free content, obsolete per-level folders, generic copied phrases, lesson-type safety/content expectations, and lightweight C# routing coverage.

## Desktop release gate: active lesson heartbeat guard

The desktop release gate includes the backend-enforced single active lesson guard. The guard is heartbeat-based: Lesson Chat sends a heartbeat for the current backend lesson session about every 30 seconds, and the backend treats an active lesson as blocking only while its heartbeat is fresh (2 minutes). A closed or crashed desktop app therefore should not lock the user out for 12 hours; after the short heartbeat freshness window expires, the backend allows a new lesson and preserves the old session as abandoned history.

Expected release behavior:

- An active parallel lesson on another signed-in device is still blocked with `active_lesson_exists` while that lesson's heartbeat is fresh.
- Pressing Finish lesson marks the session `Finished` and releases the guard immediately.
- Leaving Lesson Chat or closing the app stops heartbeat; app shutdown also attempts to abandon the active lesson through the backend release endpoint.
- If the app crashes or is force-closed before release completes, heartbeat timeout releases the guard after the configured short freshness window.
- If the user chooses to end the active lesson on another device and continue, the backend marks the old session `Abandoned`; the old device/session cannot continue, and old heartbeat or lesson-bound message actions are rejected with `lesson_session_ended_elsewhere`.
- Run `tools\smoke_single_active_lesson_guard.ps1` with the required backend/test setup to validate fresh blocking, stale heartbeat release, remote release, old-session invalidation, and old heartbeat/message rejection.

## CMS draft-save audit smoke

After starting the backend in Development and authenticating as the bootstrap admin, run:

```powershell
$env:EVT_ADMIN_BEARER_TOKEN = '<admin bearer token from the existing admin auth flow>'
powershell -ExecutionPolicy Bypass -File tools\smoke_cms_draft_save_audit.ps1
```

The smoke loads `static-json-v1`, performs safe draft edits and restores for one topic, one scenario bounded field, one full scenario JSON field, one prompt template, and one tutor behavior profile. It verifies recent `DraftSaved` CMS audit entries include actor, entity type, stable key, operation, changed fields, and before/after hashes. It does not edit lesson JSON files, prompt source files, or tutor source files.
