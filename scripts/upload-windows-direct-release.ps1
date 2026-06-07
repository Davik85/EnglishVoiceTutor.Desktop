param(
    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string]$ServerHost,

    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string]$ServerUser,

    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string]$RemotePath,

    [ValidateNotNullOrEmpty()]
    [string]$ReleaseDirectory,

    [ValidateRange(1, 65535)]
    [int]$SshPort = 22,

    [switch]$DryRun
)

$ErrorActionPreference = "Stop"

$requiredManifestFiles = @(
    "latest.json",
    "changelog.json",
    "known-issues.json",
    "checksums.sha256"
)

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = (Resolve-Path (Join-Path $scriptRoot "..")).Path
if (-not $ReleaseDirectory) {
    $ReleaseDirectory = Join-Path $repoRoot "artifacts\releases\windows\direct"
}

function Assert-SafeSimpleValue {
    param(
        [Parameter(Mandatory = $true)][string]$Name,
        [Parameter(Mandatory = $true)][string]$Value,
        [Parameter(Mandatory = $true)][string]$Pattern
    )

    if ($Value -notmatch $Pattern) {
        throw "$Name contains unsupported characters for this upload helper. Use plain SSH-safe host, user, and path values."
    }
}

function Format-CommandPreview {
    param([Parameter(Mandatory = $true)][string[]]$Arguments)

    return ($Arguments | ForEach-Object {
        if ($_ -match '[\s"'']') {
            '"' + ($_ -replace '"', '\"') + '"'
        }
        else {
            $_
        }
    }) -join ' '
}

function Invoke-LoggedCommand {
    param([Parameter(Mandatory = $true)][string[]]$Arguments)

    Write-Host (Format-CommandPreview -Arguments $Arguments)
    if ($DryRun) {
        return
    }

    $command = $Arguments[0]
    $commandArguments = @($Arguments | Select-Object -Skip 1)
    & $command @commandArguments
    if ($LASTEXITCODE -ne 0) {
        throw "$command failed with exit code $LASTEXITCODE."
    }
}

Assert-SafeSimpleValue -Name "ServerHost" -Value $ServerHost -Pattern '^[A-Za-z0-9._-]+$'
Assert-SafeSimpleValue -Name "ServerUser" -Value $ServerUser -Pattern '^[A-Za-z0-9._-]+$'
Assert-SafeSimpleValue -Name "RemotePath" -Value $RemotePath -Pattern '^/[A-Za-z0-9._~/-]+$'

$validationScript = Join-Path $scriptRoot "validate-windows-direct-release.ps1"
if (-not (Test-Path $validationScript -PathType Leaf)) {
    throw "Validation script was not found: $validationScript"
}

Write-Host "Validating local Windows direct release before upload..."
& $validationScript -ReleaseDirectory $ReleaseDirectory
if ($LASTEXITCODE -ne 0) {
    throw "Windows direct release validation failed. Upload stopped."
}

$resolvedReleaseDirectory = (Resolve-Path $ReleaseDirectory).Path
$latestPath = Join-Path $resolvedReleaseDirectory "latest.json"
$latest = Get-Content -Path $latestPath -Raw | ConvertFrom-Json
$installerFileName = [string]$latest.installerFileName
if ([string]::IsNullOrWhiteSpace($installerFileName)) {
    throw "latest.json does not contain installerFileName."
}

$localFiles = @($requiredManifestFiles + $installerFileName) | ForEach-Object { Join-Path $resolvedReleaseDirectory $_ }
foreach ($localFile in $localFiles) {
    if (-not (Test-Path $localFile -PathType Leaf)) {
        throw "Expected upload file is missing: $localFile"
    }
}

$remoteTarget = "$ServerUser@$ServerHost`:$RemotePath/"

Write-Host "Windows direct release upload summary"
Write-Host "Local release directory: $resolvedReleaseDirectory"
Write-Host "Remote target: $remoteTarget"
Write-Host "SSH port: $SshPort"
Write-Host "Dry run: $([bool]$DryRun)"
Write-Host "Files:"
foreach ($localFile in $localFiles) {
    $item = Get-Item -Path $localFile
    Write-Host (" - {0} ({1} bytes)" -f $item.Name, $item.Length)
}

$sshTarget = "$ServerUser@$ServerHost"
$mkdirCommand = @("ssh", "-p", [string]$SshPort, "--", $sshTarget, "mkdir", "-p", $RemotePath)
Invoke-LoggedCommand -Arguments $mkdirCommand

$scpCommand = @("scp", "-P", [string]$SshPort, "--") + @($localFiles) + @($remoteTarget)
Invoke-LoggedCommand -Arguments $scpCommand

if ($DryRun) {
    Write-Host "Dry run completed. No files were uploaded."
}
else {
    Write-Host "Upload completed. Verify remote checksums before sharing public URLs."
}
