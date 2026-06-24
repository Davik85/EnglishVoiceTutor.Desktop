Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$errors = New-Object System.Collections.Generic.List[string]

$indexPath = Join-Path $repoRoot "backend/EnglishVoiceTutor.Api/wwwroot/admin/index.html"
$jsPath = Join-Path $repoRoot "backend/EnglishVoiceTutor.Api/wwwroot/admin/admin.js"
$cssPath = Join-Path $repoRoot "backend/EnglishVoiceTutor.Api/wwwroot/admin/admin.css"
$checkedFiles = @($indexPath, $jsPath, $cssPath)
$requiredAdminAssetVersionToken = "admin-cms-20260613-raw-json-fix"

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
    "cms-scenario-form", "cms-selected-scenario-identity", "cms-scenario-title", "cms-scenario-description", "cms-scenario-setup-message", "cms-scenario-is-active", "cms-scenario-first-bot-message-lines", "cms-scenario-context-option-lines", "cms-scenario-valid-context-keywords-lines", "cms-scenario-custom-context-rules-lines", "cms-scenario-invalid-context-redirect", "cms-scenario-goal-text", "cms-scenario-can-do-lines", "cms-scenario-opening-text", "cms-scenario-first-user-task", "cms-scenario-guided-follow-up-lines", "cms-scenario-ai-instruction-lines", "cms-scenario-wrap-up-message", "cms-scenario-final-message", "cms-scenario-hint-example", "cms-scenario-structured-save-button", "cms-scenario-structured-reset-button", "cms-scenario-validate-structured-button", "cms-scenario-structured-status", "cms-scenario-save-button", "cms-scenario-reset-button", "cms-scenario-message", "cms-scenario-structured-publish-discovery", "cms-scenario-json-publish-discovery",
    "cms-prompt-template-form", "cms-selected-prompt-template-identity", "cms-prompt-template-body", "cms-prompt-template-is-active", "cms-prompt-template-save-button", "cms-prompt-template-reset-button", "cms-prompt-template-message", "cms-prompt-template-publish-discovery",
    "cms-tutor-profile-form", "cms-selected-tutor-profile-identity", "cms-tutor-profile-display-name", "cms-tutor-profile-communication-style-json", "cms-tutor-profile-safety-notes-json", "cms-tutor-profile-is-active", "cms-tutor-profile-save-button", "cms-tutor-profile-reset-button", "cms-tutor-profile-message", "cms-tutor-profile-publish-discovery",
    "cms-validation-preview-notice-heading", "cms-run-validation-button", "cms-validation-result", "cms-load-preview-button", "cms-preview-summary", "cms-runtime-status-heading", "cms-load-runtime-status-button", "cms-runtime-status",
    "cms-publish-section", "cms-unpublished-draft-notice", "cms-publish-instructions", "cms-publish-error-details", "cms-load-versions-button", "cms-publish-change-summary", "cms-publish-button", "cms-restore-version-select", "cms-restore-reason", "cms-restore-button", "cms-versions-list", "cms-load-audit-button", "cms-audit-entity-type", "cms-audit-stable-key", "cms-audit-limit", "cms-audit-show-smoke", "cms-audit-loading", "cms-audit-error", "cms-audit-smoke-filter-status", "cms-audit-list"
)
$requiredSystemIds = @("capabilities-list")

