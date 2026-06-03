Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$indexPath = Join-Path $repoRoot "backend/EnglishVoiceTutor.Api/wwwroot/admin/index.html"
$jsPath = Join-Path $repoRoot "backend/EnglishVoiceTutor.Api/wwwroot/admin/admin.js"
$cssPath = Join-Path $repoRoot "backend/EnglishVoiceTutor.Api/wwwroot/admin/admin.css"
$errors = New-Object System.Collections.Generic.List[string]

function Add-Error([string]$message) { $errors.Add($message) }
function Assert-FileContains([string]$path, [string]$needle) {
    $content = Get-Content -LiteralPath $path -Raw
    if ($content.IndexOf($needle, [System.StringComparison]::Ordinal) -lt 0) {
        Add-Error "$(Split-Path -Leaf $path): missing '$needle'."
    }
}

foreach ($path in @($indexPath, $jsPath, $cssPath)) {
    if (-not (Test-Path -LiteralPath $path)) { Add-Error "Missing file: $path" }
}

if ($errors.Count -eq 0) {
    foreach ($needle in @(
        'tab-button-cms-content',
        'tab-panel-cms-content',
        'CMS Content',
        'Development-only content editing shell. Runtime still uses static JSON by default.',
        'cms-content-pack-select',
        'cms-topics-list',
        'cms-scenarios-list',
        'cms-prompt-templates-list',
        'cms-tutor-profiles-list',
        'cms-run-validation-button',
        'cms-load-preview-button',
        'cms-publish-button',
        'cms-restore-button'
    )) { Assert-FileContains -path $indexPath -needle $needle }

    foreach ($needle in @(
        '/api/admin/dev/cms/content-packs',
        '/api/admin/dev/cms/content-packs/{slug}/topics',
        '/api/admin/dev/cms/content-packs/{slug}/scenarios',
        '/api/admin/dev/cms/content-packs/{slug}/prompt-templates',
        '/api/admin/dev/cms/content-packs/{slug}/tutor-behavior-profiles',
        '/api/admin/dev/cms/content-packs/{slug}/validate',
        '/api/admin/dev/cms/content-packs/{slug}/preview-summary',
        '/api/admin/dev/cms/content-packs/{slug}/versions',
        '/api/admin/dev/cms/content-packs/{slug}/publish',
        '/api/admin/dev/cms/content-packs/{slug}/versions/{versionNumber}/restore',
        'confirm('
    )) { Assert-FileContains -path $jsPath -needle $needle }

    foreach ($needle in @('cms-grid-two', 'cms-toolbar', 'cms-json-output', 'cms-lifecycle-actions')) {
        Assert-FileContains -path $cssPath -needle $needle
    }

    $staticContentStatus = git -C $repoRoot status --short -- Content/Lessons Content/Prompts Content/Tutors
    if ($staticContentStatus) {
        Write-Host $staticContentStatus
        Add-Error 'Static lesson, prompt, or tutor files have local changes.'
    }
}

Write-Host "Admin CMS UI shell smoke"
Write-Host "Repository: $repoRoot"

if ($errors.Count -eq 0) {
    Write-Host "Status: PASSED" -ForegroundColor Green
    exit 0
}

Write-Host "Status: FAILED" -ForegroundColor Red
Write-Host "Errors:" -ForegroundColor Red
foreach ($errorMessage in $errors) { Write-Host " - $errorMessage" -ForegroundColor Red }
exit 1
