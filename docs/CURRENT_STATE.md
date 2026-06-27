# Current State

Review date: 2026-06-27.

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

Generated local files under `artifacts/`, including `latest.json`, `changelog.json`, `known-issues.json`, `checksums.sha256`, installers, and packages, are generated release outputs and must not be committed. Generated local files under `artifacts/` are not proof that a version is live on the public site. A locally built installer becomes public only after the Windows direct release files are uploaded to the website release folder and `latest.json` is verified over HTTPS.


## Admin/CMS statistics boundary fix (2026-06-25)

- Admin/CMS release analytics now treat Premium and Trial users as access categories, not installs/devices.
- The current live card label is `Tracked signed-in app/device records`. The app/device metric is a count of signed-in backend `DeviceEntity` app/device records only; it is not raw installer downloads and must not include Premium entitlements, Trial grants, subscription snapshots, billing events, or users solely because they currently have Premium access.
- Registered users remain derived from backend `UserEntity` rows, active trials from active Trial grants, active Premium users from active Premium entitlements, active/free user categories from recent activity and current access state, and language statistics from user settings/profile or lesson/usage activity as appropriate.
- This is a release analytics correctness fix before Paddle live readiness. Production/live Paddle readiness remains deferred, and broad public production readiness is still not claimed.

## Current tester Windows direct release

The public website `latest.json` remains the public source of truth for the live Windows direct tester release. Latest built/manual-check snapshot: `0.1.36-tester.24` installer was built and manually checked for controlled sandbox billing validation. The public website `latest.json` still must be checked before handoff because it is the public source of truth for what is live. This is the current verified uploaded tester release snapshot. Continue to verify the HTTPS `latest.json` before handoff. This remains a controlled tester/direct Windows release, not a broad public production launch.

## Release backend lock (server-only installed builds)

Release/tester installed builds are server-only. The only backend for packaged non-Debug Windows builds is `https://api.languagevoicetutor.com`. Local backend URLs are DEBUG/developer-only and must not be present as normal user Settings options. Diagnostics and Backend URL editing are not part of user/release Settings. Stale AppData `settings.json` backend URL values from older installs are ignored by release builds and are not written back into user-editable settings.

Clean-machine smoke must verify registration/login/lesson/history/progress/update from an installed build against the fixed production backend. The installed build connectivity signal is `GET https://api.languagevoicetutor.com/health`; registration calls `POST https://api.languagevoicetutor.com/api/auth/register`, login calls `POST https://api.languagevoicetutor.com/api/auth/login`, and auth restore calls `GET https://api.languagevoicetutor.com/api/auth/me`. Optional cloud settings or subscription/status endpoint failures must not block auth or lessons and must not be treated as the backend connectivity signal.


## Current production backend state

Current state: verified production backend snapshot is `/opt/languagevoicetutor/backend/releases/0.1.35-backend.57`, and `/opt/languagevoicetutor/backend/current` points to that release. Verify the live value with the server symlink command before calling it current.

Previous backend release for rollback reference must be verified from `/opt/languagevoicetutor/backend/previous` before rollback.

Completed: current backend `0.1.35-backend.57` is deployed and includes the Admin statistics device/payment metric wording and boundary fixes, retains the Phase 5C Production logging hardening first deployed in `0.1.35-backend.40`, and includes the Phase 3 rate limiting / abuse-protection implementation plus the earlier Admin RBAC persistence/cutover validation work and billing/subscription foundation work: current-user cancel-renewal endpoint, Paddle cancel-at-period-end adapter support, current-user subscription status fields needed by the Desktop Account billing UI, and a cancel request path that must not directly revoke `EntitlementEntity`. It also retains the Admin CMS Validation & Preview readable UI fix and `/admin` static asset cache busting/no-cache behavior from earlier backend releases.


Completed: Admin Product Statistics now shows `Tracked signed-in app/device records`, `Successful payments total`, and `Successful payments current month`. Successful payment metrics are internal payment/billing-event metrics, separate from active Premium entitlement state. Production/live Paddle readiness remains deferred, and broad public production readiness is still not claimed.

## Production Admin RBAC current state

Current state: backend production deploy `0.1.35-backend.57` is complete, `/opt/languagevoicetutor/backend/current` points to `/opt/languagevoicetutor/backend/releases/0.1.35-backend.57` at last verification. Production migration `20260620165657_AddAdminRoleAssignmentPersistence` has been applied. Production now has the Admin RBAC persistence tables `admin_users`, `admin_user_roles`, and `admin_role_assignment_events`.

Completed: the first persistent owner-equivalent Admin mapping exists, the current admin actor mapping resolves, and the active persistent production admin role is `super_admin`. Role-assignment diagnostics reported `totalAdminUsers=1`, `activeAdminUsers=1`, `totalRoleAssignments=1`, `activeRoleAssignments=1`, and `rolesInUse` includes `super_admin`.

