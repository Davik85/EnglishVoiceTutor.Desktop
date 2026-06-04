Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$errors = New-Object System.Collections.Generic.List[string]

$indexPath = Join-Path $repoRoot "backend/EnglishVoiceTutor.Api/wwwroot/admin/index.html"
$jsPath = Join-Path $repoRoot "backend/EnglishVoiceTutor.Api/wwwroot/admin/admin.js"
$cssPath = Join-Path $repoRoot "backend/EnglishVoiceTutor.Api/wwwroot/admin/admin.css"
$checkedFiles = @($indexPath, $jsPath, $cssPath)

$requiredTabButtonIds = @(
    "tab-button-overview", "tab-button-user-lookup", "tab-button-premium", "tab-button-free-lesson", "tab-button-audit-log", "tab-button-cms-content", "tab-button-system"
)
$requiredTabPanelIds = @(
    "tab-panel-overview", "tab-panel-user-lookup", "tab-panel-premium", "tab-panel-free-lesson", "tab-panel-audit-log", "tab-panel-cms-content", "tab-panel-system"
)
$requiredLookupIds = @("lookup-form", "lookup-email", "search-user-button", "lookup-loading", "lookup-error", "lookup-result")
$requiredPremiumLookupIds = @("premium-lookup-form", "premium-lookup-email", "premium-search-user-button", "premium-lookup-loading", "premium-lookup-error")
$requiredPremiumControlIds = @(
    "premium-empty-state", "premium-content", "premium-entitlement-schedule-result", "active-entitlements-result", "grant-card", "grant-form", "grant-duration-days", "grant-reason", "grant-button", "revoke-card", "revoke-form", "revoke-entitlement-id", "revoke-entitlement-preview", "revoke-reason", "revoke-button"
)
$requiredFreeLessonLookupIds = @("free-lesson-lookup-form", "free-lesson-lookup-email", "free-lesson-search-user-button", "free-lesson-lookup-loading", "free-lesson-lookup-error")
$requiredFreeLessonResetIds = @("free-lesson-empty-state", "free-lesson-reset-card", "free-lesson-reset-form", "free-lesson-reset-usage-date", "free-lesson-reset-reason", "free-lesson-reset-button")
$requiredAuditIds = @("audit-empty-state", "audit-card", "audit-limit", "load-audit-button", "audit-result")
$requiredCmsSubTabIds = @(
    "cms-sub-tab-button-overview", "cms-sub-tab-button-topics", "cms-sub-tab-button-scenarios", "cms-sub-tab-button-prompts", "cms-sub-tab-button-tutors", "cms-sub-tab-button-validation-preview", "cms-sub-tab-button-versions-publish", "cms-sub-tab-button-audit"
)
$requiredCmsSubPanelIds = @(
    "cms-sub-panel-overview", "cms-sub-panel-topics", "cms-sub-panel-scenarios", "cms-sub-panel-prompts", "cms-sub-panel-tutors", "cms-sub-panel-validation-preview", "cms-sub-panel-versions-publish", "cms-sub-panel-audit"
)
$requiredCmsIds = @(
    "cms-load-content-packs-button", "cms-content-pack-select", "cms-refresh-button", "cms-loading", "cms-error", "cms-success",
    "cms-content-pack-summary", "cms-summary-slug", "cms-summary-name", "cms-summary-status", "cms-summary-topic-count", "cms-summary-scenario-count", "cms-summary-prompt-template-count", "cms-summary-tutor-profile-count", "cms-summary-published-version",
    "cms-topics-list", "cms-topic-filter", "cms-scenarios-list", "cms-scenario-filter", "cms-scenario-topic-filter", "cms-prompt-templates-list", "cms-tutor-profiles-list",
    "cms-topic-form", "cms-selected-topic-identity", "cms-topic-title", "cms-topic-description", "cms-topic-sort-order", "cms-topic-is-active", "cms-topic-save-button", "cms-topic-reset-button", "cms-topic-message",
    "cms-scenario-form", "cms-selected-scenario-identity", "cms-scenario-title", "cms-scenario-description", "cms-scenario-setup-message", "cms-scenario-is-active", "cms-scenario-save-button", "cms-scenario-reset-button", "cms-scenario-message",
    "cms-prompt-template-form", "cms-selected-prompt-template-identity", "cms-prompt-template-body", "cms-prompt-template-is-active", "cms-prompt-template-save-button", "cms-prompt-template-reset-button", "cms-prompt-template-message",
    "cms-tutor-profile-form", "cms-selected-tutor-profile-identity", "cms-tutor-profile-display-name", "cms-tutor-profile-communication-style-json", "cms-tutor-profile-safety-notes-json", "cms-tutor-profile-is-active", "cms-tutor-profile-save-button", "cms-tutor-profile-reset-button", "cms-tutor-profile-message",
    "cms-run-validation-button", "cms-validation-result", "cms-load-preview-button", "cms-preview-summary",
    "cms-load-versions-button", "cms-publish-change-summary", "cms-publish-button", "cms-restore-version-select", "cms-restore-reason", "cms-restore-button", "cms-versions-list", "cms-load-audit-button", "cms-audit-limit", "cms-audit-list"
)
$requiredSystemIds = @("capabilities-list")

