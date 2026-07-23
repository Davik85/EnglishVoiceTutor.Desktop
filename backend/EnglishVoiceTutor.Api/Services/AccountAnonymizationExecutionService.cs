using System.Data;
using System.Text.Json;
using EnglishVoiceTutor.Api.Constants;
using EnglishVoiceTutor.Api.Contracts.Admin;
using EnglishVoiceTutor.Api.Data;
using EnglishVoiceTutor.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace EnglishVoiceTutor.Api.Services;

public interface IAccountAnonymizationExecutionService
{
    Task<AccountAnonymizationExecutionResult> ExecuteAsync(Guid actorAdminUserId, Guid reportId, AccountAnonymizationExecuteRequest request, CancellationToken cancellationToken);
}

public sealed class AccountAnonymizationExecutionService(AppDbContext dbContext) : IAccountAnonymizationExecutionService
{
    public const string ExecutingState = "executing";
    public const string CompletedState = "completed";

    public async Task<AccountAnonymizationExecutionResult> ExecuteAsync(Guid actorAdminUserId, Guid reportId, AccountAnonymizationExecuteRequest request, CancellationToken cancellationToken)
    {
        if (request.OperationId == Guid.Empty || string.IsNullOrWhiteSpace(request.PreflightFingerprint)) return AccountAnonymizationExecutionResult.InvalidRequest();
        await using var transaction = dbContext.Database.IsRelational()
            ? await dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken) : null;
        try
        {
            var report = await dbContext.UserFeedbackReports.Include(item => item.Replies).SingleOrDefaultAsync(item => item.Id == reportId, cancellationToken);
            if (report is null) return AccountAnonymizationExecutionResult.NotFound();
            if (report.Category != UserFeedbackReportConstants.AccountDeletionCategory) return AccountAnonymizationExecutionResult.WrongCategory();
            var operation = await dbContext.AccountAnonymizationOperations.SingleOrDefaultAsync(item => item.ReportId == reportId, cancellationToken);
            if (operation is null) return AccountAnonymizationExecutionResult.PreflightMissing();
            if (operation.Id != request.OperationId || !string.Equals(operation.PreflightFingerprint, request.PreflightFingerprint, StringComparison.Ordinal)) return AccountAnonymizationExecutionResult.OperationMismatch();
            if (operation.State == CompletedState) return AccountAnonymizationExecutionResult.Completed(ToResponse(operation));
            if (report.Status != UserFeedbackReportConstants.ProcessingStatus) return AccountAnonymizationExecutionResult.ReportNotProcessing();
            if (operation.State == ExecutingState) return AccountAnonymizationExecutionResult.Executing();
            if (operation.ExpiresAtUtc <= DateTimeOffset.UtcNow || operation.ProcedureVersion != AccountAnonymizationPreflightService.ProcedureVersion) return AccountAnonymizationExecutionResult.PreflightStale();
            if (DeserializeCodes(operation.BlockingCodesJson).Count != 0) return AccountAnonymizationExecutionResult.DependencyBlocked();

            var targetAdmin = await dbContext.AdminUsers.AnyAsync(item => item.UserId == report.UserId && item.Status == "active", cancellationToken);
            var actorUserId = await dbContext.AdminUsers.Where(item => item.Id == actorAdminUserId).Select(item => item.UserId).SingleOrDefaultAsync(cancellationToken);
            if (actorUserId == report.UserId) return AccountAnonymizationExecutionResult.SelfBlocked();
            if (targetAdmin) return AccountAnonymizationExecutionResult.AdminBlocked();
            if (await HasAdminOrCmsDependencyAsync(report.UserId, cancellationToken)) return AccountAnonymizationExecutionResult.DependencyBlocked();
            if (await HasActivePremiumAsync(report.UserId, cancellationToken)) return AccountAnonymizationExecutionResult.ActivePremium();

            operation.State = ExecutingState;
            operation.StartedAtUtc = DateTimeOffset.UtcNow;
            operation.FailureCode = null;
            operation.VerificationState = "in_progress";
            operation.ConcurrencyRevision++;
            await dbContext.SaveChangesAsync(cancellationToken);

            var counts = await RemoveLearnerDataAsync(report, operation, cancellationToken);
            await VerifyAsync(report.UserId, operation.Id, counts, cancellationToken);
            operation.State = CompletedState;
            operation.CompletedAtUtc = DateTimeOffset.UtcNow;
            operation.VerificationState = "verified";
            operation.ResultCountsJson = JsonSerializer.Serialize(counts);
            operation.UpdatedAtUtc = DateTimeOffset.UtcNow;
            operation.ConcurrencyRevision++;
            await dbContext.SaveChangesAsync(cancellationToken);
            if (transaction is not null) await transaction.CommitAsync(cancellationToken);
            return AccountAnonymizationExecutionResult.Completed(ToResponse(operation));
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            if (transaction is not null) await transaction.RollbackAsync(cancellationToken);
            dbContext.ChangeTracker.Clear();
            return AccountAnonymizationExecutionResult.Unavailable();
        }
    }

    private async Task<Dictionary<string, int>> RemoveLearnerDataAsync(UserFeedbackReportEntity report, AccountAnonymizationOperationEntity operation, CancellationToken cancellationToken)
    {
        var userId = report.UserId;
        var sessions = await dbContext.LessonSessions.Where(item => item.UserId == userId).ToListAsync(cancellationToken);
        var sessionIds = sessions.Select(item => item.Id).ToArray();
        var messages = await dbContext.LessonMessages.Where(item => sessionIds.Contains(item.SessionId)).ToListAsync(cancellationToken);
        var messageIds = messages.Select(item => item.Id).ToArray();
        var feedback = await dbContext.FeedbackResults.Where(item => sessionIds.Contains(item.SessionId) || messageIds.Contains(item.MessageId)).ToListAsync(cancellationToken);
        var summaries = await dbContext.LessonSummaries.Where(item => sessionIds.Contains(item.SessionId)).ToListAsync(cancellationToken);
        var reports = await dbContext.UserFeedbackReports.Include(item => item.Replies).Where(item => item.UserId == userId).ToListAsync(cancellationToken);
        var refresh = await dbContext.UserRefreshTokens.Where(item => item.UserId == userId).ToListAsync(cancellationToken);
        var reset = await dbContext.PasswordResetTokens.Where(item => item.UserId == userId).ToListAsync(cancellationToken);
        var profiles = await dbContext.UserProfiles.Where(item => item.UserId == userId).ToListAsync(cancellationToken);
        var settings = await dbContext.UserSettings.Where(item => item.UserId == userId).ToListAsync(cancellationToken);
        var devices = await dbContext.Devices.Where(item => item.UserId == userId).ToListAsync(cancellationToken);
        var usages = await dbContext.UsageEvents.Where(item => item.UserId == userId || (item.SessionId.HasValue && sessionIds.Contains(item.SessionId.Value))).ToListAsync(cancellationToken);
        var counters = await dbContext.DailyUsageCounters.Where(item => item.UserId == userId).ToListAsync(cancellationToken);
        var freeUsage = await dbContext.DailyFreeLessonUsages.Where(item => item.UserId == userId || sessionIds.Contains(item.LessonSessionId)).ToListAsync(cancellationToken);
        var trials = await dbContext.TrialGrants.Where(item => item.UserId == userId).ToListAsync(cancellationToken);
        var entitlements = await dbContext.Entitlements.Where(item => item.UserId == userId).ToListAsync(cancellationToken);
        dbContext.RemoveRange(feedback); dbContext.RemoveRange(summaries); dbContext.RemoveRange(messages); dbContext.RemoveRange(freeUsage); dbContext.RemoveRange(usages); dbContext.RemoveRange(sessions);
        dbContext.RemoveRange(refresh); dbContext.RemoveRange(reset); dbContext.RemoveRange(profiles); dbContext.RemoveRange(settings); dbContext.RemoveRange(devices); dbContext.RemoveRange(counters); dbContext.RemoveRange(trials); dbContext.RemoveRange(entitlements);
        foreach (var item in reports)
        {
            item.Message = "[redacted]"; item.ReportedAiText = null;
            foreach (var reply in item.Replies) { reply.ReplyText = "[redacted]"; reply.RecipientEmail = "redacted@deleted.invalid"; reply.FailureMessage = null; }
        }
        report.Status = UserFeedbackReportConstants.ResolvedStatus;
        var user = await dbContext.Users.SingleAsync(item => item.Id == userId, cancellationToken);
        user.Email = $"deleted+{operation.Id:N}@deleted.invalid";
        user.PasswordHash = "account-deleted-no-login";
        user.Status = "deleted";
        user.LastLoginAt = null;
        await dbContext.SaveChangesAsync(cancellationToken);
        return new Dictionary<string, int> { ["lesson_sessions"] = sessions.Count, ["lesson_messages"] = messages.Count, ["feedback_results"] = feedback.Count, ["tokens"] = refresh.Count + reset.Count, ["entitlements"] = entitlements.Count, ["reports_redacted"] = reports.Count };
    }

    private async Task VerifyAsync(Guid userId, Guid operationId, Dictionary<string, int> counts, CancellationToken cancellationToken)
    {
        var user = await dbContext.Users.AsNoTracking().SingleAsync(item => item.Id == userId, cancellationToken);
        var noAccess = user.Status == "deleted" && user.Email == $"deleted+{operationId:N}@deleted.invalid" && await dbContext.Users.CountAsync(item => item.Email == user.Email, cancellationToken) == 1;
        var clean = !await dbContext.UserRefreshTokens.AnyAsync(item => item.UserId == userId, cancellationToken) && !await dbContext.PasswordResetTokens.AnyAsync(item => item.UserId == userId, cancellationToken) && !await dbContext.UserProfiles.AnyAsync(item => item.UserId == userId, cancellationToken) && !await dbContext.UserSettings.AnyAsync(item => item.UserId == userId, cancellationToken) && !await dbContext.Devices.AnyAsync(item => item.UserId == userId, cancellationToken) && !await dbContext.LessonSessions.AnyAsync(item => item.UserId == userId, cancellationToken) && !await dbContext.Entitlements.AnyAsync(item => item.UserId == userId, cancellationToken);
        if (!noAccess || !clean) throw new InvalidOperationException("account_anonymization_verification_failed");
    }

    private async Task<bool> HasActivePremiumAsync(Guid userId, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        return await dbContext.Entitlements.AnyAsync(item => item.UserId == userId && item.EntitlementType == SubscriptionConstants.Entitlements.PremiumAccessType && item.Status == SubscriptionConstants.Entitlements.StatusActive && item.StartsAtUtc <= now && (!item.ExpiresAtUtc.HasValue || item.ExpiresAtUtc > now), cancellationToken)
            || await dbContext.Subscriptions.AnyAsync(item => item.UserId == userId && item.CurrentPeriodEndUtc > now && item.Status == "active", cancellationToken);
    }
    private async Task<bool> HasAdminOrCmsDependencyAsync(Guid userId, CancellationToken cancellationToken)
        => await dbContext.AdminUsers.AnyAsync(item => item.UserId == userId, cancellationToken)
            || await dbContext.AdminActions.AnyAsync(item => item.TargetUserId == userId, cancellationToken)
            || await dbContext.ContentPacks.AnyAsync(item => item.CreatedByUserId == userId || item.UpdatedByUserId == userId, cancellationToken)
            || await dbContext.PromptTemplates.AnyAsync(item => item.UpdatedByUserId == userId, cancellationToken)
            || await dbContext.ContentVersions.AnyAsync(item => item.PublishedByUserId == userId, cancellationToken)
            || await dbContext.ContentAuditLogs.AnyAsync(item => item.ActorUserId == userId, cancellationToken);
    private static List<string> DeserializeCodes(string value) => JsonSerializer.Deserialize<List<string>>(value) ?? [];
    private static AccountAnonymizationExecuteResponse ToResponse(AccountAnonymizationOperationEntity operation) => new() { OperationId = operation.Id, State = operation.State, VerificationState = operation.VerificationState, CompletedAtUtc = operation.CompletedAtUtc };
}

