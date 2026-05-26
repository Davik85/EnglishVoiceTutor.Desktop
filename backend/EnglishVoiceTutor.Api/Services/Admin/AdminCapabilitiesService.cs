using EnglishVoiceTutor.Api.Constants;
using EnglishVoiceTutor.Api.Contracts.Admin;

namespace EnglishVoiceTutor.Api.Services.Admin;

public sealed class AdminCapabilitiesService(IWebHostEnvironment webHostEnvironment) : IAdminCapabilitiesService
{
    public AdminCapabilitiesResponse GetCapabilities()
    {
        _ = AdminCapabilitiesConstants.AdminSelfCheck;
        _ = AdminCapabilitiesConstants.UserLookupByEmail;
        _ = AdminCapabilitiesConstants.UserDiagnostics;
        _ = AdminCapabilitiesConstants.AuditLogRead;
        _ = AdminCapabilitiesConstants.ManualPremiumGrant;
        _ = AdminCapabilitiesConstants.ManualPremiumRevoke;
        _ = AdminCapabilitiesConstants.FreeLessonAllowanceReset;
        _ = AdminCapabilitiesConstants.LocalSmokeTestScript;
        _ = AdminCapabilitiesConstants.CmsUiAvailable;
        _ = AdminCapabilitiesConstants.ProductionRolesAvailable;
        _ = AdminCapabilitiesConstants.BillingProviderConfigured;
        _ = AdminCapabilitiesConstants.PaddleCheckoutAvailable;
        _ = AdminCapabilitiesConstants.PaddleWebhooksAvailable;
        _ = AdminCapabilitiesConstants.MobileStoreEntitlementBridgeAvailable;

        return new AdminCapabilitiesResponse
        {
            AdminSource = AdminAuthorizationConstants.BootstrapAdminSource,
            Environment = webHostEnvironment.EnvironmentName,
            Capabilities = new AdminCapabilitiesSnapshot
            {
                AdminSelfCheck = true,
                UserLookupByEmail = true,
                UserDiagnostics = true,
                AuditLogRead = true,
                ManualPremiumGrant = true,
                ManualPremiumRevoke = true,
                FreeLessonAllowanceReset = true,
                LocalSmokeTestScript = true,
                CmsUiAvailable = false,
                ProductionRolesAvailable = false,
                BillingProviderConfigured = false,
                PaddleCheckoutAvailable = false,
                PaddleWebhooksAvailable = false,
                MobileStoreEntitlementBridgeAvailable = false
            },
            CheckedAtUtc = DateTimeOffset.UtcNow
        };
    }
}
