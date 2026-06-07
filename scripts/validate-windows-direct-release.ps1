param(
    [ValidateNotNullOrEmpty()]
    [string]$ReleaseDirectory
)

$ErrorActionPreference = "Stop"

$requiredProductName = "Language Voice Tutor"
$requiredAppId = "LanguageVoiceTutor.Desktop"
$requiredPlatform = "windows"
$requiredArchitecture = "win-x64"
$requiredUpdateMode = "manual-confirmation"
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

$validationErrors = New-Object System.Collections.Generic.List[string]

function Write-ValidationPass {
    param([Parameter(Mandatory = $true)][string]$Message)
    Write-Host "PASS: $Message"
}

function Write-ValidationFail {
    param([Parameter(Mandatory = $true)][string]$Message)
    Write-Host "FAIL: $Message" -ForegroundColor Red
    $script:validationErrors.Add($Message) | Out-Null
}

function Assert-Equal {
    param(
        [Parameter(Mandatory = $true)][string]$Name,
        [AllowNull()][object]$Actual,
        [Parameter(Mandatory = $true)][object]$Expected
    )

    if ($Actual -eq $Expected) {
        Write-ValidationPass "$Name is '$Expected'."
    }
    else {
        Write-ValidationFail "$Name should be '$Expected' but was '$Actual'."
    }
}

function Assert-PresentString {
    param(
        [Parameter(Mandatory = $true)][string]$Name,
        [AllowNull()][object]$Value
    )

    if ($null -ne $Value -and -not [string]::IsNullOrWhiteSpace([string]$Value)) {
        Write-ValidationPass "$Name is present."
    }
    else {
        Write-ValidationFail "$Name is required."
    }
}

function Read-JsonManifest {
    param([Parameter(Mandatory = $true)][string]$Path)

    try {
        return Get-Content -Path $Path -Raw | ConvertFrom-Json
    }
    catch {
        Write-ValidationFail "$(Split-Path -Leaf $Path) is not valid JSON: $($_.Exception.Message)"
        return $null
    }
}

function Get-ChecksumHashForFile {
    param(
        [Parameter(Mandatory = $true)][string]$ChecksumsPath,
        [Parameter(Mandatory = $true)][string]$FileName
    )

    $escapedFileName = [regex]::Escape($FileName)
    $pattern = "^\s*(?<hash>[A-Fa-f0-9]{64})\s+\*?$escapedFileName\s*$"
    $matches = @(Get-Content -Path $ChecksumsPath | Where-Object { $_ -match $pattern })

    if ($matches.Count -eq 0) {
        return $null
    }

    if ($matches.Count -gt 1) {
        Write-ValidationFail "checksums.sha256 contains multiple entries for $FileName."
        return $null
    }

    [void]($matches[0] -match $pattern)
    return $Matches["hash"].ToLowerInvariant()
}

Write-Host "Validating Windows direct release artifacts..."
Write-Host "Release directory: $ReleaseDirectory"

if (-not (Test-Path $ReleaseDirectory -PathType Container)) {
    Write-ValidationFail "Release directory does not exist: $ReleaseDirectory"
}
else {
    Write-ValidationPass "Release directory exists."
}

$resolvedReleaseDirectory = $null
if (Test-Path $ReleaseDirectory -PathType Container) {
    $resolvedReleaseDirectory = (Resolve-Path $ReleaseDirectory).Path
}
else {
    $resolvedReleaseDirectory = $ReleaseDirectory
}

$manifestPaths = @{}
foreach ($fileName in $requiredManifestFiles) {
    $path = Join-Path $resolvedReleaseDirectory $fileName
    $manifestPaths[$fileName] = $path

    if (Test-Path $path -PathType Leaf) {
        Write-ValidationPass "$fileName exists."
    }
    else {
        Write-ValidationFail "$fileName is missing."
    }
}

$latest = $null
$changelog = $null
$knownIssues = $null

if (Test-Path $manifestPaths["latest.json"] -PathType Leaf) {
    $latest = Read-JsonManifest -Path $manifestPaths["latest.json"]
    if ($null -ne $latest) { Write-ValidationPass "latest.json parses as JSON." }
}

if (Test-Path $manifestPaths["changelog.json"] -PathType Leaf) {
    $changelog = Read-JsonManifest -Path $manifestPaths["changelog.json"]
    if ($null -ne $changelog) { Write-ValidationPass "changelog.json parses as JSON." }
}

