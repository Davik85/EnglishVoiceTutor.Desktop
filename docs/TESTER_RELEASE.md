# Historical tester release workflow

Review date: 2026-08-24.

## Source of truth for current versions

These docs are a snapshot of the last known verified state. They can become stale and must not be used as the only source of truth for live versions. Always verify the live/public state before telling a tester that a version is current.

Check the public Windows direct release from the live website manifest:

```powershell
Invoke-RestMethod https://languagevoicetutor.com/releases/windows/direct/latest.json
```

If a PowerShell path reads raw manifest text and `ConvertFrom-Json` fails because a UTF-8 BOM is present at the start of `latest.json`, strip the BOM before parsing:

```powershell
($raw -replace "^\uFEFF", "") | ConvertFrom-Json
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

## Historical tester status and current direct release

The public Windows direct manifest baseline must be checked from the live website `latest.json`. Last verified public snapshot: `latest.json` pointed to `LanguageVoiceTutorSetup-1.4.exe` with `version` set to `1.4`, `backendBaseUrl` set to `https://api.languagevoicetutor.com`, `minimumSupportedVersion` set to `1.4`, and `updateMode` set to `manual-confirmation`.

Windows Direct Release 1.4 is now published on the public direct channel. Public download, in-app manual-confirmation update, installed application launch, and the downloaded installer SHA-256 were verified; no backend deployment or migration was part of this Windows upload. Historical tester-release notes in this document are retained only as history; they are not the current active release state. This does not mean every operational area is fully public production-ready.

## Release artifact boundary

`latest.json`, `changelog.json`, `known-issues.json`, `checksums.sha256`, installers, packages, and other files under `artifacts/` are generated outputs and must not be committed. Public direct Windows release files are uploaded to `/var/www/languagevoicetutor/releases/windows/direct`; the public website root is separate at `/var/www/languagevoicetutor/site`. Backend deploy and Windows installer upload are separate flows.

## Historical backend notes

Historical tester-era backend snapshot notes are retained only for context. Current production backend for the Windows Direct Release 1.4 documentation state is `0.1.35-backend.140`, with `.139` rollback; verify the live value from the server symlink before calling it current.

`https://api.languagevoicetutor.com/health` and `https://api.languagevoicetutor.com/api/health/database` return `200 OK`. Operator manual smoke should continue to verify app launch, login, Account opening, lesson start, at least 7 Daily Life / Introductions or guided roleplay user messages without a generic server error, Lesson History updates, and Progress updates.

## Backend URL profile

Production tester/release installed builds are server-only and always use `https://api.languagevoicetutor.com`. The installed app must not use localhost, 127.0.0.1, local-network URLs, plain HTTP development URLs, empty URLs, stale AppData overrides, or any user-editable backend URL.

Release Settings must not show a Diagnostics tab. Release Settings must not show a Backend URL field. Localhost/dev backend usage is DEBUG/developer-only and must not be described as normal tester/release behavior. Testers do not need to run a local backend.

## Historical verified tester behavior

The historical tester build verified these behaviors; these are historical tester observations, while the current active release is Windows Direct Release 1.4:

- registration and login from installed builds against `https://api.languagevoicetutor.com`;
- registration on another device;
- trial grant after registration;
- lesson start;
- TTS/bot voice;
- Conversation Mode;
- Lesson History saving;
- Progress;
- auth session persistence after app restart and Windows restart;
- update from older builds migrates preserved auth/session data from legacy `EnglishVoiceTutor.Desktop` local-data paths;
- Welcome/start screen clamped to the visible working area;
- Welcome primary actions visible without scrolling on smaller screens;
- Welcome cover image using cover-style fill/crop without gray bars;
- public download page and live `latest.json` verified over HTTPS before naming the public installer;
- Installed file names were renamed to `LanguageVoiceTutor.Desktop.*`; legacy `EnglishVoiceTutor.Desktop.*` install-folder files are cleaned during update/reinstall without deleting user AppData.

Raw passwords are not stored. Auth/session data is protected under the current user's app-data area, and Logout clears persisted auth session data. Desktop stores auth session safely in the current user's app-data storage, protected by Windows user-scoped data protection rather than raw password storage, and the desktop does not store raw passwords. A successful token refresh persists the replacement access token and refresh token so the next app restart uses the refreshed session. The update/reinstall should preserve auth session policy is intentional: update/reinstall should preserve auth session, settings, Lesson History, and Progress rather than deleting app-data session storage. The app should not log out the user just because the access token expired when the refresh token is still valid; authenticated desktop clients should use the central refresh-aware request flow so stale access tokens are refreshed and retried consistently.

