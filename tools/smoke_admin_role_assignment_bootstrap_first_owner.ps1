param(
    [string]$BaseUrl = "http://localhost:5000",
    [string]$AdminEmail,
    [string]$AdminPassword,
    [string]$Reason = "Manual local first-owner bootstrap smoke validation.",
    [string]$SafeMetadataJson = '{"source":"manual_local_smoke","operation":"first_owner_bootstrap_validation"}',
    [switch]$ConfirmCreateFirstOwner,
    [switch]$AllowProductionUrl
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$JsonContentType = "application/json"
$MethodGet = "GET"
$MethodPost = "POST"
$StatusOk = 200
$StatusConflict = 409

$AuthLoginPath = "/api/auth/login"
$ActorPath = "/api/admin/role-assignments/actor"
$BootstrapFirstOwnerPath = "/api/admin/role-assignments/bootstrap-first-owner"
$DiagnosticsPath = "/api/admin/role-assignments/diagnostics"
$HealthPath = "/health"
$ProductionApiHost = "api.languagevoicetutor.com"

if (-not $ConfirmCreateFirstOwner) {
    throw "Refusing to run first-owner bootstrap smoke without -ConfirmCreateFirstOwner. No HTTP calls were made."
}

if ([string]::IsNullOrWhiteSpace($BaseUrl)) {
    throw "BaseUrl is required. Use a known safe local or controlled test environment."
}

$baseUri = [Uri]$BaseUrl
$isLocalHost = $baseUri.Host -in @("localhost", "127.0.0.1", "::1")
$isProductionHost = $baseUri.Host -ieq $ProductionApiHost -or $baseUri.Host -imatch "(^|\.)languagevoicetutor\.com$"
if ($isProductionHost -and -not $AllowProductionUrl) {
    throw "Refusing to run against production-looking URL '$BaseUrl' without -AllowProductionUrl. Do not run this casually against production."
}
if (-not $isLocalHost -and -not $AllowProductionUrl) {
    throw "Refusing to run against non-local URL '$BaseUrl' without -AllowProductionUrl. Use only known safe environments."
}

if ([string]::IsNullOrWhiteSpace($AdminEmail) -or [string]::IsNullOrWhiteSpace($AdminPassword)) {
    throw "AdminEmail and AdminPassword parameters are required for the existing local admin smoke-test login pattern. No credentials are embedded in this script."
}


function Get-BackendPrerequisiteMessage {
    param([string]$TargetBaseUrl)

    return @"
Backend reachability preflight failed for '$TargetBaseUrl'.

This first-owner bootstrap smoke is a special manual backend operation. The backend must already be running before this script logs in or calls the mutating bootstrap endpoint.

For a local backend, configure connection string 'DefaultConnection' outside committed repository files before starting the backend, for example via user secrets, appsettings.Development.json, or environment variables according to the project convention.

The normal desktop tester flow does not require running a local backend. If the local backend/database is intentionally not configured, do not run this bootstrap smoke.

No secrets or connection strings were printed by this script.
"@
}

function Test-BackendReachability {
    param([string]$TargetBaseUrl)

    $healthUrl = "$TargetBaseUrl$HealthPath"

    try {
        Invoke-WebRequest -Method $MethodGet -Uri $healthUrl -UseBasicParsing -TimeoutSec 10 | Out-Null
        return
    }
    catch {
        throw (Get-BackendPrerequisiteMessage -TargetBaseUrl $TargetBaseUrl)
    }
}

function Write-Step {
    param([string]$Message)
    Write-Host "[STEP] $Message" -ForegroundColor Cyan
}

function Write-SafeResult {
    param(
        [string]$Label,
        $Body
    )

    if ($null -eq $Body) {
        Write-Host "[RESULT] ${Label}: no response body"
        return
    }

    $safe = [ordered]@{}
    foreach ($name in @(
        "success",
        "errorCode",
        "message",
        "isActorMappingFound",
        "roleIds",
        "totalAdminUsers",
        "activeAdminUsers",
        "disabledAdminUsers",
        "totalRoleAssignments",
        "activeRoleAssignments",
        "revokedRoleAssignments",
        "totalRoleAssignmentEvents",
        "rolesInUse",
        "generatedAtUtc",
        "occurredAtUtc"
    )) {
        if ($Body.PSObject.Properties.Name -contains $name) {
            $safe[$name] = $Body.$name
        }
    }

    Write-Host "[RESULT] ${Label}:"
    $safe | ConvertTo-Json -Depth 5 | Write-Host
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
    }

    if ($Headers) {
        $invokeParams.Headers = $Headers
    }

    if ($null -ne $Body) {
        $invokeParams.Body = ($Body | ConvertTo-Json -Depth 5)
        $invokeParams.ContentType = $JsonContentType
    }

    return Invoke-RestMethod @invokeParams
}

