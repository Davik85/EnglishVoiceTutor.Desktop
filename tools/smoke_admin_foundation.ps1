param(
    [string]$BaseUrl = "http://localhost:5000",
    [string]$AdminEmail = "admin-test@example.com",
    [string]$AdminPassword = "TestPassword123!",
    [string]$NormalEmail = "normal-user@example.com",
    [string]$NormalPassword = "TestPassword123!"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$JsonContentType = "application/json"
$MethodGet = "GET"
$MethodPost = "POST"

$StatusOk = 200
$StatusCreated = 201
$StatusBadRequest = 400
$StatusUnauthorized = 401
$StatusForbidden = 403
$StatusNotFound = 404

$AuthLoginPath = "/api/auth/login"
$AdminMePath = "/api/admin/me"
$AdminUsersByEmailPath = "/api/admin/users/by-email"
$AdminCapabilitiesPath = "/api/admin/capabilities"
$AdminShellPath = "/admin/"
$AdminAuditActionsPathTemplate = "/api/admin/users/{0}/audit-actions"
$AdminPremiumGrantsPathTemplate = "/api/admin/users/{0}/premium-grants"
$AdminPremiumRevokePathTemplate = "/api/admin/users/{0}/premium-grants/{1}/revoke"
$AdminFreeLessonResetPathTemplate = "/api/admin/users/{0}/free-lesson-allowance/reset"
$DiagnosticsDailyFreeLessonConsumedPath = "/api/me/subscription-diagnostics/scenarios/daily_free_lesson_consumed"

$PremiumPlanId = "premium"
$ManualAdminSource = "manual_admin"
$ActiveStatus = "active"
$RevokedStatus = "revoked"
$AdminSourceDevelopmentBootstrap = "development_config_bootstrap"

$GrantReason = "Admin smoke test manual Premium grant."
$RevokeReason = "Admin smoke test manual Premium revoke."
$ResetReason = "Admin smoke test free lesson allowance reset."
$InvalidUsageDate = "26-05-2026"
$MissingUserAuditUserId = "00000000-0000-0000-0000-000000000001"

function Write-Step {
    param([string]$Message)
    Write-Host "[STEP] $Message" -ForegroundColor Cyan
}

function Write-Pass {
    param([string]$Message)
    Write-Host "[PASS] $Message" -ForegroundColor Green
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
        $invokeParams.Body = ($Body | ConvertTo-Json -Depth 10)
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

function Assert-True {
    param(
        [bool]$Condition,
        [string]$Message
    )

    if (-not $Condition) {
        throw "Assert-True failed: $Message"
    }
}

function Assert-PropertyExists {
    param(
        $Object,
        [string]$PropertyName,
        [string]$Message
    )

    if ($null -eq $Object) {
        throw "Assert-PropertyExists failed: object is null. $Message"
    }

    $propertyExists = $Object.PSObject.Properties.Name -contains $PropertyName
    if (-not $propertyExists) {
        throw "Assert-PropertyExists failed: property '$PropertyName' not found. $Message"
    }
}

$adminHeaders = $null
$normalHeaders = $null


Write-Step "Verify GET /admin/ static admin shell"
$adminShellUrl = "$BaseUrl$AdminShellPath"
$adminShellResponse = Invoke-WebRequest -Method $MethodGet -Uri $adminShellUrl
Assert-Equal -Expected $StatusOk -Actual ([int]$adminShellResponse.StatusCode) -Message "admin shell status"
Write-Pass "Admin shell static page is reachable"

Write-Step "Login as admin"
$adminLoginUrl = "$BaseUrl$AuthLoginPath"
$adminLoginBody = @{ email = $AdminEmail; password = $AdminPassword }
$adminLogin = Invoke-ExpectStatusCode -Method $MethodPost -Url $adminLoginUrl -Headers $null -Body $adminLoginBody -ExpectedStatusCodes @($StatusOk)
Assert-PropertyExists -Object $adminLogin.Body -PropertyName "accessToken" -Message "Admin login response must include accessToken"
$adminToken = $adminLogin.Body.accessToken
Assert-True -Condition (-not [string]::IsNullOrWhiteSpace($adminToken)) -Message "Admin token must not be empty"
$adminHeaders = @{ Authorization = "Bearer $adminToken" }
Write-Pass "Admin login succeeded"

Write-Step "Login as normal user"
$normalLoginBody = @{ email = $NormalEmail; password = $NormalPassword }
$normalLogin = Invoke-ExpectStatusCode -Method $MethodPost -Url $adminLoginUrl -Headers $null -Body $normalLoginBody -ExpectedStatusCodes @($StatusOk)
Assert-PropertyExists -Object $normalLogin.Body -PropertyName "accessToken" -Message "Normal user login response must include accessToken"
$normalToken = $normalLogin.Body.accessToken
Assert-True -Condition (-not [string]::IsNullOrWhiteSpace($normalToken)) -Message "Normal token must not be empty"
$normalHeaders = @{ Authorization = "Bearer $normalToken" }
Write-Pass "Normal user login succeeded"

Write-Step "Verify GET /api/admin/me"
$adminMeUrl = "$BaseUrl$AdminMePath"
$adminMe = Invoke-ExpectStatusCode -Method $MethodGet -Url $adminMeUrl -Headers $adminHeaders -Body $null -ExpectedStatusCodes @($StatusOk)
Assert-Equal -Expected $true -Actual $adminMe.Body.isAdmin -Message "admin/me isAdmin"
Assert-Equal -Expected $AdminSourceDevelopmentBootstrap -Actual $adminMe.Body.adminSource -Message "admin/me adminSource"
Write-Pass "Admin identity check succeeded"

Write-Step "Verify GET /api/admin/capabilities"
$adminCapabilitiesUrl = "$BaseUrl$AdminCapabilitiesPath"
$adminCapabilities = Invoke-ExpectStatusCode -Method $MethodGet -Url $adminCapabilitiesUrl -Headers $adminHeaders -Body $null -ExpectedStatusCodes @($StatusOk)
Assert-Equal -Expected $AdminSourceDevelopmentBootstrap -Actual $adminCapabilities.Body.adminSource -Message "admin/capabilities adminSource"
Assert-Equal -Expected $true -Actual $adminCapabilities.Body.capabilities.adminSelfCheck -Message "capabilities.adminSelfCheck"
Assert-Equal -Expected $true -Actual $adminCapabilities.Body.capabilities.userLookupByEmail -Message "capabilities.userLookupByEmail"
Assert-Equal -Expected $true -Actual $adminCapabilities.Body.capabilities.userDiagnostics -Message "capabilities.userDiagnostics"
Assert-Equal -Expected $true -Actual $adminCapabilities.Body.capabilities.auditLogRead -Message "capabilities.auditLogRead"
Assert-Equal -Expected $true -Actual $adminCapabilities.Body.capabilities.manualPremiumGrant -Message "capabilities.manualPremiumGrant"
Assert-Equal -Expected $true -Actual $adminCapabilities.Body.capabilities.manualPremiumRevoke -Message "capabilities.manualPremiumRevoke"
Assert-Equal -Expected $true -Actual $adminCapabilities.Body.capabilities.freeLessonAllowanceReset -Message "capabilities.freeLessonAllowanceReset"
Assert-Equal -Expected $true -Actual $adminCapabilities.Body.capabilities.localSmokeTestScript -Message "capabilities.localSmokeTestScript"
Assert-Equal -Expected $false -Actual $adminCapabilities.Body.capabilities.cmsUiAvailable -Message "capabilities.cmsUiAvailable"
Assert-Equal -Expected $false -Actual $adminCapabilities.Body.capabilities.productionRolesAvailable -Message "capabilities.productionRolesAvailable"
Assert-Equal -Expected $false -Actual $adminCapabilities.Body.capabilities.billingProviderConfigured -Message "capabilities.billingProviderConfigured"
Assert-Equal -Expected $false -Actual $adminCapabilities.Body.capabilities.paddleCheckoutAvailable -Message "capabilities.paddleCheckoutAvailable"
Assert-Equal -Expected $false -Actual $adminCapabilities.Body.capabilities.paddleWebhooksAvailable -Message "capabilities.paddleWebhooksAvailable"
Assert-Equal -Expected $false -Actual $adminCapabilities.Body.capabilities.mobileStoreEntitlementBridgeAvailable -Message "capabilities.mobileStoreEntitlementBridgeAvailable"
Write-Pass "Admin capabilities check succeeded"

Write-Step "Verify admin user lookup by email"
$lookupUrl = "{0}{1}?email={2}" -f $BaseUrl, $AdminUsersByEmailPath, [uri]::EscapeDataString($NormalEmail)
$lookup = Invoke-ExpectStatusCode -Method $MethodGet -Url $lookupUrl -Headers $adminHeaders -Body $null -ExpectedStatusCodes @($StatusOk)
Assert-PropertyExists -Object $lookup.Body -PropertyName "user" -Message "Lookup must include user"
Assert-PropertyExists -Object $lookup.Body.user -PropertyName "userId" -Message "Lookup user must include userId"
Assert-PropertyExists -Object $lookup.Body -PropertyName "subscriptionStatus" -Message "Lookup must include subscriptionStatus"
Assert-PropertyExists -Object $lookup.Body -PropertyName "recentLessonSessions" -Message "Lookup must include recentLessonSessions"
Assert-PropertyExists -Object $lookup.Body -PropertyName "dailyUsageCounters" -Message "Lookup must include dailyUsageCounters"
Assert-PropertyExists -Object $lookup.Body -PropertyName "activeEntitlements" -Message "Lookup must include activeEntitlements"
Assert-PropertyExists -Object $lookup.Body -PropertyName "recentUsageEvents" -Message "Lookup must include recentUsageEvents"
$targetUserId = $lookup.Body.user.userId
Assert-True -Condition (-not [string]::IsNullOrWhiteSpace($targetUserId)) -Message "Target userId must not be empty"
Write-Pass "Admin user lookup succeeded"

Write-Step "Verify audit log endpoint with limit=5"
$auditUrlLimit5 = "$BaseUrl$([string]::Format($AdminAuditActionsPathTemplate, $targetUserId))?limit=5"
$auditLimit5 = Invoke-ExpectStatusCode -Method $MethodGet -Url $auditUrlLimit5 -Headers $adminHeaders -Body $null -ExpectedStatusCodes @($StatusOk)
Assert-PropertyExists -Object $auditLimit5.Body -PropertyName "items" -Message "Audit response must include items"
Assert-Equal -Expected 5 -Actual $auditLimit5.Body.limit -Message "Audit response limit"
Write-Pass "Audit log read succeeded"

Write-Step "Verify manual Premium grant"
$grantUrl = "$BaseUrl$([string]::Format($AdminPremiumGrantsPathTemplate, $targetUserId))"
$grantBody = @{ durationDays = 1; reason = $GrantReason }
$grant = Invoke-ExpectStatusCode -Method $MethodPost -Url $grantUrl -Headers $adminHeaders -Body $grantBody -ExpectedStatusCodes @($StatusOk, $StatusCreated)
Assert-Equal -Expected $PremiumPlanId -Actual $grant.Body.planId -Message "Grant planId"
Assert-Equal -Expected $ManualAdminSource -Actual $grant.Body.source -Message "Grant source"
Assert-Equal -Expected $ActiveStatus -Actual $grant.Body.status -Message "Grant status"
Assert-Equal -Expected $true -Actual $grant.Body.auditWritten -Message "Grant auditWritten"
Assert-PropertyExists -Object $grant.Body -PropertyName "entitlementId" -Message "Grant must include entitlementId"
$entitlementId = $grant.Body.entitlementId
Assert-True -Condition (-not [string]::IsNullOrWhiteSpace($entitlementId)) -Message "EntitlementId must not be empty"
Write-Pass "Manual Premium grant succeeded"

Write-Step "Verify manual Premium revoke"
$revokeUrl = "$BaseUrl$([string]::Format($AdminPremiumRevokePathTemplate, $targetUserId, $entitlementId))"
$revokeBody = @{ reason = $RevokeReason }
$revoke = Invoke-ExpectStatusCode -Method $MethodPost -Url $revokeUrl -Headers $adminHeaders -Body $revokeBody -ExpectedStatusCodes @($StatusOk)
Assert-Equal -Expected $RevokedStatus -Actual $revoke.Body.status -Message "Revoke status"
Assert-Equal -Expected $ManualAdminSource -Actual $revoke.Body.source -Message "Revoke source"
Assert-Equal -Expected $true -Actual $revoke.Body.auditWritten -Message "Revoke auditWritten"
Write-Pass "Manual Premium revoke succeeded"

Write-Step "Prepare daily free lesson consumed diagnostics scenario"
$diagnosticsUrl = "$BaseUrl$DiagnosticsDailyFreeLessonConsumedPath"
$diagnostics = Invoke-ExpectStatusCode -Method $MethodPost -Url $diagnosticsUrl -Headers $normalHeaders -Body @{} -ExpectedStatusCodes @($StatusOk)
Write-Pass "Diagnostics scenario applied"

Write-Step "Verify lookup reflects consumed free lesson"
$lookupAfterDiagnostics = Invoke-ExpectStatusCode -Method $MethodGet -Url $lookupUrl -Headers $adminHeaders -Body $null -ExpectedStatusCodes @($StatusOk)
Assert-Equal -Expected $true -Actual $lookupAfterDiagnostics.Body.subscriptionStatus.freeLessonUsedToday -Message "freeLessonUsedToday after diagnostics"
Assert-Equal -Expected 0 -Actual $lookupAfterDiagnostics.Body.subscriptionStatus.freeLessonRemainingToday -Message "freeLessonRemainingToday after diagnostics"
Write-Pass "Consumed free lesson state verified"

Write-Step "Verify free lesson allowance reset"
$resetUrl = "$BaseUrl$([string]::Format($AdminFreeLessonResetPathTemplate, $targetUserId))"
$resetBody = @{ reason = $ResetReason }
$reset = Invoke-ExpectStatusCode -Method $MethodPost -Url $resetUrl -Headers $adminHeaders -Body $resetBody -ExpectedStatusCodes @($StatusOk)
Assert-Equal -Expected $true -Actual $reset.Body.resetApplied -Message "resetApplied"
Assert-Equal -Expected $true -Actual $reset.Body.auditWritten -Message "reset auditWritten"
Assert-PropertyExists -Object $reset.Body -PropertyName "removedDailyFreeLessonUsageId" -Message "Reset must include removedDailyFreeLessonUsageId"
Write-Pass "Free lesson allowance reset succeeded"

Write-Step "Verify lookup reflects reset free lesson"
$lookupAfterReset = Invoke-ExpectStatusCode -Method $MethodGet -Url $lookupUrl -Headers $adminHeaders -Body $null -ExpectedStatusCodes @($StatusOk)
Assert-Equal -Expected $false -Actual $lookupAfterReset.Body.subscriptionStatus.freeLessonUsedToday -Message "freeLessonUsedToday after reset"
Assert-Equal -Expected 1 -Actual $lookupAfterReset.Body.subscriptionStatus.freeLessonRemainingToday -Message "freeLessonRemainingToday after reset"
Write-Pass "Reset free lesson state verified"

Write-Step "Verify audit log contains expected action types"
$auditUrlLimit10 = "$BaseUrl$([string]::Format($AdminAuditActionsPathTemplate, $targetUserId))?limit=10"
$auditLimit10 = Invoke-ExpectStatusCode -Method $MethodGet -Url $auditUrlLimit10 -Headers $adminHeaders -Body $null -ExpectedStatusCodes @($StatusOk)
Assert-PropertyExists -Object $auditLimit10.Body -PropertyName "items" -Message "Audit response must include items"
$actionTypes = @($auditLimit10.Body.items | ForEach-Object { $_.actionType })
Assert-True -Condition ($actionTypes -contains "manual_premium_grant") -Message "Audit must contain manual_premium_grant"
Assert-True -Condition ($actionTypes -contains "manual_premium_revoke") -Message "Audit must contain manual_premium_revoke"
Assert-True -Condition ($actionTypes -contains "free_lesson_allowance_reset") -Message "Audit must contain free_lesson_allowance_reset"
Write-Pass "Audit action types verified"

Write-Step "Verify expected error statuses"
Invoke-ExpectStatusCode -Method $MethodGet -Url $adminMeUrl -Headers $null -Body $null -ExpectedStatusCodes @($StatusUnauthorized) | Out-Null
Invoke-ExpectStatusCode -Method $MethodGet -Url $adminCapabilitiesUrl -Headers $null -Body $null -ExpectedStatusCodes @($StatusUnauthorized) | Out-Null
Invoke-ExpectStatusCode -Method $MethodGet -Url $adminCapabilitiesUrl -Headers $normalHeaders -Body $null -ExpectedStatusCodes @($StatusForbidden) | Out-Null
Invoke-ExpectStatusCode -Method $MethodGet -Url ("{0}{1}?email={2}" -f $BaseUrl, $AdminUsersByEmailPath, [uri]::EscapeDataString($AdminEmail)) -Headers $normalHeaders -Body $null -ExpectedStatusCodes @($StatusForbidden) | Out-Null
Invoke-ExpectStatusCode -Method $MethodGet -Url "$BaseUrl$([string]::Format($AdminAuditActionsPathTemplate, $targetUserId))?limit=0" -Headers $adminHeaders -Body $null -ExpectedStatusCodes @($StatusBadRequest) | Out-Null
Invoke-ExpectStatusCode -Method $MethodGet -Url "$BaseUrl$([string]::Format($AdminAuditActionsPathTemplate, $MissingUserAuditUserId))" -Headers $adminHeaders -Body $null -ExpectedStatusCodes @($StatusNotFound) | Out-Null
Invoke-ExpectStatusCode -Method $MethodPost -Url $grantUrl -Headers $adminHeaders -Body @{ durationDays = 1; reason = "" } -ExpectedStatusCodes @($StatusBadRequest) | Out-Null
Invoke-ExpectStatusCode -Method $MethodPost -Url $resetUrl -Headers $adminHeaders -Body @{ reason = $ResetReason; usageDate = $InvalidUsageDate } -ExpectedStatusCodes @($StatusBadRequest) | Out-Null
Write-Pass "Expected error status checks succeeded"

Write-Pass "Admin foundation smoke test passed."
