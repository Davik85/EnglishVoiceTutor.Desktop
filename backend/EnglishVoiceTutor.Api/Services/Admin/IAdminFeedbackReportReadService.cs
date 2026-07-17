using EnglishVoiceTutor.Api.Contracts.Admin;

namespace EnglishVoiceTutor.Api.Services.Admin;

public interface IAdminFeedbackReportReadService
{
    Task<AdminFeedbackReportListResponse> ListAsync(
        string? status,
        string? category,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<AdminFeedbackReportDetailsResponse?> GetByIdAsync(Guid reportId, CancellationToken cancellationToken);
}
