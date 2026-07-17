using System.Net.Mail;
using System.Text.Json;
using EnglishVoiceTutor.Api.Constants;
using EnglishVoiceTutor.Api.Contracts.Admin;
using EnglishVoiceTutor.Api.Data;
using EnglishVoiceTutor.Api.Data.Entities;
using EnglishVoiceTutor.Api.Services.Email;
using Microsoft.EntityFrameworkCore;

namespace EnglishVoiceTutor.Api.Services.Admin;

public sealed class AdminFeedbackReportReplyService(
    AppDbContext dbContext,
    IEmailSender emailSender,
    IAdminAuditService adminAuditService) : IAdminFeedbackReportReplyService
{
    private const string Subject = "Language Voice Tutor support";
    private const string ReplySentReason = "Feedback report reply sent.";
    private const string ReplyFailedReason = "Feedback report reply failed.";
    private const string StatusChangedReason = "Feedback report status changed after reply delivery.";
    private const string EmailNotConfigured = "email_not_configured";
    private const string EmailDeliveryFailed = "email_delivery_failed";

    public async Task<AdminFeedbackReportReplyResult> SendAsync(
        Guid adminUserId,
        Guid reportId,
        string? replyText,
        CancellationToken cancellationToken)
    {
        var normalizedReplyText = replyText?.Trim() ?? string.Empty;
        if (normalizedReplyText.Length == 0 || normalizedReplyText.Length > EntityConstants.Lengths.FeedbackReportMessageMaxLength)
        {
            return AdminFeedbackReportReplyResult.Invalid();
        }

        var adminUser = await dbContext.AdminUsers
            .AsNoTracking()
            .SingleOrDefaultAsync(admin => admin.Id == adminUserId && admin.DisabledAtUtc == null && admin.Status == "active", cancellationToken);
        if (adminUser?.UserId is null)
        {
            return AdminFeedbackReportReplyResult.ActorUnavailable();
        }

        var report = await dbContext.UserFeedbackReports
            .Include(candidate => candidate.User)
            .ThenInclude(user => user.Profile)
            .SingleOrDefaultAsync(candidate => candidate.Id == reportId, cancellationToken);
        if (report is null)
        {
            return AdminFeedbackReportReplyResult.NotFound();
        }

        var recipientEmail = report.User.Email?.Trim() ?? string.Empty;
        if (!IsValidRecipientEmail(recipientEmail))
        {
            return AdminFeedbackReportReplyResult.RecipientUnavailable();
        }

        var now = DateTimeOffset.UtcNow;
        var attempt = new UserFeedbackReportReplyEntity
        {
            Id = Guid.NewGuid(),
            FeedbackReportId = report.Id,
            AdminUserId = adminUser.Id,
            ReplyText = normalizedReplyText,
            RecipientEmail = recipientEmail,
            DeliveryStatus = UserFeedbackReportReplyConstants.DeliveryStatuses.Pending,
            CreatedAtUtc = now
        };
        dbContext.UserFeedbackReportReplies.Add(attempt);
        await dbContext.SaveChangesAsync(cancellationToken);

        if (!emailSender.IsConfigured)
        {
            return await MarkFailedAsync(report, attempt, adminUser.UserId.Value, EmailNotConfigured, "Email delivery is not configured.", cancellationToken);
        }

        try
        {
            await emailSender.SendAsync(
                new EmailMessage(recipientEmail, Subject, BuildPlainTextBody(report.User.Profile?.DisplayName, normalizedReplyText)),
                cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception)
        {
            return await MarkFailedAsync(report, attempt, adminUser.UserId.Value, EmailDeliveryFailed, "Email delivery failed.", cancellationToken);
        }

        var previousStatus = report.Status;
        attempt.DeliveryStatus = UserFeedbackReportReplyConstants.DeliveryStatuses.Sent;
        attempt.SentAtUtc = DateTimeOffset.UtcNow;
        attempt.FailureCode = null;
        attempt.FailureMessage = null;
        if (string.Equals(report.Status, "new", StringComparison.Ordinal))
        {
            report.Status = "reviewed";
            report.ReviewedAtUtc ??= attempt.SentAtUtc;
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        await adminAuditService.RecordTargetUserActionAsync(
            adminUser.UserId.Value, report.UserId, AdminAuditConstants.ActionTypes.FeedbackReportReplySent, ReplySentReason,
            SerializeMetadata(report, attempt, null), cancellationToken);
        if (!string.Equals(previousStatus, report.Status, StringComparison.Ordinal))
        {
            await adminAuditService.RecordTargetUserActionAsync(
                adminUser.UserId.Value, report.UserId, AdminAuditConstants.ActionTypes.FeedbackReportStatusChanged, StatusChangedReason,
                JsonSerializer.Serialize(new Dictionary<string, object?>
                {
                    ["feedbackReportId"] = report.Id,
                    ["previousStatus"] = previousStatus,
                    ["newStatus"] = report.Status,
                    ["category"] = report.Category
                }), cancellationToken);
        }
        await transaction.CommitAsync(cancellationToken);

        return AdminFeedbackReportReplyResult.Success(ToResponse(report, attempt));
    }

    private async Task<AdminFeedbackReportReplyResult> MarkFailedAsync(
        UserFeedbackReportEntity report,
        UserFeedbackReportReplyEntity attempt,
        Guid auditActorUserId,
        string failureCode,
        string failureMessage,
        CancellationToken cancellationToken)
    {
        attempt.DeliveryStatus = UserFeedbackReportReplyConstants.DeliveryStatuses.Failed;
        attempt.FailureCode = failureCode;
        attempt.FailureMessage = failureMessage;
        await dbContext.SaveChangesAsync(cancellationToken);
        await adminAuditService.RecordTargetUserActionAsync(
            auditActorUserId, report.UserId, AdminAuditConstants.ActionTypes.FeedbackReportReplyFailed, ReplyFailedReason,
            SerializeMetadata(report, attempt, failureCode), cancellationToken);
        return AdminFeedbackReportReplyResult.DeliveryFailed(ToResponse(report, attempt));
    }

    private static AdminFeedbackReportReplyResponse ToResponse(UserFeedbackReportEntity report, UserFeedbackReportReplyEntity attempt) => new()
    {
        ReplyId = attempt.Id, FeedbackReportId = report.Id, DeliveryStatus = attempt.DeliveryStatus,
        CreatedAtUtc = attempt.CreatedAtUtc, SentAtUtc = attempt.SentAtUtc, ReportStatus = report.Status,
        ReviewedAtUtc = report.ReviewedAtUtc, FailureCode = attempt.FailureCode
    };

    private static string SerializeMetadata(UserFeedbackReportEntity report, UserFeedbackReportReplyEntity attempt, string? failureCode)
    {
        var metadata = new Dictionary<string, object?>
        {
            ["feedbackReportId"] = report.Id, ["replyId"] = attempt.Id, ["category"] = report.Category,
            ["deliveryStatus"] = attempt.DeliveryStatus
        };
        if (failureCode is not null) metadata["failureCode"] = failureCode;
        return JsonSerializer.Serialize(metadata);
    }

    private static string BuildPlainTextBody(string? displayName, string replyText)
    {
        var normalizedName = displayName?.Trim();
        var greeting = !string.IsNullOrWhiteSpace(normalizedName) && normalizedName.Length <= EntityConstants.Lengths.DisplayNameMaxLength && !normalizedName.Contains('\r') && !normalizedName.Contains('\n')
            ? $"Hello {normalizedName},"
            : "Hello,";
        return $"{greeting}\n\n{replyText}\n\nLanguage Voice Tutor\nhttps://languagevoicetutor.com/\n\nLanguage Voice Tutor Support";
    }

    private static bool IsValidRecipientEmail(string email)
    {
        if (email.Length == 0 || email.Length > EntityConstants.Lengths.EmailMaxLength) return false;
        try { _ = new MailAddress(email); return true; }
        catch (FormatException) { return false; }
    }
}

public sealed class AdminFeedbackReportReplyResult
{
    public bool IsInvalid { get; private init; }
    public bool IsNotFound { get; private init; }
    public bool IsActorUnavailable { get; private init; }
    public bool IsRecipientUnavailable { get; private init; }
    public bool IsDeliveryFailed { get; private init; }
    public AdminFeedbackReportReplyResponse? Response { get; private init; }
    public static AdminFeedbackReportReplyResult Invalid() => new() { IsInvalid = true };
    public static AdminFeedbackReportReplyResult NotFound() => new() { IsNotFound = true };
    public static AdminFeedbackReportReplyResult ActorUnavailable() => new() { IsActorUnavailable = true };
    public static AdminFeedbackReportReplyResult RecipientUnavailable() => new() { IsRecipientUnavailable = true };
    public static AdminFeedbackReportReplyResult DeliveryFailed(AdminFeedbackReportReplyResponse response) => new() { IsDeliveryFailed = true, Response = response };
    public static AdminFeedbackReportReplyResult Success(AdminFeedbackReportReplyResponse response) => new() { Response = response };
}
