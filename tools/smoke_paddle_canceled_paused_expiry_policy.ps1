param(
    [string]$BaseUrl = "http://localhost:5000"
)

$ErrorActionPreference = "Stop"

$secret = "test_webhook_secret"
$route = "/api/billing/webhooks/paddle"
$webhookUri = ($BaseUrl.TrimEnd('/') + $route)
$jsonContentType = "application/json"
$dateTimeComparisonToleranceSeconds = 1
$httpMethodsWithJsonBody = @("POST", "PUT", "PATCH")

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

function Invoke-JsonRequest {
    param(
        [string]$Method,
        [string]$RequestUri,
        [string]$RawBody,
        [hashtable]$Headers = @{}
    )

    $requestParameters = @{
        Uri = $RequestUri
        Method = $Method
        UseBasicParsing = $true
    }

    $normalizedMethod = $Method.ToUpperInvariant()
    $hasRawBody = $PSBoundParameters.ContainsKey("RawBody")
    if ($hasRawBody) {
        if (-not ($httpMethodsWithJsonBody -contains $normalizedMethod)) {
            Fail ("{0} {1}: request body is only supported for JSON body methods ({2})." -f $normalizedMethod, $RequestUri, ($httpMethodsWithJsonBody -join ", "))
        }

        $requestParameters.ContentType = $jsonContentType
        $requestParameters.Body = $RawBody
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

function Invoke-JsonGet {
    param(
        [string]$RequestUri,
        [hashtable]$Headers = @{}
    )

    return Invoke-JsonRequest -Method "Get" -RequestUri $RequestUri -Headers $Headers
}

function Invoke-JsonPost {
    param(
        [string]$RequestUri,
        [string]$RawBody,
        [hashtable]$Headers = @{}
    )

    return Invoke-JsonRequest -Method "Post" -RequestUri $RequestUri -RawBody $RawBody -Headers $Headers
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
        [pscustomobject]$Response,
        [int]$ExpectedStatusCode,
        [string]$Scenario
    )

    if ($Response.StatusCode -ne $ExpectedStatusCode) {
        Fail ("{0}: expected HTTP {1} but got {2}. Body: {3}" -f $Scenario, $ExpectedStatusCode, $Response.StatusCode, $Response.Body)
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
    if ([bool]$json.$PropertyName -ne $ExpectedValue) {
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

function Assert-JsonDateWithinSeconds {
    param(
        [string]$Body,
        [string]$PropertyName,
        [DateTimeOffset]$ExpectedValue,
        [int]$ToleranceSeconds,
        [string]$Scenario
    )

    $json = $Body | ConvertFrom-Json
    $actualText = [string]$json.$PropertyName
    if ([string]::IsNullOrWhiteSpace($actualText)) {
        Fail ("{0}: expected {1} to be present. Body: {2}" -f $Scenario, $PropertyName, $Body)
    }

    $actual = [DateTimeOffset]::Parse($actualText).ToUniversalTime()
    $expected = $ExpectedValue.ToUniversalTime()
    $delta = [Math]::Abs(($actual - $expected).TotalSeconds)
    if ($delta -gt $ToleranceSeconds) {
        Fail ("{0}: expected {1} within {2}s of {3:o} but got {4:o}. Body: {5}" -f $Scenario, $PropertyName, $ToleranceSeconds, $expected, $actual, $Body)
    }
}

function Assert-StatusDateWithinSeconds {
    param(
        [pscustomobject]$Status,
        [string]$PropertyName,
        [DateTimeOffset]$ExpectedValue,
        [string]$Scenario
    )

    $actualText = [string]$Status.$PropertyName
    if ([string]::IsNullOrWhiteSpace($actualText)) {
        Fail ("{0}: expected {1} to be present. Status: {2}" -f $Scenario, $PropertyName, ($Status | ConvertTo-Json -Depth 8 -Compress))
    }

    $actual = [DateTimeOffset]::Parse($actualText).ToUniversalTime()
    $expected = $ExpectedValue.ToUniversalTime()
    $delta = [Math]::Abs(($actual - $expected).TotalSeconds)
    if ($delta -gt $dateTimeComparisonToleranceSeconds) {
        Fail ("{0}: expected {1} within {2}s of {3:o} but got {4:o}." -f $Scenario, $PropertyName, $dateTimeComparisonToleranceSeconds, $expected, $actual)
    }
}

function New-SmokeUser {
    param([string]$NamePrefix)

    $suffix = New-RandomSuffix
    $registerBody = ([ordered]@{
        email = ("{0}-{1}@example.test" -f $NamePrefix, $suffix)
        password = ("SmokeTest!{0}" -f $suffix)
        displayName = "Paddle Canceled Paused Expiry Smoke"
    } | ConvertTo-Json -Depth 5 -Compress)

    $registerResponse = Invoke-JsonPost -RequestUri ($BaseUrl.TrimEnd('/') + "/api/auth/register") -RawBody $registerBody
    Assert-StatusCode -Response $registerResponse -ExpectedStatusCode 201 -Scenario ("register {0}" -f $NamePrefix)

    $authJson = $registerResponse.Body | ConvertFrom-Json
    $accessToken = $authJson.accessToken
    if ([string]::IsNullOrWhiteSpace($accessToken)) {
        Fail ("register {0}: accessToken was missing. Body: {1}" -f $NamePrefix, $registerResponse.Body)
    }

    $userId = Get-JwtClaimValue -AccessToken $accessToken -ClaimName "evt_user_id"
    $parsedUserId = [Guid]::Empty
    if (-not ([Guid]::TryParse($userId, [ref]$parsedUserId))) {
        Fail ("register {0}: evt_user_id claim was missing or invalid. Claim: {1}" -f $NamePrefix, $userId)
    }

    return [pscustomobject]@{
        UserId = $userId
        AccessToken = $accessToken
        Headers = @{ "Authorization" = ("Bearer {0}" -f $accessToken) }
    }
}

function New-TransactionPayload {
    param(
        [string]$EventType,
        [string]$UserId,
        [string]$SubscriptionId,
        [DateTimeOffset]$PeriodStartsAt,
        [DateTimeOffset]$PeriodEndsAt,
        [string]$EventSuffix = (New-RandomSuffix),
        [string]$TransactionSuffix = (New-RandomSuffix)
    )

    $transactionStatus = if ($EventType -eq "transaction.payment_failed") { "failed" } else { "completed" }

    return ([ordered]@{
        event_id = ("evt_test_{0}" -f $EventSuffix)
        event_type = $EventType
        occurred_at = [DateTimeOffset]::UtcNow.ToString("o")
        data = [ordered]@{
            id = ("txn_test_{0}" -f $TransactionSuffix)
            subscription_id = $SubscriptionId
            status = $transactionStatus
            custom_data = [ordered]@{
                evt_user_id = $UserId
                evt_plan_id = "premium"
            }
            billing_period = [ordered]@{
                starts_at = $PeriodStartsAt.ToUniversalTime().ToString("o")
                ends_at = $PeriodEndsAt.ToUniversalTime().ToString("o")
            }
        }
    } | ConvertTo-Json -Depth 10 -Compress)
}

function New-SubscriptionPayload {
    param(
        [string]$EventType,
        [string]$UserId,
        [string]$SubscriptionId,
        [string]$CustomerId,
        [DateTimeOffset]$PeriodStartsAt,
        [DateTimeOffset]$PeriodEndsAt,
        [string]$Status,
        [DateTimeOffset]$EffectiveAt,
        [bool]$ScheduledCancellation = $false,
        [DateTimeOffset]$ScheduledChangeEffectiveAt
    )

    $data = [ordered]@{
        id = $SubscriptionId
        status = $Status
        customer_id = $CustomerId
        custom_data = [ordered]@{
            evt_user_id = $UserId
            evt_plan_id = "premium"
        }
        current_billing_period = [ordered]@{
            starts_at = $PeriodStartsAt.ToUniversalTime().ToString("o")
            ends_at = $PeriodEndsAt.ToUniversalTime().ToString("o")
        }
        effective_at = $EffectiveAt.ToUniversalTime().ToString("o")
        items = @(
            [ordered]@{
                price = [ordered]@{
                    id = ("pri_test_{0}" -f (New-RandomSuffix))
                    product_id = ("pro_test_{0}" -f (New-RandomSuffix))
                }
            }
        )
    }

    if ($ScheduledCancellation) {
        $data["cancel_at_period_end"] = $true
        $data["scheduled_change"] = [ordered]@{
            action = "cancel"
            effective_at = $ScheduledChangeEffectiveAt.ToUniversalTime().ToString("o")
        }
    }

    return ([ordered]@{
        event_id = ("evt_test_{0}" -f (New-RandomSuffix))
        event_type = $EventType
        occurred_at = [DateTimeOffset]::UtcNow.ToString("o")
        data = $data
    } | ConvertTo-Json -Depth 12 -Compress)
}

function Invoke-SignedWebhook {
    param([string]$Payload)

    $timestamp = Get-UnixTimestamp
    $signature = New-PaddleSignature -RawBody $Payload -SecretKey $secret -Timestamp $timestamp
    return Invoke-JsonPost -RequestUri $webhookUri -RawBody $Payload -Headers @{ "Paddle-Signature" = $signature }
}

function Get-SubscriptionStatus {
    param([hashtable]$Headers)

    $response = Invoke-JsonGet -RequestUri ($BaseUrl.TrimEnd('/') + "/api/me/subscription-status") -Headers $Headers
    Assert-StatusCode -Response $response -ExpectedStatusCode 200 -Scenario "subscription status"
    return ($response.Body | ConvertFrom-Json)
}

function Assert-PremiumStatus {
    param(
        [hashtable]$Headers,
        [bool]$ExpectedPremiumActive,
        [string]$Scenario
    )

    $lessonAccessResponse = Invoke-JsonGet -RequestUri ($BaseUrl.TrimEnd('/') + "/api/me/lesson-access") -Headers $Headers
    Assert-StatusCode -Response $lessonAccessResponse -ExpectedStatusCode 200 -Scenario ("{0}: lesson access" -f $Scenario)
    Assert-JsonFlag -Body $lessonAccessResponse.Body -PropertyName "premiumActive" -ExpectedValue $ExpectedPremiumActive -Scenario ("{0}: lesson access premiumActive" -f $Scenario)

    $subscriptionStatusResponse = Invoke-JsonGet -RequestUri ($BaseUrl.TrimEnd('/') + "/api/me/subscription-status") -Headers $Headers
    Assert-StatusCode -Response $subscriptionStatusResponse -ExpectedStatusCode 200 -Scenario ("{0}: subscription status" -f $Scenario)
    Assert-JsonFlag -Body $subscriptionStatusResponse.Body -PropertyName "premiumActive" -ExpectedValue $ExpectedPremiumActive -Scenario ("{0}: subscription status premiumActive" -f $Scenario)
}


function Assert-WebhookEntitlementExpiry {
    param(
        [pscustomobject]$Response,
        [DateTimeOffset]$ExpectedValue,
        [string]$Scenario
    )

    Assert-JsonNumber -Body $Response.Body -PropertyName "providerEventEntitlementExpiredCount" -ExpectedValue 1 -Scenario $Scenario
    Assert-JsonDateWithinSeconds -Body $Response.Body -PropertyName "providerEventEntitlementExpiresAtUtc" -ExpectedValue $ExpectedValue -ToleranceSeconds $dateTimeComparisonToleranceSeconds -Scenario $Scenario
}

Write-Host "Paddle canceled/paused expiry policy smoke test"
Write-Host ("BaseUrl: {0}" -f $BaseUrl)
Write-Host "Expected backend environment:"
Write-Host "  PaddleWebhook__Enabled=true"
Write-Host '  PaddleWebhook__SecretKey="test_webhook_secret"'
Write-Host "  PaddleWebhook__TimestampToleranceSeconds=300"
Write-Host ""

Write-Step "Registering canceled-event smoke user."
$canceledUser = New-SmokeUser -NamePrefix "paddle-canceled-expiry-smoke"
Write-Pass ("Registered canceled-event smoke user {0}." -f $canceledUser.UserId)

$canceledPeriodStartsAt = [DateTimeOffset]::UtcNow.AddMinutes(-1)
$canceledPeriodEndsAt = [DateTimeOffset]::UtcNow.AddDays(30)
$canceledSubscriptionId = ("sub_test_{0}" -f (New-RandomSuffix))
$canceledCustomerId = ("ctm_test_{0}" -f (New-RandomSuffix))

Write-Step "Posting transaction.completed for canceled-event smoke user with period end +30 days."
$canceledCompletedPayload = New-TransactionPayload -EventType "transaction.completed" -UserId $canceledUser.UserId -SubscriptionId $canceledSubscriptionId -PeriodStartsAt $canceledPeriodStartsAt -PeriodEndsAt $canceledPeriodEndsAt
$canceledCompletedResponse = Invoke-SignedWebhook -Payload $canceledCompletedPayload
Assert-StatusCode -Response $canceledCompletedResponse -ExpectedStatusCode 200 -Scenario "canceled user transaction.completed"
Assert-JsonFlag -Body $canceledCompletedResponse.Body -PropertyName "accepted" -ExpectedValue $true -Scenario "canceled user transaction.completed"
Assert-JsonNumber -Body $canceledCompletedResponse.Body -PropertyName "entitlementActivatedCount" -ExpectedValue 1 -Scenario "canceled user transaction.completed"
Assert-JsonDateWithinSeconds -Body $canceledCompletedResponse.Body -PropertyName "entitlementExpiresAtUtc" -ExpectedValue $canceledPeriodEndsAt -ToleranceSeconds $dateTimeComparisonToleranceSeconds -Scenario "canceled user transaction.completed"
Assert-PremiumStatus -Headers $canceledUser.Headers -ExpectedPremiumActive $true -Scenario "canceled user after transaction.completed"
Write-Pass "transaction.completed created active provider_event Premium entitlement for canceled-event smoke user."

Write-Step "Posting actual subscription.canceled for the same provider subscription."
$canceledEffectiveAt = [DateTimeOffset]::UtcNow
$canceledPayload = New-SubscriptionPayload -EventType "subscription.canceled" -UserId $canceledUser.UserId -SubscriptionId $canceledSubscriptionId -CustomerId $canceledCustomerId -PeriodStartsAt $canceledPeriodStartsAt -PeriodEndsAt $canceledPeriodEndsAt -Status "canceled" -EffectiveAt $canceledEffectiveAt
$canceledResponse = Invoke-SignedWebhook -Payload $canceledPayload
Assert-StatusCode -Response $canceledResponse -ExpectedStatusCode 200 -Scenario "subscription.canceled"
Assert-JsonFlag -Body $canceledResponse.Body -PropertyName "accepted" -ExpectedValue $true -Scenario "subscription.canceled"
Assert-WebhookEntitlementExpiry -Response $canceledResponse -ExpectedValue $canceledEffectiveAt -Scenario "subscription.canceled entitlement expiry"
Assert-PremiumStatus -Headers $canceledUser.Headers -ExpectedPremiumActive $false -Scenario "after subscription.canceled"
$canceledStatus = Get-SubscriptionStatus -Headers $canceledUser.Headers
Assert-JsonString -Body ($canceledStatus | ConvertTo-Json -Depth 8 -Compress) -PropertyName "subscriptionStatus" -ExpectedValue "canceled" -Scenario "subscription status after canceled"
Write-Pass "subscription.canceled expired only the provider_event Premium entitlement and made Premium inactive."

Write-Step "Posting duplicate subscription.canceled."
$duplicateCanceledResponse = Invoke-SignedWebhook -Payload $canceledPayload
Assert-StatusCode -Response $duplicateCanceledResponse -ExpectedStatusCode 200 -Scenario "duplicate subscription.canceled"
Assert-JsonFlag -Body $duplicateCanceledResponse.Body -PropertyName "accepted" -ExpectedValue $true -Scenario "duplicate subscription.canceled"
Assert-JsonFlag -Body $duplicateCanceledResponse.Body -PropertyName "duplicate" -ExpectedValue $true -Scenario "duplicate subscription.canceled"
Assert-JsonNumber -Body $duplicateCanceledResponse.Body -PropertyName "providerEventEntitlementExpiredCount" -ExpectedValue 0 -Scenario "duplicate subscription.canceled"
Assert-PremiumStatus -Headers $canceledUser.Headers -ExpectedPremiumActive $false -Scenario "after duplicate subscription.canceled"
Write-Pass "Duplicate subscription.canceled was accepted and did not repeat entitlement mutation."

Write-Step "Registering paused-event smoke user."
$pausedUser = New-SmokeUser -NamePrefix "paddle-paused-expiry-smoke"
Write-Pass ("Registered paused-event smoke user {0}." -f $pausedUser.UserId)

$pausedPeriodStartsAt = [DateTimeOffset]::UtcNow.AddMinutes(-1)
$pausedPeriodEndsAt = [DateTimeOffset]::UtcNow.AddDays(30)
$pausedSubscriptionId = ("sub_test_{0}" -f (New-RandomSuffix))
$pausedCustomerId = ("ctm_test_{0}" -f (New-RandomSuffix))

Write-Step "Posting transaction.completed for paused-event smoke user with period end +30 days."
$pausedCompletedPayload = New-TransactionPayload -EventType "transaction.completed" -UserId $pausedUser.UserId -SubscriptionId $pausedSubscriptionId -PeriodStartsAt $pausedPeriodStartsAt -PeriodEndsAt $pausedPeriodEndsAt
$pausedCompletedResponse = Invoke-SignedWebhook -Payload $pausedCompletedPayload
Assert-StatusCode -Response $pausedCompletedResponse -ExpectedStatusCode 200 -Scenario "paused user transaction.completed"
Assert-JsonFlag -Body $pausedCompletedResponse.Body -PropertyName "accepted" -ExpectedValue $true -Scenario "paused user transaction.completed"
Assert-JsonNumber -Body $pausedCompletedResponse.Body -PropertyName "entitlementActivatedCount" -ExpectedValue 1 -Scenario "paused user transaction.completed"
Assert-JsonDateWithinSeconds -Body $pausedCompletedResponse.Body -PropertyName "entitlementExpiresAtUtc" -ExpectedValue $pausedPeriodEndsAt -ToleranceSeconds $dateTimeComparisonToleranceSeconds -Scenario "paused user transaction.completed"
Assert-PremiumStatus -Headers $pausedUser.Headers -ExpectedPremiumActive $true -Scenario "paused user after transaction.completed"
Write-Pass "transaction.completed created active provider_event Premium entitlement for paused-event smoke user."

Write-Step "Posting actual subscription.paused for the same provider subscription."
$pausedEffectiveAt = [DateTimeOffset]::UtcNow
$pausedPayload = New-SubscriptionPayload -EventType "subscription.paused" -UserId $pausedUser.UserId -SubscriptionId $pausedSubscriptionId -CustomerId $pausedCustomerId -PeriodStartsAt $pausedPeriodStartsAt -PeriodEndsAt $pausedPeriodEndsAt -Status "paused" -EffectiveAt $pausedEffectiveAt
$pausedResponse = Invoke-SignedWebhook -Payload $pausedPayload
Assert-StatusCode -Response $pausedResponse -ExpectedStatusCode 200 -Scenario "subscription.paused"
Assert-JsonFlag -Body $pausedResponse.Body -PropertyName "accepted" -ExpectedValue $true -Scenario "subscription.paused"
Assert-WebhookEntitlementExpiry -Response $pausedResponse -ExpectedValue $pausedEffectiveAt -Scenario "subscription.paused entitlement expiry"
Assert-PremiumStatus -Headers $pausedUser.Headers -ExpectedPremiumActive $false -Scenario "after subscription.paused"
$pausedStatus = Get-SubscriptionStatus -Headers $pausedUser.Headers
Assert-JsonString -Body ($pausedStatus | ConvertTo-Json -Depth 8 -Compress) -PropertyName "subscriptionStatus" -ExpectedValue "paused" -Scenario "subscription status after paused"
Write-Pass "subscription.paused expired only the provider_event Premium entitlement and made Premium inactive."

Write-Pass "Paddle canceled/paused expiry policy smoke test passed. Run the existing scheduled-cancellation, entitlement-extension, payment-persistence, subscription-lifecycle, and webhook-ingestion smokes separately to verify unchanged policies."
