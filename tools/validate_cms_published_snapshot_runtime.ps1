<#
.SYNOPSIS
    Operator-safe CMS published-snapshot runtime validation helper.

.DESCRIPTION
    Default mode is read-only. It calls the protected Admin CMS runtime status
    diagnostic on the server backend and verifies that normal learner runtime is
    still using StaticJson by default.

    With -GenerateServerValidationPlan, the script does not change production
    configuration. It prints the explicit, temporary flags and a reversible
    operator checklist for a controlled server validation window.

.PARAMETER BaseUrl
    Backend root URL. Defaults to https://api.languagevoicetutor.com. Localhost
    must be passed explicitly only for approved developer/local runs.

.PARAMETER AccessToken
    Optional admin bearer token. If omitted, the script also reads
    EVT_ADMIN_BEARER_TOKEN. Token values are never printed.

.PARAMETER GenerateServerValidationPlan
    Print the operator-guided CMS published-snapshot validation plan. This mode
    does not call server endpoints and does not change configuration.

.NOTES
    This script does not enable CMS runtime.
    This script does not edit production config files.
    This script does not restart services.
    This script prints safe metadata only and must not print content bodies,
    prompt bodies, scenario DefinitionJson, tutor instruction bodies, secrets,
    tokens, connection strings, or auth headers.
