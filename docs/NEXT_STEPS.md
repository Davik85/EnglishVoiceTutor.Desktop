# Next Steps

Review date: 2026-06-22.

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

The live public tester manifest baseline must be checked from `latest.json`, not from this document. Latest built/manual-check snapshot: `v0.1.36-tester.24` installer was built and manually checked for controlled sandbox billing validation. The live public tester manifest baseline must still be checked from `latest.json` before handoff because the website manifest remains the public source of truth; current verified uploaded tester release snapshot: `0.1.36-tester.24`.

This is still a private tester/direct Windows release, not broad public production readiness.

## Public distribution direction

Current controlled tester/direct Windows releases continue to use the existing Inno Setup installer flow. The owner-preferred direction for an eventual full public release is Microsoft Store + MSIX, but that work is deferred until the project is fully release-ready. Do not change the current packaging scripts, upload scripts, `latest.json` format, or release validation behavior as part of this planning note. A later public-release planning pass should add a separate Microsoft Store/MSIX readiness checklist before any Store submission or MSIX packaging work begins.


## Latest verified release summary

Clean-machine smoke passed; small screen/tablet visual smoke passed; the localized Welcome Russian/French fix passed; the admin roles/permissions policy and UI policy tests passed; the desktop release gate passed; and backend `0.1.35-backend.39` is deployed and healthy after the Admin RBAC persistence migration. CMS/Admin published snapshot runtime validation passed for controlled tester lessons, and Save draft + Publish changes are visible in newly started desktop lessons.

## Immediate next steps

1. Hand off `0.1.36-tester.24` only through the controlled tester/direct Windows channel after verifying live `latest.json`; keep it clearly labeled as controlled tester/sandbox billing validation, not broad production/live billing readiness.
2. Collect controlled tester feedback and keep non-blocking feedback in triage.
3. Validate update-over-existing-install from an older `EnglishVoiceTutor.Desktop.*` installed build if that path has not already been recorded for this exact tester handoff.
4. Only then decide the next smallest safe CMS/Admin or scenario/avatar behavior step.

Do not move billing/Paddle production readiness into the immediate next step. Continue sandbox checkout/cancel-renewal validation and Desktop billing UI hardening first; production/live billing readiness remains deferred.

## Release-readiness roadmap

### Phase 2. Production Admin RBAC

- Current state: implemented and production-deployed on backend `0.1.35-backend.39` with Admin RBAC persistence tables, two verified persistent `super_admin` accounts, actor mapping, cutover status endpoint, read-only Admin UI cutover status, and release-gated static validation.
- Completed on 2026-06-22: controlled fallback cutover rehearsal and rollback/restoration drill. Both approved `super_admin` accounts passed `tools/smoke_admin_rbac_cutover_validation.ps1` with fallback enabled, then with fallback temporarily disabled, then again after fallback was restored. AdminPermission read endpoints and role-management read endpoints returned `200` in the enabled and disabled phases.
- Completed on 2026-06-22: permanent production fallback disable. Production `backend.env` now sets `AdminAuthorization__EnableBootstrapAdminFallbackForAdminPermissionPolicies=false`, the backend service was restarted successfully, health/database health returned `200 OK`, persistent role authorization is enabled and verified, two persistent `super_admin` accounts are verified, and both approved accounts passed validation with fallback disabled. Rollback remains available by setting the fallback flag to `true` and restarting the backend.
- Required before public RC: validation that non-owner roles behave correctly, critical-change approval, and the other release blockers listed below.

### Phase 3. Rate limiting / abuse protection

- Completed and production-verified on backend `0.1.35-backend.39` with `RateLimiting__Enabled=true`.
- Coverage includes all Phase 3 slices: auth login/register/password reset; audio/STT, TTS speech and speech stream, translation, and realtime voice start-rate protection; Admin read/write/role-management throttling; billing checkout/cancel-renewal, Paddle checkout launch, and Paddle webhook throttling; plus auth refresh/revoke/current-user/password-change, authenticated lesson start, lesson hint/feedback, authenticated persisted lesson messages, and learner/subscription/status/trial/access-style endpoints where implemented by the final slice.
- Current limiter storage is single-instance/in-memory. True distributed/shared limiter storage remains future work before multi-instance scale-out, and true concurrent realtime voice WebSocket connection caps remain future work if not implemented.
- Phase 3 did not change Admin RBAC authorization behavior, product/free usage semantics, Premium entitlement semantics, billing/Paddle semantics, Paddle webhook signature verification, provider-event handling, Desktop behavior, Admin UI behavior, CMS runtime behavior, deployment scripts, or EF migrations.
- Production still has `AdminAuthorization__EnableBootstrapAdminFallbackForAdminPermissionPolicies=false`; Admin RBAC smoke passed with `fallbackEnabled=False`, `persistentRoleAuthorizationEnabled=True`, and `actorMappingFound=True`.
- No database migrations were added or run for Phase 3, and backend package/upload scripts still do not run EF migrations automatically.

