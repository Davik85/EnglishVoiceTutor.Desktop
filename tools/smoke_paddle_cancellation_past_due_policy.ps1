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
        displayName = "Paddle Cancellation Past Due Smoke"
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

Write-Host "Paddle scheduled cancellation and past_due policy smoke test"
Write-Host ("BaseUrl: {0}" -f $BaseUrl)
Write-Host "Expected backend environment:"
Write-Host "  PaddleWebhook__Enabled=true"
Write-Host '  PaddleWebhook__SecretKey="test_webhook_secret"'
Write-Host "  PaddleWebhook__TimestampToleranceSeconds=300"
Write-Host ""

Write-Step "Registering primary smoke user."
$primaryUser = New-SmokeUser -NamePrefix "paddle-cancel-past-due-primary-smoke"
Write-Pass ("Registered primary smoke user {0}." -f $primaryUser.UserId)

$periodStartsAt = [DateTimeOffset]::UtcNow.AddMinutes(-1)
$periodEndsAt = [DateTimeOffset]::UtcNow.AddDays(30)
$subscriptionId = ("sub_test_{0}" -f (New-RandomSuffix))
$customerId = ("ctm_test_{0}" -f (New-RandomSuffix))

Write-Step "Posting valid transaction.completed with period end +30 days."
$completedPayload = New-TransactionPayload -EventType "transaction.completed" -UserId $primaryUser.UserId -SubscriptionId $subscriptionId -PeriodStartsAt $periodStartsAt -PeriodEndsAt $periodEndsAt
$completedResponse = Invoke-SignedWebhook -Payload $completedPayload
Assert-StatusCode -Response $completedResponse -ExpectedStatusCode 200 -Scenario "transaction.completed"
Assert-JsonFlag -Body $completedResponse.Body -PropertyName "accepted" -ExpectedValue $true -Scenario "transaction.completed"
Assert-JsonNumber -Body $completedResponse.Body -PropertyName "entitlementActivatedCount" -ExpectedValue 1 -Scenario "transaction.completed"
Assert-JsonDateWithinSeconds -Body $completedResponse.Body -PropertyName "entitlementExpiresAtUtc" -ExpectedValue $periodEndsAt -ToleranceSeconds $dateTimeComparisonToleranceSeconds -Scenario "transaction.completed"
Assert-PremiumStatus -Headers $primaryUser.Headers -ExpectedPremiumActive $true -Scenario "after transaction.completed"
Write-Pass "transaction.completed created active Premium entitlement."

Write-Step "Posting subscription.updated with scheduled cancellation at the current period end."
$scheduledCancellationPayload = New-SubscriptionPayload -EventType "subscription.updated" -UserId $primaryUser.UserId -SubscriptionId $subscriptionId -CustomerId $customerId -PeriodStartsAt $periodStartsAt -PeriodEndsAt $periodEndsAt -Status "active" -ScheduledCancellation $true -ScheduledChangeEffectiveAt $periodEndsAt
$scheduledCancellationResponse = Invoke-SignedWebhook -Payload $scheduledCancellationPayload
Assert-StatusCode -Response $scheduledCancellationResponse -ExpectedStatusCode 200 -Scenario "scheduled cancellation subscription.updated"
Assert-JsonFlag -Body $scheduledCancellationResponse.Body -PropertyName "accepted" -ExpectedValue $true -Scenario "scheduled cancellation subscription.updated"
Assert-JsonNumber -Body $scheduledCancellationResponse.Body -PropertyName "entitlementActivatedCount" -ExpectedValue 0 -Scenario "scheduled cancellation subscription.updated"
Assert-PremiumStatus -Headers $primaryUser.Headers -ExpectedPremiumActive $true -Scenario "after scheduled cancellation"
$statusAfterScheduledCancellation = Get-SubscriptionStatus -Headers $primaryUser.Headers
if ($statusAfterScheduledCancellation.PSObject.Properties.Name -contains "cancelAtPeriodEnd") {
    if ([bool]$statusAfterScheduledCancellation.cancelAtPeriodEnd -ne $true) {
        Fail ("scheduled cancellation status: expected cancelAtPeriodEnd=true. Status: {0}" -f ($statusAfterScheduledCancellation | ConvertTo-Json -Depth 8 -Compress))
    }
}
if ($statusAfterScheduledCancellation.PSObject.Properties.Name -contains "scheduledChangeAction") {
    if ([string]$statusAfterScheduledCancellation.scheduledChangeAction -ne "cancel") {
        Fail ("scheduled cancellation status: expected scheduledChangeAction=cancel. Status: {0}" -f ($statusAfterScheduledCancellation | ConvertTo-Json -Depth 8 -Compress))
    }
}
if ($statusAfterScheduledCancellation.PSObject.Properties.Name -contains "scheduledChangeEffectiveAtUtc") {
    Assert-StatusDateWithinSeconds -Status $statusAfterScheduledCancellation -PropertyName "scheduledChangeEffectiveAtUtc" -ExpectedValue $periodEndsAt -Scenario "scheduled cancellation status"
}
Assert-StatusDateWithinSeconds -Status $statusAfterScheduledCancellation -PropertyName "premiumEntitlementExpiresAtUtc" -ExpectedValue $periodEndsAt -Scenario "scheduled cancellation should not shorten entitlement"
Write-Pass "Scheduled cancellation was recorded while Premium and entitlement expiry stayed unchanged."

