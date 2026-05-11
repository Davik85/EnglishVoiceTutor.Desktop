param(
    [switch]$SelfContained
)

$ErrorActionPreference = "Stop"

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = (Resolve-Path (Join-Path $scriptRoot "..")).Path
$projectPath = Join-Path $repoRoot "EnglishVoiceTutor.Desktop.csproj"

if (-not (Test-Path $projectPath)) {
    throw "EnglishVoiceTutor.Desktop.csproj was not found. Run this script from the repository checkout or keep it in the scripts folder."
}

Set-Location $repoRoot

$packageKind = "framework-dependent"
$selfContainedValue = "false"

if ($SelfContained) {
    $packageKind = "self-contained"
    $selfContainedValue = "true"
}

$publishDirectory = Join-Path $repoRoot ("artifacts\publish\win-x64-{0}" -f $packageKind)
$packagesDirectory = Join-Path $repoRoot "artifacts\packages"
$zipPath = Join-Path $packagesDirectory ("EnglishVoiceTutor.Desktop-win-x64-{0}.zip" -f $packageKind)

Write-Host "English Voice Tutor Desktop tester package"
Write-Host "Repository root: $repoRoot"
Write-Host "Package type: $packageKind"
Write-Host "Publish directory: $publishDirectory"
Write-Host "Zip path: $zipPath"

Remove-Item -Recurse -Force $publishDirectory -ErrorAction SilentlyContinue
Remove-Item -Force $zipPath -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Force -Path $publishDirectory | Out-Null
New-Item -ItemType Directory -Force -Path $packagesDirectory | Out-Null

Write-Host "Publishing desktop app..."
dotnet publish $projectPath -c Release -r win-x64 --self-contained $selfContainedValue -o $publishDirectory

$exePath = Join-Path $publishDirectory "EnglishVoiceTutor.Desktop.exe"
if (-not (Test-Path $exePath)) {
    throw "Publish completed, but EnglishVoiceTutor.Desktop.exe was not found in the publish directory."
}

$forbiddenFiles = Get-ChildItem -Path $publishDirectory -Recurse -File |
    Where-Object {
        $_.Name -ieq "settings.json" -or
        $_.Name -ieq "lesson-history.json" -or
        $_.Name -imatch "openai.*api.*key" -or
        $_.Name -imatch "api.*key"
    }

if ($forbiddenFiles) {
    $forbiddenList = ($forbiddenFiles | ForEach-Object { $_.FullName }) -join [Environment]::NewLine
    throw "Publish output contains forbidden tester package files:$([Environment]::NewLine)$forbiddenList"
}

Write-Host "Creating zip package..."
Compress-Archive -Path (Join-Path $publishDirectory "*") -DestinationPath $zipPath -Force

if (-not (Test-Path $zipPath)) {
    throw "Zip package was not created."
}

Write-Host "Tester package created successfully."
Write-Host "Publish output: $publishDirectory"
Write-Host "Zip package: $zipPath"
