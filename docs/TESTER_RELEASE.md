# Tester release package workflow

This document describes a simple local workflow for creating and sharing a tester zip package for the first external/manual MVP tests of `EnglishVoiceTutor.Desktop`.

## What this tester release is

This tester release is:

- a zip package created from a published Windows desktop app folder;
- a repeatable local workflow for manual MVP testing;
- intended for testers who will run `EnglishVoiceTutor.Desktop.exe` directly;
- intended to work with a separately running backend, either local, ngrok, or hosted;
- focused on checking launch, Settings, Diagnostics, Lesson Chat, voice recording, bot voice, Conversation Mode, Summary, and History.

## What this tester release is not

This tester release is **not**:

- an installer;
- an MSIX package;
- Microsoft Store packaging;
- a code-signed release;
- an auto-update system;
- a backend deployment;
- a place to store or distribute any OpenAI API key.

Keep the MVP tester release simple until installer, signing, and hosting requirements are decided.

## Package types

The default tester package is self-contained so early testers can unzip the package and run the app without manually installing `windowsdesktop-runtime-10` or any other .NET Desktop Runtime:

```text
artifacts/packages/EnglishVoiceTutor.Desktop-win-x64-self-contained.zip
```

The self-contained package is larger because it includes the required runtime components. This is the recommended package for tester releases.

A framework-dependent package is still available as an advanced smaller option for developer checks or controlled machines that already have the matching .NET Desktop Runtime installed:

```text
artifacts/packages/EnglishVoiceTutor.Desktop-win-x64-framework-dependent.zip
```

## Prepare the app package

Run these commands from the repository root on the Windows development machine:

```powershell
cd C:\dev\EnglishVoiceTutor.Desktop
powershell -ExecutionPolicy Bypass -File .\scripts\package-tester-release.ps1
```

The script publishes the desktop app to:

```text
artifacts/publish/win-x64-self-contained
```

Then it creates this tester zip:

```text
artifacts/packages/EnglishVoiceTutor.Desktop-win-x64-self-contained.zip
```

To create the advanced framework-dependent package instead:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\package-tester-release.ps1 -FrameworkDependent
```

That command publishes to:

```text
artifacts/publish/win-x64-framework-dependent
```

And creates:

```text
artifacts/packages/EnglishVoiceTutor.Desktop-win-x64-framework-dependent.zip
```

The package script does not require administrator privileges, does not publish or modify the backend, does not create or modify `%APPDATA%` settings, and does not include local lesson history.

## Before sending to a tester

- Start the backend locally or prepare a temporary ngrok URL to the backend.
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

## Tester run instructions

Ask the tester to follow these steps:

1. Unzip the package.
2. Run `EnglishVoiceTutor.Desktop.exe`.
3. Open Settings.
4. Set Backend URL.
5. Click Save.
6. Open Diagnostics.
7. Click Refresh diagnostics.
8. Confirm backend is connected.
9. Confirm AI is configured or not configured.
10. Start lesson.
11. Send text answer.
12. Use Hint.
13. Use Translate.
14. Use Start/Stop recording.
15. Use Play voice.
16. Use Conversation Mode.
17. Finish lesson.
18. Check Summary.
19. Check History.
20. Restart app and confirm settings/history remain.

## What to verify

During the tester run, verify:

- the app opens from the unzipped folder;
- Settings can save the Backend URL;
- Diagnostics refresh works;
- Diagnostics backend status matches the configured Backend URL;
- Diagnostics AI status is shown as configured or not configured;
- Diagnostics does not show any OpenAI API key;
- Lesson Chat works with text answers;
- Hint works;
- Translate works;
- Start/Stop recording works;
- Play voice works;
- Conversation Mode works;
- Finish lesson opens Summary;
- completed lessons appear in History;
- settings and lesson history remain after app restart.

## Feedback and logs to request

Ask the tester to send back:

- the completed feedback template below;
- a screenshot of Diagnostics;
- screenshots or exact text for any error messages;
- what they clicked immediately before a crash or failure;
- whether the Backend URL was local, ngrok, or hosted;
- whether the issue still happens after restarting the app.

Do not ask testers to send API keys. If you need settings or history files for debugging, check them first and remove any private personal data before sharing further.

## Feedback template

```text
- Windows version:
- App package type:
- Backend URL type:
  - local / ngrok / hosted
- App opened:
  yes / no
- Diagnostics backend status:
- Diagnostics AI status:
- Lesson Chat worked:
  yes / no
- Voice recording worked:
  yes / no
- Bot voice worked:
  yes / no
- Conversation Mode worked:
  yes / no
- Summary worked:
  yes / no
- History worked:
  yes / no
- Any crashes/errors:
- Screenshot of Diagnostics:
- Notes:
```

## Security notes

- Do not send `OPENAI_API_KEY` to testers.
- Do not paste `OPENAI_API_KEY` into the desktop app.
- The desktop app only needs Backend URL.
- OpenAI key stays only on the backend environment variable.
- Diagnostics must not show the key.
- `settings.json` must not contain the key.
- The tester package must not include `%APPDATA%\EnglishVoiceTutor.Desktop\settings.json`.
- The tester package must not include `%APPDATA%\EnglishVoiceTutor.Desktop\lesson-history.json`.

## Package output cleanup

Generated package output lives under `artifacts/` and should not be committed. The package script removes previous output for the selected package type before publishing so the zip does not include artifacts from previous runs.
