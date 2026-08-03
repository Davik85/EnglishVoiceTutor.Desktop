# Migration rollback/remediation runbook

Review date: 2026-06-23.

Scope: Phase 4C operator-readiness documentation and dry-run command planning only. This runbook does not apply SQL, does not run EF migrations, does not restore over production, does not change backend runtime behavior, and does not change Desktop, Admin UI, CMS, billing, Paddle, packaging, upload, or deployment behavior.

## Current baseline to verify before using this runbook

Last known production baseline at the time this Phase 4C asset was added:

- Backend active release: `/opt/languagevoicetutor/backend/releases/0.1.35-backend.39`.
- Backend `current` symlink pointed to `0.1.35-backend.39` at last verification.
- `/health` returned `200 OK` at last verification.
- `/api/health/database` returned `200 OK` at last verification.
- Latest known production migration: `20260620165657_AddAdminRoleAssignmentPersistence`.
- Phase 3 rate limiting is completed and production-verified.
- Phase 4A backup/readability/separate-drill-restore is completed.
- Phase 4B local PostgreSQL backup scheduling is active on production via `languagevoicetutor-postgres-backup.timer`, and the latest known readability check returned `245` `pg_restore --list` lines.
- Contabo VPS Auto Backup is enabled at provider/VPS level as an additional safety layer.
- Controlled live Paddle validation is complete; broader launch readiness remains pending.
- Broad public production readiness is not claimed.
- Phase 4D permission-fidelity restore drill is completed for the current release-readiness level.

Treat these as a historical snapshot. Always verify live state before a future schema-dependent release or incident response.

## 2026-07-28 Google Play claim-table migration record

Migration `20260727045935_AddGooglePlayPurchaseClaims` was applied from the prior production migration `20260723045852_AddAccountAnonymizationExecution` before backend `.134` deployment. A fresh PostgreSQL backup was created first at `/var/backups/languagevoicetutor/postgres/lvt_app_db_20260728_125528Z.dump` (7,253,981 bytes); its `pg_restore --list` readability check returned 287 lines. The reviewed bounded SQL and checksum comparison were completed before execution. The SQL added only `public.google_play_purchase_claims`, its primary key, the unique `PurchaseTokenFingerprint` index, the `UserId` index, and the migration-history entry.

After application, ownership was changed to `lvt_app`, runtime read access was verified, and inherited `lvt_analytics_reader` access was explicitly revoked because this provider-ownership table contains UserId and purchase-token fingerprints. Backend and database health remained good; temporary SQL files were removed. The change is additive, and backend `.133` remains code-compatible with the empty table. It did not alter subscriptions, entitlements, payments, billing events, Paddle data, users, or existing billing tables.

## 2026-08-03 Google Play RTDN and pending-refund migration record

Starting migration `20260727045935_AddGooglePlayPurchaseClaims` advanced to ending migration `20260803052655_AddGooglePlayPendingRefundReviewFoundation` by applying `20260802154345_AddGooglePlayRtdnPersistenceFoundation` and `20260803052655_AddGooglePlayPendingRefundReviewFoundation`. A fresh backup was created first at `/var/backups/languagevoicetutor/postgres/lvt_app_db_20260803_060304Z.dump` (7,754,175 bytes); `pg_restore --list` returned 293 lines. Reviewed temporary SQL was removed after use.

The additive change created `google_play_purchase_token_secrets`, `google_play_rtdn_events`, and `google_play_pending_refund_reviews`. All three are owned by `lvt_app`, runtime table privileges were verified, and `lvt_analytics_reader` has no listed privileges. Each table was empty immediately after migration; database health returned HTTP 200. Backend deployment was separate; `.138` remains schema-compatible as the code rollback target. No existing billing table or entitlement/Paddle data was altered.

## Principles

### Code rollback and database rollback are separate

Backend release rollback changes which application files the `current` symlink points to and restarts `languagevoicetutor-backend.service`. It does not undo schema changes, reference-data changes, data migrations, or provider/customer records already written by the database.

Database rollback or remediation is separate because PostgreSQL state persists independently of deployed backend binaries. A database action must be planned from the exact migration/data-change failure mode, current data shape, and backup state.

### Code rollback is often the safest first action

If a newly deployed backend is unhealthy, throws errors because of an application bug, or is incompatible with traffic, first consider switching the backend symlink back to the previous reviewed release and restarting the service. This can reduce user impact quickly while preserving database state for investigation.

Code rollback is not enough when the new release already applied schema/data changes that older code cannot tolerate. Future schema-dependent releases must therefore include compatibility notes, expected latest migration id, required tables, backup evidence, and an explicit database remediation plan before release.

### Targeted reviewed SQL remediation may be appropriate

Targeted SQL may be appropriate only when all of these are true:

1. The issue is understood and bounded.
2. The SQL is reviewed separately by the owner/operator and, when possible, generated from source-controlled migration knowledge.
3. A fresh custom-format PostgreSQL backup exists and `pg_restore --list` proves it is readable.
4. The SQL avoids dumping raw user data and avoids broad destructive operations.
5. The operator has documented pre-change and post-change evidence.

### Broad unreviewed SQL is forbidden

Do not paste, generate, or run broad unreviewed SQL against production. Forbidden examples include blind `DROP`, blind `DELETE`, blind `UPDATE`, unbounded table rewrites, ad-hoc SQL copied from chat, or SQL that exposes raw user data, secrets, provider payloads, tokens, connection strings, password reset material, private keys, or backup contents.

### Restoring over production is forbidden during rehearsal

A rehearsal must never restore over the production database. Restore drills must use a separate drill database, as in the Phase 4A pattern. Production restore-over must be treated as a real incident-recovery action requiring owner approval, downtime/user-impact planning, fresh backup evidence, and separate rollback notes.

### Backend package/upload scripts do not run EF migrations automatically

Backend package and upload scripts intentionally deploy application files and switch the backend release symlink. They do not run `dotnet ef database update`, do not apply migration SQL, and do not read or print database secrets. EF migrations remain a separate explicit operator action.

## Before any future schema-dependent backend release

1. Identify the release version, previous release, expected migration id, expected tables/columns, and whether older backend code remains compatible with the new schema.
2. Verify the backend `current` symlink and record the active and previous reviewed release paths.
3. Verify `/health` and `/api/health/database`.
4. Verify the latest migration in production using `__EFMigrationsHistory` without dumping table data.
5. Verify required key tables exist with `information_schema.tables`; do not select raw application rows.
6. Confirm `languagevoicetutor-postgres-backup.timer` is enabled/active and note its last/next run.
7. Confirm a fresh local PostgreSQL custom-format backup exists and is readable with `pg_restore --list`.
8. Confirm Contabo VPS Auto Backup remains enabled in the provider console, but do not treat it as a substitute for PostgreSQL `pg_dump`/`pg_restore` validation.
9. Prepare a code rollback command template for switching `current` to the previous reviewed release and restarting `languagevoicetutor-backend.service`.
10. Prepare a migration-specific remediation note. If SQL might be needed, keep it reviewed, bounded, redacted, and separate from the code rollback command.
11. Define evidence to collect before and after the action.

## Safe verification commands

Use `tools/migration_rollback_remediation_commands.ps1` to print commands. By default it prints only; it does not SSH, query production, mutate symlinks, restart services, run EF migrations, apply SQL, restore backups, delete backups, read `/etc/languagevoicetutor/backend.env`, or print secrets.

The printed command sections cover:

- Backend symlink check.
- Backend health and database health checks.
- Local PostgreSQL backup discovery.
- Backup readability with `pg_restore --list`.
- Latest `__EFMigrationsHistory` migration id.
- Required key table existence checks.
- Backend service status.
- Current/previous backend release path display.
- Manual code rollback command template.
- Redacted incident evidence collection.
- Local scheduled backup timer status.
- Manual provider/VPS Auto Backup status note.

## Evidence to collect

Collect only redacted operational evidence:

- UTC timestamp, operator, incident/release id, and decision owner.
- Active backend symlink target before and after.
- Previous reviewed release path.
- `systemctl status languagevoicetutor-backend.service --no-pager` summary.
- HTTP status for `/health` and `/api/health/database` before and after.
- Latest migration id from `__EFMigrationsHistory`.
- Required table existence count/list from `information_schema.tables`.
- Latest backup filename, size, timestamp, and `pg_restore --list` line count.
- `languagevoicetutor-postgres-backup.timer` enabled/active status, last run, and next run.
- Manual confirmation that Contabo VPS Auto Backup is enabled.
- Exact reviewed code rollback command used, if any.
- Exact reviewed SQL remediation file reference, if any, without pasting secrets or raw data.
- User-visible impact summary and follow-up tasks.

Do not include secrets, passwords, connection strings, `.env` contents, SQL dumps, backup binary contents, Paddle signatures, private keys, raw provider payloads, JWTs, refresh tokens, password reset codes, or raw user data.

## Remediation decision guide

- If new backend code is faulty but database state is still compatible with the previous release, prefer code rollback first.
- If the database is healthy and the issue is an endpoint/runtime regression, prefer code rollback and investigation over SQL.
- If a migration partially failed before application traffic resumed, stop and preserve evidence; decide between forward-fix migration, targeted reviewed SQL, or incident restore planning.
- If production data was corrupted, do not improvise. Preserve logs/evidence, confirm backups, decide on downtime/user communication, and use an owner-approved incident plan.
- If a restore is required, rehearse against a separate database first when time allows; restoring over production is not part of Phase 4C rehearsal.

## Phase 4C completed dry-run rehearsal evidence (2026-06-23)

