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
        AdminPermissionConstants.UsersDiagnosticsRead,
        AdminPermissionConstants.AuditRead,
        AdminPermissionConstants.CmsContentRead,
        AdminPermissionConstants.CmsContentWriteDraft,
        AdminPermissionConstants.CmsContentPublish,
        AdminPermissionConstants.CmsContentRestore,
        AdminPermissionConstants.CmsRuntimeStatusRead,
        AdminPermissionConstants.SubscriptionsDiagnosticsRead,
        AdminPermissionConstants.PremiumGrant,
        AdminPermissionConstants.PremiumRevoke,
        AdminPermissionConstants.FreeLessonAllowanceReset,
        AdminPermissionConstants.BillingDiagnosticsRead,
        AdminPermissionConstants.ProductStatisticsRead
    ];

    public IReadOnlyList<string> GetBootstrapAdminRoles() => BootstrapAdminRoles;

    public IReadOnlyList<string> GetBootstrapAdminPermissions() => BootstrapAdminPermissions;
}
