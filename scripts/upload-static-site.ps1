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

    [ValidateRange(1, 65535)]
    [int]$SshPort = 22,

    [switch]$DryRun
)

$ErrorActionPreference = "Stop"

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = (Resolve-Path (Join-Path $scriptRoot "..")).Path
$siteSource = Join-Path $repoRoot "site\public"

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

if (-not (Test-Path $siteSource -PathType Container)) {
    throw "Static site source folder was not found: $siteSource"
}

$localFiles = @(Get-ChildItem -Path $siteSource -File -Recurse | Sort-Object -Property FullName)
if ($localFiles.Count -eq 0) {
    throw "Static site source folder does not contain files: $siteSource"
}

$remoteTarget = "$ServerUser@$ServerHost`:$RemotePath/"

Write-Host "Static site upload summary"
Write-Host "Local source: $siteSource"
Write-Host "Remote target: $remoteTarget"
Write-Host "SSH port: $SshPort"
Write-Host "Dry run: $([bool]$DryRun)"
Write-Host "Scope: recursively uploads site/public files, including site/public/assets, to the static website folder without touching releases/windows/direct."
Write-Host "Release files: not touched. Backend deployment: not touched."
Write-Host "Files:"
foreach ($localFile in $localFiles) {
    $relativePath = [System.IO.Path]::GetRelativePath($siteSource, $localFile.FullName)
    Write-Host (" - {0} ({1} bytes)" -f $relativePath, $localFile.Length)
}

$sshTarget = "$ServerUser@$ServerHost"
$mkdirCommand = @("ssh", "-p", [string]$SshPort, "--", $sshTarget, "mkdir", "-p", $RemotePath)
Invoke-LoggedCommand -Arguments $mkdirCommand

foreach ($localFile in $localFiles) {
    $relativeDirectory = [System.IO.Path]::GetRelativePath($siteSource, $localFile.DirectoryName)
    $normalizedRelativeDirectory = if ($relativeDirectory -eq ".") { "" } else { ($relativeDirectory -replace "\\", "/") }
    $remoteDirectory = if ([string]::IsNullOrWhiteSpace($normalizedRelativeDirectory)) { $RemotePath } else { "$RemotePath/$normalizedRelativeDirectory" }
    $remoteFileTarget = "$ServerUser@$ServerHost`:$remoteDirectory/"
    Invoke-LoggedCommand -Arguments @("ssh", "-p", [string]$SshPort, "--", $sshTarget, "mkdir", "-p", $remoteDirectory)
    Invoke-LoggedCommand -Arguments @("scp", "-P", [string]$SshPort, "--", $localFile.FullName, $remoteFileTarget)
}

if ($DryRun) {
    Write-Host "Dry run completed. No files were uploaded."
}
else {
    Write-Host "Static site upload completed. Verify the public page over HTTPS before sharing it."
}
