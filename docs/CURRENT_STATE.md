# Current State

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

## Current tester Windows direct release

The public website `latest.json` remains the public source of truth for the live Windows direct tester release. Last verified snapshot: it pointed to `LanguageVoiceTutorSetup-0.1.35-tester.1.exe`, set `version` and `minimumSupportedVersion` to `0.1.35-tester.1`, set `backendBaseUrl` to `https://api.languagevoicetutor.com`, and used `updateMode: manual-confirmation`. This matches the installed tester/release backend lock.

Local build `0.1.36-tester.2` has been built and validated locally, but it must not be described as public/live unless the live website `latest.json` points to it over HTTPS. This remains a private tester/direct Windows release, not a broad public production launch.

## Release backend lock (server-only installed builds)

Release/tester installed builds are server-only. The only backend for packaged non-Debug Windows builds is `https://api.languagevoicetutor.com`. Local backend URLs are DEBUG/developer-only and must not be present as normal user Settings options. Diagnostics and Backend URL editing are not part of user/release Settings. Stale AppData `settings.json` backend URL values from older installs are ignored by release builds and are not written back into user-editable settings.

Clean-machine smoke must verify registration/login/lesson/history/progress/update from an installed build against the fixed production backend. The installed build connectivity signal is `GET https://api.languagevoicetutor.com/health`; registration calls `POST https://api.languagevoicetutor.com/api/auth/register`, login calls `POST https://api.languagevoicetutor.com/api/auth/login`, and auth restore calls `GET https://api.languagevoicetutor.com/api/auth/me`. Optional cloud settings or subscription/status endpoint failures must not block auth or lessons and must not be treated as the backend connectivity signal.

## Current production backend state

Current state: last known production backend snapshot is `/opt/languagevoicetutor/backend/releases/0.1.35-backend.8`, and `/opt/languagevoicetutor/backend/current` points to that release. Verify the live value with the server symlink command before calling it current. Previous backend release for rollback reference: `/opt/languagevoicetutor/backend/releases/0.1.35-backend.7`.

Completed: backend `0.1.35-backend.8` is deployed and contains the latest Admin CMS Validation & Preview readable UI fix and the Admin static asset cache busting/no-cache fix for `/admin` assets. The Admin CMS Validation & Preview area no longer shows raw JSON directly in the main result area. Validation renders a readable panel with Passed/Failed status, counts, errors, warnings, and collapsed raw validation JSON. Preview renders readable metadata, counts, sample topics, sample scenarios, and collapsed raw preview JSON. Admin static asset cache busting was added for `admin.js` and `admin.css` using token `admin-cms-20260613-raw-json-fix`, and no-cache headers were added for `/admin` static files only.

Completed: health and database health are green after deploy. `https://api.languagevoicetutor.com/health` returns `200 OK`, and `https://api.languagevoicetutor.com/api/health/database` returns `200 OK`. The build is green, the Admin shell audit is green, and the EF model check reports no pending model changes. No EF migration was required. Operator manual smoke should continue to verify app launch, login, Account opening, lesson start, at least 7 Daily Life / Introductions or guided roleplay user messages without a generic server error, Lesson History updates, and Progress updates.

## Auth, account, and persistence

Registration and login now work from installed tester/release builds against `https://api.languagevoicetutor.com`, including the current backend permission-fixed login path. Trial assignment is granted after registration. The desktop stores the authenticated session under the current user's app-data area with Windows DPAPI protection and does not store raw passwords. Logout clears persisted auth session data.

Auth session persistence works across app restart and Windows restart. Installed file names were renamed to `LanguageVoiceTutor.Desktop.*`. Legacy installed `EnglishVoiceTutor.Desktop.*` application files are cleaned from the install folder during update/reinstall without deleting user AppData. Update from older builds must migrate preserved auth/session data from legacy `EnglishVoiceTutor.Desktop` local-data paths, and update/reinstall must preserve login, settings, Lesson History, and Progress. For policy tracking, update/reinstall should preserve auth session, user settings, Lesson History, and Progress.

## Lessons and learner runtime

