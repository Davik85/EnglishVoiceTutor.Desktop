param(
    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string]$Version,

    [ValidateNotNullOrEmpty()]
    [string]$IsccPath,

    [ValidateNotNullOrEmpty()]
    [string]$BackendBaseUrl = "https://api.languagevoicetutor.com",

    [string[]]$ChangelogItem = @()
)

$ErrorActionPreference = "Stop"

$runtime = "win-x64"
$productName = "Language Voice Tutor"
$appId = "LanguageVoiceTutor.Desktop"
$platform = "windows"
$architecture = "win-x64"
$channel = "direct-tester"
$updateMode = "manual-confirmation"
$productionBackendBaseUrl = "https://api.languagevoicetutor.com"
$mainExe = "LanguageVoiceTutor.Desktop.exe"
$bundledVersionFileName = "release-version.txt"
$installerBaseName = "LanguageVoiceTutorSetup-$Version.exe"
$semVerPattern = '^(0|[1-9]\d*)\.(0|[1-9]\d*)\.(0|[1-9]\d*)(?:-[0-9A-Za-z-]+(?:\.[0-9A-Za-z-]+)*)?$'
$semVerCorePattern = '^(?<major>0|[1-9]\d*)\.(?<minor>0|[1-9]\d*)\.(?<patch>0|[1-9]\d*)'
$defaultChangelogItems = @(
    "Windows direct-download installer package generated for tester validation.",
    "Settings displays the installed app version for support and bug reports.",
    "Server-ready direct-download release manifest files generated for future download and update-check flows."
)
$knownIssues = @(
    "Installer is unsigned and may trigger Windows SmartScreen warnings.",
    "In-app updates use a manual-confirmation check from Settings."
)
$manifestNotes = @(
    "backendBaseUrl records the non-secret packaged default backend profile",
    "code signing deferred",
    "manual-confirmation update flow",
    "finish active lessons before starting an installer"
)

if ($Version -notmatch $semVerPattern) {
    throw "Installer version '$Version' is invalid. Use a SemVer-compatible version such as 0.1.0 or 0.1.0-beta.1. Build metadata and four-part versions are not supported for installer file naming."
}

$semVerCoreMatch = [regex]::Match($Version, $semVerCorePattern)
if (-not $semVerCoreMatch.Success) {
    throw "Installer version '$Version' did not contain a numeric SemVer core."
}

$numericAssemblyVersion = "{0}.{1}.{2}.0" -f $semVerCoreMatch.Groups["major"].Value, $semVerCoreMatch.Groups["minor"].Value, $semVerCoreMatch.Groups["patch"].Value
$releaseChangelogItems = if ($ChangelogItem.Count -gt 0) { @($ChangelogItem) } else { @($defaultChangelogItems) }

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = (Resolve-Path (Join-Path $scriptRoot "..")).Path
$projectPath = Join-Path $repoRoot "EnglishVoiceTutor.Desktop.csproj"
$innoScriptPath = Join-Path $repoRoot "installer\windows\LanguageVoiceTutor.iss"
$appIconPath = Join-Path $repoRoot "Assets\Branding\app-icon.ico"
$publishDirectory = Join-Path $repoRoot "artifacts\publish\win-x64-inno"
$installerDirectory = Join-Path $repoRoot "artifacts\installers\windows"
$releaseDirectory = Join-Path $repoRoot "artifacts\releases\windows\direct"
$expectedInstallerPath = Join-Path $installerDirectory $installerBaseName
$releaseInstallerPath = Join-Path $releaseDirectory $installerBaseName


function Normalize-BackendBaseUrl {
    param([string]$Value)

    $trimmedValue = $Value.Trim().TrimEnd("/")
    $uri = $null
    if (-not [System.Uri]::TryCreate($trimmedValue, [System.UriKind]::Absolute, [ref]$uri)) {
        throw "BackendBaseUrl must be an absolute http/https URL. Received: $Value"
    }

    if ($uri.Scheme -ne [System.Uri]::UriSchemeHttp -and $uri.Scheme -ne [System.Uri]::UriSchemeHttps) {
        throw "BackendBaseUrl must use http or https. Received: $Value"
    }

    return $uri.AbsoluteUri.TrimEnd("/")
}

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

    $forbiddenReleaseStrings = @(
        "http://localhost:5000",
        "127.0.0.1",
        "localhost",
        "Backend URL"
    )

    $publishedFiles = @(Get-ChildItem -Path $PublishPath -Recurse -File -Force)
    foreach ($file in $publishedFiles) {
        foreach ($forbidden in $forbiddenReleaseStrings) {
            $match = Select-String -Path $file.FullName -Pattern $forbidden -SimpleMatch -Quiet -ErrorAction SilentlyContinue
            if ($match) {
                throw "Release publish output contains forbidden backend override/UI string '$forbidden' in $($file.FullName)."
            }
        }
    }
}

function Write-JsonFile {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path,

        [Parameter(Mandatory = $true)]
        [object]$Value
    )

    $json = $Value | ConvertTo-Json -Depth 8
    Set-Content -Path $Path -Value $json -Encoding utf8
}

if (-not (Test-Path $projectPath -PathType Leaf)) {
    throw "EnglishVoiceTutor.Desktop.csproj was not found. Run this script from the repository checkout or keep it in the scripts folder."
}

