# Phase 4A Backup and Restore Drill Runbook

Review date: 2026-06-23.

This runbook is for production-safe PostgreSQL backup creation, backup readability verification, and restore drills for the Language Voice Tutor / English Voice Tutor backend database. It is written for operators who connect from Windows PowerShell 7 to the Ubuntu server over SSH.

> Status note: this Phase 4A toolkit/runbook is prepared operator tooling only. It does not prove that a production backup/restore drill has been executed. As of the post-Phase-3 documentation update, no production backup/restore drill is recorded here as completed. Phase 4A is the next operational phase after Phase 3 rate limiting / abuse-protection documentation is accurate.

## Safety rules

- Never restore over the production database during a drill.
- Never paste connection strings, `.env` files, passwords, SQL dumps, backup files, private keys, tokens, cookies, provider payloads, Paddle signatures, or raw user data exports into chat, issue trackers, documentation, or commits.
- Never read or print `/etc/languagevoicetutor/backend.env` as part of a drill report.
- Never use a production database name as the restore target. Restore drills must use a separate drill database.
- Never commit generated backup files, SQL dumps, migration SQL artifacts, or terminal transcripts containing secrets.
- Do not run production backup or restore commands from automated coding agents. Operators run the commands only during an approved maintenance or verification window.
- Backup scripts do not replace production monitoring, alerting, log review, or database health checks.
- Backend package/upload scripts do not run EF migrations automatically. Database migrations remain explicit operator actions.
- A pre-migration backup is required before future schema-dependent backend releases.

## What Phase 4A covers

Phase 4A provides an operator toolkit for:

1. Creating a PostgreSQL custom-format backup of the production database.
2. Confirming the backup file exists and has non-zero size.
3. Verifying the backup archive is readable with `pg_restore --list`.
4. Restoring the backup into a separate local/server-side drill database.
5. Checking restored schema objects, EF migration history, required tables, ownership, and grants without exposing secrets.
6. Cleaning up the drill database after verification.
7. Following a pre-migration backup checklist before future schema-dependent backend releases.
8. Following a migration rollback/remediation checklist if a schema-dependent deployment fails.

## What Phase 4A does not cover

Phase 4A does not:

- Change backend runtime behavior.
- Add or run EF migrations.
- Change Desktop, Admin UI, billing, Paddle, CMS runtime behavior, or deployment/package behavior.
- Replace production monitoring, alerting, off-server backup replication, disaster recovery planning, or formal retention policy.
- Authorize restoring data into production as part of a drill.
- Authorize sharing secrets or raw production data.

## Server paths and naming convention

Recommended server backup directory:

```text
/var/backups/languagevoicetutor/postgres
```

Recommended backup filename pattern:

```text
lvt_app_db_YYYYMMDD_HHMMSSZ.dump
```

Example:

```text
/var/backups/languagevoicetutor/postgres/lvt_app_db_20260622_193000Z.dump
```

Use PostgreSQL custom format (`pg_dump --format=custom`) so that `pg_restore --list` and controlled restores are available.

## Retention recommendation

Until a formal retention policy is approved, keep at least:

- 7 daily backups,
- 4 weekly backups,
- 3 monthly backups,
- and the most recent pre-migration backup for every schema-dependent backend release until that release is fully verified and a later safe backup exists.

Prefer encrypted off-server copies for disaster recovery. Do not store backup files in git, chat, tickets, desktop screenshots, or public cloud locations that are not approved for production data.

## Helper script

The repository includes a local PowerShell helper that prints commands without reading secrets:

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\db_backup_restore_drill_commands.ps1
```

The helper prints SSH/server commands for backup creation, readability checks, restore drill checks, and cleanup. It does not read `/etc/languagevoicetutor/backend.env`, does not print connection strings, and refuses restore targets that match the production database name.

To print commands for a specific drill database name:

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\db_backup_restore_drill_commands.ps1 `
  -DrillDatabase lvt_app_db_restore_drill_20260622
```

To print the drill database drop command, require explicit confirmation:

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\db_backup_restore_drill_commands.ps1 `
  -DrillDatabase lvt_app_db_restore_drill_20260622 `
  -IncludeDropDrillDatabaseCommand `
  -ConfirmDropDrillDatabase
