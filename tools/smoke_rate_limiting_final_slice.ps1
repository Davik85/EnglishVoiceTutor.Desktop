param(
    [Parameter(Mandatory = $false)]
    [string]$BaseUrl = "http://localhost:5000",

    [Parameter(Mandatory = $false)]
    [string]$BearerToken,

    [Parameter(Mandatory = $false)]
    [switch]$RunLocalThrottleChecks,

    [Parameter(Mandatory = $false)]
    [switch]$AllowProductionUrl,

    [Parameter(Mandatory = $false)]
    [switch]$ConfirmIntentionalThrottleTest
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Write-Step([string]$Message) {
    Write-Host "[rate-limit-final-slice] $Message"
}

function Test-ProductionLookingUrl([string]$Url) {
    return $Url -notmatch "localhost|127\.0\.0\.1|\[::1\]|\.local|dev|test|staging"
}

function Invoke-SafeRequest {
    param(
        [Parameter(Mandatory = $true)][string]$Method,
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $false)][hashtable]$Headers = @{}
    )

    $uri = "$($BaseUrl.TrimEnd('/'))$Path"
    try {
        $response = Invoke-WebRequest -Method $Method -Uri $uri -Headers $Headers -TimeoutSec 15 -SkipHttpErrorCheck
        Write-Step "$Method $Path -> HTTP $($response.StatusCode)"
        return $response.StatusCode
    }
    catch {
        Write-Step "$Method $Path -> request failed without printing response bodies: $($_.Exception.Message)"
        return $null
    }
}

if ((Test-ProductionLookingUrl $BaseUrl) -and -not $AllowProductionUrl) {
    throw "BaseUrl looks production-like. Re-run with -AllowProductionUrl for production-safe status checks only. Throttling checks also require -ConfirmIntentionalThrottleTest."
}

$headers = @{}
if (-not [string]::IsNullOrWhiteSpace($BearerToken)) {
    $headers["Authorization"] = "Bearer $BearerToken"
}

Write-Step "Running production-safe final-slice status checks. No secrets or response bodies will be printed."
Invoke-SafeRequest -Method "GET" -Path "/health" | Out-Null
Invoke-SafeRequest -Method "GET" -Path "/api/health/database" | Out-Null

if (-not [string]::IsNullOrWhiteSpace($BearerToken)) {
    Invoke-SafeRequest -Method "GET" -Path "/api/auth/me" -Headers $headers | Out-Null
    Invoke-SafeRequest -Method "GET" -Path "/api/me/subscription-status" -Headers $headers | Out-Null
    Invoke-SafeRequest -Method "GET" -Path "/api/me/lesson-access" -Headers $headers | Out-Null
}
else {
    Write-Step "BearerToken was not supplied; authenticated status checks were skipped."
}

if (-not $RunLocalThrottleChecks) {
    Write-Step "Throttle checks skipped. Use -RunLocalThrottleChecks -ConfirmIntentionalThrottleTest against local/test with deliberately low limits."
    exit 0
}

if ((Test-ProductionLookingUrl $BaseUrl) -and (-not $AllowProductionUrl -or -not $ConfirmIntentionalThrottleTest)) {
    throw "Refusing intentional throttling against a production-looking URL without both -AllowProductionUrl and -ConfirmIntentionalThrottleTest."
}

if (-not $ConfirmIntentionalThrottleTest) {
    throw "Intentional local/test throttling requires -ConfirmIntentionalThrottleTest. Configure low local limits first; this script will not hammer production."
}

Write-Step "Running minimal local/test throttle validation. Configure low limits before running."
for ($i = 1; $i -le 3; $i++) {
    Invoke-SafeRequest -Method "GET" -Path "/api/auth/me" -Headers $headers | Out-Null
}
Write-Step "Completed minimal throttle validation without printing bodies. Confirm 429 RateLimitExceeded in server logs or caller output when low limits are configured."
