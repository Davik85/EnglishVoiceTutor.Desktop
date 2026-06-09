# Tester release workflow

## Current blocker status as of 2026-06-09

Desktop authenticated session persistence is now part of the tester-readiness foundation. The desktop does not store raw passwords; token/session data is stored under the current user app-data folder with Windows DPAPI protection. Logout clears persisted auth session data. Reinstall/update should preserve user app data and session storage. Same-version installer reinstall confirmation remains in place. The basic manual in-app update UI now checks `latest.json`, validates the manifest, verifies SHA-256 before offering to open the installer/folder, and does not silently auto-update. External tester handoff remains blocked until persisted-session verification and clean-machine smoke pass.

External tester handoff is still blocked. The backend is deployed at `https://api.languagevoicetutor.com`, PostgreSQL is healthy, the static tester download page is deployed at `https://languagevoicetutor.com`, and password reset/change flows are working. The remaining blockers are production/server CMS/Admin verification and clean-machine update/version-check verification. During CMS/Admin verification, static JSON remains the default runtime source unless the published snapshot flag is intentionally enabled. Public release is not ready. The basic manual update UI/system is implemented; the Windows installer has installed-version checks, and both still need package/clean-machine verification before tester handoff. The next Windows installer package should use `0.1.17-tester.1` unless release conventions intentionally select a different SemVer-compatible tester version.

## Current approval status

Version `0.1.17-tester.1` passed the basic internal end-to-end smoke test against the real production-like server setup. This confirms internal smoke readiness only. Tester handoff is **not approved yet**, and Language Voice Tutor is not publicly released.

Confirmed in the internal smoke test: app start, registration, trial entitlement, lesson start, normal lesson chat, Conversation Mode, TTS in normal chat and Conversation Mode, translation, feedback, hints, lesson history saving, active/restored session behavior after closing and reopening, backend reachability at `https://api.languagevoicetutor.com`, healthy backend health endpoint, healthy database health endpoint, applied PostgreSQL migrations, working static Windows direct release hosting, available production-domain `latest.json` for `0.1.17-tester.1`, and generated/validated/uploaded Windows installer release files verified on the server.

## Tester handoff blockers

External tester handoff remains blocked by:

1. CMS/Admin content flow connected and verified on the server.
2. Basic update system / update UI plus installed-version check verification so testers can update from inside the app or through a clear guided flow.
3. Clean-machine tester release smoke and controlled tester checklist completion.

Password reset/change flows and the static tester download page are working, but public release is still not ready.

## Minimum tester handoff checklist

Before the first controlled external tester receives Language Voice Tutor, confirm all items below:

- Password recovery / password reset works end to end.
- Signed-in password change works end to end.
- CMS/Admin content flow is connected and verified on the server.
- Public tester download page exists and points to the correct Windows installer.
- Basic update system / update UI exists and gives testers a clear manual update path.
- Installed-version checks handle same-version reinstall confirmation, older-version update, newer-version warning/block or explicit confirmation, and never auto-update during an active lesson.
- Clean-machine tester release smoke passes for install, launch, registration/login/session restore, trial entitlement, lesson start, normal chat, Conversation Mode, TTS, translation, feedback, hints, lesson history, active/restored session behavior, update guidance, and uninstall/upgrade expectations.
- Generated files under `artifacts/` are not committed.
- No passwords, API keys, tokens, private keys, provider credentials, private environment values, or personal credentials are committed or included in release docs.
- Code signing remains deferred and expected SmartScreen warnings are documented for controlled testers.


## Backend URL profile

Production tester builds must use `https://api.languagevoicetutor.com`. The installed app must show/use that effective Backend URL and must not silently fall back to localhost, 127.0.0.1, an empty URL, or a stale local development override. Clean-machine smoke must verify health, registration, login, settings sync, lesson start, history, progress, password reset, and update check from a real installed build. Tester handoff is blocked until the installed-build backend connectivity issue is verified fixed on a second Windows device.

Local development builds default to `http://localhost:5000`. The primary Inno tester/release installer flow defaults packaged builds to `https://api.languagevoicetutor.com` by passing `DesktopBackendBaseUrl` during publish; use `-BackendBaseUrl` only when intentionally testing another absolute http/https backend. Settings/Diagnostics continue to show the current Backend URL so tester reports can confirm the profile in use.

Existing installed-user settings are handled conservatively: empty Backend URL values use the current build default, saved legacy `http://localhost:5000` values migrate to `https://api.languagevoicetutor.com` in tester/release builds where that is the build default, and unsafe localhost, loopback, or plain-http development overrides are ignored instead of silently replacing production.

The backend remains the source of truth. The desktop must not store OpenAI API keys and must not call OpenAI directly. Production billing remains deferred, public release is not approved, and external tester handoff remains blocked until server-connected CMS/Admin verification and manual update UI verification, clean-machine install, and the controlled tester checklist pass.

The recommended Windows tester handoff is now the Inno Setup installer documented in [`docs/WINDOWS_INSTALLER_RELEASE_FLOW.md`](WINDOWS_INSTALLER_RELEASE_FLOW.md), after the installer smoke checklist passes.

The older ZIP package created by `scripts/package-tester-release.ps1` remains available only as an emergency/developer fallback. Do not present the ZIP as the main tester handoff when the Inno installer is available and smoke-tested.

Velopack is deprecated/rejected for this project. Its Windows installer is a one-click flow and does not match the desired release-like installer UX with destination-directory selection. External testers should not receive Velopack packages.

