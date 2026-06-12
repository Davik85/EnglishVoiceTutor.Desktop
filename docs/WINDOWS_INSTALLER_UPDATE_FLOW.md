# Windows installer and manual update flow

Review date: 2026-06-12.

Inno Setup is the primary Windows direct-download installer foundation for Language Voice Tutor. See [`docs/WINDOWS_INSTALLER_RELEASE_FLOW.md`](WINDOWS_INSTALLER_RELEASE_FLOW.md) for the build and smoke workflow.

## Current update status

`0.1.28-tester.1` is the current public tester Windows direct manifest baseline. The public tester download page and the desktop manual update flow both use `https://languagevoicetutor.com/releases/windows/direct/latest.json`, which points to `LanguageVoiceTutorSetup-0.1.28-tester.1.exe`, keeps `minimumSupportedVersion` at `0.1.28-tester.1`, and uses `updateMode: manual-confirmation`.

The desktop release UX has a simple user-facing **Check for updates** button in Settings. The old technical update dashboard in Diagnostics is not part of release UX. Release Settings must not expose Diagnostics or Backend URL editing.

## Manual update policy

Direct-download updates reuse the same Inno Setup AppId, `LanguageVoiceTutor.Desktop`, so installing a newer installer updates the existing installation.

The update flow is manual-confirmation only:

1. The user chooses **Check for updates**.
2. The app fetches `latest.json`.
3. The app validates product identity, app id, Windows platform, and x64 architecture.
4. The app compares the installed version with the manifest version.
5. The app asks before downloading an available update.
6. The app verifies SHA-256 before launching the installer.
7. The app asks before launching the installer.

The app does not silently auto-update and must not launch an installer before SHA-256 verification.

## Direct-download manifest files

`latest.json` describes the Language Voice Tutor Windows x64 direct-tester installer with a relative installer URL, SHA-256 checksum, file size, `manual-confirmation` update mode, and notes that code signing is deferred. The same folder also contains `changelog.json`, `known-issues.json`, and `checksums.sha256`. These files are generated artifacts under `artifacts\` and must not be committed.

## Uninstall/update behavior

The installer should use standard Windows uninstall integration. Uninstall removes installed app files and shortcuts. It should not delete local user settings/session/cache by default and cannot delete backend account data. Update/reinstall should preserve auth session, settings, Lesson History, and Progress.

## Release backend requirement

Release/tester installed builds are server-only and use `https://api.languagevoicetutor.com`. Localhost and local backend switching are DEBUG/developer-only and not normal tester/release behavior.

## Deferred items

- Code signing is deferred, but required before broad public distribution.
- Microsoft Store/MSIX is deferred.
- Production billing/Paddle/subscription payment lifecycle remains deferred.
- Public production readiness is not claimed.
