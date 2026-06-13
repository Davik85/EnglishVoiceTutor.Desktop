# Next Steps

Review date: 2026-06-13.

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

## Current release baseline

The live public tester manifest baseline must be checked from `latest.json`, not from this document. Last verified public snapshot: `latest.json` pointed to `LanguageVoiceTutorSetup-0.1.36-tester.8.exe` with `backendBaseUrl` set to `https://api.languagevoicetutor.com`, `minimumSupportedVersion` set to `0.1.36-tester.8`, and `updateMode` set to `manual-confirmation`. `0.1.36-tester.8` is the current uploaded Windows tester build in the public direct Windows release folder; verify the website `latest.json` over HTTPS before tester handoff.

This is still a private tester/direct Windows release, not broad public production readiness.

## Current backend verification

Current state: last known production backend snapshot is `/opt/languagevoicetutor/backend/releases/0.1.35-backend.11` active via `/opt/languagevoicetutor/backend/current`; verify the live value from the server symlink before calling it current. Previous backend release for rollback reference: `/opt/languagevoicetutor/backend/releases/0.1.35-backend.8`. Backend `0.1.35-backend.11` contains the latest Admin CMS Validation & Preview readable UI fix plus `/admin` static asset cache busting/no-cache behavior. `https://api.languagevoicetutor.com/health` and `https://api.languagevoicetutor.com/api/health/database` return `200 OK`. No EF migration was required.

Deployed runtime status diagnostics are visible on backend `0.1.35-backend.11` from the server `/admin` page and protected runtime-status endpoint. The current server diagnostic is clean and confirms learner runtime uses CMS published snapshot: `effectiveSource=CmsPublishedSnapshot`, `validationSuccess=true`, `fallbackUsed=false`, no errors, no warnings, and `tutorBehaviorProfiles=3`. The tutor behavior profile mismatch was fixed by validating the approved tutor ids `david`, `elena`, and `nelli` instead of an obsolete exact count of 2. The next step is controlled tester handoff and feedback collection.

## CMS connection readiness and controlled release preparation

Current state: CMS practical readiness has passed the runtime connection milestone. CMS published snapshot is now the active runtime content source for controlled tester lessons. Do not start broad public release from this state.

### A. Verify deployed Admin CMS manually

1. Login to `/admin`.
2. Open **CMS Content**.
3. Open `static-json-v1`.
4. Run **Validation**.
5. Load **Preview** summary.
6. Confirm the Validation and Preview results are readable.
7. Confirm raw JSON appears only inside collapsed details blocks.

### B. Prepare full CMS content workflow

1. Initialize or verify `static-json-v1` draft content.
2. Validate the draft.
3. Preview sample topics and scenarios.
4. Save a safe draft edit.
5. Confirm the audit entry.
6. Publish with a clear change summary.
7. Restore the previous published version.
8. Confirm old versions are immutable.

### C. Runtime milestone status

1. Confirm runtime status remains `effectiveSource=CmsPublishedSnapshot`, `validationSuccess=true`, `fallbackUsed=false`, with no errors and no warnings.
2. Keep fallback to static JSON available for rollback, but treat any normal-runtime fallback as a condition to investigate.
3. Remember that **Save draft** alone does not affect the app; **Publish** is required. Existing active lessons may keep old content until a new lesson starts.
4. Keep broad public production release and production billing deferred.

## Immediate tester-readiness work

1. Verify the installed tester build from the public site and current `latest.json`.
2. Validate update-over-existing-install from a prior `EnglishVoiceTutor.Desktop.*` installed tester build and confirm old installed `EnglishVoiceTutor.Desktop.*` files are cleaned from the install folder, preserved auth/session data migrates to the current `LanguageVoiceTutor.Desktop` local-data path, and login, user settings, Lesson History, and Progress survive update/reinstall.
3. Confirm auth session restore across app restart and Windows restart.
4. Confirm smaller-screen/scaled-display layout on at least one 1366x768, 1280x720, or equivalent scaled-display environment.
5. Confirm Release Settings have only the simple **Check for updates** action and do not expose Diagnostics or Backend URL editing.
6. Perform a short smoke test: launch, login/register, start a new lesson, confirm CMS-controlled scenario content is visible after publish, compare A1 and B2 behavior, verify voice/TTS, Lesson History, and Progress.
7. Prepare the small external tester handoff group and instructions.
8. Collect feedback on lesson quality, A1/A2/B1/B2 level behavior, voice, UI, and CMS-controlled content.