if (Test-Path $manifestPaths["known-issues.json"] -PathType Leaf) {
    $knownIssues = Read-JsonManifest -Path $manifestPaths["known-issues.json"]
    if ($null -ne $knownIssues) { Write-ValidationPass "known-issues.json parses as JSON." }
}

if ($null -ne $latest) {
    Assert-Equal -Name "productName" -Actual $latest.productName -Expected $requiredProductName
    Assert-Equal -Name "appId" -Actual $latest.appId -Expected $requiredAppId
    Assert-Equal -Name "platform" -Actual $latest.platform -Expected $requiredPlatform
    Assert-Equal -Name "architecture" -Actual $latest.architecture -Expected $requiredArchitecture
    Assert-PresentString -Name "channel" -Value $latest.channel
    Assert-PresentString -Name "version" -Value $latest.version
    Assert-PresentString -Name "installerFileName" -Value $latest.installerFileName
    Assert-PresentString -Name "installerRelativeUrl" -Value $latest.installerRelativeUrl
    Assert-PresentString -Name "installerSha256" -Value $latest.installerSha256
    Assert-Equal -Name "updateMode" -Actual $latest.updateMode -Expected $requiredUpdateMode

    if ($latest.installerSizeBytes -is [int] -or $latest.installerSizeBytes -is [long] -or $latest.installerSizeBytes -is [decimal] -or $latest.installerSizeBytes -is [double]) {
        if ([int64]$latest.installerSizeBytes -gt 0) {
            Write-ValidationPass "installerSizeBytes is greater than 0."
        }
        else {
            Write-ValidationFail "installerSizeBytes must be greater than 0."
        }
    }
    else {
        Write-ValidationFail "installerSizeBytes must be numeric and greater than 0."
    }

    $latestJsonRaw = Get-Content -Path $manifestPaths["latest.json"] -Raw
    if ($latestJsonRaw -match '(?i)[A-Z]:\\' -or $latestJsonRaw -match '\\\\' -or $latestJsonRaw -match '\\[^"/]*\\') {
        Write-ValidationFail "latest.json appears to contain an absolute or backslash-heavy local Windows path."
    }
    else {
        Write-ValidationPass "latest.json does not contain obvious local Windows paths."
    }

    if ($latest.installerSha256 -and ([string]$latest.installerSha256) -notmatch '^[A-Fa-f0-9]{64}$') {
        Write-ValidationFail "installerSha256 must be a 64-character SHA-256 hex value."
    }

    if (-not [string]::IsNullOrWhiteSpace([string]$latest.installerFileName)) {
        $installerPath = Join-Path $resolvedReleaseDirectory ([string]$latest.installerFileName)
        if (Test-Path $installerPath -PathType Leaf) {
            Write-ValidationPass "Installer file exists: $($latest.installerFileName)"

            $computedHash = (Get-FileHash -Path $installerPath -Algorithm SHA256).Hash.ToLowerInvariant()
            $manifestHash = ([string]$latest.installerSha256).ToLowerInvariant()
            if ($computedHash -eq $manifestHash) {
                Write-ValidationPass "Installer SHA-256 matches latest.json."
            }
            else {
                Write-ValidationFail "Installer SHA-256 '$computedHash' does not match latest.json '$manifestHash'."
            }

            if (Test-Path $manifestPaths["checksums.sha256"] -PathType Leaf) {
                $checksumHash = Get-ChecksumHashForFile -ChecksumsPath $manifestPaths["checksums.sha256"] -FileName ([string]$latest.installerFileName)
                if ($null -eq $checksumHash) {
                    Write-ValidationFail "checksums.sha256 does not contain an entry for $($latest.installerFileName)."
                }
                elseif ($computedHash -eq $checksumHash) {
                    Write-ValidationPass "Installer SHA-256 matches checksums.sha256."
                }
                else {
                    Write-ValidationFail "Installer SHA-256 '$computedHash' does not match checksums.sha256 '$checksumHash'."
                }
            }
        }
        else {
            Write-ValidationFail "Installer file from latest.json is missing: $installerPath"
        }
    }
}

if ($null -ne $latest -and $null -ne $changelog) {
    Assert-Equal -Name "changelog.json version" -Actual $changelog.version -Expected $latest.version
}

if ($null -ne $latest -and $null -ne $knownIssues) {
    Assert-Equal -Name "known-issues.json version" -Actual $knownIssues.version -Expected $latest.version
}

if ($validationErrors.Count -gt 0) {
    Write-Host "Windows direct release validation FAILED with $($validationErrors.Count) error(s)." -ForegroundColor Red
    exit 1
}

Write-Host "Windows direct release validation PASSED."
exit 0
