namespace EnglishVoiceTutor.Api.Contracts.Admin;

public sealed class AdminRbacCutoverStatusResponse
{
    public bool BootstrapAdminFallbackForAdminPermissionPoliciesEnabled { get; init; }
    public bool BootstrapAdminFallbackDefaultEnabled { get; init; }
    public string BootstrapAdminFallbackConfigurationKey { get; init; } = string.Empty;
    public bool BootstrapAdminFallbackConfigurationValuePresent { get; init; }
    public bool PersistentRoleAuthorizationEnabled { get; init; }
    public string AdminPermissionAuthorizationMode { get; init; } = string.Empty;
    public DateTimeOffset GeneratedAtUtc { get; init; }
}
