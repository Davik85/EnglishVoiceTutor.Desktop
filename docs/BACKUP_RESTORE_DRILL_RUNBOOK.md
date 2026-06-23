# Phase 4A/4B Backup and Restore Drill Runbook

Review date: 2026-06-23.

This runbook is for production-safe PostgreSQL backup creation, backup readability verification, and restore drills for the Language Voice Tutor / English Voice Tutor backend database. It is written for operators who connect from Windows PowerShell 7 to the Ubuntu server over SSH.

> Status note: Phase 4A's initial production-safe backup/readability/separate-drill-restore was completed on 2026-06-23. Phase 4B local PostgreSQL backup scheduling was installed and activated on production on 2026-06-23 with `languagevoicetutor-postgres-backup.timer` enabled and active. This is not a full disaster recovery plan, not encrypted off-server backup replication, not a production overwrite restore, and not full production permission-fidelity validation.

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
5. Checking restored schema objects, EF migration history, and required tables without exposing secrets.
6. Cleaning up the drill database after verification.
7. Following a pre-migration backup checklist before future schema-dependent backend releases.
8. Following a migration rollback/remediation checklist if a schema-dependent deployment fails.


## What Phase 4B adds

Phase 4B adds repository-managed operator assets for local daily backup scheduling and local retention automation:

- `ops/postgres/backup_lvt_postgres.sh`: a server-side Bash script that creates a timestamped PostgreSQL custom-format backup, verifies the file is non-zero, checks archive readability with `pg_restore --list`, and applies local retention only to safe matching backup filenames.
- `ops/systemd/languagevoicetutor-postgres-backup.service`: a systemd one-shot service template that runs the installed backup script as the `postgres` service account.
- `ops/systemd/languagevoicetutor-postgres-backup.timer`: a daily timer template scheduled for 03:15 server time with `Persistent=true`.
- `tools/install_postgres_backup_schedule_commands.ps1`: a PowerShell command printer for uploading, installing, verifying, disabling, and rolling back the scheduled backup assets.

The schedule must be installed manually by an operator. As of 2026-06-23, production activation has been completed and recorded below; future hosts or rebuilt hosts still require manual operator installation and verification.

Linux shell and systemd assets must use LF line endings. `.gitattributes` enforces LF for server-executed Bash and systemd files because CRLF copied from Windows caused Bash syntax errors on Ubuntu during production installation feedback.

Phase 4B retention is local-server retention only. It is not disaster recovery, not encrypted off-server backup replication, not immutable backup storage, and not a replacement for monitoring or restore drills.

## What Phase 4A does not cover

Phase 4A does not:

- Change backend runtime behavior.
- Add or run EF migrations.
- Change Desktop, Admin UI, billing, Paddle, CMS runtime behavior, or deployment/package behavior.
- Replace production monitoring, alerting, encrypted off-server backup replication, disaster recovery planning, immutable backup storage, or formal retention policy.
- Authorize restoring data into production as part of a drill.
- Authorize sharing secrets or raw production data.
- Prove production permission/grant fidelity when `pg_restore --no-owner --no-acl` is used.
- Complete migration rollback/remediation rehearsal; that remains future work.

## Completed Phase 4A drill record: 2026-06-23

The initial production-safe Phase 4A drill was completed on 2026-06-23 with these verified results:

- Active backend before and after the drill: `/opt/languagevoicetutor/backend/releases/0.1.35-backend.39`.
- `/health` returned `200 OK` before and after the drill.
- `/api/health/database` returned `200 OK` before and after the drill.
- A production PostgreSQL custom-format backup was created successfully at `/var/backups/languagevoicetutor/postgres/lvt_app_db_20260623_045111Z.dump`.
- The backup size shown by `ls` was `3.4M`.
- `pg_restore --list` successfully read the backup, and the restore list count was `245` lines.
- The restore was performed into the separate drill database `lvt_app_db_restore_drill_20260623_045111Z`, not into the production database.
- Required restored table checks passed with `OK` for `__EFMigrationsHistory`, `users`, `subscriptions`, `entitlements`, `plans`, `admin_users`, `admin_user_roles`, and `admin_role_assignment_events`.
- Latest EF migration check confirmed `20260620165657_AddAdminRoleAssignmentPersistence`.
- Drill database cleanup completed, and the final cleanup check returned no rows for `lvt_app_db_restore_drill_20260623_045111Z`.

