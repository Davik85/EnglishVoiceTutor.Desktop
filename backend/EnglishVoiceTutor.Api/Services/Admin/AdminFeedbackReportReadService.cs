using EnglishVoiceTutor.Api.Contracts.Admin;
using EnglishVoiceTutor.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace EnglishVoiceTutor.Api.Services.Admin;

public sealed class AdminFeedbackReportReadService(AppDbContext dbContext) : IAdminFeedbackReportReadService
{
    public const int MessagePreviewMaxLength = 200;

    public async Task<AdminFeedbackReportListResponse> ListAsync(
        string? status,
        string? category,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var reports = dbContext.UserFeedbackReports.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(status))
        {
            reports = reports.Where(report => report.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(category))
        {
            reports = reports.Where(report => report.Category == category);
        }

        var totalCount = await reports.CountAsync(cancellationToken);
        var items = await reports
            .OrderByDescending(report => report.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(report => new AdminFeedbackReportListItem
            {
                ReportId = report.Id,
                Category = report.Category,
                Status = report.Status,
                MessagePreview = report.Message.Length <= MessagePreviewMaxLength
                    ? report.Message
                    : report.Message.Substring(0, MessagePreviewMaxLength),
                HasReportedAiText = report.ReportedAiText != null && report.ReportedAiText != string.Empty,
                CreatedAtUtc = report.CreatedAtUtc,
                ClientPlatform = report.ClientPlatform,
                ClientVersion = report.ClientVersion,
                UserEmail = report.User.Email,
                UserDisplayName = report.User.Profile == null ? null : report.User.Profile.DisplayName
            })
            .ToListAsync(cancellationToken);

        return new AdminFeedbackReportListResponse
        {
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            Items = items
        };
    }

    public Task<AdminFeedbackReportDetailsResponse?> GetByIdAsync(Guid reportId, CancellationToken cancellationToken)
    {
        return dbContext.UserFeedbackReports
            .AsNoTracking()
            .Where(report => report.Id == reportId)
            .Select(report => new AdminFeedbackReportDetailsResponse
            {
                ReportId = report.Id,
                Category = report.Category,
                Status = report.Status,
                Message = report.Message,
                ReportedAiText = report.ReportedAiText,
                CreatedAtUtc = report.CreatedAtUtc,
                ReviewedAtUtc = report.ReviewedAtUtc,
                ClientPlatform = report.ClientPlatform,
                ClientVersion = report.ClientVersion,
                User = new AdminFeedbackReportUser
                {
                    UserId = report.UserId,
                    Email = report.User.Email,
                    DisplayName = report.User.Profile == null ? null : report.User.Profile.DisplayName
                },
                Replies = report.Replies
                    .OrderByDescending(reply => reply.CreatedAtUtc)
                    .Select(reply => new AdminFeedbackReportReplyHistoryItem
                    {
                        ReplyId = reply.Id,
                        ReplyText = reply.ReplyText,
                        RecipientEmail = reply.RecipientEmail,
                        DeliveryStatus = reply.DeliveryStatus,
                        CreatedAtUtc = reply.CreatedAtUtc,
                        SentAtUtc = reply.SentAtUtc,
                        FailureCode = reply.FailureCode
                    })
                    .ToList()
            })
            .SingleOrDefaultAsync(cancellationToken);
    }
}
