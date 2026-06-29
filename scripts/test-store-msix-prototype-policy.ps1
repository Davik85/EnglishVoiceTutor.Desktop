$ErrorActionPreference = 'Stop'
$Root = Split-Path -Parent $PSScriptRoot
$WapProj = Join-Path $Root 'packaging/windows-msix/LanguageVoiceTutor.StorePrototype.wapproj'
$Manifest = Join-Path $Root 'packaging/windows-msix/Package.appxmanifest'
$Inno = Join-Path $Root 'installer/windows/LanguageVoiceTutor.iss'
$Docs = Join-Path $Root 'docs/WINDOWS_STORE_MSIX_PROTOTYPE.md'
$AssetGenerator = Join-Path $Root 'scripts/generate-store-msix-placeholder-assets.ps1'

if (-not (Test-Path $WapProj)) { throw "Missing MSIX prototype project: $WapProj" }
if (-not (Test-Path $Manifest)) { throw "Missing MSIX prototype manifest: $Manifest" }
if (-not (Test-Path $Inno)) { throw "Direct Inno installer script is missing: $Inno" }
if (-not (Test-Path $Docs)) { throw "Missing MSIX prototype documentation: $Docs" }
if (-not (Test-Path $AssetGenerator)) { throw "Missing local MSIX placeholder asset generator: $AssetGenerator" }

$WapContent = Get-Content $WapProj -Raw
if ($WapContent -notmatch 'DesktopDistributionChannel=Store') { throw 'MSIX prototype project must pass DesktopDistributionChannel=Store to the desktop app project.' }
if ($WapContent -notmatch 'RuntimeIdentifier=win-x64') { throw 'MSIX prototype project must pass RuntimeIdentifier=win-x64 so restore includes net10.0-windows/win-x64 assets.' }

$ManifestContent = Get-Content $Manifest -Raw
if ($ManifestContent -notmatch 'LanguageVoiceTutor\.Desktop\.StorePrototype') { throw 'MSIX prototype manifest must use the local prototype identity.' }
if ($ManifestContent -notmatch 'Version="\d+\.\d+\.\d+\.\d+"') { throw 'MSIX package version must be four numeric components.' }
foreach ($AssetPath in @('Assets\Square44x44Logo.png', 'Assets\Square150x150Logo.png', 'Assets\Wide310x150Logo.png', 'Assets\StoreLogo.png', 'Assets\SplashScreen.png')) {
    if ($ManifestContent -notmatch [regex]::Escape($AssetPath)) { throw "MSIX manifest must reference generated asset path: $AssetPath" }
}

$TrackedGeneratedArtifacts = git -C $Root ls-files -- '*.pfx' '*.pvk' '*.snk' '*.cer' 'packaging/windows-msix/Assets/*.png' 'packaging/windows-msix/AppPackages/*'
if ($TrackedGeneratedArtifacts) { throw "Tracked generated/signing artifacts are forbidden:`n$TrackedGeneratedArtifacts" }

$DocContent = Get-Content $Docs -Raw
foreach ($Forbidden in @('is available in the Microsoft Store', 'has passed WACK', 'submitted to Microsoft Store')) {
    if ($DocContent -match [regex]::Escape($Forbidden)) { throw "Prototype docs must not claim: $Forbidden" }
}

Write-Host 'Store MSIX prototype policy checks passed.'