Boundaries for this completed drill:

- No restore was performed over the production database.
- No production data dump, backup binary contents, SQL dump, secrets, connection strings, `.env` contents, provider payloads, Paddle signatures, or raw user data were committed or pasted into documentation.
- No database migrations were run.
- No backend code or runtime behavior changed.
- No Desktop, Admin UI, CMS runtime, billing, Paddle, migration, package-script, deployment-script, or tool behavior changed.

Ownership/grants nuance: the drill used `pg_restore --no-owner --no-acl`. As expected for this safe drill style, ownership/grant inspection in the restored drill database showed postgres-only ownership/grants. This confirms the restored schema was accessible to the restore operator, but it does not prove production ownership/grant fidelity. That limitation is acceptable for the Phase 4A backup readability/schema restore drill. Phase 4D later completed that permission-fidelity restore check for the current release-readiness level.


## Completed Phase 4B schedule activation record: 2026-06-23

The Phase 4B local PostgreSQL backup schedule was installed and activated on production on 2026-06-23 with these verified results:

- Installed script path: `/opt/languagevoicetutor-ops/postgres/backup_lvt_postgres.sh`. This path is used because the `postgres` account must be able to traverse the script path without gaining access to the backend release/config tree under `/opt/languagevoicetutor`.
- The script was converted from CRLF to LF on the server after Bash reported a CRLF syntax error; repository `.gitattributes` now enforces LF for Linux shell/systemd assets.
- `bash -n` passed after line-ending correction.
- `sudo -u postgres test -x /opt/languagevoicetutor-ops/postgres/backup_lvt_postgres.sh` passed.
- Manual dry-run backup succeeded with backup file `/var/backups/languagevoicetutor/postgres/lvt_app_db_20260623_150447Z.dump`, backup size `3476326` bytes, and `pg_restore --list` line count `245`.
- Manual systemd service run succeeded with `Result=success`, `ExecMainCode=0`, and `ExecMainStatus=0`.
- Service log metadata confirmed backup file `/var/backups/languagevoicetutor/postgres/lvt_app_db_20260623_150541Z.dump`, backup size `3476326` bytes, `pg_restore --list` line count `245`, and retention action `removed 0 old local backup file(s) older than 14 day(s)`.
- Latest backup readability check succeeded with `245` lines from `pg_restore --list`.
- Latest verified backup: `/var/backups/languagevoicetutor/postgres/lvt_app_db_20260623_150541Z.dump`.
- Active timer: `languagevoicetutor-postgres-backup.timer`; it is enabled and `active (waiting)`.
- Next observed trigger: `2026-06-24 03:15 CEST`.
- Production backend remained healthy on `/opt/languagevoicetutor/backend/releases/0.1.35-backend.39`; `/opt/languagevoicetutor/backend/current` pointed to that release, `/health` returned `200 OK`, and `/api/health/database` returned `200 OK`.
- The systemd service sets `WorkingDirectory=/tmp` to avoid `postgres` working-directory warnings or failures when manual/sudo checks originate from a deploy user's home directory.

Phase 4 backup/restore/migration rollback drills are complete for the current release-readiness level. Encrypted off-server backups remain optional future infrastructure hardening.

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


## Phase 4B scheduled local backup install helper