Completed on 2026-06-22: the Production Admin RBAC cutover rehearsal was performed successfully against `https://api.languagevoicetutor.com`. Current production backend was `/opt/languagevoicetutor/backend/releases/0.1.35-backend.39`; `/health` and `/api/health/database` returned `200 OK`, and the production database was healthy. A second backup `super_admin` account was created through the existing Admin Role Management UI. Final diagnostics after backup admin setup reported `totalAdminUsers=2`, `activeAdminUsers=2`, `activeRoleAssignments=2`, and `rolesInUse=super_admin`. Both approved admin accounts could log in to `/admin` and passed `tools/smoke_admin_rbac_cutover_validation.ps1` while fallback was enabled.

Completed: during pre-rehearsal validation, both approved `super_admin` accounts passed with `ExpectedFallbackEnabled true` and `ExpectedActorMappingFound true`; AdminPermission read endpoints and role-management read endpoints returned `200`. Status showed `fallbackEnabled=True`, `defaultFallbackEnabled=True`, `configValuePresent=False`, `persistentRoleAuthorizationEnabled=True`, and `actorMappingFound=True`. During the controlled rehearsal, a timestamped backup of `/etc/languagevoicetutor/backend.env` was created, `AdminAuthorization__EnableBootstrapAdminFallbackForAdminPermissionPolicies` was temporarily set to `false`, and `languagevoicetutor-backend.service` was restarted. Both approved accounts then passed with `ExpectedFallbackEnabled false` and `ExpectedActorMappingFound true`; AdminPermission read endpoints and role-management read endpoints returned `200`. Disabled-fallback status showed `fallbackEnabled=False`, `defaultFallbackEnabled=True`, `configValuePresent=True`, `persistentRoleAuthorizationEnabled=True`, and `actorMappingFound=True`.

Final state: the earlier rehearsal passed and rollback/restoration was proven, and the later permanent fallback disable also passed on 2026-06-22. Production `backend.env` now has `AdminAuthorization__EnableBootstrapAdminFallbackForAdminPermissionPolicies=false`, `languagevoicetutor-backend.service` was restarted successfully, `/health` and `/api/health/database` returned `200 OK`, and both approved persistent `super_admin` accounts passed `tools/smoke_admin_rbac_cutover_validation.ps1` with `ExpectedFallbackEnabled false`, `ExpectedActorMappingFound true`, `ExpectedAdminPermissionEndpointStatus 200`, and `ExpectedRoleManagementEndpointStatus 200`. Current RBAC status showed `fallbackEnabled=False`, `defaultFallbackEnabled=True`, `configValuePresent=True`, `persistentRoleAuthorizationEnabled=True`, and `actorMappingFound=True`. BootstrapAdmin fallback for `AdminPermission:*` policies is now disabled in production, persistent role authorization is enabled and verified, two persistent `super_admin` accounts are verified, and rollback remains available by setting `AdminAuthorization__EnableBootstrapAdminFallbackForAdminPermissionPolicies=true` and restarting the backend.

Public release is still not complete. Production Admin RBAC fallback disable is complete, but non-owner role validation and critical-change approval remain future work.

## Phase 3 rate limiting / abuse protection current state

Completed and production-verified: Phase 3 rate limiting / abuse protection is implemented on backend `0.1.35-backend.39` and enabled in production with `RateLimiting__Enabled=true`. The active implementation is single-instance/in-memory limiter protection; true distributed/shared limiter storage remains future work before multi-instance scale-out. True concurrent realtime voice WebSocket connection caps remain future work if not implemented by the current backend.

Completed Phase 3 coverage includes auth login/register/password reset, auth refresh/revoke/current-user/password-change, authenticated lesson start, lesson chat reply, lesson hint, lesson feedback, authenticated persisted lesson messages, audio transcription/STT, TTS speech and speech stream, translation, realtime voice start-rate protection, Admin read/write/role-management throttling, billing checkout and cancel-renewal throttling, Paddle checkout launch throttling, Paddle webhook throttling, and final-slice learner/subscription/status/trial/access-style endpoints where implemented.

Phase 3 did not change Admin RBAC authorization behavior, product/free usage semantics, Premium entitlement semantics, billing/Paddle semantics, Paddle webhook signature verification, or provider-event handling. BootstrapAdmin fallback remains disabled in production with `AdminAuthorization__EnableBootstrapAdminFallbackForAdminPermissionPolicies=false`. No database migrations were added or run for Phase 3, and backend package/upload scripts still do not run EF migrations automatically.

Normal production checks passed after the Phase 3 deployment: backend `/health`, `/api/health/database`, Admin RBAC smoke with `fallbackEnabled=False`, `persistentRoleAuthorizationEnabled=True`, and `actorMappingFound=True`, Admin UI manual check, Desktop app manual check, and Billing UI manual check. Billing UI behavior remained unchanged: paid users see payment action inactive, and non-premium users see payment action active.

