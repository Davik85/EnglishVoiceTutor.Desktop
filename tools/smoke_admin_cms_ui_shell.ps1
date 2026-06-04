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
        'Development-only/admin-only content editing shell. Runtime still uses static JSON by default.',
        'cms-content-pack-select',
        'cms-sub-tab-button-overview',
        'cms-sub-tab-button-topics',
        'cms-sub-tab-button-scenarios',
        'cms-sub-tab-button-prompts',
        'cms-sub-tab-button-tutors',
        'cms-sub-tab-button-validation-preview',
        'cms-sub-tab-button-versions-publish',
        'cms-sub-panel-overview',
        'cms-sub-panel-topics',
        'cms-sub-panel-scenarios',
        'cms-sub-panel-prompts',
        'cms-sub-panel-tutors',
        'cms-sub-panel-validation-preview',
        'cms-sub-panel-versions-publish',
        'Overview',
        'Topics',
        'Scenarios',
        'Prompts',
        'Tutors',
        'Validation &amp; Preview',
        'Versions &amp; Publish',
        'cms-topics-list',
        'cms-topic-filter',
        'cms-scenarios-list',
        'cms-scenario-filter',
        'cms-scenario-topic-filter',
        'cms-prompt-templates-list',
        'cms-tutor-profiles-list',
        'cms-run-validation-button',
        'cms-load-preview-button',
        'cms-publish-button',
        'cms-restore-button',
        'cms-topic-title',
        'cms-topic-description',
        'cms-topic-sort-order',
        'cms-scenario-title',
        'cms-scenario-description',
        'cms-scenario-setup-message',
        'cms-prompt-template-body',
        'cms-tutor-profile-display-name',
        'cms-tutor-profile-communication-style-json',
        'cms-tutor-profile-safety-notes-json'
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
        'confirm(',
        'selectCmsSubTab',
        'renderCmsTopicsTable',
        'renderCmsScenariosTable',
        'renderCmsPromptTemplatesTable',
        'renderCmsTutorProfilesTable',
        'cms-selectable-row',
        'cms-selected-row',
        'aria-current',
        'event.stopPropagation()',
        'button.type = "button"'
    )) { Assert-FileContains -path $jsPath -needle $needle }

    foreach ($needle in @('cms-grid-two', 'cms-toolbar', 'cms-json-output', 'cms-lifecycle-actions', 'cms-sub-tabs', 'cms-sub-tab-button', 'cms-sub-panel', 'cms-workspace-grid', 'cms-selectable-row', 'cms-selected-row', 'cms-action-column', 'cms-select-button')) {
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
