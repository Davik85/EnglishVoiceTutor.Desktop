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
$statusUrl = "$BaseUrl/api/admin/dev/cms/published-content/status"

try {
    Invoke-RestMethod -Method Get -Uri $healthUrl -TimeoutSec 15 | Out-Null
} catch {
    throw "Backend is not reachable at $healthUrl. Start the backend in Development before running this smoke test. $_"
}

$beforeStaticStatus = git -C $repoRoot status --short -- Content/Lessons Content/Prompts Content/Tutors
if ($beforeStaticStatus) {
    Write-Host $beforeStaticStatus
    throw 'Static lesson, prompt, or tutor files have local changes before CMS published content read smoke test.'
}

$status = Invoke-RestMethod -Method Get -Uri $statusUrl -Headers $headers -TimeoutSec 60

if ($status.contentPackSlug -ne 'static-json-v1') {
    $status | ConvertTo-Json -Depth 12 | Write-Host
    throw "Unexpected content pack slug: $($status.contentPackSlug)"
}

if ($status.source -eq 'CmsPublishedSnapshot') {
    if (-not $status.success -or -not $status.validationPassed -or -not $status.hashValid) {
        $status | ConvertTo-Json -Depth 12 | Write-Host
        throw 'CMS published content status did not report a valid published snapshot.'
    }

    if ($status.versionNumber -lt 1 -or $status.topicCount -lt 1 -or $status.scenarioCount -lt 1 -or $status.promptTemplateCount -ne 3 -or $status.tutorBehaviorProfileCount -lt 1) {
        $status | ConvertTo-Json -Depth 12 | Write-Host
        throw 'CMS published content status returned unexpected counts.'
    }
} elseif ($status.source -eq 'StaticJsonFallback') {
    if (-not $status.success -or -not $status.fallbackUsed) {
        $status | ConvertTo-Json -Depth 12 | Write-Host
        throw 'CMS published content status did not report a safe static JSON fallback.'
    }

    Write-Warning 'CMS published snapshot read path returned StaticJsonFallback. Enable CmsContent__ReadPublishedSnapshotEnabled=true and import CMS content to validate published snapshot counts.'
} else {
    $status | ConvertTo-Json -Depth 12 | Write-Host
    throw "Unexpected CMS published content source: $($status.source)"
}

$afterStaticStatus = git -C $repoRoot status --short -- Content/Lessons Content/Prompts Content/Tutors
if ($afterStaticStatus) {
    Write-Host $afterStaticStatus
    throw 'Static lesson, prompt, or tutor files changed during CMS published content read smoke test.'
}

Write-Host 'CMS published content read smoke test passed.'
Write-Host ($status | ConvertTo-Json -Depth 8)
