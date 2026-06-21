namespace EnglishVoiceTutor.Api.Services.Admin;

public interface IAdminRoleAssignmentSafetyService
{
    Task<AdminRoleAssignmentSafetyCheckResult> ValidateAssignRoleAsync(
        AdminRoleAssignmentSafetyCheckRequest request,
        CancellationToken cancellationToken = default);

    Task<AdminRoleAssignmentSafetyCheckResult> ValidateRevokeRoleAsync(
        AdminRoleAssignmentSafetyCheckRequest request,
        CancellationToken cancellationToken = default);

    Task<AdminRoleAssignmentSafetyCheckResult> ValidateDisableAdminAsync(
        AdminRoleAssignmentSafetyCheckRequest request,
        CancellationToken cancellationToken = default);
}
