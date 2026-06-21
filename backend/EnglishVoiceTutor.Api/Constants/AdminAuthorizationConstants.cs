namespace EnglishVoiceTutor.Api.Constants;

public static class AdminAuthorizationConstants
{
    public const string BootstrapAdminPolicyName = "BootstrapAdmin";
    public const string AdminSelfReadPermissionPolicyName = "AdminPermission:admin.self.read";
    public const string AdminCapabilitiesReadPermissionPolicyName = "AdminPermission:admin.capabilities.read";
    public const string ProductStatisticsReadPermissionPolicyName = "AdminPermission:product_statistics.read";
    public const string CmsRuntimeStatusReadPermissionPolicyName = "AdminPermission:cms.runtime_status.read";
    public const string CmsContentReadPermissionPolicyName = "AdminPermission:cms.content.read";
    public const string CmsDraftSavePermissionPolicyName = "AdminPermission:cms.content.write_draft";
    public const string CmsPublishPermissionPolicyName = "AdminPermission:cms.content.publish";
    public const string CmsRestorePermissionPolicyName = "AdminPermission:cms.content.restore";
    public const string UserLookupPermissionPolicyName = "AdminPermission:users.lookup.read";
    public const string UserOverviewPermissionPolicyName = "AdminPermission:users.overview.read";
    public const string LessonHistoryDiagnosticsPermissionPolicyName = "AdminPermission:lesson_history.diagnostics.read";
    public const string PremiumDiagnosticsPermissionPolicyName = "AdminPermission:premium.diagnostics.read";
    public const string ManualPremiumGrantPermissionPolicyName = "AdminPermission:premium.grant";
    public const string ManualPremiumRevokePermissionPolicyName = "AdminPermission:premium.revoke";
    public const string FreeLessonResetPermissionPolicyName = "AdminPermission:free_lesson_allowance.reset";
    public const string BillingCancelRenewalPermissionPolicyName = "AdminPermission:billing.cancel_renewal";
    public const string BillingEventDiagnosticsPermissionPolicyName = "AdminPermission:billing.diagnostics.read";
    public const string AuditLogViewPermissionPolicyName = "AdminPermission:audit.read";
    public const string SystemDiagnosticsPermissionPolicyName = "AdminPermission:system.diagnostics.read";
    public const string AdminRoleManagementPermissionPolicyName = "AdminPermission:admin.roles.manage";
    public const string AdminCookieAuthenticationScheme = "AdminShellCookie";
    public const string AdminCookieName = "evt_admin_session";
    public const string BootstrapAdminSource = "development_config_bootstrap";
    public const string AdminUserLookupSource = "admin_user_lookup";
    public const string AdminAuthorizationConfigurationSection = "AdminAuthorization";
    public const string EnableBootstrapAdminFallbackForAdminPermissionPoliciesConfigurationKey = "EnableBootstrapAdminFallbackForAdminPermissionPolicies";
    public const string EnableBootstrapAdminFallbackForAdminPermissionPoliciesConfigurationPath = AdminAuthorizationConfigurationSection + ":" + EnableBootstrapAdminFallbackForAdminPermissionPoliciesConfigurationKey;
}
