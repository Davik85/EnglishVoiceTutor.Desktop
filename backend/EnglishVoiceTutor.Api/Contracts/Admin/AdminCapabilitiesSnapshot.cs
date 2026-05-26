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
    public bool MobileStoreEntitlementBridgeAvailable { get; init; }
}
