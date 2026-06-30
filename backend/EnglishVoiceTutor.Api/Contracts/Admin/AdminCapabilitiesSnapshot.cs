namespace EnglishVoiceTutor.Api.Contracts.Admin;

public sealed class AdminCapabilitiesSnapshot
{
    public bool AdminSelfCheck { get; init; }
    public bool UserLookupByEmail { get; init; }
    public bool UserDiagnostics { get; init; }
    public bool AuditLogRead { get; init; }
    public bool ManualPremiumGrant { get; init; }
    public bool ManualPremiumRevoke { get; init; }
    public bool FreeLessonAllowanceReset { get; init; }
    public bool LocalSmokeTestScript { get; init; }
    public bool CmsUiAvailable { get; init; }
    public bool ProductionRolesAvailable { get; init; }
    public bool BillingProviderConfigured { get; init; }
    public bool PaddleCheckoutAvailable { get; init; }
    public bool PaddleWebhooksAvailable { get; init; }
    public bool PaddleLiveConfigured { get; init; }
    public bool PaddleCheckoutUrlConfigured { get; init; }
    public bool PaddleLivePriceConfigured { get; init; }
    public bool PaddleLiveProductConfigured { get; init; }
    public bool PaddleExpectedCustomDataConfigured { get; init; }
    public bool PaddlePublicCheckoutPageConfigured { get; init; }
    public bool BillingLivePaymentTestComplete { get; init; }
    public bool BillingPaidLaunchReleaseComplete { get; init; }
    public bool MobileStoreEntitlementBridgeAvailable { get; init; }
}
