using System.Text.Json;
using EnglishVoiceTutor.Api.Constants;
using EnglishVoiceTutor.Api.Contracts.Admin;
using EnglishVoiceTutor.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace EnglishVoiceTutor.Api.Services.Admin;

public sealed class AdminPremiumRevokeService(
    AppDbContext dbContext,
    IAdminAuditService adminAuditService) : IAdminPremiumRevokeService
{
    public async Task<AdminManualPremiumRevokeResult> RevokePremiumAsync(
        Guid adminUserId,
        Guid targetUserId,
        Guid entitlementId,
        AdminManualPremiumRevokeRequest request,
        CancellationToken cancellationToken)
    {
        if (targetUserId == Guid.Empty)
        {
            return BuildInvalidResult(nameof(AdminPremiumRevokeConstants.TargetUserNotFoundError), AdminPremiumRevokeConstants.TargetUserNotFoundError);
        }

        if (entitlementId == Guid.Empty)
        {
            return BuildNotFoundResult(nameof(AdminPremiumRevokeConstants.EntitlementNotFoundError), AdminPremiumRevokeConstants.EntitlementNotFoundError);
        }

        if (string.IsNullOrWhiteSpace(request.Reason))
        {
            return BuildInvalidResult(nameof(AdminPremiumRevokeConstants.ReasonRequiredError), AdminPremiumRevokeConstants.ReasonRequiredError);
        }

        var normalizedReason = request.Reason.Trim();
        if (normalizedReason.Length > EntityConstants.Lengths.EntitlementReasonMaxLength)
        {
            return BuildInvalidResult(nameof(AdminPremiumRevokeConstants.ReasonTooLongError), AdminPremiumRevokeConstants.ReasonTooLongError);
        }

        var targetUserExists = await dbContext.Users
            .AsNoTracking()
            .AnyAsync(user => user.Id == targetUserId, cancellationToken);

        if (!targetUserExists)
        {
            return BuildNotFoundResult(nameof(AdminPremiumRevokeConstants.TargetUserNotFoundError), AdminPremiumRevokeConstants.TargetUserNotFoundError);
        }

        var entitlement = await dbContext.Entitlements
            .SingleOrDefaultAsync(item => item.Id == entitlementId && item.UserId == targetUserId, cancellationToken);

        if (entitlement is null)
        {
            return BuildNotFoundResult(nameof(AdminPremiumRevokeConstants.EntitlementNotFoundError), AdminPremiumRevokeConstants.EntitlementNotFoundError);
        }

        if (entitlement.PlanId != SubscriptionConstants.Plans.PremiumPlanId ||
            entitlement.EntitlementType != SubscriptionConstants.Entitlements.PremiumAccessType ||
            entitlement.Source != SubscriptionConstants.Entitlements.SourceManualAdmin ||
            entitlement.Status != SubscriptionConstants.Entitlements.StatusActive)
        {
            return BuildConflictResult(nameof(AdminPremiumRevokeConstants.EntitlementNotRevokableError), AdminPremiumRevokeConstants.EntitlementNotRevokableError);
        }

        var revokedAtUtc = DateTimeOffset.UtcNow;
        var previousStatus = entitlement.Status;
        var previousExpiresAtUtc = entitlement.ExpiresAtUtc;

        entitlement.Status = SubscriptionConstants.Entitlements.StatusRevoked;
        if (!entitlement.ExpiresAtUtc.HasValue || entitlement.ExpiresAtUtc.Value > revokedAtUtc)
        {
            entitlement.ExpiresAtUtc = revokedAtUtc;
        }

        entitlement.UpdatedAt = revokedAtUtc;

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        var safeMetadataJson = JsonSerializer.Serialize(new Dictionary<string, object?>
        {
            [AdminPremiumRevokeConstants.MetadataKeys.EntitlementId] = entitlement.Id,
            [AdminPremiumRevokeConstants.MetadataKeys.PreviousStatus] = previousStatus,
            [AdminPremiumRevokeConstants.MetadataKeys.NewStatus] = entitlement.Status,
            [AdminPremiumRevokeConstants.MetadataKeys.PreviousExpiresAtUtc] = previousExpiresAtUtc,
            [AdminPremiumRevokeConstants.MetadataKeys.RevokedAtUtc] = revokedAtUtc,
            [AdminPremiumRevokeConstants.MetadataKeys.Source] = entitlement.Source
        });

        await adminAuditService.RecordTargetUserActionAsync(
            adminUserId,
            targetUserId,
            AdminAuditConstants.ActionTypes.ManualPremiumRevoke,
            normalizedReason,
            safeMetadataJson,
            cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        return new AdminManualPremiumRevokeResult
        {
            Response = new AdminManualPremiumRevokeResponse
            {
                EntitlementId = entitlement.Id,
                UserId = entitlement.UserId,
                PlanId = entitlement.PlanId,
                EntitlementType = entitlement.EntitlementType,
                Source = entitlement.Source,
                Status = entitlement.Status,
                StartsAtUtc = entitlement.StartsAtUtc,
                ExpiresAtUtc = entitlement.ExpiresAtUtc,
                Reason = normalizedReason,
                RevokedAtUtc = revokedAtUtc,
                UpdatedAt = entitlement.UpdatedAt,
                AuditWritten = true
            }
        };
    }

    private static AdminManualPremiumRevokeResult BuildInvalidResult(string errorCode, string errorMessage)
    {
        return new AdminManualPremiumRevokeResult
        {
            IsInvalid = true,
            ErrorCode = errorCode,
            ErrorMessage = errorMessage
        };
    }

    private static AdminManualPremiumRevokeResult BuildNotFoundResult(string errorCode, string errorMessage)
    {
        return new AdminManualPremiumRevokeResult
        {
            IsNotFound = true,
            ErrorCode = errorCode,
            ErrorMessage = errorMessage
        };
    }

    private static AdminManualPremiumRevokeResult BuildConflictResult(string errorCode, string errorMessage)
    {
        return new AdminManualPremiumRevokeResult
        {
            IsConflict = true,
            ErrorCode = errorCode,
            ErrorMessage = errorMessage
        };
    }
}
