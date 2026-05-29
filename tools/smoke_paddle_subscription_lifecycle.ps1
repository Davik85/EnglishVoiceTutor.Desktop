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

function Invoke-JsonGet {
    param(
        [string]$RequestUri,
        [hashtable]$Headers = @{}
    )

    $requestParameters = @{
        Uri = $RequestUri
        Method = "Get"
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

function Assert-JsonString {
    param(
        [string]$Body,
        [string]$PropertyName,
        [string]$ExpectedValue,
        [string]$Scenario
    )

    $json = $Body | ConvertFrom-Json
    if ([string]$json.$PropertyName -ne $ExpectedValue) {
        Fail ("{0}: expected {1}={2}. Body: {3}" -f $Scenario, $PropertyName, $ExpectedValue, $Body)
    }
}

function Assert-DateTimeOffsetClose {
    param(
        [string]$ActualValue,
        [string]$ExpectedValue,
        [string]$Scenario,
        [string]$PropertyName,
        [int]$ToleranceSeconds = 1,
        [string]$Body = ""
    )

    $actual = [DateTimeOffset]::Parse($ActualValue).ToUniversalTime()
    $expected = [DateTimeOffset]::Parse($ExpectedValue).ToUniversalTime()
    $difference = [Math]::Abs(($actual - $expected).TotalSeconds)

    if ($difference -gt $ToleranceSeconds) {
        Fail ("{0}: expected {1} close to {2}, got {3}. Difference: {4:N6}s; tolerance: {5}s. Body: {6}" -f $Scenario, $PropertyName, $expected.ToString("o"), $actual.ToString("o"), $difference, $ToleranceSeconds, $Body)
    }
}

function Assert-JsonDateString {
    param(
        [string]$Body,
        [string]$PropertyName,
        [string]$ExpectedValue,
        [string]$Scenario
    )

    $json = $Body | ConvertFrom-Json
    Assert-DateTimeOffsetClose `
        -ActualValue ([string]$json.$PropertyName) `
        -ExpectedValue $ExpectedValue `
        -Scenario $Scenario `
        -PropertyName $PropertyName `
        -ToleranceSeconds 1 `
        -Body $Body
}

function Invoke-SignedWebhookPost {
    param([string]$RawBody)

    $timestamp = Get-UnixTimestamp
    $signature = New-PaddleSignature -RawBody $RawBody -SecretKey $secret -Timestamp $timestamp
    return Invoke-JsonPost -RequestUri $uri -RawBody $RawBody -Headers @{ "Paddle-Signature" = $signature }
}

function New-SubscriptionPayload {
    param(
        [string]$EventId,
        [string]$EventType,
        [string]$SubscriptionId,
        [string]$CustomerId,
        [string]$UserId,
        [string]$Status,
        [string]$OccurredAt,
        [string]$PeriodStartsAt,
        [string]$PeriodEndsAt,
        [string]$PriceId,
        [string]$ProductId
    )

    $payloadObject = [ordered]@{
        event_id = $EventId
        event_type = $EventType
        occurred_at = $OccurredAt
        data = [ordered]@{
            id = $SubscriptionId
            customer_id = $CustomerId
            status = $Status
            custom_data = [ordered]@{
                internalUserId = $UserId
                internalPlanId = "premium"
            }
            current_billing_period = [ordered]@{
                starts_at = $PeriodStartsAt
                ends_at = $PeriodEndsAt
            }
            items = @(
                [ordered]@{
                    price = [ordered]@{
                        id = $PriceId
                        product_id = $ProductId
                    }
                }
            )
        }
    }

    return ($payloadObject | ConvertTo-Json -Depth 12 -Compress)
}

Write-Host "Paddle subscription lifecycle smoke test"
Write-Host ("BaseUrl: {0}" -f $BaseUrl)
Write-Host "Expected backend environment:"
Write-Host "  PaddleWebhook__Enabled=true"
Write-Host '  PaddleWebhook__SecretKey="test_webhook_secret"'
Write-Host "  PaddleWebhook__TimestampToleranceSeconds=300"
Write-Host ""

Write-Step "Registering a local smoke test user."
$userSuffix = New-RandomSuffix
$testEmail = ("paddle-subscription-lifecycle-smoke-{0}@example.test" -f $userSuffix)
$testPassword = ("SmokeTest!{0}" -f $userSuffix)
$registerBody = ([ordered]@{
    email = $testEmail
    password = $testPassword
    displayName = "Paddle Subscription Lifecycle Smoke"
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
$authHeaders = @{ "Authorization" = ("Bearer {0}" -f $accessToken) }
Write-Pass ("Registered smoke user and extracted evt_user_id={0} from JWT." -f $userId)

$eventSuffix = New-RandomSuffix
$subscriptionId = ("sub_test_{0}" -f $eventSuffix)
$customerId = ("ctm_test_{0}" -f $eventSuffix)
$priceId = ("pri_test_{0}" -f $eventSuffix)
$productId = ("pro_test_{0}" -f $eventSuffix)
$createdOccurredAt = [DateTimeOffset]::UtcNow.AddMinutes(-5).ToString("o")
$createdPeriodStartsAt = [DateTimeOffset]::UtcNow.AddMinutes(-10).ToString("o")
$createdPeriodEndsAt = [DateTimeOffset]::UtcNow.AddDays(30).ToString("o")

$createdPayload = New-SubscriptionPayload `
    -EventId ("evt_sub_created_{0}" -f $eventSuffix) `
    -EventType "subscription.created" `
    -SubscriptionId $subscriptionId `
    -CustomerId $customerId `
    -UserId $userId `
    -Status "active" `
    -OccurredAt $createdOccurredAt `
    -PeriodStartsAt $createdPeriodStartsAt `
    -PeriodEndsAt $createdPeriodEndsAt `
    -PriceId $priceId `
    -ProductId $productId

Write-Step "Posting signed subscription.created webhook."
$created = Invoke-SignedWebhookPost -RawBody $createdPayload
Assert-StatusCode -Response $created -ExpectedStatusCode 200 -Scenario "subscription.created webhook"
Assert-JsonFlag -Body $created.Body -PropertyName "accepted" -ExpectedValue $true -Scenario "subscription.created webhook"
Assert-JsonFlag -Body $created.Body -PropertyName "duplicate" -ExpectedValue $false -Scenario "subscription.created webhook"
Assert-JsonNumber -Body $created.Body -PropertyName "entitlementActivatedCount" -ExpectedValue 0 -Scenario "subscription.created webhook"
Write-Pass "subscription.created was accepted without activating entitlement."

$statusAfterCreated = Invoke-JsonGet -RequestUri ($BaseUrl.TrimEnd('/') + "/api/me/subscription-status") -Headers $authHeaders
Assert-StatusCode -Response $statusAfterCreated -ExpectedStatusCode 200 -Scenario "subscription status after created"
Assert-JsonString -Body $statusAfterCreated.Body -PropertyName "subscriptionStatus" -ExpectedValue "active" -Scenario "subscription status after created"
Assert-JsonString -Body $statusAfterCreated.Body -PropertyName "billingProvider" -ExpectedValue "paddle" -Scenario "subscription status after created"
Assert-JsonDateString -Body $statusAfterCreated.Body -PropertyName "currentPeriodEndUtc" -ExpectedValue $createdPeriodEndsAt -Scenario "subscription status after created"
Assert-JsonFlag -Body $statusAfterCreated.Body -PropertyName "premiumActive" -ExpectedValue $false -Scenario "subscription status after created"
Write-Pass "Subscription status shows the provider snapshot while Premium remains inactive."

Write-Step "Posting duplicate subscription.created webhook."
$duplicateCreated = Invoke-SignedWebhookPost -RawBody $createdPayload
Assert-StatusCode -Response $duplicateCreated -ExpectedStatusCode 200 -Scenario "duplicate subscription.created webhook"
Assert-JsonFlag -Body $duplicateCreated.Body -PropertyName "accepted" -ExpectedValue $true -Scenario "duplicate subscription.created webhook"
Assert-JsonFlag -Body $duplicateCreated.Body -PropertyName "duplicate" -ExpectedValue $true -Scenario "duplicate subscription.created webhook"
Assert-JsonNumber -Body $duplicateCreated.Body -PropertyName "entitlementActivatedCount" -ExpectedValue 0 -Scenario "duplicate subscription.created webhook"
$statusAfterDuplicateCreated = Invoke-JsonGet -RequestUri ($BaseUrl.TrimEnd('/') + "/api/me/subscription-status") -Headers $authHeaders
Assert-StatusCode -Response $statusAfterDuplicateCreated -ExpectedStatusCode 200 -Scenario "subscription status after duplicate created"
Assert-JsonString -Body $statusAfterDuplicateCreated.Body -PropertyName "subscriptionStatus" -ExpectedValue "active" -Scenario "subscription status after duplicate created"
Assert-JsonDateString -Body $statusAfterDuplicateCreated.Body -PropertyName "currentPeriodEndUtc" -ExpectedValue $createdPeriodEndsAt -Scenario "subscription status after duplicate created"
Assert-JsonFlag -Body $statusAfterDuplicateCreated.Body -PropertyName "premiumActive" -ExpectedValue $false -Scenario "subscription status after duplicate created"
Write-Pass "Duplicate subscription.created was accepted idempotently without changing the snapshot or entitlement state."

$updatedOccurredAt = [DateTimeOffset]::UtcNow.AddMinutes(5).ToString("o")
$updatedPeriodStartsAt = [DateTimeOffset]::UtcNow.AddDays(30).ToString("o")
$updatedPeriodEndsAt = [DateTimeOffset]::UtcNow.AddDays(60).ToString("o")
$updatedPayload = New-SubscriptionPayload `
    -EventId ("evt_sub_updated_{0}" -f $eventSuffix) `
    -EventType "subscription.updated" `
    -SubscriptionId $subscriptionId `
    -CustomerId $customerId `
    -UserId $userId `
    -Status "active" `
    -OccurredAt $updatedOccurredAt `
    -PeriodStartsAt $updatedPeriodStartsAt `
    -PeriodEndsAt $updatedPeriodEndsAt `
    -PriceId $priceId `
    -ProductId $productId

Write-Step "Posting signed subscription.updated webhook."
$updated = Invoke-SignedWebhookPost -RawBody $updatedPayload
Assert-StatusCode -Response $updated -ExpectedStatusCode 200 -Scenario "subscription.updated webhook"
Assert-JsonFlag -Body $updated.Body -PropertyName "accepted" -ExpectedValue $true -Scenario "subscription.updated webhook"
Assert-JsonNumber -Body $updated.Body -PropertyName "entitlementActivatedCount" -ExpectedValue 0 -Scenario "subscription.updated webhook"

$statusAfterUpdated = Invoke-JsonGet -RequestUri ($BaseUrl.TrimEnd('/') + "/api/me/subscription-status") -Headers $authHeaders
Assert-StatusCode -Response $statusAfterUpdated -ExpectedStatusCode 200 -Scenario "subscription status after updated"
Assert-JsonString -Body $statusAfterUpdated.Body -PropertyName "subscriptionStatus" -ExpectedValue "active" -Scenario "subscription status after updated"
Assert-JsonDateString -Body $statusAfterUpdated.Body -PropertyName "currentPeriodEndUtc" -ExpectedValue $updatedPeriodEndsAt -Scenario "subscription status after updated"
Assert-JsonFlag -Body $statusAfterUpdated.Body -PropertyName "premiumActive" -ExpectedValue $false -Scenario "subscription status after updated"
Write-Pass "subscription.updated updated the provider snapshot period without activating Premium."

$olderOccurredAt = [DateTimeOffset]::UtcNow.AddMinutes(-20).ToString("o")
$olderPeriodStartsAt = [DateTimeOffset]::UtcNow.AddDays(1).ToString("o")
$olderPeriodEndsAt = [DateTimeOffset]::UtcNow.AddDays(2).ToString("o")
$olderPayload = New-SubscriptionPayload `
    -EventId ("evt_sub_older_{0}" -f $eventSuffix) `
    -EventType "subscription.updated" `
    -SubscriptionId $subscriptionId `
    -CustomerId $customerId `
    -UserId $userId `
    -Status "active" `
    -OccurredAt $olderOccurredAt `
    -PeriodStartsAt $olderPeriodStartsAt `
    -PeriodEndsAt $olderPeriodEndsAt `
    -PriceId $priceId `
    -ProductId $productId

Write-Step "Posting older out-of-order subscription.updated webhook."
$older = Invoke-SignedWebhookPost -RawBody $olderPayload
Assert-StatusCode -Response $older -ExpectedStatusCode 200 -Scenario "older subscription.updated webhook"
Assert-JsonFlag -Body $older.Body -PropertyName "accepted" -ExpectedValue $true -Scenario "older subscription.updated webhook"
Assert-JsonNumber -Body $older.Body -PropertyName "entitlementActivatedCount" -ExpectedValue 0 -Scenario "older subscription.updated webhook"

$statusAfterOlder = Invoke-JsonGet -RequestUri ($BaseUrl.TrimEnd('/') + "/api/me/subscription-status") -Headers $authHeaders
Assert-StatusCode -Response $statusAfterOlder -ExpectedStatusCode 200 -Scenario "subscription status after older updated"
Assert-JsonString -Body $statusAfterOlder.Body -PropertyName "subscriptionStatus" -ExpectedValue "active" -Scenario "subscription status after older updated"
Assert-JsonDateString -Body $statusAfterOlder.Body -PropertyName "currentPeriodEndUtc" -ExpectedValue $updatedPeriodEndsAt -Scenario "subscription status after older updated"
Assert-JsonFlag -Body $statusAfterOlder.Body -PropertyName "premiumActive" -ExpectedValue $false -Scenario "subscription status after older updated"
Write-Pass "Older out-of-order subscription.updated did not regress the subscription snapshot and did not activate Premium."

Write-Pass "Paddle subscription lifecycle snapshot smoke test passed."
