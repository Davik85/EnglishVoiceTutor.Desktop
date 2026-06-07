param(
    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string]$Version,

    [ValidateNotNullOrEmpty()]
    [string]$Channel = "win-x64-tester"
)

$ErrorActionPreference = "Stop"

$packageId = "EnglishVoiceTutor.Desktop"
$packageTitle = "English Voice Tutor"
$runtime = "win-x64"
$mainExe = "EnglishVoiceTutor.Desktop.exe"

$semVer2Pattern = '^(0|[1-9]\d*)\.(0|[1-9]\d*)\.(0|[1-9]\d*)(?:-[0-9A-Za-z-]+(?:\.[0-9A-Za-z-]+)*)?(?:\+[0-9A-Za-z-]+(?:\.[0-9A-Za-z-]+)*)?$'
$fourPartVersionPattern = '^\d+\.\d+\.\d+\.\d+(?:[-+].*)?$'

if ($Version -match $fourPartVersionPattern -or $Version -notmatch $semVer2Pattern) {
    throw "Velopack package version '$Version' is invalid. Use a SemVer 2 compatible version such as 0.1.0-tester.1. Do not use four-part versions such as 0.1.0.0."
}

$vpkCommand = Get-Command "vpk" -ErrorAction SilentlyContinue
if (-not $vpkCommand) {
    throw "The Velopack CLI command 'vpk' was not found. Install it as a .NET global tool, then reopen your terminal or ensure the .NET tools folder is on PATH: dotnet tool install -g vpk --version 1.2.0"
}

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = (Resolve-Path (Join-Path $scriptRoot "..")).Path
$projectPath = Join-Path $repoRoot "EnglishVoiceTutor.Desktop.csproj"

if (-not (Test-Path $projectPath)) {
    throw "EnglishVoiceTutor.Desktop.csproj was not found. Run this script from the repository checkout or keep it in the scripts folder."
}

Set-Location $repoRoot

$publishDirectory = Join-Path $repoRoot "artifacts\publish\win-x64-velopack-tester"
$releaseDirectory = Join-Path $repoRoot "artifacts\releases\windows\tester"

Write-Host "English Voice Tutor Desktop Velopack tester release"
Write-Host "Repository root: $repoRoot"
Write-Host "Package id: $packageId"
Write-Host "Package title: $packageTitle"
Write-Host "Package version: $Version"
Write-Host "Runtime: $runtime"
Write-Host "Channel: $Channel"
Write-Host "Publish directory: $publishDirectory"
Write-Host "Release directory: $releaseDirectory"

Remove-Item -Recurse -Force $publishDirectory -ErrorAction SilentlyContinue
Remove-Item -Recurse -Force $releaseDirectory -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Force -Path $publishDirectory | Out-Null
New-Item -ItemType Directory -Force -Path $releaseDirectory | Out-Null

Write-Host "Publishing desktop app..."
dotnet publish $projectPath -c Release -r $runtime --self-contained true -o $publishDirectory

$exePath = Join-Path $publishDirectory $mainExe
if (-not (Test-Path $exePath)) {
    throw "Publish completed, but $mainExe was not found in the publish directory."
}

$forbiddenFiles = Get-ChildItem -Path $publishDirectory -Recurse -File |
    Where-Object {
        $_.Name -ieq "settings.json" -or
        $_.Name -ieq "lesson-history.json" -or
        $_.Name -ieq "auth-session.json" -or
        $_.Name -imatch "token" -or
        $_.Name -imatch "secret" -or
        $_.Name -imatch "openai.*api.*key" -or
        $_.Name -imatch "api.*key"
    }

if ($forbiddenFiles) {
    $forbiddenList = ($forbiddenFiles | ForEach-Object { $_.FullName }) -join [Environment]::NewLine
    throw "Publish output contains forbidden Velopack tester release files:$([Environment]::NewLine)$forbiddenList"
}

Write-Host "Creating Velopack release files..."
& $vpkCommand.Source pack `
    --packId $packageId `
    --packTitle $packageTitle `
    --packVersion $Version `
    --packDir $publishDirectory `
    --mainExe $mainExe `
    --outputDir $releaseDirectory `
    --runtime $runtime `
    --channel $Channel

if ($LASTEXITCODE -ne 0) {
    throw "vpk pack failed with exit code $LASTEXITCODE."
}

$setupPath = Join-Path $releaseDirectory "Setup.exe"
if (-not (Test-Path $setupPath)) {
    $setupCandidates = Get-ChildItem -Path $releaseDirectory -Filter "*-Setup.exe" -File -ErrorAction SilentlyContinue
    if ($setupCandidates.Count -eq 1) {
        Copy-Item -Path $setupCandidates[0].FullName -Destination $setupPath -Force
        Write-Host "Copied $($setupCandidates[0].Name) to Setup.exe for the tester handoff convention."
    }
}

$releaseIndexPath = Join-Path $releaseDirectory ("releases.{0}.json" -f $Channel)
$fullPackages = Get-ChildItem -Path $releaseDirectory -Filter "*-full.nupkg" -File -ErrorAction SilentlyContinue

if (-not (Test-Path $setupPath)) {
    throw "Velopack Setup.exe was not created in $releaseDirectory."
}

if (-not (Test-Path $releaseIndexPath)) {
    throw "Velopack release index releases.$Channel.json was not created in $releaseDirectory."
}

if (-not $fullPackages) {
    throw "Velopack full .nupkg package was not created in $releaseDirectory."
}

Write-Host "Velopack tester release created successfully."
Write-Host "Publish output: $publishDirectory"
Write-Host "Release output: $releaseDirectory"
Write-Host "Installer: $setupPath"
Write-Host "Release index: $releaseIndexPath"
Write-Host "Full packages:"
$fullPackages | ForEach-Object { Write-Host "- $($_.FullName)" }