## Phase 5A logging/privacy audit current state

Completed on 2026-06-23: Phase 5A lightweight production logging/privacy audit is documented in `docs/LOGGING_PRIVACY_AUDIT.md`. This was documentation/audit only: no code, backend runtime behavior, Desktop behavior, billing/Paddle semantics, EF migrations, deployment scripts, external services, or heavy monitoring infrastructure were changed. No obvious dangerous source-code logging issue requiring immediate fix was found. Operator guidance now explicitly says what terminal output may be pasted and what must be redacted, and the smallest next hardening step is a bounded production log sampling/redaction checklist. Broad public production readiness is still not claimed, and production/live Paddle readiness remains deferred.

Phase 4A initial production-safe backup/readability/separate-drill-restore was completed on 2026-06-23. The active backend before and after the drill was `/opt/languagevoicetutor/backend/releases/0.1.35-backend.39`; `/health` and `/api/health/database` returned `200 OK` before and after. The production PostgreSQL custom-format backup `/var/backups/languagevoicetutor/postgres/lvt_app_db_20260623_045111Z.dump` was created successfully, `ls` showed size `3.4M`, and `pg_restore --list` succeeded with `245` lines. The backup was restored into separate drill database `lvt_app_db_restore_drill_20260623_045111Z`, not over production. Required table checks passed for `__EFMigrationsHistory`, `users`, `subscriptions`, `entitlements`, `plans`, `admin_users`, `admin_user_roles`, and `admin_role_assignment_events`; latest migration `20260620165657_AddAdminRoleAssignmentPersistence` was confirmed; drill database cleanup completed and the final cleanup check returned no rows. No production data dump was committed or pasted, no database migrations were run, no backend code/runtime behavior changed, and no deployment/package scripts changed. Because the drill used `pg_restore --no-owner --no-acl`, postgres-only ownership/grants in the restored drill DB are expected and do not prove production permission-fidelity. Phase 4B repository-managed assets exist for local PostgreSQL backup scheduling; production activation happened on 2026-06-23, but rebuilt hosts still require manual operator installation and verification. Phase 4B local PostgreSQL backup scheduling was installed and activated on production on 2026-06-23 via `languagevoicetutor-postgres-backup.timer`; the timer is enabled and `active (waiting)`, with latest backup readability verified by `pg_restore --list` at `245` lines for `/var/backups/languagevoicetutor/postgres/lvt_app_db_20260623_150541Z.dump`. Off-server encrypted backups remain optional future infrastructure hardening, not an immediate release blocker.

## Phase 4C migration rollback/remediation dry-run rehearsal

Completed on 2026-06-23: Phase 4C migration rollback/remediation dry-run rehearsal passed as a read-only production verification. Backend `current` pointed to `/opt/languagevoicetutor/backend/releases/0.1.35-backend.39`, with previous rollback reference `/opt/languagevoicetutor/backend/releases/0.1.35-backend.38`; `/health` and `/api/health/database` both returned `200 OK`. Latest readable PostgreSQL backup was `/var/backups/languagevoicetutor/postgres/lvt_app_db_20260623_153008Z.dump`, and `pg_restore --list` returned `245` lines. Production latest EF migration was `20260620165657_AddAdminRoleAssignmentPersistence`. Required key tables checked `OK`: `__EFMigrationsHistory`, `users`, `subscriptions`, `entitlements`, `plans`, `admin_users`, `admin_user_roles`, and `admin_role_assignment_events`.

Operational checks also passed: `languagevoicetutor-backend.service` was active and enabled; `languagevoicetutor-postgres-backup.timer` was enabled and active with next observed run `2026-06-24 03:15 CEST`; and Contabo VPS Auto Backup was manually confirmed enabled as an additional provider/VPS-level layer, not a substitute for PostgreSQL `pg_dump`/`pg_restore` validation. The rehearsal made no production DB mutation, ran no EF migrations, applied no SQL remediation, attempted no restore-over-production, and changed no backend runtime, Desktop, Admin, CMS, billing, or Paddle behavior. Broad public production readiness is not claimed, and production/live Paddle readiness remains deferred.

## Phase 4D permission-fidelity restore drill

Completed on 2026-06-23: Phase 4D permission-fidelity restore drill passed for the current release-readiness level. An owner/ACL-aware backup of production database `lvt_app_db` was created at `/var/backups/languagevoicetutor/postgres/lvt_app_db_owner_acl_20260623_161611Z.dump`, confirmed non-empty at `3.4M`, and passed a `pg_restore --list` readability check with `245` lines. The backup was restored into separate drill database `lvt_app_db_owner_acl_drill_20260623_161611Z`; no production restore-over was attempted.

