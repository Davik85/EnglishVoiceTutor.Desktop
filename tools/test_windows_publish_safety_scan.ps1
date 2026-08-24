$ErrorActionPreference = "Stop"

$packageScriptPath = Join-Path $PSScriptRoot "..\scripts\package-windows-inno-release.ps1"
$tokens = $null
$parseErrors = $null
$scriptAst = [System.Management.Automation.Language.Parser]::ParseFile(
    $packageScriptPath,
    [ref]$tokens,
    [ref]$parseErrors)

if ($parseErrors.Count -gt 0) {
    throw "Package script contains PowerShell parse errors: $($parseErrors -join '; ')"
}

$safetyFunctionAst = $scriptAst.Find({
        param($node)
        $node -is [System.Management.Automation.Language.FunctionDefinitionAst] -and
        $node.Name -eq "Assert-PublishOutputIsSafe"
    }, $true)

if (-not $safetyFunctionAst) {
    throw "Assert-PublishOutputIsSafe was not found in $packageScriptPath."
}

Invoke-Expression $safetyFunctionAst.Extent.Text

function Assert-SafetyScanFailsWith {
    param(
        [string]$PublishPath,
        [string]$ExpectedMessage
    )

    try {
        Assert-PublishOutputIsSafe -PublishPath $PublishPath
    }
    catch {
        if ($_.Exception.Message -notlike "*$ExpectedMessage*") {
            throw "Safety scan failed with an unexpected message: $($_.Exception.Message)"
        }

        return
    }

    throw "Safety scan unexpectedly accepted $PublishPath."
}

$testRoot = Join-Path ([System.IO.Path]::GetTempPath()) ("lvt-publish-safety-" + [Guid]::NewGuid().ToString("N"))
New-Item -ItemType Directory -Path $testRoot | Out-Null

try {
    $binaryPublishPath = Join-Path $testRoot "binary"
    New-Item -ItemType Directory -Path $binaryPublishPath | Out-Null
    [System.IO.File]::WriteAllBytes(
        (Join-Path $binaryPublishPath "PresentationCore.dll"),
        [System.Text.Encoding]::UTF8.GetBytes("opaque runtime bytes localhost Backend URL"))
    Assert-PublishOutputIsSafe -PublishPath $binaryPublishPath

    $textPublishPath = Join-Path $testRoot "text"
    New-Item -ItemType Directory -Path $textPublishPath | Out-Null
    Set-Content -Path (Join-Path $textPublishPath "appsettings.json") -Value '{"backendBaseUrl":"http://localhost:5000"}' -Encoding utf8
    Assert-SafetyScanFailsWith -PublishPath $textPublishPath -ExpectedMessage "forbidden backend override/UI string 'http://localhost:5000'"

    $forbiddenFilenamePublishPath = Join-Path $testRoot "forbidden-filename"
    New-Item -ItemType Directory -Path $forbiddenFilenamePublishPath | Out-Null
    Set-Content -Path (Join-Path $forbiddenFilenamePublishPath "auth-session.json") -Value '{}' -Encoding utf8
    Assert-SafetyScanFailsWith -PublishPath $forbiddenFilenamePublishPath -ExpectedMessage "Publish output contains forbidden installer files"
}
finally {
    Remove-Item -Path $testRoot -Recurse -Force -ErrorAction SilentlyContinue
}

Write-Host "Windows publish safety scan regression checks passed."