$requiredJsEndpoints = @(
    "/api/auth/login", "/api/admin/capabilities", "/api/admin/users/by-email", "/api/admin/users/{userId}/audit-actions", "/api/admin/users/{userId}/premium-grants", "/api/admin/users/{userId}/premium-grants/{entitlementId}/revoke", "/api/admin/users/{userId}/free-lesson-allowance/reset",
    "/api/admin/dev/cms/content-packs", "/api/admin/dev/cms/content-packs/{slug}", "/api/admin/dev/cms/content-packs/{slug}/topics", "/api/admin/dev/cms/content-packs/{slug}/topics/{topicId}",
    "/api/admin/dev/cms/content-packs/{slug}/scenarios", "/api/admin/dev/cms/content-packs/{slug}/scenarios/{scenarioId}",
    "/api/admin/dev/cms/content-packs/{slug}/prompt-templates", "/api/admin/dev/cms/content-packs/{slug}/prompt-templates/{templateId}",
    "/api/admin/dev/cms/content-packs/{slug}/tutor-behavior-profiles", "/api/admin/dev/cms/content-packs/{slug}/tutor-behavior-profiles/{profileId}",
    "/api/admin/dev/cms/content-packs/{slug}/validate", "/api/admin/dev/cms/content-packs/{slug}/preview-summary", "/api/admin/dev/cms/content-packs/{slug}/versions", "/api/admin/dev/cms/content-packs/{slug}/publish", "/api/admin/dev/cms/content-packs/{slug}/versions/{versionNumber}/restore", "/api/admin/dev/cms/content-packs/{slug}/audit-entries"
)
$requiredJsLookupRefs = @("user-lookup", "premium", "free-lesson", "cms-content")
$requiredJsFunctionRefs = @("updateSelectedUserHeader", "updateUserRequiredEmptyStates", "applySelectedUserPayload", "clearSelectedUserState", "selectCmsSubTab", "loadCmsContentPacks", "renderCmsContentPackSummary", "renderCmsTopicsTable", "renderCmsScenariosTable", "renderCmsPromptTemplatesTable", "renderCmsTutorProfilesTable", "runCmsValidation", "loadCmsPreviewSummary", "loadCmsVersions", "publishCmsDraft", "restoreCmsVersion", "loadCmsAuditEntries", "renderCmsAuditEntries")
$forbiddenJsStorageTokens = @("localStorage", "sessionStorage")

$requiredCssSelectors = @("admin-shell", "admin-sidebar", "admin-tab-button", "tab-panel", "selected-user-summary", "empty-state-card", "compact-table", "cms-grid-two", "cms-toolbar", "cms-sub-tabs", "cms-sub-tab-button", "cms-sub-panel", "cms-workspace-grid", "cms-json-output", "cms-selectable-row", "cms-selected-row", "cms-action-column", "cms-select-button")

function Add-Error([string]$message) { $errors.Add($message) }

function Assert-ContainsOnceById {
    param([hashtable]$idCounts, [string]$id)
    $matchCount = 0
    if ($idCounts.ContainsKey($id)) {
        $matchCount = [int]$idCounts[$id]
    }
    if ($matchCount -ne 1) {
        Add-Error "index.html: expected exactly one id '$id', found $matchCount."
    }
}

