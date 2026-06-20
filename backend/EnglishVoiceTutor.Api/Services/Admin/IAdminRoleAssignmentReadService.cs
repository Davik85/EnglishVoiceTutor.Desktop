namespace EnglishVoiceTutor.Api.Services.Admin;

public interface IAdminRoleAssignmentReadService
{
    Task<AdminRoleAssignmentReadResult> GetEffectiveRolesByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<AdminRoleAssignmentReadResult> GetEffectiveRolesByNormalizedEmailAsync(
        string normalizedEmail,
        CancellationToken cancellationToken = default);
}
