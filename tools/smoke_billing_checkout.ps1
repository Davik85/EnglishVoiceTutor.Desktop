param(
    [string]$BaseUrl = "http://localhost:5000",
    [string]$Email = "billing-smoke@example.com",
    [string]$Password = "TestPassword123!"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$JsonContentType = "application/json"
$MethodPost = "POST"

$StatusOk = 200
$StatusBadRequest = 400
$StatusUnauthorized = 401

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

function Assert-ExpectedHttpStatus {
    param(
        [string]$Method,
        [string]$Url,
        [hashtable]$Headers,
        $Body,
        [int]$ExpectedStatus
    )

    try {
        $result = Invoke-Json -Method $Method -Url $Url -Headers $Headers -Body $Body
        if ($result.StatusCode -ne $ExpectedStatus) {
            throw "Expected status code $ExpectedStatus but got $($result.StatusCode) for $Method $Url"
        }

        return $result
    }
    catch {
        $httpStatus = $null

        if ($_.Exception.Response -and $_.Exception.Response.StatusCode) {
            $httpStatus = [int]$_.Exception.Response.StatusCode.value__
        }

        if ($null -eq $httpStatus) {
            throw
        }

        if ($httpStatus -ne $ExpectedStatus) {
            throw "Expected status code $ExpectedStatus but got $httpStatus for $Method $Url"
        }

        return [pscustomobject]@{
            StatusCode = $httpStatus
            Body = $null
        }
    }
}

try {
    Write-Host "Billing checkout smoke test" -ForegroundColor Yellow
    Write-Host "BaseUrl: $BaseUrl"
    Write-Host "Test email: $Email"

    $registerUrl = "$BaseUrl$AuthRegisterPath"
    $loginUrl = "$BaseUrl$AuthLoginPath"
    $checkoutUrl = "$BaseUrl$CheckoutSessionPath"

    $authBody = @{ email = $Email; password = $Password }

    Write-Step "Register or login billing smoke user"
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

    $premiumBody = @{
        planId = "premium"
        returnUrl = "https://example.com/success"
        cancelUrl = "https://example.com/cancel"
    }

    Write-Step "Verify unauthenticated checkout returns 401"
    $unauthenticatedResult = Assert-ExpectedHttpStatus -Method $MethodPost -Url $checkoutUrl -Headers $null -Body $premiumBody -ExpectedStatus $StatusUnauthorized
    Assert-Equal -Expected $StatusUnauthorized -Actual $unauthenticatedResult.StatusCode -Message "unauthenticated checkout status"
    Write-Pass "Unauthenticated checkout returned 401"

    Write-Step "Verify missing plan id returns 400"
    $missingPlanBody = @{
        planId = ""
        returnUrl = "https://example.com/success"
        cancelUrl = "https://example.com/cancel"
    }
    $missingPlanResult = Assert-ExpectedHttpStatus -Method $MethodPost -Url $checkoutUrl -Headers $headers -Body $missingPlanBody -ExpectedStatus $StatusBadRequest
    Assert-Equal -Expected $StatusBadRequest -Actual $missingPlanResult.StatusCode -Message "missing plan id status"
    Write-Pass "Missing plan id returned 400"

    Write-Step "Verify unsupported plan returns 400"
    $unsupportedPlanBody = @{
        planId = "enterprise"
        returnUrl = "https://example.com/success"
        cancelUrl = "https://example.com/cancel"
    }
    $unsupportedPlanResult = Assert-ExpectedHttpStatus -Method $MethodPost -Url $checkoutUrl -Headers $headers -Body $unsupportedPlanBody -ExpectedStatus $StatusBadRequest
    Assert-Equal -Expected $StatusBadRequest -Actual $unsupportedPlanResult.StatusCode -Message "unsupported plan status"
    Write-Pass "Unsupported plan returned 400"

    Write-Step "Verify premium checkout returns disabled/provider-not-configured response"
    $premiumResult = Assert-ExpectedHttpStatus -Method $MethodPost -Url $checkoutUrl -Headers $headers -Body $premiumBody -ExpectedStatus $StatusOk

    Assert-Equal -Expected $false -Actual $premiumResult.Body.created -Message "created"
    Assert-Equal -Expected $false -Actual $premiumResult.Body.checkoutEnabled -Message "checkoutEnabled"
    Assert-Equal -Expected "none" -Actual $premiumResult.Body.provider -Message "provider"
    Assert-Equal -Expected "premium" -Actual $premiumResult.Body.planId -Message "planId"
    Assert-Empty -Value $premiumResult.Body.checkoutUrl -Message "checkoutUrl must be empty or null"
    Assert-Equal -Expected "billing_provider_not_configured" -Actual $premiumResult.Body.errorCode -Message "errorCode"
    Assert-Equal -Expected "Billing checkout is not configured yet." -Actual $premiumResult.Body.message -Message "message"
    Assert-NotEmpty -Value $premiumResult.Body.checkedAtUtc -Message "checkedAtUtc must be present"

    Write-Pass "Premium checkout disabled response verified"
    Write-Pass "Billing checkout smoke test passed."
    exit 0
}
catch {
    Fail $_.Exception.Message
}
