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

function Assert-AuditEntry {
    param(
        [string]$EntityType,
        [string]$StableKey,
        [string]$ExpectedChangedField,
        [datetimeoffset]$SinceUtc
    )

    $encodedEntityType = [uri]::EscapeDataString($EntityType)
    $encodedStableKey = [uri]::EscapeDataString($StableKey)
    $payload = Invoke-RestMethod -Method Get -Uri "$auditUrl?entityType=$encodedEntityType&stableKey=$encodedStableKey&limit=10" -Headers $headers -TimeoutSec 60
    $entry = @($payload.entries | Where-Object { $_.operation -eq 'DraftSaved' -and $_.entityType -eq $EntityType -and $_.stableKey -eq $StableKey -and ([datetimeoffset]$_.createdAtUtc) -ge $SinceUtc }) | Select-Object -First 1
    if (-not $entry) {
        $payload | ConvertTo-Json -Depth 20 | Write-Host
        throw "No recent DraftSaved CMS audit entry found for $EntityType '$StableKey'."
    }

    foreach ($required in @('id', 'createdAtUtc', 'actorUserId', 'contentPackId', 'contentPackSlug', 'entityType', 'entityId', 'stableKey', 'operation', 'changedFields', 'beforeHash', 'afterHash', 'source', 'status')) {
        if ($null -eq $entry.$required -or [string]::IsNullOrWhiteSpace([string]$entry.$required)) {
            $entry | ConvertTo-Json -Depth 20 | Write-Host
            throw "CMS audit entry for $EntityType '$StableKey' is missing required field '$required'."
        }
    }

    if ($entry.contentPackSlug -ne 'static-json-v1' -or $entry.operation -ne 'DraftSaved' -or $entry.source -ne 'AdminCms') {
        $entry | ConvertTo-Json -Depth 20 | Write-Host
        throw "CMS audit entry for $EntityType '$StableKey' has unexpected pack, operation, or source."
    }

    if (-not (@($entry.changedFields) -contains $ExpectedChangedField)) {
        $entry | ConvertTo-Json -Depth 20 | Write-Host
        throw "CMS audit entry for $EntityType '$StableKey' does not include changed field '$ExpectedChangedField'."
    }

    if ($entry.beforeHash -eq $entry.afterHash) {
        $entry | ConvertTo-Json -Depth 20 | Write-Host
        throw "CMS audit entry for $EntityType '$StableKey' has identical before/after hashes."
    }
}

Invoke-RestMethod -Method Get -Uri "$BaseUrl/api/health" -TimeoutSec 15 | Out-Null
$startedAtUtc = [datetimeoffset]::UtcNow.AddSeconds(-5)

$topics = Invoke-RestMethod -Method Get -Uri "$contentPackUrl/topics" -Headers $headers -TimeoutSec 60
$topic = @($topics)[0]
$topicDetail = Invoke-RestMethod -Method Get -Uri "$contentPackUrl/topics/$($topic.id)" -Headers $headers -TimeoutSec 60
$topicDescription = [string]$topicDetail.description
$topicMarker = "audit-smoke-$([guid]::NewGuid().ToString('N'))"
$topicBody = @{ title = $topicDetail.title; description = "$topicDescription $topicMarker".Trim(); sortOrder = $topicDetail.sortOrder; isActive = $topicDetail.isActive; reason = 'CMS draft-save audit smoke: topic bounded field.' } | ConvertTo-Json -Depth 20
Invoke-RestMethod -Method Put -Uri "$contentPackUrl/topics/$($topic.id)" -Headers $jsonHeaders -Body $topicBody -TimeoutSec 60 | Out-Null
Assert-AuditEntry -EntityType 'Topic' -StableKey $topicDetail.stableTopicKey -ExpectedChangedField 'Description' -SinceUtc $startedAtUtc
$restoreTopicBody = @{ title = $topicDetail.title; description = $topicDescription; sortOrder = $topicDetail.sortOrder; isActive = $topicDetail.isActive; reason = 'CMS draft-save audit smoke: restore topic bounded field.' } | ConvertTo-Json -Depth 20
Invoke-RestMethod -Method Put -Uri "$contentPackUrl/topics/$($topic.id)" -Headers $jsonHeaders -Body $restoreTopicBody -TimeoutSec 60 | Out-Null

$scenarios = Invoke-RestMethod -Method Get -Uri "$contentPackUrl/scenarios" -Headers $headers -TimeoutSec 60
$scenario = @($scenarios)[0]
$scenarioDetail = Invoke-RestMethod -Method Get -Uri "$contentPackUrl/scenarios/$($scenario.id)" -Headers $headers -TimeoutSec 60
$scenarioDescription = [string]$scenarioDetail.description
$scenarioBody = @{ title = $scenarioDetail.title; description = "$scenarioDescription $topicMarker".Trim(); setupMessage = $scenarioDetail.setupMessage; definitionJson = $scenarioDetail.definitionJson; isActive = $scenarioDetail.isActive; reason = 'CMS draft-save audit smoke: scenario bounded field.' } | ConvertTo-Json -Depth 100
Invoke-RestMethod -Method Put -Uri "$contentPackUrl/scenarios/$($scenario.id)" -Headers $jsonHeaders -Body $scenarioBody -TimeoutSec 60 | Out-Null
Assert-AuditEntry -EntityType 'Scenario' -StableKey $scenarioDetail.stableScenarioKey -ExpectedChangedField 'Description' -SinceUtc $startedAtUtc