Write-Step "Posting subscription.past_due for the same provider subscription."
$pastDuePayload = New-SubscriptionPayload -EventType "subscription.past_due" -UserId $primaryUser.UserId -SubscriptionId $subscriptionId -CustomerId $customerId -PeriodStartsAt $periodStartsAt -PeriodEndsAt $periodEndsAt -Status "past_due"
$pastDueResponse = Invoke-SignedWebhook -Payload $pastDuePayload
Assert-StatusCode -Response $pastDueResponse -ExpectedStatusCode 200 -Scenario "subscription.past_due primary"
Assert-JsonFlag -Body $pastDueResponse.Body -PropertyName "accepted" -ExpectedValue $true -Scenario "subscription.past_due primary"
Assert-JsonNumber -Body $pastDueResponse.Body -PropertyName "entitlementActivatedCount" -ExpectedValue 0 -Scenario "subscription.past_due primary"
Assert-PremiumStatus -Headers $primaryUser.Headers -ExpectedPremiumActive $true -Scenario "after subscription.past_due with existing entitlement"
$statusAfterPastDue = Get-SubscriptionStatus -Headers $primaryUser.Headers
Assert-JsonString -Body ($statusAfterPastDue | ConvertTo-Json -Depth 8 -Compress) -PropertyName "subscriptionStatus" -ExpectedValue "past_due" -Scenario "subscription status after past_due"
Assert-StatusDateWithinSeconds -Status $statusAfterPastDue -PropertyName "premiumEntitlementExpiresAtUtc" -ExpectedValue $periodEndsAt -Scenario "past_due should not shorten entitlement"
Write-Pass "subscription.past_due updated the subscription snapshot while existing Premium remained active."

Write-Step "Registering secondary smoke user without a successful transaction.completed."
$secondaryUser = New-SmokeUser -NamePrefix "paddle-cancel-past-due-secondary-smoke"
Assert-PremiumStatus -Headers $secondaryUser.Headers -ExpectedPremiumActive $false -Scenario "secondary initial status"
Write-Pass ("Registered secondary smoke user {0}." -f $secondaryUser.UserId)

Write-Step "Posting subscription.past_due for secondary user without existing entitlement."
$secondarySubscriptionId = ("sub_test_{0}" -f (New-RandomSuffix))
$secondaryCustomerId = ("ctm_test_{0}" -f (New-RandomSuffix))
$secondaryPastDuePayload = New-SubscriptionPayload -EventType "subscription.past_due" -UserId $secondaryUser.UserId -SubscriptionId $secondarySubscriptionId -CustomerId $secondaryCustomerId -PeriodStartsAt $periodStartsAt -PeriodEndsAt $periodEndsAt -Status "past_due"
$secondaryPastDueResponse = Invoke-SignedWebhook -Payload $secondaryPastDuePayload
Assert-StatusCode -Response $secondaryPastDueResponse -ExpectedStatusCode 200 -Scenario "subscription.past_due secondary"
Assert-JsonFlag -Body $secondaryPastDueResponse.Body -PropertyName "accepted" -ExpectedValue $true -Scenario "subscription.past_due secondary"
Assert-JsonNumber -Body $secondaryPastDueResponse.Body -PropertyName "entitlementActivatedCount" -ExpectedValue 0 -Scenario "subscription.past_due secondary"
Assert-PremiumStatus -Headers $secondaryUser.Headers -ExpectedPremiumActive $false -Scenario "secondary after subscription.past_due"
$secondaryStatusAfterPastDue = Get-SubscriptionStatus -Headers $secondaryUser.Headers
Assert-JsonString -Body ($secondaryStatusAfterPastDue | ConvertTo-Json -Depth 8 -Compress) -PropertyName "subscriptionStatus" -ExpectedValue "past_due" -Scenario "secondary subscription status after past_due"
Write-Pass "subscription.past_due did not make a user Premium without an active entitlement."

Write-Step "Posting transaction.payment_failed for secondary user."
$paymentFailedPayload = New-TransactionPayload -EventType "transaction.payment_failed" -UserId $secondaryUser.UserId -SubscriptionId $secondarySubscriptionId -PeriodStartsAt $periodStartsAt -PeriodEndsAt $periodEndsAt
$paymentFailedResponse = Invoke-SignedWebhook -Payload $paymentFailedPayload
Assert-StatusCode -Response $paymentFailedResponse -ExpectedStatusCode 200 -Scenario "transaction.payment_failed secondary"
Assert-JsonFlag -Body $paymentFailedResponse.Body -PropertyName "accepted" -ExpectedValue $true -Scenario "transaction.payment_failed secondary"
Assert-JsonNumber -Body $paymentFailedResponse.Body -PropertyName "entitlementActivatedCount" -ExpectedValue 0 -Scenario "transaction.payment_failed secondary"
Assert-PremiumStatus -Headers $secondaryUser.Headers -ExpectedPremiumActive $false -Scenario "secondary after transaction.payment_failed"
Write-Pass "transaction.payment_failed persisted diagnostics without activating Premium."

Write-Pass "Paddle scheduled cancellation and past_due policy smoke test passed."
