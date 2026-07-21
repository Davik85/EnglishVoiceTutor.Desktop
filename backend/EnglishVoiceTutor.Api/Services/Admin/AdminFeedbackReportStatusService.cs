using System.Text.Json;
using EnglishVoiceTutor.Api.Constants;
using EnglishVoiceTutor.Api.Contracts.Admin;
using EnglishVoiceTutor.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace EnglishVoiceTutor.Api.Services.Admin;

public sealed class AdminFeedbackReportStatusService(
    AppDbContext dbContext,
    IAdminAuditService adminAuditService) : IAdminFeedbackReportStatusService
{
    private const string AuditReason = "Feedback report status changed.";

    public async Task<AdminFeedbackReportStatusChangeResult> ChangeStatusAsync(
        Guid adminUserId,
        Guid reportId,
        string? requestedStatus,
        CancellationToken cancellationToken)
    {
        var status = NormalizeStatus(requestedStatus);
        if (status is null || !UserFeedbackReportConstants.Statuses.Contains(status)
            || status == UserFeedbackReportConstants.NewStatus)
        {
            return AdminFeedbackReportStatusChangeResult.Invalid();
        }

        var report = await dbContext.UserFeedbackReports
            .SingleOrDefaultAsync(candidate => candidate.Id == reportId, cancellationToken);
        if (report is null)
        {
            return AdminFeedbackReportStatusChangeResult.NotFound();
        }

        if (string.Equals(report.Status, status, StringComparison.Ordinal))
        {
            return AdminFeedbackReportStatusChangeResult.Success(ToResponse(report));
        }

        var previousStatus = report.Status;
        var now = DateTimeOffset.UtcNow;
        report.Status = status;
        if (report.ReviewedAtUtc is null)
        {
            report.ReviewedAtUtc = now;
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        var safeMetadataJson = JsonSerializer.Serialize(new Dictionary<string, object?>
        {
            ["feedbackReportId"] = report.Id,
            ["previousStatus"] = previousStatus,
            ["newStatus"] = report.Status,
            ["category"] = report.Category
        });
        await adminAuditService.RecordTargetUserActionAsync(
            adminUserId,
            report.UserId,
            AdminAuditConstants.ActionTypes.FeedbackReportStatusChanged,
            AuditReason,
            safeMetadataJson,
            cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return AdminFeedbackReportStatusChangeResult.Success(ToResponse(report));
    }

    private static AdminFeedbackReportStatusChangeResponse ToResponse(EnglishVoiceTutor.Api.Data.Entities.UserFeedbackReportEntity report) => new()
    {
        ReportId = report.Id,
        Status = report.Status,
        ReviewedAtUtc = report.ReviewedAtUtc
    };

    private static string? NormalizeStatus(string? status) =>
        string.IsNullOrWhiteSpace(status) ? null : status.Trim().ToLowerInvariant();
}

public sealed class AdminFeedbackReportStatusChangeResult
{
    public bool IsInvalid { get; private init; }
    public bool IsNotFound { get; private init; }
    public AdminFeedbackReportStatusChangeResponse? Response { get; private init; }

    public static AdminFeedbackReportStatusChangeResult Invalid() => new() { IsInvalid = true };
    public static AdminFeedbackReportStatusChangeResult NotFound() => new() { IsNotFound = true };
    public static AdminFeedbackReportStatusChangeResult Success(AdminFeedbackReportStatusChangeResponse response) => new() { Response = response };
}