The current tester build has verified lesson start, normal lesson chat, TTS/bot voice, Conversation Mode, Lesson History saving, and Progress. Learner runtime content still uses packaged static JSON by default.

Early manual **Finish lesson** clicks during an active lesson now ask for confirmation so accidental clicks do not end the session. Forced/final **Finish lesson** after the lesson limit remains one-click, and this is a desktop UI safety fix only. No backend deploy, no DB migration, and no Windows upload are required until an installer is intentionally packaged and published.

## CMS/Admin and runtime content

Current state: CMS/Admin is connected. The `static-json-v1` CMS content pack has been initialized as Draft/admin content. This initialization does not publish runtime content automatically and does not switch learner runtime.

Completed: the Admin CMS Validation & Preview UI regression is fixed and readable. Raw JSON diagnostics are available only inside collapsed details blocks. `Save draft` remains draft-only, and publishing remains isolated in **Versions & Publish**.

Do not enable by default: learners still use packaged static JSON by default. CMS published-snapshot runtime reads remain disabled/not the learner default unless `CmsContent__ReadPublishedSnapshotEnabled=true`, `CmsContent__UsePublishedSnapshotForRuntime=true`, and the related published-snapshot read path are explicitly enabled and validated later in a controlled or explicitly approved environment. Keep fallback to static JSON. Do not describe CMS runtime publishing as production-live for learners yet. Production RBAC and critical-change approval remain future work.

Next safe step: run controlled CMS published-snapshot runtime validation with an explicit, temporary, reversible, operator-approved flag plan. Use `tools/validate_cms_published_snapshot_runtime.ps1` first in read-only mode to confirm the current default remains `effectiveSource=StaticJson`, then use `-GenerateServerValidationPlan` to print the temporary server flags and rollback checklist. CMS runtime must not become the learner default until this controlled validation passes and a separate enablement decision is approved.

## Manual desktop update UI

A simple user-facing **Check for updates** button is available near the top of normal Settings. It is not Diagnostics-only and it does not expose the old technical update dashboard.

The manual-confirmation update flow is:

1. The user clicks **Check for updates**.
2. The app fetches `https://languagevoicetutor.com/releases/windows/direct/latest.json`.
3. The app validates the manifest identity: product name, app id, platform, and architecture.
4. The app compares the installed version with the manifest version using tester prerelease-aware version comparison.
5. If a newer version is available, the app asks before downloading.
6. The app verifies the downloaded installer SHA-256 against `installerSha256`.
7. The app asks before launching the installer.

There is no silent auto-update, no background update service, and no installer launch before SHA-256 verification.

Downloaded update installers from **Check for updates** are saved in the current user's local update cache: `%LOCALAPPDATA%\LanguageVoiceTutor\Updates\LanguageVoiceTutorSetup-{version}.exe`. In-progress downloads use `.exe.download`. Failed or invalid in-progress downloads are deleted by the app, but older verified installer EXEs are retained until replaced by the same filename or manually removed. Cleanup command: `Remove-Item "$env:LOCALAPPDATA\LanguageVoiceTutor\Updates\LanguageVoiceTutorSetup-*.exe*" -Force -ErrorAction SilentlyContinue`.

## Desktop startup window and Welcome layout

The desktop window clamps startup size and position to the visible working area before normal use, respecting the Windows taskbar and display scaling. The Welcome/start screen primary actions are visible on smaller laptop screens without scrolling. The cover image uses cover-style fill/crop behavior without gray letterbox bars.

Clean-machine smoke must include a smaller laptop / scaled display check, including at least one 1366x768, 1280x720, or scaled-display equivalent where the title bar, close button, dragging, Settings, Account, Learning, Progress, Lesson History, lesson start, and Conversation Mode are verified. Backend/auth/lessons remain unchanged by this window-placement fix.

## Billing and subscriptions

Billing/Paddle/subscription payment lifecycle remains deferred. Do not imply production billing is ready. Trial entitlement for registration is working, but production checkout, webhook operations, subscription lifecycle operations, billing support operations, and broad payment readiness remain later work.

## Release status

Current state: the project is preparing for controlled release / tester handoff. CMS readiness is now part of release preparation because practical content operations must work before broader handoff. Broad public production release is still not ready. Production billing remains deferred, and Paddle production readiness remains deferred.

