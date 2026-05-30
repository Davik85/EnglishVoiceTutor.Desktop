param(
    [string]$BaseUrl = "http://localhost:5000",
    [string]$Email = "",
    [string]$Password = "TestPassword123!",
    [switch]$AllowRealPaddleCall
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$JsonContentType = "application/json"
$MethodPost = "POST"
$StatusOk = 200

$AuthRegisterPath = "/api/auth/register"
$AuthLoginPath = "/api/auth/login"
$CheckoutSessionPath = "/api/me/billing/checkout-session"

function Write-Step {
    param([string]$Message)
    Write-Host "[STEP] $Message" -ForegroundColor Cyan
}

function Write-Pass {
    param([string]$Message)
    Write-Host "[PASS] $Message" -ForegroundColor Green
}

function Fail {
    param([string]$Message)
    Write-Host "[FAIL] $Message" -ForegroundColor Red
    exit 1
}

function Invoke-Json {
    param(
        [string]$Method,
        [string]$Url,
        [hashtable]$Headers,
        $Body
    )

    $invokeParams = @{
        Method = $Method
        Uri = $Url
        UseBasicParsing = $true
    }

    if ($Headers) {
        $invokeParams.Headers = $Headers
    }

    if ($null -ne $Body) {
        $invokeParams.Body = ($Body | ConvertTo-Json -Depth 10)
        $invokeParams.ContentType = $JsonContentType
    }

    $response = Invoke-WebRequest @invokeParams

    $parsedBody = $null
    if (-not [string]::IsNullOrWhiteSpace($response.Content)) {
        $parsedBody = $response.Content | ConvertFrom-Json
    }

    return [pscustomobject]@{
        StatusCode = [int]$response.StatusCode
        Body = $parsedBody
    }
}

function Assert-Equal {
    param(
        $Expected,
        $Actual,
        [string]$Message
    )

    if ($Expected -ne $Actual) {
        throw "Assert-Equal failed: $Message. Expected '$Expected', actual '$Actual'."
    }
}

function Assert-Empty {
    param(
        $Value,
        [string]$Message
    )

    if (-not [string]::IsNullOrWhiteSpace([string]$Value)) {
        throw "Assert-Empty failed: $Message. Actual '$Value'."
    }
}

function Assert-NotEmpty {
    param(
        $Value,
        [string]$Message
    )

    if ([string]::IsNullOrWhiteSpace([string]$Value)) {
        throw "Assert-NotEmpty failed: $Message."
    }
}

function Assert-PaddleHostedCheckoutUrl {
    param(
        [string]$Value,
        [string]$Message
    )

    $parsedUri = $null
    if (-not [System.Uri]::TryCreate($Value, [System.UriKind]::Absolute, [ref]$parsedUri)) {
        throw "Assert-PaddleHostedCheckoutUrl failed: $Message. URL is not absolute."
    }

    if ($parsedUri.Scheme -ne "https" -or $parsedUri.Host -ne "pay.paddle.io" -or -not $parsedUri.AbsolutePath.StartsWith("/checkout/")) {
        throw "Assert-PaddleHostedCheckoutUrl failed: $Message. Actual '$Value'."
    }

    if ($parsedUri.Query -match "[?&]_ptxn=") {
        throw "Assert-PaddleHostedCheckoutUrl failed: $Message. URL contains legacy _ptxn query parameter."
    }
}

try {
    if (-not $AllowRealPaddleCall) {
        Fail "Refusing to create a real Paddle sandbox transaction without -AllowRealPaddleCall."
    }

    Write-Host "This test creates a real Paddle sandbox transaction. No internal entitlement is activated." -ForegroundColor Yellow
    Write-Host "Backend must already be running with Paddle sandbox checkout environment variables configured outside this script." -ForegroundColor Yellow
    Write-Host "Set PaddleBilling__HostedCheckoutUrl to a sandbox Paddle hosted checkout link when Paddle returns a default payment URL that the backend does not serve." -ForegroundColor Yellow
    Write-Host "BaseUrl: $BaseUrl"

    if ([string]::IsNullOrWhiteSpace($Email)) {
        $Email = "paddle-live-sandbox-smoke+$([Guid]::NewGuid().ToString('N'))@example.com"
    }

    Write-Host "Test email: $Email"

    $registerUrl = "$BaseUrl$AuthRegisterPath"
    $loginUrl = "$BaseUrl$AuthLoginPath"
    $checkoutSessionUrl = "$BaseUrl$CheckoutSessionPath"

    $authBody = @{ email = $Email; password = $Password }

    Write-Step "Register or login Paddle sandbox smoke user"
    $authResult = $null

    try {
        $registerResult = Invoke-Json -Method $MethodPost -Url $registerUrl -Headers $null -Body $authBody
        if ($registerResult.StatusCode -eq $StatusOk) {
            $authResult = $registerResult
        }
        else {
            $authResult = Invoke-Json -Method $MethodPost -Url $loginUrl -Headers $null -Body $authBody
        }
    }
    catch {
        $registerStatus = $null
        if ($_.Exception.Response -and $_.Exception.Response.StatusCode) {
            $registerStatus = [int]$_.Exception.Response.StatusCode.value__
        }

        if ($null -ne $registerStatus -and $registerStatus -ge 400 -and $registerStatus -lt 500) {
            $authResult = Invoke-Json -Method $MethodPost -Url $loginUrl -Headers $null -Body $authBody
        }
        else {
            throw
        }
    }

    Assert-Equal -Expected $StatusOk -Actual $authResult.StatusCode -Message "auth response status"
    Assert-NotEmpty -Value $authResult.Body.accessToken -Message "auth accessToken must not be empty"
    Write-Pass "Auth succeeded"

    $headers = @{ Authorization = "Bearer $($authResult.Body.accessToken)" }
    $checkoutBody = @{
        planId = "premium"
        returnUrl = "https://example.com/paddle-smoke/success"
        cancelUrl = "https://example.com/paddle-smoke/cancel"
    }

    Write-Step "Create real Paddle sandbox checkout transaction"
    $checkoutResult = Invoke-Json -Method $MethodPost -Url $checkoutSessionUrl -Headers $headers -Body $checkoutBody

    Assert-Equal -Expected $StatusOk -Actual $checkoutResult.StatusCode -Message "checkout response status"
    Assert-Equal -Expected $true -Actual $checkoutResult.Body.created -Message "created"
    Assert-Equal -Expected $true -Actual $checkoutResult.Body.checkoutEnabled -Message "checkoutEnabled"
    Assert-Equal -Expected "paddle" -Actual $checkoutResult.Body.provider -Message "provider"
    Assert-Equal -Expected "premium" -Actual $checkoutResult.Body.planId -Message "planId"
    Assert-NotEmpty -Value $checkoutResult.Body.checkoutUrl -Message "checkoutUrl"
    Assert-PaddleHostedCheckoutUrl -Value $checkoutResult.Body.checkoutUrl -Message "checkoutUrl must be Paddle hosted and must not be a broken /pay placeholder"
    Assert-Empty -Value $checkoutResult.Body.errorCode -Message "errorCode"
    Assert-Equal -Expected "Checkout session created." -Actual $checkoutResult.Body.message -Message "message"
    Assert-NotEmpty -Value $checkoutResult.Body.checkedAtUtc -Message "checkedAtUtc"

    Write-Pass "Paddle sandbox checkout transaction was created"
    Write-Host "checkoutUrl: $($checkoutResult.Body.checkoutUrl)" -ForegroundColor Green
    Write-Host "Payment completion, webhooks, and entitlement activation are intentionally not tested by this script." -ForegroundColor Yellow
}
catch {
    Fail $_.Exception.Message
}
