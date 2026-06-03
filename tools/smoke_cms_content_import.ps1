param(
    [string]$BaseUrl = 'http://localhost:5000',
    [string]$BearerToken = $env:EVT_ADMIN_BEARER_TOKEN
)

$ErrorActionPreference = 'Stop'

if ([string]::IsNullOrWhiteSpace($BearerToken)) {
    throw 'Admin bearer token is required. Pass -BearerToken or set EVT_ADMIN_BEARER_TOKEN after authenticating as the bootstrap admin.'
}

$headers = @{ Authorization = "Bearer $BearerToken" }
$healthUrl = "$BaseUrl/api/health"
$importUrl = "$BaseUrl/api/admin/dev/cms/static-content/import"

try {
    Invoke-RestMethod -Method Get -Uri $healthUrl -TimeoutSec 15 | Out-Null
} catch {
    throw "Backend is not reachable at $healthUrl. Start the backend in Development before running this smoke test. $_"
}

$beforeStaticStatus = git -C (Resolve-Path (Join-Path $PSScriptRoot '..')) status --short -- Content/Lessons Content/Prompts Content/Tutors
if ($beforeStaticStatus) {
    Write-Host $beforeStaticStatus
    throw 'Static lesson, prompt, or tutor files have local changes before import smoke test.'
}

$first = Invoke-RestMethod -Method Post -Uri $importUrl -Headers $headers -TimeoutSec 120
if (-not $first.success) {
    $first | ConvertTo-Json -Depth 12 | Write-Host
    throw 'First CMS static content import did not report success.'
}

$second = Invoke-RestMethod -Method Post -Uri $importUrl -Headers $headers -TimeoutSec 120
if (-not $second.success) {
    $second | ConvertTo-Json -Depth 12 | Write-Host
    throw 'Second CMS static content import did not report success.'
}

if ($first.counts.topicsRead -lt 1 -or $first.counts.scenariosRead -lt 1 -or $first.counts.promptTemplatesRead -ne 3 -or $first.counts.tutorBehaviorProfilesRead -lt 1) {
    $first | ConvertTo-Json -Depth 12 | Write-Host
    throw 'First CMS static content import returned unexpected source counts.'
}

if (-not $first.publishedSnapshotCreated -and $first.counts.publishedSnapshotsSkipped -lt 1) {
    $first | ConvertTo-Json -Depth 12 | Write-Host
    throw 'First CMS static content import neither created nor found a published snapshot.'
}

if (-not $second.idempotentNoChanges) {
    $second | ConvertTo-Json -Depth 12 | Write-Host
    throw 'Second CMS static content import was not idempotent.'
}

$afterStaticStatus = git -C (Resolve-Path (Join-Path $PSScriptRoot '..')) status --short -- Content/Lessons Content/Prompts Content/Tutors
if ($afterStaticStatus) {
    Write-Host $afterStaticStatus
    throw 'Static lesson, prompt, or tutor files changed during import smoke test.'
}

Write-Host 'CMS static content import smoke test passed.'
Write-Host ($second | ConvertTo-Json -Depth 8)
