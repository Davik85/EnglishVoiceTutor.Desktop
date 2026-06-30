param(
    [string]$OutputDirectory = (Join-Path (Split-Path -Parent $PSScriptRoot) 'packaging/windows-msix/Assets'),
    [string]$SourceIconPath = (Join-Path (Split-Path -Parent $PSScriptRoot) 'Assets/Branding/app-icon.ico')
)

$ErrorActionPreference = 'Stop'

# Local-only MSIX prototype asset generator.
# Assets are generated from the same tracked application icon used by the Direct
# EXE/Inno desktop app. Generated PNG files are ignored by git.
Add-Type -AssemblyName System.Drawing

if (-not (Test-Path -LiteralPath $SourceIconPath)) {
    throw "Missing source app icon: $SourceIconPath"
}

if (-not (Test-Path -LiteralPath $OutputDirectory)) {
    New-Item -ItemType Directory -Path $OutputDirectory | Out-Null
}

$assets = @(
    @{ Name = 'Square44x44Logo.png'; Width = 44; Height = 44; IconScale = 0.82 },
    @{ Name = 'Square150x150Logo.png'; Width = 150; Height = 150; IconScale = 0.82 },
    @{ Name = 'Square310x310Logo.png'; Width = 310; Height = 310; IconScale = 0.82 },
    @{ Name = 'Wide310x150Logo.png'; Width = 310; Height = 150; IconScale = 0.68 },
    @{ Name = 'StoreLogo.png'; Width = 50; Height = 50; IconScale = 0.82 },
    @{ Name = 'SplashScreen.png'; Width = 620; Height = 300; IconScale = 0.46 }
)

$sourceIcon = New-Object System.Drawing.Icon $SourceIconPath
try {
    foreach ($asset in $assets) {
        $bitmap = New-Object System.Drawing.Bitmap $asset.Width, $asset.Height, ([System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
        $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
        try {
            $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
            $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
            $graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
            $graphics.Clear([System.Drawing.Color]::FromArgb(30, 64, 175))

            $iconSize = [int][Math]::Round([Math]::Min($asset.Width, $asset.Height) * $asset.IconScale)
            $x = [int][Math]::Round(($asset.Width - $iconSize) / 2)
            $y = [int][Math]::Round(($asset.Height - $iconSize) / 2)
            $rectangle = New-Object System.Drawing.Rectangle $x, $y, $iconSize, $iconSize
            $graphics.DrawIcon($sourceIcon, $rectangle)

            $path = Join-Path $OutputDirectory $asset.Name
            if (Test-Path -LiteralPath $path) {
                Remove-Item -LiteralPath $path -Force
            }

            $bitmap.Save($path, [System.Drawing.Imaging.ImageFormat]::Png)
            Write-Host "Generated $path from $SourceIconPath"
        }
        finally {
            $graphics.Dispose()
            $bitmap.Dispose()
        }
    }
}
finally {
    $sourceIcon.Dispose()
}
