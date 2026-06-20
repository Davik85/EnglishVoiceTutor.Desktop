"""Static policy checks for safe Paddle sandbox cancellation diagnostics."""
from pathlib import Path
ROOT = Path(__file__).resolve().parents[1]

def read(path): return (ROOT / path).read_text(encoding='utf-8')
def require(text, needle, label):
    if needle not in text: raise AssertionError(f"Missing {label}: {needle}")
def forbid(text, needle, label):
    if needle in text: raise AssertionError(f"Forbidden {label}: {needle}")

xaml = read('Views/SettingsView.xaml')
cancel_service = read('backend/EnglishVoiceTutor.Api/Services/Billing/BillingSubscriptionCancellationService.cs')
adapter = read('backend/EnglishVoiceTutor.Api/Services/Billing/PaddleBillingProviderCheckoutAdapter.cs')
admin_service = read('backend/EnglishVoiceTutor.Api/Services/Admin/AdminBillingCancellationService.cs')
admin_response = read('backend/EnglishVoiceTutor.Api/Contracts/Admin/AdminBillingCancelRenewalResponse.cs')
billing_response = read('backend/EnglishVoiceTutor.Api/Contracts/Billing/CancelBillingSubscriptionResponse.cs')
provider_result = read('backend/EnglishVoiceTutor.Api/Services/Billing/BillingProviderSubscriptionCancelResult.cs')
admin_js = read('backend/EnglishVoiceTutor.Api/wwwroot/admin/admin.js')
doc = read('docs/paddle-sandbox-cancellation-validation.md')
foundation = read('docs/subscription-billing-foundation.md')

# Desktop billing buttons must be flexible and wrapping for localized labels.
require(xaml, '<WrapPanel Margin="0,12,0,0" Orientation="Horizontal">', 'billing button WrapPanel')
for binding in ['BuyPremiumButtonText', 'CancelSubscriptionButtonText', 'RefreshStatusButtonText']:
    require(xaml, f'Text="{{Binding {binding}}}"', f'{binding} text binding')
require(xaml, 'TextWrapping="Wrap"', 'button text wrapping')
forbid(xaml, 'MaxWidth="300"', 'cancel button fixed clipping maximum')
button_region = xaml[xaml.find('<WrapPanel Margin="0,12,0,0" Orientation="Horizontal">'):xaml.find('</WrapPanel>', xaml.find('<WrapPanel Margin="0,12,0,0" Orientation="Horizontal">'))]
for tiny in ['Width="120"', 'Width="150"', 'Width="110"']:
    forbid(button_region, tiny, 'hard fixed tiny billing button width')


# Paddle Billing cancel-renewal request shape: official cancel endpoint with safe body only.
require(adapter, 'SubscriptionCancelSuffix = "/cancel"', 'Paddle cancel endpoint suffix')
require(adapter, 'new HttpRequestMessage(HttpMethod.Post, subscriptionUri)', 'Paddle cancel uses POST')
require(adapter, 'effective_from = "next_billing_period"', 'Paddle cancel at period end body')
forbid(adapter, 'scheduled_change = new', 'old update-subscription scheduled_change body for cancel')
forbid(adapter, 'new HttpRequestMessage(HttpMethod.Patch, subscriptionUri)', 'old PATCH update-subscription cancel request')

# Safe diagnostics fields must flow provider -> backend response -> Admin response/audit.
for needle in ['ProviderErrorCode', 'ProviderErrorMessageSafe', 'ProviderHttpStatusCode', 'ProviderRequestId', 'CancellationAttemptedAtUtc', 'ProviderSubscriptionPresent', 'ProviderSubscriptionIdLast4', 'ProviderSubscriptionIdHash']:
    require(provider_result, needle, f'provider result {needle}')
    require(billing_response, needle, f'billing response {needle}')
    require(admin_response, needle, f'admin response {needle}')
for needle in ['providerErrorCode', 'providerErrorMessageSafe', 'providerHttpStatusCode', 'providerRequestId', 'cancellationAttemptedAtUtc', 'providerSubscriptionPresent', 'providerSubscriptionIdLast4', 'providerSubscriptionIdHash']:
    require(admin_service, needle, f'audit safe metadata {needle}')
    require(admin_js, needle, f'Admin UI provider diagnostic {needle}')

# Provider errors are failures, not scheduled-cancellation success.
require(cancel_service, 'if (!result.Accepted)', 'provider failure branch')
require(cancel_service, 'subscription.CancelAtPeriodEnd = true;', 'only accepted branch mutates cancelAtPeriodEnd')
if cancel_service.find('subscription.CancelAtPeriodEnd = true;') < cancel_service.find('if (!result.Accepted)'):
    raise AssertionError('cancelAtPeriodEnd mutation must occur only after provider accepted cancellation')
require(admin_service, 'if (response.Success || response.Accepted) return response.AlreadyCanceling ? "already_scheduled" : "cancellation_scheduled";', 'only success/accepted maps to scheduled')
require(admin_service, 'return "provider_error";', 'provider error result code')
require(admin_js, 'if (resultCode === "provider_error")', 'provider_error warning branch')
require(admin_js, 'Cancellation was not confirmed by the provider', 'provider_error warning copy')
require(admin_js, 'billingCancelRenewalErrorElement.textContent', 'provider_error uses error element')
forbid(admin_js, 'provider_error`) billingCancelRenewalSuccessElement', 'provider_error success display')

# No direct Paddle/secrets in Desktop learner or Admin UI.
for secret in ['Authorization', 'ApiKey', 'webhook secret', 'Paddle-Signature', 'api.paddle.com']:
    forbid(xaml, secret, 'desktop XAML')
for secret in ['ApiKey', 'webhook secret', 'Paddle-Signature', 'api.paddle.com']:
    forbid(admin_js, secret, 'admin JS')

# Cancellation safety: provider failure must not touch entitlements or revoke paid access.
forbid(cancel_service, 'dbContext.Entitlements', 'cancel service entitlement mutation')
forbid(admin_service, 'dbContext.Entitlements', 'admin cancel entitlement mutation')

# Documentation/checklist coverage.
for needle in [
    'controlled tester/sandbox validation only',
    'Desktop cancellation path',
    'Admin support cancellation path',
    'premiumActive = Yes',
    'billingProvider = paddle',
    'renewalStatus = renewal_active',
    'nextRenewalState = renewal_expected',
    'hasActivePaidProviderSubscription = Yes',
    'providerSubscriptionPresent = Yes',
    'canRequestCancelRenewal = Yes',
    'currentPeriodEndUtc',
    'paidAccessUntilUtc',
    'renewalStatus = cancellation_scheduled',
    'nextRenewalState = no_renewal_scheduled',
    'cancelAtPeriodEnd = Yes',
    'Premium remains active until the paid access end',
    'not production/live Paddle readiness',
    'provider_error',
]:
    require(doc, needle, f'documentation checklist {needle}')
require(foundation, 'docs/paddle-sandbox-cancellation-validation.md', 'foundation doc link')

print('Paddle cancellation diagnostics policy checks passed.')
