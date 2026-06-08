param(
    [Parameter(Mandatory = $true)]
    [ValidatePattern('^[0-9]+\.[0-9]+\.[0-9]+(?:[-+][0-9A-Za-z][0-9A-Za-z.-]*)?$')]
    [string]$Version,

    [Parameter(Mandatory = $true)]
    [string]$ServerHost,

    [Parameter(Mandatory = $true)]
    [string]$ServerUser,

    [Parameter(Mandatory = $true)]
    [string]$RemotePath,

    [int]$SshPort = 22,

    [switch]$DryRun,

    [switch]$RestartService
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
$archiveName = "LanguageVoiceTutor.Backend-linux-x64-$Version.zip"
$archivePath = Join-Path $repoRoot "artifacts/packages/backend/$archiveName"
$remoteBase = $RemotePath.TrimEnd('/')
$remoteUploadDir = "$remoteBase/uploads/$Version"
$remoteReleasesDir = "$remoteBase/releases"
$remoteReleaseDir = "$remoteReleasesDir/$Version"
$remoteCurrentLink = "$remoteBase/current"
$remoteArchivePath = "$remoteUploadDir/$archiveName"
$serverTarget = "$ServerUser@$ServerHost"
$serviceName = 'languagevoicetutor-backend.service'

function Invoke-LoggedCommand {
    param(
        [Parameter(Mandatory = $true)]
        [string[]]$Command
    )

    Write-Host "> $($Command -join ' ')"
    if (-not $DryRun) {
        $executable = $Command[0]
        $arguments = $Command[1..($Command.Length - 1)]
        & $executable @arguments
        if ($LASTEXITCODE -ne 0) {
            throw "Command failed with exit code $LASTEXITCODE: $($Command -join ' ')"
        }
    }
}

function Quote-ForRemoteShell {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Value
    )

    return "'" + $Value.Replace("'", "'\''") + "'"
}

if (-not (Test-Path -LiteralPath $archivePath)) {
    throw "Backend archive was not found. Create it first with scripts/package-backend-linux-release.ps1 -Version $Version. Expected: $archivePath"
}

if ($SshPort -lt 1 -or $SshPort -gt 65535) {
    throw "SshPort must be between 1 and 65535."
}

Write-Host "Preparing backend upload for Language Voice Tutor $Version."
Write-Host "Local archive: $archivePath"
Write-Host "Server: $serverTarget"
Write-Host "Remote release: $remoteReleaseDir"
Write-Host "Current symlink: $remoteCurrentLink"
if ($DryRun) {
    Write-Host "Dry-run mode: commands will be printed but not executed."
}

$mkdirCommand = "mkdir -p $(Quote-ForRemoteShell $remoteUploadDir) $(Quote-ForRemoteShell $remoteReleasesDir)"
Invoke-LoggedCommand -Command @('ssh', '-p', $SshPort.ToString(), $serverTarget, $mkdirCommand)

Invoke-LoggedCommand -Command @('scp', '-P', $SshPort.ToString(), $archivePath, "${serverTarget}:$remoteArchivePath")

$deployCommand = @(
    "set -e",
    "rm -rf $(Quote-ForRemoteShell $remoteReleaseDir)",
    "mkdir -p $(Quote-ForRemoteShell $remoteReleaseDir)",
    "unzip -q $(Quote-ForRemoteShell $remoteArchivePath) -d $(Quote-ForRemoteShell $remoteReleaseDir)",
    "chmod +x $(Quote-ForRemoteShell "$remoteReleaseDir/EnglishVoiceTutor.Api") || true",
    "ln -sfn $(Quote-ForRemoteShell $remoteReleaseDir) $(Quote-ForRemoteShell $remoteCurrentLink)"
) -join ' && '
Invoke-LoggedCommand -Command @('ssh', '-p', $SshPort.ToString(), $serverTarget, $deployCommand)

if ($RestartService) {
    Invoke-LoggedCommand -Command @('ssh', '-p', $SshPort.ToString(), $serverTarget, "sudo systemctl restart $serviceName")
}

Write-Host "Backend upload flow completed."
Write-Host "Release folder: $remoteReleaseDir"
Write-Host "Current symlink: $remoteCurrentLink -> $remoteReleaseDir"
Write-Host "Next manual server commands:"
Write-Host "  sudo systemctl daemon-reload"
Write-Host "  sudo systemctl start $serviceName"
Write-Host "  sudo systemctl status $serviceName --no-pager"
Write-Host "  journalctl -u $serviceName -n 100 --no-pager"
Write-Host "This script does not write secrets and does not run EF migrations."