public sealed class AccountAnonymizationExecutionResult
{
    public AccountAnonymizationExecuteResponse? Response { get; private init; }
    public string? Error { get; private init; }
    public bool IsNotFound => Error == "account_anonymization_report_not_found";
    public bool IsCompleted => Response is not null;
    public static AccountAnonymizationExecutionResult Completed(AccountAnonymizationExecuteResponse response) => new() { Response = response };
    public static AccountAnonymizationExecutionResult NotFound() => ErrorResult("account_anonymization_report_not_found"); public static AccountAnonymizationExecutionResult WrongCategory() => ErrorResult("account_anonymization_not_deletion_request"); public static AccountAnonymizationExecutionResult ReportNotProcessing() => ErrorResult("account_anonymization_report_not_processing"); public static AccountAnonymizationExecutionResult PreflightMissing() => ErrorResult("account_anonymization_preflight_not_found"); public static AccountAnonymizationExecutionResult PreflightStale() => ErrorResult("account_anonymization_preflight_stale"); public static AccountAnonymizationExecutionResult OperationMismatch() => ErrorResult("account_anonymization_operation_mismatch"); public static AccountAnonymizationExecutionResult ActivePremium() => ErrorResult("account_anonymization_active_premium"); public static AccountAnonymizationExecutionResult SelfBlocked() => ErrorResult("account_anonymization_self_target_blocked"); public static AccountAnonymizationExecutionResult AdminBlocked() => ErrorResult("account_anonymization_admin_target_blocked"); public static AccountAnonymizationExecutionResult Executing() => ErrorResult("account_anonymization_operation_executing"); public static AccountAnonymizationExecutionResult DependencyBlocked() => ErrorResult("account_anonymization_dependency_blocked"); public static AccountAnonymizationExecutionResult InvalidRequest() => ErrorResult("account_anonymization_execute_request_invalid"); public static AccountAnonymizationExecutionResult Unavailable() => ErrorResult("account_anonymization_execution_unavailable"); private static AccountAnonymizationExecutionResult ErrorResult(string error) => new() { Error = error };
}
