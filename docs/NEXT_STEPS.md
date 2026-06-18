# Next Steps

Review date: 2026-06-18.

## Source of truth for current versions

These docs are a snapshot of the last known verified state. They can become stale and must not be used as the only source of truth for live versions. Always verify the live/public state before telling a tester that a version is current.

Check the public Windows direct tester release from the live website manifest:

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

## Current release baseline

The live public tester manifest baseline must be checked from `latest.json`, not from this document. Last verified public snapshot: `latest.json` pointed to `LanguageVoiceTutorSetup-0.1.36-tester.16.exe` with `backendBaseUrl` set to `https://api.languagevoicetutor.com`, `minimumSupportedVersion` set to `0.1.36-tester.16`, and `updateMode` set to `manual-confirmation`. `0.1.36-tester.16` is the current uploaded Windows tester build in the public direct Windows release folder; previous tester release: `0.1.36-tester.15`; verify the website `latest.json` over HTTPS before tester handoff.

This is still a private tester/direct Windows release, not broad public production readiness.

## Latest verified release summary

Clean-machine smoke passed; small screen/tablet visual smoke passed; the localized Welcome Russian/French fix passed; the admin roles/permissions policy and UI policy tests passed; the desktop release gate passed; and backend `0.1.35-backend.24` is deployed and healthy. CMS/Admin published snapshot runtime validation passed for controlled tester lessons, and Save draft + Publish changes are visible in newly started desktop lessons.

## Immediate next steps

1. Hand off `0.1.36-tester.16` only through the controlled tester/direct Windows channel after verifying live `latest.json`.
2. Collect controlled tester feedback and keep non-blocking feedback in triage.
3. Validate update-over-existing-install from an older `EnglishVoiceTutor.Desktop.*` installed build if that path has not already been recorded for this exact tester handoff.
4. Only then decide the next smallest safe CMS/Admin or scenario/avatar behavior step.

Do not move billing/Paddle production readiness into the immediate next step; billing remains deferred until desktop hardening and tester feedback justify revisiting it.

## Subscription base plan deployment note

- Treat active `free` and `premium` plan rows as required database reference data.
- Missing plan rows break subscriptions and entitlements through FK constraints.
- The backend includes an idempotent EF migration to seed/upsert those rows; operators should apply migrations explicitly during backend deployment validation when this release is deployed.
- Do not add manual SQL as a recurring deployment requirement.
- Keep free/trial/Premium status backend-owned, with Premium determined by entitlements rather than Desktop local state or Paddle directly.
- Provider-event paid Premium should continue to stack after active trial/Premium access.
- Production/live Paddle readiness remains deferred.

## Current backend verification

Current state: last known production backend snapshot is `/opt/languagevoicetutor/backend/releases/0.1.35-backend.24` active via `/opt/languagevoicetutor/backend/current`; verify the live value from the server symlink before calling it current. Previous backend release for rollback reference: `/opt/languagevoicetutor/backend/releases/0.1.35-backend.23`. Backend `0.1.35-backend.24` contains the latest Admin CMS Validation & Preview readable UI fix plus `/admin` static asset cache busting/no-cache behavior. `https://api.languagevoicetutor.com/health` and `https://api.languagevoicetutor.com/api/health/database` return `200 OK`. No EF migration was required.

Deployed runtime status diagnostics are visible on backend `0.1.35-backend.24` from the server `/admin` page and protected runtime-status endpoint. The current server diagnostic is clean and confirms learner runtime uses CMS published snapshot: `effectiveSource=CmsPublishedSnapshot`, `validationSuccess=true`, `fallbackUsed=false`, no errors, no warnings, and `tutorBehaviorProfiles=3`. The tutor behavior profile mismatch was fixed by validating the approved tutor ids `david`, `elena`, and `nelli` instead of an obsolete exact count of 2. The next steps are intentionally small: collect controlled tester feedback, triage known non-blocking issues, and only then choose the next smallest safe CMS/Admin or scenario/avatar behavior step.

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

1. Verify the installed tester build from the public site and current `latest.json` before every handoff.
2. Validate update-over-existing-install from a prior `EnglishVoiceTutor.Desktop.*` installed tester build if not already recorded for this exact handoff, and confirm old installed `EnglishVoiceTutor.Desktop.*` files are cleaned from the install folder, preserved auth/session data migrates to the current `LanguageVoiceTutor.Desktop` local-data path, and login, user settings, Lesson History, and Progress survive update/reinstall.
3. Prepare the small external tester handoff group and instructions.
4. Collect feedback on lesson quality, A1/A2/B1/B2 level behavior, voice, UI, CMS-controlled content, and smaller-screen/touch behavior.
5. Keep known non-blocking follow-ups in triage: touch drag/hold can visually select multiple topic/subtopic items, some scenario/avatar dialogue can restart or repeat, short scenarios such as "Asking someone to repeat" may need prompt/content polishing, bot voice autoplay can sometimes not play even when enabled, and occasional server-error feedback should remain in triage unless reproduced consistently.

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
- Production role management/RBAC and critical-change approval remain deferred.
- Code signing remains deferred.
- Broader public release readiness remains deferred until after controlled tester feedback and operational hardening.

## CMS runtime status validation path

The Admin CMS now exposes a read-only **Runtime content status** section and the protected endpoint `GET /api/admin/dev/cms/runtime-status`. Use it to confirm the effective learner content source, validation result, counts, published snapshot metadata, and fallback state without exposing content bodies or secrets.

CMS published snapshot is the active runtime source. The diagnostic confirms runtime source and fallback state. Runtime status is clean on backend `0.1.35-backend.24` with approved tutor-id validation for `david`, `elena`, and `nelli`. Normal status should show `effectiveSource=CmsPublishedSnapshot`, `validationSuccess=true`, `fallbackUsed=false`, no errors, and no warnings. Rollback remains disabling CMS runtime flags and restarting backend so runtime returns to static JSON. Billing/Paddle is not involved.

## CMS-managed level profiles (A1-B2)

- CMS now manages A1, A2, B1, and B2 level behavior profiles through the CMS Content **Levels** tab.
- Level profiles include stable level keys, display names, active flags, sort order, wrap-up turn, final-message turn, language complexity guidance, correction guidance, answer-length guidance, and admin notes.
- Lesson length defaults come from the selected level profile: A1 is configured for a shorter lesson around 15 learner turns, while B2 supports a longer dialogue.
- Scenario-specific lesson length values remain optional overrides when explicitly set and valid. Priority is: scenario override, then CMS level profile, then safe backend constants.
- Backend runtime content remains the source of truth for lesson behavior. Desktop may keep its current level labels for display, but desktop and future mobile should use backend runtime behavior from the CMS published snapshot.
- Static JSON fallback remains available; fallback runtime also receives safe default level profiles.

## CMS tutor display name verification

When changing a tutor Display name in Admin CMS, use Save draft + Publish and then start a new desktop lesson to verify the lesson chat bubble uses the CMS-published display name. Keep the stable tutor/avatar IDs (`elena`, `nelli`, `david`) unchanged because avatar image selection continues to use those IDs rather than display names.

## Premium billing follow-up

- Continue validating the desktop Buy Premium and cancel-renewal flows against the sandbox backend.
- Verify webhook-driven entitlement activation and cancel-at-period-end subscription snapshots before any production/live Paddle launch decision.
- Do not add refund/reversal handling or Paddle customer portal flows until those backend-owned lifecycle policies are explicitly designed.
