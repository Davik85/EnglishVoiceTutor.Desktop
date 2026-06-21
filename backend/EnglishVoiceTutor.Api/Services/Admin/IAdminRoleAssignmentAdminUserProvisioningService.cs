namespace EnglishVoiceTutor.Api.Services.Admin;

public interface IAdminRoleAssignmentAdminUserProvisioningService
{
    Task<AdminRoleAssignmentAdminUserProvisioningResult> ProvisionAdminUserAsync(
        AdminRoleAssignmentAdminUserProvisioningRequest request,
        CancellationToken cancellationToken = default);
}
