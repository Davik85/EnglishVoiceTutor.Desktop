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

try {
    Invoke-RestMethod -Method Get -Uri $healthUrl -TimeoutSec 15 | Out-Null
} catch {
    throw "Backend is not reachable at $healthUrl. Start the backend in Development before running this smoke test. $_"
}

$beforeStaticStatus = git -C $repoRoot status --short -- Content/Lessons Content/Prompts Content/Tutors
if ($beforeStaticStatus) {
    Write-Host $beforeStaticStatus
    throw 'Static lesson, prompt, or tutor files have local changes before CMS admin content API smoke test.'
}

$contentPacks = Invoke-RestMethod -Method Get -Uri $contentPacksUrl -Headers $headers -TimeoutSec 60
$staticPack = @($contentPacks | Where-Object { $_.slug -eq 'static-json-v1' }) | Select-Object -First 1
if (-not $staticPack) {
    $contentPacks | ConvertTo-Json -Depth 12 | Write-Host
    throw 'static-json-v1 content pack was not returned by the CMS admin content pack list endpoint.'
}

$summary = Invoke-RestMethod -Method Get -Uri $contentPackUrl -Headers $headers -TimeoutSec 60
if ($summary.slug -ne 'static-json-v1' -or $summary.topicCount -lt 1 -or $summary.scenarioCount -lt 1 -or $summary.promptTemplateCount -ne 3 -or $summary.tutorBehaviorProfileCount -lt 1) {
    $summary | ConvertTo-Json -Depth 12 | Write-Host
    throw 'CMS admin content pack summary returned unexpected values.'
}

$topics = Invoke-RestMethod -Method Get -Uri "$contentPackUrl/topics" -Headers $headers -TimeoutSec 60
if (@($topics).Count -lt 1) {
    $topics | ConvertTo-Json -Depth 12 | Write-Host
    throw 'CMS admin topics endpoint returned no topics.'
}

$topic = @($topics)[0]
$topicDetail = Invoke-RestMethod -Method Get -Uri "$contentPackUrl/topics/$($topic.id)" -Headers $headers -TimeoutSec 60
if ($topicDetail.id -ne $topic.id) {
    $topicDetail | ConvertTo-Json -Depth 12 | Write-Host
    throw 'CMS admin topic detail endpoint returned the wrong topic.'
}

$scenarios = Invoke-RestMethod -Method Get -Uri "$contentPackUrl/scenarios" -Headers $headers -TimeoutSec 60
if (@($scenarios).Count -lt 1) {
    $scenarios | ConvertTo-Json -Depth 12 | Write-Host
    throw 'CMS admin scenarios endpoint returned no scenarios.'
}

$scenario = @($scenarios)[0]
$scenarioDetail = Invoke-RestMethod -Method Get -Uri "$contentPackUrl/scenarios/$($scenario.id)" -Headers $headers -TimeoutSec 60
if ($scenarioDetail.id -ne $scenario.id) {
    $scenarioDetail | ConvertTo-Json -Depth 12 | Write-Host
    throw 'CMS admin scenario detail endpoint returned the wrong scenario.'
}

if ([string]::IsNullOrWhiteSpace([string]$scenarioDetail.definitionJson) -or $scenarioDetail.isDefinitionJsonFallback) {
    $scenarioDetail | ConvertTo-Json -Depth 12 | Write-Host
    throw 'CMS admin scenario detail endpoint did not return persisted full scenario JSON.'
}

$definition = $scenarioDetail.definitionJson | ConvertFrom-Json
if ([string]::IsNullOrWhiteSpace([string]$definition.id) -or -not $definition.metadata -or -not $definition.lessonSetup -or [string]::IsNullOrWhiteSpace([string]$definition.lessonSetup.setupMessage)) {
    $scenarioDetail | ConvertTo-Json -Depth 12 | Write-Host
    throw 'CMS admin scenario detail full scenario JSON is missing required baseline fields.'
}

$validDefinition = $scenarioDetail.definitionJson | ConvertFrom-Json
$validDefinition | Add-Member -NotePropertyName smokeScenarioJsonEdit -NotePropertyValue 'Step 5D-6c valid draft save smoke' -Force
$validDefinitionJson = $validDefinition | ConvertTo-Json -Depth 100
$saveBody = @{
    title = $scenarioDetail.title
    description = $scenarioDetail.description
    setupMessage = $scenarioDetail.setupMessage
    definitionJson = $validDefinitionJson
    isActive = $scenarioDetail.isActive
    reason = 'Step 5D-6c smoke: valid full scenario JSON draft save.'
} | ConvertTo-Json -Depth 100
$saveResponse = Invoke-RestMethod -Method Put -Uri "$contentPackUrl/scenarios/$($scenario.id)" -Headers $jsonHeaders -Body $saveBody -TimeoutSec 60
if (-not $saveResponse.success) {
    $saveResponse | ConvertTo-Json -Depth 12 | Write-Host
    throw 'CMS admin scenario draft save did not accept valid full scenario JSON.'
}

