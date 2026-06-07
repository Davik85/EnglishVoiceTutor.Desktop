param(
    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string]$Version,

    [ValidateNotNullOrEmpty()]
    [string]$IsccPath
)

$ErrorActionPreference = "Stop"

$runtime = "win-x64"
$mainExe = "EnglishVoiceTutor.Desktop.exe"
$installerBaseName = "LanguageVoiceTutorSetup-$Version.exe"
$semVerPattern = '^(0|[1-9]\d*)\.(0|[1-9]\d*)\.(0|[1-9]\d*)(?:-[0-9A-Za-z-]+(?:\.[0-9A-Za-z-]+)*)?$'

if ($Version -notmatch $semVerPattern) {
    throw "Installer version '$Version' is invalid. Use a SemVer-compatible version such as 0.1.0 or 0.1.0-beta.1. Build metadata and four-part versions are not supported for installer file naming."
}

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = (Resolve-Path (Join-Path $scriptRoot "..")).Path
$projectPath = Join-Path $repoRoot "EnglishVoiceTutor.Desktop.csproj"
$innoScriptPath = Join-Path $repoRoot "installer\windows\LanguageVoiceTutor.iss"
$publishDirectory = Join-Path $repoRoot "artifacts\publish\win-x64-inno"
$installerDirectory = Join-Path $repoRoot "artifacts\installers\windows"
$expectedInstallerPath = Join-Path $installerDirectory $installerBaseName

function Resolve-IsccPath {
    param([string]$ExplicitPath)

    if ($ExplicitPath) {
        if (-not (Test-Path $ExplicitPath -PathType Leaf)) {
            throw "ISCC.exe was not found at the supplied -IsccPath: $ExplicitPath"
        }

        return (Resolve-Path $ExplicitPath).Path
    }

    $candidatePaths = @(
        "C:\Program Files (x86)\Inno Setup 6\ISCC.exe",
        "C:\Program Files\Inno Setup 6\ISCC.exe"
    )

    foreach ($candidatePath in $candidatePaths) {
        if (Test-Path $candidatePath -PathType Leaf) {
            return $candidatePath
        }
    }

    $pathCommand = Get-Command "ISCC.exe" -ErrorAction SilentlyContinue
    if ($pathCommand) {
        return $pathCommand.Source
    }

    throw "ISCC.exe was not found. Install Inno Setup 6 from https://jrsoftware.org/isinfo.php, use the default install path, or pass -IsccPath 'C:\Path\To\ISCC.exe'."
}

function Assert-PublishOutputIsSafe {
    param([string]$PublishPath)

    $forbiddenFiles = Get-ChildItem -Path $PublishPath -Recurse -File -Force |
        Where-Object {
            $_.Name -ieq "settings.json" -or
            $_.Name -ieq "lesson-history.json" -or
            $_.Name -ieq "auth-session.json" -or
            $_.Name -imatch "token" -or
            $_.Name -imatch "secret" -or
            $_.Name -imatch "api[._ -]*key" -or
            $_.Name -imatch "openai[._ -]*api[._ -]*key" -or
            $_.Name -ieq ".env" -or
            $_.Name -ilike ".env.*"
        }

    if ($forbiddenFiles) {
        $forbiddenList = ($forbiddenFiles | ForEach-Object { $_.FullName }) -join [Environment]::NewLine
        throw "Publish output contains forbidden installer files:$([Environment]::NewLine)$forbiddenList"
    }
}

if (-not (Test-Path $projectPath -PathType Leaf)) {
    throw "EnglishVoiceTutor.Desktop.csproj was not found. Run this script from the repository checkout or keep it in the scripts folder."
}

if (-not (Test-Path $innoScriptPath -PathType Leaf)) {
    throw "Inno Setup script was not found: $innoScriptPath"
}

$isccExe = Resolve-IsccPath -ExplicitPath $IsccPath

Set-Location $repoRoot

Write-Host "Language Voice Tutor Windows Inno Setup release"
Write-Host "Repository root: $repoRoot"
Write-Host "Version: $Version"
Write-Host "Runtime: $runtime"
Write-Host "Publish directory: $publishDirectory"
Write-Host "Installer directory: $installerDirectory"
Write-Host "ISCC.exe: $isccExe"

Remove-Item -Recurse -Force $publishDirectory -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Force -Path $publishDirectory | Out-Null
New-Item -ItemType Directory -Force -Path $installerDirectory | Out-Null
Remove-Item -Force $expectedInstallerPath -ErrorAction SilentlyContinue

Write-Host "Publishing desktop app..."
dotnet publish $projectPath -c Release -r $runtime --self-contained true -o $publishDirectory

$exePath = Join-Path $publishDirectory $mainExe
if (-not (Test-Path $exePath -PathType Leaf)) {
    throw "Publish completed, but $mainExe was not found in the publish directory."
}

Write-Host "Scanning publish output for forbidden local data and secret-like files..."
Assert-PublishOutputIsSafe -PublishPath $publishDirectory

Write-Host "Building Inno Setup installer..."
$innoScriptDirectory = Split-Path -Parent $innoScriptPath
Push-Location $innoScriptDirectory
try {
    & $isccExe "/DAppVersion=$Version" $innoScriptPath

    if ($LASTEXITCODE -ne 0) {
        throw "ISCC.exe failed with exit code $LASTEXITCODE."
    }
}
finally {
    Pop-Location
}

if (-not (Test-Path $expectedInstallerPath -PathType Leaf)) {
    throw "Expected installer was not created: $expectedInstallerPath"
}

Write-Host "Inno Setup installer created successfully."
Write-Host "Publish output: $publishDirectory"
Write-Host "Installer: $expectedInstallerPath"
