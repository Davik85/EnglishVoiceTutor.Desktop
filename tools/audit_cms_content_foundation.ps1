$ErrorActionPreference = 'Stop'

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot '..')
$migrationPath = Join-Path $repoRoot 'backend/EnglishVoiceTutor.Api/Migrations/20260603120000_AddCmsContentFoundation.cs'
$importServicePath = Join-Path $repoRoot 'backend/EnglishVoiceTutor.Api/Services/Cms/CmsContentImportService.cs'
$validationServicePath = Join-Path $repoRoot 'backend/EnglishVoiceTutor.Api/Services/Cms/CmsContentValidationService.cs'
$smokeImportPath = Join-Path $repoRoot 'tools/smoke_cms_content_import.ps1'
$entityRoot = Join-Path $repoRoot 'backend/EnglishVoiceTutor.Api/Data/Entities/Cms'
$desktopCmsReferences = Get-ChildItem -Path $repoRoot -Recurse -File |
    Where-Object {
        $_.FullName -notmatch '[\\/]backend[\\/]' -and
        $_.FullName -notmatch '[\\/]\.git[\\/]' -and
        $_.FullName -notmatch '[\\/]bin[\\/]' -and
        $_.FullName -notmatch '[\\/]obj[\\/]' -and
        $_.FullName -notmatch '[\\/]docs[\\/]' -and
        $_.FullName -notmatch '[\\/]tools[\\/]'
    } |
    Select-String -Pattern 'cms_content_packs|cms_lesson_topics|cms_lesson_scenarios|cms_prompt_templates|cms_tutor_behavior_profiles|cms_content_versions|cms_published_content_snapshots|cms_content_audit_logs'

$requiredEntities = @(
    'ContentPackEntity.cs',
    'CmsLessonTopicEntity.cs',
    'CmsLessonScenarioEntity.cs',
    'PromptTemplateEntity.cs',
    'TutorBehaviorProfileEntity.cs',
    'ContentVersionEntity.cs',
    'PublishedContentSnapshotEntity.cs',
    'ContentAuditLogEntity.cs'
)

if (-not (Test-Path $migrationPath)) {
    throw "CMS foundation migration is missing: $migrationPath"
}

foreach ($requiredPath in @($importServicePath, $validationServicePath, $smokeImportPath)) {
    if (-not (Test-Path $requiredPath)) {
        throw "CMS import foundation file is missing: $requiredPath"
    }
}

foreach ($entityFile in $requiredEntities) {
    $path = Join-Path $entityRoot $entityFile
    if (-not (Test-Path $path)) {
        throw "CMS entity file is missing: $path"
    }
}

if ($desktopCmsReferences) {
    $desktopCmsReferences | ForEach-Object { Write-Host $_ }
    throw 'Desktop code must not reference CMS table names directly.'
}

$lessonJsonChanges = git -C $repoRoot status --short -- Content/Lessons
if ($lessonJsonChanges) {
    Write-Host $lessonJsonChanges
    throw 'Lesson JSON files must remain unchanged for CMS static import foundation work.'
}

$constantsPath = Join-Path $repoRoot 'backend/EnglishVoiceTutor.Api/Data/CmsContentConstants.cs'
$constantsText = Get-Content $constantsPath -Raw
foreach ($expectedConstant in @('static-json-v1', 'Static JSON Baseline', 'ImportPublished')) {
    if ($constantsText -notmatch [regex]::Escape($expectedConstant)) {
        throw "CMS import constant is missing: $expectedConstant"
    }
}

Write-Host 'CMS content foundation audit passed.'