Key public tables in production were owned by `lvt_app`, and `lvt_app` had the expected privileges on checked key tables. In the drill database, key tables returned `OK` for `__EFMigrationsHistory`, `users`, `subscriptions`, `entitlements`, `plans`, `admin_users`, `admin_user_roles`, and `admin_role_assignment_events`; latest migration was `20260620165657_AddAdminRoleAssignmentPersistence`; checked ownership remained `lvt_app`; and checked grants matched the production baseline. Drill cleanup completed: backend termination returned `0` rows, `dropdb` completed, and the final existence query returned no rows. Production backend remained healthy on `/opt/languagevoicetutor/backend/releases/0.1.35-backend.39`, with `/health` and `/api/health/database` returning `200 OK`. No EF migrations, SQL remediation, runtime/deployment/package, Desktop, Admin UI, CMS, billing, or Paddle changes occurred.

Phase 4 backup/restore/migration rollback drills are complete for the current release-readiness level: Phase 4A backup/readability/separate restore drill completed; Phase 4B local scheduled PostgreSQL backups active; Phase 4C migration rollback/remediation dry-run completed; and Phase 4D permission-fidelity restore drill completed. Off-server encrypted backups remain optional future infrastructure hardening, not an immediate release blocker.

## Subscription base plan reference data

The `plans` table requires active `free`, `trial`, and `premium` reference rows. Missing rows break subscription and entitlement writes through `FK_subscriptions_plans_PlanId` and `FK_entitlements_plans_PlanId`. The required rows are `free / Free / free / active`, `trial / Trial / premium / active`, and `premium / Premium / premium / active`. The Trial reference plan exists in production and is required for learner-facing tariff display while Trial access remains entitlement-owned. Applying EF migrations remains a separate explicit operator action and is not performed by packaging/upload scripts.

Free/trial/Premium status logic remains backend-owned. Premium access is determined by entitlements, not Desktop local state or Paddle directly. Provider-event paid Premium stacks after active trial/Premium access. If a user is on trial and buys Premium, paid Premium starts after `trialEndsAtUtc` and preserves the paid duration; future-start provider-event Premium does not count as `premiumActive` until `StartsAtUtc`, so the active trial remains the current access source until trial expiry. Production/live Paddle readiness remains deferred.

## Auth, account, and persistence

Registration and login now work from installed tester/release builds against `https://api.languagevoicetutor.com`, including the current backend permission-fixed login path. Trial assignment is granted after registration. The desktop stores the authenticated session under the current user's app-data area with Windows DPAPI protection and does not store raw passwords. Logout clears persisted auth session data.

Auth session persistence works across app restart and Windows restart. Installed file names were renamed to `LanguageVoiceTutor.Desktop.*`. Legacy installed `EnglishVoiceTutor.Desktop.*` application files are cleaned from the install folder during update/reinstall without deleting user AppData. Update from older builds must migrate preserved auth/session data from legacy `EnglishVoiceTutor.Desktop` local-data paths, and update/reinstall must preserve login, settings, Lesson History, and Progress. For policy tracking, update/reinstall should preserve auth session, user settings, Lesson History, and Progress.

## Lessons and learner runtime

The current tester build has verified lesson start, normal lesson chat, TTS/bot voice, Conversation Mode, Lesson History saving, and Progress. Learner runtime content now uses the CMS published snapshot as the active content source.

Early manual **Finish lesson** clicks during an active lesson now ask for confirmation so accidental clicks do not end the session. Forced/final **Finish lesson** after the lesson limit remains one-click, and this is a desktop UI safety fix only. No backend deploy, no DB migration, and no Windows upload are required until an installer is intentionally packaged and published.

## CMS/Admin and runtime content

Current state: CMS/Admin is connected, and the CMS published snapshot is now the active runtime content source for learner lessons. Runtime status is clean with `effectiveSource=CmsPublishedSnapshot`, `validationSuccess=Yes`, `fallbackUsed=No`, no errors, and no warnings.

Completed: CMS scenario edits are visible in the desktop app after the operator clicks **Save draft** and then **Publish current draft**. `Save draft` alone remains draft-only and does not affect the app; **Publish** is required. Existing active lessons may keep old content until the learner starts a new lesson.

Completed: CMS-managed A1, A2, B1, and B2 level behavior profiles are active and affect lesson behavior. A1 and B2 lessons differ as expected, and additional level polishing will continue later based on tester feedback.

