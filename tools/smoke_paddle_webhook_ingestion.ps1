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


function Invoke-JsonPost {
    param(
        [string]$RequestUri,
        [string]$RawBody,
        [hashtable]$Headers = @{}
    )

    $requestParameters = @{
        Uri = $RequestUri
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

function ConvertFrom-Base64UrlString {
    param([string]$Value)

    $base64 = $Value.Replace('-', '+').Replace('_', '/')
    switch ($base64.Length % 4) {
        2 { $base64 = $base64 + "==" }
        3 { $base64 = $base64 + "=" }
        0 { }
        default { Fail "Invalid base64url value in JWT payload." }
    }

    return [System.Text.Encoding]::UTF8.GetString([System.Convert]::FromBase64String($base64))
}

function Get-JwtClaimValue {
    param(
        [string]$AccessToken,
        [string]$ClaimName
    )

    $parts = $AccessToken.Split('.')
    if ($parts.Length -lt 2) {
        Fail "Access token is not a JWT."
    }

    $payloadJson = ConvertFrom-Base64UrlString -Value $parts[1]
    $payload = $payloadJson | ConvertFrom-Json
    return $payload.$ClaimName
}

function Assert-JsonNumberAtLeast {
    param(
        [string]$Body,
        [string]$PropertyName,
        [int]$MinimumValue,
        [string]$Scenario
    )

    $json = $Body | ConvertFrom-Json
    if ([int]$json.$PropertyName -lt $MinimumValue) {
        Fail ("{0}: expected {1}>={2}. Body: {3}" -f $Scenario, $PropertyName, $MinimumValue, $Body)
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

Write-Step "Registering a local smoke test user for entitlement activation."
$userSuffix = New-RandomSuffix
$testEmail = ("paddle-webhook-smoke-{0}@example.test" -f $userSuffix)
$testPassword = ("SmokeTest!{0}" -f $userSuffix)
$registerBody = ([ordered]@{
    email = $testEmail
    password = $testPassword
    displayName = "Paddle Webhook Smoke"
} | ConvertTo-Json -Depth 5 -Compress)
$registerResponse = Invoke-JsonPost -RequestUri ($BaseUrl.TrimEnd('/') + "/api/auth/register") -RawBody $registerBody
Assert-StatusCode -Response $registerResponse -ExpectedStatusCode 201 -Scenario "register smoke user"
$authJson = $registerResponse.Body | ConvertFrom-Json
$accessToken = $authJson.accessToken
if ([string]::IsNullOrWhiteSpace($accessToken)) {
    Fail ("register smoke user: accessToken was missing. Body: {0}" -f $registerResponse.Body)
}

$userId = Get-JwtClaimValue -AccessToken $accessToken -ClaimName "evt_user_id"
$parsedUserId = [Guid]::Empty
if (-not ([Guid]::TryParse($userId, [ref]$parsedUserId))) {
    Fail ("register smoke user: evt_user_id claim was missing or invalid. Claim: {0}" -f $userId)
}
Write-Pass ("Registered smoke user and extracted evt_user_id={0} from JWT." -f $userId)

$eventSuffix = New-RandomSuffix
$transactionSuffix = New-RandomSuffix
$occurredAt = [DateTimeOffset]::UtcNow.ToString("o")
$billingPeriodStartsAt = [DateTimeOffset]::UtcNow.ToString("o")
$billingPeriodEndsAt = [DateTimeOffset]::UtcNow.AddDays(7).ToString("o")

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
        billing_period = [ordered]@{
            starts_at = $billingPeriodStartsAt
            ends_at = $billingPeriodEndsAt
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
Assert-JsonFlag -Body $first.Body -PropertyName "entitlementActivated" -ExpectedValue $true -Scenario "first signed webhook"
Assert-JsonNumberAtLeast -Body $first.Body -PropertyName "entitlementActivatedCount" -MinimumValue 1 -Scenario "first signed webhook"
Assert-JsonNumber -Body $first.Body -PropertyName "entitlementActivationBlocked" -ExpectedValue 0 -Scenario "first signed webhook"
Assert-JsonNumber -Body $first.Body -PropertyName "entitlementActivationFailed" -ExpectedValue 0 -Scenario "first signed webhook"
Assert-BodyDoesNotContain -Body $first.Body -Needle "payment" -Scenario "first signed webhook"
Assert-BodyDoesNotContain -Body $first.Body -Needle "subscription" -Scenario "first signed webhook"
Write-Pass "First signed webhook returned HTTP 200 and activated Premium entitlement for the real smoke user."

Write-Step "Posting duplicate signed webhook."
$duplicate = Invoke-WebhookPost -RawBody $payload -Headers $headers
Assert-StatusCode -Response $duplicate -ExpectedStatusCode 200 -Scenario "duplicate signed webhook"
Assert-JsonFlag -Body $duplicate.Body -PropertyName "accepted" -ExpectedValue $true -Scenario "duplicate signed webhook"
Assert-JsonFlag -Body $duplicate.Body -PropertyName "duplicate" -ExpectedValue $true -Scenario "duplicate signed webhook"
Assert-JsonFlag -Body $duplicate.Body -PropertyName "billingEventCreated" -ExpectedValue $false -Scenario "duplicate signed webhook"
Assert-JsonFlag -Body $duplicate.Body -PropertyName "existingBillingEvent" -ExpectedValue $true -Scenario "duplicate signed webhook"
Assert-JsonFlag -Body $duplicate.Body -PropertyName "reconciliationPending" -ExpectedValue $false -Scenario "duplicate signed webhook"
Assert-JsonNumber -Body $duplicate.Body -PropertyName "reconciliationFailed" -ExpectedValue 0 -Scenario "duplicate signed webhook"
Assert-JsonNumber -Body $duplicate.Body -PropertyName "entitlementActivatedCount" -ExpectedValue 0 -Scenario "duplicate signed webhook"
Write-Pass "Duplicate signed webhook returned HTTP 200 with accepted=true duplicate=true billingEventCreated=false existingBillingEvent=true and no duplicate entitlement activation."

Write-Step "Posting unsigned webhook; it must be rejected."
$unsigned = Invoke-WebhookPost -RawBody $payload
Assert-StatusCode -Response $unsigned -ExpectedStatusCode 401 -Scenario "unsigned webhook"
Write-Pass "Unsigned webhook returned HTTP 401."

Write-Step "Posting invalid signature webhook; it must be rejected."
$invalid = Invoke-WebhookPost -RawBody $payload -Headers @{ "Paddle-Signature" = "ts=$timestamp;h1=0000000000000000000000000000000000000000000000000000000000000000" }
Assert-StatusCode -Response $invalid -ExpectedStatusCode 401 -Scenario "invalid signature webhook"
Write-Pass "Invalid signature webhook returned HTTP 401."

Write-Pass "Paddle webhook ingestion, normalization, reconciliation decision, and entitlement activation smoke test passed."
