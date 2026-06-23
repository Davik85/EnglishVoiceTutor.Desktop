param(
    [string]$ServerHost = "lvt-server",
    [string]$ProductionDatabase = "lvt_app_db",
    [string]$BackupDirectory = "/var/backups/languagevoicetutor/postgres",
    [string]$CurrentReleasePath = "/opt/languagevoicetutor/backend/releases/0.1.35-backend.39",
    [string]$PreviousReleasePath = "/opt/languagevoicetutor/backend/releases/0.1.35-backend.38"
)

$ErrorActionPreference = "Stop"

function Test-SafeArgument {
    param(
        [Parameter(Mandatory = $true)][string]$Value,
        [Parameter(Mandatory = $true)][string]$Label
    )

    if ($Value -notmatch '^[A-Za-z0-9_./:-]+$') {
        throw "$Label contains unsupported characters. Use only letters, numbers, underscore, dot, slash, colon, or hyphen."
    }
}

function Convert-ToShellDoubleQuotedArgumentContent {
    param([Parameter(Mandatory = $true)][string]$Value)
    return $Value.Replace('\', '\\').Replace('"', '\"')
}

function Write-CommandBlock {
    param(
        [Parameter(Mandatory = $true)][string]$Title,
        [Parameter(Mandatory = $true)][string[]]$Commands
    )

    Write-Host ""
    Write-Host "## $Title"
    foreach ($command in $Commands) {
        Write-Host $command
    }
}

Test-SafeArgument -Value $ServerHost -Label "ServerHost"
Test-SafeArgument -Value $ProductionDatabase -Label "ProductionDatabase"
Test-SafeArgument -Value $BackupDirectory -Label "BackupDirectory"
Test-SafeArgument -Value $CurrentReleasePath -Label "CurrentReleasePath"
Test-SafeArgument -Value $PreviousReleasePath -Label "PreviousReleasePath"

$latestMigrationSql = 'select "MigrationId" from "__EFMigrationsHistory" order by "MigrationId" desc limit 1;'
$keyTablesSql = "select table_name from information_schema.tables where table_schema = 'public' and table_name in ('__EFMigrationsHistory','users','subscriptions','entitlements','plans','admin_users','admin_user_roles','admin_role_assignment_events') order by table_name;"
$redactedEvidenceSql = 'select now() at time zone ''utc'' as checked_at_utc;'
$latestMigrationSqlForShell = Convert-ToShellDoubleQuotedArgumentContent -Value $latestMigrationSql
$keyTablesSqlForShell = Convert-ToShellDoubleQuotedArgumentContent -Value $keyTablesSql
$redactedEvidenceSqlForShell = Convert-ToShellDoubleQuotedArgumentContent -Value $redactedEvidenceSql

Write-Host "Phase 4C migration rollback/remediation command printer"
Write-Host "This helper prints commands only by default. It does not execute SSH, psql, pg_restore, systemctl, EF migrations, SQL remediation, restore, delete, or symlink changes."
Write-Host "It does not read /etc/languagevoicetutor/backend.env and must not be used to print connection strings, passwords, SQL dumps, backup contents, raw table data, secrets, or provider payloads."
Write-Host "Production database label: $ProductionDatabase"
Write-Host "Expected current release path: $CurrentReleasePath"
Write-Host "Previous reviewed release path: $PreviousReleasePath"

Write-CommandBlock -Title "Current backend symlink check" -Commands @(
    ('ssh {0} "readlink -f /opt/languagevoicetutor/backend/current"' -f $ServerHost),
    ('ssh {0} "readlink /opt/languagevoicetutor/backend/previous || true"' -f $ServerHost)
)

Write-CommandBlock -Title "Backend health and database health checks" -Commands @(
    'Invoke-WebRequest https://api.languagevoicetutor.com/health -UseBasicParsing',
    'Invoke-WebRequest https://api.languagevoicetutor.com/api/health/database -UseBasicParsing',
    'curl -fsS -o /dev/null -w "%{http_code}`n" https://api.languagevoicetutor.com/health',
    'curl -fsS -o /dev/null -w "%{http_code}`n" https://api.languagevoicetutor.com/api/health/database'
)

Write-CommandBlock -Title "Latest local PostgreSQL backup discovery" -Commands @(
    ('ssh {0} "sudo -u postgres find {1} -maxdepth 1 -type f -name ''*.dump'' -printf ''%T@ %TY-%Tm-%TdT%TH:%TM:%TSZ %s %p`n'' | sort -nr | head -5"' -f $ServerHost, $BackupDirectory)
)

Write-CommandBlock -Title "Latest local PostgreSQL backup readability check" -Commands @(
    ('ssh {0} "set -euo pipefail; backup=$(sudo -u postgres find {1} -maxdepth 1 -type f -name ''*.dump'' -printf ''%T@ %p`n'' | sort -nr | head -1 | cut -d'' '' -f2-); test -n \"$backup\"; sudo -u postgres pg_restore --list \"$backup\" >/tmp/lvt_phase4c_pg_restore_list.txt; wc -l /tmp/lvt_phase4c_pg_restore_list.txt; rm -f /tmp/lvt_phase4c_pg_restore_list.txt; sudo -u postgres ls -lh \"$backup\""' -f $ServerHost, $BackupDirectory)
)

Write-CommandBlock -Title "Production __EFMigrationsHistory latest migration check" -Commands @(
    ('ssh {0} "sudo -u postgres psql -d {1} -v ON_ERROR_STOP=1 -c \"{2}\""' -f $ServerHost, $ProductionDatabase, $latestMigrationSqlForShell)
)

Write-CommandBlock -Title "Required key table existence check" -Commands @(
    ('ssh {0} "sudo -u postgres psql -d {1} -v ON_ERROR_STOP=1 -c \"{2}\""' -f $ServerHost, $ProductionDatabase, $keyTablesSqlForShell)
)

Write-CommandBlock -Title "Backend service status check" -Commands @(
    ('ssh {0} "systemctl is-active languagevoicetutor-backend.service; systemctl is-enabled languagevoicetutor-backend.service; systemctl status languagevoicetutor-backend.service --no-pager"' -f $ServerHost)
)

Write-CommandBlock -Title "Current and previous backend release path display" -Commands @(
    ('ssh {0} "test -d {1} && echo current-reviewed-release={1}; test -d {2} && echo previous-reviewed-release={2}; readlink -f /opt/languagevoicetutor/backend/current"' -f $ServerHost, $CurrentReleasePath, $PreviousReleasePath)
)

Write-CommandBlock -Title "Manual code rollback command template" -Commands @(
    'WARNING: Code rollback only switches backend files and restarts the service. It does not revert database schema, migrations, reference data, or user/provider records.',
    ('ssh {0} "set -euo pipefail; test -d {1}; sudo ln -sfn {1} /opt/languagevoicetutor/backend/current.rollback-candidate; sudo mv -Tf /opt/languagevoicetutor/backend/current.rollback-candidate /opt/languagevoicetutor/backend/current; sudo systemctl restart languagevoicetutor-backend.service; readlink -f /opt/languagevoicetutor/backend/current; systemctl status languagevoicetutor-backend.service --no-pager"' -f $ServerHost, $PreviousReleasePath)
)

Write-CommandBlock -Title "SQL remediation warning" -Commands @(
    'WARNING: SQL remediation must be reviewed separately, bounded to the incident, and never generated or applied blindly.',
    'WARNING: Do not run EF migrations, do not apply SQL, and do not restore over production from this helper output.'
)

Write-CommandBlock -Title "Collect redacted evidence for incident notes" -Commands @(
    ('ssh {0} "date -u +%Y-%m-%dT%H:%M:%SZ; readlink -f /opt/languagevoicetutor/backend/current; systemctl is-active languagevoicetutor-backend.service; sudo -u postgres psql -d {1} -v ON_ERROR_STOP=1 -c \"{2}\""' -f $ServerHost, $ProductionDatabase, $redactedEvidenceSqlForShell),
    'Invoke-WebRequest https://api.languagevoicetutor.com/health -UseBasicParsing',
    'Invoke-WebRequest https://api.languagevoicetutor.com/api/health/database -UseBasicParsing',
    'Record operator, UTC time, incident/release id, decision owner, health status codes, latest migration id, key table existence, latest backup filename/size/list-line-count, action taken, and follow-up tasks. Redact secrets and raw user/provider data.'
)

Write-CommandBlock -Title "Local scheduled DB backup timer status" -Commands @(
    ('ssh {0} "systemctl is-enabled languagevoicetutor-postgres-backup.timer; systemctl is-active languagevoicetutor-postgres-backup.timer; systemctl list-timers languagevoicetutor-postgres-backup.timer --no-pager; systemctl status languagevoicetutor-postgres-backup.service --no-pager || true"' -f $ServerHost)
)

Write-CommandBlock -Title "Provider/VPS-level Auto Backup manual note" -Commands @(
    'Manually confirm in the Contabo control panel that VPS Auto Backup is enabled. Do not call provider APIs from this helper and do not treat provider snapshots as a substitute for PostgreSQL pg_dump/pg_restore validation.'
)
