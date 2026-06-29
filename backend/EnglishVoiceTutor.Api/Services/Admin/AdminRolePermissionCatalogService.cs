using EnglishVoiceTutor.Api.Constants;

namespace EnglishVoiceTutor.Api.Services.Admin;

public sealed class AdminRolePermissionCatalogService : IAdminRolePermissionCatalogService
{
    private static readonly string[] BootstrapAdminRoles =
    [
        AdminRoleConstants.SuperAdmin
    ];

    private static readonly string[] BootstrapAdminPermissions =
    [
        AdminPermissionConstants.AdminSelfRead,
        AdminPermissionConstants.AdminCapabilitiesRead,
        AdminPermissionConstants.UsersRead,
        AdminPermissionConstants.UserLookupRead,
        AdminPermissionConstants.UserOverviewRead,
        AdminPermissionConstants.UsersDiagnosticsRead,
        AdminPermissionConstants.LessonHistoryDiagnosticsRead,
        AdminPermissionConstants.AuditRead,
        AdminPermissionConstants.CmsContentRead,
        AdminPermissionConstants.CmsContentWriteDraft,
        AdminPermissionConstants.CmsContentPublish,
        AdminPermissionConstants.CmsContentRestore,
        AdminPermissionConstants.CmsRuntimeStatusRead,
        AdminPermissionConstants.SubscriptionsDiagnosticsRead,
        AdminPermissionConstants.PremiumDiagnosticsRead,
        AdminPermissionConstants.PremiumGrant,
        AdminPermissionConstants.PremiumRevoke,
        AdminPermissionConstants.FreeLessonAllowanceReset,
        AdminPermissionConstants.BillingCancelRenewal,
        AdminPermissionConstants.BillingDiagnosticsRead,
        AdminPermissionConstants.ProductStatisticsRead,
        AdminPermissionConstants.SystemDiagnosticsRead,
        AdminPermissionConstants.SystemAiModelSettingsManage,
        AdminPermissionConstants.AdminRolesManage
    ];

    private static readonly IReadOnlyDictionary<string, IReadOnlyList<string>> ProductionRolePermissions =
        new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal)
        {
            [AdminRoleConstants.SuperAdmin] = BootstrapAdminPermissions,
            [AdminRoleConstants.Support] =
            [
                AdminPermissionConstants.AdminSelfRead,
                AdminPermissionConstants.AdminCapabilitiesRead,
                AdminPermissionConstants.UsersRead,
                AdminPermissionConstants.UserLookupRead,
                AdminPermissionConstants.UserOverviewRead,
                AdminPermissionConstants.UsersDiagnosticsRead,
                AdminPermissionConstants.LessonHistoryDiagnosticsRead,
                AdminPermissionConstants.FreeLessonAllowanceReset,
                AdminPermissionConstants.SystemDiagnosticsRead
            ],
            [AdminRoleConstants.ContentEditor] =
            [
                AdminPermissionConstants.AdminSelfRead,
                AdminPermissionConstants.AdminCapabilitiesRead,
                AdminPermissionConstants.CmsContentRead,
                AdminPermissionConstants.CmsContentWriteDraft,
                AdminPermissionConstants.CmsRuntimeStatusRead
            ],
            [AdminRoleConstants.BillingSupport] =
            [
                AdminPermissionConstants.AdminSelfRead,
                AdminPermissionConstants.AdminCapabilitiesRead,
                AdminPermissionConstants.UserLookupRead,
                AdminPermissionConstants.UserOverviewRead,
                AdminPermissionConstants.SubscriptionsDiagnosticsRead,
                AdminPermissionConstants.PremiumDiagnosticsRead,
                AdminPermissionConstants.BillingDiagnosticsRead,
                AdminPermissionConstants.BillingCancelRenewal
            ],
            [AdminRoleConstants.ReadOnlyAuditor] =
            [
                AdminPermissionConstants.AdminSelfRead,
                AdminPermissionConstants.AdminCapabilitiesRead,
                AdminPermissionConstants.AuditRead,
                AdminPermissionConstants.UsersDiagnosticsRead,
                AdminPermissionConstants.LessonHistoryDiagnosticsRead,
                AdminPermissionConstants.SubscriptionsDiagnosticsRead,
                AdminPermissionConstants.PremiumDiagnosticsRead,
                AdminPermissionConstants.BillingDiagnosticsRead,
                AdminPermissionConstants.ProductStatisticsRead,
                AdminPermissionConstants.SystemDiagnosticsRead
            ]
        };

    public IReadOnlyList<string> GetBootstrapAdminRoles() => BootstrapAdminRoles;

    public IReadOnlyList<string> GetBootstrapAdminPermissions() => BootstrapAdminPermissions;

    public IReadOnlyDictionary<string, IReadOnlyList<string>> GetProductionRolePermissions() => ProductionRolePermissions;
}
