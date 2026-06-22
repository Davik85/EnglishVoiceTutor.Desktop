param(
    [string]$BaseUrl = "http://localhost:5000",
    [switch]$AllowProductionUrl,
    [switch]$ConfirmThrottleTest,
    [string]$Email = "rate-limit-smoke@example.invalid",
    [string]$Password = "local-smoke-password-not-secret",
    [int]$Attempts = 12,
    [switch]$CheckLessonChatReply
)

$ErrorActionPreference = "Stop"

if (-not $ConfirmThrottleTest) {
    throw "Refusing to intentionally test throttling without -ConfirmThrottleTest. Use a local backend with low RateLimiting limits."
}

$uri = [Uri]$BaseUrl
$productionHostPattern = "languagevoicetutor.com|englishvoicetutor.com"
if (-not $AllowProductionUrl -and $uri.Host -match $productionHostPattern) {
    throw "Refusing production-looking URL '$BaseUrl'. Re-run with -AllowProductionUrl only if you have an approved production throttle test window."
}

function Invoke-SafePostJson {
    param([string]$Path, [hashtable]$Body)
    try {
        Invoke-WebRequest -Method Post -Uri "$BaseUrl$Path" -ContentType "application/json" -Body ($Body | ConvertTo-Json -Depth 8) -SkipHttpErrorCheck
    } catch {
        Write-Host "Request failed before HTTP response was available for $Path. Status only will be reported when available."
        throw
    }
}

Write-Host "Testing login throttling against $BaseUrl. Response bodies, cookies, passwords, and tokens are not printed."
$loginThrottled = $false
for ($i = 1; $i -le $Attempts; $i++) {
    $response = Invoke-SafePostJson -Path "/api/auth/login" -Body @{ email = $Email; password = $Password }
    Write-Host "login attempt $i status=$($response.StatusCode) retryAfter=$($response.Headers['Retry-After'])"
    if ($response.StatusCode -eq 429) { $loginThrottled = $true; break }
}

if (-not $loginThrottled) {
    Write-Warning "Login did not return 429 within $Attempts attempts. Confirm RateLimiting:Enabled=true and low local login limits."
}

if ($CheckLessonChatReply) {
    Write-Host "Testing unauthenticated lesson chat reply throttling only. Provider responses are not printed. Prefer a local mock/stub provider when running this check."
    $chatThrottled = $false
    for ($i = 1; $i -le $Attempts; $i++) {
        $response = Invoke-SafePostJson -Path "/api/lesson-chat/reply" -Body @{ userMessage = "hello"; messages = @() }
        Write-Host "lesson chat attempt $i status=$($response.StatusCode) retryAfter=$($response.Headers['Retry-After'])"
        if ($response.StatusCode -eq 429) { $chatThrottled = $true; break }
    }
    if (-not $chatThrottled) {
        Write-Warning "Lesson chat reply did not return 429 within $Attempts attempts. Confirm RateLimiting:Enabled=true and low local chat limits."
    }
}
