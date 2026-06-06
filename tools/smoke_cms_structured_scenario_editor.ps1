param(
    [string]$BaseUrl = 'http://localhost:5000',
    [string]$BearerToken = $env:EVT_ADMIN_BEARER_TOKEN
)

$ErrorActionPreference = 'Stop'

if ([string]::IsNullOrWhiteSpace($BearerToken)) {
    throw 'Admin bearer token is required. Pass -BearerToken or set EVT_ADMIN_BEARER_TOKEN after authenticating as the bootstrap admin.'
}

$headers = @{ Authorization = "Bearer $BearerToken" }
$jsonHeaders = @{ Authorization = "Bearer $BearerToken"; 'Content-Type' = 'application/json' }
$contentPackUrl = "$BaseUrl/api/admin/dev/cms/content-packs/static-json-v1"
$auditUrl = "$contentPackUrl/audit-entries"

function Assert-ValidScenarioDefinition {
    param([string]$DefinitionJson, [string]$ExpectedScenarioKey, [string]$ExpectedGoal)

    $definition = $DefinitionJson | ConvertFrom-Json
    if ($definition.id -ne $ExpectedScenarioKey) {
        throw "DefinitionJson id '$($definition.id)' does not match stable scenario key '$ExpectedScenarioKey'."
    }

    if ($definition.learningGoal.goal -ne $ExpectedGoal) {
        throw "DefinitionJson learningGoal.goal was not persisted. Expected '$ExpectedGoal' but got '$($definition.learningGoal.goal)'."
    }

    if (-not $definition.lessonSetup.setupMessage) {
        throw 'DefinitionJson is missing required lessonSetup.setupMessage after structured save.'
    }
}

function Assert-RecentStructuredAuditEntry {
    param([string]$StableKey, [datetimeoffset]$SinceUtc)

    $lookupUrl = '{0}?entityType=Scenario&stableKey={1}&limit=10' -f $auditUrl, [uri]::EscapeDataString($StableKey)
    $payload = Invoke-RestMethod -Method Get -Uri $lookupUrl -Headers $headers -TimeoutSec 60
    $entry = @($payload.entries | Where-Object { $_.operation -eq 'DraftSaved' -and $_.entityType -eq 'Scenario' -and $_.stableKey -eq $StableKey -and ([datetimeoffset]$_.createdAtUtc) -ge $SinceUtc }) | Select-Object -First 1
    if (-not $entry) {
        $payload | ConvertTo-Json -Depth 20 | Write-Host
        throw "No recent structured scenario DraftSaved audit entry found for '$StableKey'."
    }

    foreach ($field in @('DefinitionJson', 'StructuredScenarioFields')) {
        if (-not (@($entry.changedFields) -contains $field)) {
            $entry | ConvertTo-Json -Depth 20 | Write-Host
            throw "Structured scenario audit entry does not include changed field '$field'."
        }
    }

    if ($entry.beforeHash -eq $entry.afterHash) {
        throw 'Structured scenario audit entry has identical before/after hashes.'
    }
}

Invoke-RestMethod -Method Get -Uri "$BaseUrl/api/health" -TimeoutSec 15 | Out-Null
Invoke-RestMethod -Method Get -Uri $contentPackUrl -Headers $headers -TimeoutSec 60 | Out-Null
$scenarios = Invoke-RestMethod -Method Get -Uri "$contentPackUrl/scenarios" -Headers $headers -TimeoutSec 60
$scenario = @($scenarios)[0]
if (-not $scenario) { throw 'No CMS scenarios were returned.' }

$detail = Invoke-RestMethod -Method Get -Uri "$contentPackUrl/scenarios/$($scenario.id)" -Headers $headers -TimeoutSec 60
$definition = $detail.definitionJson | ConvertFrom-Json
$originalGoal = [string]$definition.learningGoal.goal
$marker = "structured-smoke-$([guid]::NewGuid().ToString('N'))"
$updatedGoal = "$originalGoal [$marker]"

$definition.learningGoal.goal = $updatedGoal
$updatedDefinitionJson = $definition | ConvertTo-Json -Depth 100
$startedAtUtc = [datetimeoffset]::UtcNow.AddSeconds(-5)

$updateBody = @{
    title = $detail.title
    description = $detail.description
    setupMessage = $detail.setupMessage
    definitionJson = $updatedDefinitionJson
    structuredScenarioFieldsEdited = $true
    isActive = $detail.isActive
    reason = 'SMOKE: CMS structured scenario editor update learning goal.'
} | ConvertTo-Json -Depth 100
Invoke-RestMethod -Method Put -Uri "$contentPackUrl/scenarios/$($detail.id)" -Headers $jsonHeaders -Body $updateBody -TimeoutSec 60 | Out-Null

$updatedDetail = Invoke-RestMethod -Method Get -Uri "$contentPackUrl/scenarios/$($detail.id)" -Headers $headers -TimeoutSec 60
Assert-ValidScenarioDefinition -DefinitionJson $updatedDetail.definitionJson -ExpectedScenarioKey $detail.stableScenarioKey -ExpectedGoal $updatedGoal
Assert-RecentStructuredAuditEntry -StableKey $detail.stableScenarioKey -SinceUtc $startedAtUtc

$restoreDefinition = $updatedDetail.definitionJson | ConvertFrom-Json
$restoreDefinition.learningGoal.goal = $originalGoal
$restoreBody = @{
    title = $detail.title
    description = $detail.description
    setupMessage = $detail.setupMessage
    definitionJson = ($restoreDefinition | ConvertTo-Json -Depth 100)
    structuredScenarioFieldsEdited = $true
    isActive = $detail.isActive
    reason = 'SMOKE: CMS structured scenario editor restore learning goal.'
} | ConvertTo-Json -Depth 100
Invoke-RestMethod -Method Put -Uri "$contentPackUrl/scenarios/$($detail.id)" -Headers $jsonHeaders -Body $restoreBody -TimeoutSec 60 | Out-Null

$restoredDetail = Invoke-RestMethod -Method Get -Uri "$contentPackUrl/scenarios/$($detail.id)" -Headers $headers -TimeoutSec 60
Assert-ValidScenarioDefinition -DefinitionJson $restoredDetail.definitionJson -ExpectedScenarioKey $detail.stableScenarioKey -ExpectedGoal $originalGoal

Write-Host "CMS structured scenario editor smoke passed for $($detail.stableScenarioKey)."
