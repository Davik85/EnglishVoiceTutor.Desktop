# Next Steps

Review date: 2026-06-12.

## Current release baseline

The current public tester manifest baseline is `0.1.28-tester.1`. The public tester download page reads `latest.json`, and `latest.json` points to `LanguageVoiceTutorSetup-0.1.28-tester.1.exe` with `backendBaseUrl` set to `https://api.languagevoicetutor.com`, `minimumSupportedVersion` set to `0.1.28-tester.1`, and `updateMode` set to `manual-confirmation`.

This is still a private tester/direct Windows release, not broad public production readiness.

## Current backend verification

Production backend release `/opt/languagevoicetutor/backend/releases/0.1.35-backend.2` is active via `/opt/languagevoicetutor/backend/current`. The refresh-token migration `20260611000000_AddUserRefreshTokens` is applied; `user_refresh_tokens` ownership/permissions were corrected for the application DB user after the migration was initially applied as `postgres`. `https://api.languagevoicetutor.com/health` and `https://api.languagevoicetutor.com/api/health/database` return `200 OK`, and operator smoke verified app launch, login, Account opening, lesson start, Lesson History updates, and Progress updates.

## Immediate tester-readiness work

1. Run a clean-machine smoke test from the public download page and current `latest.json`.
2. Validate update-over-existing-install from a prior `EnglishVoiceTutor.Desktop.*` installed tester build and confirm preserved auth/session data migrates to the current `LanguageVoiceTutor.Desktop` local-data path while login, user settings, Lesson History, and Progress survive update/reinstall.
3. Confirm auth session restore across app restart and Windows restart.
4. Confirm smaller-screen/scaled-display layout on at least one 1366x768, 1280x720, or equivalent scaled-display environment.
5. Confirm Release Settings have only the simple **Check for updates** action and do not expose Diagnostics or Backend URL editing.
6. Prepare the small external tester handoff group and feedback collection process.

## Release backend lock (server-only installed builds)

Release/tester installed builds are server-only. The only backend for packaged non-Debug Windows builds is `https://api.languagevoicetutor.com`. Local backend URLs are DEBUG/developer-only and must not be present as normal user Settings options. Diagnostics and Backend URL editing are not part of user/release Settings. Stale AppData `settings.json` backend URL values from older installs are ignored by release builds and are not written back into user-editable settings.

Clean-machine smoke must verify registration/login/lesson/history/progress/update from an installed build against the fixed production backend. The installed build connectivity signal is `GET https://api.languagevoicetutor.com/health`; registration calls `POST https://api.languagevoicetutor.com/api/auth/register`, login calls `POST https://api.languagevoicetutor.com/api/auth/login`, and auth restore calls `GET https://api.languagevoicetutor.com/api/auth/me`. Optional cloud settings or subscription/status endpoint failures must not block auth or lessons and must not be treated as the backend connectivity signal.

## Smoke checklist additions

Clean-machine smoke must verify:

- public page downloads the installer named by `latest.json`;
- registration/login work against `https://api.languagevoicetutor.com`;
- trial is granted after registration;
- lesson start, bot voice/TTS, Conversation Mode, Lesson History, and Progress work;
- auth persists after app restart and Windows restart;
- update/reinstall preserves login, settings, Lesson History, and Progress after migrating preserved auth/session data from legacy `EnglishVoiceTutor.Desktop` local-data paths;
- raw passwords are not stored;
- Welcome/start window clamps to the visible working area;
- Welcome primary actions are visible without scrolling on smaller laptop screens;
- Welcome cover image uses cover-style fill/crop with no gray bars;
- Release Settings do not show Diagnostics or Backend URL editing;
- **Check for updates** asks before download/install, verifies SHA-256, and does not silently auto-update.

## CMS/Admin follow-up

CMS/Admin is connected and `static-json-v1` is initialized as Draft/admin content. Learner runtime still uses static JSON by default. Do not enable CMS published-snapshot runtime reads for learners until the runtime read/publish path is explicitly enabled, validated, and documented later.

## Deferred work

- Production billing/Paddle/subscription payment lifecycle remains deferred.
- Production CMS RBAC and critical-change approval remain deferred.
- Code signing remains deferred.
- Broader public release readiness remains deferred until after controlled tester feedback and operational hardening.
