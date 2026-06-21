namespace EnglishVoiceTutor.Api.Services.Admin;

public interface IAdminRoleAssignmentAuditService
{
    Task<AdminRoleAssignmentAuditResult> AppendAuditEventAsync(
        AdminRoleAssignmentAuditRequest request,
        CancellationToken cancellationToken = default);
}