Fallback to packaged static JSON remains available for initialization and rollback/safety, but fallback should not be active during normal runtime status. The Admin CMS action **Initialize from static JSON** is the explicit first-setup/recovery action for `static-json-v1`; it initializes CMS draft/admin content from packaged static JSON, does not publish automatically, and does not switch runtime. Normal learner runtime should use the CMS published snapshot when `CmsContent__UsePublishedSnapshotForRuntime=true` and the published snapshot is active and valid, with status showing `effectiveSource=CmsPublishedSnapshot`, `validationSuccess=Yes`, `fallbackUsed=No`, no errors, and no warnings. Static JSON remains an initialization/emergency fallback source, not the normal learner runtime source when CMS published snapshot is active and valid. Production Admin RBAC cutover rehearsal passed on 2026-06-22, and the later permanent production fallback disable also passed on 2026-06-22. BootstrapAdmin fallback for `AdminPermission:*` policies is now disabled; rollback remains setting `AdminAuthorization__EnableBootstrapAdminFallbackForAdminPermissionPolicies=true` and restarting the backend. Critical-change approval remains future work.

## Admin roles/permissions foundation

Current state: stable admin role constants exist for `super_admin`, `support`, `content_editor`, `billing_support`, and `read_only_auditor` (with legacy alias constants mapped to the current target role ids). Stable permission constants exist for admin self/capabilities, users, audit, CMS, runtime status, subscriptions diagnostics, premium grant/revoke, free lesson allowance reset, billing diagnostics, and product statistics. Bootstrap admins map to `super_admin` and currently receive the full permission set. A static production role-to-permission catalog exists as a foundation seam, but endpoint enforcement remains BootstrapAdmin-based for controlled testing only.

Admin Shell roles/permissions UI-awareness is completed. The Admin Shell loads `/api/admin/me` and `/api/admin/capabilities`; Overview shows admin source, environment, checked timestamp, Bootstrap admin status, role badges, permission count, and a Roles and permissions card. Available workflows are rendered from permissions informationally only. Tabs, buttons, and backend calls are not blocked by the client-side permission view. The System tab shows `productionRolesAvailable=false` and keeps Billing/Paddle unavailable/deferred.

`/api/admin/me` exposes `roles`, `permissions`, and `isBootstrapAdmin`. `/api/admin/capabilities` exposes roles and permissions. Production Admin RBAC persistence and role-management validation now exist, 35 existing Admin endpoint registrations are protected by `AdminPermission:*` policies, and the first persistent `super_admin` owner-equivalent mapping exists. BootstrapAdmin fallback no longer authorizes `AdminPermission:*` endpoints because production now explicitly sets the fallback flag to disabled after the successful 2026-06-22 permanent disable. Do not claim Production Admin RBAC is fully complete until remaining non-owner role validation and critical-change approval are accepted. See `docs/PRODUCTION_ADMIN_RBAC_READINESS.md`.

## Settings sync, device tracking, and product statistics

Completed: settings sync now separates native language, selected study language, and explanation/interface language. `UserProfileEntity.NativeLanguage` is the source for native language, `UserSettingsEntity.StudyLanguage` remains the selected supported study language, and `UserSettingsEntity.ExplanationLanguage` is the separate explanation/interface language. Desktop sends `SelectedNativeLanguageOption.Id` as `NativeLanguage` and no longer sends native language as `ExplanationLanguage`. Existing `unknown` native-language values are not blindly backfilled; they are corrected when users save/sync from a fixed desktop client unless a reliable backend-side source is identified later.

Completed: authenticated device tracking is privacy-safe and counts coarse backend `DeviceEntity` rows, not installer downloads or raw installs. `AppVersion` is latest-seen metadata, not part of identity, so the same user + platform + coarse device name updates `LastSeenAt` and latest `AppVersion` instead of creating a new device row after every app update. Raw hardware identifiers, machine fingerprints, serial numbers, MAC addresses, Windows usernames, IP addresses, and personal device IDs are not collected.

Completed: Admin Product Statistics now exposes aggregate-only product metrics. Language distributions keep native language from profile, explanation language from settings, selected study language from settings, and practiced study language from lesson/usage activity filtered to supported study languages. Unsupported dirty study-language values such as Russian in `usage_events.StudyLanguage` are grouped as `Unknown/Unsupported`, not shown as supported study languages.

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

## Desktop localization audit status

Current audited release-blocking localization issues have been addressed for the 14 release-ready interface languages: `en`, `es`, `fr`, `de`, `it`, `pt`, `ru`, `pl`, `ar`, `ja`, `ko`, `sr`, `hr`, and `bg`. `tools/audit_interface_localization.py` is the localization audit tool, and the welcome/home hero layout hardening includes long localized hero text stability checks. If `tools/test_welcome_layout_stability_policy.py` exists in the branch, keep it in the release gate for localized home/welcome layout policy checks. This does not mean localization is permanently complete; future languages should be added only after full UI/audit coverage.

## Desktop startup window and Welcome layout

The desktop window clamps startup size and position to the visible working area before normal use, respecting the Windows taskbar and display scaling. The Welcome/start screen primary actions are visible on smaller laptop screens without scrolling. The cover image uses cover-style fill/crop behavior without gray letterbox bars.

