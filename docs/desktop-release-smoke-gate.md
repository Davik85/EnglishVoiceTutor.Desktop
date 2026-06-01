# Desktop Release Smoke Gate

Step: 5B desktop release gate, updated 2026-06-01 for tester ZIP acceptance and active lesson heartbeat guard documentation.

## Purpose

This smoke gate is the repeatable pre-package safety check for the current desktop release-hardening phase. It confirms that the desktop app, backend build, lesson content, interface localization coverage, local backend readiness, and backend-unavailable resilience wording are still safe before creating the canonical tester zip with `scripts/package-tester-release.ps1`.

This gate does not add product features. It does not change runtime localization behavior, billing, subscriptions, entitlements, Admin UI, database schema, lesson JSON, Study languages, Interface languages, or Native/Explanation language support.

Localization is considered closed for the current release-hardening phase. Future Interface languages must be added only 1-2 at a time after full UI localization and audit coverage.

## When to run this gate

Run this gate:

- before sharing a desktop tester build;
- after any desktop UI, settings, localization, lesson flow, or release packaging change;
- after backend changes that affect desktop-facing APIs;
- before moving from the current hardening item to the next release-hardening item;
- before revisiting production billing readiness.

Do not use this gate as proof that production billing is ready. Production billing remains deferred until desktop hardening is complete and the separate Paddle production readiness checks pass.

## Current tester package flow

The current accepted desktop tester distribution flow is:

1. Run this automated release gate.
2. Run EF checks when backend schema changed or database validation is required.
3. Create the tester zip from the repository root:

```powershell
cd C:\dev\EnglishVoiceTutor.Desktop
powershell -ExecutionPolicy Bypass -File .\scripts\package-tester-release.ps1
```

4. Use the script-created zip as the tester handoff artifact:

```text
artifacts\packages\EnglishVoiceTutor.Desktop-win-x64-self-contained.zip
```

5. Copy/send the zip to another Windows device.
6. Extract the zip.
7. Run `EnglishVoiceTutor.Desktop.exe` from the extracted folder.
8. Verify backend connection, login/account, backend history, accepted core lesson flow, active lesson guard, and remote active lesson release.

Manual `dotnet publish` commands are lower-level implementation detail only. They are not the main tester handoff flow.

## Automated checks

Run these from the repository root. The PowerShell helper can run the safe local checks:

```powershell
powershell -ExecutionPolicy Bypass -File tools/run_desktop_release_gate.ps1
```

Equivalent manual commands:

```powershell
git status
dotnet restore
dotnet build
dotnet build -c Release
dotnet build backend/EnglishVoiceTutor.Api/EnglishVoiceTutor.Api.csproj
powershell -ExecutionPolicy Bypass -File tools/audit_lesson_content.ps1
powershell -ExecutionPolicy Bypass -File tools/audit_interface_localization.ps1
powershell -ExecutionPolicy Bypass -File tools/audit_desktop_backend_boundary.ps1
```

Expected result:

- `git status` is clean before the gate is reported as passed.
- Debug and Release builds pass.
- Backend build passes.
- Lesson content audit passes.
- Interface localization audit passes.
- Desktop backend-boundary audit passes.
- The automated helper does not require the backend to be running, does not require Python, does not require `OPENAI_API_KEY`, does not require secrets, and does not test live lesson actions.

## Backend checks

Run these from the repository root when local EF/database validation is required. Use the backend project path `backend/EnglishVoiceTutor.Api/EnglishVoiceTutor.Api.csproj`.

```powershell
dotnet ef migrations list --project backend/EnglishVoiceTutor.Api/EnglishVoiceTutor.Api.csproj
dotnet ef database update --project backend/EnglishVoiceTutor.Api/EnglishVoiceTutor.Api.csproj
dotnet ef migrations has-pending-model-changes --project backend/EnglishVoiceTutor.Api/EnglishVoiceTutor.Api.csproj
```

The helper script does not run EF/database checks by default. To include them explicitly, run:

```powershell
powershell -ExecutionPolicy Bypass -File tools/run_desktop_release_gate.ps1 -IncludeEfChecks
```

Expected result:

- migrations can be listed;
- latest confirmed EF migration is `20260601090000_AddLessonSessionHeartbeat`;
- database update applies no unexpected migrations for the current local database;
- `has-pending-model-changes` reports no pending model changes.

### Backend run for local desktop smoke testing

Start the backend locally in Development before manual desktop checks that require backend APIs:

```powershell
cd C:\dev\EnglishVoiceTutor.Desktop\backend\EnglishVoiceTutor.Api
$env:ASPNETCORE_ENVIRONMENT="Development"
$env:DOTNET_ENVIRONMENT="Development"
$env:SubscriptionEnforcement__Enabled="true"
dotnet run
```

