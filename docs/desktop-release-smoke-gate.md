# Desktop Release Smoke Gate

Step: 5B desktop release gate, updated 2026-06-02 for accepted Welcome screen polish and Lesson Chat window auto-sizing checks.

## Purpose

This smoke gate is the repeatable pre-package safety check for the current desktop release-hardening phase. It confirms that the desktop app, backend build, lesson content, interface localization coverage, local backend readiness, and backend-unavailable resilience wording are still safe before creating the primary Inno tester installer with `scripts/package-windows-inno-release.ps1`.

This gate does not add product features. It does not change runtime localization behavior, billing, subscriptions, entitlements, Admin UI, database schema, lesson JSON, Study languages, Interface languages, or Native/Explanation language support.

Current audited release-blocking localization issues have been addressed for the 14 release-ready interface languages (`en`, `es`, `fr`, `de`, `it`, `pt`, `ru`, `pl`, `ar`, `ja`, `ko`, `sr`, `hr`, `bg`). Localization is not permanently complete. Future Interface languages must be added only after full UI localization and audit coverage. Run `tools/audit_interface_localization.py`; if `tools/test_welcome_layout_stability_policy.py` exists in the current branch, run it for home/welcome hero layout stability with long localized text.



## Backend URL profile check

Normal local desktop builds default to `http://localhost:5000`. The primary Inno tester/release package defaults to `https://api.languagevoicetutor.com` through the `DesktopBackendBaseUrl` MSBuild property passed by `scripts/package-windows-inno-release.ps1`; the script prints the Backend URL used. Empty saved settings use the current build default, saved legacy localhost settings can migrate to the deployed API only in those tester/release builds, and custom Backend URL values remain preserved. Settings/Diagnostics must still show the current Backend URL.

Validate packaged release metadata with:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\validate-windows-direct-release.ps1
```

The backend remains the server-side source of truth, the desktop must not contain OpenAI keys or call OpenAI directly, production billing remains deferred, and public release is still blocked until clean-machine install plus the controlled tester checklist pass.

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
3. Build the primary Inno installer from the repository root:

```powershell
cd C:\dev\EnglishVoiceTutor.Desktop
powershell -ExecutionPolicy Bypass -File .\scripts\package-windows-inno-release.ps1 -Version 0.1.0
```

4. Confirm the package script prints `Packaged backend URL: https://api.languagevoicetutor.com` unless a deliberate `-BackendBaseUrl` override is being tested.
5. Validate the server-ready direct-release folder:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\validate-windows-direct-release.ps1
```

6. Validate and upload through the canonical direct release scripts, keeping backend deploy separate from Windows release upload:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\upload-windows-direct-release.ps1 -Version 0.1.36-tester.16
```

Public direct Windows release files go to `/var/www/languagevoicetutor/releases/windows/direct`; the public website root is separate at `/var/www/languagevoicetutor/site`. Generated release artifacts must not be committed.

Use the script-created installer as the tester handoff artifact:

```text
artifacts\installers\windows\LanguageVoiceTutorSetup-{version}.exe
```

7. Copy/send the installer to another Windows device or clean VM.
8. Install and launch the app.
9. Verify backend connection, login/account, backend history, accepted core lesson flow, active lesson guard, and remote active lesson release.

Manual `dotnet publish` commands are lower-level implementation detail only. The ZIP package remains an emergency/developer fallback, not the main tester handoff flow.

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
dotnet ef migrations list --project backend/EnglishVoiceTutor.Api/EnglishVoiceTutor.Api.csproj --startup-project backend/EnglishVoiceTutor.Api/EnglishVoiceTutor.Api.csproj
dotnet ef database update --project backend/EnglishVoiceTutor.Api/EnglishVoiceTutor.Api.csproj --startup-project backend/EnglishVoiceTutor.Api/EnglishVoiceTutor.Api.csproj
dotnet ef migrations has-pending-model-changes --project backend/EnglishVoiceTutor.Api/EnglishVoiceTutor.Api.csproj --startup-project backend/EnglishVoiceTutor.Api/EnglishVoiceTutor.Api.csproj
```

The helper script does not run EF/database checks by default. To include them explicitly, run:

```powershell
powershell -ExecutionPolicy Bypass -File tools/run_desktop_release_gate.ps1 -IncludeEfChecks
```

Expected result:

- migrations can be listed;
- latest confirmed EF migration is `20260604121000_AddCmsDraftSaveAuditMetadata`;
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
- Check Welcome screen visual acceptance: large hero cover, neutral non-English-only text, compact translucent top text overlay, and visible Start lesson / Settings actions on the bottom overlay.
- Check Topic cards.
- Check Subtopics/Situations screen.
- Check Lesson Chat opens.
- Check Lesson Chat auto-size acceptance: entering Lesson Chat expands a too-small app window into a comfortable wide layout without fullscreen/maximize, keeps larger user-sized windows larger, and leaves avatar/chat columns visible and balanced.
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
- Check closing the app with X after entering Lesson Chat still works and does not show the previous close exception or leave the process hanging.
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
- Keep Language Voice Tutor global, cross-platform, and provider-agnostic.

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
- dotnet ef migrations list --project backend/EnglishVoiceTutor.Api/EnglishVoiceTutor.Api.csproj --startup-project backend/EnglishVoiceTutor.Api/EnglishVoiceTutor.Api.csproj:
- dotnet ef database update --project backend/EnglishVoiceTutor.Api/EnglishVoiceTutor.Api.csproj --startup-project backend/EnglishVoiceTutor.Api/EnglishVoiceTutor.Api.csproj:
- dotnet ef migrations has-pending-model-changes --project backend/EnglishVoiceTutor.Api/EnglishVoiceTutor.Api.csproj --startup-project backend/EnglishVoiceTutor.Api/EnglishVoiceTutor.Api.csproj:

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
- Welcome screen accepted hero layout:
- Lesson Chat auto-size accepted wide layout:
- Close after entering Lesson Chat has no previous close exception:
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
- Full production CMS/Admin readiness remains deferred: Admin CMS Content exists for development/admin editing with refresh resilience, unsaved-change warnings, Step 5D-6e Scenarios editor usability refinement (local Jump to navigation, collapsible/visually separated structured sections, helper text, structured fields as the normal path, and Advanced JSON as a technical fallback), draft-save audit logging, smoke/test audit filtering, required publish summary validation, immutable published versions/restore-as-new-version behavior, and local runtime published-snapshot read verification, but production RBAC and critical-change approval are not implemented.
- Prompt/scenario/bot-behavior quality polishing is deferred to CMS/Admin so it can support safe editing, validation, preview, versioning, rollback, and audited draft saves.
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
