$ErrorActionPreference = 'Stop'
$Root = Split-Path -Parent $PSScriptRoot
$WapProj = Join-Path $Root 'packaging/windows-msix/LanguageVoiceTutor.StorePrototype.wapproj'
$Manifest = Join-Path $Root 'packaging/windows-msix/Package.appxmanifest'
$Inno = Join-Path $Root 'installer/windows/LanguageVoiceTutor.iss'
$Docs = Join-Path $Root 'docs/WINDOWS_STORE_MSIX_PROTOTYPE.md'
$VersionProvider = Join-Path $Root 'Services/Updates/DesktopAppVersionProvider.cs'
$SettingsViewModel = Join-Path $Root 'ViewModels/SettingsViewModel.cs'
$AssetGenerator = Join-Path $Root 'scripts/generate-store-msix-placeholder-assets.ps1'

if (-not (Test-Path $WapProj)) { throw "Missing MSIX prototype project: $WapProj" }
if (-not (Test-Path $Manifest)) { throw "Missing MSIX prototype manifest: $Manifest" }
if (-not (Test-Path $Inno)) { throw "Direct Inno installer script is missing: $Inno" }
if (-not (Test-Path $Docs)) { throw "Missing MSIX prototype documentation: $Docs" }
if (-not (Test-Path $AssetGenerator)) { throw "Missing local MSIX asset generator: $AssetGenerator" }
if (-not (Test-Path $VersionProvider)) { throw "Missing desktop app version provider: $VersionProvider" }
if (-not (Test-Path $SettingsViewModel)) { throw "Missing Settings view model: $SettingsViewModel" }

$WapContent = Get-Content $WapProj -Raw
if ($WapContent -notmatch 'DesktopDistributionChannel=Store') { throw 'MSIX prototype project must pass DesktopDistributionChannel=Store to the desktop app project.' }
if ($WapContent -notmatch 'RuntimeIdentifier=win-x64') { throw 'MSIX prototype project must pass RuntimeIdentifier=win-x64 so restore includes net10.0-windows/win-x64 assets.' }
if ($WapContent -notmatch [regex]::Escape('Assets\Square310x310Logo.png')) { throw 'MSIX prototype project must package Assets\Square310x310Logo.png.' }

$ManifestContent = Get-Content $Manifest -Raw
if ($ManifestContent -notmatch 'LanguageVoiceTutor\.Desktop\.StorePrototype') { throw 'MSIX prototype manifest must use the local prototype identity.' }
if ($ManifestContent -notmatch 'Version="\d+\.\d+\.\d+\.\d+"') { throw 'MSIX package version must be four numeric components.' }
if ($ManifestContent -notmatch 'Square310x310Logo="Assets\\Square310x310Logo\.png"') { throw 'MSIX manifest Square310x310Logo must reference Assets\Square310x310Logo.png.' }
if ($ManifestContent -match 'Square310x310Logo="Assets\\Square150x150Logo\.png"') { throw 'MSIX manifest Square310x310Logo must not reuse the 150x150 asset.' }

foreach ($AssetPath in @('Assets\Square44x44Logo.png', 'Assets\Square150x150Logo.png', 'Assets\Square310x310Logo.png', 'Assets\Wide310x150Logo.png', 'Assets\StoreLogo.png', 'Assets\SplashScreen.png')) {
    if ($ManifestContent -notmatch [regex]::Escape($AssetPath)) { throw "MSIX manifest must reference generated asset path: $AssetPath" }
}


$VersionProviderContent = Get-Content $VersionProvider -Raw
if ($VersionProviderContent -notmatch 'GetCurrentPackageFullName' -or $VersionProviderContent -notmatch 'TryReadMsixPackageIdentityVersion') { throw 'Store/MSIX version display must read the current package identity version when package identity is available.' }
if ($VersionProviderContent -notmatch 'DesktopDistributionChannelProvider\.IsStore') { throw 'Version provider must branch Store/MSIX behavior from Direct behavior.' }
if ($VersionProviderContent -notmatch 'GetDirectVersionText\(\)' -or $VersionProviderContent -notmatch [regex]::Escape('TryReadBundledVersionFile()')) { throw 'Direct version behavior must keep using the bundled release-version.txt flow.' }
if ($VersionProviderContent -notmatch 'ReadInformationalVersion') { throw 'Version provider must fall back to assembly informational version when package identity is unavailable.' }
if ($VersionProviderContent -notmatch 'AppModelErrorNoPackage' -or $VersionProviderContent -notmatch 'catch \(DllNotFoundException\)' -or $VersionProviderContent -notmatch 'catch \(EntryPointNotFoundException\)') { throw 'MSIX package identity lookup must fail safely for unpackaged Direct builds or unavailable Windows APIs.' }
if ($VersionProviderContent -notmatch 'Version: \{version\}' -or $VersionProviderContent -notmatch 'Version: v\{version\}') { throw 'Store display must avoid the Direct v-prefix while Direct display keeps it.' }
if ($VersionProviderContent -match 'StoreManagedUpdatesMessage\s*=') { throw 'Version display changes must not alter the Store-managed update message.' }