$invalidRejected = $false
try {
    $invalidBody = @{
        title = $scenarioDetail.title
        description = $scenarioDetail.description
        setupMessage = $scenarioDetail.setupMessage
        definitionJson = '{ invalid scenario json'
        isActive = $scenarioDetail.isActive
        reason = 'Step 5D-6c smoke: invalid full scenario JSON rejection.'
    } | ConvertTo-Json -Depth 12
    Invoke-RestMethod -Method Put -Uri "$contentPackUrl/scenarios/$($scenario.id)" -Headers $jsonHeaders -Body $invalidBody -TimeoutSec 60 | Out-Null
} catch {
    if ($_.Exception.Response.StatusCode.value__ -eq 400) { $invalidRejected = $true }
}
if (-not $invalidRejected) {
    throw 'CMS admin scenario draft save did not reject invalid full scenario JSON with HTTP 400.'
}

$restoreBody = @{
    title = $scenarioDetail.title
    description = $scenarioDetail.description
    setupMessage = $scenarioDetail.setupMessage
    definitionJson = $scenarioDetail.definitionJson
    isActive = $scenarioDetail.isActive
    reason = 'Step 5D-6c smoke: restore original full scenario JSON after draft save smoke.'
} | ConvertTo-Json -Depth 100
Invoke-RestMethod -Method Put -Uri "$contentPackUrl/scenarios/$($scenario.id)" -Headers $jsonHeaders -Body $restoreBody -TimeoutSec 60 | Out-Null

$promptTemplates = Invoke-RestMethod -Method Get -Uri "$contentPackUrl/prompt-templates" -Headers $headers -TimeoutSec 60
if (@($promptTemplates).Count -ne 3) {
    $promptTemplates | ConvertTo-Json -Depth 12 | Write-Host
    throw 'CMS admin prompt templates endpoint did not return exactly 3 templates.'
}

$promptTemplate = @($promptTemplates)[0]
$promptTemplateDetail = Invoke-RestMethod -Method Get -Uri "$contentPackUrl/prompt-templates/$($promptTemplate.id)" -Headers $headers -TimeoutSec 60
if ($promptTemplateDetail.id -ne $promptTemplate.id) {
    $promptTemplateDetail | ConvertTo-Json -Depth 12 | Write-Host
    throw 'CMS admin prompt template detail endpoint returned the wrong template.'
}

$tutorProfiles = Invoke-RestMethod -Method Get -Uri "$contentPackUrl/tutor-behavior-profiles" -Headers $headers -TimeoutSec 60
if (@($tutorProfiles).Count -lt 1) {
    $tutorProfiles | ConvertTo-Json -Depth 12 | Write-Host
    throw 'CMS admin tutor behavior profiles endpoint returned no profiles.'
}

$tutorProfile = @($tutorProfiles)[0]
$tutorProfileDetail = Invoke-RestMethod -Method Get -Uri "$contentPackUrl/tutor-behavior-profiles/$($tutorProfile.id)" -Headers $headers -TimeoutSec 60
if ($tutorProfileDetail.id -ne $tutorProfile.id) {
    $tutorProfileDetail | ConvertTo-Json -Depth 12 | Write-Host
    throw 'CMS admin tutor behavior profile detail endpoint returned the wrong profile.'
}

$validation = Invoke-RestMethod -Method Post -Uri "$contentPackUrl/validate" -Headers $headers -TimeoutSec 60
if (-not $validation.success -or $validation.counts.topics -lt 1 -or $validation.counts.scenarios -lt 1 -or $validation.counts.promptTemplates -ne 3 -or $validation.counts.tutorBehaviorProfiles -lt 1) {
    $validation | ConvertTo-Json -Depth 12 | Write-Host
    throw 'CMS admin draft validation endpoint returned unexpected validation results.'
}

$preview = Invoke-RestMethod -Method Get -Uri "$contentPackUrl/preview-summary" -Headers $headers -TimeoutSec 60
if ($preview.contentPackSlug -ne 'static-json-v1' -or $preview.topicCount -lt 1 -or $preview.scenarioCount -lt 1 -or $preview.promptTemplateCount -ne 3 -or $preview.tutorBehaviorProfileCount -lt 1 -or -not $preview.validation.success -or @($preview.sampleScenarios | Where-Object { -not $_.definitionJsonPresent -or -not $_.definitionJsonValid }).Count -gt 0) {
    $preview | ConvertTo-Json -Depth 12 | Write-Host
    throw 'CMS admin preview summary endpoint returned unexpected values.'
}

$afterStaticStatus = git -C $repoRoot status --short -- Content/Lessons Content/Prompts Content/Tutors
if ($afterStaticStatus) {
    Write-Host $afterStaticStatus
    throw 'Static lesson, prompt, or tutor files changed during CMS admin content API smoke test.'
}

Write-Host 'CMS content admin API smoke test passed.'
Write-Host ($preview | ConvertTo-Json -Depth 8)
