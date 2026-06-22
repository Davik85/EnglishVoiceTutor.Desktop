param(
    [string]$BaseUrl = "http://localhost:5000",
    [switch]$AllowProductionUrl,
    [switch]$ConfirmThrottleTest,
    [switch]$RunThrottleChecks,
    [string]$BearerToken = ""
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Test-ProductionLookingUrl {
    param([string]$Url)
    return $Url -match "languagevoicetutor\.com" -or $Url -match "^https://api\."
}

function Write-SafeResult {
    param([string]$Name, [int]$StatusCode)
    Write-Host ("{0}: HTTP {1}" -f $Name, $StatusCode)
}

$base = $BaseUrl.TrimEnd('/')
$isProductionLooking = Test-ProductionLookingUrl -Url $base

if ($isProductionLooking -and -not $AllowProductionUrl) {
    throw "Production-looking URL refused. Re-run with -AllowProductionUrl for safe normal checks only."
}

if ($RunThrottleChecks) {
    if ($isProductionLooking -and (-not $AllowProductionUrl -or -not $ConfirmThrottleTest)) {
        throw "Refusing production-looking throttling checks without both -AllowProductionUrl and -ConfirmThrottleTest."
    }

    if (-not $ConfirmThrottleTest) {
        throw "Throttle checks require -ConfirmThrottleTest and should only target local/test environments."
    }
}

Write-Host "Billing rate-limiting slice smoke checks against $base"
Write-Host "Safe mode: response bodies, tokens, signatures, cookies, raw provider payloads, and checkout URLs are not printed."

try {
    $health = Invoke-WebRequest -Uri "$base/health" -Method GET -SkipHttpErrorCheck
    Write-SafeResult -Name "health" -StatusCode ([int]$health.StatusCode)
} catch {
    Write-Warning "health check failed: $($_.Exception.Message)"
}

try {
    $launch = Invoke-WebRequest -Uri "$base/checkout/paddle" -Method GET -SkipHttpErrorCheck
    Write-SafeResult -Name "paddle checkout launch normal request" -StatusCode ([int]$launch.StatusCode)
} catch {
    Write-Warning "paddle checkout launch normal request failed: $($_.Exception.Message)"
}

if ($RunThrottleChecks) {
    Write-Host "Running local/test throttle validation for Paddle checkout launch only. This intentionally sends repeated safe GET requests without transaction ids."
    for ($i = 1; $i -le 35; $i++) {
        $response = Invoke-WebRequest -Uri "$base/checkout/paddle" -Method GET -SkipHttpErrorCheck
        if ([int]$response.StatusCode -eq 429) {
            Write-SafeResult -Name "paddle checkout launch throttled after request $i" -StatusCode 429
            break
        }
    }
}

if ($BearerToken) {
    Write-Host "Bearer token supplied; authenticated endpoint status-only checks can be added locally without printing token or response bodies."
}