function Invoke-ExpectStatusCode {
    param(
        [string]$Method,
        [string]$Url,
        [hashtable]$Headers,
        $Body,
        [int[]]$ExpectedStatusCodes
    )

    try {
        $response = Invoke-Json -Method $Method -Url $Url -Headers $Headers -Body $Body
        $actualStatusCode = $StatusOk

        if ($ExpectedStatusCodes -notcontains $actualStatusCode) {
            throw "Expected status code $($ExpectedStatusCodes -join ', ') but got $actualStatusCode for $Method $Url"
        }

        return [pscustomobject]@{
            StatusCode = $actualStatusCode
            Body = $response
        }
    }
    catch {
        if ($_.Exception.Response -and $_.Exception.Response.StatusCode) {
            $actualStatusCode = [int]$_.Exception.Response.StatusCode.value__

            if ($ExpectedStatusCodes -contains $actualStatusCode) {
                return [pscustomobject]@{
                    StatusCode = $actualStatusCode
                    Body = $null
                }
            }

            throw "Expected status code $($ExpectedStatusCodes -join ', ') but got $actualStatusCode for $Method $Url"
        }

        throw
    }
}

Write-Host "WARNING: This manual smoke may create the first persistent AdminUser/SuperAdmin-equivalent mapping in the target database." -ForegroundColor Yellow
Write-Host "WARNING: Use only against a known safe environment. Persistent roles are not globally active for authorization yet." -ForegroundColor Yellow
Write-Host "Target BaseUrl: $BaseUrl"

Write-Step "Preflight backend reachability without creating data"
Test-BackendReachability -TargetBaseUrl $BaseUrl

Write-Step "Login using the existing local admin smoke-test pattern"
$login = Invoke-ExpectStatusCode -Method $MethodPost -Url "$BaseUrl$AuthLoginPath" -Headers $null -Body @{ email = $AdminEmail; password = $AdminPassword } -ExpectedStatusCodes @($StatusOk)
if ($null -eq $login.Body -or -not ($login.Body.PSObject.Properties.Name -contains "accessToken") -or [string]::IsNullOrWhiteSpace($login.Body.accessToken)) {
    throw "Login response did not include a usable accessToken."
}
$headers = @{ Authorization = "Bearer $($login.Body.accessToken)" }

Write-Step "GET actor mapping before bootstrap"
$actorBefore = Invoke-ExpectStatusCode -Method $MethodGet -Url "$BaseUrl$ActorPath" -Headers $headers -Body $null -ExpectedStatusCodes @($StatusOk)
Write-SafeResult -Label "actor before bootstrap" -Body $actorBefore.Body

Write-Step "POST first-owner bootstrap with server-side authenticated identity"
$bootstrapBody = @{
    reason = $Reason
    safeMetadataJson = $SafeMetadataJson
}
$bootstrap = Invoke-ExpectStatusCode -Method $MethodPost -Url "$BaseUrl$BootstrapFirstOwnerPath" -Headers $headers -Body $bootstrapBody -ExpectedStatusCodes @($StatusOk, $StatusConflict)
Write-SafeResult -Label "bootstrap first owner" -Body $bootstrap.Body

Write-Step "GET actor mapping after bootstrap"
$actorAfter = Invoke-ExpectStatusCode -Method $MethodGet -Url "$BaseUrl$ActorPath" -Headers $headers -Body $null -ExpectedStatusCodes @($StatusOk)
Write-SafeResult -Label "actor after bootstrap" -Body $actorAfter.Body

Write-Step "GET role assignment diagnostics after bootstrap"
$diagnostics = Invoke-ExpectStatusCode -Method $MethodGet -Url "$BaseUrl$DiagnosticsPath" -Headers $headers -Body $null -ExpectedStatusCodes @($StatusOk)
Write-SafeResult -Label "role assignment diagnostics" -Body $diagnostics.Body

Write-Host "First-owner bootstrap smoke completed. Review audit/database state with care before any rollback or remediation." -ForegroundColor Green
