using EnglishVoiceTutor.Api.Constants;
using EnglishVoiceTutor.Api.Contracts.FeedbackReports;
using EnglishVoiceTutor.Api.Data;
using EnglishVoiceTutor.Api.Data.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace EnglishVoiceTutor.Api.Services;

public interface IAccountDeletionRequestService
{
    Task<AccountDeletionRequestSubmissionResult> SubmitAsync(Guid userId, string? currentPassword, string? reason, CancellationToken cancellationToken);
}

public sealed class AccountDeletionRequestService(
    AppDbContext dbContext,
    IPasswordHasher<UserEntity> passwordHasher) : IAccountDeletionRequestService
{
    public async Task<AccountDeletionRequestSubmissionResult> SubmitAsync(Guid userId, string? currentPassword, string? reason, CancellationToken cancellationToken)
    {
        var normalizedReason = string.IsNullOrWhiteSpace(reason) ? string.Empty : reason.Trim();
        if (normalizedReason.Length > EntityConstants.Lengths.FeedbackReportMessageMaxLength)
        {
            return AccountDeletionRequestSubmissionResult.Invalid();
        }

        var user = await dbContext.Users.SingleOrDefaultAsync(candidate => candidate.Id == userId, cancellationToken);
        if (user is null)
        {
            return AccountDeletionRequestSubmissionResult.UserUnavailable();
        }

        if (string.IsNullOrEmpty(currentPassword)
            || passwordHasher.VerifyHashedPassword(user, user.PasswordHash, currentPassword) == PasswordVerificationResult.Failed)
        {
            return AccountDeletionRequestSubmissionResult.PasswordRejected();
        }

        var existing = await FindActiveRequestAsync(userId, cancellationToken);
        if (existing is not null)
        {
            return AccountDeletionRequestSubmissionResult.AlreadyRequested(ToResponse(existing, true));
        }

        var report = new UserFeedbackReportEntity
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Category = UserFeedbackReportConstants.AccountDeletionCategory,
            Message = normalizedReason,
            Status = UserFeedbackReportConstants.NewStatus,
            ClientPlatform = "account_deletion_request",
            ClientVersion = "v1",
            CreatedAtUtc = DateTimeOffset.UtcNow
        };
        dbContext.UserFeedbackReports.Add(report);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            dbContext.Entry(report).State = EntityState.Detached;
            existing = await FindActiveRequestAsync(userId, cancellationToken);
            if (existing is not null)
            {
                return AccountDeletionRequestSubmissionResult.AlreadyRequested(ToResponse(existing, true));
            }
            throw;
        }

        return AccountDeletionRequestSubmissionResult.Created(ToResponse(report, false));
    }

    private Task<UserFeedbackReportEntity?> FindActiveRequestAsync(Guid userId, CancellationToken cancellationToken) =>
        dbContext.UserFeedbackReports
            .Where(report => report.UserId == userId
                && report.Category == UserFeedbackReportConstants.AccountDeletionCategory
                && UserFeedbackReportConstants.ActiveAccountDeletionStatuses.Contains(report.Status))
            .OrderByDescending(report => report.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

    private static CreateAccountDeletionRequestResponse ToResponse(UserFeedbackReportEntity report, bool alreadyRequested) => new()
    {
        ReportId = report.Id,
        Status = report.Status,
        AlreadyRequested = alreadyRequested
    };
}

public sealed class AccountDeletionRequestSubmissionResult
{
    public bool IsInvalid { get; private init; }
    public bool IsPasswordRejected { get; private init; }
    public bool IsUserUnavailable { get; private init; }
    public bool IsAlreadyRequested { get; private init; }
    public CreateAccountDeletionRequestResponse? Response { get; private init; }

    public static AccountDeletionRequestSubmissionResult Invalid() => new() { IsInvalid = true };
    public static AccountDeletionRequestSubmissionResult PasswordRejected() => new() { IsPasswordRejected = true };
    public static AccountDeletionRequestSubmissionResult UserUnavailable() => new() { IsUserUnavailable = true };
    public static AccountDeletionRequestSubmissionResult AlreadyRequested(CreateAccountDeletionRequestResponse response) => new() { IsAlreadyRequested = true, Response = response };
    public static AccountDeletionRequestSubmissionResult Created(CreateAccountDeletionRequestResponse response) => new() { Response = response };
}
