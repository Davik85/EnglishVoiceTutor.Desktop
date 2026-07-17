using EnglishVoiceTutor.Api.Contracts.Admin;

namespace EnglishVoiceTutor.Api.Services.Admin;

public interface IAdminFeedbackReportStatusService
{
    Task<AdminFeedbackReportStatusChangeResult> ChangeStatusAsync(
        Guid adminUserId,
        Guid reportId,
        string? requestedStatus,
        CancellationToken cancellationToken);
}
