# Desktop Release Smoke Gate

Step: 5B-4, updated by Step 5B-5b wording correction.

## Purpose

This smoke gate is the repeatable safety check for the current desktop release-hardening phase. It confirms that the desktop app, backend build, lesson content, interface localization coverage, local backend readiness, and backend-unavailable resilience wording are still safe after changes.

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

No new environment variables are required for Step 5B-4. `OPENAI_API_KEY` is not needed for UI-only smoke testing. If real AI functions are tested manually, `OPENAI_API_KEY` must be set only locally and must never be committed.

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

Start the local backend in Development before these checks. Full functional lesson flow requires backend APIs.

- Start desktop app.
- Open Settings and save the Backend URL for the running backend.
- Check Diagnostics visibility behavior:
  - Diagnostics visible in Debug if expected.
  - Diagnostics hidden in Release by default unless `EVT_DESKTOP_DIAGNOSTICS=1` is set.
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
- Check Hint button.
- Check Translation show/hide.
- Check Play voice/TTS if audio and backend AI/audio configuration are available.
- Check Finish lesson.
- Check Lesson Summary.
- Check Account Login/Logout flows as applicable.
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

## Required report format

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

## Known deferred items

- Production billing readiness remains deferred until desktop hardening is complete.
- Production Paddle configuration, webhook delivery, provider credentials, product/price mapping, and environment separation remain separate readiness work.
- Refund, chargeback, manual revocation automation, optional subscription reconciliation, and future mobile entitlement bridge work remain deferred.
- Full production CMS/Admin scope remains deferred until desktop readiness and minimum operational support requirements are clear.
- Clean-machine installer/signing validation remains a later release packaging gate.
- Public release is not implied by passing this smoke gate; final P0/P1 triage is still required.

## Next recommended phase after this gate

After this smoke gate passes, continue to the next approved desktop hardening item in `docs/desktop-release-work-plan.md`. Keep production billing deferred until the desktop hardening gate and final release triage are complete.

## Step 5B-5 backend-unavailable and account UX hardening

Step 5B-5 adds focused desktop hardening for backend-unavailable, slow, or failed backend requests. Backend-unavailable testing is resilience-only: the app should not crash, Settings/Account should not break, and backend-required lesson or AI actions should be blocked, unavailable, or fail gracefully with short localized messages. Full lesson functionality must be tested only with the backend running.

This step does not change billing, Paddle, subscription, entitlement, Admin UI, lesson JSON, database schema, EF migrations, or backend AI behavior. Desktop AI-related actions continue to call backend APIs only.
