param(
    [Parameter(Mandatory = $true)]
    [ValidatePattern('^[A-Za-z0-9._-]+$')]
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
$remoteDeployScriptPath = "$remoteUploadDir/deploy-backend-release.sh"
$localTempDeployDir = Join-Path $repoRoot "artifacts/temp/backend-linux-upload/$Version"
$localDeployScriptPath = Join-Path $localTempDeployDir 'deploy-backend-release.sh'
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

if ($RemotePath -notmatch '^/[-A-Za-z0-9._/]+$') {
    throw "RemotePath must be an absolute Linux path containing only letters, digits, slash, dot, dash, and underscore."
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

$remoteExecutablePath = "$remoteReleaseDir/EnglishVoiceTutor.Api"
$remoteCurrentTempLink = "$remoteBase/current.next"

New-Item -ItemType Directory -Force -Path $localTempDeployDir | Out-Null

$deployScriptContent = @"
#!/usr/bin/env bash
set -euo pipefail

version=$(Quote-ForRemoteShell $Version)
remote_base=$(Quote-ForRemoteShell $remoteBase)
upload_dir=$(Quote-ForRemoteShell $remoteUploadDir)
archive_path=$(Quote-ForRemoteShell $remoteArchivePath)
release_dir=$(Quote-ForRemoteShell $remoteReleaseDir)
current_link=$(Quote-ForRemoteShell $remoteCurrentLink)
previous_link=$(Quote-ForRemoteShell $remotePreviousLink)
current_temp_link=$(Quote-ForRemoteShell $remoteCurrentTempLink)
executable_path=$(Quote-ForRemoteShell $remoteExecutablePath)

printf 'Deploying backend version: %s\n' "`$version"
printf 'Upload folder: %s\n' "`$upload_dir"
printf 'Release path: %s\n' "`$release_dir"

if [ ! -f "`$archive_path" ]; then
    printf 'Backend archive is missing: %s\n' "`$archive_path" >&2
    exit 1
fi

previous_target=''
if [ -L "`$current_link" ]; then
    previous_target="`$(readlink -f "`$current_link")"
fi

rm -rf "`$release_dir"
mkdir -p "`$release_dir"
unzip -q "`$archive_path" -d "`$release_dir"

if [ ! -f "`$executable_path" ]; then
    printf 'Backend executable is missing after extraction: %s\n' "`$executable_path" >&2
    exit 1
fi

chmod 755 "`$executable_path"
if [ ! -x "`$executable_path" ]; then
    printf 'Backend executable is not executable after chmod: %s\n' "`$executable_path" >&2
    exit 1
fi

ln -sfn "`$release_dir" "`$current_temp_link"
mv -Tf "`$current_temp_link" "`$current_link"

if [ -n "`$previous_target" ] && [ -d "`$previous_target" ]; then
    ln -sfn "`$previous_target" "`$previous_link"
    printf 'previous=%s\n' "`$previous_target"
else
    rm -f "`$previous_link"
    printf 'previous=<none>\n'
fi

current_target="`$(readlink -f "`$current_link")"
printf 'current=%s\n' "`$current_target"
printf 'release=%s\n' "`$release_dir"
"@

$utf8NoBom = New-Object System.Text.UTF8Encoding($false)
[System.IO.File]::WriteAllText($localDeployScriptPath, $deployScriptContent.Replace("`r`n", "`n"), $utf8NoBom)

Write-Host "Generated deploy script: $localDeployScriptPath"
if ($DryRun) {
    Write-Host "Generated deploy script content:"
    Write-Host $deployScriptContent
}

$mkdirCommand = "mkdir -p $(Quote-ForRemoteShell $remoteUploadDir) $(Quote-ForRemoteShell $remoteReleasesDir)"
Invoke-LoggedCommand -Command @('ssh', '-p', $SshPort.ToString(), $serverTarget, $mkdirCommand)

Invoke-LoggedCommand -Command @('scp', '-P', $SshPort.ToString(), $archivePath, "${serverTarget}:$remoteArchivePath")
Invoke-LoggedCommand -Command @('scp', '-P', $SshPort.ToString(), $localDeployScriptPath, "${serverTarget}:$remoteDeployScriptPath")

$deployCommand = "bash $(Quote-ForRemoteShell $remoteDeployScriptPath)"
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