```

## Production backup creation

Run from Windows PowerShell after confirming the SSH host alias points to the intended server. These commands create a backup directory and a timestamped custom-format backup on the server. They avoid printing database passwords and do not read the backend environment file.

```powershell
ssh lvt-server "sudo install -d -o postgres -g postgres -m 0750 /var/backups/languagevoicetutor/postgres"
ssh lvt-server "set -euo pipefail; ts=\$(date -u +%Y%m%d_%H%M%SZ); backup=/var/backups/languagevoicetutor/postgres/lvt_app_db_\${ts}.dump; sudo -u postgres pg_dump --format=custom --no-owner --no-acl --file=\"\$backup\" lvt_app_db; sudo -u postgres test -s \"\$backup\"; sudo -u postgres ls -lh \"\$backup\""
```

If the production database is not reachable by local peer/admin access for the `postgres` system user, stop and use an approved credential-handling procedure that lets PostgreSQL prompt securely. Do not echo passwords, `PGPASSWORD`, database URLs, or raw connection strings into terminal history.

## Backup file verification

Verify the backup file exists, is non-zero, and can be read by `pg_restore --list`:

```powershell
ssh lvt-server "sudo -u postgres test -s /var/backups/languagevoicetutor/postgres/<backup-file>.dump && sudo -u postgres ls -lh /var/backups/languagevoicetutor/postgres/<backup-file>.dump"
ssh lvt-server "sudo -u postgres pg_restore --list /var/backups/languagevoicetutor/postgres/<backup-file>.dump >/tmp/lvt_pg_restore_list.txt && wc -l /tmp/lvt_pg_restore_list.txt && sed -n '1,40p' /tmp/lvt_pg_restore_list.txt && rm -f /tmp/lvt_pg_restore_list.txt"
```

The list output is metadata, not table data. Still treat it as operationally sensitive and do not paste long output into public places.

## Restore drill into a separate database

Use a separate drill database name. The drill database must not be `lvt_app_db`.

Example drill database name:

```text
lvt_app_db_restore_drill_20260622
```

Create and restore into the drill database:

```powershell
ssh lvt-server "sudo -u postgres createdb lvt_app_db_restore_drill_20260622"
ssh lvt-server "sudo -u postgres pg_restore --dbname=lvt_app_db_restore_drill_20260622 --clean --if-exists --no-owner --no-acl /var/backups/languagevoicetutor/postgres/<backup-file>.dump"
```

If `createdb` fails because the drill database already exists, stop and confirm it is an old drill database before dropping or reusing it. Never run a restore drill against the production database.

## Required checks after restore

### Current backend release symlink

Confirm the backend release that is active while the backup was created or while the drill is documented:

```powershell
ssh lvt-server "readlink -f /opt/languagevoicetutor/backend/current"
```

### Backend and database health

Check public health endpoints without credentials:

```powershell
Invoke-WebRequest https://api.languagevoicetutor.com/health -UseBasicParsing
Invoke-WebRequest https://api.languagevoicetutor.com/api/health/database -UseBasicParsing
```

### Restored schema and required tables

Check key restored tables without dumping row contents:

```powershell
ssh lvt-server "sudo -u postgres psql -d lvt_app_db_restore_drill_20260622 -v ON_ERROR_STOP=1 -c \"select table_schema, table_name from information_schema.tables where table_schema = 'public' and table_name in ('__EFMigrationsHistory','users','subscriptions','entitlements','plans','admin_users','admin_user_roles','admin_role_assignment_events') order by table_name;\""
```

Expected key tables:

- `__EFMigrationsHistory`
- `users`
- `subscriptions`
- `entitlements`
- `plans`
- `admin_users`
- `admin_user_roles`
- `admin_role_assignment_events`

If any expected table is missing from the restored drill database, treat the backup or migration state as failed until investigated.

### EF migration history

Check the most recent migration id without dumping application data:

```powershell
ssh lvt-server "sudo -u postgres psql -d lvt_app_db_restore_drill_20260622 -v ON_ERROR_STOP=1 -c \"select \"\"MigrationId\"\" from \"\"__EFMigrationsHistory\"\" order by \"\"MigrationId\"\" desc limit 10;\""
```

For the current known production state, the latest expected migration is:

```text
20260620165657_AddAdminRoleAssignmentPersistence
```

Future schema-dependent releases must update the expected migration id in the release checklist after reviewing the new migration.

### Ownership and grants

Inspect table owners and grants without exposing passwords or row data:

```powershell
ssh lvt-server "sudo -u postgres psql -d lvt_app_db_restore_drill_20260622 -v ON_ERROR_STOP=1 -c \"select schemaname, tablename, tableowner from pg_tables where schemaname = 'public' order by tablename;\""
ssh lvt-server "sudo -u postgres psql -d lvt_app_db_restore_drill_20260622 -v ON_ERROR_STOP=1 -c \"select grantee, table_schema, table_name, privilege_type from information_schema.role_table_grants where table_schema = 'public' and table_name in ('users','subscriptions','entitlements','plans','admin_users','admin_user_roles','admin_role_assignment_events') order by table_name, grantee, privilege_type;\""
```

Do not paste full grant output into public places if role names are considered sensitive. Redact as needed.

## Drill database cleanup

After verification, drop only the drill database. First terminate any connections to the drill database, then drop it. Replace the database name with the approved drill database name. Never use `lvt_app_db` here.

```powershell
ssh lvt-server "sudo -u postgres psql -d postgres -v ON_ERROR_STOP=1 -c \"select pg_terminate_backend(pid) from pg_stat_activity where datname = 'lvt_app_db_restore_drill_20260622' and pid <> pg_backend_pid();\""
ssh lvt-server "sudo -u postgres dropdb --if-exists lvt_app_db_restore_drill_20260622"
```

Confirm cleanup:

```powershell
ssh lvt-server "sudo -u postgres psql -d postgres -tAc \"select 1 from pg_database where datname = 'lvt_app_db_restore_drill_20260622';\""
```

The confirmation query should return no rows.

## Pre-migration backup checklist

Before any future schema-dependent backend release:

1. Verify the current backend symlink:
   ```powershell
   ssh lvt-server "readlink -f /opt/languagevoicetutor/backend/current"
   ```
2. Verify backend and database health:
   ```powershell
   Invoke-WebRequest https://api.languagevoicetutor.com/health -UseBasicParsing
   Invoke-WebRequest https://api.languagevoicetutor.com/api/health/database -UseBasicParsing
   ```
3. Create a fresh custom-format production backup.
4. Confirm the backup file exists and has non-zero size.
5. Run `pg_restore --list` against the backup.
6. Run a restore drill into a separate drill database when the release materially depends on schema/data changes or when the migration risk is not trivial.
7. Verify required tables, `__EFMigrationsHistory`, latest expected migration, ownership, and grants in the restored drill database.
8. Review generated migration SQL before applying it to production.
9. Confirm rollback code version and compatibility before switching `current`.
10. Record only redacted command results in the release notes.

## Migration failure remediation checklist

If a schema-dependent deploy fails or the backend reports missing relation/table errors:

1. Do not run broad unreviewed SQL in production.
2. Capture redacted symptoms: endpoint status, service status, and relevant log lines without secrets or raw user data.
3. Roll back code when the active backend is incompatible with the current production schema and a previous compatible release exists.
4. Keep database migrations separate from code rollback unless a reviewed remediation plan explicitly requires SQL changes.
5. Generate or select targeted reviewed migration SQL for the missing schema change.
6. Confirm a recent readable backup exists before applying SQL.
7. Apply only the reviewed targeted SQL needed for the intended migration/remediation.
8. Verify `__EFMigrationsHistory`, required tables, ownership, grants, backend health, and database health.
9. Switch code forward again only after schema verification passes.
10. Document the incident with secrets redacted.

## When to roll back code

Roll back code when:

- The new backend expects tables/columns that do not exist yet.
- The new backend produces repeated `500` errors due to schema mismatch.
- The previous backend is known to be compatible with the current schema.
- Applying the migration immediately is unsafe, unreviewed, or blocked.

Use the existing release symlink rollback procedure from the backend deployment runbook and restart the service after the symlink switch.

## When to apply targeted reviewed SQL

Apply targeted reviewed SQL when:

- The intended migration is known and reviewed.
- A current backup exists and `pg_restore --list` can read it.
- The SQL scope is limited to the intended migration/remediation.
- The operator can verify `__EFMigrationsHistory`, required tables, ownership, and grants afterward.

Prefer EF-generated migration SQL reviewed by a human. Avoid manually invented production SQL unless it is the safest reviewed remediation path.

## What not to do

- Do not restore over `lvt_app_db` during a drill.
- Do not paste production secrets, connection strings, `.env` contents, SQL dumps, backup binaries, private keys, tokens, cookies, provider payloads, Paddle signatures, or raw user data into chat or git.
- Do not run EF migrations from backend package/upload scripts.
- Do not treat backup creation alone as a restore test.
- Do not keep drill databases longer than needed.
- Do not use production drill commands against an unknown SSH host.
- Do not apply unreviewed broad SQL to production.
