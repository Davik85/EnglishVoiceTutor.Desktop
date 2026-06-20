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
        AdminPermissionConstants.AdminRolesManage
    ];

    public IReadOnlyList<string> GetBootstrapAdminRoles() => BootstrapAdminRoles;

    public IReadOnlyList<string> GetBootstrapAdminPermissions() => BootstrapAdminPermissions;
}
