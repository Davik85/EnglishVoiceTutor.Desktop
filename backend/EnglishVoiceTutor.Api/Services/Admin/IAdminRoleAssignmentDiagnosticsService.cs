namespace EnglishVoiceTutor.Api.Services.Admin;

public interface IAdminRoleAssignmentDiagnosticsService
{
    Task<AdminRoleAssignmentDiagnosticsResult> GetDiagnosticsAsync(CancellationToken cancellationToken = default);
}
