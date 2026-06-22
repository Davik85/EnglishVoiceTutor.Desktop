param(
    [string]$ServerHost = "lvt-server",
    [string]$ProductionDatabase = "lvt_app_db",
    [string]$BackupDirectory = "/var/backups/languagevoicetutor/postgres",
    [string]$BackupFile = "<backup-file>.dump",
    [string]$DrillDatabase,
    [switch]$IncludeDropDrillDatabaseCommand,
    [switch]$ConfirmDropDrillDatabase
)

$ErrorActionPreference = "Stop"

function Test-SafeName {
    param(
        [Parameter(Mandatory = $true)][string]$Name,
        [Parameter(Mandatory = $true)][string]$Label
    )

    if ($Name -notmatch '^[A-Za-z0-9_./<>-]+$') {
        throw "$Label contains unsupported characters. Use only letters, numbers, underscore, dot, slash, angle brackets, or hyphen."
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

if ([string]::IsNullOrWhiteSpace($DrillDatabase)) {
    $stamp = (Get-Date).ToUniversalTime().ToString("yyyyMMdd_HHmmss")
    $DrillDatabase = "$($ProductionDatabase)_restore_drill_$stamp"
}

Test-SafeName -Name $ServerHost -Label "ServerHost"
Test-SafeName -Name $ProductionDatabase -Label "ProductionDatabase"
Test-SafeName -Name $BackupDirectory -Label "BackupDirectory"
Test-SafeName -Name $BackupFile -Label "BackupFile"
Test-SafeName -Name $DrillDatabase -Label "DrillDatabase"

if ($DrillDatabase -eq $ProductionDatabase) {
    throw "Refusing to use production database '$ProductionDatabase' as the restore drill target. Choose a separate drill database."
}

if ($DrillDatabase -notmatch '_restore_drill_|_drill_') {
    throw "Refusing drill database '$DrillDatabase' because the name does not clearly indicate a drill. Include '_restore_drill_' or '_drill_' in the name."
}

$backupPath = "$BackupDirectory/$BackupFile"
$requiredTablesSql = "select table_schema, table_name from information_schema.tables where table_schema = 'public' and table_name in ('__EFMigrationsHistory','users','subscriptions','entitlements','plans','admin_users','admin_user_roles','admin_role_assignment_events') order by table_name;"
$migrationsSql = "select ""MigrationId"" from ""__EFMigrationsHistory"" order by ""MigrationId"" desc limit 10;"
$ownersSql = "select schemaname, tablename, tableowner from pg_tables where schemaname = 'public' order by tablename;"
$grantsSql = "select grantee, table_schema, table_name, privilege_type from information_schema.role_table_grants where table_schema = 'public' and table_name in ('users','subscriptions','entitlements','plans','admin_users','admin_user_roles','admin_role_assignment_events') order by table_name, grantee, privilege_type;"
$requiredTablesSqlForShell = Convert-ToShellDoubleQuotedArgumentContent -Value $requiredTablesSql
$migrationsSqlForShell = Convert-ToShellDoubleQuotedArgumentContent -Value $migrationsSql
$ownersSqlForShell = Convert-ToShellDoubleQuotedArgumentContent -Value $ownersSql
$grantsSqlForShell = Convert-ToShellDoubleQuotedArgumentContent -Value $grantsSql

$terminateSql = "select pg_terminate_backend(pid) from pg_stat_activity where datname = '$DrillDatabase' and pid <> pg_backend_pid();"
$cleanupCheckSql = "select 1 from pg_database where datname = '$DrillDatabase';"
$terminateSqlForShell = Convert-ToShellDoubleQuotedArgumentContent -Value $terminateSql
$cleanupCheckSqlForShell = Convert-ToShellDoubleQuotedArgumentContent -Value $cleanupCheckSql

Write-Host "Phase 4A production-safe backup/restore drill command printer"
Write-Host "This script prints commands only. It does not execute SSH, pg_dump, pg_restore, psql, createdb, or dropdb."
Write-Host "It does not read or print /etc/languagevoicetutor/backend.env, connection strings, passwords, SQL dumps, or backup contents."
Write-Host "Production database: $ProductionDatabase"
Write-Host "Drill database: $DrillDatabase"

Write-CommandBlock -Title "Server/backend health context" -Commands @(
    ('ssh {0} "readlink -f /opt/languagevoicetutor/backend/current"' -f $ServerHost),
    'Invoke-WebRequest https://api.languagevoicetutor.com/health -UseBasicParsing',
    'Invoke-WebRequest https://api.languagevoicetutor.com/api/health/database -UseBasicParsing'
)

Write-CommandBlock -Title "Create backup directory" -Commands @(
    ('ssh {0} "sudo install -d -o postgres -g postgres -m 0750 {1}"' -f $ServerHost, $BackupDirectory)
)

Write-CommandBlock -Title "Create timestamped production backup" -Commands @(
    ('ssh {0} "set -euo pipefail; ts=$(date -u +%Y%m%d_%H%M%SZ); backup={1}/{2}_${{ts}}.dump; sudo -u postgres pg_dump --format=custom --no-owner --no-acl --file=\"$backup\" {2}; sudo -u postgres test -s \"$backup\"; sudo -u postgres ls -lh \"$backup\""' -f $ServerHost, $BackupDirectory, $ProductionDatabase)
)

Write-CommandBlock -Title "Verify selected backup file exists and is readable" -Commands @(
    ('ssh {0} "sudo -u postgres test -s {1} && sudo -u postgres ls -lh {1}"' -f $ServerHost, $backupPath),
    ('ssh {0} "sudo -u postgres pg_restore --list {1} >/tmp/lvt_pg_restore_list.txt && wc -l /tmp/lvt_pg_restore_list.txt && sed -n ''1,40p'' /tmp/lvt_pg_restore_list.txt && rm -f /tmp/lvt_pg_restore_list.txt"' -f $ServerHost, $backupPath)
)

Write-CommandBlock -Title "Restore drill into separate database" -Commands @(
    ('ssh {0} "sudo -u postgres createdb {1}"' -f $ServerHost, $DrillDatabase),
    ('ssh {0} "sudo -u postgres pg_restore --dbname={1} --clean --if-exists --no-owner --no-acl {2}"' -f $ServerHost, $DrillDatabase, $backupPath)
)

Write-CommandBlock -Title "Verify restored drill database" -Commands @(
    ('ssh {0} "sudo -u postgres psql -d {1} -v ON_ERROR_STOP=1 -c \"{2}\""' -f $ServerHost, $DrillDatabase, $requiredTablesSqlForShell),
    ('ssh {0} "sudo -u postgres psql -d {1} -v ON_ERROR_STOP=1 -c \"{2}\""' -f $ServerHost, $DrillDatabase, $migrationsSqlForShell),
    ('ssh {0} "sudo -u postgres psql -d {1} -v ON_ERROR_STOP=1 -c \"{2}\""' -f $ServerHost, $DrillDatabase, $ownersSqlForShell),
    ('ssh {0} "sudo -u postgres psql -d {1} -v ON_ERROR_STOP=1 -c \"{2}\""' -f $ServerHost, $DrillDatabase, $grantsSqlForShell)
)

if ($IncludeDropDrillDatabaseCommand) {
    if (-not $ConfirmDropDrillDatabase) {
        throw "Refusing to print dropdb commands unless -ConfirmDropDrillDatabase is also supplied."
    }

    Write-CommandBlock -Title "Drop drill database after verification" -Commands @(
        ('ssh {0} "sudo -u postgres psql -d postgres -v ON_ERROR_STOP=1 -c \"{1}\""' -f $ServerHost, $terminateSqlForShell),
        ('ssh {0} "sudo -u postgres dropdb --if-exists {1}"' -f $ServerHost, $DrillDatabase),
        ('ssh {0} "sudo -u postgres psql -d postgres -tAc \"{1}\""' -f $ServerHost, $cleanupCheckSqlForShell)
    )
} else {
    Write-Host ""
    Write-Host "Drop drill database commands are hidden by default. Re-run with -IncludeDropDrillDatabaseCommand -ConfirmDropDrillDatabase after verification."
}