### Phase 4. Backups / restore / migration rollback drills

- Completed on 2026-06-23: initial Phase 4A production-safe backup/readability/separate-drill-restore. Backup `/var/backups/languagevoicetutor/postgres/lvt_app_db_20260623_045111Z.dump` was created, `ls` showed `3.4M`, `pg_restore --list` succeeded with `245` lines, and the backup was restored into separate drill database `lvt_app_db_restore_drill_20260623_045111Z` rather than production. Required key tables were present, latest migration `20260620165657_AddAdminRoleAssignmentPersistence` was confirmed, drill DB cleanup completed, and production `/health` plus `/api/health/database` remained `200 OK`.
- The completed drill did not restore over production, did not commit or paste production data dumps, did not run database migrations, did not change backend code/runtime behavior, and did not change deployment/package scripts.
- The drill used `pg_restore --no-owner --no-acl`; restored drill DB ownership/grants are therefore not full production permission-fidelity proof, which is acceptable for Phase 4A.
- Future work remains: formal backup schedule/retention automation, encrypted off-server backups, optional permission-fidelity restore drills, and migration rollback/remediation rehearsals before broader release-readiness claims.
- Before any future schema-dependent backend release, operators must create a fresh PostgreSQL custom-format production backup, verify it with `pg_restore --list`, and run/record a separate drill-database restore when migration risk warrants it. Restore drills must never target the production database, and backend package/upload scripts still do not run EF migrations automatically.
- Keep production secrets, connection strings, `.env` contents, SQL dumps, backup files, and raw user data out of chat, docs, terminal transcripts intended for sharing, and git.

### Phase 5. Monitoring / logging / privacy hardening

- Cover health checks, service logs, error visibility, and alerts.
- Confirm PII/secrets redaction rules.
- Avoid logging tokens, passwords, secrets, raw provider payloads, raw connection strings, or private keys.
- Review EF/SQL logging level for production privacy before broad public release.

### Phase 6. Paddle live readiness + legal/support blockers

- Verify live Paddle product/price mapping, live webhook setup and verification, and live checkout flow.
- Define cancellation/refund/support path, support contact, and operational runbook.
- Complete terms, privacy, refund, and subscription disclosures.
- Desktop/Admin UI must not call Paddle directly; the backend remains the source of truth for entitlements.

### Phase 7. Microsoft Store + MSIX

- Treat Store/MSIX as future public distribution work.
- Before implementation, verify current official Microsoft Store/MSIX requirements.
- Keep current Inno/direct tester distribution valid until the owner explicitly changes the release channel.
- Do not replace the current installer flow in this task.

## Subscription base plan deployment note

- Treat active `free` and `premium` plan rows as required database reference data.
- Missing plan rows break subscriptions and entitlements through FK constraints.
- EF migration `20260618090000_SeedBaseSubscriptionPlans` idempotently seeds/upserts those rows and is now recorded in production `__EFMigrationsHistory`; operators should still apply future migrations explicitly during backend deployment validation.
- Do not add manual SQL as a recurring deployment requirement.
- Keep free/trial/Premium status backend-owned, with Premium determined by entitlements rather than Desktop local state or Paddle directly.
- Provider-event paid Premium should continue to stack after active trial/Premium access; future-start provider-event Premium must not count as `premiumActive` until `StartsAtUtc`, and active trial should remain the current access source until trial expiry.
- Production/live Paddle readiness remains deferred.

## Current backend verification

Current state: last known production backend snapshot is `/opt/languagevoicetutor/backend/releases/0.1.35-backend.39` active via `/opt/languagevoicetutor/backend/current`; verify the live value from the server symlink before calling it current.

