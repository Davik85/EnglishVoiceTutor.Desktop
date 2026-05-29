param(
    [string]$BaseUrl = "http://localhost:5000"
)

$ErrorActionPreference = "Stop"

$secret = "test_webhook_secret"
$route = "/api/billing/webhooks/paddle"
$uri = ($BaseUrl.TrimEnd('/') + $route)

function Write-Step { param([string]$Message) Write-Host ("[STEP] {0}" -f $Message) }
function Write-Pass { param([string]$Message) Write-Host ("[PASS] {0}" -f $Message) }
function Fail { param([string]$Message) throw $Message }
function New-RandomSuffix { return ([Guid]::NewGuid().ToString("N").Substring(0, 12)) }
function Get-UnixTimestamp { return [int64]([DateTimeOffset]::UtcNow.ToUnixTimeSeconds()) }

function ConvertTo-HexString {
    param([byte[]]$Bytes)

    $builder = New-Object System.Text.StringBuilder
    foreach ($byte in $Bytes) { [void]$builder.Append($byte.ToString("x2")) }
    return $builder.ToString()
}

function New-PaddleSignature {
    param([string]$RawBody, [string]$SecretKey, [int64]$Timestamp)

    $payload = ("{0}:{1}" -f $Timestamp, $RawBody)
    $encoding = [System.Text.Encoding]::UTF8
    $hmac = New-Object System.Security.Cryptography.HMACSHA256
    $hmac.Key = $encoding.GetBytes($SecretKey)
    try { $hash = $hmac.ComputeHash($encoding.GetBytes($payload)) }
    finally { $hmac.Dispose() }

    return ("ts={0};h1={1}" -f $Timestamp, (ConvertTo-HexString -Bytes $hash))
}

function Read-HttpResponseBody {
    param([System.Net.WebResponse]$Response)

    if ($null -eq $Response) { return "" }
    $stream = $Response.GetResponseStream()
    if ($null -eq $stream) { return "" }
    $reader = New-Object System.IO.StreamReader($stream)
    try { return $reader.ReadToEnd() }
    finally { $reader.Dispose() }
}

function Invoke-JsonPost {
    param([string]$RequestUri, [string]$RawBody, [hashtable]$Headers = @{})

    $requestParameters = @{ Uri = $RequestUri; Method = "Post"; ContentType = "application/json"; Body = $RawBody; UseBasicParsing = $true }
    if (($null -ne $Headers) -and ($Headers.Count -gt 0)) { $requestParameters.Headers = $Headers }

    try {
        $response = Invoke-WebRequest @requestParameters
        return [pscustomobject]@{ StatusCode = [int]$response.StatusCode; Body = $response.Content }
    }
    catch [System.Net.WebException] {
        $httpResponse = $_.Exception.Response
        if ($null -eq $httpResponse) { Fail "No HTTP response was returned. Check that backend is running and endpoint did not crash." }
        try { return [pscustomobject]@{ StatusCode = [int]$httpResponse.StatusCode; Body = (Read-HttpResponseBody -Response $httpResponse) } }
        finally { $httpResponse.Dispose() }
    }
}

