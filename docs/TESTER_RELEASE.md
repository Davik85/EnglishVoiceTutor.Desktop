# Tester release package workflow

This document is the canonical current desktop tester distribution workflow for `EnglishVoiceTutor.Desktop`. The accepted tester handoff artifact is the zip created by `scripts/package-tester-release.ps1`, not a loose manual `dotnet publish` folder.

## What this tester release is

This tester release is:

- a zip package created from a published Windows desktop app folder;
- the current accepted way to create and share a desktop tester build;
- intended for testers who extract the zip and run `EnglishVoiceTutor.Desktop.exe` directly;
- intended to work with a separately running reachable backend, either local, ngrok, or hosted;
- focused on checking launch, Settings, account login/session restore, backend history, Lesson Chat, voice recording/transcription, TTS, Conversation Mode, translation, hints, feedback, Summary, active lesson guard, and clean close behavior.

## What this tester release is not

This tester release is **not**:

- an installer;
- an MSIX package;
- Microsoft Store packaging;
- a code-signed release;
- an auto-update system;
- a backend deployment;
- proof that public release is ready;
- proof that production billing is ready;
- a place to store or distribute any OpenAI API key.

Keep the MVP tester release simple until installer, signing, hosting, and public release requirements are decided.

## Required order before sharing a tester package

1. Run the automated desktop release gate from the repository root:

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\run_desktop_release_gate.ps1
```

2. Run EF checks only when backend schema changed or database validation is required:

```powershell
dotnet ef migrations list --project backend/EnglishVoiceTutor.Api/EnglishVoiceTutor.Api.csproj
dotnet ef database update --project backend/EnglishVoiceTutor.Api/EnglishVoiceTutor.Api.csproj
dotnet ef migrations has-pending-model-changes --project backend/EnglishVoiceTutor.Api/EnglishVoiceTutor.Api.csproj
```

3. Create the tester zip with the canonical package command.
4. Copy or send the zip to another Windows device.
5. Extract the zip on that device.
6. Run `EnglishVoiceTutor.Desktop.exe` from the extracted folder.
7. Verify backend connection, account login/session restore, backend lesson history, and the accepted core lesson flow.

## Canonical tester package command

Run these commands from the repository root on the Windows development machine:

```powershell
cd C:\dev\EnglishVoiceTutor.Desktop
powershell -ExecutionPolicy Bypass -File .\scripts\package-tester-release.ps1
```

The script publishes the desktop app to:

```text
artifacts\publish\win-x64-self-contained
```

Then it creates the current tester handoff zip:

```text
artifacts\packages\EnglishVoiceTutor.Desktop-win-x64-self-contained.zip
```

Send this zip to testers. Do not send a loose `dotnet publish` command as the main tester flow. Manual `dotnet publish` remains only a lower-level implementation detail and developer troubleshooting tool.

## Package types

The default tester package is self-contained so early testers can unzip the package and run the app without manually installing `windowsdesktop-runtime-10` or any other .NET Desktop Runtime:

```text
artifacts\packages\EnglishVoiceTutor.Desktop-win-x64-self-contained.zip
```

The self-contained package is larger because it includes the required runtime components. This is the recommended package for tester releases.

A framework-dependent package is still available as an advanced smaller option for developer checks or controlled machines that already have the matching .NET Desktop Runtime installed:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\package-tester-release.ps1 -FrameworkDependent
```

That advanced command publishes to:

```text
artifacts\publish\win-x64-framework-dependent
```

And creates:

```text
artifacts\packages\EnglishVoiceTutor.Desktop-win-x64-framework-dependent.zip
```

The package script does not require administrator privileges, does not publish or modify the backend, does not create or modify `%APPDATA%` settings, does not include local lesson history, and rejects obvious API-key-like files in the publish output.

## Backend requirement

The packaged desktop app is still backend-driven. A reachable backend is required for:

- account registration, login, logout, and session restore validation;
- backend lesson history;
- lesson start and continuation;
- AI bot replies;
- voice recording transcription/STT;
- TTS / Play voice;
- translation;
- hints;
- feedback;
- final summary;
- subscription/access checks;
- single active lesson guard and remote active lesson release.

The desktop app does not contain an OpenAI API key, must not call OpenAI directly, and must call backend APIs only. All AI/TTS/STT requests go through the backend.

Active lesson guard behavior is backend-enforced: one active lesson per account, heartbeat keeps the active session fresh, stale heartbeat stops blocking after the current 2-minute freshness window, remote release marks the old session `Abandoned`, and old heartbeat or lesson-bound message actions are rejected after release. UI wording must stay neutral and must not use fraud language.

## Backend run for local tester validation

Start the backend locally in Development before local package validation that requires backend APIs:

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

Rules for this variable:

- it is needed only on the backend for real AI/TTS/STT testing;
- never put it in the desktop app;
- never paste a real key into docs;
- never commit it;
- never send it to testers.

## Release Diagnostics behavior

The packaged Release app does not show the Diagnostics tab by default. Diagnostics can appear in Release only when this local environment variable is set on that machine before launching the app:

```powershell
$env:EVT_DESKTOP_DIAGNOSTICS="1"
```

Do not commit this variable in scripts, docs with machine-specific values, settings files, or launch shortcuts. Diagnostics and copied diagnostics output must continue masking secrets, tokens, API keys, environment variables, lesson messages, raw audio file paths, and lesson history content.

## Before sending to a tester

