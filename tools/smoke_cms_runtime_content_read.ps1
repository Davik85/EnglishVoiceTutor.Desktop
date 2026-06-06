param(
    [string]$BaseUrl = 'http://localhost:5000',
    [string]$BearerToken = $env:EVT_ADMIN_BEARER_TOKEN
)

$ErrorActionPreference = 'Stop'

if ([string]::IsNullOrWhiteSpace($BearerToken)) {
    throw 'Admin bearer token is required. Pass -BearerToken or set EVT_ADMIN_BEARER_TOKEN after authenticating as the bootstrap admin.'
}

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot '..')
$headers = @{ Authorization = "Bearer $BearerToken" }
$healthUrl = "$BaseUrl/api/health"
$importUrl = "$BaseUrl/api/admin/dev/cms/static-content/import"
$statusUrl = "$BaseUrl/api/admin/dev/cms/runtime-content/status"

try {
    Invoke-RestMethod -Method Get -Uri $healthUrl -TimeoutSec 15 | Out-Null
} catch {
    throw "Backend is not reachable at $healthUrl. Start the backend in Development with CmsContent__UsePublishedSnapshotForRuntime=true and CmsContent__ReadPublishedSnapshotEnabled=true before running this smoke test. $_"
}

$beforeStaticStatus = git -C $repoRoot status --short -- Content/Lessons Content/Prompts Content/Tutors
if ($beforeStaticStatus) {
    Write-Host $beforeStaticStatus
    throw 'Static lesson, prompt, or tutor files have local changes before CMS runtime content read smoke test.'
}

Invoke-RestMethod -Method Post -Uri $importUrl -Headers $headers -TimeoutSec 120 | Out-Null
$status = Invoke-RestMethod -Method Get -Uri $statusUrl -Headers $headers -TimeoutSec 60

if ($status.source -ne 'CmsPublishedSnapshot') {
    $status | ConvertTo-Json -Depth 12 | Write-Host
    throw 'CMS runtime content status must report CmsPublishedSnapshot. Start backend with CmsContent__UsePublishedSnapshotForRuntime=true and CmsContent__ReadPublishedSnapshotEnabled=true.'
}

if (-not $status.success -or -not $status.validationPassed -or -not $status.hashValid) {
    $status | ConvertTo-Json -Depth 12 | Write-Host
    throw 'CMS runtime content status did not report a valid published snapshot.'
}

if ($status.contentPackSlug -ne 'static-json-v1') {
    $status | ConvertTo-Json -Depth 12 | Write-Host
    throw "Unexpected content pack slug: $($status.contentPackSlug)"
}

if ($status.topicCount -ne 6 -or $status.scenarioCount -ne 26 -or $status.promptTemplateCount -ne 3 -or $status.tutorBehaviorProfileCount -ne 2) {
    $status | ConvertTo-Json -Depth 12 | Write-Host
    throw 'CMS runtime content status returned unexpected counts.'
}

if ($status.versionNumber -lt 1) {
    $status | ConvertTo-Json -Depth 12 | Write-Host
    throw 'CMS runtime content status did not return a published version number.'
}

if ([string]::IsNullOrWhiteSpace($status.snapshotHash) -or $status.snapshotHash -notmatch '^[a-f0-9]{64}$') {
    $status | ConvertTo-Json -Depth 12 | Write-Host
    throw 'CMS runtime content status did not return a valid SHA-256 snapshot hash.'
}

if ($status.fallbackUsed) {
    $status | ConvertTo-Json -Depth 12 | Write-Host
    throw 'CMS runtime content happy path unexpectedly used fallback.'
}

$afterStaticStatus = git -C $repoRoot status --short -- Content/Lessons Content/Prompts Content/Tutors
if ($afterStaticStatus) {
    Write-Host $afterStaticStatus
    throw 'Static lesson, prompt, or tutor files changed during CMS runtime content read smoke test.'
}

Write-Host 'CMS runtime content read smoke test passed.'
Write-Host ($status | ConvertTo-Json -Depth 8)