$SettingsViewModelContent = Get-Content $SettingsViewModel -Raw
if ($SettingsViewModelContent -notmatch 'DesktopAppVersionProvider\.GetInstalledVersionDisplayText\(\)') { throw 'Settings footer must use the channel-aware installed version display text.' }
if ($SettingsViewModelContent -match 'InstalledAppVersionText\s*=>\s*\$"Version: v\{appVersionText\}"') { throw 'Settings footer must not hard-code Direct v-prefix for Store/MSIX builds.' }

$AssetGeneratorContent = Get-Content $AssetGenerator -Raw
if ($AssetGeneratorContent -notmatch [regex]::Escape('Assets/Branding/app-icon.ico') -and $AssetGeneratorContent -notmatch [regex]::Escape('Assets\Branding\app-icon.ico')) { throw 'MSIX asset generator must use the tracked Direct desktop app icon source.' }
if ($AssetGeneratorContent -notmatch 'System\.Drawing\.Icon' -or $AssetGeneratorContent -notmatch 'DrawIcon') { throw 'MSIX asset generator must render from the real app icon instead of text-only placeholder drawing.' }
if ($AssetGeneratorContent -match "DrawString\s*\(\s*'LVT'" -or $AssetGeneratorContent -match 'placeholder generator') { throw 'MSIX asset generator must not use placeholder-only LVT drawing logic.' }
if ($AssetGeneratorContent -notmatch "Name\s*=\s*'Square310x310Logo\.png';\s*Width\s*=\s*310;\s*Height\s*=\s*310") { throw 'MSIX asset generator must create Square310x310Logo.png as 310x310.' }
foreach ($GeneratedAsset in @('Square44x44Logo.png', 'Square150x150Logo.png', 'Square310x310Logo.png', 'Wide310x150Logo.png', 'StoreLogo.png', 'SplashScreen.png')) {
    if ($AssetGeneratorContent -notmatch [regex]::Escape($GeneratedAsset)) { throw "MSIX asset generator must generate $GeneratedAsset." }
}

$ExpectedDimensions = @{
    'Square44x44Logo.png' = @(44, 44)
    'Square150x150Logo.png' = @(150, 150)
    'Square310x310Logo.png' = @(310, 310)
    'Wide310x150Logo.png' = @(310, 150)
    'StoreLogo.png' = @(50, 50)
    'SplashScreen.png' = @(620, 300)
}
foreach ($AssetName in $ExpectedDimensions.Keys) {
    $AssetFile = Join-Path $Root "packaging/windows-msix/Assets/$AssetName"
    if (Test-Path $AssetFile) {
        Add-Type -AssemblyName System.Drawing
        $Image = [System.Drawing.Image]::FromFile($AssetFile)
        try {
            if ($Image.Width -ne $ExpectedDimensions[$AssetName][0] -or $Image.Height -ne $ExpectedDimensions[$AssetName][1]) {
                throw "Generated asset $AssetName has dimensions $($Image.Width)x$($Image.Height); expected $($ExpectedDimensions[$AssetName][0])x$($ExpectedDimensions[$AssetName][1])."
            }
        }
        finally { $Image.Dispose() }
    }
}

$DesktopProjectContent = Get-Content (Join-Path $Root 'EnglishVoiceTutor.Desktop.csproj') -Raw
$InnoContent = Get-Content $Inno -Raw
if ($DesktopProjectContent -notmatch [regex]::Escape('Assets\Branding\app-icon.ico')) { throw 'Desktop project must continue to use Assets\Branding\app-icon.ico.' }
if ($InnoContent -notmatch [regex]::Escape('Assets\Branding\app-icon.ico') -or $InnoContent -notmatch 'IconFilename') { throw 'Direct Inno installer must continue to use the tracked app icon for shortcuts.' }
$InnoDiff = git -C $Root diff -- installer/windows/LanguageVoiceTutor.iss
if ($InnoDiff) { throw "Direct Inno installer script has uncommitted changes, but this prototype icon change must not alter Inno behavior:`n$InnoDiff" }

$TrackedGeneratedArtifacts = git -C $Root ls-files -- '*.pfx' '*.pvk' '*.snk' '*.cer' 'packaging/windows-msix/Assets/*.png' 'packaging/windows-msix/AppPackages/*'
if ($TrackedGeneratedArtifacts) { throw "Tracked generated/signing artifacts are forbidden:`n$TrackedGeneratedArtifacts" }

$DocContent = Get-Content $Docs -Raw
if ($DocContent -notmatch 'Settings footer') { throw 'MSIX prototype docs must document Settings footer version verification.' }
if ($DocContent -notmatch 'Assets/Branding/app-icon.ico' -or $DocContent -match 'placeholder PNG') { throw 'MSIX prototype docs must document local asset generation from the Direct desktop app icon source, not placeholder assets.' }
if ($DocContent -notmatch '0\.1\.36\.0') { throw 'MSIX prototype docs must document the current prototype package version.' }

foreach ($Forbidden in @('is available in the Microsoft Store', 'has passed WACK', 'submitted to Microsoft Store')) {
    if ($DocContent -match [regex]::Escape($Forbidden)) { throw "Prototype docs must not claim: $Forbidden" }
}

Write-Host 'Store MSIX prototype policy checks passed.'