$requiredJsEndpoints = @(
    "/api/auth/login", "/api/admin/capabilities", "/api/admin/users/by-email", "/api/admin/users/{userId}/audit-actions", "/api/admin/users/{userId}/premium-grants", "/api/admin/users/{userId}/premium-grants/{entitlementId}/revoke", "/api/admin/users/{userId}/free-lesson-allowance/reset",
    "/api/admin/dev/cms/content-packs", "/api/admin/dev/cms/content-packs/{slug}", "/api/admin/dev/cms/content-packs/{slug}/topics", "/api/admin/dev/cms/content-packs/{slug}/topics/{topicId}",
    "/api/admin/dev/cms/content-packs/{slug}/scenarios", "/api/admin/dev/cms/content-packs/{slug}/scenarios/{scenarioId}",
    "/api/admin/dev/cms/content-packs/{slug}/prompt-templates", "/api/admin/dev/cms/content-packs/{slug}/prompt-templates/{templateId}",
    "/api/admin/dev/cms/content-packs/{slug}/tutor-behavior-profiles", "/api/admin/dev/cms/content-packs/{slug}/tutor-behavior-profiles/{profileId}",
    "/api/admin/dev/cms/content-packs/{slug}/validate", "/api/admin/dev/cms/content-packs/{slug}/preview-summary", "/api/admin/dev/cms/runtime-status", "/api/admin/dev/cms/content-packs/{slug}/versions", "/api/admin/dev/cms/content-packs/{slug}/publish", "/api/admin/dev/cms/content-packs/{slug}/versions/{versionNumber}/restore", "/api/admin/dev/cms/content-packs/{slug}/audit-entries"
)
$requiredJsLookupRefs = @("user-lookup", "premium", "free-lesson", "cms-content")
$requiredJsFunctionRefs = @("updateSelectedUserHeader", "updateUserRequiredEmptyStates", "applySelectedUserPayload", "clearSelectedUserState", "selectCmsSubTab", "loadCmsContentPacks", "renderCmsContentPackSummary", "renderCmsTopicsTable", "renderCmsScenariosTable", "renderCmsPromptTemplatesTable", "renderCmsTutorProfilesTable", "validateCmsStructuredScenarioInput", "mergeCmsStructuredScenarioFieldsToDefinition", "runCmsValidation", "loadCmsPreviewSummary", "renderCmsValidationResult", "renderCmsPreviewSummary", "renderCmsRuntimeStatus", "loadCmsRuntimeStatus", "appendCmsRawJsonDetails", "getCmsResponseValue", "loadCmsVersions", "publishCmsDraft", "restoreCmsVersion", "loadCmsAuditEntries", "renderCmsAuditEntries", "goToCmsPublishSection", "showScenarioDraftSavedPublishCallouts", "extractCmsBackendMessages", "renderCmsPublishErrorDetails", "clearCmsPublishErrorDetails", "isCmsSmokeAuditEntry", "shouldShowCmsSmokeAuditEntries")
$forbiddenJsStorageTokens = @("localStorage", "sessionStorage")

$requiredCssSelectors = @("admin-shell", "admin-sidebar", "admin-tab-button", "tab-panel", "selected-user-summary", "empty-state-card", "compact-table", "cms-grid-two", "cms-toolbar", "cms-result-panel", "cms-readable-result-panel", "cms-status-row", "cms-raw-json-details", "cms-section-header", "cms-sub-tabs", "cms-sub-tab-button", "cms-sub-panel", "cms-workspace-grid", "cms-json-output", "cms-selectable-row", "cms-selected-row", "cms-action-column", "cms-select-button", "cms-scenario-structured-editor", "cms-fieldset", "cms-publish-discovery", "cms-publish-notice", "cms-publish-instructions", "cms-publish-error-details", "cms-publish-focus", "cms-scenario-structured-save-row", "cms-audit-smoke-toggle", "cms-audit-smoke-filter-status", "cms-visible-state-marker")

function Add-Error([string]$message) { $errors.Add($message) }

function Get-JsFunctionBody([string]$content, [string]$signature) {
    $start = $content.IndexOf($signature, [System.StringComparison]::Ordinal)
    if ($start -lt 0) { return $null }
    $braceStart = $content.IndexOf("{", $start, [System.StringComparison]::Ordinal)
    if ($braceStart -lt 0) { return $null }
    $depth = 0
    for ($index = $braceStart; $index -lt $content.Length; $index++) {
        $character = $content[$index]
        if ($character -eq '{') { $depth++ }
        elseif ($character -eq '}') {
            $depth--
            if ($depth -eq 0) {
                return $content.Substring($braceStart + 1, $index - $braceStart - 1)
            }
        }
    }
    return $null
}

