param(
    [string]$ServerHost = "lvt-server",
    [string]$LocalOpsDirectory = "ops",
    [string]$RemoteRoot = "/opt/languagevoicetutor",
    [string]$BackupDirectory = "/var/backups/languagevoicetutor/postgres",
    [string]$ServiceName = "languagevoicetutor-postgres-backup.service",
    [string]$TimerName = "languagevoicetutor-postgres-backup.timer"
)

$ErrorActionPreference = "Stop"

function Test-SafeValue {
    param(
        [Parameter(Mandatory = $true)][string]$Value,
        [Parameter(Mandatory = $true)][string]$Label
    )

    if ($Value -notmatch '^[A-Za-z0-9_./:-]+$') {
        throw "$Label contains unsupported characters. Use only letters, numbers, underscore, dot, slash, colon, or hyphen."
    }
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

Test-SafeValue -Value $ServerHost -Label "ServerHost"
Test-SafeValue -Value $LocalOpsDirectory -Label "LocalOpsDirectory"
Test-SafeValue -Value $RemoteRoot -Label "RemoteRoot"
Test-SafeValue -Value $BackupDirectory -Label "BackupDirectory"
Test-SafeValue -Value $ServiceName -Label "ServiceName"
Test-SafeValue -Value $TimerName -Label "TimerName"

$remoteOps = "$RemoteRoot/ops"
$remoteScript = "$remoteOps/postgres/backup_lvt_postgres.sh"
$remoteSystemd = "$remoteOps/systemd"

Write-Host "Phase 4B PostgreSQL local backup schedule command printer"
Write-Host "This helper prints commands only. It does not execute SSH, SCP, systemctl, pg_dump, or pg_restore."
Write-Host "It does not read or print backend.env, passwords, connection strings, SQL dumps, backup contents, or raw user data."
Write-Host "WARNING: Backup files remain on the server under $BackupDirectory. Do not copy backup files into git, chat, tickets, screenshots, or unapproved storage."

Write-CommandBlock -Title "Upload repository-managed operator assets" -Commands @(
    ('ssh {0} "sudo install -d -o root -g root -m 0755 {1}/postgres {1}/systemd"' -f $ServerHost, $remoteOps),
    ('scp {0}/postgres/backup_lvt_postgres.sh {1}:/tmp/backup_lvt_postgres.sh' -f $LocalOpsDirectory, $ServerHost),
    ('scp {0}/systemd/{1} {2}:/tmp/{1}' -f $LocalOpsDirectory, $ServiceName, $ServerHost),
    ('scp {0}/systemd/{1} {2}:/tmp/{1}' -f $LocalOpsDirectory, $TimerName, $ServerHost),
    ('ssh {0} "sudo install -o root -g root -m 0755 /tmp/backup_lvt_postgres.sh {1} && sudo install -o root -g root -m 0644 /tmp/{2} /etc/systemd/system/{2} && sudo install -o root -g root -m 0644 /tmp/{3} /etc/systemd/system/{3} && sudo rm -f /tmp/backup_lvt_postgres.sh /tmp/{2} /tmp/{3}"' -f $ServerHost, $remoteScript, $ServiceName, $TimerName)
)

Write-CommandBlock -Title "Prepare backup directory" -Commands @(
    ('ssh {0} "sudo install -d -o postgres -g postgres -m 0750 {1}"' -f $ServerHost, $BackupDirectory)
)

Write-CommandBlock -Title "Run backup script once in dry-run retention mode" -Commands @(
    ('ssh {0} "sudo -u postgres {1} --dry-run"' -f $ServerHost, $remoteScript)
)

Write-CommandBlock -Title "Run one immediate backup for real" -Commands @(
    ('ssh {0} "sudo -u postgres {1}"' -f $ServerHost, $remoteScript)
)

Write-CommandBlock -Title "Enable and start the daily systemd timer" -Commands @(
    ('ssh {0} "sudo systemctl daemon-reload && sudo systemctl enable --now {1}"' -f $ServerHost, $TimerName)
)

Write-CommandBlock -Title "Verify timer and schedule" -Commands @(
    ('ssh {0} "systemctl status {1} --no-pager"' -f $ServerHost, $TimerName),
    ('ssh {0} "systemctl list-timers {1} --no-pager"' -f $ServerHost, $TimerName),
    ('ssh {0} "systemctl status {1} --no-pager"' -f $ServerHost, $ServiceName)
)

Write-CommandBlock -Title "Inspect recent logs safely" -Commands @(
    ('ssh {0} "journalctl -u {1} -n 80 --no-pager"' -f $ServerHost, $ServiceName)
)

Write-CommandBlock -Title "Verify latest backup readability without printing dump contents" -Commands @(
    ('ssh {0} "set -euo pipefail; latest=$(sudo -u postgres find {1} -maxdepth 1 -type f -name ''lvt_app_db_*.dump'' -printf ''%T@ %p\n'' | sort -n | tail -1 | cut -d'' '' -f2-); test -n \"$latest\"; sudo -u postgres test -s \"$latest\"; tmp=$(mktemp /tmp/lvt_pg_restore_list.XXXXXX); sudo -u postgres pg_restore --list \"$latest\" >\"$tmp\"; wc -l \"$tmp\"; rm -f \"$tmp\"; printf ''Latest backup verified: %s\n'' \"$latest\""' -f $ServerHost, $BackupDirectory)
)

Write-CommandBlock -Title "Disable scheduled backups without deleting existing backup files" -Commands @(
    ('ssh {0} "sudo systemctl disable --now {1}"' -f $ServerHost, $TimerName)
)

Write-CommandBlock -Title "Rollback/remove installed schedule assets" -Commands @(
    ('ssh {0} "sudo systemctl disable --now {1} || true; sudo rm -f /etc/systemd/system/{1} /etc/systemd/system/{2}; sudo systemctl daemon-reload; sudo rm -f {3}"' -f $ServerHost, $TimerName, $ServiceName, $remoteScript)
)

Write-Host ""
Write-Host "Rollback note: the rollback command removes installed script/unit/timer assets only. Existing backup files under $BackupDirectory are intentionally left in place for operator-controlled retention or secure deletion."
