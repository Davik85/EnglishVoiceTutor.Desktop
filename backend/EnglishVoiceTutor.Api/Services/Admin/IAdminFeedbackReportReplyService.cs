using EnglishVoiceTutor.Api.Contracts.Admin;

namespace EnglishVoiceTutor.Api.Services.Admin;

public interface IAdminFeedbackReportReplyService
{
    Task<AdminFeedbackReportReplyResult> SendAsync(
        Guid adminUserId,
        Guid reportId,
        string? replyText,
        CancellationToken cancellationToken);
}
