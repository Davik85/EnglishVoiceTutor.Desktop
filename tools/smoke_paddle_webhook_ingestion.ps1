param(
    [string]$BaseUrl = "http://localhost:5000"
)

$ErrorActionPreference = "Stop"

$secret = "test_webhook_secret"
$route = "/api/billing/webhooks/paddle"
$uri = ($BaseUrl.TrimEnd('/') + $route)

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

function Invoke-WebhookPost {
    param(
        [string]$RawBody,
        [hashtable]$Headers = @{}
    )

    try {
        $response = Invoke-WebRequest -Uri $uri -Method Post -ContentType "application/json" -Headers $Headers -Body $RawBody -UseBasicParsing
        return [pscustomobject]@{
            StatusCode = [int]$response.StatusCode
            Body = $response.Content
        }
    }
    catch [System.Net.WebException] {
        $httpResponse = $_.Exception.Response
        if ($null -eq $httpResponse) {
            throw
        }

        $reader = New-Object System.IO.StreamReader($httpResponse.GetResponseStream())
        try {
            $body = $reader.ReadToEnd()
        }
        finally {
            $reader.Dispose()
            $httpResponse.Dispose()
        }

        return [pscustomobject]@{
            StatusCode = [int]$httpResponse.StatusCode
            Body = $body
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
        throw ("{0}: expected HTTP {1}, got {2}. Body: {3}" -f $Scenario, $ExpectedStatusCode, $Response.StatusCode, $Response.Body)
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
        throw ("{0}: expected {1}={2}. Body: {3}" -f $Scenario, $PropertyName, $ExpectedValue, $Body)
    }
}

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

$first = Invoke-WebhookPost -RawBody $payload -Headers $headers
Assert-StatusCode -Response $first -ExpectedStatusCode 200 -Scenario "first signed webhook"
Assert-JsonFlag -Body $first.Body -PropertyName "accepted" -ExpectedValue $true -Scenario "first signed webhook"
Assert-JsonFlag -Body $first.Body -PropertyName "duplicate" -ExpectedValue $false -Scenario "first signed webhook"

$duplicate = Invoke-WebhookPost -RawBody $payload -Headers $headers
Assert-StatusCode -Response $duplicate -ExpectedStatusCode 200 -Scenario "duplicate signed webhook"
Assert-JsonFlag -Body $duplicate.Body -PropertyName "accepted" -ExpectedValue $true -Scenario "duplicate signed webhook"
Assert-JsonFlag -Body $duplicate.Body -PropertyName "duplicate" -ExpectedValue $true -Scenario "duplicate signed webhook"

$unsigned = Invoke-WebhookPost -RawBody $payload
Assert-StatusCode -Response $unsigned -ExpectedStatusCode 401 -Scenario "unsigned webhook"

$invalid = Invoke-WebhookPost -RawBody $payload -Headers @{ "Paddle-Signature" = "ts=$timestamp;h1=0000000000000000000000000000000000000000000000000000000000000000" }
Assert-StatusCode -Response $invalid -ExpectedStatusCode 401 -Scenario "invalid signature webhook"

Write-Host "[PASS] Paddle webhook ingestion smoke test passed."
