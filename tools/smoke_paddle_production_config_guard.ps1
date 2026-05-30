param(
    [switch]$Strict,
    [switch]$AssumeProduction
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$Title = "Paddle production config guard"
$TestWebhookSecretPlaceholder = "test_webhook_secret"
$ExpectedBillingProvider = "paddle"
$SandboxEnvironmentName = "sandbox"
$LiveEnvironmentName = "live"
$PositiveIntegerPattern = "^[1-9][0-9]*$"

$PaddleWebhookEnabledName = "PaddleWebhook__Enabled"
$PaddleWebhookSecretKeyName = "PaddleWebhook__SecretKey"
$PaddleWebhookTimestampToleranceSecondsName = "PaddleWebhook__TimestampToleranceSeconds"
$BillingCheckoutEnabledName = "Billing__CheckoutEnabled"
$BillingProviderName = "Billing__Provider"
$PaddleCheckoutAdapterEnabledName = "PaddleBilling__CheckoutAdapterEnabled"
$PaddleEnvironmentName = "PaddleBilling__Environment"
$PaddleApiKeyName = "PaddleBilling__ApiKey"
$PaddlePremiumPriceIdName = "PaddleBilling__PremiumPriceId"
$PaddleClientSideTokenName = "PaddleBilling__ClientSideToken"

$SecretVariableNames = @(
    $PaddleWebhookSecretKeyName,
    $PaddleApiKeyName,
    $PaddlePremiumPriceIdName,
    $PaddleClientSideTokenName
)

$AllVariableNames = @(
    $PaddleWebhookEnabledName,
    $PaddleWebhookSecretKeyName,
    $PaddleWebhookTimestampToleranceSecondsName,
    $BillingCheckoutEnabledName,
    $BillingProviderName,
    $PaddleCheckoutAdapterEnabledName,
    $PaddleEnvironmentName,
    $PaddleApiKeyName,
    $PaddlePremiumPriceIdName,
    $PaddleClientSideTokenName
)

function Get-ConfigValue {
    param([Parameter(Mandatory = $true)][string]$Name)

    return [Environment]::GetEnvironmentVariable($Name)
}

function Test-IsSet {
    param([AllowNull()][string]$Value)

    return -not [string]::IsNullOrWhiteSpace($Value)
}

function Test-IsTrueValue {
    param([AllowNull()][string]$Value)

    return (Test-IsSet $Value) -and ($Value.Trim().ToLowerInvariant() -eq "true")
}

function Test-IsSecretVariable {
    param([Parameter(Mandatory = $true)][string]$Name)

    return $SecretVariableNames -contains $Name
}

function Write-ConfigStatus {
    param(
        [Parameter(Mandatory = $true)][string]$Name,
        [AllowNull()][string]$Value
    )

    if (Test-IsSecretVariable $Name) {
        if (Test-IsSet $Value) {
            Write-Host ("{0}: set" -f $Name)
        }
        else {
            Write-Host ("{0}: missing" -f $Name)
        }

        return
    }

    if (Test-IsSet $Value) {
        Write-Host ("{0}: {1}" -f $Name, $Value)
    }
    else {
        Write-Host ("{0}: missing" -f $Name)
    }
}

function Add-Issue {
    param(
        [Parameter(Mandatory = $true)]
        [AllowEmptyCollection()]
        [System.Collections.Generic.List[string]]$Issues,
        [Parameter(Mandatory = $true)][string]$Message
    )

    $Issues.Add($Message) | Out-Null
}

function Require-Set {
    param(
        [Parameter(Mandatory = $true)]
        [AllowEmptyCollection()]
        [System.Collections.Generic.List[string]]$Issues,
        [Parameter(Mandatory = $true)][string]$Name,
        [AllowNull()][string]$Value
    )

    if (-not (Test-IsSet $Value)) {
        Add-Issue $Issues ("{0} is required but missing." -f $Name)
    }
}

Write-Host $Title
Write-Host "This script checks local environment/config shape only. It does not call Paddle and does not prove production delivery."
Write-Host "Secret-like values are never printed."
Write-Host ""

$config = @{}
foreach ($name in $AllVariableNames) {
    $value = Get-ConfigValue $name
    $config[$name] = $value
    Write-ConfigStatus -Name $name -Value $value
}

Write-Host ""

$issues = [System.Collections.Generic.List[string]]::new()
$warnings = [System.Collections.Generic.List[string]]::new()

$webhookEnabled = $config[$PaddleWebhookEnabledName]
$webhookSecretKey = $config[$PaddleWebhookSecretKeyName]
$timestampToleranceSeconds = $config[$PaddleWebhookTimestampToleranceSecondsName]
$checkoutEnabled = $config[$BillingCheckoutEnabledName]
$billingProvider = $config[$BillingProviderName]
$checkoutAdapterEnabled = $config[$PaddleCheckoutAdapterEnabledName]
$paddleEnvironment = $config[$PaddleEnvironmentName]
$paddleApiKey = $config[$PaddleApiKeyName]
$paddlePremiumPriceId = $config[$PaddlePremiumPriceIdName]
$paddleClientSideToken = $config[$PaddleClientSideTokenName]

$isCheckoutEnabled = Test-IsTrueValue $checkoutEnabled
$isCheckoutAdapterEnabled = Test-IsTrueValue $checkoutAdapterEnabled
$isWebhookEnabled = Test-IsTrueValue $webhookEnabled
$isLiveEnvironment = (Test-IsSet $paddleEnvironment) -and ($paddleEnvironment.Trim().ToLowerInvariant() -eq $LiveEnvironmentName)
$isSandboxEnvironment = (Test-IsSet $paddleEnvironment) -and ($paddleEnvironment.Trim().ToLowerInvariant() -eq $SandboxEnvironmentName)
$useLiveGradeChecks = $AssumeProduction.IsPresent -or $isLiveEnvironment
$useRequiredChecks = $Strict.IsPresent -or $useLiveGradeChecks

if ($AssumeProduction.IsPresent) {
    $warnings.Add("AssumeProduction is enabled; live-grade checks apply even if PaddleBilling__Environment is missing.") | Out-Null
}

if ($isLiveEnvironment) {
    $warnings.Add("PaddleBilling__Environment is live. This script does not call Paddle and does not prove production delivery.") | Out-Null
}

if ($useRequiredChecks -and -not $isWebhookEnabled) {
    Add-Issue $issues ("{0} should be true for webhook environments." -f $PaddleWebhookEnabledName)
}
elseif ((Test-IsSet $webhookEnabled) -and -not $isWebhookEnabled) {
    $warnings.Add(("{0} is not true; webhook endpoint may be intentionally disabled for local development." -f $PaddleWebhookEnabledName)) | Out-Null
}

if ($useRequiredChecks) {
    Require-Set -Issues $issues -Name $PaddleWebhookSecretKeyName -Value $webhookSecretKey
}

if ($useLiveGradeChecks -and (Test-IsSet $webhookSecretKey) -and ($webhookSecretKey -eq $TestWebhookSecretPlaceholder)) {
    Add-Issue $issues ("{0} must not use the local test webhook secret placeholder in live/production mode." -f $PaddleWebhookSecretKeyName)
}
elseif ((Test-IsSet $webhookSecretKey) -and ($webhookSecretKey -eq $TestWebhookSecretPlaceholder)) {
    $warnings.Add(("{0} uses the local test webhook secret placeholder; this is acceptable only for local smoke tests." -f $PaddleWebhookSecretKeyName)) | Out-Null
}

if ($useRequiredChecks) {
    Require-Set -Issues $issues -Name $PaddleWebhookTimestampToleranceSecondsName -Value $timestampToleranceSeconds
}

if (Test-IsSet $timestampToleranceSeconds) {
    $trimmedTolerance = $timestampToleranceSeconds.Trim()
    if ($trimmedTolerance -notmatch $PositiveIntegerPattern) {
        Add-Issue $issues ("{0} must be a positive integer." -f $PaddleWebhookTimestampToleranceSecondsName)
    }
}

if ($isCheckoutEnabled -and ((-not (Test-IsSet $billingProvider)) -or ($billingProvider.Trim().ToLowerInvariant() -ne $ExpectedBillingProvider))) {
    Add-Issue $issues ("{0} should be paddle when {1} is true." -f $BillingProviderName, $BillingCheckoutEnabledName)
}
elseif ($useRequiredChecks -and -not (Test-IsSet $billingProvider)) {
    Add-Issue $issues ("{0} is required in strict or production-readiness checks." -f $BillingProviderName)
}

if ($isCheckoutAdapterEnabled) {
    if (-not ($isSandboxEnvironment -or $isLiveEnvironment)) {
        Add-Issue $issues ("{0} should be sandbox or live when {1} is true." -f $PaddleEnvironmentName, $PaddleCheckoutAdapterEnabledName)
    }

    Require-Set -Issues $issues -Name $PaddleApiKeyName -Value $paddleApiKey
    Require-Set -Issues $issues -Name $PaddlePremiumPriceIdName -Value $paddlePremiumPriceId
    Require-Set -Issues $issues -Name $PaddleClientSideTokenName -Value $paddleClientSideToken
}
elseif ($useRequiredChecks) {
    if (-not (Test-IsSet $checkoutAdapterEnabled)) {
        Add-Issue $issues ("{0} is required in strict or production-readiness checks." -f $PaddleCheckoutAdapterEnabledName)
    }

    if (-not ($isSandboxEnvironment -or $isLiveEnvironment)) {
        Add-Issue $issues ("{0} should be sandbox or live in strict or production-readiness checks." -f $PaddleEnvironmentName)
    }

    Require-Set -Issues $issues -Name $PaddleApiKeyName -Value $paddleApiKey
    Require-Set -Issues $issues -Name $PaddlePremiumPriceIdName -Value $paddlePremiumPriceId
    Require-Set -Issues $issues -Name $PaddleClientSideTokenName -Value $paddleClientSideToken
}

if ($useLiveGradeChecks) {
    Require-Set -Issues $issues -Name $PaddleWebhookSecretKeyName -Value $webhookSecretKey
    Require-Set -Issues $issues -Name $PaddleApiKeyName -Value $paddleApiKey
    Require-Set -Issues $issues -Name $PaddlePremiumPriceIdName -Value $paddlePremiumPriceId
    Require-Set -Issues $issues -Name $PaddleClientSideTokenName -Value $paddleClientSideToken
}

if (@($AllVariableNames | Where-Object { Test-IsSet ($config[$_]) }).Count -eq 0) {
    $warnings.Add("No Paddle billing environment variables are set. For local dev smoke only, set the test variables used by existing smoke scripts. For sandbox/live readiness, rerun with -Strict and secure environment configuration.") | Out-Null
}

if ($warnings.Count -gt 0) {
    Write-Host "Warnings:"
    foreach ($warning in $warnings) {
        Write-Host ("- {0}" -f $warning)
    }
    Write-Host ""
}

if ($issues.Count -gt 0) {
    Write-Host "Failed checks:"
    foreach ($issue in $issues) {
        Write-Host ("- {0}" -f $issue)
    }
    exit 1
}

Write-Host "Config guard completed without blocking issues."
if (-not $useRequiredChecks) {
    Write-Host "Non-strict local mode: missing production/sandbox values are guidance only. Use -Strict for sandbox/live readiness checks."
}

exit 0