function Invoke-JsonGet {
    param([string]$RequestUri, [hashtable]$Headers = @{})

    $requestParameters = @{ Uri = $RequestUri; Method = "Get"; UseBasicParsing = $true }
    if (($null -ne $Headers) -and ($Headers.Count -gt 0)) { $requestParameters.Headers = $Headers }

    try {
        $response = Invoke-WebRequest @requestParameters
        return [pscustomobject]@{ StatusCode = [int]$response.StatusCode; Body = $response.Content }
    }
    catch [System.Net.WebException] {
        $httpResponse = $_.Exception.Response
        if ($null -eq $httpResponse) { Fail "No HTTP response was returned. Check that backend is running and endpoint did not crash." }
        try { return [pscustomobject]@{ StatusCode = [int]$httpResponse.StatusCode; Body = (Read-HttpResponseBody -Response $httpResponse) } }
        finally { $httpResponse.Dispose() }
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
    param([string]$AccessToken, [string]$ClaimName)

    $parts = $AccessToken.Split('.')
    if ($parts.Length -lt 2) { Fail "Access token is not a JWT." }
    $payload = (ConvertFrom-Base64UrlString -Value $parts[1]) | ConvertFrom-Json
    return $payload.$ClaimName
}

function Assert-StatusCode {
    param([object]$Response, [int]$ExpectedStatusCode, [string]$Scenario)

    if ($Response.StatusCode -ne $ExpectedStatusCode) { Fail ("{0}: expected HTTP {1}, got {2}. Body: {3}" -f $Scenario, $ExpectedStatusCode, $Response.StatusCode, $Response.Body) }
}

function Assert-JsonFlag {
    param([string]$Body, [string]$PropertyName, [bool]$ExpectedValue, [string]$Scenario)

    $json = $Body | ConvertFrom-Json
    if ($json.$PropertyName -ne $ExpectedValue) { Fail ("{0}: expected {1}={2}. Body: {3}" -f $Scenario, $PropertyName, $ExpectedValue, $Body) }
}

function Assert-JsonNumber {
    param([string]$Body, [string]$PropertyName, [int]$ExpectedValue, [string]$Scenario)

    $json = $Body | ConvertFrom-Json
    if ([int]$json.$PropertyName -ne $ExpectedValue) { Fail ("{0}: expected {1}={2}. Body: {3}" -f $Scenario, $PropertyName, $ExpectedValue, $Body) }
}

function Assert-JsonString {
    param([string]$Body, [string]$PropertyName, [string]$ExpectedValue, [string]$Scenario)

    $json = $Body | ConvertFrom-Json
    if ([string]$json.$PropertyName -ne $ExpectedValue) { Fail ("{0}: expected {1}={2}. Body: {3}" -f $Scenario, $PropertyName, $ExpectedValue, $Body) }
}

function New-SmokeUser {
    param([string]$DisplayName)

    $suffix = New-RandomSuffix
    $body = ([ordered]@{
        email = ("paddle-payment-{0}@example.test" -f $suffix)
        password = ("SmokeTest!{0}" -f $suffix)
        displayName = $DisplayName
    } | ConvertTo-Json -Depth 5 -Compress)

    $response = Invoke-JsonPost -RequestUri ($BaseUrl.TrimEnd('/') + "/api/auth/register") -RawBody $body
    Assert-StatusCode -Response $response -ExpectedStatusCode 201 -Scenario "register smoke user"
    $auth = $response.Body | ConvertFrom-Json
    if ([string]::IsNullOrWhiteSpace($auth.accessToken)) { Fail ("register smoke user: accessToken was missing. Body: {0}" -f $response.Body) }

    $userId = Get-JwtClaimValue -AccessToken $auth.accessToken -ClaimName "evt_user_id"
    $parsedUserId = [Guid]::Empty
    if (-not ([Guid]::TryParse($userId, [ref]$parsedUserId))) { Fail ("register smoke user: evt_user_id claim was missing or invalid. Claim: {0}" -f $userId) }

    return [pscustomobject]@{ UserId = $userId; AccessToken = $auth.accessToken; Headers = @{ "Authorization" = ("Bearer {0}" -f $auth.accessToken) } }
}

function New-TransactionPayload {
    param(
        [string]$EventId,
        [string]$EventType,
        [string]$TransactionId,
        [string]$UserId,
        [string]$OccurredAt,
        [string]$SubscriptionId,
        [string]$CustomerId,
        [string]$PriceId,
        [string]$ProductId,
        [int]$AmountMinor,
        [string]$Currency
    )

    $data = [ordered]@{
        id = $TransactionId
        status = $(if ($EventType -eq "transaction.payment_failed") { "payment_failed" } else { "completed" })
        customer_id = $CustomerId
        subscription_id = $SubscriptionId
        currency_code = $Currency
        custom_data = [ordered]@{ evt_user_id = $UserId; evt_plan_id = "premium"; internalUserId = $UserId; internalPlanId = "premium" }
        details = [ordered]@{ totals = [ordered]@{ total = ("{0}" -f $AmountMinor) }; currency_code = $Currency }
        items = @([ordered]@{ price = [ordered]@{ id = $PriceId; product_id = $ProductId } })
        billing_period = [ordered]@{ starts_at = [DateTimeOffset]::UtcNow.ToString("o"); ends_at = [DateTimeOffset]::UtcNow.AddDays(7).ToString("o") }
    }

    if ($EventType -eq "transaction.payment_failed") { $data.failed_at = $OccurredAt } else { $data.completed_at = $OccurredAt; $data.paid_at = $OccurredAt }

    return ([ordered]@{ event_id = $EventId; event_type = $EventType; occurred_at = $OccurredAt; data = $data } | ConvertTo-Json -Depth 10 -Compress)
}

function Invoke-SignedWebhookPost {
    param([string]$RawBody)

    $timestamp = Get-UnixTimestamp
    $signature = New-PaddleSignature -RawBody $RawBody -SecretKey $secret -Timestamp $timestamp
    return Invoke-JsonPost -RequestUri $uri -RawBody $RawBody -Headers @{ "Paddle-Signature" = $signature }
}

Write-Host "Paddle payment persistence smoke test"
Write-Host ("BaseUrl: {0}" -f $BaseUrl)
Write-Host "Expected backend environment:"
Write-Host "  PaddleWebhook__Enabled=true"
Write-Host '  PaddleWebhook__SecretKey="test_webhook_secret"'
Write-Host "  PaddleWebhook__TimestampToleranceSeconds=300"
Write-Host ""

Write-Step "Registering smoke users."
$completedUser = New-SmokeUser -DisplayName "Paddle Payment Completed Smoke"
$failedUser = New-SmokeUser -DisplayName "Paddle Payment Failed Smoke"
Write-Pass "Registered payment smoke users."

$suffix = New-RandomSuffix
$completedPayload = New-TransactionPayload `
    -EventId ("evt_pay_completed_{0}" -f $suffix) `
    -EventType "transaction.completed" `
    -TransactionId ("txn_pay_completed_{0}" -f $suffix) `
    -UserId $completedUser.UserId `
    -OccurredAt ([DateTimeOffset]::UtcNow.ToString("o")) `
    -SubscriptionId ("sub_pay_completed_{0}" -f $suffix) `
    -CustomerId ("ctm_pay_completed_{0}" -f $suffix) `
    -PriceId ("pri_pay_completed_{0}" -f $suffix) `
    -ProductId ("pro_pay_completed_{0}" -f $suffix) `
    -AmountMinor 1299 `
    -Currency "USD"

Write-Step "Posting signed transaction.completed webhook."
$completed = Invoke-SignedWebhookPost -RawBody $completedPayload
Assert-StatusCode -Response $completed -ExpectedStatusCode 200 -Scenario "transaction.completed webhook"
Assert-JsonFlag -Body $completed.Body -PropertyName "accepted" -ExpectedValue $true -Scenario "transaction.completed webhook"
Assert-JsonNumber -Body $completed.Body -PropertyName "paymentPersistedOrUpdatedCount" -ExpectedValue 1 -Scenario "transaction.completed webhook"
Assert-JsonNumber -Body $completed.Body -PropertyName "paymentPersistenceFailed" -ExpectedValue 0 -Scenario "transaction.completed webhook"
Assert-JsonString -Body $completed.Body -PropertyName "paymentStatus" -ExpectedValue "completed" -Scenario "transaction.completed webhook"
Assert-JsonNumber -Body $completed.Body -PropertyName "entitlementActivatedCount" -ExpectedValue 1 -Scenario "transaction.completed webhook"
Write-Pass "transaction.completed persisted or updated one payment and preserved entitlement activation."

Write-Step "Posting duplicate transaction.completed webhook."
$completedDuplicate = Invoke-SignedWebhookPost -RawBody $completedPayload
Assert-StatusCode -Response $completedDuplicate -ExpectedStatusCode 200 -Scenario "duplicate transaction.completed webhook"
Assert-JsonFlag -Body $completedDuplicate.Body -PropertyName "duplicate" -ExpectedValue $true -Scenario "duplicate transaction.completed webhook"
Assert-JsonNumber -Body $completedDuplicate.Body -PropertyName "paymentPersistedOrUpdatedCount" -ExpectedValue 0 -Scenario "duplicate transaction.completed webhook"
Assert-JsonNumber -Body $completedDuplicate.Body -PropertyName "paymentAlreadyCurrentCount" -ExpectedValue 1 -Scenario "duplicate transaction.completed webhook"
Assert-JsonNumber -Body $completedDuplicate.Body -PropertyName "entitlementActivatedCount" -ExpectedValue 0 -Scenario "duplicate transaction.completed webhook"
Write-Pass "Duplicate transaction.completed did not duplicate PaymentEntity or entitlement activation."

$completedStatus = Invoke-JsonGet -RequestUri ($BaseUrl.TrimEnd('/') + "/api/me/subscription-status") -Headers $completedUser.Headers
Assert-StatusCode -Response $completedStatus -ExpectedStatusCode 200 -Scenario "completed user subscription status"
Assert-JsonFlag -Body $completedStatus.Body -PropertyName "premiumActive" -ExpectedValue $true -Scenario "completed user subscription status"
Assert-JsonString -Body $completedStatus.Body -PropertyName "planId" -ExpectedValue "premium" -Scenario "completed user subscription status"
Write-Pass "Existing entitlement activation path still makes the completed user Premium."

$failedSuffix = New-RandomSuffix
$failedPayload = New-TransactionPayload `
    -EventId ("evt_pay_failed_{0}" -f $failedSuffix) `
    -EventType "transaction.payment_failed" `
    -TransactionId ("txn_pay_failed_{0}" -f $failedSuffix) `
    -UserId $failedUser.UserId `
    -OccurredAt ([DateTimeOffset]::UtcNow.ToString("o")) `
    -SubscriptionId ("sub_pay_failed_{0}" -f $failedSuffix) `
    -CustomerId ("ctm_pay_failed_{0}" -f $failedSuffix) `
    -PriceId ("pri_pay_failed_{0}" -f $failedSuffix) `
    -ProductId ("pro_pay_failed_{0}" -f $failedSuffix) `
    -AmountMinor 1299 `
    -Currency "USD"

Write-Step "Posting signed transaction.payment_failed webhook."
$failed = Invoke-SignedWebhookPost -RawBody $failedPayload
Assert-StatusCode -Response $failed -ExpectedStatusCode 200 -Scenario "transaction.payment_failed webhook"
Assert-JsonFlag -Body $failed.Body -PropertyName "accepted" -ExpectedValue $true -Scenario "transaction.payment_failed webhook"
Assert-JsonNumber -Body $failed.Body -PropertyName "paymentPersistedOrUpdatedCount" -ExpectedValue 1 -Scenario "transaction.payment_failed webhook"
Assert-JsonNumber -Body $failed.Body -PropertyName "paymentPersistenceFailed" -ExpectedValue 0 -Scenario "transaction.payment_failed webhook"
Assert-JsonString -Body $failed.Body -PropertyName "paymentStatus" -ExpectedValue "failed" -Scenario "transaction.payment_failed webhook"
Assert-JsonNumber -Body $failed.Body -PropertyName "entitlementActivatedCount" -ExpectedValue 0 -Scenario "transaction.payment_failed webhook"
Write-Pass "transaction.payment_failed persisted or updated one Failed payment snapshot without activating Premium."

$failedStatus = Invoke-JsonGet -RequestUri ($BaseUrl.TrimEnd('/') + "/api/me/subscription-status") -Headers $failedUser.Headers
Assert-StatusCode -Response $failedStatus -ExpectedStatusCode 200 -Scenario "failed user subscription status"
Assert-JsonFlag -Body $failedStatus.Body -PropertyName "premiumActive" -ExpectedValue $false -Scenario "failed user subscription status"
Write-Pass "transaction.payment_failed did not activate Premium."

Write-Step "Posting duplicate transaction.payment_failed webhook."
$failedDuplicate = Invoke-SignedWebhookPost -RawBody $failedPayload
Assert-StatusCode -Response $failedDuplicate -ExpectedStatusCode 200 -Scenario "duplicate transaction.payment_failed webhook"
Assert-JsonFlag -Body $failedDuplicate.Body -PropertyName "duplicate" -ExpectedValue $true -Scenario "duplicate transaction.payment_failed webhook"
Assert-JsonNumber -Body $failedDuplicate.Body -PropertyName "paymentPersistedOrUpdatedCount" -ExpectedValue 0 -Scenario "duplicate transaction.payment_failed webhook"
Assert-JsonNumber -Body $failedDuplicate.Body -PropertyName "paymentAlreadyCurrentCount" -ExpectedValue 1 -Scenario "duplicate transaction.payment_failed webhook"
Assert-JsonNumber -Body $failedDuplicate.Body -PropertyName "entitlementActivatedCount" -ExpectedValue 0 -Scenario "duplicate transaction.payment_failed webhook"
Write-Pass "Duplicate transaction.payment_failed did not duplicate PaymentEntity or activate Premium."

Write-Pass "Paddle payment persistence smoke test passed."
