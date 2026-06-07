# Windows installer and future update flow

Inno Setup is now the primary Windows direct-download installer foundation for Language Voice Tutor. See [`docs/WINDOWS_INSTALLER_RELEASE_FLOW.md`](WINDOWS_INSTALLER_RELEASE_FLOW.md) for the build and smoke workflow.

Velopack was rejected/deprecated for this project because its Windows installer is one-click and does not provide the desired release-like wizard with destination-directory selection. Do not send Velopack packages to external testers.

## Current installer foundation

- AppId: `LanguageVoiceTutor.Desktop`
- AppName: `Language Voice Tutor`
- Default install directory: `{autopf}\Language Voice Tutor`
- Start Menu shortcut: `Language Voice Tutor`
- Optional Desktop shortcut: `Language Voice Tutor`
- Expected artifact: `artifacts\installers\windows\LanguageVoiceTutorSetup-{version}.exe`

The existing executable remains `EnglishVoiceTutor.Desktop.exe` to avoid risky project-wide renames.

## Future update policy

Future direct-download updates should reuse the same Inno Setup AppId, `LanguageVoiceTutor.Desktop`, so installing a newer installer updates the existing installation.

No automatic update UX is implemented yet. Future in-app update UX should download the same Inno installer and run it only after explicit user confirmation. It must not run during an active lesson and must not perform silent updates.

## Uninstall behavior

The installer should use standard Windows uninstall integration. Uninstall removes installed app files and shortcuts. It should not delete local user settings/session/cache by default and cannot delete backend account data.

## Deferred items

- Code signing is deferred, but required before broad public distribution.
- Microsoft Store/MSIX is deferred.
- Public release readiness is not claimed.