- Run the automated release gate.
- Confirm EF checks if backend schema changed.
- Create `artifacts\packages\EnglishVoiceTutor.Desktop-win-x64-self-contained.zip`.
- Start the backend locally, prepare a temporary ngrok URL, or confirm a hosted backend URL.
- Verify backend health before sending instructions.
- Do not send `OPENAI_API_KEY` to testers.
- Do not send `%APPDATA%\EnglishVoiceTutor.Desktop\settings.json`.
- Do not send `%APPDATA%\EnglishVoiceTutor.Desktop\lesson-history.json`.
- Send only:
  - the zip package;
  - the Backend URL or ngrok URL;
  - short testing instructions.

## Backend URL options

### Local backend on the tester machine

Use this option if the tester can run the backend locally.

1. Start the backend separately from `backend/EnglishVoiceTutor.Api`.
2. Configure backend secrets only in the backend environment.
3. Use this Backend URL in the desktop app:

```text
http://localhost:5000
```

### ngrok URL to your local backend

Use this option when the backend runs on the developer machine and the tester needs temporary remote access.

1. Start the backend locally.
2. Start ngrok for the backend port:

```powershell
ngrok http 5000
```

3. Test the ngrok URL manually before sending it:

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

4. Send only the `https` ngrok URL to the tester.
5. Tell the tester to paste that URL into Settings -> Backend URL.

ngrok URLs are temporary. If the ngrok session changes, send the tester the new Backend URL.

### Hosted backend

Use this option only if a hosted backend is available. Send the hosted Backend URL to the tester and keep all backend secrets on the hosted backend environment.

## Tester run instructions for another Windows device

Ask the tester to follow these steps:

1. Copy or download `EnglishVoiceTutor.Desktop-win-x64-self-contained.zip` on the target Windows device.
2. Extract the zip to a normal writable folder.
3. Open the extracted folder.
4. Run `EnglishVoiceTutor.Desktop.exe`.
5. Open Settings.
6. Set Backend URL.
7. In Audio input, select the microphone to test, or keep System default.
8. Click Test microphone and confirm the app does not crash.
9. Click Save.
10. Restart the app and confirm the selected microphone persists, or safely falls back to System default if unavailable.
11. Confirm Diagnostics is hidden by default in the Release package.
12. Log in to an account and verify session restore after restart.
13. Confirm backend lesson history is visible/preserved for the account.
14. Start a lesson.
15. Send a text answer.
16. Use Start/Stop recording and verify transcription.
17. Use Play voice / TTS.
18. Use Translate.
19. Use Hint.
20. Use Feedback.
21. Finish the lesson.
22. Check Summary.
23. Use Conversation Mode.
24. Verify single active lesson guard.
25. Verify heartbeat stale protection or run `tools\smoke_single_active_lesson_guard.ps1` with the required backend/test setup.
26. Verify remote active lesson release stops the old device/session.
27. Verify old heartbeat/message actions are rejected after remote release, or record that the smoke script covered it.
28. Close the app with X during an active lesson and confirm the process does not hang.

## Accepted current tester package result

The current tester zip flow has been manually verified on another Windows device:

- the tester zip was copied to another Windows device;
- the app launched after extraction;
- the packaged Release app hid Diagnostics by default;
- backend connection worked;
- account login worked;
- backend lesson history was available/preserved;
- Settings opened;
- account login/session restore worked;
- Home opened;
- Subtopics opened;
- Lesson Chat opened;
- Send worked;
- voice recording/transcription worked;
- TTS / Play voice worked;
- Translation worked;
- Hint worked;
- Feedback worked;
- Finish lesson worked;
- Summary appeared;
- Conversation Mode worked;
- single active lesson guard worked;
- heartbeat stale protection worked;
- remote active lesson release stopped the old device/session;
- old heartbeat/message actions were rejected after remote release;
- closing the app with X during an active lesson did not leave the process hanging.

## Deferred quality polishing

Do not continue polishing dialogue or prompt quality in code during this release packaging step. Prompt, scenario, and bot-behavior quality work is deferred to CMS/Admin so it can later be edited safely with validation, preview, versioning, and rollback.

## Required tester package report format

Use this report for every shared tester zip:

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
Old heartbeat/message rejected after remote release:
Close with X:
Known issues:
```

## Security notes

- Do not send `OPENAI_API_KEY` to testers.
- Do not paste `OPENAI_API_KEY` into the desktop app.
- The desktop app only needs Backend URL.
- OpenAI key stays only on the backend environment variable.
- The desktop app must call backend APIs only and must not call OpenAI directly.
- Diagnostics must not show or copy the key.
- `settings.json` must not contain the key.
- The tester package must not include `%APPDATA%\EnglishVoiceTutor.Desktop\settings.json`.
- The tester package must not include `%APPDATA%\EnglishVoiceTutor.Desktop\lesson-history.json`.

## Package output cleanup

Generated package output lives under `artifacts/` and should not be committed. The package script removes previous output for the selected package type before publishing so the zip does not include artifacts from previous runs.

## Step 5B-9 additional tester checks

- Account session restore: sign in, close the packaged app, reopen it on the same Windows user, and confirm the Account tab still shows the user as signed in while backend lesson history remains visible.
- Logout: sign out, close and reopen the app, and confirm the stored account session is cleared.
- Settings localization: switch Interface language to Russian and confirm Progress helper text is Russian, Account signed-out/status text is localized, and the Save button shows the full `Сохранить` label without clipping.
- Password reset is not available to testers unless it is explicitly enabled later after domain email/provider setup.
- The tester ZIP must not contain `auth-session.json`, local account tokens, OpenAI API keys, or provider secrets.
