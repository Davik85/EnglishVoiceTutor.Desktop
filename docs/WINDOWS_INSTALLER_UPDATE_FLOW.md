# Windows installer and manual update flow

Review date: 2026-06-12.

## Source of truth for current versions

These docs are a snapshot of the last known verified state. They can become stale and must not be used as the only source of truth for live versions. Always verify the live/public state before telling a tester that a version is current.

Check the public Windows direct tester release from the live website manifest:

```powershell
Invoke-RestMethod https://languagevoicetutor.com/releases/windows/direct/latest.json
```

Check the production backend release from the server `current` symlink:

```powershell
ssh lvt-server "readlink -f /opt/languagevoicetutor/backend/current"
```

Check production backend health and database health:

```powershell
Invoke-WebRequest https://api.languagevoicetutor.com/health -UseBasicParsing
Invoke-WebRequest https://api.languagevoicetutor.com/api/health/database -UseBasicParsing
```

Generated local files under `artifacts/` are not proof that a version is live on the public site. A locally built installer becomes public only after the Windows direct release files are uploaded to the website release folder and `latest.json` is verified over HTTPS.

Inno Setup is the primary Windows direct-download installer foundation for Language Voice Tutor. See [`docs/WINDOWS_INSTALLER_RELEASE_FLOW.md`](WINDOWS_INSTALLER_RELEASE_FLOW.md) for the build and smoke workflow.

## Current update status

The public tester Windows direct manifest baseline must be checked from live `https://languagevoicetutor.com/releases/windows/direct/latest.json`. Last verified public snapshot: it pointed to `LanguageVoiceTutorSetup-0.1.35-tester.1.exe`, kept `minimumSupportedVersion` at `0.1.35-tester.1`, and used `updateMode: manual-confirmation`. Local build `0.1.36-tester.2` has been built and validated locally, but it is not public/live unless the live website manifest points to it.

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

`latest.json` describes the Language Voice Tutor Windows x64 direct-tester installer with a relative installer URL, SHA-256 checksum, file size, `manual-confirmation` update mode, and notes that code signing is deferred. The same folder also contains `changelog.json`, `known-issues.json`, and `checksums.sha256`. These files are generated artifacts under `artifacts\` and must not be committed. Generated artifacts are not source of truth for the public/live Windows release until uploaded and verified through live `latest.json`.

## Uninstall/update behavior

The installer should use standard Windows uninstall integration. Uninstall removes installed app files and shortcuts. It should not delete local user settings/session/cache by default and cannot delete backend account data. Update/reinstall should preserve auth session, settings, Lesson History, and Progress. Updates from older `EnglishVoiceTutor.Desktop.*` installed builds should clean old installed files from the install folder and migrate local user data to the current `LanguageVoiceTutor.Desktop` path without losing login.

## Release backend requirement

Release/tester installed builds are server-only and use `https://api.languagevoicetutor.com`. Localhost and local backend switching are DEBUG/developer-only and not normal tester/release behavior.

## Deferred items

- Code signing is deferred, but required before broad public distribution.
- Microsoft Store/MSIX is deferred.
- Production billing/Paddle/subscription payment lifecycle remains deferred.
- Public production readiness is not claimed.
