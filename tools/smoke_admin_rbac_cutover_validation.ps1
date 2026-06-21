<#
Manual Admin RBAC cutover validation smoke script.

This script is intentionally opt-in only and validation-only. It does not change
backend configuration, role assignments, CMS content, billing state, Premium
state, free-lesson allowances, or deployment settings.

Expected behavior for AdminAuthorization:EnableBootstrapAdminFallbackForAdminPermissionPolicies:

Fallback enabled mode (missing setting or true):
- BootstrapAdmin users may still pass AdminPermission-protected endpoints through fallback.
- Persistent-role users may pass if their role grants the required permission.

Fallback disabled mode (false):
- AdminPermission-protected endpoints require persistent roles.
- BootstrapAdmin fallback should no longer authorize AdminPermission:* policies.
- AdminRoleManagementPermissionPolicyName endpoints are separate and are not controlled by this switch.
- BootstrapAdmin-only endpoints, such as CMS import/init, are not affected by this switch.

The script does not assert fallback-disabled behavior unless callers explicitly pass
-ExpectedFallbackEnabled $false and matching expected status codes for the account
being tested. Use placeholders for credentials and never paste secrets into logs.
#>

param(
    [string]$BaseUrl = "http://localhost:5000",
    [string]$AdminEmail,
    [string]$AdminPassword,
    [Nullable[bool]]$ExpectedFallbackEnabled = $null,
    [Nullable[bool]]$ExpectedActorMappingFound = $null,
    [int]$ExpectedAdminPermissionEndpointStatus = 200,
    [int]$ExpectedRoleManagementEndpointStatus = 200,
    [switch]$AllowProductionUrl,
    [switch]$ConfirmRbacCutoverValidation
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$JsonContentType = "application/json"
$MethodGet = "GET"
$MethodPost = "POST"
$HealthPath = "/health"
$AuthLoginPath = "/api/auth/login"
$FallbackSettingPath = "AdminAuthorization:EnableBootstrapAdminFallbackForAdminPermissionPolicies"
$ProductionHostPattern = "(^|\.)(languagevoicetutor\.com|englishvoicetutor\.com)$"

$AdminPermissionReadEndpoints = @(
    @{ Label = "admin self"; Path = "/api/admin/me" },
    @{ Label = "admin capabilities"; Path = "/api/admin/capabilities" },
    @{ Label = "product statistics overview"; Path = "/api/admin/statistics/overview" },
    @{ Label = "CMS runtime status"; Path = "/api/admin/dev/cms/runtime-status" }
)

$RoleManagementReadEndpoints = @(
    @{ Label = "role assignment actor mapping"; Path = "/api/admin/role-assignments/actor" },
    @{ Label = "role assignment diagnostics"; Path = "/api/admin/role-assignments/diagnostics" },
    @{ Label = "RBAC cutover status"; Path = "/api/admin/rbac/cutover-status" }
)

$RbacCutoverStatusPath = "/api/admin/rbac/cutover-status"

function Write-Step {
    param([string]$Message)
    Write-Host "[STEP] $Message" -ForegroundColor Cyan
}

function Write-SafeSummary {
    param(
        [string]$Label,
        [int]$StatusCode,
        [bool]$Passed
    )

    $result = if ($Passed) { "PASS" } else { "FAIL" }
    Write-Host ("[RESULT] {0}: status={1}; result={2}" -f $Label, $StatusCode, $result)
}

function Test-SafeBaseUrl {
    param([string]$TargetBaseUrl)

    if ([string]::IsNullOrWhiteSpace($TargetBaseUrl)) {
        throw "BaseUrl is required. Use a known local or owner-approved controlled validation environment."
    }

    $baseUri = [Uri]$TargetBaseUrl
    $isLocalHost = $baseUri.Host -in @("localhost", "127.0.0.1", "::1")
    $isPrivateHost = $baseUri.Host -match "^(10\.|192\.168\.|172\.(1[6-9]|2[0-9]|3[0-1])\.)"
    $isProductionLookingHost = $baseUri.Host -imatch $ProductionHostPattern -or $baseUri.Scheme -eq "https"

    if ((-not $isLocalHost -and -not $isPrivateHost) -and -not $AllowProductionUrl) {
        throw "Refusing to run against a non-local URL without -AllowProductionUrl. This validation is cutover-sensitive."
    }

    if ($isProductionLookingHost -and -not $AllowProductionUrl) {
        throw "Refusing to run against a production-looking URL without -AllowProductionUrl. Use owner-approved controlled validation only."
    }
}

function Assert-Confirmed {
    if (-not $ConfirmRbacCutoverValidation) {
        throw "Admin RBAC cutover validation requires -ConfirmRbacCutoverValidation. The script stopped before making Admin requests. Run only in an owner-approved controlled environment."
    }
}

function Assert-LoginInputs {
    if ([string]::IsNullOrWhiteSpace($AdminEmail) -or [string]::IsNullOrWhiteSpace($AdminPassword)) {
        throw "AdminEmail and AdminPassword parameters are required. No credentials are embedded in this script."
    }
}

function Invoke-StatusOnlyRequest {
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
        $invokeParams.Body = ($Body | ConvertTo-Json -Depth 5)
        $invokeParams.ContentType = $JsonContentType
    }

    try {
        $response = Invoke-WebRequest @invokeParams
        return [int]$response.StatusCode
    }
    catch {
        if ($_.Exception.Response -and $_.Exception.Response.StatusCode) {
            return [int]$_.Exception.Response.StatusCode.value__
        }

        throw "HTTP request failed before a status code was returned. Check backend reachability and service logs."
    }
}

