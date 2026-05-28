param(
    [string]$BaseUrl = "http://localhost:5000",
    [string]$Email = "paddle-adapter-smoke@example.com",
    [string]$Password = "TestPassword123!"
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

try {
    Write-Host "This smoke test expects the backend to be started with Billing__CheckoutEnabled=true and Billing__Provider=paddle." -ForegroundColor Yellow
    Write-Host "It must not call Paddle or create a real checkout session." -ForegroundColor Yellow
    Write-Host "Expected backend environment:" -ForegroundColor Yellow
    Write-Host "  Billing__CheckoutEnabled=true" -ForegroundColor Yellow
    Write-Host "  Billing__Provider=paddle" -ForegroundColor Yellow
    Write-Host "  PaddleBilling__CheckoutAdapterEnabled=false" -ForegroundColor Yellow
    Write-Host "  PaddleBilling__Environment=sandbox" -ForegroundColor Yellow
    Write-Host "  PaddleBilling__ApiKey=\"\"" -ForegroundColor Yellow
    Write-Host "  PaddleBilling__PremiumPriceId=\"\"" -ForegroundColor Yellow
    Write-Host ""

    Write-Host "Paddle checkout adapter smoke test" -ForegroundColor Yellow
    Write-Host "BaseUrl: $BaseUrl"
    Write-Host "Test email: $Email"

    $registerUrl = "$BaseUrl$AuthRegisterPath"
    $loginUrl = "$BaseUrl$AuthLoginPath"
    $checkoutUrl = "$BaseUrl$CheckoutSessionPath"

    $authBody = @{ email = $Email; password = $Password }

    Write-Step "Register or login Paddle adapter smoke user"
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

    $headers = @{ Authorization = "Bearer $($authResult.Body.accessToken)" }
    Write-Pass "Auth succeeded"

    Write-Step "Verify safe Paddle adapter disabled checkout response"
    $checkoutBody = @{
        planId = "premium"
        returnUrl = "https://example.com/success"
        cancelUrl = "https://example.com/cancel"
    }

    $checkoutResult = Invoke-Json -Method $MethodPost -Url $checkoutUrl -Headers $headers -Body $checkoutBody
    Assert-Equal -Expected $StatusOk -Actual $checkoutResult.StatusCode -Message "checkout response status"

    Assert-Equal -Expected $false -Actual $checkoutResult.Body.created -Message "created"
    Assert-Equal -Expected $false -Actual $checkoutResult.Body.checkoutEnabled -Message "checkoutEnabled"
    Assert-Equal -Expected "paddle" -Actual $checkoutResult.Body.provider -Message "provider"
    Assert-Equal -Expected "premium" -Actual $checkoutResult.Body.planId -Message "planId"
    Assert-Empty -Value $checkoutResult.Body.checkoutUrl -Message "checkoutUrl must be empty or null"
    Assert-Equal -Expected "paddle_checkout_not_configured" -Actual $checkoutResult.Body.errorCode -Message "errorCode"
    Assert-Equal -Expected "Paddle checkout adapter is disabled." -Actual $checkoutResult.Body.message -Message "message"
    Assert-NotEmpty -Value $checkoutResult.Body.checkedAtUtc -Message "checkedAtUtc must be present"

    Write-Pass "Safe Paddle adapter disabled response verified"
    Write-Pass "Paddle checkout adapter smoke test passed."
    exit 0
}
catch {
    Fail $_.Exception.Message
}