Environment variable meaning:

- `ASPNETCORE_ENVIRONMENT="Development"`: runs the ASP.NET backend in local development mode.
- `DOTNET_ENVIRONMENT="Development"`: tells .NET hosting to use development configuration.
- `SubscriptionEnforcement__Enabled="true"`: keeps backend access/limit checks enabled for realistic local testing.

`OPENAI_API_KEY` is not needed for UI-only smoke testing. If real AI/TTS/STT functions are tested manually, set it only as a local backend environment variable and never in desktop:

```powershell
$env:OPENAI_API_KEY="PASTE_YOUR_LOCAL_OPENAI_KEY_HERE"
```

Never commit a real key, never paste a real key into docs, and never send a key to testers. The desktop app does not contain an OpenAI key, must not call OpenAI directly, and must call backend APIs only for AI/TTS/STT.

## Manual desktop checks

Manual desktop checks are split into two different scopes. Do not mix the expectations.

### Section A — Backend unavailable resilience checks

These checks are resilience-only. The desktop app is backend-driven, so a stopped or unreachable backend is not a functional lesson-test environment.

- Stop the backend or set Backend URL to a known invalid local URL.
- Start the desktop app while the backend is not running or not reachable.
- Confirm the app does not crash.
- Open Settings.
- Check Learning tab.
- Check Account tab.
- Check Audio tab.
- Check Progress tab.
- Confirm the Account tab shows a friendly localized backend-unavailable or connection message when an account action requires backend.
- Do not expect login, register, lesson start, lesson message Send, Hint, Translate, Play voice/TTS, transcription, Conversation Mode, Finish lesson, or Summary generation to succeed while backend is unavailable.
- If the user tries a backend-required action, confirm the app shows a friendly localized backend-required, backend-unavailable, or connection message and returns to a usable state.
- Confirm normal learner-facing UI does not show a raw stack trace or unhandled exception text.

With backend off, lesson/AI actions should not crash and should show a friendly backend-required or backend-unavailable message. With backend running, these actions should work as described in Section B.

### Section B — Backend running functional checks

Start the local backend in Development before these checks. Full functional lesson flow requires backend APIs for login, history, AI, voice/TTS/STT, translation, hints, feedback, summary, subscription/access checks, and active lesson guard.

- Start desktop app.
- Open Settings and save the Backend URL for the running backend.
- Check Diagnostics visibility behavior:
  - Diagnostics visible in Debug if expected.
  - Diagnostics hidden in packaged Release by default unless `EVT_DESKTOP_DIAGNOSTICS=1` is set locally before launch.
  - `EVT_DESKTOP_DIAGNOSTICS=1` is not committed.
  - Diagnostics and copied output continue masking secrets, tokens, API keys, environment variables, lesson messages, audio paths, and lesson history content.
- Check Study language list remains exactly:
  - English
  - French
  - German
  - Portuguese
  - Spanish
  - Italian
- Check Native language list remains broad.
- Check Interface language list remains the release-ready list:
  - English
  - Español
  - Français
  - Deutsch
  - Italiano
  - Português
  - Русский
  - Polski
  - العربية
  - 日本語
  - 한국어
  - Српски
  - Hrvatski
  - Български
- Check Home screen.
- Check Topic cards.
- Check Subtopics/Situations screen.
- Check Lesson Chat opens.
- Check Send with a text message.
- Check voice recording/transcription.
- Check Play voice/TTS if audio and backend AI/audio configuration are available.
- Check Translation show/hide.
- Check Hint button.
- Check Feedback.
- Check Finish lesson.
- Check Lesson Summary.
- Check Conversation Mode.
- Check Account Login/Logout and session restore flows as applicable.
- Check backend lesson history is visible/preserved.
- Check single active lesson guard.
- Check heartbeat stale protection.
- Check remote active lesson release stops the old device/session.
- Check old heartbeat and old lesson-bound messages are rejected after remote release, or run `tools/smoke_single_active_lesson_guard.ps1` with the required backend/test setup.
- Check closing the app with X during an active lesson does not leave the process hanging.
- Check Back navigation.

## Localization checks

Verify these Interface language paths manually after automated localization audit passes:

- English UI.
- Russian UI.
- Serbian UI.
- Japanese UI.
- One European UI language, for example Polish or German.
- One RTL language, Arabic.
- Verify Settings, Home, Subtopics, Lesson Chat, and Summary do not show unexpected English text when a non-English Interface language is selected.
- Lesson content and generated bot messages are not part of interface localization and may remain in the study language or backend-generated language.