function Invoke-RbacCutoverStatus {
    param(
        [string]$Url,
        [hashtable]$Headers
    )

    try {
        return Invoke-RestMethod -Method $MethodGet -Uri $Url -Headers $Headers
    }
    catch {
        if ($_.Exception.Response -and $_.Exception.Response.StatusCode) {
            $statusCode = [int]$_.Exception.Response.StatusCode.value__
            throw "RBAC cutover status request failed with HTTP status $statusCode. Response body was not printed."
        }

        throw "RBAC cutover status request failed before a status code was returned. Response body was not printed."
    }
}

function Get-RequiredPropertyValue {
    param(
        $Source,
        [string]$PropertyName
    )

    if ($null -eq $Source -or -not ($Source.PSObject.Properties.Name -contains $PropertyName)) {
        throw "RBAC cutover status response did not include required property '$PropertyName'."
    }

    return $Source.$PropertyName
}

function Invoke-Login {
    param([string]$Url)

    try {
        $login = Invoke-RestMethod -Method $MethodPost -Uri $Url -ContentType $JsonContentType -Body (@{ email = $AdminEmail; password = $AdminPassword } | ConvertTo-Json -Depth 3)
    }
    catch {
        if ($_.Exception.Response -and $_.Exception.Response.StatusCode) {
            $statusCode = [int]$_.Exception.Response.StatusCode.value__
            throw "Login failed with HTTP status $statusCode. Credentials were not printed."
        }

        throw "Login failed before a status code was returned. Credentials were not printed."
    }

    if ($null -eq $login -or -not ($login.PSObject.Properties.Name -contains "accessToken") -or [string]::IsNullOrWhiteSpace($login.accessToken)) {
        throw "Login response did not include a usable access credential. Response body was not printed."
    }

    return @{ Authorization = "Bearer $($login.accessToken)" }
}

Test-SafeBaseUrl -TargetBaseUrl $BaseUrl

Write-Step "Check backend health at $HealthPath"
$healthStatus = Invoke-StatusOnlyRequest -Method $MethodGet -Url "$BaseUrl$HealthPath" -Headers $null -Body $null
Write-SafeSummary -Label "backend health" -StatusCode $healthStatus -Passed ($healthStatus -ge 200 -and $healthStatus -lt 500)
if ($healthStatus -ge 500) {
    throw "Backend health endpoint returned $healthStatus. Stop and fix backend reachability before Admin validation."
}

