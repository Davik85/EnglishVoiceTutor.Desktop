# Tester release workflow


## Current approval status

Version `0.1.8-tester.1` passed the basic internal end-to-end smoke test against the real production-like server setup. This confirms internal smoke readiness only. Tester handoff is **not approved yet**, and Language Voice Tutor is not publicly released.

Confirmed in the internal smoke test: app start, registration, trial entitlement, lesson start, normal lesson chat, Conversation Mode, TTS in normal chat and Conversation Mode, translation, feedback, hints, lesson history saving, active/restored session behavior after closing and reopening, backend reachability at `https://api.languagevoicetutor.com`, healthy backend health endpoint, healthy database health endpoint, applied PostgreSQL migrations, working static Windows direct release hosting, available production-domain `latest.json` for `0.1.8-tester.1`, and generated/validated/uploaded Windows installer release files verified on the server.

## Tester handoff blockers

External tester handoff remains blocked by:

1. Password recovery / password reset flow.
2. Password change flow for signed-in users.
3. CMS/Admin content flow connected and verified on the server.
4. Basic public download website/page where testers can download the installer.
5. Basic update system / update UI so testers can update from inside the app or through a clear guided flow.

## Minimum tester handoff checklist

Before the first controlled external tester receives Language Voice Tutor, confirm all items below:

- Password recovery / password reset works end to end.
- Signed-in password change works end to end.
- CMS/Admin content flow is connected and verified on the server.
- Public tester download page exists and points to the correct Windows installer.
- Basic update system / update UI exists and gives testers a clear manual update path.
- Clean-machine tester release smoke passes for install, launch, registration/login/session restore, trial entitlement, lesson start, normal chat, Conversation Mode, TTS, translation, feedback, hints, lesson history, active/restored session behavior, update guidance, and uninstall/upgrade expectations.
- Generated files under `artifacts/` are not committed.
- No passwords, API keys, tokens, private keys, provider credentials, private environment values, or personal credentials are committed or included in release docs.
- Code signing remains deferred and expected SmartScreen warnings are documented for controlled testers.


## Backend URL profile

Local development builds default to `http://localhost:5000`. The primary Inno tester/release installer flow defaults packaged builds to `https://api.languagevoicetutor.com` by passing `DesktopBackendBaseUrl` during publish; use `-BackendBaseUrl` only when intentionally testing another absolute http/https backend. Settings/Diagnostics continue to show the current Backend URL so tester reports can confirm the profile in use.

Existing installed-user settings are handled conservatively: empty Backend URL values use the current build default, saved legacy `http://localhost:5000` values migrate to `https://api.languagevoicetutor.com` only in tester/release builds where that is the build default, and custom values are preserved.

The backend remains the source of truth. The desktop must not store OpenAI API keys and must not call OpenAI directly. Production billing remains deferred, public release is not approved, and external tester handoff remains blocked until password recovery/change, server-connected CMS verification, a public download page, update UI/system, clean-machine install, and the controlled tester checklist pass.

The recommended Windows tester handoff is now the Inno Setup installer documented in [`docs/WINDOWS_INSTALLER_RELEASE_FLOW.md`](WINDOWS_INSTALLER_RELEASE_FLOW.md), after the installer smoke checklist passes.

The older ZIP package created by `scripts/package-tester-release.ps1` remains available only as an emergency/developer fallback. Do not present the ZIP as the main tester handoff when the Inno installer is available and smoke-tested.

Velopack is deprecated/rejected for this project. Its Windows installer is a one-click flow and does not match the desired release-like installer UX with destination-directory selection. External testers should not receive Velopack packages.

The server-ready direct-download folder can now be validated locally and dry-run uploaded with the manual helper documented in [`docs/WINDOWS_RELEASE_SERVER_UPLOAD.md`](WINDOWS_RELEASE_SERVER_UPLOAD.md). Server upload is prepared but not executed automatically, and it does not deploy the backend, create the download website, or add update UI. External tester handoff remains blocked until password recovery/change, server-connected CMS verification, a basic public download page, update UI/system, clean-machine install, and the controlled tester checklist pass.

## What the tester release is

The tester release is:

- a Windows installer named `LanguageVoiceTutorSetup-{version}.exe`;
- server-ready direct-download files under `artifacts\releases\windows\direct`;
- branded publicly as `Language Voice Tutor`;
- built from the desktop app publish output;
- intended to work with the deployed hosted backend by default, with local/ngrok/custom backends only for deliberate overrides;
- focused on checking launch, Settings, account login/session restore, backend history, Lesson Chat, voice recording/transcription, TTS, Conversation Mode, translation, hints, feedback, Summary, active lesson guard, and clean close behavior.

## What the tester release is not

This tester release is **not**:

- an MSIX package;
- Microsoft Store packaging;
- a code-signed public release;
- an auto-update system or update-check UI;
- a backend deployment;
- proof that external tester handoff is approved;
- proof that public release is ready;
- proof that production billing is ready;
- a place to store or distribute any OpenAI API key.

## Required order before sharing

1. Run the automated desktop release gate from the repository root:

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\run_desktop_release_gate.ps1
```

2. Build the Inno Setup installer:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\package-windows-inno-release.ps1 -Version 0.1.0
```

3. Smoke-test the generated installer on another Windows device or clean VM.
4. Verify install directory selection, launch-after-install, Start Menu shortcut, optional Desktop shortcut, login/session, lesson start, TTS/STT with backend, over-install upgrade, and uninstall.
5. Do not hand off the installer artifact yet unless the Tester handoff blockers and Minimum tester handoff checklist above are complete.

Expected installer artifact:

```text
artifacts\installers\windows\LanguageVoiceTutorSetup-0.1.0.exe
```

Expected server-ready direct-download files:

```text
artifacts\releases\windows\direct\LanguageVoiceTutorSetup-0.1.0.exe
artifacts\releases\windows\direct\latest.json
artifacts\releases\windows\direct\changelog.json
artifacts\releases\windows\direct\known-issues.json
artifacts\releases\windows\direct\checksums.sha256
```

`latest.json` for `0.1.8-tester.1` is available from the production domain and the server-side release files were verified. The manifest is still for a future download page and future in-app update-check only. The current app does not check this manifest automatically. Future update UI must require manual confirmation and must not run during an active lesson.

## Emergency/developer ZIP fallback

If the installer toolchain is unavailable, create the fallback ZIP:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\package-tester-release.ps1
```

Expected ZIP:

```text
artifacts\packages\LanguageVoiceTutor.Desktop-win-x64-self-contained.zip
```

The ZIP script remains intentionally simple and does not require administrator privileges. It must not include local settings, local lesson history, auth sessions, `.env` files, secrets, or API-key-like files.

## Backend requirement

The packaged desktop app is still backend-driven. A reachable backend is required for account registration, login, logout, session restore validation, backend lesson history, lesson start and continuation, AI bot replies, voice transcription/STT, TTS, translation, hints, feedback, final summary, subscription/access checks, active lesson guard, and remote active lesson release.

The desktop app does not contain an OpenAI API key, must not call OpenAI directly, and must call backend APIs only. All AI/TTS/STT requests go through the backend.

## Smoke checklist

- Install the generated Inno installer.
- Choose a custom install directory.
- Use launch-after-install.
- Launch from the Start Menu shortcut.
- Create and launch from the optional Desktop shortcut.
- Verify backend URL configuration and login/session behavior.
- Verify the Settings footer shows the installed app version, and tell testers to include that version in bug reports.
- Start a lesson.
- Verify TTS/STT if the backend is running and configured.
- Install a newer version over an older version.
- Uninstall via Windows Settings / Installed Apps.
- Verify the install directory is removed.
- Verify user/backend account data is not deleted.

## Password recovery/change tester note (2026-06-08)

Password recovery/reset and signed-in password change are implemented for the desktop Account settings flow and backend API. Password reset email delivery requires SMTP settings on the server in `/etc/languagevoicetutor/backend.env`; real SMTP credentials must never be committed. The intended production sender is `support@languagevoicetutor.com`.

Tester handoff is not ready yet. Remaining blockers are CMS server verification, a basic public download page, a basic update UI/system, clean-machine smoke, and final checklist completion.