Release-ready Interface language IDs remain exactly:

- `en`
- `es`
- `fr`
- `de`
- `it`
- `pt`
- `ru`
- `pl`
- `ar`
- `ja`
- `ko`
- `sr`
- `hr`
- `bg`

## What must not change

- Do not change Study languages.
- Do not change lesson JSON.
- Do not change billing/Paddle/subscription/entitlement/Admin UI.
- Do not create EF migrations.
- Do not add secrets.
- Do not add OpenAI API key to desktop.
- Do not let desktop call OpenAI directly.
- Do not narrow Native/Explanation language support.
- Do not expand Interface languages without full localization QA.
- Do not add YooKassa, Russian payment flows, or Russia-only billing assumptions.
- Keep English Voice Tutor global, cross-platform, and provider-agnostic.

## Required smoke gate report format

Record the smoke gate result with this format:

```text
Desktop release smoke gate result:
Date:
Branch/commit:
Environment:

Automated checks:
- git status:
- dotnet restore:
- dotnet build:
- dotnet build -c Release:
- backend dotnet build:
- lesson content audit:
- interface localization audit:
- desktop backend boundary audit:
- single active lesson guard smoke, if run (`tools/smoke_single_active_lesson_guard.ps1`):

Backend/EF checks, if run:
- dotnet ef migrations list:
- dotnet ef database update:
- dotnet ef migrations has-pending-model-changes:

Backend unavailable resilience:
- app starts:
- Settings opens:
- Account backend-required message:
- lesson/AI actions blocked or fail gracefully:
- no raw exception:

Backend running functional flow:
- backend started:
- SubscriptionEnforcement__Enabled=true:
- Settings/Diagnostics:
- Study language list:
- Native language list:
- Interface language list:
- Home/Subtopics/Lesson Chat:
- Send:
- Hint:
- Translate:
- Play voice:
- Finish/Summary:
- Account Login/Logout:
- Backend lesson history visible/preserved:
- Active lesson guard:
- Heartbeat stale protection:
- Remote active lesson release stops old device/session:
- Old heartbeat/message rejected after remote release:
- Back navigation:

Localization checks:
- English:
- Russian:
- Serbian:
- Japanese:
- Polish or German:
- Arabic RTL:

Forbidden-change confirmation:
- Study languages unchanged:
- Interface languages unchanged:
- Native/Explanation languages remain broad:
- Lesson JSON unchanged:
- No EF migration created:
- No secrets added:
- Billing/Paddle/subscription/entitlement/Admin UI unchanged:
- Backend AI behavior unchanged:

Known issues or follow-ups:
Release decision:
```

## Required tester package report format

Record each shared tester package with this format:

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

## Known deferred items

- Production billing readiness remains deferred until desktop hardening is complete.
- Production Paddle configuration, webhook delivery, provider credentials, product/price mapping, and environment separation remain separate readiness work.
- Refund, chargeback, manual revocation automation, optional subscription reconciliation, and future mobile entitlement bridge work remain deferred.
- Full production CMS/Admin scope remains deferred until desktop readiness and minimum operational support requirements are clear.
- Prompt/scenario/bot-behavior quality polishing is deferred to CMS/Admin so it can later support safe editing, validation, preview, versioning, and rollback.
- Clean-machine installer/signing validation remains a later release packaging gate.
- Public release is not implied by passing this smoke gate; final P0/P1 triage is still required.

## Next recommended phase after this gate

After this smoke gate passes, continue to the next approved desktop hardening item in `docs/desktop-release-work-plan.md`. Keep production billing deferred until the desktop hardening gate and final release triage are complete.

## Step 5B-5 backend-unavailable and account UX hardening

Step 5B-5 adds focused desktop hardening for backend-unavailable, slow, or failed backend requests. Backend-unavailable testing is resilience-only: the app should not crash, Settings/Account should not break, and backend-required lesson or AI actions should be blocked, unavailable, or fail gracefully with short localized messages. Full lesson functionality must be tested only with the backend running.

This step does not change billing, Paddle, subscription, entitlement, Admin UI, lesson JSON, database schema, EF migrations, or backend AI behavior. Desktop AI-related actions continue to call backend APIs only.

## Step 5B-9 account and localization gate

- Packaged app session restore: login → close app → reopen app → confirm the same Windows user remains signed in and backend history is still visible.
- Logout clears the protected stored session and the next app launch remains signed out.
- Russian Settings check: Progress text must not contain Spanish, Account signed-out/status text must not contain English fallback, and the Save button must show `Сохранить` without clipping.
- Password reset is backend-foundation-only and disabled/not exposed as a working tester flow until email delivery is configured.
