param(
    [string]$BaseUrl = "http://localhost:5000",
    [switch]$AllowProductionUrl,
    [switch]$ConfirmThrottleTest,
    [int]$Attempts = 15,
    [string]$BearerToken,
    [string]$AdminCookie,
    [switch]$CheckRoleManagementThrottle
)

$ErrorActionPreference = "Stop"

$uri = [Uri]$BaseUrl
$productionHostPattern = "languagevoicetutor.com|englishvoicetutor.com"
if (-not $AllowProductionUrl -and $uri.Host -match $productionHostPattern) {
    throw "Refusing production-looking URL '$BaseUrl'. Re-run with -AllowProductionUrl for approved production-safe checks. Add -ConfirmThrottleTest only during an approved throttle test window."
}

if ($ConfirmThrottleTest -and -not ($uri.IsLoopback -or ($AllowProductionUrl -and $env:EVT_CONFIRM_PRODUCTION_RATE_LIMIT_TEST -eq "I_UNDERSTAND"))) {
    throw "Refusing intentional throttling against a non-local URL unless -AllowProductionUrl is supplied and EVT_CONFIRM_PRODUCTION_RATE_LIMIT_TEST=I_UNDERSTAND. Prefer localhost with low RateLimiting:Admin limits."
}

if ([string]::IsNullOrWhiteSpace($BearerToken) -and [string]::IsNullOrWhiteSpace($AdminCookie)) {
    throw "Provide either -BearerToken or -AdminCookie. This script never prints credentials, cookies, raw bodies, CMS JSON, role-change reason text, or secrets."
}

if (-not $ConfirmThrottleTest) {
    Write-Host "Production-safe mode: making one request per Admin read check only. Add -ConfirmThrottleTest for intentional local throttling with low test limits."
    $Attempts = 1
}

function New-SafeHeaders {
    $headers = @{}
    if (-not [string]::IsNullOrWhiteSpace($BearerToken)) {
        $headers["Authorization"] = "Bearer $BearerToken"
    }
    if (-not [string]::IsNullOrWhiteSpace($AdminCookie)) {
        $headers["Cookie"] = $AdminCookie
    }
    return $headers
}

function Invoke-SafeGet {
    param([string]$Path)
    Invoke-WebRequest -Method Get -Uri "$BaseUrl$Path" -Headers (New-SafeHeaders) -SkipHttpErrorCheck
}

function Write-AttemptStatus {
    param([string]$Name, [int]$Attempt, $Response)
    Write-Host "$Name attempt $Attempt status=$($Response.StatusCode) retryAfter=$($Response.Headers['Retry-After'])"
}

$readPaths = @(
    "/api/admin/me",
    "/api/admin/capabilities",
    "/api/admin/statistics/overview"
)

foreach ($path in $readPaths) {
    Write-Host "Checking Admin read endpoint $path. Raw response bodies are not printed."
    $throttled = $false
    for ($i = 1; $i -le $Attempts; $i++) {
        $response = Invoke-SafeGet -Path $path
        Write-AttemptStatus -Name $path -Attempt $i -Response $response
        if (-not $ConfirmThrottleTest -and $response.StatusCode -ne 200) {
            Write-Warning "$path returned status $($response.StatusCode). Confirm the supplied principal has the required Admin permission."
        }
        if ($response.StatusCode -eq 429) { $throttled = $true; break }
    }
    if ($ConfirmThrottleTest -and -not $throttled) { Write-Warning "$path did not return 429 within $Attempts attempts. Confirm RateLimiting:Enabled=true and low local Admin read limits." }
}

if ($CheckRoleManagementThrottle) {
    Write-Host "Checking Admin role-management read-side throttling via /api/admin/role-assignments/actor. No role mutations are performed."
    $throttled = $false
    for ($i = 1; $i -le $Attempts; $i++) {
        $response = Invoke-SafeGet -Path "/api/admin/role-assignments/actor"
        Write-AttemptStatus -Name "role-management" -Attempt $i -Response $response
        if ($response.StatusCode -eq 429) { $throttled = $true; break }
    }
    if ($ConfirmThrottleTest -and -not $throttled) { Write-Warning "Role-management check did not return 429 within $Attempts attempts. Confirm low local Admin role-management limits if testing mutation policies separately." }
}