## Release backend lock (server-only installed builds)

Release/tester installed builds are server-only. The only backend for packaged non-Debug Windows builds is `https://api.languagevoicetutor.com`. Local backend URLs are DEBUG/developer-only and must not be present as normal user Settings options. Diagnostics and Backend URL editing are not part of user/release Settings. Stale AppData `settings.json` backend URL values from older installs are ignored by release builds and are not written back into user-editable settings.

Clean-machine smoke must verify registration/login/lesson/history/progress/update from an installed build against the fixed production backend. The installed build connectivity signal is `GET https://api.languagevoicetutor.com/health`; registration calls `POST https://api.languagevoicetutor.com/api/auth/register`, login calls `POST https://api.languagevoicetutor.com/api/auth/login`, and auth restore calls `GET https://api.languagevoicetutor.com/api/auth/me`. Optional cloud settings or subscription/status endpoint failures must not block auth or lessons and must not be treated as the backend connectivity signal.

## Smoke checklist additions

Clean-machine smoke must verify:

- public page downloads the installer named by `latest.json`;
- registration/login work against `https://api.languagevoicetutor.com`;
- trial is granted after registration;
- lesson start, bot voice/TTS, Conversation Mode, Lesson History, and Progress work;
- Daily Life / Introductions or another guided roleplay allows at least 7 user messages without showing a generic server error;
- auth persists after app restart and Windows restart;
- update/reinstall preserves login, settings, Lesson History, and Progress after migrating preserved auth/session data from legacy `EnglishVoiceTutor.Desktop` local-data paths;
- raw passwords are not stored;
- Welcome/start window clamps to the visible working area;
- Welcome primary actions are visible without scrolling on smaller laptop screens;
- Welcome cover image uses cover-style fill/crop with no gray bars;
- Release Settings do not show Diagnostics or Backend URL editing;
- **Check for updates** asks before download/install, verifies SHA-256, and does not silently auto-update.

## CMS/Admin follow-up

Next safe step: controlled tester handoff and feedback collection. CMS published snapshot runtime is active; verify Save draft + Publish changes in the desktop app, keep static JSON fallback available, and investigate if normal runtime status shows fallback active.

## Deferred work

- Production billing/Paddle/subscription payment lifecycle remains deferred.
- Production CMS RBAC and critical-change approval remain deferred.
- Code signing remains deferred.
- Broader public release readiness remains deferred until after controlled tester feedback and operational hardening.

## CMS runtime status validation path

The Admin CMS now exposes a read-only **Runtime content status** section and the protected endpoint `GET /api/admin/dev/cms/runtime-status`. Use it to confirm the effective learner content source, validation result, counts, published snapshot metadata, and fallback state without exposing content bodies or secrets.

CMS published snapshot is the active runtime source. The diagnostic confirms runtime source and fallback state. Runtime status is clean on backend `0.1.35-backend.11` with approved tutor-id validation for `david`, `elena`, and `nelli`. Normal status should show `effectiveSource=CmsPublishedSnapshot`, `validationSuccess=true`, `fallbackUsed=false`, no errors, and no warnings. Rollback remains disabling CMS runtime flags and restarting backend so runtime returns to static JSON. Billing/Paddle is not involved.

## CMS-managed level profiles (A1-B2)

- CMS now manages A1, A2, B1, and B2 level behavior profiles through the CMS Content **Levels** tab.
- Level profiles include stable level keys, display names, active flags, sort order, wrap-up turn, final-message turn, language complexity guidance, correction guidance, answer-length guidance, and admin notes.
- Lesson length defaults come from the selected level profile: A1 is configured for a shorter lesson around 15 learner turns, while B2 supports a longer dialogue.
- Scenario-specific lesson length values remain optional overrides when explicitly set and valid. Priority is: scenario override, then CMS level profile, then safe backend constants.
- Backend runtime content remains the source of truth for lesson behavior. Desktop may keep its current level labels for display, but desktop and future mobile should use backend runtime behavior from the CMS published snapshot.
- Static JSON fallback remains available; fallback runtime also receives safe default level profiles.
