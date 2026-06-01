# Local Windows release workflow

This document describes local Windows release checks for `EnglishVoiceTutor.Desktop` without Visual Studio. The canonical current desktop tester distribution flow is the tester zip package created by `scripts/package-tester-release.ps1`; manual `dotnet publish` commands in this file are lower-level developer troubleshooting details only.

For the full shareable tester zip workflow, see [`docs/TESTER_RELEASE.md`](TESTER_RELEASE.md).

Canonical tester package command from the repository root:

```powershell
cd C:\dev\EnglishVoiceTutor.Desktop
powershell -ExecutionPolicy Bypass -File .\scripts\package-tester-release.ps1
```

Expected default tester zip output:

```text
artifacts\packages\EnglishVoiceTutor.Desktop-win-x64-self-contained.zip
```

Tester releases should use this self-contained tester zip by default so testers can copy it to another Windows device, extract it, and run `EnglishVoiceTutor.Desktop.exe` without installing the .NET Desktop Runtime. Use the framework-dependent publish documented below only for developer checks or controlled machines that already have the matching .NET Desktop Runtime installed.

## Scope

This workflow is for a local MVP desktop release check:

- run the automated release gate before packaging;
- create the canonical self-contained tester zip;
- copy/send the zip to another Windows device;
- extract the zip and launch `EnglishVoiceTutor.Desktop.exe` from the extracted folder;
- verify the app with a local backend, optional ngrok URL, or hosted backend;
- verify backend account login/session restore and backend lesson history;
- verify that no OpenAI API key is stored in the desktop app or publish output.

This workflow does **not** add or configure:

- Microsoft Store packaging;
- MSIX packaging;
- a Windows installer;
- code signing;
- auto-update;
- a deployed production backend;
- single-file publishing.

Keep this release folder-based until installer and packaging requirements are decided.

## Prerequisites

On the Windows development machine:

- .NET SDK that matches the project target framework;
- PowerShell;
- `curl` for backend checks;
- optional: `ngrok` for testing the desktop app against a temporary public backend URL.

For framework-dependent publish mode, the target Windows machine also needs the matching .NET Desktop Runtime installed. Use self-contained publish mode when the target machine may not have the required runtime.

## Repository paths

The examples below assume this local repository path:

```powershell
C:\dev\EnglishVoiceTutor.Desktop
```

Run desktop publish commands from the repository root:

```powershell
cd C:\dev\EnglishVoiceTutor.Desktop
```

The backend project is separate and lives here:

```powershell
C:\dev\EnglishVoiceTutor.Desktop\backend\EnglishVoiceTutor.Api
```

## Avatar GIF assets

The desktop project uses WPF pack URIs for the lesson avatar animations. The required GIF files are:

- `Assets/Avatars/avatar-idle.gif`
- `Assets/Avatars/avatar-listening.gif`
- `Assets/Avatars/avatar-speaking.gif`
- `Assets/Avatars/avatar-thinking.gif`
- `Assets/Avatars/avatar-transcribing.gif`

These files are included by the project as WPF `Resource` items, so they are embedded into the desktop application resources during build and publish. They are not expected to appear as loose files next to the published `.exe` unless the project is intentionally changed later to copy them as content.

After publishing, verify the avatar by launching the published app, opening a lesson, and checking that the avatar GIF appears and changes state while recording, transcribing, waiting for a reply, and playing speech.

## Required order for tester package validation

1. Run the automated release gate from the repository root:

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\run_desktop_release_gate.ps1
```

2. Run EF checks when backend schema changed or database validation is required.
3. Run the canonical tester package command:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\package-tester-release.ps1
```

4. Copy `artifacts\packages\EnglishVoiceTutor.Desktop-win-x64-self-contained.zip` to another Windows device.
5. Extract the zip.
6. Run `EnglishVoiceTutor.Desktop.exe` from the extracted folder.
7. Verify backend connection, login/account, backend history, and the core lesson flow.

## Clean previous publish output

Before rebuilding a local release, remove previous publish output:

```powershell
Remove-Item -Recurse -Force .\artifacts\publish -ErrorAction SilentlyContinue
```

The `artifacts` folder is generated output and should not be committed.

## Start the local backend

Open a PowerShell window for the backend:

```powershell
cd C:\dev\EnglishVoiceTutor.Desktop\backend\EnglishVoiceTutor.Api
$env:ASPNETCORE_ENVIRONMENT="Development"
$env:DOTNET_ENVIRONMENT="Development"
$env:SubscriptionEnforcement__Enabled="true"
dotnet run
```

For real AI/TTS/STT testing, set the OpenAI key only in the local backend environment before `dotnet run`:

```powershell
$env:OPENAI_API_KEY="PASTE_YOUR_LOCAL_OPENAI_KEY_HERE"
```

Security rules:

- Do not commit `OPENAI_API_KEY`.
- Do not store `OPENAI_API_KEY` in the desktop app.
- Do not put a real API key in source files, documentation examples, publish output, or settings files.
- Keep the key only in backend environment variables or another secure backend-only secret store.
- The desktop app does not contain the key, must not call OpenAI directly, and must call backend APIs only for AI/TTS/STT.

If `OPENAI_API_KEY` is not configured, the backend may still run, but OpenAI-backed features should be treated as not configured.

## Backend health checks

With the backend running, open another PowerShell window and run:

```powershell
curl http://localhost:5000/health
curl http://localhost:5000/api/backend/config-status
```

Expected results:

- `/health` returns a status of `ok`.
- `/api/backend/config-status` reports whether OpenAI is configured.
- `/api/backend/config-status` must never return the OpenAI API key.

## Backend-required functional scope

The packaged desktop app requires a reachable backend for login, account/session restore validation, backend lesson history, AI replies, voice transcription/STT, TTS, translation, hints, feedback, summary, subscription/access checks, active lesson guard, and remote active lesson release. Backend-unavailable checks are resilience-only and must not be treated as functional lesson acceptance.

## Release Diagnostics behavior

The packaged Release app does not show Diagnostics by default. Diagnostics can appear in Release only when `EVT_DESKTOP_DIAGNOSTICS=1` is set locally before launching the app. Do not commit this variable in scripts, settings, docs with machine-specific values, or shortcuts. Diagnostics and copied diagnostics output must continue masking secrets and tokens.

## Publish Mode A: framework-dependent folder

Use this mode for developer checks or when the target Windows machine has the required .NET Desktop Runtime installed. Do not use this as the default tester package because early testers should not need to install `windowsdesktop-runtime-10` manually.

From the repository root:

```powershell
dotnet publish .\EnglishVoiceTutor.Desktop.csproj -c Release -r win-x64 --self-contained false -o .\artifacts\publish\win-x64-framework-dependent
```

Run the published app:

```powershell
.\artifacts\publish\win-x64-framework-dependent\EnglishVoiceTutor.Desktop.exe
```

Verify the executable exists:

```powershell
Test-Path .\artifacts\publish\win-x64-framework-dependent\EnglishVoiceTutor.Desktop.exe
```

Expected result: `True`.

## Publish Mode B: self-contained folder

Use this mode when the target Windows machine may not have the required .NET runtime installed. This is the default mode for tester release zips.

From the repository root:

```powershell
dotnet publish .\EnglishVoiceTutor.Desktop.csproj -c Release -r win-x64 --self-contained true -o .\artifacts\publish\win-x64-self-contained
```

Run the published app:

```powershell
.\artifacts\publish\win-x64-self-contained\EnglishVoiceTutor.Desktop.exe
```

This mode creates a larger output folder because it includes the runtime. Keep this as a folder-based publish. Do not enable single-file publish yet unless WPF behavior and avatar GIF resources are verified separately.


## Settings diagnostics

Use Settings -> Diagnostics during local release checks to inspect the current Backend URL, backend status, AI status, settings file path, lesson history file path, interface language, native language, tutor avatar, and app version. Click **Refresh diagnostics** to re-check `/api/health`, `/api/health/database`, and `/api/backend/config-status` for the configured backend.

Diagnostics must never display an OpenAI API key; the desktop app should only show whether AI is configured, not the secret value.

## Verify local settings and history persistence

The desktop app stores user settings here:

```text
%APPDATA%\EnglishVoiceTutor.Desktop\settings.json
```

The desktop app stores lesson history here:

```text
%APPDATA%\EnglishVoiceTutor.Desktop\lesson-history.json
```

Verification steps:

1. Launch the published `EnglishVoiceTutor.Desktop.exe`.
2. Open Settings.
3. Change the Backend URL, interface language, user profile fields, or tutor avatar.
4. Save settings.
5. Close the app.
6. Launch the same published `.exe` again.
7. Confirm the saved settings are still present.
8. Complete a lesson.
9. Confirm `lesson-history.json` is created and the completed lesson appears in History.

The desktop settings file must not contain an OpenAI API key.

## Verify Backend URL behavior

Default Backend URL:

```text
http://localhost:5000
```

Local backend verification:

1. Start the backend with `dotnet run`.
2. Launch the published desktop app.
3. Open Settings.
4. Set Backend URL to `http://localhost:5000`.
5. Save settings.
6. Start a new lesson.
7. Verify the backend indicators show a connected backend.
8. Verify AI-backed features that require backend access work.
9. Restart the desktop app and confirm the Backend URL persisted.

Invalid backend resilience verification:

This is a resilience-only check. Do not expect login, lesson start, Send, Hint, Translate, Play voice/TTS, transcription, Conversation Mode, Finish lesson, or Summary generation to work while the backend is unavailable.