if (-not (Test-Path $innoScriptPath -PathType Leaf)) {
    throw "Inno Setup script was not found: $innoScriptPath"
}

if (-not (Test-Path $appIconPath -PathType Leaf)) {
    throw "Application icon was not found: $appIconPath. Generate it with scripts/generate-app-icon.ps1 before packaging."
}

$BackendBaseUrl = Normalize-BackendBaseUrl -Value $BackendBaseUrl
if ($BackendBaseUrl -ne $productionBackendBaseUrl) {
    throw "Tester/release installed builds are server-only and must use $productionBackendBaseUrl. Local/custom backend URLs are DEBUG/developer-only."
}
$isccExe = Resolve-IsccPath -ExplicitPath $IsccPath

Set-Location $repoRoot

Write-Host "Language Voice Tutor Windows Inno Setup release"
Write-Host "Repository root: $repoRoot"
Write-Host "Version: $Version"
Write-Host "Runtime: $runtime"
Write-Host "Publish directory: $publishDirectory"
Write-Host "Installer directory: $installerDirectory"
Write-Host "Direct release directory: $releaseDirectory"
Write-Host "ISCC.exe: $isccExe"
Write-Host "Packaged backend URL: $BackendBaseUrl"
Write-Host "Application icon: $appIconPath"

Remove-Item -Recurse -Force $publishDirectory -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Force -Path $publishDirectory | Out-Null
New-Item -ItemType Directory -Force -Path $installerDirectory | Out-Null
New-Item -ItemType Directory -Force -Path $releaseDirectory | Out-Null
Remove-Item -Force $expectedInstallerPath -ErrorAction SilentlyContinue
Remove-Item -Force (Join-Path $releaseDirectory "LanguageVoiceTutorSetup-*.exe") -ErrorAction SilentlyContinue
Remove-Item -Force (Join-Path $releaseDirectory "latest.json") -ErrorAction SilentlyContinue
Remove-Item -Force (Join-Path $releaseDirectory "changelog.json") -ErrorAction SilentlyContinue
Remove-Item -Force (Join-Path $releaseDirectory "known-issues.json") -ErrorAction SilentlyContinue
Remove-Item -Force (Join-Path $releaseDirectory "checksums.sha256") -ErrorAction SilentlyContinue

Write-Host "Publishing desktop app..."
dotnet publish $projectPath -c Release -r $runtime --self-contained true -o $publishDirectory `
    /p:Version=$Version `
    /p:InformationalVersion=$Version `
    /p:AssemblyVersion=$numericAssemblyVersion `
    /p:FileVersion=$numericAssemblyVersion `
    /p:DesktopBackendBaseUrl=$BackendBaseUrl

$exePath = Join-Path $publishDirectory $mainExe
if (-not (Test-Path $exePath -PathType Leaf)) {
    throw "Publish completed, but $mainExe was not found in the publish directory."
}

$bundledVersionFilePath = Join-Path $publishDirectory $bundledVersionFileName
Set-Content -Path $bundledVersionFilePath -Value $Version -Encoding ascii -NoNewline
Write-Host "Bundled release version file: $bundledVersionFilePath"

Write-Host "Scanning publish output for forbidden local data, backend overrides, and secret-like files..."
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

Write-Host "Creating server-ready direct-download release manifest files..."
Copy-Item -Path $expectedInstallerPath -Destination $releaseInstallerPath -Force
$installerFile = Get-Item -Path $releaseInstallerPath
$installerHash = (Get-FileHash -Path $releaseInstallerPath -Algorithm SHA256).Hash.ToLowerInvariant()
$releaseDateUtc = (Get-Date).ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ")

$latestManifest = [ordered]@{
    productName = $productName
    appId = $appId
    platform = $platform
    architecture = $architecture
    channel = $channel
    version = $Version
    releaseDateUtc = $releaseDateUtc
    installerFileName = $installerBaseName
    installerRelativeUrl = $installerBaseName
    installerSha256 = $installerHash
    installerSizeBytes = $installerFile.Length
    backendBaseUrl = $BackendBaseUrl
    minimumSupportedVersion = $Version
    updateMode = $updateMode
    notes = @($manifestNotes)
}

$changelogManifest = [ordered]@{
    version = $Version
    releaseDateUtc = $releaseDateUtc
    items = @($releaseChangelogItems)
}

$knownIssuesManifest = [ordered]@{
    version = $Version
    releaseDateUtc = $releaseDateUtc
    issues = @($knownIssues)
}

Write-JsonFile -Path (Join-Path $releaseDirectory "latest.json") -Value $latestManifest
Write-JsonFile -Path (Join-Path $releaseDirectory "changelog.json") -Value $changelogManifest
Write-JsonFile -Path (Join-Path $releaseDirectory "known-issues.json") -Value $knownIssuesManifest
Set-Content -Path (Join-Path $releaseDirectory "checksums.sha256") -Value ("$installerHash  $installerBaseName") -Encoding ascii

Write-Host "Inno Setup installer created successfully."
Write-Host "Publish output: $publishDirectory"
Write-Host "Installer: $expectedInstallerPath"
Write-Host "Direct release output: $releaseDirectory"
Write-Host "Latest manifest: $(Join-Path $releaseDirectory 'latest.json')"
Write-Host "Packaged backend URL: $BackendBaseUrl"
