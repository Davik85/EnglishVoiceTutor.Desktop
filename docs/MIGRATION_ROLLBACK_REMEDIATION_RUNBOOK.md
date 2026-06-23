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
- Production/live Paddle readiness remains deferred.
- Broad public production readiness is not claimed.

Treat these as a historical snapshot. Always verify live state before a future schema-dependent release or incident response.

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

## Deferred work

- Off-server encrypted PostgreSQL backup strategy.
- Permission-fidelity restore drill that validates production-like ownership/grants rather than only `--no-owner --no-acl` readability and separate restore.
- Executed migration rollback/remediation rehearsal on a non-production or explicitly approved drill target.
- Broader incident-response automation and monitoring dashboards.
- Production/live Paddle readiness.
- Broad public production readiness.
