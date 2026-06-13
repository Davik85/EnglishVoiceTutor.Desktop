<#
.SYNOPSIS
    Safely checks the admin CMS runtime status diagnostic.

.PARAMETER BaseUrl
    Defaults to the production/tester backend used for release/backend verification:
    https://api.languagevoicetutor.com

    Localhost is not the release/backend verification default. Pass a localhost
    BaseUrl only for explicit, approved developer runs against a local backend.

.PARAMETER AccessToken
    Optional admin bearer token. Token values are never printed. If the endpoint
    requires admin authentication and no token or approved auth method is
    provided, the script fails clearly instead of treating 401/403 as success.

.NOTES
    This script does not start a backend.
    This script does not enable CMS runtime.
    This script does not change configuration.
    This script prints safe metadata only and must not print content bodies or secrets.
#>
param(
    [string]$BaseUrl = "https://api.languagevoicetutor.com",
    [string]$AccessToken = ""
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$RuntimeStatusPath = "/api/admin/dev/cms/runtime-status"
$ReleaseVerificationBaseUrl = "https://api.languagevoicetutor.com"
$ForbiddenFields = @(
    "content", "definitionJson", "body", "lesson", "prompt", "communicationStyleJson", "safetyNotesJson",
    "connectionString", "password", "token", "apiKey", "authorization", "bearer"
)
$RequiredFields = @(
    "checkedAtUtc", "contentPackSlug", "usePublishedSnapshotForRuntime", "readPublishedSnapshotEnabled",
    "fallbackToStaticJson", "effectiveSource", "topicCount", "scenarioCount", "promptTemplateCount",
    "tutorBehaviorProfileCount", "validationSuccess", "fallbackUsed", "message", "errors", "warnings"
)

function Join-Url([string]$Root, [string]$Path) {
    return $Root.TrimEnd("/") + $Path
}

function Assert-True([bool]$Condition, [string]$Message) {
    if (-not $Condition) { throw $Message }
}

function Assert-PropertyExists($Object, [string]$PropertyName) {
    if ($Object.PSObject.Properties.Name -notcontains $PropertyName) {
        throw "Missing required field '$PropertyName'."
    }
}

function Get-Count($Value) {
    if ($null -eq $Value) { return 0 }
    if ($Value -is [System.Array]) { return $Value.Count }
    return @($Value).Count
}

$headers = @{}
if (-not [string]::IsNullOrWhiteSpace($AccessToken)) {
    $headers["Authorization"] = "Bearer $AccessToken"
}

$url = Join-Url -Root $BaseUrl -Path $RuntimeStatusPath
Write-Host "[INFO] BaseUrl: $BaseUrl" -ForegroundColor Cyan
Write-Host "[STEP] GET $url" -ForegroundColor Cyan

try {
    $response = Invoke-RestMethod -Method GET -Uri $url -Headers $headers
} catch {
    $statusCode = $null
    if ($null -ne $_.Exception.Response) {
        try { $statusCode = [int]$_.Exception.Response.StatusCode } catch { $statusCode = $null }
    }

    if ($statusCode -eq 401 -or $statusCode -eq 403) {
        throw "CMS runtime status request returned HTTP $statusCode. Admin authentication is required for this diagnostic. Provide an admin bearer token with -AccessToken or use another approved admin auth method. Token values must not be printed or hardcoded. BaseUrl used: $BaseUrl."
    }

    throw "CMS runtime status request failed for BaseUrl '$BaseUrl'. Release/backend verification expects the server backend to be reachable at $ReleaseVerificationBaseUrl. This script does not start a backend. Pass -BaseUrl explicitly only for approved developer/local runs. Underlying error: $($_.Exception.Message)"
}

foreach ($field in $RequiredFields) { Assert-PropertyExists $response $field }

$json = $response | ConvertTo-Json -Depth 20
foreach ($field in $ForbiddenFields) {
    Assert-True ($json -notmatch ('"' + [regex]::Escape($field) + '"\s*:')) "Forbidden field '$field' was present in runtime-status response."
}

$useCmsRuntime = [bool]$response.usePublishedSnapshotForRuntime
$readPublished = [bool]$response.readPublishedSnapshotEnabled
$effectiveSource = [string]$response.effectiveSource
if (-not ($useCmsRuntime -and $readPublished)) {
    Assert-True ($effectiveSource -eq "StaticJson" -or $effectiveSource -eq "StaticJsonFallback") "Default or partially enabled runtime must remain static JSON, but effectiveSource was '$effectiveSource'."
}

$warningCount = Get-Count $response.warnings
$errorCount = Get-Count $response.errors

Write-Host "[INFO] effectiveSource: $effectiveSource"
Write-Host "[INFO] usePublishedSnapshotForRuntime: $useCmsRuntime"
Write-Host "[INFO] readPublishedSnapshotEnabled: $readPublished"
Write-Host "[INFO] fallbackToStaticJson: $([bool]$response.fallbackToStaticJson)"
Write-Host "[INFO] validationSuccess: $([bool]$response.validationSuccess)"
Write-Host "[INFO] errors count: $errorCount"
Write-Host "[INFO] warnings count: $warningCount"
Write-Host "[PASS] CMS runtime status response shape is safe." -ForegroundColor Green
Write-Host "[PASS] Runtime defaults remain static JSON unless both CMS runtime flags are enabled." -ForegroundColor Green