Clean-machine smoke must include a smaller laptop / scaled display check, including at least one 1366x768, 1280x720, or scaled-display equivalent where the title bar, close button, dragging, Settings, Account, Learning, Progress, Lesson History, lesson start, and Conversation Mode are verified. Backend/auth/lessons remain unchanged by this window-placement fix.

## Billing and subscriptions

Billing work is in controlled sandbox/tester validation, not broad production/live billing readiness. Trial entitlement for registration is working, the Trial reference plan exists and is required, Paddle sandbox checkout works through backend-hosted checkout, and cancel-renewal works in sandbox. Premium activates only after valid backend Paddle webhook processing updates entitlement state. Admin support cancel-renewal exists and requires a reason. Production/live Paddle readiness, broad payment readiness, referral/promo logic, and public launch readiness remain deferred.

## Release status

Current state: the project is preparing for controlled release / tester handoff. CMS readiness is now part of release preparation because practical content operations must work before broader handoff. Broad public production release is still not ready. Production billing remains deferred, and Paddle production readiness remains deferred.

Not ready yet: do not claim broad public production readiness, production billing readiness, production role management/RBAC readiness, critical-change approval readiness, full Admin CMS production readiness, or mobile readiness.

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

Current verified tester/release summary:

- clean-machine install from the public download works;
- installed app launches correctly;
- registration/login works and auth/session persists after restart;
- interface language, native language, and study language selection work;
- lesson start, translation, hints, bot voice/TTS, Conversation Mode, Lesson History, and Progress work;
- CMS scenario edits and level profile edits are visible in newly started desktop lessons after **Save draft** plus **Publish**;
- smaller Windows tablet / small-screen visual smoke passed for Welcome/start, primary actions, Settings, and lesson flow;
- Russian and French Welcome/start header text no longer truncates or clips after the localized layout fix;
- admin roles/permissions policy and UI policy tests passed, the desktop release gate passed, and backend `0.1.35-backend.56` is deployed and healthy with Admin RBAC persistence migration applied.

Remaining realistic readiness items:

1. Validate update-over-existing-install from old `EnglishVoiceTutor.Desktop.*` executable builds and confirm preserved auth/session data migrates to the current `LanguageVoiceTutor.Desktop` local-data path without losing login, settings, Lesson History, or Progress.
2. Hand off to a small controlled external tester group.
3. Establish the feedback collection and triage process.
4. Triage known tester feedback without claiming it is solved: touch drag/hold can visually select multiple topic/subtopic items, some scenario/avatar dialogue can restart or repeat, short scenarios such as "Asking someone to repeat" need content polishing, bot voice autoplay can sometimes not play even when enabled, and occasional server-error feedback should stay in triage unless reproduced consistently.
5. Continue controlled sandbox billing validation, especially cancel-renewal UX and Paddle sandbox end-to-end cancellation, without claiming production/live billing readiness.
6. Add code signing later to reduce SmartScreen friction before broad distribution.

Do not state that the product is fully public production-ready. The current state is a validated controlled tester/direct Windows release.

- Lesson Chat UI polish: Finish confirmation typography improved, Start recording is green, Hint uses hint-color styling.

## CMS runtime status diagnostics

Completed: the backend has an admin-only, read-only CMS runtime content status diagnostic at `GET /api/admin/dev/cms/runtime-status` (legacy alias: `/api/admin/dev/cms/runtime-content/status`). It reports safe metadata only: source, content pack slug, CMS runtime flags, fallback state, published version/hash when applicable, content counts, validation state, and bounded errors/warnings. It does not return lesson bodies, scenario `DefinitionJson`, prompt bodies, tutor instruction bodies, secrets, tokens, API keys, connection strings, or auth headers.


Current runtime-status result on deployed backend `0.1.35-backend.56` is clean, retaining the CMS-first/runtime behavior previously verified on `0.1.35-backend.48`: `effectiveSource=CmsPublishedSnapshot`, `validationSuccess=Yes`, `fallbackUsed=No`, no errors, no warnings, and `tutorBehaviorProfiles=3`. Learner runtime now uses the CMS published snapshot. The prior runtime-validation root cause was an obsolete hardcoded exact tutor behavior profile count of 2. Static JSON, CMS static import/draft construction, and desktop tutor avatar options all define the approved tutor ids `david`, `lana`, and `nelli`; the third profile is legitimate product content, not a smoke/test artifact. Runtime validation now derives the required tutor ids from the approved desktop avatar definitions and reports expected, actual, missing, unknown/extra, and duplicate tutor ids without exposing tutor instruction bodies. The `tools/smoke_cms_runtime_status.ps1` and `tools/validate_cms_published_snapshot_runtime.ps1` scripts default to the server-only backend `https://api.languagevoicetutor.com`; localhost must be passed explicitly only for approved local developer runs.

