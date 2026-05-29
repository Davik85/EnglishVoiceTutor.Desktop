param(
    [string]$BaseUrl = "http://localhost:5000"
)

$ErrorActionPreference = "Stop"

$secret = "test_webhook_secret"
$route = "/api/billing/webhooks/paddle"
$webhookUri = ($BaseUrl.TrimEnd('/') + $route)
$jsonContentType = "application/json"
$dateTimeComparisonToleranceSeconds = 1
$httpMethodsWithJsonBody = @(
    "POST",
    "PUT",
    "PATCH"
)

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

function New-SmokeUser {
    param([string]$NamePrefix)

    $suffix = New-RandomSuffix
    $registerBody = ([ordered]@{
        email = ("{0}-{1}@example.test" -f $NamePrefix, $suffix)
        password = ("SmokeTest!{0}" -f $suffix)
        displayName = "Paddle Entitlement Extension Smoke"
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

    return ([ordered]@{
        event_id = ("evt_test_{0}" -f $EventSuffix)
        event_type = $EventType
        occurred_at = [DateTimeOffset]::UtcNow.ToString("o")
        data = [ordered]@{
            id = ("txn_test_{0}" -f $TransactionSuffix)
            subscription_id = $SubscriptionId
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
        [DateTimeOffset]$PeriodStartsAt,
        [DateTimeOffset]$PeriodEndsAt
    )

    return ([ordered]@{
        event_id = ("evt_test_{0}" -f (New-RandomSuffix))
        event_type = $EventType
        occurred_at = [DateTimeOffset]::UtcNow.ToString("o")
        data = [ordered]@{
            id = $SubscriptionId
            status = "active"
            customer_id = ("ctm_test_{0}" -f (New-RandomSuffix))
            custom_data = [ordered]@{
                evt_user_id = $UserId
                evt_plan_id = "premium"
            }
            current_billing_period = [ordered]@{
                starts_at = $PeriodStartsAt.ToUniversalTime().ToString("o")
                ends_at = $PeriodEndsAt.ToUniversalTime().ToString("o")
            }
        }
    } | ConvertTo-Json -Depth 10 -Compress)
}

function Invoke-SignedWebhook {
    param([string]$Payload)

    $timestamp = Get-UnixTimestamp
    $signature = New-PaddleSignature -RawBody $Payload -SecretKey $secret -Timestamp $timestamp
    return Invoke-JsonPost -RequestUri $webhookUri -RawBody $Payload -Headers @{ "Paddle-Signature" = $signature }
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

    if ($ExpectedPremiumActive) {
        Assert-JsonString -Body $subscriptionStatusResponse.Body -PropertyName "planId" -ExpectedValue "premium" -Scenario ("{0}: subscription status planId" -f $Scenario)
    }
}

Write-Host "Paddle entitlement extension smoke test"
Write-Host ("BaseUrl: {0}" -f $BaseUrl)
Write-Host "Expected backend environment:"
Write-Host "  PaddleWebhook__Enabled=true"
Write-Host '  PaddleWebhook__SecretKey="test_webhook_secret"'
Write-Host "  PaddleWebhook__TimestampToleranceSeconds=300"
Write-Host ""

Write-Step "Registering primary smoke user."
$primaryUser = New-SmokeUser -NamePrefix "paddle-entitlement-extension-smoke"
Write-Pass ("Registered primary smoke user {0}." -f $primaryUser.UserId)

$periodStartsAt = [DateTimeOffset]::UtcNow.AddMinutes(-1)
$firstPeriodEndsAt = [DateTimeOffset]::UtcNow.AddDays(30)
$renewalPeriodEndsAt = [DateTimeOffset]::UtcNow.AddDays(60)
$olderPeriodEndsAt = [DateTimeOffset]::UtcNow.AddDays(15)
$subscriptionId = ("sub_test_{0}" -f (New-RandomSuffix))
$firstEventSuffix = New-RandomSuffix
$firstTransactionSuffix = New-RandomSuffix

Write-Step "Posting first valid transaction.completed with period end +30 days."
$firstPayload = New-TransactionPayload -EventType "transaction.completed" -UserId $primaryUser.UserId -SubscriptionId $subscriptionId -PeriodStartsAt $periodStartsAt -PeriodEndsAt $firstPeriodEndsAt -EventSuffix $firstEventSuffix -TransactionSuffix $firstTransactionSuffix
$firstResponse = Invoke-SignedWebhook -Payload $firstPayload
Assert-StatusCode -Response $firstResponse -ExpectedStatusCode 200 -Scenario "first transaction.completed"
Assert-JsonFlag -Body $firstResponse.Body -PropertyName "accepted" -ExpectedValue $true -Scenario "first transaction.completed"
Assert-JsonFlag -Body $firstResponse.Body -PropertyName "duplicate" -ExpectedValue $false -Scenario "first transaction.completed"
Assert-JsonFlag -Body $firstResponse.Body -PropertyName "entitlementActivated" -ExpectedValue $true -Scenario "first transaction.completed"
Assert-JsonNumber -Body $firstResponse.Body -PropertyName "entitlementActivatedCount" -ExpectedValue 1 -Scenario "first transaction.completed"
Assert-JsonNumber -Body $firstResponse.Body -PropertyName "entitlementActivationBlocked" -ExpectedValue 0 -Scenario "first transaction.completed"
Assert-JsonNumber -Body $firstResponse.Body -PropertyName "entitlementActivationFailed" -ExpectedValue 0 -Scenario "first transaction.completed"
Assert-JsonDateWithinSeconds -Body $firstResponse.Body -PropertyName "entitlementExpiresAtUtc" -ExpectedValue $firstPeriodEndsAt -ToleranceSeconds $dateTimeComparisonToleranceSeconds -Scenario "first transaction.completed"
Assert-PremiumStatus -Headers $primaryUser.Headers -ExpectedPremiumActive $true -Scenario "after first transaction.completed"
Write-Pass "First transaction.completed created a provider-event Premium entitlement."

Write-Step "Posting duplicate transaction.completed."
$duplicateResponse = Invoke-SignedWebhook -Payload $firstPayload
Assert-StatusCode -Response $duplicateResponse -ExpectedStatusCode 200 -Scenario "duplicate transaction.completed"
Assert-JsonFlag -Body $duplicateResponse.Body -PropertyName "accepted" -ExpectedValue $true -Scenario "duplicate transaction.completed"
Assert-JsonFlag -Body $duplicateResponse.Body -PropertyName "duplicate" -ExpectedValue $true -Scenario "duplicate transaction.completed"
Assert-JsonFlag -Body $duplicateResponse.Body -PropertyName "billingEventCreated" -ExpectedValue $false -Scenario "duplicate transaction.completed"
Assert-JsonFlag -Body $duplicateResponse.Body -PropertyName "entitlementActivated" -ExpectedValue $false -Scenario "duplicate transaction.completed"
Assert-JsonNumber -Body $duplicateResponse.Body -PropertyName "entitlementActivatedCount" -ExpectedValue 0 -Scenario "duplicate transaction.completed"
Assert-PremiumStatus -Headers $primaryUser.Headers -ExpectedPremiumActive $true -Scenario "after duplicate transaction.completed"
Write-Pass "Duplicate transaction.completed did not duplicate or change the entitlement."

Write-Step "Posting renewal-like transaction.completed with period end +60 days."
$renewalPayload = New-TransactionPayload -EventType "transaction.completed" -UserId $primaryUser.UserId -SubscriptionId $subscriptionId -PeriodStartsAt $firstPeriodEndsAt -PeriodEndsAt $renewalPeriodEndsAt
$renewalResponse = Invoke-SignedWebhook -Payload $renewalPayload
Assert-StatusCode -Response $renewalResponse -ExpectedStatusCode 200 -Scenario "renewal transaction.completed"
Assert-JsonFlag -Body $renewalResponse.Body -PropertyName "duplicate" -ExpectedValue $false -Scenario "renewal transaction.completed"
Assert-JsonFlag -Body $renewalResponse.Body -PropertyName "entitlementActivated" -ExpectedValue $true -Scenario "renewal transaction.completed"
Assert-JsonNumber -Body $renewalResponse.Body -PropertyName "entitlementActivatedCount" -ExpectedValue 1 -Scenario "renewal transaction.completed"
Assert-JsonDateWithinSeconds -Body $renewalResponse.Body -PropertyName "entitlementExpiresAtUtc" -ExpectedValue $renewalPeriodEndsAt -ToleranceSeconds $dateTimeComparisonToleranceSeconds -Scenario "renewal transaction.completed"
Assert-PremiumStatus -Headers $primaryUser.Headers -ExpectedPremiumActive $true -Scenario "after renewal transaction.completed"
Write-Pass "Renewal-like transaction.completed extended the provider-event Premium entitlement."

Write-Step "Posting older out-of-order transaction.completed with an earlier period end."
$olderPayload = New-TransactionPayload -EventType "transaction.completed" -UserId $primaryUser.UserId -SubscriptionId $subscriptionId -PeriodStartsAt $periodStartsAt -PeriodEndsAt $olderPeriodEndsAt
$olderResponse = Invoke-SignedWebhook -Payload $olderPayload
Assert-StatusCode -Response $olderResponse -ExpectedStatusCode 200 -Scenario "older transaction.completed"
Assert-JsonFlag -Body $olderResponse.Body -PropertyName "duplicate" -ExpectedValue $false -Scenario "older transaction.completed"
Assert-JsonFlag -Body $olderResponse.Body -PropertyName "entitlementActivated" -ExpectedValue $false -Scenario "older transaction.completed"
Assert-JsonNumber -Body $olderResponse.Body -PropertyName "entitlementActivatedCount" -ExpectedValue 0 -Scenario "older transaction.completed"
Assert-JsonDateWithinSeconds -Body $olderResponse.Body -PropertyName "entitlementExpiresAtUtc" -ExpectedValue $renewalPeriodEndsAt -ToleranceSeconds $dateTimeComparisonToleranceSeconds -Scenario "older transaction.completed"
Assert-PremiumStatus -Headers $primaryUser.Headers -ExpectedPremiumActive $true -Scenario "after older transaction.completed"
Write-Pass "Older out-of-order transaction.completed did not shorten the provider-event Premium entitlement."

Write-Step "Posting transaction.payment_failed for another smoke user."
$failedUser = New-SmokeUser -NamePrefix "paddle-entitlement-extension-failed-smoke"
$failedPayload = New-TransactionPayload -EventType "transaction.payment_failed" -UserId $failedUser.UserId -SubscriptionId ("sub_test_{0}" -f (New-RandomSuffix)) -PeriodStartsAt $periodStartsAt -PeriodEndsAt ([DateTimeOffset]::UtcNow.AddDays(90))
$failedResponse = Invoke-SignedWebhook -Payload $failedPayload
Assert-StatusCode -Response $failedResponse -ExpectedStatusCode 200 -Scenario "transaction.payment_failed"
Assert-JsonFlag -Body $failedResponse.Body -PropertyName "accepted" -ExpectedValue $true -Scenario "transaction.payment_failed"
Assert-JsonFlag -Body $failedResponse.Body -PropertyName "entitlementActivated" -ExpectedValue $false -Scenario "transaction.payment_failed"
Assert-JsonNumber -Body $failedResponse.Body -PropertyName "entitlementActivatedCount" -ExpectedValue 0 -Scenario "transaction.payment_failed"
Assert-PremiumStatus -Headers $failedUser.Headers -ExpectedPremiumActive $false -Scenario "after transaction.payment_failed"
Write-Pass "transaction.payment_failed did not create or extend Premium."

Write-Step "Posting subscription.created and subscription.updated for another smoke user."
$subscriptionOnlyUser = New-SmokeUser -NamePrefix "paddle-entitlement-extension-subscription-smoke"
$subscriptionOnlyId = ("sub_test_{0}" -f (New-RandomSuffix))
$subscriptionCreatedPayload = New-SubscriptionPayload -EventType "subscription.created" -UserId $subscriptionOnlyUser.UserId -SubscriptionId $subscriptionOnlyId -PeriodStartsAt $periodStartsAt -PeriodEndsAt ([DateTimeOffset]::UtcNow.AddDays(30))
$subscriptionCreatedResponse = Invoke-SignedWebhook -Payload $subscriptionCreatedPayload
Assert-StatusCode -Response $subscriptionCreatedResponse -ExpectedStatusCode 200 -Scenario "subscription.created"
Assert-JsonFlag -Body $subscriptionCreatedResponse.Body -PropertyName "entitlementActivated" -ExpectedValue $false -Scenario "subscription.created"
Assert-JsonNumber -Body $subscriptionCreatedResponse.Body -PropertyName "entitlementActivatedCount" -ExpectedValue 0 -Scenario "subscription.created"
Assert-PremiumStatus -Headers $subscriptionOnlyUser.Headers -ExpectedPremiumActive $false -Scenario "after subscription.created"

$subscriptionUpdatedPayload = New-SubscriptionPayload -EventType "subscription.updated" -UserId $subscriptionOnlyUser.UserId -SubscriptionId $subscriptionOnlyId -PeriodStartsAt $periodStartsAt -PeriodEndsAt ([DateTimeOffset]::UtcNow.AddDays(60))
$subscriptionUpdatedResponse = Invoke-SignedWebhook -Payload $subscriptionUpdatedPayload
Assert-StatusCode -Response $subscriptionUpdatedResponse -ExpectedStatusCode 200 -Scenario "subscription.updated"
Assert-JsonFlag -Body $subscriptionUpdatedResponse.Body -PropertyName "entitlementActivated" -ExpectedValue $false -Scenario "subscription.updated"
Assert-JsonNumber -Body $subscriptionUpdatedResponse.Body -PropertyName "entitlementActivatedCount" -ExpectedValue 0 -Scenario "subscription.updated"
Assert-PremiumStatus -Headers $subscriptionOnlyUser.Headers -ExpectedPremiumActive $false -Scenario "after subscription.updated"
Write-Pass "subscription.created and subscription.updated did not activate Premium by themselves."

Write-Pass "Paddle entitlement extension smoke test passed. Run tools/smoke_paddle_subscription_lifecycle.ps1 separately to keep subscription snapshot smoke coverage explicit."
