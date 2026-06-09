# Windows installer and future update flow

Inno Setup is now the primary Windows direct-download installer foundation for Language Voice Tutor. See [`docs/WINDOWS_INSTALLER_RELEASE_FLOW.md`](WINDOWS_INSTALLER_RELEASE_FLOW.md) for the build and smoke workflow.

Velopack was rejected/deprecated for this project because its Windows installer is one-click and does not provide the desired release-like wizard with destination-directory selection. Do not send Velopack packages to external testers.

## Current installer foundation

- AppId: `LanguageVoiceTutor.Desktop`
- AppName: `Language Voice Tutor`
- Default install directory: `{autopf}\Language Voice Tutor`
- Start Menu shortcut: `Language Voice Tutor`
- Optional Desktop shortcut: `Language Voice Tutor`
- Expected installer artifact: `artifacts\installers\windows\LanguageVoiceTutorSetup-{version}.exe`
- Server-ready direct-download files: `artifacts\releases\windows\direct`

The existing executable remains `EnglishVoiceTutor.Desktop.exe` to avoid risky project-wide renames.

## Future update policy

Future direct-download updates should reuse the same Inno Setup AppId, `LanguageVoiceTutor.Desktop`, so installing a newer installer updates the existing installation.

No automatic update UX is implemented yet. The packaging script generates `artifacts\releases\windows\direct\latest.json` as a stable foundation for a future download page and future in-app update-check, but the current app does not read the manifest automatically and does not show update prompts. Future in-app update UX should download the same Inno installer and run it only after explicit user confirmation. It must not run during an active lesson and must not perform silent updates.

## Direct-download manifest files

`latest.json` is intentionally simple and future update-check friendly. It describes the Language Voice Tutor Windows x64 direct-tester installer with a relative installer URL, SHA-256 checksum, file size, `manual-confirmation` update mode, and notes that code signing is deferred and the desktop uses a manual-confirmation update flow. The same folder also contains `changelog.json`, `known-issues.json`, and `checksums.sha256`. These files are generated artifacts under `artifacts\` and must not be committed.

## Uninstall behavior

The installer should use standard Windows uninstall integration. Uninstall removes installed app files and shortcuts. It should not delete local user settings/session/cache by default and cannot delete backend account data.

## Deferred items

- Code signing is deferred, but required before broad public distribution.
- Microsoft Store/MSIX is deferred.
- Public release readiness is not claimed.

## Server upload boundary

The server-ready folder can be validated and optionally copied to a future static HTTPS server folder using the scripts documented in [`docs/WINDOWS_RELEASE_SERVER_UPLOAD.md`](WINDOWS_RELEASE_SERVER_UPLOAD.md). This upload preparation is manual only and does not deploy the backend, does not create a download website, and does not implement update UI. The current desktop app still does not fetch `latest.json`; future update UI must keep manual confirmation and must not prompt during an active lesson.
