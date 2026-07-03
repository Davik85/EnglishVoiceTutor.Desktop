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

function Get-RelativePathFromSiteRoot {
    param([Parameter(Mandatory = $true)][string]$Path)

    $sourceWithSeparator = $siteSource.TrimEnd([System.IO.Path]::DirectorySeparatorChar, [System.IO.Path]::AltDirectorySeparatorChar) + [System.IO.Path]::DirectorySeparatorChar
    return $Path.Substring($sourceWithSeparator.Length).Replace([System.IO.Path]::DirectorySeparatorChar, '/').Replace([System.IO.Path]::AltDirectorySeparatorChar, '/')
}

Assert-SafeSimpleValue -Name "ServerHost" -Value $ServerHost -Pattern '^[A-Za-z0-9._-]+$'
Assert-SafeSimpleValue -Name "ServerUser" -Value $ServerUser -Pattern '^[A-Za-z0-9._-]+$'
Assert-SafeSimpleValue -Name "RemotePath" -Value $RemotePath -Pattern '^/[A-Za-z0-9._~/-]+$'

if (-not (Test-Path $siteSource -PathType Container)) {
    throw "Static site source folder was not found: $siteSource"
}

$rootFiles = @(Get-ChildItem -Path $siteSource -File | Sort-Object -Property Name)
$topLevelDirectories = @(Get-ChildItem -Path $siteSource -Directory | Where-Object { $_.Name -ne "releases" } | Sort-Object -Property Name)
$uploadFiles = @($rootFiles)
foreach ($directory in $topLevelDirectories) {
    $uploadFiles += @(Get-ChildItem -Path $directory.FullName -File -Recurse | Sort-Object -Property FullName)
}

if ($uploadFiles.Count -eq 0) {
    throw "Static site source folder does not contain uploadable files outside site/public/releases: $siteSource"
}

$remoteTarget = "$ServerUser@$ServerHost`:$RemotePath/"
$sshTarget = "$ServerUser@$ServerHost"

Write-Host "Static site upload summary"
Write-Host "Local source: $siteSource"
Write-Host "Remote target: $remoteTarget"
Write-Host "SSH port: $SshPort"
Write-Host "Dry run: $([bool]$DryRun)"
Write-Host "Scope: uploads site/public root files and top-level folders such as site/public/assets in grouped scp commands. site/public/releases/** is skipped completely."
Write-Host "Release files: skipped. Backend deployment: not touched."
Write-Host "Files:"
foreach ($localFile in $uploadFiles) {
    $relativePath = Get-RelativePathFromSiteRoot -Path $localFile.FullName
    Write-Host (" - {0} ({1} bytes)" -f $relativePath, $localFile.Length)
}

Invoke-LoggedCommand -Arguments @("ssh", "-p", [string]$SshPort, "--", $sshTarget, "mkdir", "-p", $RemotePath)

if ($rootFiles.Count -gt 0) {
    $rootScpCommand = @("scp", "-P", [string]$SshPort, "--") + @($rootFiles | ForEach-Object { $_.FullName }) + @($remoteTarget)
    Invoke-LoggedCommand -Arguments $rootScpCommand
}

foreach ($directory in $topLevelDirectories) {
    Invoke-LoggedCommand -Arguments @("scp", "-P", [string]$SshPort, "-r", "--", $directory.FullName, $remoteTarget)
}

if ($DryRun) {
    Write-Host "Dry run completed. No files were uploaded."
}
else {
    Write-Host "Static site upload completed. Verify the public page over HTTPS before sharing it."
}
