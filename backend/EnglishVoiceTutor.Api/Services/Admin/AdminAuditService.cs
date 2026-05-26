using EnglishVoiceTutor.Api.Constants;
using EnglishVoiceTutor.Api.Data;
using EnglishVoiceTutor.Api.Data.Entities;

namespace EnglishVoiceTutor.Api.Services.Admin;

public sealed class AdminAuditService(AppDbContext dbContext) : IAdminAuditService
{
    public async Task RecordTargetUserActionAsync(
        Guid adminUserId,
        Guid targetUserId,
        string actionType,
        string reason,
        string? safeMetadataJson,
        CancellationToken cancellationToken)
    {
        if (adminUserId == Guid.Empty)
        {
            throw new AdminAuditValidationException(AdminAuditConstants.ValidationErrors.AdminUserIdRequiredError);
        }

        if (targetUserId == Guid.Empty)
        {
            throw new AdminAuditValidationException(AdminAuditConstants.ValidationErrors.TargetUserIdRequiredError);
        }

        if (string.IsNullOrWhiteSpace(actionType))
        {
            throw new AdminAuditValidationException(AdminAuditConstants.ValidationErrors.ActionTypeRequiredError);
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new AdminAuditValidationException(AdminAuditConstants.ValidationErrors.ReasonRequiredError);
        }

        var normalizedActionType = actionType.Trim();
        var normalizedReason = reason.Trim();
        var normalizedSafeMetadataJson = string.IsNullOrWhiteSpace(safeMetadataJson)
            ? null
            : safeMetadataJson.Trim();

        if (normalizedActionType.Length > EntityConstants.Lengths.ActionTypeMaxLength)
        {
            throw new AdminAuditValidationException(AdminAuditConstants.ValidationErrors.ActionTypeTooLongError);
        }

        if (normalizedReason.Length > EntityConstants.Lengths.MediumTextMaxLength)
        {
            throw new AdminAuditValidationException(AdminAuditConstants.ValidationErrors.ReasonTooLongError);
        }

        if (normalizedSafeMetadataJson is not null &&
            normalizedSafeMetadataJson.Length > EntityConstants.Lengths.MetadataJsonMaxLength)
        {
            throw new AdminAuditValidationException(AdminAuditConstants.ValidationErrors.SafeMetadataTooLongError);
        }

        var entity = new AdminActionEntity
        {
            Id = Guid.NewGuid(),
            AdminUserId = adminUserId,
            TargetUserId = targetUserId,
            ActionType = normalizedActionType,
            Reason = normalizedReason,
            SafeMetadataJson = normalizedSafeMetadataJson,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        dbContext.AdminActions.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}

internal sealed class AdminAuditValidationException(string message) : Exception(message);
