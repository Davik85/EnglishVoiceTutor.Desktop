param(
    [Parameter(Mandatory = $true)]
    [ValidatePattern('^[0-9]+\.[0-9]+\.[0-9]+(?:[-+][0-9A-Za-z][0-9A-Za-z.-]*)?$')]
    [string]$Version,

    [string]$ServerHost = 'lvt-server',

    [string]$ServerUser = 'deploy',

    [string]$RemotePath = '/opt/languagevoicetutor/backend',

    [int]$SshPort = 22,

    [switch]$DryRun,

    [switch]$PackageFirst,

    [switch]$RestartService,

    [switch]$NoRestart
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
$remotePreviousLink = "$remoteBase/previous"
$remoteArchivePath = "$remoteUploadDir/$archiveName"
$serverTarget = "$ServerUser@$ServerHost"
$serviceName = 'languagevoicetutor-backend.service'
$shouldRestartService = -not $NoRestart -or $RestartService

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
            throw ("Command failed with exit code {0}: {1}" -f $LASTEXITCODE, ($Command -join ' '))
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

if ($SshPort -lt 1 -or $SshPort -gt 65535) {
    throw "SshPort must be between 1 and 65535."
}

if ($PackageFirst) {
    $packageScript = Join-Path $PSScriptRoot 'package-backend-linux-release.ps1'
    Write-Host "Creating backend linux-x64 package first."
    Write-Host "> powershell -ExecutionPolicy Bypass -File $packageScript -Version $Version"
    & powershell -ExecutionPolicy Bypass -File $packageScript -Version $Version
    if ($LASTEXITCODE -ne 0) {
        throw "Backend package script failed with exit code $LASTEXITCODE."
    }
}

if (-not (Test-Path -LiteralPath $archivePath)) {
    throw "Backend archive was not found. Create it first with scripts/package-backend-linux-release.ps1 -Version $Version, or pass -PackageFirst. Expected: $archivePath"
}

Write-Host "Preparing backend upload for Language Voice Tutor $Version."
Write-Host "Local archive: $archivePath"
Write-Host "Server: $serverTarget"
Write-Host "Remote release: $remoteReleaseDir"
Write-Host "Current symlink: $remoteCurrentLink"
Write-Host "Previous symlink: $remotePreviousLink"
Write-Host "Service: $serviceName"
Write-Host "Restart service: $shouldRestartService"
if ($DryRun) {
    Write-Host "Dry-run mode: commands will be printed but not executed."
}

$mkdirCommand = "mkdir -p $(Quote-ForRemoteShell $remoteUploadDir) $(Quote-ForRemoteShell $remoteReleasesDir)"
Invoke-LoggedCommand -Command @('ssh', '-p', $SshPort.ToString(), $serverTarget, $mkdirCommand)

Invoke-LoggedCommand -Command @('scp', '-P', $SshPort.ToString(), $archivePath, "${serverTarget}:$remoteArchivePath")

$remoteExecutablePath = "$remoteReleaseDir/EnglishVoiceTutor.Api"
$remoteCurrentTempLink = "$remoteBase/current.next"
$remoteDeployScript = @(
    "set -euo pipefail",
    "previous_target=\"\"",
    "if [ -L $(Quote-ForRemoteShell $remoteCurrentLink) ]; then previous_target=\$(readlink -f $(Quote-ForRemoteShell $remoteCurrentLink)); fi",
    "rm -rf $(Quote-ForRemoteShell $remoteReleaseDir)",
    "mkdir -p $(Quote-ForRemoteShell $remoteReleaseDir)",
    "unzip -q $(Quote-ForRemoteShell $remoteArchivePath) -d $(Quote-ForRemoteShell $remoteReleaseDir)",
    "test -f $(Quote-ForRemoteShell $remoteExecutablePath)",
    "chmod 755 $(Quote-ForRemoteShell $remoteExecutablePath)",
    "test -x $(Quote-ForRemoteShell $remoteExecutablePath)",
    "ln -sfn $(Quote-ForRemoteShell $remoteReleaseDir) $(Quote-ForRemoteShell $remoteCurrentTempLink)",
    "mv -Tf $(Quote-ForRemoteShell $remoteCurrentTempLink) $(Quote-ForRemoteShell $remoteCurrentLink)",
    "if [ -n \"\$previous_target\" ] && [ -d \"\$previous_target\" ]; then ln -sfn \"\$previous_target\" $(Quote-ForRemoteShell $remotePreviousLink); fi",
    "readlink -f $(Quote-ForRemoteShell $remoteCurrentLink)",
    "if [ -L $(Quote-ForRemoteShell $remotePreviousLink) ]; then printf 'previous=%s\\n' \$(readlink -f $(Quote-ForRemoteShell $remotePreviousLink)); fi"
) -join ' && '
$deployCommand = "bash -lc $(Quote-ForRemoteShell $remoteDeployScript)"
Invoke-LoggedCommand -Command @('ssh', '-p', $SshPort.ToString(), $serverTarget, $deployCommand)

if ($shouldRestartService) {
    Invoke-LoggedCommand -Command @('ssh', '-p', $SshPort.ToString(), $serverTarget, "sudo systemctl restart $serviceName")
    Invoke-LoggedCommand -Command @('ssh', '-p', $SshPort.ToString(), $serverTarget, "sudo systemctl status $serviceName --no-pager")
}
else {
    Write-Host "Service restart skipped because -NoRestart was provided."
}

Write-Host "Backend upload flow completed."
Write-Host "Release folder: $remoteReleaseDir"
Write-Host "Current symlink: $remoteCurrentLink -> $remoteReleaseDir"
Write-Host "Previous symlink: $remotePreviousLink (if an older current release existed)"
Write-Host "This script does not write secrets and does not run EF migrations. Apply reviewed migrations separately."
