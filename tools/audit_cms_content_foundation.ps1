$ErrorActionPreference = 'Stop'

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot '..')
$migrationPath = Join-Path $repoRoot 'backend/EnglishVoiceTutor.Api/Migrations/20260603120000_AddCmsContentFoundation.cs'
$auditMetadataMigrationPath = Join-Path $repoRoot 'backend/EnglishVoiceTutor.Api/Migrations/20260604121000_AddCmsDraftSaveAuditMetadata.cs'
$modelSnapshotPath = Join-Path $repoRoot 'backend/EnglishVoiceTutor.Api/Migrations/AppDbContextModelSnapshot.cs'
$appDbContextPath = Join-Path $repoRoot 'backend/EnglishVoiceTutor.Api/Data/AppDbContext.cs'
$contentAuditLogEntityPath = Join-Path $repoRoot 'backend/EnglishVoiceTutor.Api/Data/Entities/Cms/ContentAuditLogEntity.cs'
$importServicePath = Join-Path $repoRoot 'backend/EnglishVoiceTutor.Api/Services/Cms/CmsContentImportService.cs'
$validationServicePath = Join-Path $repoRoot 'backend/EnglishVoiceTutor.Api/Services/Cms/CmsContentValidationService.cs'
$smokeImportPath = Join-Path $repoRoot 'tools/smoke_cms_content_import.ps1'
$publishedReadServicePath = Join-Path $repoRoot 'backend/EnglishVoiceTutor.Api/Services/Cms/CmsPublishedContentService.cs'
$publishedReadInterfacePath = Join-Path $repoRoot 'backend/EnglishVoiceTutor.Api/Services/Cms/ICmsPublishedContentService.cs'
$publishedReadModelsPath = Join-Path $repoRoot 'backend/EnglishVoiceTutor.Api/Services/Cms/CmsPublishedContentModels.cs'
$cmsContentOptionsPath = Join-Path $repoRoot 'backend/EnglishVoiceTutor.Api/Options/CmsContentOptions.cs'
$smokePublishedReadPath = Join-Path $repoRoot 'tools/smoke_cms_published_content_read.ps1'
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

if (-not (Test-Path $auditMetadataMigrationPath)) {
    throw "CMS draft-save audit metadata migration is missing or unrecognized by filename: $auditMetadataMigrationPath"
}

foreach ($requiredPath in @(
    $modelSnapshotPath,
    $appDbContextPath,
    $contentAuditLogEntityPath
)) {
    if (-not (Test-Path $requiredPath)) {
        throw "CMS draft-save audit metadata consistency file is missing: $requiredPath"
    }
}

$auditMetadataMigrationText = Get-Content $auditMetadataMigrationPath -Raw
foreach ($expectedMigrationText in @(
    '[DbContext(typeof(AppDbContext))]',
    '[Migration("20260604121000_AddCmsDraftSaveAuditMetadata")]',
    'public partial class AddCmsDraftSaveAuditMetadata : Migration',
    'IX_cms_content_audit_logs_ContentPackSlug_CreatedAtUtc',
    'IX_cms_content_audit_logs_EntityType_CreatedAtUtc',
    'IX_cms_content_audit_logs_StableKey_CreatedAtUtc'
)) {
    if ($auditMetadataMigrationText -notmatch [regex]::Escape($expectedMigrationText)) {
        throw "CMS draft-save audit metadata migration is not EF-recognizable or is missing expected schema/index text: $expectedMigrationText"
    }
}

$modelSnapshotText = Get-Content $modelSnapshotPath -Raw
$appDbContextText = Get-Content $appDbContextPath -Raw
$contentAuditLogEntityText = Get-Content $contentAuditLogEntityPath -Raw
foreach ($expectedAuditColumn in @(
    'ActorEmail',
    'ContentPackSlug',
    'Source',
    'StableKey',
    'Status'
)) {
    if ($auditMetadataMigrationText -notmatch ('name: "' + [regex]::Escape($expectedAuditColumn) + '"')) {
        throw "CMS draft-save audit metadata migration does not add cms_content_audit_logs.$expectedAuditColumn. Apply/fix migration 20260604121000_AddCmsDraftSaveAuditMetadata."
    }

    if ($modelSnapshotText -notmatch ('Property<string>\("' + [regex]::Escape($expectedAuditColumn) + '"\)')) {
        throw "CMS EF model snapshot is missing ContentAuditLogEntity.$expectedAuditColumn."
    }

    if ($contentAuditLogEntityText -notmatch [regex]::Escape($expectedAuditColumn)) {
        throw "CMS audit entity is missing ContentAuditLogEntity.$expectedAuditColumn."
    }

    if ($appDbContextText -notmatch ('log => log\.' + [regex]::Escape($expectedAuditColumn))) {
        throw "AppDbContext does not map ContentAuditLogEntity.$expectedAuditColumn."
    }
}

foreach ($requiredPath in @(
    $importServicePath,
    $validationServicePath,
    $smokeImportPath,
    $publishedReadServicePath,
    $publishedReadInterfacePath,
    $publishedReadModelsPath,
    $cmsContentOptionsPath,
    $smokePublishedReadPath
)) {
    if (-not (Test-Path $requiredPath)) {
        throw "CMS content foundation/read-path file is missing: $requiredPath"
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
foreach ($expectedConstant in @(
    'static-json-v1',
    'Static JSON Baseline',
    'ImportPublished',
    'CmsContent:ReadPublishedSnapshotEnabled',
    'CmsContent:ContentPackSlug',
    'CmsContent:FallbackToStaticJson',
    'CmsPublishedSnapshot',
    'StaticJsonFallback',
    'CmsSnapshotHashMismatch'
)) {
    if ($constantsText -notmatch [regex]::Escape($expectedConstant)) {
        throw "CMS import constant is missing: $expectedConstant"
    }
}

Write-Host 'CMS content foundation audit passed.'
