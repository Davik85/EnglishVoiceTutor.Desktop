param(
    [string]$BaseUrl = "http://localhost:5000",
    [string]$AdminEmail,
    [string]$AdminPassword,
    [string]$TargetAppUserId,
    [string]$TargetAdminUserId,
    [string]$RoleId,
    [string]$Reason,
    [string]$SafeMetadataJson,
    [switch]$AllowProductionUrl,
    [switch]$ConfirmRoleManagementMutations
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$JsonContentType = "application/json"
$MethodGet = "GET"
$MethodPost = "POST"
$StatusOk = 200

$AuthLoginPath = "/api/auth/login"
$ActorPath = "/api/admin/role-assignments/actor"
$DiagnosticsPath = "/api/admin/role-assignments/diagnostics"
$ProvisionAdminUserPath = "/api/admin/role-assignments/provision-admin-user"
$AssignRolePath = "/api/admin/role-assignments/assign"
$RevokeRolePath = "/api/admin/role-assignments/revoke"
$DisableAdminPath = "/api/admin/role-assignments/disable-admin"
$EnableAdminPath = "/api/admin/role-assignments/enable-admin"
$ProductionApiHost = "api.languagevoicetutor.com"

function Write-Step {
    param([string]$Message)
    Write-Host "[STEP] $Message" -ForegroundColor Cyan
}

function Write-Skip {
    param([string]$Message)
    Write-Host "[SKIP] $Message" -ForegroundColor Yellow
}

function Test-SafeBaseUrl {
    param([string]$TargetBaseUrl)

    if ([string]::IsNullOrWhiteSpace($TargetBaseUrl)) {
        throw "BaseUrl is required. Use a known safe local or controlled test environment."
    }

    $baseUri = [Uri]$TargetBaseUrl
    $isLocalHost = $baseUri.Host -in @("localhost", "127.0.0.1", "::1")
    $isProductionHost = $baseUri.Host -ieq $ProductionApiHost -or $baseUri.Host -imatch "(^|\.)languagevoicetutor\.com$"

    if ($isProductionHost -and -not $AllowProductionUrl) {
        throw "Refusing to run against production-looking URL without -AllowProductionUrl. Do not run this casually against production."
    }

    if (-not $isLocalHost -and -not $AllowProductionUrl) {
        throw "Refusing to run against non-local URL without -AllowProductionUrl. Use only known safe environments."
    }
}

function Assert-LoginInputs {
    if ([string]::IsNullOrWhiteSpace($AdminEmail) -or [string]::IsNullOrWhiteSpace($AdminPassword)) {
        throw "AdminEmail and AdminPassword parameters are required for the existing admin smoke-test login pattern. No credentials are embedded in this script."
    }
}

function Assert-MutationReason {
    if ([string]::IsNullOrWhiteSpace($Reason)) {
        throw "Mutating role-management calls require a non-empty Reason."
    }
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

function Invoke-ExpectOk {
    param(
        [string]$Method,
        [string]$Url,
        [hashtable]$Headers,
        $Body
    )

    try {
        return Invoke-Json -Method $Method -Url $Url -Headers $Headers -Body $Body
    }
    catch {
        if ($_.Exception.Response -and $_.Exception.Response.StatusCode) {
            $actualStatusCode = [int]$_.Exception.Response.StatusCode.value__
            throw "Expected status code $StatusOk but got $actualStatusCode for $Method role-management smoke call."
        }

        throw "HTTP role-management smoke call failed. Review backend logs for details."
    }
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
        "occurredAtUtc",
        "targetAdminUserId",
        "adminUserId",
        "roleId",
        "auditEventId"
    )) {
        if ($Body.PSObject.Properties.Name -contains $name) {
            $safe[$name] = $Body.$name
        }
    }

    Write-Host "[RESULT] ${Label}:"
    $safe | ConvertTo-Json -Depth 5 | Write-Host
}

function New-MutationBody {
    param([hashtable]$Fields)

    $body = @{
        reason = $Reason
    }

    if (-not [string]::IsNullOrWhiteSpace($SafeMetadataJson)) {
        $body.safeMetadataJson = $SafeMetadataJson
    }

    foreach ($key in $Fields.Keys) {
        $body[$key] = $Fields[$key]
    }

    return $body
}

Test-SafeBaseUrl -TargetBaseUrl $BaseUrl
Assert-LoginInputs

