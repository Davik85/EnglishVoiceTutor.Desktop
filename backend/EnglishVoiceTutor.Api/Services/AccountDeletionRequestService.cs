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
    Task<AccountDeletionRequestSubmissionResult> SubmitAdminAsync(Guid userId, string? comment, CancellationToken cancellationToken);
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

        return await CreateOrReturnExistingAsync(userId, normalizedReason, "account_deletion_request", returnUnavailableOnPersistenceFailure: false, cancellationToken);
    }

    public async Task<AccountDeletionRequestSubmissionResult> SubmitAdminAsync(Guid userId, string? comment, CancellationToken cancellationToken)
    {
        var normalizedComment = string.IsNullOrWhiteSpace(comment) ? string.Empty : comment.Trim();
        if (normalizedComment.Length == 0 || normalizedComment.Length > EntityConstants.Lengths.FeedbackReportMessageMaxLength)
        {
            return AccountDeletionRequestSubmissionResult.Invalid();
        }

        var user = await dbContext.Users.SingleOrDefaultAsync(candidate => candidate.Id == userId, cancellationToken);
        if (user is null)
        {
            return AccountDeletionRequestSubmissionResult.UserUnavailable();
        }

        return await CreateOrReturnExistingAsync(userId, normalizedComment, "admin_account_deletion_request", returnUnavailableOnPersistenceFailure: true, cancellationToken);
    }

    private async Task<AccountDeletionRequestSubmissionResult> CreateOrReturnExistingAsync(Guid userId, string message, string clientPlatform, bool returnUnavailableOnPersistenceFailure, CancellationToken cancellationToken)
    {
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
            Message = message,
            Status = UserFeedbackReportConstants.NewStatus,
            ClientPlatform = clientPlatform,
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
            if (returnUnavailableOnPersistenceFailure)
            {
                return AccountDeletionRequestSubmissionResult.Unavailable();
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
    public bool IsUnavailable { get; private init; }
    public CreateAccountDeletionRequestResponse? Response { get; private init; }

    public static AccountDeletionRequestSubmissionResult Invalid() => new() { IsInvalid = true };
    public static AccountDeletionRequestSubmissionResult PasswordRejected() => new() { IsPasswordRejected = true };
    public static AccountDeletionRequestSubmissionResult UserUnavailable() => new() { IsUserUnavailable = true };
    public static AccountDeletionRequestSubmissionResult AlreadyRequested(CreateAccountDeletionRequestResponse response) => new() { IsAlreadyRequested = true, Response = response };
    public static AccountDeletionRequestSubmissionResult Created(CreateAccountDeletionRequestResponse response) => new() { Response = response };
    public static AccountDeletionRequestSubmissionResult Unavailable() => new() { IsUnavailable = true };
}