function Assert-NoDirectValidationPreviewJsonRendering([string]$functionBody, [string]$functionName) {
    if ($null -eq $functionBody) {
        Add-Error "admin.js: missing function body for $functionName."
        return
    }
    foreach ($forbiddenPattern in @(
        'renderJsonOutput\s*\(',
        'JSON\.stringify\s*\(',
        '\.textContent\s*=\s*JSON\.stringify',
        '\.innerText\s*=\s*JSON\.stringify',
        '\.append\s*\([^)]*JSON\.stringify'
    )) {
        if ([regex]::IsMatch($functionBody, $forbiddenPattern)) {
            Add-Error "admin.js: $functionName must not render JSON directly in the Validation & Preview result area (matched '$forbiddenPattern')."
        }
    }
}

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
    foreach ($assetReferencePattern in @(
        "<link[^>]+href=""/admin/admin\.css\?v=$requiredAdminAssetVersionToken""",
        "<script[^>]+src=""/admin/admin\.js\?v=$requiredAdminAssetVersionToken"""
    )) {
        if (-not [regex]::IsMatch($indexContent, $assetReferencePattern)) {
            Add-Error "index.html: admin.css and admin.js must be referenced with the current cache-busting query token '$requiredAdminAssetVersionToken'."
        }
    }
    foreach ($unversionedAssetReferencePattern in @(
        "<link[^>]+href=""/admin/admin\.css""",
        "<script[^>]+src=""/admin/admin\.js"""
    )) {
        if ([regex]::IsMatch($indexContent, $unversionedAssetReferencePattern)) {
            Add-Error "index.html: Admin shell assets must not use unversioned references."
        }
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
    foreach ($publishDiscoveryMarker in @('Draft saved. To apply this content to runtime, publish the current draft.', 'Go to Publish', 'Draft changes are saved but not visible to runtime until published.', '1. Enter a short change summary. 2. Click Publish current draft. 3. Confirm publishing.', 'Publish change summary', 'Required before publishing from the Admin CMS browser UI', 'data-cms-publish-error-details="true"', 'Publish current draft', 'data-cms-publish-discovery="true"', 'cms-scenario-structured-publish-discovery', 'cms-scenario-structured-publish-discovery', 'cms-scenario-json-publish-discovery', 'Smoke/test entries hidden.', 'Smoke/test entries visible.', 'Show smoke/test entries')) {
        if ($indexContent.IndexOf($publishDiscoveryMarker, [System.StringComparison]::Ordinal) -lt 0) {
            Add-Error "index.html: missing CMS publish discoverability marker '$publishDiscoveryMarker'."
        }
    }

    foreach ($validationPreviewMarker in @('Draft validation and preview only', 'does not publish content', 'does not enable CMS content for learners', 'does not change learner runtime behavior', 'Run validation', 'Load preview summary', 'Runtime content status', 'Load runtime status', 'does not enable CMS runtime content')) {
        if ($indexContent.IndexOf($validationPreviewMarker, [System.StringComparison]::Ordinal) -lt 0) {
            Add-Error "index.html: missing Validation & Preview marker '$validationPreviewMarker'."
        }
    }

    foreach ($resultContainerPattern in @(
        'id="cms-validation-result"[^>]*class="[^"]*cms-json-output',
        'id="cms-preview-summary"[^>]*class="[^"]*cms-json-output'
    )) {
        if ([regex]::IsMatch($indexContent, $resultContainerPattern)) {
            Add-Error "index.html: Validation & Preview root result containers must not use cms-json-output."
        }
    }


    foreach ($explicitScenarioCallout in @('id="cms-scenario-structured-publish-discovery"', 'id="cms-scenario-json-publish-discovery"', 'data-cms-scenario-draft-saved-visible="false"')) {
        if ($indexContent.IndexOf($explicitScenarioCallout, [System.StringComparison]::Ordinal) -lt 0) {
            Add-Error "index.html: missing explicit scenario callout marker '$explicitScenarioCallout'."
        }
    }

    $scenarioStructuredPattern = [regex]'id="cms-scenario-structured-save-button"[\s\S]*?id="cms-scenario-structured-publish-discovery"[\s\S]*?Go to Publish'
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

    if ($jsContent.IndexOf("showScenarioDraftSavedPublishCallouts", [System.StringComparison]::Ordinal) -lt 0 -or $jsContent.IndexOf("cmsScenarioStructuredPublishDiscoveryElement", [System.StringComparison]::Ordinal) -lt 0 -or $jsContent.IndexOf("cmsScenarioJsonPublishDiscoveryElement", [System.StringComparison]::Ordinal) -lt 0) {
        Add-Error "admin.js: missing explicit function that directly shows both scenario publish callouts."
    }
    $summaryGuardIndex = $jsContent.IndexOf("if (!summary)", [System.StringComparison]::Ordinal)
    $publishFetchIndex = $jsContent.IndexOf("adminFetch(cmsPath(ApiPaths.cmsPublishTemplate", [System.StringComparison]::Ordinal)
    if ($summaryGuardIndex -lt 0 -or $publishFetchIndex -lt 0 -or $summaryGuardIndex -gt $publishFetchIndex) {
        Add-Error "admin.js: publish summary empty guard must run before the publish API call."
    }
    if ($jsContent.IndexOf("JSON.stringify({ changeSummary: summary })", [System.StringComparison]::Ordinal) -lt 0) {
        Add-Error "admin.js: publish request payload must send changeSummary."
    }
    if ($jsContent.IndexOf('String(entry?.reason || entry?.Reason || "").toLowerCase()', [System.StringComparison]::Ordinal) -lt 0 -or $jsContent.IndexOf('reason.includes("smoke")', [System.StringComparison]::Ordinal) -lt 0) {
        Add-Error "admin.js: audit smoke filter must check reason case-insensitively for smoke."
    }
    if ($jsContent.IndexOf('cmsAuditShowSmokeInput.addEventListener("change"', [System.StringComparison]::Ordinal) -lt 0 -or ($jsContent.IndexOf('updateCmsAuditSmokeFilterStatus(); await loadCmsAuditEntries();', [System.StringComparison]::Ordinal) -lt 0 -and $jsContent.IndexOf('renderCmsAuditEntries', [System.StringComparison]::Ordinal) -lt 0)) {
        Add-Error "admin.js: audit checkbox change handler must re-render or reload audit entries."
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
    foreach ($validationPreviewJsMarker in @("renderCmsValidationResult(validation)", "renderCmsPreviewSummary(preview)", "Show raw validation JSON", "Show raw preview JSON", "Content pack name", "Current published version number", "definitionJsonPresent", "definitionJsonValid")) {
        if ($jsContent.IndexOf($validationPreviewJsMarker, [System.StringComparison]::Ordinal) -lt 0) {
            Add-Error "admin.js: missing readable Validation & Preview marker '$validationPreviewJsMarker'."
        }
    }
    $runValidationBody = Get-JsFunctionBody -content $jsContent -signature "async function runCmsValidation()"
    $loadPreviewBody = Get-JsFunctionBody -content $jsContent -signature "async function loadCmsPreviewSummary()"
    Assert-NoDirectValidationPreviewJsonRendering -functionBody $runValidationBody -functionName "runCmsValidation"
    Assert-NoDirectValidationPreviewJsonRendering -functionBody $loadPreviewBody -functionName "loadCmsPreviewSummary"
    foreach ($rendererMarker in @(
        'appendCmsRawJsonDetails(cmsValidationResultElement, "Show raw validation JSON", validation)',
        'appendCmsRawJsonDetails(cmsPreviewSummaryElement, "Show raw preview JSON", preview)',
        'raw.textContent = JSON.stringify(payload, null, 2)'
    )) {
        if ($jsContent.IndexOf($rendererMarker, [System.StringComparison]::Ordinal) -lt 0) {
            Add-Error "admin.js: missing collapsed raw JSON renderer marker '$rendererMarker'."
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
