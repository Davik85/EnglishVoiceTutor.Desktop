Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$errors = New-Object System.Collections.Generic.List[string]

$indexPath = Join-Path $repoRoot "backend/EnglishVoiceTutor.Api/wwwroot/admin/index.html"
$jsPath = Join-Path $repoRoot "backend/EnglishVoiceTutor.Api/wwwroot/admin/admin.js"
$cssPath = Join-Path $repoRoot "backend/EnglishVoiceTutor.Api/wwwroot/admin/admin.css"
$checkedFiles = @($indexPath, $jsPath, $cssPath)

$requiredTabButtonIds = @(
    "tab-button-overview", "tab-button-user-lookup", "tab-button-premium", "tab-button-free-lesson", "tab-button-audit-log", "tab-button-system"
)
$requiredTabPanelIds = @(
    "tab-panel-overview", "tab-panel-user-lookup", "tab-panel-premium", "tab-panel-free-lesson", "tab-panel-audit-log", "tab-panel-system"
)
$requiredLookupIds = @("lookup-form", "lookup-email", "search-user-button", "lookup-loading", "lookup-error", "lookup-result")
$requiredPremiumLookupIds = @("premium-lookup-form", "premium-lookup-email", "premium-search-user-button", "premium-lookup-loading", "premium-lookup-error")
$requiredPremiumControlIds = @(
    "premium-empty-state", "premium-content", "premium-entitlement-schedule-result", "active-entitlements-result", "grant-card", "grant-form", "grant-duration-days", "grant-reason", "grant-button", "revoke-card", "revoke-form", "revoke-entitlement-id", "revoke-entitlement-preview", "revoke-reason", "revoke-button"
)
$requiredFreeLessonLookupIds = @("free-lesson-lookup-form", "free-lesson-lookup-email", "free-lesson-search-user-button", "free-lesson-lookup-loading", "free-lesson-lookup-error")
$requiredFreeLessonResetIds = @("free-lesson-empty-state", "free-lesson-reset-card", "free-lesson-reset-form", "free-lesson-reset-usage-date", "free-lesson-reset-reason", "free-lesson-reset-button")
$requiredAuditIds = @("audit-empty-state", "audit-card", "audit-limit", "load-audit-button", "audit-result")
$requiredSystemIds = @("capabilities-list")

$requiredJsEndpoints = @(
    "/api/auth/login", "/api/admin/capabilities", "/api/admin/users/by-email", "/api/admin/users/{userId}/audit-actions", "/api/admin/users/{userId}/premium-grants", "/api/admin/users/{userId}/premium-grants/{entitlementId}/revoke", "/api/admin/users/{userId}/free-lesson-allowance/reset"
)
$requiredJsLookupRefs = @("user-lookup", "premium", "free-lesson")
$requiredJsFunctionRefs = @("updateSelectedUserHeader", "updateUserRequiredEmptyStates", "applySelectedUserPayload", "clearSelectedUserState")
$forbiddenJsStorageTokens = @("localStorage", "sessionStorage")

$requiredCssSelectors = @("admin-shell", "admin-sidebar", "admin-tab-button", "tab-panel", "selected-user-summary", "empty-state-card", "compact-table")

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
    $idAttributeRegex = [regex]'id\s*=\s*(["''])(?<id>[^"'']+)\1'
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
        $requiredPremiumControlIds + $requiredFreeLessonLookupIds + $requiredFreeLessonResetIds + $requiredAuditIds + $requiredSystemIds
    )
    foreach ($id in $requiredIndexIds) {
        Assert-ContainsOnceById -idCounts $idCounts -id $id
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
    foreach ($forbiddenToken in $forbiddenJsStorageTokens) {
        if ($jsContent.IndexOf($forbiddenToken, [System.StringComparison]::OrdinalIgnoreCase) -ge 0) {
            Add-Error "admin.js: forbidden token '$forbiddenToken' found. JWT must remain memory-only."
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
