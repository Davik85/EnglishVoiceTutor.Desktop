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
    "cms-topic-form", "cms-selected-topic-identity", "cms-topic-title", "cms-topic-description", "cms-topic-sort-order", "cms-topic-is-active", "cms-topic-save-button", "cms-topic-reset-button", "cms-topic-message", "cms-topic-publish-discovery",
    "cms-scenario-form", "cms-selected-scenario-identity", "cms-scenario-title", "cms-scenario-description", "cms-scenario-setup-message", "cms-scenario-is-active", "cms-scenario-first-bot-message-lines", "cms-scenario-soft-wrap-turn", "cms-scenario-final-message-turn", "cms-scenario-context-option-lines", "cms-scenario-valid-context-keywords-lines", "cms-scenario-custom-context-rules-lines", "cms-scenario-invalid-context-redirect", "cms-scenario-goal-text", "cms-scenario-can-do-lines", "cms-scenario-opening-text", "cms-scenario-first-user-task", "cms-scenario-guided-follow-up-lines", "cms-scenario-ai-instruction-lines", "cms-scenario-wrap-up-message", "cms-scenario-final-message", "cms-scenario-hint-example", "cms-scenario-structured-save-button", "cms-scenario-structured-reset-button", "cms-scenario-validate-structured-button", "cms-scenario-structured-status", "cms-scenario-save-button", "cms-scenario-reset-button", "cms-scenario-message", "cms-scenario-publish-discovery", "cms-scenario-json-publish-discovery",
    "cms-prompt-template-form", "cms-selected-prompt-template-identity", "cms-prompt-template-body", "cms-prompt-template-is-active", "cms-prompt-template-save-button", "cms-prompt-template-reset-button", "cms-prompt-template-message", "cms-prompt-template-publish-discovery",
    "cms-tutor-profile-form", "cms-selected-tutor-profile-identity", "cms-tutor-profile-display-name", "cms-tutor-profile-communication-style-json", "cms-tutor-profile-safety-notes-json", "cms-tutor-profile-is-active", "cms-tutor-profile-save-button", "cms-tutor-profile-reset-button", "cms-tutor-profile-message", "cms-tutor-profile-publish-discovery",
    "cms-run-validation-button", "cms-validation-result", "cms-load-preview-button", "cms-preview-summary",
    "cms-publish-section", "cms-unpublished-draft-notice", "cms-publish-instructions", "cms-publish-error-details", "cms-load-versions-button", "cms-publish-change-summary", "cms-publish-button", "cms-restore-version-select", "cms-restore-reason", "cms-restore-button", "cms-versions-list", "cms-load-audit-button", "cms-audit-entity-type", "cms-audit-stable-key", "cms-audit-limit", "cms-audit-show-smoke", "cms-audit-loading", "cms-audit-error", "cms-audit-list"
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
$requiredJsFunctionRefs = @("updateSelectedUserHeader", "updateUserRequiredEmptyStates", "applySelectedUserPayload", "clearSelectedUserState", "selectCmsSubTab", "loadCmsContentPacks", "renderCmsContentPackSummary", "renderCmsTopicsTable", "renderCmsScenariosTable", "renderCmsPromptTemplatesTable", "renderCmsTutorProfilesTable", "validateCmsStructuredScenarioInput", "mergeCmsStructuredScenarioFieldsToDefinition", "runCmsValidation", "loadCmsPreviewSummary", "loadCmsVersions", "publishCmsDraft", "restoreCmsVersion", "loadCmsAuditEntries", "renderCmsAuditEntries", "goToCmsPublishSection", "showCmsPublishDiscoveryForMessage", "extractCmsBackendMessages", "renderCmsPublishErrorDetails", "clearCmsPublishErrorDetails", "isCmsSmokeAuditEntry", "shouldShowCmsSmokeAuditEntries")
$forbiddenJsStorageTokens = @("localStorage", "sessionStorage")

$requiredCssSelectors = @("admin-shell", "admin-sidebar", "admin-tab-button", "tab-panel", "selected-user-summary", "empty-state-card", "compact-table", "cms-grid-two", "cms-toolbar", "cms-sub-tabs", "cms-sub-tab-button", "cms-sub-panel", "cms-workspace-grid", "cms-json-output", "cms-selectable-row", "cms-selected-row", "cms-action-column", "cms-select-button", "cms-scenario-structured-editor", "cms-fieldset", "cms-publish-discovery", "cms-publish-notice", "cms-publish-instructions", "cms-publish-error-details", "cms-publish-focus", "cms-scenario-structured-save-row", "cms-audit-smoke-toggle")

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
    if ($indexContent.IndexOf('placeholder="Optional summary"', [System.StringComparison]::OrdinalIgnoreCase) -ge 0) {
        Add-Error "index.html: publish change summary must not be labelled optional because backend requires changeSummary when changed content is published."
    }
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
    foreach ($publishDiscoveryMarker in @('Draft saved. To apply this content to runtime, publish the current draft.', 'Go to Publish', 'Draft changes are saved but not visible to runtime until published.', '1. Enter a short change summary. 2. Click Publish current draft. 3. Confirm publishing.', 'Publish change summary', 'Required before publishing from the Admin CMS browser UI', 'data-cms-publish-error-details="true"', 'Publish current draft', 'data-cms-publish-discovery="true"', 'cms-scenario-json-publish-discovery', 'Show smoke/test entries')) {
        if ($indexContent.IndexOf($publishDiscoveryMarker, [System.StringComparison]::Ordinal) -lt 0) {
            Add-Error "index.html: missing CMS publish discoverability marker '$publishDiscoveryMarker'."
        }
    }

    $scenarioStructuredPattern = [regex]'id="cms-scenario-structured-save-button"[\s\S]*?id="cms-scenario-publish-discovery"[\s\S]*?Go to Publish'
    if (-not $scenarioStructuredPattern.IsMatch($indexContent)) { Add-Error "index.html: scenario structured save area is not followed by a local Go to Publish callout." }
    $scenarioJsonPattern = [regex]'data-cms-scenario-save-area="advanced-json"[\s\S]*?id="cms-scenario-json-publish-discovery"[\s\S]*?Go to Publish'
    if (-not $scenarioJsonPattern.IsMatch($indexContent)) { Add-Error "index.html: scenario Advanced JSON save area is not followed by a local Go to Publish callout." }
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
    if ($jsContent.IndexOf("Enter a publish change summary before publishing.", [System.StringComparison]::Ordinal) -lt 0 -or $jsContent.IndexOf("if (!summary)", [System.StringComparison]::Ordinal) -lt 0) {
        Add-Error "admin.js: missing unconditional local publish summary validation before publish."
    }
    foreach ($publishErrorMarker in @("Publish failed", "source.title", "source.detail", "validation?.errors", "validation?.warnings")) {
        if ($jsContent.IndexOf($publishErrorMarker, [System.StringComparison]::Ordinal) -lt 0) {
            Add-Error "admin.js: missing detailed publish error marker '$publishErrorMarker'."
        }
    }
    foreach ($auditSmokeMarker in @("isCmsSmokeAuditEntry", "shouldShowCmsSmokeAuditEntries", "cmsAuditShowSmokeInput")) {
        if ($jsContent.IndexOf($auditSmokeMarker, [System.StringComparison]::Ordinal) -lt 0) {
            Add-Error "admin.js: missing audit smoke filter marker '$auditSmokeMarker'."
        }
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
