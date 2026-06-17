namespace EnglishVoiceTutor.Api.Services.Admin;

public interface IAdminRolePermissionCatalogService
{
    IReadOnlyList<string> GetBootstrapAdminRoles();

    IReadOnlyList<string> GetBootstrapAdminPermissions();
}
