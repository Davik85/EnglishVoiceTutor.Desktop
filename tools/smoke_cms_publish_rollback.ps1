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
$jsonHeaders = @{ Authorization = "Bearer $BearerToken"; 'Content-Type' = 'application/json' }
$healthUrl = "$BaseUrl/api/health"
$contentPacksUrl = "$BaseUrl/api/admin/dev/cms/content-packs"
$contentPackUrl = "$contentPacksUrl/static-json-v1"
$versionsUrl = "$contentPackUrl/versions"
$publishUrl = "$contentPackUrl/publish"
$markerPrefix = '[Step 5D-5 smoke publish rollback]'

function Invoke-JsonPost([string]$Uri, [object]$Body) {
    $json = $Body | ConvertTo-Json -Depth 12
    return Invoke-RestMethod -Method Post -Uri $Uri -Headers $jsonHeaders -Body $json -TimeoutSec 120
}

function Invoke-JsonPut([string]$Uri, [object]$Body) {
    $json = $Body | ConvertTo-Json -Depth 12
    return Invoke-RestMethod -Method Put -Uri $Uri -Headers $jsonHeaders -Body $json -TimeoutSec 120
}

try {
    Invoke-RestMethod -Method Get -Uri $healthUrl -TimeoutSec 15 | Out-Null
} catch {
    throw "Backend is not reachable at $healthUrl. Start the backend in Development before running this smoke test. $_"
}

$beforeStaticStatus = git -C $repoRoot status --short -- Content/Lessons Content/Prompts Content/Tutors
if ($beforeStaticStatus) {
    Write-Host $beforeStaticStatus
    throw 'Static lesson, prompt, or tutor files have local changes before CMS publish/rollback smoke test.'
}

$contentPacks = Invoke-RestMethod -Method Get -Uri $contentPacksUrl -Headers $headers -TimeoutSec 60
$staticPack = @($contentPacks | Where-Object { $_.slug -eq 'static-json-v1' }) | Select-Object -First 1
if (-not $staticPack) {
    $contentPacks | ConvertTo-Json -Depth 12 | Write-Host
    throw 'static-json-v1 content pack was not returned by the CMS admin content pack list endpoint.'
}

$versionsBefore = Invoke-RestMethod -Method Get -Uri $versionsUrl -Headers $headers -TimeoutSec 60
if (-not $versionsBefore.success -or @($versionsBefore.versions).Count -lt 1) {
    $versionsBefore | ConvertTo-Json -Depth 12 | Write-Host
    throw 'CMS content version list did not return at least one version.'
}

$latestBefore = @($versionsBefore.versions | Sort-Object -Property versionNumber -Descending)[0]
$previousVersionNumber = [int]$latestBefore.versionNumber
$previousSnapshotHash = [string]$latestBefore.snapshotHash
if ($previousVersionNumber -lt 1 -or [string]::IsNullOrWhiteSpace($previousSnapshotHash)) {
    $latestBefore | ConvertTo-Json -Depth 12 | Write-Host
    throw 'Latest CMS content version was missing version number or snapshot hash.'
}

$versionDetail = Invoke-RestMethod -Method Get -Uri "$versionsUrl/$previousVersionNumber" -Headers $headers -TimeoutSec 60
if ($versionDetail.versionNumber -ne $previousVersionNumber -or -not $versionDetail.snapshotHashValid) {
    $versionDetail | ConvertTo-Json -Depth 12 | Write-Host
    throw 'CMS content version detail endpoint returned an invalid latest version.'
}

$topics = Invoke-RestMethod -Method Get -Uri "$contentPackUrl/topics" -Headers $headers -TimeoutSec 60
$topic = @($topics | Sort-Object -Property sortOrder, stableTopicKey)[0]
if (-not $topic) {
    $topics | ConvertTo-Json -Depth 12 | Write-Host
    throw 'CMS topics endpoint returned no topic to update.'
}

$originalDescription = [string]$topic.description
$smokeDescription = "$originalDescription $markerPrefix version-$previousVersionNumber".Trim()
if ($smokeDescription -eq $originalDescription) {
    $smokeDescription = "$markerPrefix version-$previousVersionNumber"
}

$updateBody = @{
    description = $smokeDescription
    reason = 'Step 5D-5 smoke: bounded topic description update before publish.'
}
$update = Invoke-JsonPut -Uri "$contentPackUrl/topics/$($topic.id)" -Body $updateBody
if (-not $update.success -or $update.noChanges) {
    $update | ConvertTo-Json -Depth 12 | Write-Host
    throw 'CMS bounded draft topic update did not change the draft content.'
}

$scenarios = Invoke-RestMethod -Method Get -Uri "$contentPackUrl/scenarios" -Headers $headers -TimeoutSec 60
$scenario = @($scenarios | Sort-Object -Property stableScenarioKey)[0]
$scenarioDetail = Invoke-RestMethod -Method Get -Uri "$contentPackUrl/scenarios/$($scenario.id)" -Headers $headers -TimeoutSec 60
if ([string]::IsNullOrWhiteSpace([string]$scenarioDetail.definitionJson) -or $scenarioDetail.isDefinitionJsonFallback) {
    $scenarioDetail | ConvertTo-Json -Depth 12 | Write-Host
    throw 'CMS scenario detail did not return persisted full scenario JSON before publish/rollback smoke.'
}

