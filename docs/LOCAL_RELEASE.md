# Local Windows release workflow

This document describes the local Windows release checks for `EnglishVoiceTutor.Desktop` without Visual Studio.

The recommended Windows direct-download installer path is now the Inno Setup flow in [`docs/WINDOWS_INSTALLER_RELEASE_FLOW.md`](WINDOWS_INSTALLER_RELEASE_FLOW.md). The emergency/developer fallback ZIP is still available through `scripts/package-tester-release.ps1`, but it is no longer the preferred external tester handoff when the Inno installer smoke passes.

## Naming note

Installed tester/release output files use `LanguageVoiceTutor.Desktop.*` names. Internal project, repository, folder, and source namespace names may still contain `EnglishVoiceTutor.*` until a later safe cleanup. Public-facing release naming is `Language Voice Tutor`, while avoiding risky project-wide namespace churn.

## Scope

This workflow is for local desktop release validation:

- run the automated release gate before packaging;
- build the Inno Setup installer for normal Windows installer smoke testing;
- optionally build the ZIP fallback for developer/emergency use;
- verify the app against a reachable backend;
- verify backend account login/session restore and backend lesson history;
- verify that no OpenAI API key is stored in the desktop app or publish output.

This workflow does **not** add or configure Microsoft Store/MSIX packaging, code signing, automatic updates, a deployed backend, a download website, upload to the public website, or public release readiness. Local artifacts are not proof of the live public version.

## Prerequisites

On the Windows development machine:

- .NET SDK that matches the project target framework;
- PowerShell;
- Inno Setup 6 for the installer flow;
- `curl` for backend checks;
- optional `ngrok` for testing against a temporary public backend URL.

The Inno installer defaults to Program Files through `{autopf}\Language Voice Tutor`, so installer smoke tests require administrator privileges.

## Required order

Run from the repository root:

```powershell
cd C:\dev\EnglishVoiceTutor.Desktop
powershell -ExecutionPolicy Bypass -File .\tools\run_desktop_release_gate.ps1
$ReleaseVersion = "<release-version>"
powershell -ExecutionPolicy Bypass -File .\scripts\package-windows-inno-release.ps1 -Version $ReleaseVersion
```

Expected installer output:

```text
artifacts\installers\windows\LanguageVoiceTutorSetup-{version}.exe
```

Expected server-ready direct-download output:

```text
artifacts\releases\windows\direct\LanguageVoiceTutorSetup-{version}.exe
artifacts\releases\windows\direct\latest.json
artifacts\releases\windows\direct\changelog.json
artifacts\releases\windows\direct\known-issues.json
artifacts\releases\windows\direct\checksums.sha256
```

Validate the direct-release metadata and checksums before server preparation or tester handoff:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\validate-windows-direct-release.ps1
```

Optional upload to a static HTTPS server folder is documented in [`docs/WINDOWS_RELEASE_SERVER_UPLOAD.md`](WINDOWS_RELEASE_SERVER_UPLOAD.md). The upload helper supports `-DryRun` and never runs automatically; backend deployment, database migrations, the download website, and update UI remain separate work. A local installer is public/live only after the Windows direct release files are uploaded and live `latest.json` is verified over HTTPS.

Copy the installer to another Windows device or clean VM, install it, choose a custom directory during smoke testing, launch the app, and verify backend connection, login/account, backend history, and the core lesson flow. Also verify the Settings footer displays the installed version, for example `Version: v0.1.0`; testers should report this value when filing bugs.

## Generated release artifacts

Latest verified status (not a local-artifact claim): Windows Direct 1.2 passed the Desktop release gate, packaging, and direct-release validation; an installed 1.1 application updated to 1.2, launched, and completed a lesson smoke.

`latest.json`, `changelog.json`, `known-issues.json`, `checksums.sha256`, installers, packages, and other files under `artifacts/` are generated outputs. Validate them locally and upload through the Windows direct release flow when intended, but do not commit them.

## Emergency/developer ZIP fallback

ZIP packaging remains available only as a developer/emergency fallback:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\package-tester-release.ps1
```

Expected default ZIP output:

```text
artifacts\packages\LanguageVoiceTutor.Desktop-win-x64-self-contained.zip
```

The ZIP flow is useful if the installer toolchain is unavailable, but it should not be presented as the main external tester handoff once the Inno installer smoke passes.

## Backend requirement

The packaged desktop app is backend-driven. A reachable backend is required for login/register/logout/session restore validation, backend lesson history, lesson start, AI bot replies, voice transcription/STT, TTS, translation, hints, feedback, summary, subscription/access checks, active lesson guard, and remote active lesson release.

The desktop app does not contain an OpenAI API key, must not call OpenAI directly, and must call backend APIs only.

## Deferred items

- Microsoft Store/MSIX was evaluated and discontinued for now.
- Code signing is deferred but required before broad public distribution.
- The release Settings UX includes a simple **Check for updates** button. It checks `latest.json`, validates the manifest, asks before download/install, verifies SHA-256 before launching the installer, and does not silently auto-update.
- Public release is not declared ready.