Write-Host "Admin role-management smoke target: $BaseUrl"
if (-not $ConfirmRoleManagementMutations) {
    Write-Host "Running read-only mode. Add -ConfirmRoleManagementMutations plus required target parameters to run guarded mutations." -ForegroundColor Yellow
}
else {
    Write-Host "Running mutation-enabled mode for a controlled test environment. Role changes are audited." -ForegroundColor Yellow
    Assert-MutationReason
}

Write-Step "Login using the existing admin smoke-test pattern"
$login = Invoke-ExpectOk -Method $MethodPost -Url "$BaseUrl$AuthLoginPath" -Headers $null -Body @{ email = $AdminEmail; password = $AdminPassword }
if ($null -eq $login -or -not ($login.PSObject.Properties.Name -contains "accessToken") -or [string]::IsNullOrWhiteSpace($login.accessToken)) {
    throw "Login response did not include a usable access token."
}
$headers = @{ Authorization = "Bearer $($login.accessToken)" }

Write-Step "GET current actor mapping"
$actor = Invoke-ExpectOk -Method $MethodGet -Url "$BaseUrl$ActorPath" -Headers $headers -Body $null
Write-SafeResult -Label "actor mapping" -Body $actor

Write-Step "GET role-assignment diagnostics"
$diagnostics = Invoke-ExpectOk -Method $MethodGet -Url "$BaseUrl$DiagnosticsPath" -Headers $headers -Body $null
Write-SafeResult -Label "role assignment diagnostics" -Body $diagnostics

if (-not $ConfirmRoleManagementMutations) {
    Write-Host "Read-only role-management smoke completed. No mutating role-management calls were made." -ForegroundColor Green
    return
}

if ([string]::IsNullOrWhiteSpace($TargetAppUserId)) {
    Write-Skip "Provision AdminUser skipped because TargetAppUserId was not provided."
}
else {
    Write-Step "POST provision AdminUser for a safe target app user"
    $provisionBody = New-MutationBody -Fields @{ targetAppUserId = $TargetAppUserId }
    $provision = Invoke-ExpectOk -Method $MethodPost -Url "$BaseUrl$ProvisionAdminUserPath" -Headers $headers -Body $provisionBody
    Write-SafeResult -Label "provision AdminUser" -Body $provision
}

if ([string]::IsNullOrWhiteSpace($TargetAdminUserId) -or [string]::IsNullOrWhiteSpace($RoleId)) {
    Write-Skip "Assign/revoke role skipped because TargetAdminUserId and RoleId are both required."
}
else {
    Write-Step "POST assign role"
    $assignBody = New-MutationBody -Fields @{ targetAdminUserId = $TargetAdminUserId; roleId = $RoleId }
    $assign = Invoke-ExpectOk -Method $MethodPost -Url "$BaseUrl$AssignRolePath" -Headers $headers -Body $assignBody
    Write-SafeResult -Label "assign role" -Body $assign

    Write-Step "POST revoke role"
    $revokeBody = New-MutationBody -Fields @{ targetAdminUserId = $TargetAdminUserId; roleId = $RoleId }
    $revoke = Invoke-ExpectOk -Method $MethodPost -Url "$BaseUrl$RevokeRolePath" -Headers $headers -Body $revokeBody
    Write-SafeResult -Label "revoke role" -Body $revoke
}

if ([string]::IsNullOrWhiteSpace($TargetAdminUserId)) {
    Write-Skip "Disable/enable AdminUser skipped because TargetAdminUserId was not provided."
}
else {
    Write-Step "POST disable AdminUser"
    $disableBody = New-MutationBody -Fields @{ targetAdminUserId = $TargetAdminUserId }
    $disable = Invoke-ExpectOk -Method $MethodPost -Url "$BaseUrl$DisableAdminPath" -Headers $headers -Body $disableBody
    Write-SafeResult -Label "disable AdminUser" -Body $disable

    Write-Step "POST enable AdminUser"
    $enableBody = New-MutationBody -Fields @{ targetAdminUserId = $TargetAdminUserId }
    $enable = Invoke-ExpectOk -Method $MethodPost -Url "$BaseUrl$EnableAdminPath" -Headers $headers -Body $enableBody
    Write-SafeResult -Label "enable AdminUser" -Body $enable
}

Write-Host "Mutation-enabled role-management smoke completed. Review audit/database state with care." -ForegroundColor Green