$originalScenarioDefinitionJson = [string]$scenarioDetail.definitionJson
$scenarioDefinition = $scenarioDetail.definitionJson | ConvertFrom-Json
$scenarioDefinition | Add-Member -NotePropertyName smokePublishRollbackMarker -NotePropertyValue "$markerPrefix version-$previousVersionNumber" -Force
$scenarioDefinitionJson = $scenarioDefinition | ConvertTo-Json -Depth 100
$scenarioUpdate = Invoke-JsonPut -Uri "$contentPackUrl/scenarios/$($scenario.id)" -Body @{
    title = $scenarioDetail.title
    description = $scenarioDetail.description
    setupMessage = $scenarioDetail.setupMessage
    definitionJson = $scenarioDefinitionJson
    isActive = $scenarioDetail.isActive
    reason = 'Step 5D-6c smoke: full scenario JSON update before publish.'
}
if (-not $scenarioUpdate.success) {
    $scenarioUpdate | ConvertTo-Json -Depth 12 | Write-Host
    throw 'CMS scenario draft update did not save full scenario JSON before publish.'
}

$validation = Invoke-JsonPost -Uri "$contentPackUrl/validate" -Body @{}
if (-not $validation.success) {
    $validation | ConvertTo-Json -Depth 12 | Write-Host
    throw 'CMS draft validation failed after bounded smoke update.'
}

$publish = Invoke-JsonPost -Uri $publishUrl -Body @{ changeSummary = 'Step 5D-5 smoke: publish bounded topic description update.' }
if (-not $publish.success -or -not $publish.created -or $publish.noChanges) {
    $publish | ConvertTo-Json -Depth 12 | Write-Host
    throw 'CMS publish endpoint did not create a changed published version.'
}

if ([int]$publish.versionNumber -le $previousVersionNumber) {
    $publish | ConvertTo-Json -Depth 12 | Write-Host
    throw 'CMS publish endpoint did not increase the version number.'
}

if ([string]$publish.snapshotHash -eq $previousSnapshotHash) {
    $publish | ConvertTo-Json -Depth 12 | Write-Host
    throw 'CMS publish endpoint did not change the snapshot hash after the draft update.'
}

$versionsAfterPublish = Invoke-RestMethod -Method Get -Uri $versionsUrl -Headers $headers -TimeoutSec 60
$publishedVersion = @($versionsAfterPublish.versions | Where-Object { $_.versionNumber -eq $publish.versionNumber }) | Select-Object -First 1
if (-not $publishedVersion -or $publishedVersion.snapshotHash -ne $publish.snapshotHash) {
    $versionsAfterPublish | ConvertTo-Json -Depth 12 | Write-Host
    throw 'CMS version list did not include the newly published version.'
}

$restore = Invoke-JsonPost -Uri "$versionsUrl/$previousVersionNumber/restore" -Body @{
    reason = 'Step 5D-5 smoke: restore previous published version after bounded update.'
    publishRestoredVersion = $true
}
if (-not $restore.success -or -not $restore.draftRestored) {
    $restore | ConvertTo-Json -Depth 12 | Write-Host
    throw 'CMS restore endpoint did not restore the selected previous version into draft.'
}

if (-not $restore.publishedNewVersion -and -not $restore.noChanges) {
    $restore | ConvertTo-Json -Depth 12 | Write-Host
    throw 'CMS restore endpoint neither published a rollback version nor reported no changes.'
}

$restoredScenarioDetail = Invoke-RestMethod -Method Get -Uri "$contentPackUrl/scenarios/$($scenario.id)" -Headers $headers -TimeoutSec 60
if ([string]::IsNullOrWhiteSpace([string]$restoredScenarioDetail.definitionJson) -or $restoredScenarioDetail.definitionJson -ne $originalScenarioDefinitionJson) {
    $restoredScenarioDetail | ConvertTo-Json -Depth 12 | Write-Host
    throw 'CMS restore did not restore the original full scenario JSON into the draft.'
}

$validationAfterRestore = Invoke-JsonPost -Uri "$contentPackUrl/validate" -Body @{}
if (-not $validationAfterRestore.success) {
    $validationAfterRestore | ConvertTo-Json -Depth 12 | Write-Host
    throw 'CMS draft validation failed after restore.'
}

$versionsAfterRestore = Invoke-RestMethod -Method Get -Uri $versionsUrl -Headers $headers -TimeoutSec 60
$latestAfterRestore = @($versionsAfterRestore.versions | Sort-Object -Property versionNumber -Descending)[0]
if ($restore.publishedNewVersion) {
    if ([int]$latestAfterRestore.versionNumber -ne [int]$restore.newVersionNumber -or [string]$latestAfterRestore.snapshotHash -ne $previousSnapshotHash) {
        $latestAfterRestore | ConvertTo-Json -Depth 12 | Write-Host
        throw 'Latest CMS version after restore does not match the restored snapshot.'
    }
} elseif ([string]$latestAfterRestore.snapshotHash -ne $previousSnapshotHash) {
    $latestAfterRestore | ConvertTo-Json -Depth 12 | Write-Host
    throw 'CMS restore no-change result did not leave the latest snapshot matching the selected version.'
}

$afterStaticStatus = git -C $repoRoot status --short -- Content/Lessons Content/Prompts Content/Tutors
if ($afterStaticStatus) {
    Write-Host $afterStaticStatus
    throw 'Static lesson, prompt, or tutor files changed during CMS publish/rollback smoke test.'
}

Write-Host 'CMS publish/rollback smoke test passed.'
Write-Host (@{
    previousVersionNumber = $previousVersionNumber
    publishedVersionNumber = $publish.versionNumber
    restoredVersionNumber = $restore.newVersionNumber
    restoredSnapshotHash = $restore.newSnapshotHash
    validationAfterRestore = $validationAfterRestore.success
} | ConvertTo-Json -Depth 8)