if (-not (Test-Path -LiteralPath $indexPath)) {
    Add-Error "Missing file: $indexPath"
} else {
    $indexContent = Get-Content -LiteralPath $indexPath -Raw
    $idAttributeRegex = [regex]'\sid\s*=\s*(["''])(?<id>[^"'']+)\1'
    $allIdMatches = $idAttributeRegex.Matches($indexContent)
    $idCounts = @{}
    foreach ($match in $allIdMatches) {
        $idValue = $match.Groups["id"].Value
        if ($idCounts.ContainsKey($idValue)) {
            $idCounts[$idValue] = [int]$idCounts[$idValue] + 1
        } else {
            $idCounts[$idValue] = 1
        }
    }
    foreach ($entry in $idCounts.GetEnumerator()) {
        if ([int]$entry.Value -gt 1) {
            Add-Error "index.html: duplicate id '$($entry.Key)' found $($entry.Value) times."
        }
    }

    $requiredIndexIds = @(
        $requiredTabButtonIds + $requiredTabPanelIds + $requiredLookupIds + $requiredPremiumLookupIds +
        $requiredPremiumControlIds + $requiredFreeLessonLookupIds + $requiredFreeLessonResetIds + $requiredAuditIds + $requiredCmsSubTabIds + $requiredCmsSubPanelIds + $requiredCmsIds + $requiredSystemIds
    )
    foreach ($id in $requiredIndexIds) {
        Assert-ContainsOnceById -idCounts $idCounts -id $id
    }

    foreach ($cmsMarker in @('data-cms-sub-tabs="true"', 'data-cms-sub-panel="overview"', 'data-cms-sub-panel="topics"', 'data-cms-sub-panel="scenarios"', 'data-cms-sub-panel="prompts"', 'data-cms-sub-panel="tutors"', 'data-cms-sub-panel="validation-preview"', 'data-cms-sub-panel="versions-publish"', 'data-cms-sub-panel="audit"')) {
        if ($indexContent.IndexOf($cmsMarker, [System.StringComparison]::Ordinal) -lt 0) {
            Add-Error "index.html: missing CMS sub-tab marker '$cmsMarker'."
        }
    }
}


if (-not (Test-Path -LiteralPath $jsPath)) {
    Add-Error "Missing file: $jsPath"
} else {
    $jsContent = Get-Content -LiteralPath $jsPath -Raw
    foreach ($endpoint in $requiredJsEndpoints) {
        if ($jsContent.IndexOf($endpoint, [System.StringComparison]::Ordinal) -lt 0) {
            Add-Error "admin.js: missing endpoint string '$endpoint'."
        }
    }
    foreach ($lookupRef in $requiredJsLookupRefs) {
        if ($jsContent.IndexOf($lookupRef, [System.StringComparison]::Ordinal) -lt 0) {
            Add-Error "admin.js: missing lookup reference '$lookupRef'."
        }
    }
    foreach ($fnRef in $requiredJsFunctionRefs) {
        if ($jsContent.IndexOf($fnRef, [System.StringComparison]::Ordinal) -lt 0) {
            Add-Error "admin.js: missing function reference '$fnRef'."
        }
    }
    if ($jsContent.IndexOf("confirm(", [System.StringComparison]::Ordinal) -lt 0) {
        Add-Error "admin.js: missing publish/restore confirm() safety guard."
    }
    foreach ($forbiddenToken in $forbiddenJsStorageTokens) {
        if ($jsContent.IndexOf($forbiddenToken, [System.StringComparison]::OrdinalIgnoreCase) -ge 0) {
            Add-Error "admin.js: forbidden token '$forbiddenToken' found. JWT must remain memory-only."
        }
    }
}

foreach ($file in $checkedFiles) {
    if (Test-Path -LiteralPath $file) {
        $content = Get-Content -LiteralPath $file -Raw
        foreach ($secretMarker in @('sk-', 'api_key', 'apikey', 'client_secret', 'webhook_secret', 'smtp_password')) {
            if ($content.IndexOf($secretMarker, [System.StringComparison]::OrdinalIgnoreCase) -ge 0) {
                Add-Error "$(Split-Path -Leaf $file): possible secret marker '$secretMarker' found."
            }
        }
    }
}


if (-not (Test-Path -LiteralPath $cssPath)) {
    Add-Error "Missing file: $cssPath"
} else {
    $cssContent = Get-Content -LiteralPath $cssPath -Raw
    foreach ($selector in $requiredCssSelectors) {
        if ($cssContent.IndexOf($selector, [System.StringComparison]::Ordinal) -lt 0) {
            Add-Error "admin.css: missing selector/style reference '$selector'."
        }
    }
}

$scriptPath = $MyInvocation.MyCommand.Path
$scriptSource = Get-Content -LiteralPath $scriptPath -Raw
$forbiddenRegexFragment = "[{0}\{1}]" -f "'", '"'
if ($scriptSource.IndexOf($forbiddenRegexFragment, [System.StringComparison]::Ordinal) -ge 0) {
    Add-Error "audit_admin_shell.ps1: forbidden fragment $forbiddenRegexFragment found. Use quote-safe single-quoted regex patterns."
}

Write-Host "Admin shell audit"
Write-Host "Repository: $repoRoot"
Write-Host "Checked files:"
foreach ($file in $checkedFiles) { Write-Host " - $file" }

if ($errors.Count -eq 0) {
    Write-Host "Status: PASSED" -ForegroundColor Green
    exit 0
}

Write-Host "Status: FAILED" -ForegroundColor Red
Write-Host "Errors:" -ForegroundColor Red
foreach ($errorMessage in $errors) {
    Write-Host " - $errorMessage" -ForegroundColor Red
}
exit 1