1. Open Settings.
2. Temporarily enter an invalid Backend URL, such as `http://localhost:5999`.
3. Save settings.
4. Open Account or try a backend-required action.
5. Confirm the app reports a friendly localized backend-unavailable, backend-required, or failed-health-check message without crashing.
6. Confirm normal learner-facing UI does not show a raw stack trace.
7. Restore the valid Backend URL before running functional lesson checks.

## Optional ngrok testing

Use ngrok only for testing a temporary public URL to the local backend.

With the backend running on port `5000`, start ngrok:

```powershell
ngrok http 5000
```

Before using the URL in the desktop app, test it manually:

```powershell
curl.exe -H "ngrok-skip-browser-warning: 1" https://YOUR-NGROK-URL/health
```

Expected health response:

```json
{
  "status": "ok"
}
```

If this `curl.exe` command fails, the desktop app will not connect either. Confirm the backend is running locally, ngrok is forwarding port `5000`, and the copied URL is the `https` URL from the current ngrok session.

The desktop app sends the `ngrok-skip-browser-warning: 1` header automatically for backend calls, including Diagnostics and Lesson Chat. The same header is safe for local and hosted ASP.NET Core backends because unknown headers are ignored.

Then:

1. Copy the `https` ngrok URL.
2. Open the published desktop app.
3. Go to Settings -> Backend URL.
4. Paste the ngrok URL.
5. Save settings.
6. Open Diagnostics.
7. Click Refresh diagnostics.
8. Confirm Backend status becomes connected.
9. Confirm AI status becomes configured or not configured.
10. Open a new lesson.
11. Verify backend indicators and AI features.
12. Restart the desktop app and confirm the ngrok Backend URL persisted if you still need it.

ngrok URLs are temporary and meant for testing. A proper domain and deployed backend can be added later.

## Required tester package report format

```text
Date:
Branch/commit:
Package command:
ZIP path:
Tested device:
App starts after extraction:
Diagnostics hidden:
Backend connected:
Login/account:
History visible/preserved:
Chat mode:
Conversation Mode:
Voice transcription:
TTS:
Translation:
Hint:
Feedback:
Summary:
Active lesson guard:
Remote release:
Close with X:
Known issues:
```

## Release verification checklist

### Desktop launch

- [ ] Extract `artifacts\packages\EnglishVoiceTutor.Desktop-win-x64-self-contained.zip` on another Windows device when validating tester handoff.
- [ ] Run `EnglishVoiceTutor.Desktop.exe` from the extracted folder.
- [ ] App opens without Visual Studio.
- [ ] Welcome screen opens.
- [ ] Settings opens.
- [ ] Avatar GIF appears.
- [ ] Interface language switching works.
- [ ] Backend URL persists after restart.
- [ ] User profile persists after restart.
- [ ] Tutor avatar persists after restart.

### Backend

- [ ] Local, ngrok, or hosted backend is reachable.
- [ ] Login/account works.
- [ ] Backend lesson history is visible/preserved.
- [ ] `curl http://localhost:5000/health` works and returns status `ok`.
- [ ] `curl http://localhost:5000/api/backend/config-status` works and does not return an API key.
- [ ] Invalid backend URL does not crash the app.
- [ ] ngrok URL works if tested with `curl.exe -H "ngrok-skip-browser-warning: 1" https://YOUR-NGROK-URL/health`.

### Lesson flow with backend running

Full lesson functionality requires a running backend. Run this section only after backend health succeeds.

- [ ] Choose level.
- [ ] Choose topic.
- [ ] Choose situation.
- [ ] Send text message.
- [ ] Account login/session restore works.
- [ ] Hint works.
- [ ] Translate works.
- [ ] Record voice.
- [ ] Transcription works.
- [ ] Play voice works.
- [ ] Conversation Mode works.
- [ ] Finish lesson works.
- [ ] Summary opens.
- [ ] History stores completed lesson.
- [ ] Feedback works.
- [ ] Single active lesson guard works.
- [ ] Remote active lesson release stops the old device/session.
- [ ] Closing the app with X during an active lesson does not leave the process hanging.

### Files and secrets

- [ ] `%APPDATA%\EnglishVoiceTutor.Desktop\settings.json` is created.
- [ ] `%APPDATA%\EnglishVoiceTutor.Desktop\lesson-history.json` is created.
- [ ] No OpenAI key exists in publish output.
- [ ] No OpenAI key exists in `settings.json`.

## Known limitations

- This is not an installer; testers extract the zip and run the `.exe` from the extracted folder.
- This is not an MSIX or Microsoft Store package.
- The app is not code-signed by this workflow.
- The backend must be started separately or hosted elsewhere.
- ngrok is temporary and for testing only.
- Single-file publish is intentionally not used yet.
- Framework-dependent output requires the matching .NET Desktop Runtime on the target Windows machine.
- Self-contained output is larger because it includes the runtime.
