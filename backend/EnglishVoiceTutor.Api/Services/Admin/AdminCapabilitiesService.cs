using EnglishVoiceTutor.Api.Constants;
using EnglishVoiceTutor.Api.Contracts.Admin;
using EnglishVoiceTutor.Api.Options;
using Microsoft.Extensions.Options;

namespace EnglishVoiceTutor.Api.Services.Admin;

public sealed class AdminCapabilitiesService(
    IWebHostEnvironment webHostEnvironment,
    IAdminRolePermissionCatalogService adminRolePermissionCatalogService,
    IOptions<BillingOptions> billingOptionsAccessor,
    IOptions<PaddleBillingOptions> paddleBillingOptionsAccessor,
    IOptions<PaddleWebhookOptions> paddleWebhookOptionsAccessor,
    IConfiguration configuration) : IAdminCapabilitiesService
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

        var billingOptions = billingOptionsAccessor.Value;
        var paddleBillingOptions = paddleBillingOptionsAccessor.Value;
        var paddleWebhookOptions = paddleWebhookOptionsAccessor.Value;
        var billingProviderConfigured = IsPaddleBillingProviderConfigured(billingOptions);
        var paddleLiveConfigured = IsLivePaddleEnvironment(paddleBillingOptions.Environment);
        var paddleCheckoutUrlConfigured = IsHttpUrlConfigured(paddleBillingOptions.CheckoutUrl);
        var paddleLivePriceConfigured = IsConfigured(ResolvePremiumPriceId(paddleBillingOptions));
        var paddleLiveProductConfigured = IsConfigured(ResolvePremiumProductId(paddleBillingOptions));
        var paddleExpectedCustomDataConfigured = IsConfigured(paddleBillingOptions.ExpectedCustomDataApp)
            && IsConfigured(paddleBillingOptions.ExpectedCustomDataProduct);
        var paddleCheckoutAvailable = billingProviderConfigured
            && paddleBillingOptions.CheckoutAdapterEnabled
            && paddleLiveConfigured
            && IsConfigured(paddleBillingOptions.ApiKey)
            && paddleLivePriceConfigured
            && paddleLiveProductConfigured
            && paddleCheckoutUrlConfigured
            && paddleExpectedCustomDataConfigured;
        var paddleWebhooksAvailable = paddleWebhookOptions.Enabled
            && IsConfigured(paddleWebhookOptions.SecretKey)
            && paddleWebhookOptions.TimestampToleranceSeconds > 0;
        var productionRolesAvailable = AdminRbacCutoverStatusReader.GetStatus(configuration) is
        {
            PersistentRoleAuthorizationEnabled: true,
            BootstrapAdminFallbackForAdminPermissionPoliciesEnabled: false,
            BootstrapAdminFallbackConfigurationValuePresent: true
        };

        return new AdminCapabilitiesResponse
        {
            AdminSource = AdminAuthorizationConstants.BootstrapAdminSource,
            Environment = webHostEnvironment.EnvironmentName,
            Roles = adminRolePermissionCatalogService.GetBootstrapAdminRoles(),
            Permissions = adminRolePermissionCatalogService.GetBootstrapAdminPermissions(),
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
                ProductionRolesAvailable = productionRolesAvailable,
                BillingProviderConfigured = billingProviderConfigured,
                PaddleCheckoutAvailable = paddleCheckoutAvailable,
                PaddleWebhooksAvailable = paddleWebhooksAvailable,
                PaddleLiveConfigured = paddleLiveConfigured,
                PaddleCheckoutUrlConfigured = paddleCheckoutUrlConfigured,
                PaddleLivePriceConfigured = paddleLivePriceConfigured,
                PaddleLiveProductConfigured = paddleLiveProductConfigured,
                PaddleExpectedCustomDataConfigured = paddleExpectedCustomDataConfigured,
                PaddlePublicCheckoutPageConfigured = paddleCheckoutUrlConfigured,
                BillingLivePaymentTestComplete = false,
                BillingPaidLaunchReleaseComplete = false,
                MobileStoreEntitlementBridgeAvailable = false
            },
            CheckedAtUtc = DateTimeOffset.UtcNow
        };
    }

    private static bool IsPaddleBillingProviderConfigured(BillingOptions options)
    {
        return options.CheckoutEnabled
            && string.Equals(options.Provider, SubscriptionConstants.BillingProviders.Paddle, StringComparison.OrdinalIgnoreCase);
    }

    private static string ResolvePremiumPriceId(PaddleBillingOptions options)
    {
        return IsLivePaddleEnvironment(options.Environment) && IsConfigured(options.PremiumLivePriceId)
            ? options.PremiumLivePriceId
            : options.PremiumPriceId;
    }

    private static string ResolvePremiumProductId(PaddleBillingOptions options)
    {
        return IsLivePaddleEnvironment(options.Environment) && IsConfigured(options.PremiumLiveProductId)
            ? options.PremiumLiveProductId
            : options.PremiumProductId;
    }

    private static bool IsLivePaddleEnvironment(string environment)
    {
        return string.Equals(environment?.Trim(), SubscriptionConstants.Billing.LivePaddleEnvironment, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsHttpUrlConfigured(string value)
    {
        return Uri.TryCreate(value, UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp);
    }

    private static bool IsConfigured(string? value)
    {
        return !string.IsNullOrWhiteSpace(value);
    }
}