Auth session persistence works across app restart and Windows restart. Regression coverage now includes the stale access token + valid refresh token path, refresh retry behavior, and persisted refreshed session state so a refreshed replacement access token and refresh token survive subsequent restore.

## Historical tester smoke summary

This historical tester smoke record predates Windows Direct 1.2 and the later backend `.134`: clean-machine smoke passed, including public download install, app launch, registration/login, lesson start, and Lesson History/Progress updates. Its then-current backend `0.1.35-backend.24` was healthy at that time; it is not the current production backend.

## Manual update flow

Release Settings expose a single user-facing **Check for updates** button. The old technical update dashboard is not part of the release UX.

The update flow is manual-confirmation only:

1. App checks `latest.json` only when the user chooses **Check for updates**.
2. App validates manifest identity.
3. App compares versions.
4. App asks before download/install.
5. App verifies SHA-256 before launching the installer.
6. App launches the installer only after user confirmation.

The app does not silently auto-update.

Downloaded update installers from **Check for updates** are saved in the current user's local update cache: `%LOCALAPPDATA%\LanguageVoiceTutor\Updates\LanguageVoiceTutorSetup-{version}.exe`. In-progress downloads use `.exe.download`. Failed or invalid in-progress downloads are deleted by the app, but older verified installer EXEs are retained until replaced by the same filename or manually removed. Cleanup command: `Remove-Item "$env:LOCALAPPDATA\LanguageVoiceTutor\Updates\LanguageVoiceTutorSetup-*.exe*" -Force -ErrorAction SilentlyContinue`.

## CMS/Admin and content runtime

CMS/Admin is connected. CMS published-snapshot runtime is active for published Windows direct lessons. **Save draft** alone does not affect the desktop app; **Save draft** plus **Publish** is required, and newly started desktop lessons pick up published CMS changes. The admin-only **Initialize from static JSON** action initializes `static-json-v1` CMS draft/admin content from packaged static JSON for first setup/recovery, does not publish automatically, and does not switch runtime. Runtime should use CMS published snapshot when `CmsContent__UsePublishedSnapshotForRuntime=true` and a valid published snapshot is active. Static JSON fallback remains available as initialization/emergency fallback; static JSON should not be the normal active learner source. Normal runtime status should remain `effectiveSource=CmsPublishedSnapshot`, `validationSuccess=true`, `fallbackUsed=false`, with no errors and no warnings. Production critical-change approval remains future work. Public release remains blocked until controlled tester feedback and operational readiness are complete.

## Windows installer installed-version behavior

Installed-version checking is now part of the Windows installer foundation. Same-version install asks for reinstall confirmation. Older installed version is treated as an update. Newer installed version warns and blocks by default so testers do not accidentally downgrade.

## Minimum controlled external tester checklist

Before or during small-group tester handoff, confirm all items below:

- Clean-machine install from the public download page succeeds.
- Downloaded installer filename matches `latest.json`.
- Installed build uses only `https://api.languagevoicetutor.com`.
- Release Settings expose no Diagnostics tab and no Backend URL field.
- Registration/login/session restore work.
- Trial entitlement is granted after registration.
- Lesson start, normal chat, TTS/bot voice, Conversation Mode, Lesson History, and Progress work.
- Auth session persists across app restart and Windows restart.
- Update/reinstall must preserve login, settings, Lesson History, and Progress, including migration from legacy `EnglishVoiceTutor.Desktop` local-data paths and cleanup of legacy installed `EnglishVoiceTutor.Desktop.*` files from the install folder.
- **Check for updates** asks before download/install, verifies SHA-256, and never silently auto-updates.
- Smaller laptop/scaled display opens with title bar and primary Welcome actions visible, without gray cover bars.
- Generated files under `artifacts/` are not committed.
- No passwords, API keys, tokens, private keys, provider credentials, private environment values, or personal credentials are committed or included in release docs.
- Code signing remains deferred and expected SmartScreen warnings are documented for controlled testers.

## Remaining readiness items

The following are still realistic follow-ups, not solved by the current private tester manifest and backend validation alone:

1. Complete update-over-existing-install validation and record results.
2. Hand off to a small controlled external tester group.
3. Run the tester feedback collection and triage process.
4. Triage known non-blocking feedback: touch drag/hold can visually select multiple topic/subtopic items even though navigation enters one item, some scenario/avatar dialogue can restart or repeat, short scenarios such as "Asking someone to repeat" need prompt/content polishing, bot voice autoplay can sometimes not play even when enabled, and occasional server-error feedback should remain in triage unless reproduced consistently.
5. Complete production billing/Paddle/subscription payment lifecycle later.
6. Add code signing later before broad distribution.
