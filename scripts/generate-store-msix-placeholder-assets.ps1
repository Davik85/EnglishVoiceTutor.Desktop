param(
    [string]$OutputDirectory = (Join-Path (Split-Path -Parent $PSScriptRoot) 'packaging/windows-msix/Assets')
)

$ErrorActionPreference = 'Stop'

# Local-only placeholder generator for the MSIX prototype.
# This intentionally uses simple built-in drawing on Windows and does not download
# or embed third-party assets. Generated PNG files are ignored by git.
Add-Type -AssemblyName System.Drawing

if (-not (Test-Path $OutputDirectory)) {
    New-Item -ItemType Directory -Path $OutputDirectory | Out-Null
}

$assets = @(
    @{ Name = 'Square44x44Logo.png'; Width = 44; Height = 44; FontSize = 13 },
    @{ Name = 'Square150x150Logo.png'; Width = 150; Height = 150; FontSize = 42 },
    @{ Name = 'Square310x310Logo.png'; Width = 310; Height = 310; FontSize = 86 },
    @{ Name = 'Wide310x150Logo.png'; Width = 310; Height = 150; FontSize = 42 },
    @{ Name = 'StoreLogo.png'; Width = 50; Height = 50; FontSize = 15 },
    @{ Name = 'SplashScreen.png'; Width = 620; Height = 300; FontSize = 72 }
)

foreach ($asset in $assets) {
    $bitmap = New-Object System.Drawing.Bitmap $asset.Width, $asset.Height
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    try {
        $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
        $graphics.Clear([System.Drawing.Color]::FromArgb(30, 64, 175))

        $font = New-Object System.Drawing.Font 'Segoe UI', $asset.FontSize, ([System.Drawing.FontStyle]::Bold), ([System.Drawing.GraphicsUnit]::Pixel)
        $brush = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::White)
        $format = New-Object System.Drawing.StringFormat
        $format.Alignment = [System.Drawing.StringAlignment]::Center
        $format.LineAlignment = [System.Drawing.StringAlignment]::Center
        $rectangle = New-Object System.Drawing.RectangleF 0, 0, $asset.Width, $asset.Height

        $graphics.DrawString('LVT', $font, $brush, $rectangle, $format)

        $path = Join-Path $OutputDirectory $asset.Name
        if (Test-Path -LiteralPath $path) {
            Remove-Item -LiteralPath $path -Force
        }

        $bitmap.Save($path, [System.Drawing.Imaging.ImageFormat]::Png)
        Write-Host "Generated $path"
    }
    finally {
        if ($format) { $format.Dispose() }
        if ($brush) { $brush.Dispose() }
        if ($font) { $font.Dispose() }
        $graphics.Dispose()
        $bitmap.Dispose()
    }
}
