param(
    [string]$BaseUrl = "http://localhost:5000",
    [switch]$AllowProductionUrl,
    [switch]$ConfirmThrottleTest,
    [int]$Attempts = 35,
    [switch]$CheckTranslation,
    [switch]$CheckTts,
    [switch]$CheckRealtimeVoice,
    [string]$BearerToken
)

$ErrorActionPreference = "Stop"

$uri = [Uri]$BaseUrl
$productionHostPattern = "languagevoicetutor.com|englishvoicetutor.com"
if (-not $AllowProductionUrl -and $uri.Host -match $productionHostPattern) {
    throw "Refusing production-looking URL '$BaseUrl'. Re-run with -AllowProductionUrl only for approved production-safe checks. Add -ConfirmThrottleTest only during an approved throttle test window."
}

if ($ConfirmThrottleTest -and -not ($uri.IsLoopback -or $AllowProductionUrl)) {
    throw "Refusing intentional throttling against a non-local URL without -AllowProductionUrl. Prefer localhost with low RateLimiting limits."
}

if (-not ($CheckTranslation -or $CheckTts -or $CheckRealtimeVoice)) {
    Write-Host "No optional checks selected. Use -CheckTranslation, -CheckTts, or -CheckRealtimeVoice. This script never prints tokens, cookies, raw response bodies, transcripts, audio, or provider payloads."
    return
}

if (-not $ConfirmThrottleTest) {
    Write-Host "Production-safe mode: making one request per selected check only. Add -ConfirmThrottleTest for intentional local throttling."
    $Attempts = 1
}

function Invoke-SafePostJson {
    param([string]$Path, [hashtable]$Body)
    $headers = @{}
    if (-not [string]::IsNullOrWhiteSpace($BearerToken)) {
        $headers["Authorization"] = "Bearer $BearerToken"
    }

    Invoke-WebRequest -Method Post -Uri "$BaseUrl$Path" -Headers $headers -ContentType "application/json" -Body ($Body | ConvertTo-Json -Depth 8) -SkipHttpErrorCheck
}

function Write-AttemptStatus {
    param([string]$Name, [int]$Attempt, $Response)
    Write-Host "$Name attempt $Attempt status=$($Response.StatusCode) retryAfter=$($Response.Headers['Retry-After'])"
}

if ($CheckTranslation) {
    Write-Host "Checking translation rate limiting. Request/response bodies and translation text are not printed."
    $throttled = $false
    for ($i = 1; $i -le $Attempts; $i++) {
        $response = Invoke-SafePostJson -Path "/api/translate" -Body @{ text = "hello"; sourceLanguage = "English"; targetLanguage = "Spanish" }
        Write-AttemptStatus -Name "translation" -Attempt $i -Response $response
        if ($response.StatusCode -eq 429) { $throttled = $true; break }
    }
    if ($ConfirmThrottleTest -and -not $throttled) { Write-Warning "Translation did not return 429 within $Attempts attempts. Confirm RateLimiting:Enabled=true and low local limits." }
}

if ($CheckTts) {
    Write-Host "Checking TTS rate limiting. Request/response bodies, TTS input text, and audio are not printed."
    $throttled = $false
    for ($i = 1; $i -le $Attempts; $i++) {
        $response = Invoke-SafePostJson -Path "/api/audio/speech" -Body @{ text = "hello"; purpose = "smoke" }
        Write-AttemptStatus -Name "tts" -Attempt $i -Response $response
        if ($response.StatusCode -eq 429) { $throttled = $true; break }
    }
    if ($ConfirmThrottleTest -and -not $throttled) { Write-Warning "TTS did not return 429 within $Attempts attempts. Confirm RateLimiting:Enabled=true and low local limits." }
}

if ($CheckRealtimeVoice) {
    Write-Host "Realtime voice WebSocket start-rate checks are intentionally manual with this script. Use a local WebSocket client and verify status/retry headers; do not print payloads."
}