This diagnostic confirms the current runtime content source. Static JSON fallback remains available for safety and rollback, but it should not be active in normal runtime status now that CMS published snapshot runtime is active.

## CMS-managed level profiles (A1-B2)

- CMS now manages A1, A2, B1, and B2 level behavior profiles through the CMS Content **Levels** tab.
- Level profiles include stable level keys, display names, active flags, sort order, wrap-up turn, final-message turn, language complexity guidance, correction guidance, answer-length guidance, and admin notes.
- Lesson length defaults come from the selected level profile: A1 is configured for a shorter lesson around 15 learner turns, while B2 supports a longer dialogue.
- Scenario-specific lesson length values remain optional overrides when explicitly set and valid. Priority is: scenario override, then CMS level profile, then safe backend constants.
- Backend runtime content remains the source of truth for lesson behavior. Desktop may keep its current level labels for display, but desktop and future mobile should use backend runtime behavior from the CMS published snapshot.
- Static JSON fallback remains available; fallback runtime also receives safe default level profiles.

## CMS tutor display names

CMS tutor behavior profile display names are runtime-managed learner-facing labels. The approved tutor/avatar IDs (`lana`, `nelli`, `david`) remain stable internal identifiers for profile lookup and avatar image selection, while newly started desktop lessons prefer the published CMS display name from backend runtime lesson content and fall back to packaged static names only when runtime metadata is unavailable.

### CMS tutor display names

CMS tutor Display name is now the runtime-managed learner-facing tutor name after Save draft + Publish. Stable tutor/avatar IDs remain fixed as `lana`, `nelli`, and `david`; desktop new lessons and the Settings tutor avatar dropdown resolve published CMS display names by stable ID and fall back to packaged local names if runtime metadata is unavailable. Safety notes JSON remains available for extra behavior rules, but it should not be the normal way to rename a tutor.

## Desktop Premium billing controls

Desktop `v0.1.36-tester.24` Account UI is simplified to four learner-facing subscription lines: **Current tariff**, **Free lessons remaining**, **Premium**, and **Auto-renewal**. It also includes Account controls for **Buy Premium**, **Cancel subscription**, and **Refresh status**. Buy Premium calls the backend checkout-session endpoint, opens the backend-hosted Paddle checkout URL in the browser, never calls Paddle directly, and does not activate Premium locally; after payment, the user must return to the app and refresh status so the app can read backend entitlement state after valid webhook processing. Cancellation is backend-owned cancel-renewal/cancel-at-period-end behavior, not immediate paid-access removal: the request path must not directly revoke `EntitlementEntity`, and existing paid Premium or scheduled paid Premium remains until entitlement expiry.

Known billing follow-ups: continue sandbox/live-separation checks, provider diagnostics review, production Paddle readiness planning, and support runbook hardening. This remains controlled tester/sandbox billing validation only; production/live Paddle readiness, referral/promo logic, and broad public launch readiness are still deferred.


## Phase 3 rate limiting / abuse protection completion - 2026-06-23

Phase 3 rate limiting / abuse protection is implemented, deployed, enabled, and production-verified on backend `0.1.35-backend.39` with `RateLimiting__Enabled=true`. The active implementation is single-instance/in-memory limiter protection. Completed coverage includes auth login/register/password reset, auth refresh/revoke/current-user/password-change, authenticated lesson start, lesson chat reply, lesson hint, lesson feedback, authenticated persisted lesson messages, audio transcription/STT, TTS speech and speech stream, translation, realtime voice start-rate protection, Admin read/write/role-management throttling, billing checkout and cancel-renewal throttling, Paddle checkout launch throttling, Paddle webhook throttling, and learner/subscription/status/trial/access-style endpoints where implemented by the final slice.

Admin RBAC authorization behavior was not changed, BootstrapAdmin fallback remains disabled with `AdminAuthorization__EnableBootstrapAdminFallbackForAdminPermissionPolicies=false`, product/free usage semantics were not changed, Premium entitlement semantics were not changed, billing/Paddle semantics were not changed, Paddle webhook signature verification and provider-event handling were not changed, CMS runtime content behavior was not changed, and no database migrations were added or run for Phase 3. Product/free-usage exhaustion remains separate from technical `RateLimitExceeded` throttles.

Future work remains: true distributed/shared limiter storage before multi-instance scale-out, true concurrent realtime voice WebSocket connection caps if still not implemented, backup schedule/retention monitoring, optional off-server encrypted backup infrastructure hardening, monitoring/privacy hardening, Paddle live readiness/legal/support blockers, Microsoft Store/MSIX, and broad public-production readiness. Broad public-production readiness is still not claimed.

## Phase 4B local PostgreSQL backup schedule activation - 2026-06-23

