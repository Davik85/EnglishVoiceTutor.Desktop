param(
    [string]$BaseUrl = 'https://api.languagevoicetutor.com',
    [string]$AdminEmail = '',
    [string]$AdminBearerToken = $env:EVT_ADMIN_BEARER_TOKEN,
    [string]$NonAdminBearerToken = $env:EVT_NON_ADMIN_BEARER_TOKEN,
    [switch]$MutatingChecks
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$StatusOk = 200
$StatusUnauthorized = 401
$StatusForbidden = 403
$StatusMethodNotAllowed = 405
$StatusNotFound = 404

$AdminShellPath = '/admin/'
$AdminMePath = '/api/admin/me'
$AdminCapabilitiesPath = '/api/admin/capabilities'
$CmsSourceStatusPath = '/api/cms/runtime-content/source-status'
$CmsContentPacksPath = '/api/admin/dev/cms/content-packs'
$CmsRuntimeStatusPath = '/api/admin/dev/cms/runtime-content/status'
$CmsValidatePath = '/api/admin/dev/cms/content-packs/static-json-v1/validate'
$CmsVersionsPath = '/api/admin/dev/cms/content-packs/static-json-v1/versions'
$CmsPublishedStatusPath = '/api/admin/dev/cms/published-content/status'

function Join-Url {
    param([string]$Root, [string]$Path)
    return $Root.TrimEnd('/') + $Path
}

function Write-Step { param([string]$Message) Write-Host "[STEP] $Message" -ForegroundColor Cyan }
function Write-Pass { param([string]$Message) Write-Host "[PASS] $Message" -ForegroundColor Green }
function Write-Warn { param([string]$Message) Write-Warning $Message }

function Invoke-Status {
    param(
        [string]$Method,
        [string]$Path,
        [hashtable]$Headers = $null,
        $Body = $null
    )

    $parameters = @{
        Method = $Method
        Uri = (Join-Url -Root $BaseUrl -Path $Path)
        TimeoutSec = 30
    }

    if ($Headers) { $parameters.Headers = $Headers }
    if ($null -ne $Body) {
        $parameters.Body = ($Body | ConvertTo-Json -Depth 8)
        $parameters.ContentType = 'application/json'
    }

    try {
        $bodyValue = Invoke-RestMethod @parameters
        return [pscustomobject]@{ StatusCode = $StatusOk; Body = $bodyValue }
    }
    catch {
        if ($_.Exception.Response -and $_.Exception.Response.StatusCode) {
            return [pscustomobject]@{ StatusCode = [int]$_.Exception.Response.StatusCode.value__; Body = $null }
        }

        throw
    }
}

function Assert-StatusIn {
    param($Result, [int[]]$Expected, [string]$Label)
    if ($Expected -notcontains $Result.StatusCode) {
        throw "$Label expected HTTP $($Expected -join '/') but got HTTP $($Result.StatusCode)."
    }
}

Write-Step "Checking public admin shell at $BaseUrl$AdminShellPath"
$shellResponse = Invoke-WebRequest -Method Get -Uri (Join-Url -Root $BaseUrl -Path $AdminShellPath) -UseBasicParsing -TimeoutSec 30
if ([int]$shellResponse.StatusCode -ne $StatusOk) {
    throw "Admin shell expected HTTP 200 but got HTTP $([int]$shellResponse.StatusCode)."
}
if ($shellResponse.Content -notmatch 'Language Voice Tutor Admin') {
    Write-Warn 'Admin shell was reachable, but the expected title text was not found.'
}
Write-Pass '/admin/ static shell is reachable.'

Write-Step 'Checking public non-secret CMS runtime source diagnostic endpoint'
$sourceStatus = Invoke-Status -Method Get -Path $CmsSourceStatusPath
Assert-StatusIn -Result $sourceStatus -Expected @($StatusOk) -Label $CmsSourceStatusPath
if ($sourceStatus.Body) {
    $sourceStatus.Body | ConvertTo-Json -Depth 6 | Write-Host
}
Write-Pass 'CMS runtime source status endpoint is reachable without exposing content.'

Write-Step 'Checking unauthenticated admin API is rejected'
$unauthMe = Invoke-Status -Method Get -Path $AdminMePath
Assert-StatusIn -Result $unauthMe -Expected @($StatusUnauthorized, $StatusForbidden) -Label 'Unauthenticated /api/admin/me'
$unauthCms = Invoke-Status -Method Get -Path $CmsContentPacksPath
Assert-StatusIn -Result $unauthCms -Expected @($StatusUnauthorized, $StatusForbidden) -Label 'Unauthenticated CMS content packs'
Write-Pass 'Unauthenticated admin API requests are rejected.'

if (-not [string]::IsNullOrWhiteSpace($NonAdminBearerToken)) {
    Write-Step 'Checking authenticated non-admin token is rejected'
    $nonAdminHeaders = @{ Authorization = "Bearer $NonAdminBearerToken" }
    $nonAdminResult = Invoke-Status -Method Get -Path $AdminMePath -Headers $nonAdminHeaders
    Assert-StatusIn -Result $nonAdminResult -Expected @($StatusForbidden) -Label 'Non-admin /api/admin/me'
    Write-Pass 'Authenticated non-admin token is rejected from admin API.'
}
else {
    Write-Warn 'Skipping non-admin rejection check because -NonAdminBearerToken / EVT_NON_ADMIN_BEARER_TOKEN was not provided.'
}

if (-not [string]::IsNullOrWhiteSpace($AdminBearerToken)) {
    Write-Step 'Checking authenticated admin token is accepted'
    $adminHeaders = @{ Authorization = "Bearer $AdminBearerToken" }
    $adminMe = Invoke-Status -Method Get -Path $AdminMePath -Headers $adminHeaders
    Assert-StatusIn -Result $adminMe -Expected @($StatusOk) -Label 'Admin /api/admin/me'
    if (-not [string]::IsNullOrWhiteSpace($AdminEmail) -and $adminMe.Body.email -ne $AdminEmail) {
        Write-Warn "Admin token email '$($adminMe.Body.email)' does not match supplied AdminEmail '$AdminEmail'."
    }

    $capabilities = Invoke-Status -Method Get -Path $AdminCapabilitiesPath -Headers $adminHeaders
    Assert-StatusIn -Result $capabilities -Expected @($StatusOk) -Label 'Admin capabilities'

    $packs = Invoke-Status -Method Get -Path $CmsContentPacksPath -Headers $adminHeaders
    Assert-StatusIn -Result $packs -Expected @($StatusOk) -Label 'Admin CMS content packs'

    $runtime = Invoke-Status -Method Get -Path $CmsRuntimeStatusPath -Headers $adminHeaders
    Assert-StatusIn -Result $runtime -Expected @($StatusOk, 503) -Label 'Admin CMS runtime status'
    if ($runtime.Body) { $runtime.Body | ConvertTo-Json -Depth 6 | Write-Host }

    $published = Invoke-Status -Method Get -Path $CmsPublishedStatusPath -Headers $adminHeaders
    Assert-StatusIn -Result $published -Expected @($StatusOk) -Label 'Admin CMS published status'

    if ($MutatingChecks) {
        Write-Step 'Running explicit CMS validation and version-list checks (no draft mutations)'
        $validate = Invoke-Status -Method Post -Path $CmsValidatePath -Headers $adminHeaders -Body @{}
        Assert-StatusIn -Result $validate -Expected @($StatusOk) -Label 'CMS validate'
        $versions = Invoke-Status -Method Get -Path $CmsVersionsPath -Headers $adminHeaders
        Assert-StatusIn -Result $versions -Expected @($StatusOk) -Label 'CMS versions'
        Write-Pass 'CMS validation and version list checks completed.'
    }
    else {
        Write-Warn 'Skipping optional CMS validation/version checks. Re-run with -MutatingChecks after admin review if desired; this script does not save drafts, publish, or restore versions.'
    }

    Write-Pass 'Authenticated admin API and CMS status checks passed.'
}
else {
    Write-Warn 'Skipping authenticated admin API checks because -AdminBearerToken / EVT_ADMIN_BEARER_TOKEN was not provided.'
}

Write-Host 'CMS/Admin server readiness verification completed.'