The server-ready direct-download folder can now be validated locally and dry-run uploaded with the manual helper documented in [`docs/WINDOWS_RELEASE_SERVER_UPLOAD.md`](WINDOWS_RELEASE_SERVER_UPLOAD.md). Server upload is prepared but not executed automatically, and it does not deploy the backend, create the download website, or add automatic update service. External tester handoff remains blocked until server-connected CMS/Admin verification, manual update UI verification, clean-machine install, and the controlled tester checklist pass.


## Manual update check UI

The desktop app now has a simple user-facing **Check for updates** button in normal Settings. It fetches `https://languagevoicetutor.com/releases/windows/direct/latest.json`, validates that the manifest belongs to Language Voice Tutor Desktop for Windows x64, and compares tester versions with the same prerelease intent as the installer version policy.

If an update is available, the app asks before downloading and installing. The app saves the installer under the current user's LocalAppData updates cache, verifies SHA-256 against `installerSha256`, and only then asks whether to start the installer. Failed hash verification deletes the downloaded file and shows a friendly error. The app does not run installers silently and does not perform automatic background updates. Testers should finish any active lesson before starting an installer; deeper active-lesson state integration from Settings remains a follow-up.

External tester handoff remains blocked until the clean-machine smoke checklist passes.

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
- an automatic updater or background update service; the app has a basic manual Settings update check UI, but it does not auto-update;
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

`latest.json` for `0.1.17-tester.1` is available from the production domain and the server-side release files were verified. The manifest is still for a future download page and future in-app update-check only. The current app does not check this manifest automatically. Future update UI must require manual confirmation and must not run during an active lesson.

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

Tester handoff is not ready yet. Remaining blockers are CMS/Admin server verification, manual update UI verification and clean-machine smoke, and final checklist completion.

## Account password flow note (2026-06-08)

The Account screen password reset/change flow is being polished for tester readiness. Forgot password and Change password panels are collapsed by default, sensitive fields are cleared on close/success, and validation/auth failures should show clear messages rather than a generic server-unavailable warning.

SMTP credentials for password reset email delivery remain server-only in `/etc/languagevoicetutor/backend.env`; no secrets are committed. Backend deployment packaging/upload now avoids Windows backslash ZIP entries for the Linux backend package and verifies the deployed backend executable bit before reporting success.

External tester handoff is still blocked until CMS/Admin server verification, manual update UI verification and clean-machine smoke, and checklist completion.

## Static tester download page foundation

A basic public download page foundation is now prepared under `site/public/`. The page is static and reads `latest.json` from the existing Windows direct release folder at `/releases/windows/direct/latest.json`. It uses the manifest `installerRelativeUrl` value for the primary **Download for Windows** button, displays the manifest `version`, and shows the manifest `installerFileName` in release details when manifest loading succeeds.

This page does not implement auto-update and complements the in-app manual update UI. It is only a tester download page for invited testers. If the manifest cannot be loaded or is invalid, the page must keep the download button disabled instead of using a hardcoded installer fallback. The page must not hardcode old installer filenames, and after every Windows release upload the displayed version and actual downloaded filename must both be verified. It must not include login, payment, pricing, account management, analytics, cookies, third-party fonts, external dependencies, or marketing claims.

External tester handoff is still blocked until the manual update UI verification and the clean-machine smoke checklist pass. Treat the page as a handoff foundation only, not as final tester-readiness approval.

## CMS/Admin initialization gate for tester release

External tester handoff is still blocked. CMS/Admin login works for the bootstrap admin path, but first production setup may require initializing `static-json-v1` inside CMS. Use the admin-only **Initialize from static JSON** action in CMS Content Overview, then verify the content pack summary and draft lists.

This action only prepares CMS draft/admin content from packaged static JSON. It does not publish automatically and does not switch runtime. Learner runtime remains static JSON while `CmsContent__UsePublishedSnapshotForRuntime=false`; only an intentional later change to `CmsContent__UsePublishedSnapshotForRuntime=true` can move runtime to a published CMS snapshot.


## Windows installer installed-version behavior

Installed-version checking is now part of the Windows installer foundation. This is an installer guard only, not automatic updating and not the future in-app update UI.

- Same-version install asks for reinstall confirmation and cancels if the user declines.
- Older installed version is treated as an update and may continue after the installer explains that it will update Language Voice Tutor.
- Newer installed version warns and blocks by default so testers do not accidentally downgrade to an older installer.
- Running app replacement is guarded by Inno Setup close-application handling for the desktop executable.

The future in-app update UI still needs to check `latest.json`, verify SHA-256 before running an installer, avoid updates during active lessons, and guide the user through download/install. Active-lesson detection is intentionally left to that future in-app UI because the standalone installer only knows whether the desktop executable is running, not whether a lesson is active.

External tester handoff is still blocked until update/version-check verification and clean-machine smoke pass.


## Current desktop update check

A simple user-facing **Check for updates** button is available near the top of Settings. It is not Diagnostics-only and it does not expose a technical update dashboard. The check fetches the Windows direct-release `latest.json`, validates product name, app id, platform, and architecture, compares the installed version with the manifest version, and shows simple dialogs for up-to-date, update-available, newer-than-manifest, and connectivity/error cases.

If an update is available, the app asks before downloading. After download, it verifies SHA-256 before offering to start the installer. The installer is launched normally only after user confirmation; no silent install switches are used and there is no background auto-update. Progress and Lesson History are account-scoped local data and must remain preserved across app restart, Windows restart, reinstall, and update. Clean-machine smoke is still required before external tester handoff.
