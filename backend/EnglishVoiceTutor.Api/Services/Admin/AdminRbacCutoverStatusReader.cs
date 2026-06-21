using EnglishVoiceTutor.Api.Constants;
using EnglishVoiceTutor.Api.Contracts.Admin;

namespace EnglishVoiceTutor.Api.Services.Admin;

public static class AdminRbacCutoverStatusReader
{
    public const bool BootstrapAdminFallbackDefaultEnabled = true;

    public static AdminRbacCutoverStatusResponse GetStatus(IConfiguration configuration)
    {
        var configuredValue = configuration.GetValue<bool?>(
            AdminAuthorizationConstants.EnableBootstrapAdminFallbackForAdminPermissionPoliciesConfigurationPath);

        return new AdminRbacCutoverStatusResponse
        {
            BootstrapAdminFallbackForAdminPermissionPoliciesEnabled = configuredValue ?? BootstrapAdminFallbackDefaultEnabled,
            BootstrapAdminFallbackDefaultEnabled = BootstrapAdminFallbackDefaultEnabled,
            BootstrapAdminFallbackConfigurationKey = AdminAuthorizationConstants.EnableBootstrapAdminFallbackForAdminPermissionPoliciesConfigurationPath,
            BootstrapAdminFallbackConfigurationValuePresent = configuredValue.HasValue,
            PersistentRoleAuthorizationEnabled = true,
            AdminPermissionAuthorizationMode = (configuredValue ?? BootstrapAdminFallbackDefaultEnabled)
                ? "persistent_roles_with_bootstrap_admin_fallback"
                : "persistent_roles_only",
            GeneratedAtUtc = DateTimeOffset.UtcNow
        };
    }

    public static bool IsBootstrapAdminFallbackEnabled(IConfiguration configuration)
    {
        return configuration.GetValue<bool?>(
            AdminAuthorizationConstants.EnableBootstrapAdminFallbackForAdminPermissionPoliciesConfigurationPath)
            ?? BootstrapAdminFallbackDefaultEnabled;
    }
}