Not ready yet: do not claim broad public production readiness, production billing readiness, production RBAC readiness, critical-change approval readiness, full Admin CMS production readiness, mobile readiness, or learner CMS runtime default readiness.

## External tester readiness

Solved release blockers for the current private tester baseline:

- the public download page and `latest.json` must be checked over HTTPS before naming the live installer;
- installed release builds no longer use localhost/local backend routing;
- backend connectivity from another device is fixed;
- registration, trial assignment, lesson start, bot voice, Conversation Mode, Lesson History, Progress, and auth restore are working;
- Release Settings do not expose Diagnostics or Backend URL editing;
- Welcome startup placement no longer opens off-screen;
- Welcome primary actions are not hidden below a scroll area on smaller screens;
- the Welcome cover no longer shows gray letterbox bars;
- the user-facing manual update check is implemented.

Remaining realistic readiness items:

1. Run a clean-machine smoke test of the current installer and public download flow.
2. Run update-over-existing-install validation from old `EnglishVoiceTutor.Desktop.*` executable builds and confirm preserved auth/session data migrates to the current `LanguageVoiceTutor.Desktop` local-data path without losing login, settings, Lesson History, or Progress.
3. Keep app restart/session restore and Windows restart/session restore in tester smoke.
4. Run smaller-screen/scaled-display smoke.
5. Hand off to a small controlled external tester group.
6. Establish the feedback collection and triage process.
7. Complete CMS practical readiness as a release gate: validate the deployed Admin CMS workflow, publish/restore safely, confirm audit traceability, and document controlled runtime-read flags and rollback before considering any learner runtime change.
8. Finish production billing/payment lifecycle later.
9. Add code signing later to reduce SmartScreen friction before broad distribution.

Do not state that the product is fully public production-ready. The current state is a validated private tester/direct Windows release.

- Lesson Chat UI polish: Finish confirmation typography improved, Start recording is green, Hint uses hint-color styling.

## CMS runtime status diagnostics

Completed: the backend has an admin-only, read-only CMS runtime content status diagnostic at `GET /api/admin/dev/cms/runtime-status` (legacy alias: `/api/admin/dev/cms/runtime-content/status`). It reports safe metadata only: source, content pack slug, CMS runtime flags, fallback state, published version/hash when applicable, content counts, validation state, and bounded errors/warnings. It does not return lesson bodies, scenario `DefinitionJson`, prompt bodies, tutor instruction bodies, secrets, tokens, API keys, connection strings, or auth headers.



Current runtime-status result on deployed backend `0.1.35-backend.8` is clean: `effectiveSource=StaticJson`, `validationSuccess=Yes`, no errors, no warnings, and `tutorBehaviorProfiles=3`. Learner runtime remains static JSON by default. The prior runtime-validation root cause was an obsolete hardcoded exact tutor behavior profile count of 2. Static JSON, CMS static import/draft construction, and desktop tutor avatar options all define the approved tutor ids `david`, `elena`, and `nelli`; the third profile is legitimate product content, not a smoke/test artifact. Runtime validation now derives the required tutor ids from the approved desktop avatar definitions and reports expected, actual, missing, unknown/extra, and duplicate tutor ids without exposing tutor instruction bodies. The next step is controlled CMS published-snapshot runtime validation, not default learner enablement. The `tools/smoke_cms_runtime_status.ps1` and `tools/validate_cms_published_snapshot_runtime.ps1` scripts default to the server-only backend `https://api.languagevoicetutor.com`; localhost must be passed explicitly only for approved local developer runs.

This diagnostic does not enable CMS runtime content. Static JSON remains the learner runtime default unless `CmsContent:UsePublishedSnapshotForRuntime=true`, `CmsContent:ReadPublishedSnapshotEnabled=true`, `CmsContent:ContentPackSlug=static-json-v1`, and `CmsContent:FallbackToStaticJson=true` are explicitly configured in a controlled environment. The next step is controlled environment validation with those explicit flags, followed by a separate decision on whether to enable CMS runtime for a limited learner/tester group.
