param(
    [string]$BackendBaseUrl = "https://api.languagevoicetutor.com",
    [string]$Email = "",
    [string]$Password = ""
)

$ErrorActionPreference = "Stop"
$base = $BackendBaseUrl.TrimEnd('/')

function Invoke-SmokeRequest {
    param(
        [string]$Method,
        [string]$Path,
        [object]$Body = $null,
        [string]$BearerToken = ""
    )

    $url = "$base$Path"
    $headers = @{}
    if ($BearerToken) {
        $headers["Authorization"] = "Bearer $BearerToken"
    }

    Write-Host "$Method $url"
    if ($null -eq $Body) {
        return Invoke-RestMethod -Method $Method -Uri $url -Headers $headers
    }

    return Invoke-RestMethod -Method $Method -Uri $url -Headers $headers -ContentType "application/json" -Body ($Body | ConvertTo-Json -Depth 8)
}

Invoke-SmokeRequest -Method GET -Path "/health" | Out-Host
Invoke-SmokeRequest -Method GET -Path "/api/health/database" | Out-Host

if ([string]::IsNullOrWhiteSpace($Email) -or [string]::IsNullOrWhiteSpace($Password)) {
    Write-Host "Skipping POST /api/auth/register and GET /api/auth/me because -Email and -Password were not provided."
    exit 0
}

$displayName = "Desktop Smoke Tester"
$registerBody = @{ email = $Email; password = $Password; displayName = $displayName }
$auth = Invoke-SmokeRequest -Method POST -Path "/api/auth/register" -Body $registerBody
$auth | Select-Object tokenType, expiresAtUtc, user | Out-Host

if (-not $auth.accessToken) {
    throw "Register response did not include accessToken; cannot check /api/auth/me."
}

Invoke-SmokeRequest -Method GET -Path "/api/auth/me" -BearerToken $auth.accessToken | Out-Host