Phase 4B local PostgreSQL backup scheduling is installed and active on production. The active timer is `languagevoicetutor-postgres-backup.timer`; it is enabled and `active (waiting)`, with next observed trigger `2026-06-24 03:15 CEST`. The systemd one-off service run succeeded with `Result=success`, `ExecMainCode=0`, and `ExecMainStatus=0`. Latest backup readability was verified with `pg_restore --list` returning `245` lines for `/var/backups/languagevoicetutor/postgres/lvt_app_db_20260623_150541Z.dump`. Historical Phase 4B verification kept production backend healthy on `0.1.35-backend.39`; at current verification `/opt/languagevoicetutor/backend/current` points to `/opt/languagevoicetutor/backend/releases/0.1.35-backend.57`, and `/health` plus `/api/health/database` returned `200 OK`.

The production-safe script path is `/opt/languagevoicetutor-ops/postgres/backup_lvt_postgres.sh` because the `postgres` account must traverse the backup script path without gaining access to the backend release/config tree. Linux shell/systemd assets must use LF line endings, enforced through `.gitattributes`, because CRLF copied from Windows broke Bash on Ubuntu before correction. The service uses `WorkingDirectory=/tmp` to avoid `postgres` working-directory warnings during manual or systemd execution. Off-server encrypted backups remain optional future infrastructure hardening; broad public production readiness and production/live Paddle readiness remain deferred.


## Phase 4C migration rollback/remediation rehearsal assets

Phase 4C operator-readiness assets now exist: `docs/MIGRATION_ROLLBACK_REMEDIATION_RUNBOOK.md` and `tools/migration_rollback_remediation_commands.ps1`. This records documentation and a command-printer helper only. No production database state was mutated, no SQL was applied, no EF migrations were run, no restore-over-production was attempted, no backend runtime behavior changed, and no Desktop, Admin UI, CMS, billing/Paddle, package, upload, or deployment behavior changed as part of this step. A real migration rollback/remediation rehearsal still requires a separate approved execution window or non-production/drill target.

The runbook documents that backend code rollback and database rollback/remediation are separate. Code rollback may be the safest first action for a bad backend deploy, but it does not revert schema or data changes. Targeted SQL remediation may be appropriate only after separate review and fresh readable backup evidence; broad unreviewed SQL is forbidden.

Phase 4A backup/readability/separate-drill-restore remains completed. Phase 4B local PostgreSQL backup scheduling remains active on production via `languagevoicetutor-postgres-backup.timer`, with the latest known readability check returning `245` `pg_restore --list` lines. Contabo VPS Auto Backup is enabled as an additional provider-level safety layer, but it does not replace PostgreSQL backup/restore validation. Off-server encrypted backups remain optional future infrastructure hardening, while production/live Paddle readiness and broad public production readiness remain deferred.

## 2026-06-24 Phase 5C production logging level hardening

Phase 5B bounded production log sampling found over-verbose EF Core SQL command logging in production: `Microsoft.EntityFrameworkCore.Database.Command[20101]` appeared at `Information` level with SQL command text. Sampled parameter values were redacted as `?`, and no raw passwords, bearer tokens, refresh-token values, connection strings, OpenAI API keys, raw Paddle payload contents, raw SQL dumps, or raw secrets were observed, so this is not recorded as a data breach. It is recorded as a release-readiness logging-noise/privacy issue because SQL text can expose sensitive schema/field names and large CMS selections.


## Admin payment-event statistics

Completed and production-verified on backend `0.1.35-backend.56`: Admin Product Statistics now distinguishes access-state metrics from payment-event metrics. Live server verification of `wwwroot/admin/admin.js` showed `successfulPaymentsTotal`, `successfulPaymentsCurrentMonth`, and `Tracked signed-in app/device records`. `Active Premium users now` remains an entitlement/access-state metric based on currently active Premium access and does not change the paid-entitlement start semantics for users who purchase during an active Trial. Separate `Successful payments total` and `Successful payments current month` metrics count successful payment records from the internal provider-agnostic payments table, using completed Premium `transaction.completed` records with completed timestamps rather than active entitlements, subscription snapshots, raw Paddle webhook payloads, or distinct-user inference. The current-month boundary is UTC calendar month start inclusive to next UTC month start exclusive. These aggregate payment metrics do not expose emails, user IDs, Paddle customer IDs, transaction IDs, raw provider payloads, signatures, or other personal data in the Admin UI. Production/live Paddle readiness remains deferred, and broad public production readiness is not claimed.


## Website CMS home header

- The Website admin area is separate from learner CMS Content and is restricted to the strongest existing Super Admin/bootstrap admin guard.
- Website CMS content is JSON/file based at `site/content/website-content.json`; no database tables or EF Core migrations are used.
- Local publishing targets `site/public` and rewrites `site/public/index.html` with SEO-visible static HTML.
- Production publishing should configure `WebsiteContent:PublicSiteRoot` to `/var/www/languagevoicetutor/site`, the verified public site root for `languagevoicetutor.com`.