$scenarioDefinition = $scenarioDetail.definitionJson | ConvertFrom-Json
$scenarioDefinition | Add-Member -NotePropertyName auditSmokeMarker -NotePropertyValue $topicMarker -Force
$scenarioJsonBody = @{ title = $scenarioDetail.title; description = "$scenarioDescription $topicMarker".Trim(); setupMessage = $scenarioDetail.setupMessage; definitionJson = ($scenarioDefinition | ConvertTo-Json -Depth 100); isActive = $scenarioDetail.isActive; reason = 'CMS draft-save audit smoke: full scenario JSON.' } | ConvertTo-Json -Depth 100
Invoke-RestMethod -Method Put -Uri "$contentPackUrl/scenarios/$($scenario.id)" -Headers $jsonHeaders -Body $scenarioJsonBody -TimeoutSec 60 | Out-Null
Assert-AuditEntry -EntityType 'Scenario' -StableKey $scenarioDetail.stableScenarioKey -ExpectedChangedField 'DefinitionJson' -SinceUtc $startedAtUtc
$restoreScenarioBody = @{ title = $scenarioDetail.title; description = $scenarioDescription; setupMessage = $scenarioDetail.setupMessage; definitionJson = $scenarioDetail.definitionJson; isActive = $scenarioDetail.isActive; reason = 'CMS draft-save audit smoke: restore scenario fields.' } | ConvertTo-Json -Depth 100
Invoke-RestMethod -Method Put -Uri "$contentPackUrl/scenarios/$($scenario.id)" -Headers $jsonHeaders -Body $restoreScenarioBody -TimeoutSec 60 | Out-Null

$templates = Invoke-RestMethod -Method Get -Uri "$contentPackUrl/prompt-templates" -Headers $headers -TimeoutSec 60
$template = @($templates)[0]
$templateDetail = Invoke-RestMethod -Method Get -Uri "$contentPackUrl/prompt-templates/$($template.id)" -Headers $headers -TimeoutSec 60
$templateBodyText = [string]$templateDetail.body
$templateBody = @{ body = "$templateBodyText`n<!-- $topicMarker -->"; isActive = $templateDetail.isActive; reason = 'CMS draft-save audit smoke: prompt template body.' } | ConvertTo-Json -Depth 20
Invoke-RestMethod -Method Put -Uri "$contentPackUrl/prompt-templates/$($template.id)" -Headers $jsonHeaders -Body $templateBody -TimeoutSec 60 | Out-Null
Assert-AuditEntry -EntityType 'PromptTemplate' -StableKey $templateDetail.templateKey -ExpectedChangedField 'Body' -SinceUtc $startedAtUtc
$restoreTemplateBody = @{ body = $templateBodyText; isActive = $templateDetail.isActive; reason = 'CMS draft-save audit smoke: restore prompt template body.' } | ConvertTo-Json -Depth 20
Invoke-RestMethod -Method Put -Uri "$contentPackUrl/prompt-templates/$($template.id)" -Headers $jsonHeaders -Body $restoreTemplateBody -TimeoutSec 60 | Out-Null

$profiles = Invoke-RestMethod -Method Get -Uri "$contentPackUrl/tutor-behavior-profiles" -Headers $headers -TimeoutSec 60
$profile = @($profiles)[0]
$profileDetail = Invoke-RestMethod -Method Get -Uri "$contentPackUrl/tutor-behavior-profiles/$($profile.id)" -Headers $headers -TimeoutSec 60
$displayName = [string]$profileDetail.displayName
$profileBody = @{ displayName = "$displayName $topicMarker".Trim(); communicationStyleJson = $profileDetail.communicationStyleJson; safetyNotesJson = $profileDetail.safetyNotesJson; isActive = $profileDetail.isActive; reason = 'CMS draft-save audit smoke: tutor profile display name.' } | ConvertTo-Json -Depth 100
Invoke-RestMethod -Method Put -Uri "$contentPackUrl/tutor-behavior-profiles/$($profile.id)" -Headers $jsonHeaders -Body $profileBody -TimeoutSec 60 | Out-Null
Assert-AuditEntry -EntityType 'TutorBehaviorProfile' -StableKey $profileDetail.tutorId -ExpectedChangedField 'DisplayName' -SinceUtc $startedAtUtc
$restoreProfileBody = @{ displayName = $displayName; communicationStyleJson = $profileDetail.communicationStyleJson; safetyNotesJson = $profileDetail.safetyNotesJson; isActive = $profileDetail.isActive; reason = 'CMS draft-save audit smoke: restore tutor profile display name.' } | ConvertTo-Json -Depth 100
Invoke-RestMethod -Method Put -Uri "$contentPackUrl/tutor-behavior-profiles/$($profile.id)" -Headers $jsonHeaders -Body $restoreProfileBody -TimeoutSec 60 | Out-Null

Write-Host 'CMS draft-save audit smoke test passed.'
