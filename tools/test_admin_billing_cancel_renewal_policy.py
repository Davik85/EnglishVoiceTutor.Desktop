from pathlib import Path
ROOT = Path(__file__).resolve().parents[1]

def read(path): return (ROOT / path).read_text(encoding='utf-8')
def require(text, needle, label):
    if needle not in text: raise AssertionError(f"Missing {label}: {needle}")
def forbid(text, needle, label):
    if needle in text: raise AssertionError(f"Forbidden {label}: {needle}")

api = read('backend/EnglishVoiceTutor.Api/Constants/ApiConstants.cs')
endpoints = read('backend/EnglishVoiceTutor.Api/Endpoints/AdminEndpoints.cs')
request = read('backend/EnglishVoiceTutor.Api/Contracts/Admin/AdminBillingCancelRenewalRequest.cs')
service = read('backend/EnglishVoiceTutor.Api/Services/Admin/AdminBillingCancellationService.cs')
cancel_service = read('backend/EnglishVoiceTutor.Api/Services/Billing/BillingSubscriptionCancellationService.cs')
admin_js = read('backend/EnglishVoiceTutor.Api/wwwroot/admin/admin.js')
index = read('backend/EnglishVoiceTutor.Api/wwwroot/admin/index.html')
audit = read('backend/EnglishVoiceTutor.Api/Constants/AdminAuditConstants.cs')

require(api, '/api/admin/users/{userId:guid}/billing/cancel-renewal', 'admin cancel-renewal route')
require(endpoints, 'CancelUserBillingRenewalAsync', 'admin endpoint handler')
require(endpoints, 'RequireAuthorization(AdminAuthorizationConstants.BootstrapAdminPolicyName)', 'admin authorization policy')
require(request, 'public string Reason', 'required reason shape')
require(service, 'string.IsNullOrWhiteSpace(request.Reason)', 'server reason validation')
forbid(request, 'ProviderSubscriptionId', 'provider subscription id in browser request')
forbid(request, 'providerSubscriptionId', 'provider subscription id in browser request')
require(cancel_service, 'CancelUserSubscriptionRenewalAsync', 'backend cancellation path')
require(cancel_service, 'ProviderSubscriptionId = subscription.ProviderSubscriptionId', 'subscription snapshot provider context')
forbid(service, 'dbContext.Entitlements', 'admin cancel renewal direct entitlement mutation')
require(audit, 'admin_billing_cancel_renewal_completed', 'audit action')
require(service, 'RecordTargetUserActionAsync', 'audit write')
require(service, 'resultCode', 'safe audit result metadata')
require(admin_js, 'billingCancelRenewalTemplate', 'admin UI endpoint')
require(index, 'Cancel paid renewal', 'admin UI section')
require(index, 'billing-cancel-renewal-reason', 'required reason input')
require(admin_js, 'This cancels future renewals only. Paid Premium access remains until the current paid period ends.', 'safe confirmation')
for needle in ['api.paddle.com', 'Paddle.Api', 'Paddle-Signature', 'ApiKey', 'webhook secret']:
    forbid(admin_js, needle, 'direct Paddle/API secret in admin UI')
print('Admin billing cancel renewal policy checks passed.')