The repository includes a PowerShell helper that prints installation and verification commands for the daily local backup schedule. It prints commands only; it does not execute SSH/SCP/systemctl commands and does not read secrets:

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\install_postgres_backup_schedule_commands.ps1
```

The printed command sequence covers:

1. Uploading `ops/postgres/backup_lvt_postgres.sh` and the systemd unit/timer templates.
2. Setting root-owned script/unit permissions and creating the postgres-owned backup directory.
3. Running the backup script once with `--dry-run` to preview retention cleanup.
4. Running one immediate real backup.
5. Enabling and starting `languagevoicetutor-postgres-backup.timer`.
6. Checking timer status, listing the next scheduled run, and checking recent service logs.
7. Verifying the latest backup with `pg_restore --list` without printing dump contents.
8. Disabling the timer or rolling back installed schedule assets if needed.

The helper warns that backup files remain on the server and must not be copied into git, chat, tickets, screenshots, or unapproved storage.

## Phase 4B systemd schedule

The intended installed paths are:

```text
/opt/languagevoicetutor-ops/postgres/backup_lvt_postgres.sh
/etc/systemd/system/languagevoicetutor-postgres-backup.service
/etc/systemd/system/languagevoicetutor-postgres-backup.timer
```

The timer runs daily at 03:15 server time and uses `Persistent=true`, so systemd can run a missed backup after boot. The unit files do not include secrets, do not include application connection strings, and do not read `/etc/languagevoicetutor/backend.env`.

Verify the installed timer and next scheduled run with:

```powershell
ssh lvt-server "systemctl status languagevoicetutor-postgres-backup.timer --no-pager"
ssh lvt-server "systemctl list-timers languagevoicetutor-postgres-backup.timer --no-pager"
```

Inspect recent backup service logs safely with:

```powershell
ssh lvt-server "journalctl -u languagevoicetutor-postgres-backup.service -n 80 --no-pager"
```

The service output is designed to print only the backup path, byte size, `pg_restore --list` line count, and retention summary. Do not paste large logs into public places if they include operational paths or other sensitive context.

Run an immediate one-off backup safely with:

```powershell
ssh lvt-server "sudo -u postgres /opt/languagevoicetutor-ops/postgres/backup_lvt_postgres.sh"
```

Preview retention cleanup without deleting old local backup files with:

```powershell
ssh lvt-server "sudo -u postgres /opt/languagevoicetutor-ops/postgres/backup_lvt_postgres.sh --dry-run"
```

Disable the schedule without deleting existing backup files with:

```powershell
ssh lvt-server "sudo systemctl disable --now languagevoicetutor-postgres-backup.timer"
```

Rollback installed schedule assets with:

```powershell
ssh lvt-server "sudo systemctl disable --now languagevoicetutor-postgres-backup.timer || true; sudo rm -f /etc/systemd/system/languagevoicetutor-postgres-backup.timer /etc/systemd/system/languagevoicetutor-postgres-backup.service; sudo systemctl daemon-reload; sudo rm -f /opt/languagevoicetutor-ops/postgres/backup_lvt_postgres.sh"
```

Rollback removes installed script/unit/timer assets only. Existing backup files remain on the server for operator-controlled retention or secure deletion.

Verify a created backup with `pg_restore --list` without printing dump contents:

```powershell
ssh lvt-server "cd /tmp && sudo -u postgres pg_restore --list /var/backups/languagevoicetutor/postgres/<backup-file>.dump >/tmp/lvt_pg_restore_list.txt && wc -l /tmp/lvt_pg_restore_list.txt && rm -f /tmp/lvt_pg_restore_list.txt"
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

Inspect table owners and grants without exposing passwords or row data. If the backup and restore were intentionally run with `--no-owner --no-acl`, expect drill ownership/grants to reflect the restore operator rather tha production permissions; record that limitation instead of treating the check as permission-fidelity proof:

```powershell
ssh lvt-server "sudo -u postgres psql -d lvt_app_db_restore_drill_20260622 -v ON_ERROR_STOP=1 -c \"select schemaname, tablename, tableowner from pg_tables where schemaname = 'public' order by tablename;\""
ssh lvt-server "sudo -u postgres psql -d lvt_app_db_restore_drill_20260622 -v ON_ERROR_STOP=1 -c \"select grantee, table_schema, table_name, privilege_type from information_schema.role_table_grants where table_schema = 'public' and table_name in ('users','subscriptions','entitlements','plans','admin_users','admin_user_roles','admin_role_assignment_events') order by table_name, grantee, privilege_type;\""
```

Do not paste full grant output into public places if role names are considered sensitive. Redact as needed. A future permission-fidelity drill can omit `--no-owner --no-acl` only under an approved, carefully scoped procedure that protects production roles and secrets.

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
7. Verify required tables, `__EFMigrationsHistory`, and latest expected migration in the restored drill database. Record ownership/grants only with the correct caveat when `--no-owner --no-acl` is used.
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


## Phase 4C migration rollback/remediation rehearsal assets

