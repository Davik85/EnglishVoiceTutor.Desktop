# Tester release workflow

Review date: 2026-06-11.

## Current approval status

`0.1.26-tester.1` is the current validated public/tester Windows direct release. The Windows direct release was built and validated. The public tester download page reads `/releases/windows/direct/latest.json`, and the current `latest.json` points to `LanguageVoiceTutorSetup-0.1.26-tester.1.exe` with `backendBaseUrl` set to `https://api.languagevoicetutor.com`.

This approves the current direct Windows package as the private tester build. It does not mean the product is fully public production-ready.

## Backend URL profile

Production tester/release installed builds are server-only and always use `https://api.languagevoicetutor.com`. The installed app must not use localhost, 127.0.0.1, local-network URLs, plain HTTP development URLs, empty URLs, stale AppData overrides, or any user-editable backend URL.

Release Settings must not show a Diagnostics tab. Release Settings must not show a Backend URL field. Localhost/dev backend usage is DEBUG/developer-only and must not be described as normal tester/release behavior. Testers do not need to run a local backend.

## Current verified tester behavior

The current tester build verifies:

- registration and login from installed builds against `https://api.languagevoicetutor.com`;
- registration on another device;
- trial grant after registration;
- lesson start;
- TTS/bot voice;
- Conversation Mode;
- Lesson History saving;
- Progress;
- auth session persistence after app restart and Windows restart;
- hardened update/reinstall preservation for known auth session paths;
- Welcome/start screen clamped to the visible working area;
- Welcome primary actions visible without scrolling on smaller screens;
- Welcome cover image using cover-style fill/crop without gray bars;
- public download page and manifest pointing to the correct `0.1.26-tester.1` installer.

Raw passwords are not stored. Auth/session data is protected under the current user's app-data area, and logout clears persisted auth session data.

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

## CMS/Admin and content runtime

CMS/Admin is connected. The `static-json-v1` CMS content pack is initialized as Draft/admin content. Learner runtime still uses packaged static JSON by default.

Do not claim CMS runtime publishing is production-live for learners. CMS published-snapshot runtime reads remain disabled/not the learner default unless explicitly enabled and validated later.

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
- Update-over-existing-install preserves auth session, settings, history, and progress.
- **Check for updates** asks before download/install, verifies SHA-256, and never silently auto-updates.
- Smaller laptop/scaled display opens with title bar and primary Welcome actions visible, without gray cover bars.
- Generated files under `artifacts/` are not committed.
- No passwords, API keys, tokens, private keys, provider credentials, private environment values, or personal credentials are committed or included in release docs.
- Code signing remains deferred and expected SmartScreen warnings are documented for controlled testers.

## Remaining readiness items

The following are still realistic follow-ups, not solved by the `0.1.26-tester.1` validation alone:

1. Complete a clean-machine smoke pass and record results.
2. Complete update-over-existing-install validation and record results.
3. Hand off to a small controlled external tester group.
4. Run the tester feedback collection and triage process.
5. Optionally validate CMS runtime read/publish later before making it the learner default.
6. Complete production billing/Paddle/subscription payment lifecycle later.
7. Add code signing later before broad distribution.
