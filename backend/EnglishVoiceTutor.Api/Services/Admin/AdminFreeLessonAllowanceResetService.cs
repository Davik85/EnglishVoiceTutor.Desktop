using System.Globalization;
using System.Text.Json;
using EnglishVoiceTutor.Api.Constants;
using EnglishVoiceTutor.Api.Contracts.Admin;
using EnglishVoiceTutor.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace EnglishVoiceTutor.Api.Services.Admin;

public sealed class AdminFreeLessonAllowanceResetService(
    AppDbContext dbContext,
    IAdminAuditService adminAuditService) : IAdminFreeLessonAllowanceResetService
{
    public async Task<AdminFreeLessonAllowanceResetResult> ResetFreeLessonAllowanceAsync(
        Guid adminUserId,
        Guid targetUserId,
        AdminFreeLessonAllowanceResetRequest request,
        CancellationToken cancellationToken)
    {
        if (targetUserId == Guid.Empty)
        {
            return BuildNotFoundResult(
                nameof(AdminFreeLessonAllowanceResetConstants.TargetUserNotFoundError),
                AdminFreeLessonAllowanceResetConstants.TargetUserNotFoundError);
        }

        if (string.IsNullOrWhiteSpace(request.Reason))
        {
            return BuildInvalidResult(
                nameof(AdminFreeLessonAllowanceResetConstants.ReasonRequiredError),
                AdminFreeLessonAllowanceResetConstants.ReasonRequiredError);
        }

        var normalizedReason = request.Reason.Trim();
        if (normalizedReason.Length > EntityConstants.Lengths.MediumTextMaxLength)
        {
            return BuildInvalidResult(
                nameof(AdminFreeLessonAllowanceResetConstants.ReasonTooLongError),
                AdminFreeLessonAllowanceResetConstants.ReasonTooLongError);
        }

        var usageDateResult = ResolveUsageDate(request.UsageDate);
        if (!usageDateResult.IsValid)
        {
            return BuildInvalidResult(
                nameof(AdminFreeLessonAllowanceResetConstants.UsageDateInvalidError),
                AdminFreeLessonAllowanceResetConstants.UsageDateInvalidError);
        }

        var usageDate = usageDateResult.Value;

        var targetUserExists = await dbContext.Users
            .AsNoTracking()
            .AnyAsync(user => user.Id == targetUserId, cancellationToken);

        if (!targetUserExists)
        {
            return BuildNotFoundResult(
                nameof(AdminFreeLessonAllowanceResetConstants.TargetUserNotFoundError),
                AdminFreeLessonAllowanceResetConstants.TargetUserNotFoundError);
        }

        var dailyFreeLessonUsage = await dbContext.DailyFreeLessonUsages
            .SingleOrDefaultAsync(item => item.UserId == targetUserId && item.UsageDate == usageDate, cancellationToken);

        if (dailyFreeLessonUsage is null)
        {
            return BuildNotFoundResult(
                nameof(AdminFreeLessonAllowanceResetConstants.DailyFreeLessonUsageNotFoundError),
                AdminFreeLessonAllowanceResetConstants.DailyFreeLessonUsageNotFoundError);
        }

        var resetAtUtc = DateTimeOffset.UtcNow;

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        dbContext.DailyFreeLessonUsages.Remove(dailyFreeLessonUsage);
        await dbContext.SaveChangesAsync(cancellationToken);

        var safeMetadataJson = JsonSerializer.Serialize(new Dictionary<string, object?>
        {
            [AdminFreeLessonAllowanceResetConstants.MetadataKeys.RemovedDailyFreeLessonUsageId] = dailyFreeLessonUsage.Id,
            [AdminFreeLessonAllowanceResetConstants.MetadataKeys.UsageDate] = usageDate.ToString(AdminFreeLessonAllowanceResetConstants.UsageDateFormat, CultureInfo.InvariantCulture),
            [AdminFreeLessonAllowanceResetConstants.MetadataKeys.LessonSessionId] = dailyFreeLessonUsage.LessonSessionId,
            [AdminFreeLessonAllowanceResetConstants.MetadataKeys.StudyLanguage] = dailyFreeLessonUsage.StudyLanguage,
            [AdminFreeLessonAllowanceResetConstants.MetadataKeys.ConsumedAtUtc] = dailyFreeLessonUsage.ConsumedAtUtc,
            [AdminFreeLessonAllowanceResetConstants.MetadataKeys.ResetAtUtc] = resetAtUtc
        });

        await adminAuditService.RecordTargetUserActionAsync(
            adminUserId,
            targetUserId,
            AdminAuditConstants.ActionTypes.FreeLessonAllowanceReset,
            normalizedReason,
            safeMetadataJson,
            cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        return new AdminFreeLessonAllowanceResetResult
        {
            Response = new AdminFreeLessonAllowanceResetResponse
            {
                UserId = targetUserId,
                UsageDate = usageDate.ToString(AdminFreeLessonAllowanceResetConstants.UsageDateFormat, CultureInfo.InvariantCulture),
                ResetApplied = true,
                RemovedDailyFreeLessonUsageId = dailyFreeLessonUsage.Id,
                LessonSessionId = dailyFreeLessonUsage.LessonSessionId,
                StudyLanguage = dailyFreeLessonUsage.StudyLanguage,
                ConsumedAtUtc = dailyFreeLessonUsage.ConsumedAtUtc,
                Reason = normalizedReason,
                ResetAtUtc = resetAtUtc,
                AuditWritten = true
            }
        };
    }

    private static (bool IsValid, DateOnly Value) ResolveUsageDate(string? usageDate)
    {
        if (string.IsNullOrWhiteSpace(usageDate))
        {
            return (true, DateOnly.FromDateTime(DateTime.UtcNow));
        }

        var normalizedUsageDate = usageDate.Trim();
        var isValid = DateOnly.TryParseExact(
            normalizedUsageDate,
            AdminFreeLessonAllowanceResetConstants.UsageDateFormat,
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out var parsedUsageDate);

        return (isValid, parsedUsageDate);
    }

    private static AdminFreeLessonAllowanceResetResult BuildInvalidResult(string errorCode, string errorMessage)
    {
        return new AdminFreeLessonAllowanceResetResult
        {
            IsInvalid = true,
            ErrorCode = errorCode,
            ErrorMessage = errorMessage
        };
    }

    private static AdminFreeLessonAllowanceResetResult BuildNotFoundResult(string errorCode, string errorMessage)
    {
        return new AdminFreeLessonAllowanceResetResult
        {
            IsNotFound = true,
            ErrorCode = errorCode,
            ErrorMessage = errorMessage
        };
    }
}