Phase 4C documentation and dry-run command-printer assets now exist in `docs/MIGRATION_ROLLBACK_REMEDIATION_RUNBOOK.md` and `tools/migration_rollback_remediation_commands.ps1`. They are operator-readiness assets only: no production mutation, EF migration execution, SQL remediation, restore-over-production, backend runtime change, Desktop change, Admin UI change, CMS change, billing/Paddle change, or deployment/package behavior change has happened as part of adding them.

The Phase 4C runbook keeps code rollback and database remediation separate. A backend symlink rollback can be the safest first response for a bad backend release, but it does not revert schema, migration history, reference data, or user/provider records. Any SQL remediation must be targeted, reviewed separately, and supported by fresh readable PostgreSQL backup evidence. Broad unreviewed SQL remains forbidden.

Current operational baseline remains: Phase 4A backup/readability/separate-drill-restore is completed; Phase 4B local PostgreSQL backup scheduling is active on production via `languagevoicetutor-postgres-backup.timer`; the latest known local backup readability check returned `245` `pg_restore --list` lines; and Contabo VPS Auto Backup is enabled as an additional provider/VPS-level safety layer. Contabo Auto Backup does not replace PostgreSQL `pg_dump`/`pg_restore` backups or restore validation.

Phase 4 is complete for the current release-readiness level: Phase 4A backup/readability/separate restore drill completed, Phase 4B local scheduled PostgreSQL backups active, Phase 4C migration rollback/remediation dry-run completed, and Phase 4D permission-fidelity restore drill completed. Off-server encrypted backups remain optional future infrastructure hardening, not an immediate release blocker. Production/live Paddle readiness and broad public production readiness remain deferred.

## 2026-06-23 Phase 4C dry-run rehearsal related evidence

Phase 4C migration rollback/remediation dry-run rehearsal was completed successfully on 2026-06-23 without production mutation. The verified backend release evidence was current `/opt/languagevoicetutor/backend/releases/0.1.35-backend.39` and previous rollback reference `/opt/languagevoicetutor/backend/releases/0.1.35-backend.38`; `/health` and `/api/health/database` returned `200 OK`; latest readable backup was `/var/backups/languagevoicetutor/postgres/lvt_app_db_20260623_153008Z.dump`; and `pg_restore --list` returned `245` lines. Latest EF migration was `20260620165657_AddAdminRoleAssignmentPersistence`, and required key tables checked `OK` for `__EFMigrationsHistory`, `users`, `subscriptions`, `entitlements`, `plans`, `admin_users`, `admin_user_roles`, and `admin_role_assignment_events`.

`languagevoicetutor-backend.service` was active/enabled. `languagevoicetutor-postgres-backup.timer` was enabled/active with next observed run `2026-06-24 03:15 CEST`. Contabo VPS Auto Backup was manually confirmed enabled as an additional provider/VPS-level layer, but it does not replace PostgreSQL `pg_dump`/`pg_restore` validation. No production DB mutation, EF migration, SQL remediation, restore-over-production, or backend runtime change occurred.

## 2026-06-23 Phase 4D permission-fidelity restore drill evidence

Phase 4D completed successfully on 2026-06-23. An owner/ACL-aware backup of production database `lvt_app_db` was created at `/var/backups/languagevoicetutor/postgres/lvt_app_db_owner_acl_20260623_161611Z.dump`, confirmed non-empty at `3.4M`, and checked with `pg_restore --list` returning `245` lines. The backup was restored into separate drill database `lvt_app_db_owner_acl_drill_20260623_161611Z`; no production restore-over was attempted.

Key production table owners and grants were baselined for `lvt_app`. In the drill database, key tables returned `OK`, latest migration was `20260620165657_AddAdminRoleAssignmentPersistence`, checked table owners were `lvt_app`, and checked `lvt_app` grants matched production. Drill cleanup completed with backend termination returning `0` rows, `dropdb` completing, and the final existence query returning no rows. Production backend remained `/opt/languagevoicetutor/backend/releases/0.1.35-backend.39`, and `/health` plus `/api/health/database` returned `200 OK`. No EF migrations, SQL remediation, runtime/deployment/package, Desktop, Admin UI, CMS, billing, or Paddle behavior changed.
