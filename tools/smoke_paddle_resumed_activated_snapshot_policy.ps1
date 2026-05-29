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
        displayName = "Paddle Resumed Activated Snapshot Smoke"
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
        [string]$UserId,
        [string]$SubscriptionId,
        [DateTimeOffset]$PeriodStartsAt,
        [DateTimeOffset]$PeriodEndsAt,
        [string]$EventSuffix = (New-RandomSuffix),
        [string]$TransactionSuffix = (New-RandomSuffix)
    )

    return ([ordered]@{
        event_id = ("evt_test_{0}" -f $EventSuffix)
        event_type = "transaction.completed"
        occurred_at = [DateTimeOffset]::UtcNow.ToString("o")
        data = [ordered]@{
            id = ("txn_test_{0}" -f $TransactionSuffix)
            subscription_id = $SubscriptionId
            status = "completed"
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
        [DateTimeOffset]$EffectiveAt = [DateTimeOffset]::UtcNow
    )

    return ([ordered]@{
        event_id = ("evt_test_{0}" -f (New-RandomSuffix))
        event_type = $EventType
        occurred_at = [DateTimeOffset]::UtcNow.ToString("o")
        data = [ordered]@{
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

function Assert-SnapshotOnlyWebhook {
    param(
        [pscustomobject]$Response,
        [string]$Scenario
    )

    Assert-StatusCode -Response $Response -ExpectedStatusCode 200 -Scenario $Scenario
    Assert-JsonFlag -Body $Response.Body -PropertyName "accepted" -ExpectedValue $true -Scenario $Scenario
    Assert-JsonNumber -Body $Response.Body -PropertyName "entitlementActivatedCount" -ExpectedValue 0 -Scenario $Scenario
    Assert-JsonNumber -Body $Response.Body -PropertyName "providerEventEntitlementExpiredCount" -ExpectedValue 0 -Scenario $Scenario
}

function Assert-ActiveSubscriptionStatus {
    param(
        [hashtable]$Headers,
        [string]$Scenario
    )

    $status = Get-SubscriptionStatus -Headers $Headers
    if ([string]$status.subscriptionStatus -ne "active") {
        Fail ("{0}: expected subscriptionStatus=active. Status: {1}" -f $Scenario, ($status | ConvertTo-Json -Depth 8 -Compress))
    }
}

Write-Host "Paddle resumed/activated snapshot-only policy smoke test"
Write-Host ("BaseUrl: {0}" -f $BaseUrl)
Write-Host "Expected backend environment:"
Write-Host "  PaddleWebhook__Enabled=true"
Write-Host '  PaddleWebhook__SecretKey="test_webhook_secret"'
Write-Host "  PaddleWebhook__TimestampToleranceSeconds=300"
Write-Host ""

Write-Step "Registering resumed snapshot-only smoke user."
$resumedUser = New-SmokeUser -NamePrefix "paddle-resumed-snapshot-smoke"
$resumedSubscriptionId = ("sub_test_{0}" -f (New-RandomSuffix))
$resumedCustomerId = ("ctm_test_{0}" -f (New-RandomSuffix))
$resumedPeriodStartsAt = [DateTimeOffset]::UtcNow.AddMinutes(-1)
$resumedPeriodEndsAt = [DateTimeOffset]::UtcNow.AddDays(30)
$resumedPayload = New-SubscriptionPayload -EventType "subscription.resumed" -UserId $resumedUser.UserId -SubscriptionId $resumedSubscriptionId -CustomerId $resumedCustomerId -PeriodStartsAt $resumedPeriodStartsAt -PeriodEndsAt $resumedPeriodEndsAt -Status "active"
$resumedResponse = Invoke-SignedWebhook -Payload $resumedPayload
Assert-SnapshotOnlyWebhook -Response $resumedResponse -Scenario "subscription.resumed snapshot-only"
Assert-ActiveSubscriptionStatus -Headers $resumedUser.Headers -Scenario "subscription.resumed snapshot-only"
Assert-PremiumStatus -Headers $resumedUser.Headers -ExpectedPremiumActive $false -Scenario "subscription.resumed snapshot-only"
Write-Pass "subscription.resumed updated the subscription snapshot to active without creating, extending, or restoring Premium."

Write-Step "Registering activated snapshot-only smoke user."
$activatedUser = New-SmokeUser -NamePrefix "paddle-activated-snapshot-smoke"
$activatedSubscriptionId = ("sub_test_{0}" -f (New-RandomSuffix))
$activatedCustomerId = ("ctm_test_{0}" -f (New-RandomSuffix))
$activatedPeriodStartsAt = [DateTimeOffset]::UtcNow.AddMinutes(-1)
$activatedPeriodEndsAt = [DateTimeOffset]::UtcNow.AddDays(30)
$activatedPayload = New-SubscriptionPayload -EventType "subscription.activated" -UserId $activatedUser.UserId -SubscriptionId $activatedSubscriptionId -CustomerId $activatedCustomerId -PeriodStartsAt $activatedPeriodStartsAt -PeriodEndsAt $activatedPeriodEndsAt -Status "active"
$activatedResponse = Invoke-SignedWebhook -Payload $activatedPayload
Assert-SnapshotOnlyWebhook -Response $activatedResponse -Scenario "subscription.activated snapshot-only"
Assert-ActiveSubscriptionStatus -Headers $activatedUser.Headers -Scenario "subscription.activated snapshot-only"
Assert-PremiumStatus -Headers $activatedUser.Headers -ExpectedPremiumActive $false -Scenario "subscription.activated snapshot-only"
Write-Pass "subscription.activated updated the subscription snapshot to active without creating, extending, or restoring Premium."

Write-Step "Registering restoration-policy smoke user."
$restorationUser = New-SmokeUser -NamePrefix "paddle-restoration-policy-smoke"
$restorationSubscriptionId = ("sub_test_{0}" -f (New-RandomSuffix))
$restorationCustomerId = ("ctm_test_{0}" -f (New-RandomSuffix))
$restorationPeriodStartsAt = [DateTimeOffset]::UtcNow.AddMinutes(-1)
$firstRestorationPeriodEndsAt = [DateTimeOffset]::UtcNow.AddDays(30)

Write-Step "Posting transaction.completed with period end +30 days."
$firstCompletedPayload = New-TransactionPayload -UserId $restorationUser.UserId -SubscriptionId $restorationSubscriptionId -PeriodStartsAt $restorationPeriodStartsAt -PeriodEndsAt $firstRestorationPeriodEndsAt
$firstCompletedResponse = Invoke-SignedWebhook -Payload $firstCompletedPayload
Assert-StatusCode -Response $firstCompletedResponse -ExpectedStatusCode 200 -Scenario "restoration user first transaction.completed"
Assert-JsonFlag -Body $firstCompletedResponse.Body -PropertyName "accepted" -ExpectedValue $true -Scenario "restoration user first transaction.completed"
Assert-JsonNumber -Body $firstCompletedResponse.Body -PropertyName "entitlementActivatedCount" -ExpectedValue 1 -Scenario "restoration user first transaction.completed"
Assert-JsonDateWithinSeconds -Body $firstCompletedResponse.Body -PropertyName "entitlementExpiresAtUtc" -ExpectedValue $firstRestorationPeriodEndsAt -ToleranceSeconds $dateTimeComparisonToleranceSeconds -Scenario "restoration user first transaction.completed"
Assert-PremiumStatus -Headers $restorationUser.Headers -ExpectedPremiumActive $true -Scenario "after first transaction.completed"
Write-Pass "transaction.completed made the restoration-policy user Premium."

Write-Step "Posting subscription.canceled for the same provider subscription."
$canceledEffectiveAt = [DateTimeOffset]::UtcNow
$canceledPayload = New-SubscriptionPayload -EventType "subscription.canceled" -UserId $restorationUser.UserId -SubscriptionId $restorationSubscriptionId -CustomerId $restorationCustomerId -PeriodStartsAt $restorationPeriodStartsAt -PeriodEndsAt $firstRestorationPeriodEndsAt -Status "canceled" -EffectiveAt $canceledEffectiveAt
$canceledResponse = Invoke-SignedWebhook -Payload $canceledPayload
Assert-StatusCode -Response $canceledResponse -ExpectedStatusCode 200 -Scenario "restoration user subscription.canceled"
Assert-JsonFlag -Body $canceledResponse.Body -PropertyName "accepted" -ExpectedValue $true -Scenario "restoration user subscription.canceled"
Assert-JsonNumber -Body $canceledResponse.Body -PropertyName "providerEventEntitlementExpiredCount" -ExpectedValue 1 -Scenario "restoration user subscription.canceled"
Assert-JsonDateWithinSeconds -Body $canceledResponse.Body -PropertyName "providerEventEntitlementExpiresAtUtc" -ExpectedValue $canceledEffectiveAt -ToleranceSeconds $dateTimeComparisonToleranceSeconds -Scenario "restoration user subscription.canceled"
Assert-PremiumStatus -Headers $restorationUser.Headers -ExpectedPremiumActive $false -Scenario "after subscription.canceled"
Write-Pass "subscription.canceled expired the active provider_event entitlement."

Write-Step "Posting subscription.resumed for the same provider subscription after cancellation."
$resumedAfterCanceledPayload = New-SubscriptionPayload -EventType "subscription.resumed" -UserId $restorationUser.UserId -SubscriptionId $restorationSubscriptionId -CustomerId $restorationCustomerId -PeriodStartsAt $restorationPeriodStartsAt -PeriodEndsAt $firstRestorationPeriodEndsAt -Status "active"
$resumedAfterCanceledResponse = Invoke-SignedWebhook -Payload $resumedAfterCanceledPayload
Assert-SnapshotOnlyWebhook -Response $resumedAfterCanceledResponse -Scenario "subscription.resumed after canceled"
Assert-ActiveSubscriptionStatus -Headers $restorationUser.Headers -Scenario "subscription.resumed after canceled"
Assert-PremiumStatus -Headers $restorationUser.Headers -ExpectedPremiumActive $false -Scenario "subscription.resumed after canceled"
Write-Pass "subscription.resumed changed the snapshot back to active while Premium remained inactive."

Write-Step "Posting a new transaction.completed for the same provider subscription with a future period end."
$secondRestorationPeriodEndsAt = [DateTimeOffset]::UtcNow.AddDays(60)
$secondCompletedPayload = New-TransactionPayload -UserId $restorationUser.UserId -SubscriptionId $restorationSubscriptionId -PeriodStartsAt ([DateTimeOffset]::UtcNow) -PeriodEndsAt $secondRestorationPeriodEndsAt
$secondCompletedResponse = Invoke-SignedWebhook -Payload $secondCompletedPayload
Assert-StatusCode -Response $secondCompletedResponse -ExpectedStatusCode 200 -Scenario "restoration user second transaction.completed"
Assert-JsonFlag -Body $secondCompletedResponse.Body -PropertyName "accepted" -ExpectedValue $true -Scenario "restoration user second transaction.completed"
Assert-JsonNumber -Body $secondCompletedResponse.Body -PropertyName "entitlementActivatedCount" -ExpectedValue 1 -Scenario "restoration user second transaction.completed"
Assert-JsonDateWithinSeconds -Body $secondCompletedResponse.Body -PropertyName "entitlementExpiresAtUtc" -ExpectedValue $secondRestorationPeriodEndsAt -ToleranceSeconds $dateTimeComparisonToleranceSeconds -Scenario "restoration user second transaction.completed"
Assert-PremiumStatus -Headers $restorationUser.Headers -ExpectedPremiumActive $true -Scenario "after second transaction.completed"
Write-Pass "A new valid transaction.completed restored Premium through the entitlement activation path."

Write-Pass "Paddle resumed/activated snapshot-only policy smoke test passed. Run the existing Paddle regression smoke scripts separately to verify unchanged policies."