#>
param(
    [string]$BaseUrl = "https://api.languagevoicetutor.com",
    [string]$AccessToken = $env:EVT_ADMIN_BEARER_TOKEN,
    [switch]$GenerateServerValidationPlan
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$RuntimeStatusPath = "/api/admin/dev/cms/runtime-status"
$ExpectedDefaultSource = "StaticJson"
$ExpectedContentPackSlug = "static-json-v1"
$ExpectedPublishedSource = "CmsPublishedSnapshot"
$ExpectedCounts = [ordered]@{
    topics = 6
    scenarios = 26
    promptTemplates = 3
    tutorBehaviorProfiles = 3
}

function Join-Url([string]$Root, [string]$Path) {
    return $Root.TrimEnd("/") + $Path
}

function Assert-PropertyExists($Object, [string]$PropertyName) {
    if ($Object.PSObject.Properties.Name -notcontains $PropertyName) {
        throw "Missing required runtime-status field '$PropertyName'."
    }
}

function Get-SafeCount($Value) {
    if ($null -eq $Value) { return 0 }
    if ($Value -is [System.Array]) { return $Value.Count }
    return @($Value).Count
}

function Write-ServerValidationPlan {
    Write-Host "CMS published-snapshot runtime validation plan" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "Safety boundary:" -ForegroundColor Yellow
    Write-Output @"
- This script does not change production configuration.
- This plan mode is offline: it does not call backend endpoints and does not require admin authentication.
- Apply these flags only in an explicitly approved controlled window.
- Remove the flags and restart the backend immediately after validation.
- Do not make CMS published-snapshot runtime the learner default until this validation passes and a separate approval is made.
"@

    Write-Host "Temporary environment variables/config flags required:" -ForegroundColor Cyan
    Write-Output @"
  CmsContent__UsePublishedSnapshotForRuntime=true
  CmsContent__ReadPublishedSnapshotEnabled=true
  CmsContent__ContentPackSlug=$ExpectedContentPackSlug
  CmsContent__FallbackToStaticJson=true
"@

    Write-Host "Operator checklist:" -ForegroundColor Cyan
    Write-Output @"
  1. Confirm backend current release, for example: ssh <server-alias> "readlink -f /opt/languagevoicetutor/backend/current"
  2. Confirm health and database health:
       Invoke-WebRequest https://api.languagevoicetutor.com/health -UseBasicParsing
       Invoke-WebRequest https://api.languagevoicetutor.com/api/health/database -UseBasicParsing
  3. Confirm Admin CMS has a published version for $ExpectedContentPackSlug.
  4. Apply the temporary CMS runtime flags only in the approved controlled window.
  5. Restart backend.
  6. Load runtime status with admin auth:
       .\tools\validate_cms_published_snapshot_runtime.ps1 -AccessToken '<admin-token>'
  7. Confirm effectiveSource=$ExpectedPublishedSource.
  8. Confirm validationSuccess=true.
  9. Confirm counts: topics=$($ExpectedCounts.topics), scenarios=$($ExpectedCounts.scenarios), promptTemplates=$($ExpectedCounts.promptTemplates), tutorBehaviorProfiles=$($ExpectedCounts.tutorBehaviorProfiles).
 10. Run a short installed-app lesson smoke only if approved for the controlled window.
 11. Remove/disable the temporary CMS runtime flags.
 12. Restart backend.
 13. Confirm effectiveSource=$ExpectedDefaultSource again.
"@

    Write-Host "Expected validation results:" -ForegroundColor Cyan
    Write-Output @"
  effectiveSource=$ExpectedPublishedSource
  validationSuccess=true
  topics=$($ExpectedCounts.topics)
  scenarios=$($ExpectedCounts.scenarios)
  promptTemplates=$($ExpectedCounts.promptTemplates)
  tutorBehaviorProfiles=$($ExpectedCounts.tutorBehaviorProfiles)
"@

    Write-Host "Rollback/disable steps (do not paste secrets into docs or chat):" -ForegroundColor Cyan
    Write-Output @"
  # Remove or set false in the approved server configuration mechanism:
  CmsContent__UsePublishedSnapshotForRuntime=false
  CmsContent__ReadPublishedSnapshotEnabled=false
  # Keep or remove non-secret slug/fallback entries according to operator policy.
  # Restart the backend with the approved service command, then rerun the read-only status check.
"@
}
if ($GenerateServerValidationPlan) {
    Write-ServerValidationPlan
    exit 0
}

$headers = @{}
if (-not [string]::IsNullOrWhiteSpace($AccessToken)) {
    $headers["Authorization"] = "Bearer $AccessToken"
}

$url = Join-Url -Root $BaseUrl -Path $RuntimeStatusPath
Write-Host "[INFO] Read-only mode. No configuration will be changed." -ForegroundColor Cyan
Write-Host "[INFO] BaseUrl: $BaseUrl" -ForegroundColor Cyan
Write-Host "[STEP] GET $url" -ForegroundColor Cyan

try {
    $status = Invoke-RestMethod -Method GET -Uri $url -Headers $headers -TimeoutSec 60
} catch {
    $statusCode = $null
    if ($null -ne $_.Exception.Response) {
        try { $statusCode = [int]$_.Exception.Response.StatusCode } catch { $statusCode = $null }
    }

    if ($statusCode -eq 401 -or $statusCode -eq 403) {
        throw "CMS runtime status request returned HTTP $statusCode. Admin authentication is required. Provide -AccessToken or set EVT_ADMIN_BEARER_TOKEN after using an approved admin auth method. Token values must not be printed or hardcoded. BaseUrl used: $BaseUrl."
    }

    throw "CMS runtime status request failed for BaseUrl '$BaseUrl'. This script is safe/read-only and does not start a backend or change configuration. Underlying error: $($_.Exception.Message)"
}

foreach ($field in @(
    "contentPackSlug", "effectiveSource", "validationSuccess", "usePublishedSnapshotForRuntime",
    "readPublishedSnapshotEnabled", "fallbackToStaticJson", "topicCount", "scenarioCount",
    "promptTemplateCount", "tutorBehaviorProfileCount", "errors", "warnings"
)) { Assert-PropertyExists $status $field }

$effectiveSource = [string]$status.effectiveSource
$validationSuccess = [bool]$status.validationSuccess
$usePublishedSnapshotForRuntime = [bool]$status.usePublishedSnapshotForRuntime
$readPublishedSnapshotEnabled = [bool]$status.readPublishedSnapshotEnabled
$errorCount = Get-SafeCount $status.errors
$warningCount = Get-SafeCount $status.warnings

if ($effectiveSource -ne $ExpectedDefaultSource) {
    throw "Expected default effectiveSource=$ExpectedDefaultSource, but runtime-status returned '$effectiveSource'. This read-only script made no changes."
}
if (-not $validationSuccess) {
    throw "Expected validationSuccess=true for default static JSON runtime status."
}
if ($usePublishedSnapshotForRuntime) {
    throw "Expected usePublishedSnapshotForRuntime=false in default learner runtime state."
}
if ($readPublishedSnapshotEnabled -and $effectiveSource -eq $ExpectedPublishedSource) {
    throw "Learner runtime appears to be using CMS published snapshot. Expected static JSON default."
}

Write-Host "[SUMMARY] CMS runtime status default validation" -ForegroundColor Cyan
Write-Host "  effectiveSource: $effectiveSource"
Write-Host "  validationSuccess: $validationSuccess"
Write-Host "  usePublishedSnapshotForRuntime: $usePublishedSnapshotForRuntime"
Write-Host "  readPublishedSnapshotEnabled: $readPublishedSnapshotEnabled"
Write-Host "  learner runtime using CMS snapshot: False"
Write-Host "  contentPackSlug: $($status.contentPackSlug)"
Write-Host "  topics: $($status.topicCount)"
Write-Host "  scenarios: $($status.scenarioCount)"
Write-Host "  promptTemplates: $($status.promptTemplateCount)"
Write-Host "  tutorBehaviorProfiles: $($status.tutorBehaviorProfileCount)"
Write-Host "  errors count: $errorCount"
Write-Host "  warnings count: $warningCount"
Write-Host "[PASS] StaticJson default is validated and no server configuration was changed." -ForegroundColor Green