Assert-Confirmed
Assert-LoginInputs

Write-Host "Admin RBAC cutover validation target: $BaseUrl"
Write-Host "Fallback setting under validation: $FallbackSettingPath"
if ($ExpectedFallbackEnabled.HasValue) {
    Write-Host ("Expected fallback enabled: {0}" -f $ExpectedFallbackEnabled.Value)
}
else {
    Write-Host "Expected fallback enabled: not asserted by this run"
}

Write-Step "Login using the existing admin smoke-test auth endpoint"
$headers = Invoke-Login -Url "$BaseUrl$AuthLoginPath"

$failures = 0

foreach ($endpoint in $AdminPermissionReadEndpoints) {
    $status = Invoke-StatusOnlyRequest -Method $MethodGet -Url "$BaseUrl$($endpoint.Path)" -Headers $headers -Body $null
    $passed = $status -eq $ExpectedAdminPermissionEndpointStatus
    Write-SafeSummary -Label $endpoint.Label -StatusCode $status -Passed $passed
    if (-not $passed) { $failures++ }
}

foreach ($endpoint in $RoleManagementReadEndpoints) {
    $status = Invoke-StatusOnlyRequest -Method $MethodGet -Url "$BaseUrl$($endpoint.Path)" -Headers $headers -Body $null
    $passed = $status -eq $ExpectedRoleManagementEndpointStatus
    Write-SafeSummary -Label $endpoint.Label -StatusCode $status -Passed $passed
    if (-not $passed) { $failures++ }
}

Write-Step "Read safe backend-reported RBAC cutover status"
$cutoverStatus = Invoke-RbacCutoverStatus -Url "$BaseUrl$RbacCutoverStatusPath" -Headers $headers
$effectiveFallbackEnabled = [bool](Get-RequiredPropertyValue -Source $cutoverStatus -PropertyName "bootstrapAdminFallbackForAdminPermissionPoliciesEnabled")
$defaultFallbackEnabled = [bool](Get-RequiredPropertyValue -Source $cutoverStatus -PropertyName "bootstrapAdminFallbackDefaultEnabled")
$configValuePresent = [bool](Get-RequiredPropertyValue -Source $cutoverStatus -PropertyName "bootstrapAdminFallbackConfigurationValuePresent")
$persistentRoleAuthorizationEnabled = [bool](Get-RequiredPropertyValue -Source $cutoverStatus -PropertyName "persistentRoleAuthorizationEnabled")
$generatedAtUtc = Get-RequiredPropertyValue -Source $cutoverStatus -PropertyName "generatedAtUtc"
Write-Host ("[INFO] RBAC cutover status: fallbackEnabled={0}; defaultFallbackEnabled={1}; configValuePresent={2}; persistentRoleAuthorizationEnabled={3}; generatedAtUtc={4}" -f $effectiveFallbackEnabled, $defaultFallbackEnabled, $configValuePresent, $persistentRoleAuthorizationEnabled, $generatedAtUtc)

if ($ExpectedFallbackEnabled.HasValue) {
    $passed = $effectiveFallbackEnabled -eq $ExpectedFallbackEnabled.Value
    $result = if ($passed) { "PASS" } else { "FAIL" }
    Write-Host ("[RESULT] RBAC cutover status fallback match: expected={0}; actual={1}; result={2}" -f $ExpectedFallbackEnabled.Value, $effectiveFallbackEnabled, $result)
    if (-not $passed) { $failures++ }
}

if ($ExpectedActorMappingFound.HasValue) {
    Write-Host ("[INFO] Expected actor mapping found: {0}. Verify this against backend diagnostics or Admin UI without pasting raw responses into logs." -f $ExpectedActorMappingFound.Value)
}

if ($failures -gt 0) {
    throw "Admin RBAC cutover validation completed with $failures unexpected status result(s)."
}

Write-Host "Admin RBAC cutover validation completed successfully. Only status summaries were printed."
