param(
    [string]$BaseUrl = "http://localhost:5000"
)

$ErrorActionPreference = "Stop"

$secret = "test_webhook_secret"
$route = "/api/billing/webhooks/paddle"
$uri = ($BaseUrl.TrimEnd('/') + $route)

function Write-Step {
    param([string]$Message)

    Write-Host ("[STEP] {0}" -f $Message)
}

function Write-Pass {
    param([string]$Message)

    Write-Host ("[PASS] {0}" -f $Message)
}

function Fail {
    param([string]$Message)

    throw $Message
}

function New-RandomSuffix {
    return ([Guid]::NewGuid().ToString("N").Substring(0, 12))
}

function Get-UnixTimestamp {
    return [int64]([DateTimeOffset]::UtcNow.ToUnixTimeSeconds())
}

function ConvertTo-HexString {
    param([byte[]]$Bytes)

    $builder = New-Object System.Text.StringBuilder
    foreach ($byte in $Bytes) {
        [void]$builder.Append($byte.ToString("x2"))
    }

    return $builder.ToString()
}

function New-PaddleSignature {
    param(
        [string]$RawBody,
        [string]$SecretKey,
        [int64]$Timestamp
    )

    $payload = ("{0}:{1}" -f $Timestamp, $RawBody)
    $encoding = [System.Text.Encoding]::UTF8
    $hmac = New-Object System.Security.Cryptography.HMACSHA256
    $hmac.Key = $encoding.GetBytes($SecretKey)
    try {
        $hash = $hmac.ComputeHash($encoding.GetBytes($payload))
    }
    finally {
        $hmac.Dispose()
    }

    return ("ts={0};h1={1}" -f $Timestamp, (ConvertTo-HexString -Bytes $hash))
}

function Read-HttpResponseBody {
    param([System.Net.WebResponse]$Response)

    if ($null -eq $Response) {
        return ""
    }

    $stream = $Response.GetResponseStream()
    if ($null -eq $stream) {
        return ""
    }

    $reader = New-Object System.IO.StreamReader($stream)
    try {
        return $reader.ReadToEnd()
    }
    finally {
        $reader.Dispose()
    }
}

function Invoke-WebhookPost {
    param(
        [string]$RawBody,
        [hashtable]$Headers = @{}
    )

    $requestParameters = @{
        Uri = $uri
        Method = "Post"
        ContentType = "application/json"
        Body = $RawBody
        UseBasicParsing = $true
    }

    if (($null -ne $Headers) -and ($Headers.Count -gt 0)) {
        $requestParameters.Headers = $Headers
    }

    try {
        $response = Invoke-WebRequest @requestParameters
        return [pscustomobject]@{
            StatusCode = [int]$response.StatusCode
            Body = $response.Content
        }
    }
    catch [System.Net.WebException] {
        $httpResponse = $_.Exception.Response
        if ($null -eq $httpResponse) {
            Fail "No HTTP response was returned. Check that backend is running and endpoint did not crash."
        }

        try {
            $body = Read-HttpResponseBody -Response $httpResponse
            return [pscustomobject]@{
                StatusCode = [int]$httpResponse.StatusCode
                Body = $body
            }
        }
        finally {
            $httpResponse.Dispose()
        }
    }
}

function Assert-StatusCode {
    param(
        [object]$Response,
        [int]$ExpectedStatusCode,
        [string]$Scenario
    )

    if ($Response.StatusCode -ne $ExpectedStatusCode) {
        Fail ("{0}: expected HTTP {1}, got {2}. Body: {3}" -f $Scenario, $ExpectedStatusCode, $Response.StatusCode, $Response.Body)
    }
}

function Assert-JsonFlag {
    param(
        [string]$Body,
        [string]$PropertyName,
        [bool]$ExpectedValue,
        [string]$Scenario
    )

    $json = $Body | ConvertFrom-Json
    if ($json.$PropertyName -ne $ExpectedValue) {
        Fail ("{0}: expected {1}={2}. Body: {3}" -f $Scenario, $PropertyName, $ExpectedValue, $Body)
    }
}

function Assert-JsonNumber {
    param(
        [string]$Body,
        [string]$PropertyName,
        [int]$ExpectedValue,
        [string]$Scenario
    )

    $json = $Body | ConvertFrom-Json
    if ([int]$json.$PropertyName -ne $ExpectedValue) {
        Fail ("{0}: expected {1}={2}. Body: {3}" -f $Scenario, $PropertyName, $ExpectedValue, $Body)
    }
}

function Assert-BodyDoesNotContain {
    param(
        [string]$Body,
        [string]$Needle,
        [string]$Scenario
    )

    if ($Body.IndexOf($Needle, [System.StringComparison]::OrdinalIgnoreCase) -ge 0) {
        Fail ("{0}: response body must not contain '{1}'. Body: {2}" -f $Scenario, $Needle, $Body)
    }
}