Completed on 2026-06-23: the Phase 4C migration rollback/remediation dry-run rehearsal was performed successfully as a read-only production verification. No production database mutation occurred, no EF migrations were run, no SQL remediation was applied, no restore-over-production was attempted, and no backend runtime behavior changed. Desktop, Admin, CMS, billing, and Paddle behavior were unchanged.

Verified production evidence:

- Backend `current` symlink target: `/opt/languagevoicetutor/backend/releases/0.1.35-backend.39`.
- Previous rollback reference: `/opt/languagevoicetutor/backend/releases/0.1.35-backend.38`.
- `https://api.languagevoicetutor.com/health` returned `200 OK`.
- `https://api.languagevoicetutor.com/api/health/database` returned `200 OK`.
- Latest local PostgreSQL backups were listed successfully.
- Latest readable backup: `/var/backups/languagevoicetutor/postgres/lvt_app_db_20260623_153008Z.dump`.
- `pg_restore --list` readability line count for that backup: `245`.
- Latest EF migration confirmed from production: `20260620165657_AddAdminRoleAssignmentPersistence`.
- Required key tables check passed with `OK` for `__EFMigrationsHistory`, `users`, `subscriptions`, `entitlements`, `plans`, `admin_users`, `admin_user_roles`, and `admin_role_assignment_events`.
- `languagevoicetutor-backend.service` was active and enabled.
- `languagevoicetutor-postgres-backup.timer` was enabled and active; next observed run was `2026-06-24 03:15 CEST`.
- Contabo VPS Auto Backup was manually confirmed as enabled in the provider control panel as a provider/VPS-level safety layer. It is not a substitute for PostgreSQL `pg_dump`/`pg_restore` backup validation.

This rehearsal confirms operator readiness evidence only. It does not claim broad public production readiness, and production/live Paddle readiness remains deferred. Phase 4D later confirmed permission-fidelity restore behavior for the current release-readiness level.

## 2026-07-21 account-deletion migration process

Database migration and backend deployment are separate operations. Generated SQL under `artifacts/` is temporary operator output and must not be committed. For a known production starting point, use this bounded process:

1. Query `__EFMigrationsHistory` and confirm the exact latest production migration.
2. Generate a short SQL script from the known previous migration to the intended new migration, rather than a broad historical script.
3. Inspect the generated SQL before upload.
4. Apply only the reviewed SQL with `psql` and `ON_ERROR_STOP=1`; do not put a database password in command arguments.
5. Verify the new `__EFMigrationsHistory` entry and the intended database object.
6. Remove the temporary SQL file locally and remotely.
7. Check public `/api/health/database`.
8. Deploy and verify the backend separately through the normal backend release flow.

For the account-deletion constraint, the confirmed bounded range was from `20260717151432_AddUserFeedbackReportReplies` to `20260721120000_AddActiveAccountDeletionRequestConstraint`. The result adds only partial unique index `IX_user_feedback_reports_ActiveAccountDeletionRequest_UserId`. Because it creates no table or sequence, no additional table grants or ownership changes were required.

Do not use the failed broad idempotent script for this known range. That first attempt included historical migrations and failed because an older historical raw SQL migration produced invalid generated PostgreSQL control-flow syntax. Preserve the historical migrations; do not rewrite them to work around a broad script when the exact safe from/to range is known.

## Deferred work

- Optional off-server encrypted PostgreSQL backup strategy as future infrastructure hardening, not an immediate release blocker.
- Repeat permission-fidelity restore drills for future material schema/security changes when risk warrants them.
- Broader incident-response automation and monitoring dashboards.
- Production/live Paddle readiness.
- Broad public production readiness.

## Phase 4 current release-readiness completion note

As of 2026-06-23, Phase 4 is complete for the current release-readiness level: Phase 4A backup/readability/separate restore drill completed; Phase 4B local scheduled PostgreSQL backups active; Phase 4C migration rollback/remediation dry-run completed; and Phase 4D permission-fidelity restore drill completed. The Phase 4D owner/ACL-aware backup restored into a separate drill database, checked key table owners/grants against the production baseline, cleaned up the drill database, and left production backend `0.1.35-backend.39` healthy. Off-server encrypted backups remain optional future infrastructure hardening.

## 2026-07-01 Admin auth audit migration evidence

Migration `20260701000000_AddAdminAuthAuditEvents` was applied in production before backend `0.1.35-backend.94`, after a fresh backup and SQL review. Safe backup evidence only: `/var/backups/languagevoicetutor/postgres/lvt_app_db_20260701_154405Z.dump`, size `6.4M`, and `pg_restore --list` line count `245`. Production verification confirmed `admin_auth_audit_events` exists, its owner was corrected to `lvt_app`, and `lvt_app` has table privileges. Admin Activity includes the `admin_auth_audit_events` source and shows verified `admin_login_success` and `admin_logout` events. `admin_login_failed`, `disabled_admin_login_denied`, and session expiration audit persistence remain pending until separately verified or implemented. Controlled Paddle live payment validation remains pending.
