# Tester release workflow

The recommended Windows tester handoff is now the Inno Setup installer documented in [`docs/WINDOWS_INSTALLER_RELEASE_FLOW.md`](WINDOWS_INSTALLER_RELEASE_FLOW.md), after the installer smoke checklist passes.

The older ZIP package created by `scripts/package-tester-release.ps1` remains available only as an emergency/developer fallback. Do not present the ZIP as the main tester handoff when the Inno installer is available and smoke-tested.

Velopack is deprecated/rejected for this project. Its Windows installer is a one-click flow and does not match the desired release-like installer UX with destination-directory selection. External testers should not receive Velopack packages.

## What the tester release is

The tester release is:

- a Windows installer named `LanguageVoiceTutorSetup-{version}.exe`;
- branded publicly as `Language Voice Tutor`;
- built from the desktop app publish output;
- intended to work with a separately reachable backend, either local, ngrok, or hosted;
- focused on checking launch, Settings, account login/session restore, backend history, Lesson Chat, voice recording/transcription, TTS, Conversation Mode, translation, hints, feedback, Summary, active lesson guard, and clean close behavior.

## What the tester release is not

This tester release is **not**:

- an MSIX package;
- Microsoft Store packaging;
- a code-signed public release;
- an auto-update system;
- a backend deployment;
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
5. Only then hand off the installer artifact.

Expected installer artifact:

```text
artifacts\installers\windows\LanguageVoiceTutorSetup-0.1.0.exe
```

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
- Start a lesson.
- Verify TTS/STT if the backend is running and configured.
- Install a newer version over an older version.
- Uninstall via Windows Settings / Installed Apps.
- Verify the install directory is removed.
- Verify user/backend account data is not deleted.
