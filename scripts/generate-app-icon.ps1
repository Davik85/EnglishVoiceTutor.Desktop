param(
    [string]$SourcePath = "Assets/Branding/app-icon-source.png",
    [string]$OutputPath = "Assets/Branding/app-icon.ico"
)

$ErrorActionPreference = "Stop"
$requiredIconSizes = @(16, 24, 32, 48, 64, 128, 256)
$repoRoot = Split-Path -Parent $PSScriptRoot
$resolvedSourcePath = Join-Path $repoRoot $SourcePath
$resolvedOutputPath = Join-Path $repoRoot $OutputPath

if (-not (Test-Path $resolvedSourcePath -PathType Leaf)) {
    throw "App icon source image was not found at $SourcePath. Place the app icon source PNG there, then rerun this script."
}

$magickCommand = Get-Command "magick" -ErrorAction SilentlyContinue
if (-not $magickCommand) {
    throw "ImageMagick was not found. Install ImageMagick and ensure 'magick' is on PATH, or keep the committed $OutputPath file for normal builds."
}

$outputDirectory = Split-Path -Parent $resolvedOutputPath
New-Item -ItemType Directory -Force -Path $outputDirectory | Out-Null

$resizeList = ($requiredIconSizes | Sort-Object -Descending) -join ","
& $magickCommand.Source $resolvedSourcePath -define "icon:auto-resize=$resizeList" $resolvedOutputPath

if ($LASTEXITCODE -ne 0) {
    throw "ImageMagick failed to generate $OutputPath."
}

if (-not (Test-Path $resolvedOutputPath -PathType Leaf)) {
    throw "Icon generation completed but did not create $OutputPath."
}

Write-Host "Generated $OutputPath from $SourcePath with sizes: $($requiredIconSizes -join ', ')"
