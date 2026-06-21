namespace EnglishVoiceTutor.Api.Services.Admin;

public interface IAdminRoleAssignmentWriteService
{
    Task<AdminRoleAssignmentWriteResult> AssignRoleAsync(
        AdminRoleAssignmentWriteRequest request,
        CancellationToken cancellationToken = default);

    Task<AdminRoleAssignmentWriteResult> RevokeRoleAsync(
        AdminRoleAssignmentWriteRequest request,
        CancellationToken cancellationToken = default);

    Task<AdminRoleAssignmentWriteResult> DisableAdminAsync(
        AdminRoleAssignmentWriteRequest request,
        CancellationToken cancellationToken = default);
}
