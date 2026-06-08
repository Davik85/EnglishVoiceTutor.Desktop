param(
    [Parameter(Mandatory = $true)]
    [ValidatePattern('^[0-9]+\.[0-9]+\.[0-9]+(?:[-+][0-9A-Za-z][0-9A-Za-z.-]*)?$')]
    [string]$Version
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
$projectPath = Join-Path $repoRoot 'backend/EnglishVoiceTutor.Api/EnglishVoiceTutor.Api.csproj'
$publishDir = Join-Path $repoRoot 'artifacts/publish/backend-linux-x64'
$packageDir = Join-Path $repoRoot 'artifacts/packages/backend'
$archivePath = Join-Path $packageDir "LanguageVoiceTutor.Backend-linux-x64-$Version.zip"
$expectedExecutable = Join-Path $publishDir 'EnglishVoiceTutor.Api'
$expectedDll = Join-Path $publishDir 'EnglishVoiceTutor.Api.dll'

$forbiddenExactFileNames = @(
    '.env',
    'settings.json',
    'lesson-history.json'
)

$forbiddenFileNameFragments = @(
    'secret',
    'token',
    'password',
    'api key',
    'api-key',
    'apikey',
    'openai',
    'paddle',
    'smtp',
    'auth-session'
)

$compiledOutputExtensions = @(
    '.dll',
    '.exe',
    '.pdb',
    '.so',
    '.deps.json',
    '.runtimeconfig.json'
)

function Assert-NoForbiddenFileNames {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path,

        [Parameter(Mandatory = $true)]
        [string]$Label
    )

    if (-not (Test-Path -LiteralPath $Path)) {
        throw "$Label path does not exist: $Path"
    }

    $matches = Get-ChildItem -LiteralPath $Path -Recurse -File | Where-Object {
        $fileName = $_.Name.ToLowerInvariant()

        foreach ($exactFileName in $forbiddenExactFileNames) {
            if ($fileName -eq $exactFileName.ToLowerInvariant()) {
                return $true
            }
        }

        $extension = $_.Extension.ToLowerInvariant()
        $isCompiledOutput = $compiledOutputExtensions.Contains($extension) -or $fileName.EndsWith('.deps.json') -or $fileName.EndsWith('.runtimeconfig.json')
        if ($isCompiledOutput) {
            return $false
        }

        foreach ($fragment in $forbiddenFileNameFragments) {
            if ($fileName.Contains($fragment.ToLowerInvariant())) {
                return $true
            }
        }

        return $false
    }

    if ($matches) {
        $matchList = ($matches | ForEach-Object { $_.FullName }) -join [Environment]::NewLine
        throw "Forbidden file names were found in $Label. Remove local-only or secret-like files before packaging:$([Environment]::NewLine)$matchList"
    }
}

Write-Host "Packaging Language Voice Tutor backend $Version for linux-x64."
Write-Host "Project: $projectPath"
Write-Host "Publish output: $publishDir"
Write-Host "Archive: $archivePath"

if (-not (Test-Path -LiteralPath $projectPath)) {
    throw "Backend project was not found: $projectPath"
}

if (Test-Path -LiteralPath $publishDir) {
    Write-Host "Cleaning old publish output."
    Remove-Item -LiteralPath $publishDir -Recurse -Force
}

if (Test-Path -LiteralPath $archivePath) {
    Write-Host "Removing previous archive for this version."
    Remove-Item -LiteralPath $archivePath -Force
}

New-Item -ItemType Directory -Path $publishDir -Force | Out-Null
New-Item -ItemType Directory -Path $packageDir -Force | Out-Null

$publishArgs = @(
    'publish',
    $projectPath,
    '-c',
    'Release',
    '-r',
    'linux-x64',
    '--self-contained',
    'true',
    '-o',
    $publishDir,
    '/p:PublishSingleFile=false'
)

Write-Host "Running: dotnet $($publishArgs -join ' ')"
& dotnet @publishArgs
if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish failed with exit code $LASTEXITCODE."
}

$developmentSettingsPath = Join-Path $publishDir 'appsettings.Development.json'
if (Test-Path -LiteralPath $developmentSettingsPath) {
    Write-Host "Removing appsettings.Development.json from publish output."
    Remove-Item -LiteralPath $developmentSettingsPath -Force
}

Assert-NoForbiddenFileNames -Path $publishDir -Label 'publish output'

if (-not (Test-Path -LiteralPath $expectedExecutable) -and -not (Test-Path -LiteralPath $expectedDll)) {
    throw "Neither the backend executable nor DLL was found in publish output. Expected one of: $expectedExecutable or $expectedDll"
}

$archiveInputFiles = Get-ChildItem -LiteralPath $publishDir -Recurse -File
if (-not $archiveInputFiles) {
    throw "Publish output is empty: $publishDir"
}

Assert-NoForbiddenFileNames -Path $publishDir -Label 'archive input'

Compress-Archive -Path (Join-Path $publishDir '*') -DestinationPath $archivePath -CompressionLevel Optimal -Force

if (-not (Test-Path -LiteralPath $archivePath)) {
    throw "Archive was not created: $archivePath"
}

Write-Host "Backend Linux package created successfully."
Write-Host "Publish output: $publishDir"
Write-Host "Archive: $archivePath"
Write-Host "No migrations were run by this packaging script."
