using EnglishVoiceTutor.Api.Constants;
using EnglishVoiceTutor.Api.Contracts.Admin;
using EnglishVoiceTutor.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace EnglishVoiceTutor.Api.Services.Admin;

public sealed class AdminAuditLogService(AppDbContext dbContext) : IAdminAuditLogService
{
    public async Task<AdminAuditActionsResult> GetTargetUserAuditActionsAsync(
        Guid targetUserId,
        int? limit,
        CancellationToken cancellationToken)
    {
        if (targetUserId == Guid.Empty)
        {
            return BuildNotFoundResult(
                nameof(AdminAuditLogConstants.TargetUserNotFoundError),
                AdminAuditLogConstants.TargetUserNotFoundError);
        }

        var validatedLimit = limit ?? AdminAuditLogConstants.DefaultLimit;
        if (validatedLimit < AdminAuditLogConstants.MinLimit || validatedLimit > AdminAuditLogConstants.MaxLimit)
        {
            return BuildInvalidResult(
                nameof(AdminAuditLogConstants.LimitOutOfRangeError),
                AdminAuditLogConstants.LimitOutOfRangeError);
        }

        var targetUserExists = await dbContext.Users
            .AsNoTracking()
            .AnyAsync(user => user.Id == targetUserId, cancellationToken);

        if (!targetUserExists)
        {
            return BuildNotFoundResult(
                nameof(AdminAuditLogConstants.TargetUserNotFoundError),
                AdminAuditLogConstants.TargetUserNotFoundError);
        }

        var auditActions = await dbContext.AdminActions
            .AsNoTracking()
            .Where(action => action.TargetUserId == targetUserId)
            .OrderByDescending(action => action.CreatedAtUtc)
            .Take(validatedLimit)
            .Select(action => new AdminAuditActionSnapshot
            {
                AdminActionId = action.Id,
                AdminUserId = action.AdminUserId,
                TargetUserId = action.TargetUserId,
                ActionType = action.ActionType,
                Reason = action.Reason,
                CreatedAtUtc = action.CreatedAtUtc,
                SafeMetadataJson = action.SafeMetadataJson
            })
            .ToListAsync(cancellationToken);

        return new AdminAuditActionsResult
        {
            Response = new AdminAuditActionsResponse
            {
                UserId = targetUserId,
                Items = auditActions,
                Limit = validatedLimit,
                CheckedAtUtc = DateTimeOffset.UtcNow
            }
        };
    }

    private static AdminAuditActionsResult BuildInvalidResult(string errorCode, string errorMessage)
    {
        return new AdminAuditActionsResult
        {
            IsInvalid = true,
            ErrorCode = errorCode,
            ErrorMessage = errorMessage
        };
    }

    private static AdminAuditActionsResult BuildNotFoundResult(string errorCode, string errorMessage)
    {
        return new AdminAuditActionsResult
        {
            IsNotFound = true,
            ErrorCode = errorCode,
            ErrorMessage = errorMessage
        };
    }
}
