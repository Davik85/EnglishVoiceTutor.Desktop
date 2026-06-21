namespace EnglishVoiceTutor.Api.Services.Admin;

public interface IAdminRoleAssignmentBootstrapService
{
    Task<AdminRoleAssignmentBootstrapResult> BootstrapFirstOwnerAsync(
        AdminRoleAssignmentBootstrapRequest request,
        CancellationToken cancellationToken = default);
}
