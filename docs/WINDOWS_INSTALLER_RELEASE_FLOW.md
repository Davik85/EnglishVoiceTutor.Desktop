# Windows installer release flow

Inno Setup is the primary Windows direct-download installer track for Language Voice Tutor. This replaces the temporary Velopack tester installer track before external tester handoff.

## Decision

- Primary installer technology: Inno Setup 6.
- Public product name: Language Voice Tutor.
- Desktop window title: Language Voice Tutor Desktop.
- Stable installer AppId: `LanguageVoiceTutor.Desktop`.
- Default install directory: `{autopf}\Language Voice Tutor`, normally under Program Files.
- Expected installer artifact: `artifacts\installers\windows\LanguageVoiceTutorSetup-{version}.exe`.
- Server-ready direct-download output: `artifacts\releases\windows\direct`.
- Existing executable name remains `EnglishVoiceTutor.Desktop.exe` to avoid risky project-wide renames.

Velopack was rejected/deprecated for this project because its Windows installer is a one-click installer and does not match the desired release-like wizard UX. External testers should not be sent Velopack packages.

ZIP packaging remains only an emergency/developer fallback. Microsoft Store/MSIX remains deferred. Code signing is deferred for now, but it is required before broad public distribution.

## Installer behavior

The Inno Setup installer:

- shows a normal wizard;
- shows the destination directory selection page;
- creates normal Windows Installed Apps / uninstall integration;
- creates a Start Menu shortcut named `Language Voice Tutor`;
- offers an optional Desktop shortcut named `Language Voice Tutor`;
- offers an optional final wizard action to launch Language Voice Tutor after installation;
- installs published app files only from `artifacts\publish\win-x64-inno`;
- does not package local app data, local auth session files, local settings, lesson history, backend environment files, or secrets.

Because the default installation directory is under Program Files, the installer requires administrator privileges. That is acceptable for the release-like direct-download installer track and should be documented for local smoke tests.

Standard uninstall removes installed application files and shortcuts. It should not delete user settings, session/account state, cache, or backend account data by default because those are outside the install directory and/or owned by the backend.

## Future updates

Future Windows direct-download updates should reuse the same AppId, `LanguageVoiceTutor.Desktop`, so installing a newer Inno installer updates the existing installation.

No automatic update UX is implemented yet. A future in-app update UX may download the same Inno installer and run it only after explicit user confirmation. It must not run during an active lesson and must not introduce silent updates. The generated `latest.json` manifest is intended for a future download page and future in-app update-check, but the current app does not fetch it automatically.

## Install Inno Setup locally

1. Download Inno Setup 6 from https://jrsoftware.org/isinfo.php.
2. Install it with the default path when possible.
3. Confirm that `ISCC.exe` exists in one of the supported locations:
   - `C:\Program Files (x86)\Inno Setup 6\ISCC.exe`
   - `C:\Program Files\Inno Setup 6\ISCC.exe`
4. If Inno Setup is installed somewhere else, pass `-IsccPath` to the packaging script.

## Build the installer locally

Run from the repository root on Windows:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\package-windows-inno-release.ps1 -Version 0.1.0
```

For prerelease builds:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\package-windows-inno-release.ps1 -Version 0.1.0-beta.1
```

If `ISCC.exe` is not in a default location:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\package-windows-inno-release.ps1 -Version 0.1.0 -IsccPath "C:\Tools\Inno Setup 6\ISCC.exe"
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

`latest.json` includes product identity, platform, architecture, channel, version, UTC release date, installer filename/relative URL, SHA-256, size, minimum supported version, manual-confirmation update mode, and current release notes. It must not include absolute local file paths. `changelog.json` and `known-issues.json` are placeholders for tester-facing release communication until richer release notes are supplied.

Generated files under `artifacts\` must not be committed.

## Smoke checklist

Before external handoff, verify on a clean or representative Windows machine:

- install the generated installer;
- choose a custom destination directory;
- use the optional launch-after-install action;
- launch from the Start Menu shortcut;
- create and launch from the optional Desktop shortcut;
- verify backend URL configuration and login/session behavior;
- verify Settings shows `Version: v{version}` and ask testers to include that Settings version when reporting bugs;
- start a lesson;
- verify TTS/STT when the backend is running and configured;
- install a newer version over an older version and confirm the same AppId upgrade path works;
- uninstall via Windows Settings / Installed Apps;
- verify the install directory is removed;
- verify user/backend account data is not deleted.

## Backend and secrets boundaries

The backend remains the source of truth. The desktop must not store OpenAI API keys and must not call OpenAI directly. Do not place secrets, local `.env` files, local auth/session files, local settings, or local lesson history in the publish output or installer.
