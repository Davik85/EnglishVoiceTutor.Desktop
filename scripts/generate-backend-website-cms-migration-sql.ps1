param(
    [string]$FromMigration = '20260620165657_AddAdminRoleAssignmentPersistence',

    [string]$ToMigration = '20260625090000_AddWebsiteCmsLegalContentFoundation',

    # Generates SQL for the Website CMS persistence table: website_cms_sections.
    [string]$OutputPath = '',

    [switch]$Idempotent
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
$projectPath = Join-Path $repoRoot 'backend/EnglishVoiceTutor.Api/EnglishVoiceTutor.Api.csproj'
$sqlDir = Join-Path $repoRoot 'artifacts/sql/backend'

if ([string]::IsNullOrWhiteSpace($OutputPath)) {
    $suffix = if ($Idempotent) { 'idempotent' } else { 'from-20260620165657' }
    $OutputPath = Join-Path $sqlDir "20260625090000_AddWebsiteCmsLegalContentFoundation.$suffix.sql"
}

if (-not (Test-Path -LiteralPath $projectPath)) {
    throw "Backend project was not found: $projectPath"
}

New-Item -ItemType Directory -Path (Split-Path -Parent $OutputPath) -Force | Out-Null

$scriptArgs = @(
    'ef',
    'migrations',
    'script',
    '--project',
    $projectPath,
    '--startup-project',
    $projectPath,
    '--context',
    'AppDbContext',
    '--output',
    $OutputPath
)

if ($Idempotent) {
    $scriptArgs += '--idempotent'
}
else {
    $scriptArgs += $FromMigration
    $scriptArgs += $ToMigration
}

Write-Host "Generating reviewed SQL migration script for $ToMigration."
Write-Host "Project: $projectPath"
Write-Host "Output: $OutputPath"
Write-Host "Running: dotnet $($scriptArgs -join ' ')"
& dotnet @scriptArgs
if ($LASTEXITCODE -ne 0) {
    throw "dotnet ef migrations script failed with exit code $LASTEXITCODE."
}

if (-not (Test-Path -LiteralPath $OutputPath)) {
    throw "Migration SQL script was not created: $OutputPath"
}

Write-Host "Migration SQL script created successfully."
Write-Host "Review the SQL before applying it to production. Do not commit generated SQL artifacts."
Write-Host "This script does not apply SQL to any database, does not connect to production, and does not read or print database secrets."