Write-Host "Paddle webhook ingestion smoke test"
Write-Host ("BaseUrl: {0}" -f $BaseUrl)
Write-Host "Expected backend environment:"
Write-Host "  PaddleWebhook__Enabled=true"
Write-Host '  PaddleWebhook__SecretKey="test_webhook_secret"'
Write-Host "  PaddleWebhook__TimestampToleranceSeconds=300"
Write-Host ""

$eventSuffix = New-RandomSuffix
$transactionSuffix = New-RandomSuffix
$userId = [Guid]::NewGuid().ToString()
$occurredAt = [DateTimeOffset]::UtcNow.ToString("o")

$payloadObject = [ordered]@{
    event_id = ("evt_test_{0}" -f $eventSuffix)
    event_type = "transaction.completed"
    occurred_at = $occurredAt
    data = [ordered]@{
        id = ("txn_test_{0}" -f $transactionSuffix)
        custom_data = [ordered]@{
            evt_user_id = $userId
            evt_plan_id = "premium"
        }
    }
}

$payload = ($payloadObject | ConvertTo-Json -Depth 10 -Compress)
$timestamp = Get-UnixTimestamp
$signature = New-PaddleSignature -RawBody $payload -SecretKey $secret -Timestamp $timestamp
$headers = @{ "Paddle-Signature" = $signature }

Write-Step "Posting first signed webhook."
$first = Invoke-WebhookPost -RawBody $payload -Headers $headers
Assert-StatusCode -Response $first -ExpectedStatusCode 200 -Scenario "first signed webhook"
Assert-JsonFlag -Body $first.Body -PropertyName "accepted" -ExpectedValue $true -Scenario "first signed webhook"
Assert-JsonFlag -Body $first.Body -PropertyName "duplicate" -ExpectedValue $false -Scenario "first signed webhook"
Assert-JsonFlag -Body $first.Body -PropertyName "normalized" -ExpectedValue $true -Scenario "first signed webhook"
Assert-JsonFlag -Body $first.Body -PropertyName "billingEventCreated" -ExpectedValue $true -Scenario "first signed webhook"
Assert-JsonFlag -Body $first.Body -PropertyName "reconciliationPending" -ExpectedValue $true -Scenario "first signed webhook"
Assert-JsonNumber -Body $first.Body -PropertyName "reconciliationFailed" -ExpectedValue 0 -Scenario "first signed webhook"
Assert-BodyDoesNotContain -Body $first.Body -Needle "activate" -Scenario "first signed webhook"
Assert-BodyDoesNotContain -Body $first.Body -Needle "entitlement" -Scenario "first signed webhook"
Assert-BodyDoesNotContain -Body $first.Body -Needle "payment" -Scenario "first signed webhook"
Assert-BodyDoesNotContain -Body $first.Body -Needle "subscription" -Scenario "first signed webhook"
Write-Pass "First signed webhook returned HTTP 200 with accepted=true duplicate=false normalized=true billingEventCreated=true reconciliationPending=true."

Write-Step "Posting duplicate signed webhook."
$duplicate = Invoke-WebhookPost -RawBody $payload -Headers $headers
Assert-StatusCode -Response $duplicate -ExpectedStatusCode 200 -Scenario "duplicate signed webhook"
Assert-JsonFlag -Body $duplicate.Body -PropertyName "accepted" -ExpectedValue $true -Scenario "duplicate signed webhook"
Assert-JsonFlag -Body $duplicate.Body -PropertyName "duplicate" -ExpectedValue $true -Scenario "duplicate signed webhook"
Assert-JsonFlag -Body $duplicate.Body -PropertyName "billingEventCreated" -ExpectedValue $false -Scenario "duplicate signed webhook"
Assert-JsonFlag -Body $duplicate.Body -PropertyName "existingBillingEvent" -ExpectedValue $true -Scenario "duplicate signed webhook"
Assert-JsonFlag -Body $duplicate.Body -PropertyName "reconciliationPending" -ExpectedValue $false -Scenario "duplicate signed webhook"
Assert-JsonNumber -Body $duplicate.Body -PropertyName "reconciliationFailed" -ExpectedValue 0 -Scenario "duplicate signed webhook"
Write-Pass "Duplicate signed webhook returned HTTP 200 with accepted=true duplicate=true billingEventCreated=false existingBillingEvent=true and no duplicate reconciliation decision."

Write-Step "Posting unsigned webhook; it must be rejected."
$unsigned = Invoke-WebhookPost -RawBody $payload
Assert-StatusCode -Response $unsigned -ExpectedStatusCode 401 -Scenario "unsigned webhook"
Write-Pass "Unsigned webhook returned HTTP 401."

Write-Step "Posting invalid signature webhook; it must be rejected."
$invalid = Invoke-WebhookPost -RawBody $payload -Headers @{ "Paddle-Signature" = "ts=$timestamp;h1=0000000000000000000000000000000000000000000000000000000000000000" }
Assert-StatusCode -Response $invalid -ExpectedStatusCode 401 -Scenario "invalid signature webhook"
Write-Pass "Invalid signature webhook returned HTTP 401."

Write-Pass "Paddle webhook ingestion, normalization, and reconciliation decision smoke test passed."