Previous backend release for rollback reference: `/opt/languagevoicetutor/backend/releases/0.1.35-backend.38`. Backend `0.1.35-backend.39` contains the current-user cancel-renewal endpoint, Paddle cancel-at-period-end adapter support, subscription status fields for Desktop Account billing UI decisions, and a cancel request path that must not directly revoke entitlements. `https://api.languagevoicetutor.com/health` and `https://api.languagevoicetutor.com/api/health/database` return `200 OK`. EF migrations through `20260620165657_AddAdminRoleAssignmentPersistence` are recorded in production `__EFMigrationsHistory`.

Deployed runtime status diagnostics are visible on backend `0.1.35-backend.39` from the server `/admin` page and protected runtime-status endpoint. The current server diagnostic is clean and confirms learner runtime uses CMS published snapshot: `effectiveSource=CmsPublishedSnapshot`, `validationSuccess=true`, `fallbackUsed=false`, no errors, no warnings, and `tutorBehaviorProfiles=3`. The tutor behavior profile mismatch was fixed by validating the approved tutor ids `david`, `elena`, and `nelli` instead of an obsolete exact count of 2. The next steps are intentionally small: collect controlled tester feedback, triage known non-blocking issues, and only then choose the next smallest safe CMS/Admin or scenario/avatar behavior step.

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

- Production/live billing/Paddle readiness remains deferred; current billing work is controlled tester/sandbox validation only.
- Desktop billing UI follow-ups remain: Premium-active free lesson label should show unlimited/no daily free limit, Buy/Cancel/Refresh and confirmation strings need full localization, cancellation result messages need clearer localized UX states, and cancel-renewal should be tested end-to-end against Paddle sandbox.
- Referral/promo logic remains future work.
- Production Admin RBAC cutover rehearsal and rollback/restoration passed on 2026-06-22, and the later permanent fallback disable also passed on 2026-06-22. BootstrapAdmin fallback for `AdminPermission:*` policies is now disabled in production; rollback remains setting the fallback flag to `true` and restarting the backend. Non-owner role validation and critical-change approval remain deferred.
- Code signing remains deferred for the controlled tester/direct release and is not a blocker for the already completed controlled tester handoff if unsigned distribution is accepted knowingly. Before a public release candidate or broad public distribution, require Windows installer signing or a documented owner-approved exception, and add signing verification to the release validation/upload gate.
- Broader public release readiness remains deferred until after controlled tester feedback and operational hardening.

## CMS runtime status validation path

The Admin CMS now exposes a read-only **Runtime content status** section and the protected endpoint `GET /api/admin/dev/cms/runtime-status`. Use it to confirm the effective learner content source, validation result, counts, published snapshot metadata, and fallback state without exposing content bodies or secrets.

CMS published snapshot is the active runtime source. The diagnostic confirms runtime source and fallback state. Runtime status is clean on backend `0.1.35-backend.39` with approved tutor-id validation for `david`, `elena`, and `nelli`. Normal status should show `effectiveSource=CmsPublishedSnapshot`, `validationSuccess=true`, `fallbackUsed=false`, no errors, and no warnings. Rollback remains disabling CMS runtime flags and restarting backend so runtime returns to static JSON. Billing/Paddle is not involved.

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
- Verify webhook-driven entitlement activation, paid Premium scheduling after trial, future-start `premiumActive=false` behavior until `StartsAtUtc`, and cancel-at-period-end subscription snapshots before any production/live Paddle launch decision.
- Do not add refund/reversal handling or Paddle customer portal flows until those backend-owned lifecycle policies are explicitly designed.


## Phase 3 rate limiting completion state — 2026-06-23

Phase 3 is implemented and production-verified on backend `0.1.35-backend.39` with `RateLimiting__Enabled=true`. Coverage includes the completed auth, learner/session, audio/voice/translation, Admin, billing, and Paddle webhook slices documented above. No Desktop, Admin UI, Admin RBAC authorization, BootstrapAdmin fallback, billing/Paddle semantics, CMS runtime content, product/free-usage counter, Premium/free entitlement, deployment-script, package-script, or database-migration change is included in Phase 3. Remaining work is operational: distributed/shared limiter storage before multi-instance scale-out, true concurrent realtime voice WebSocket connection caps if still not implemented, formal backup schedule/retention automation, off-server encrypted backups, optional permission-fidelity restore checks, migration rollback/remediation drills, monitoring/privacy hardening, Paddle live readiness, legal/support blockers, and Microsoft Store/MSIX. Broad public-production readiness is not claimed.
