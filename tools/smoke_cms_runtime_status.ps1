param(
    [string]$BaseUrl = "http://localhost:5000",
    [string]$AccessToken = ""
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$RuntimeStatusPath = "/api/admin/dev/cms/runtime-status"
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

$headers = @{}
if (-not [string]::IsNullOrWhiteSpace($AccessToken)) {
    $headers["Authorization"] = "Bearer $AccessToken"
}

$url = Join-Url -Root $BaseUrl -Path $RuntimeStatusPath
Write-Host "[STEP] GET $url" -ForegroundColor Cyan
$response = Invoke-RestMethod -Method GET -Uri $url -Headers $headers

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

Write-Host "[PASS] CMS runtime status response shape is safe." -ForegroundColor Green
Write-Host "[PASS] Runtime defaults remain static JSON unless both CMS runtime flags are enabled." -ForegroundColor Green
