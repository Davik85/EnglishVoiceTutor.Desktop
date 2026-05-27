using System.Text.Json;
using EnglishVoiceTutor.Api.Constants;
using EnglishVoiceTutor.Api.Contracts.Admin;
using EnglishVoiceTutor.Api.Data;
using EnglishVoiceTutor.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace EnglishVoiceTutor.Api.Services.Admin;

public sealed class AdminPremiumGrantService(
    AppDbContext dbContext,
    IAdminAuditService adminAuditService) : IAdminPremiumGrantService
{
    public async Task<AdminManualPremiumGrantResult> GrantPremiumAsync(
        Guid adminUserId,
        Guid targetUserId,
        AdminManualPremiumGrantRequest request,
        CancellationToken cancellationToken)
    {
        if (targetUserId == Guid.Empty)
        {
            return BuildInvalidResult(
                nameof(AdminPremiumGrantConstants.TargetUserNotFoundError),
                AdminPremiumGrantConstants.TargetUserNotFoundError);
        }

        if (request.DurationDays < AdminPremiumGrantConstants.MinDurationDays ||
            request.DurationDays > AdminPremiumGrantConstants.MaxDurationDays)
        {
            return BuildInvalidResult(
                nameof(AdminPremiumGrantConstants.DurationDaysOutOfRangeError),
                AdminPremiumGrantConstants.DurationDaysOutOfRangeError);
        }

        if (string.IsNullOrWhiteSpace(request.Reason))
        {
            return BuildInvalidResult(
                nameof(AdminPremiumGrantConstants.ReasonRequiredError),
                AdminPremiumGrantConstants.ReasonRequiredError);
        }

        var normalizedReason = request.Reason.Trim();
        if (normalizedReason.Length > EntityConstants.Lengths.EntitlementReasonMaxLength)
        {
            return BuildInvalidResult(
                nameof(AdminPremiumGrantConstants.ReasonTooLongError),
                AdminPremiumGrantConstants.ReasonTooLongError);
        }

        var targetUserExists = await dbContext.Users
            .AsNoTracking()
            .AnyAsync(user => user.Id == targetUserId, cancellationToken);

        if (!targetUserExists)
        {
            return new AdminManualPremiumGrantResult
            {
                IsNotFound = true,
                ErrorCode = nameof(AdminPremiumGrantConstants.TargetUserNotFoundError),
                ErrorMessage = AdminPremiumGrantConstants.TargetUserNotFoundError
            };
        }

        var now = DateTimeOffset.UtcNow;
        var latestActivePremiumExpiry = await dbContext.Entitlements
            .AsNoTracking()
            .Where(entitlement => entitlement.UserId == targetUserId)
            .Where(entitlement => entitlement.EntitlementType == SubscriptionConstants.Entitlements.PremiumAccessType)
            .Where(entitlement => entitlement.Status == SubscriptionConstants.Entitlements.StatusActive)
            .Where(entitlement => entitlement.ExpiresAtUtc > now)
            .MaxAsync(entitlement => (DateTimeOffset?)entitlement.ExpiresAtUtc, cancellationToken);

        var startsAtUtc = latestActivePremiumExpiry ?? now;
        var expiresAtUtc = startsAtUtc.AddDays(request.DurationDays);

        var entitlement = new EntitlementEntity
        {
            Id = Guid.NewGuid(),
            UserId = targetUserId,
            PlanId = SubscriptionConstants.Plans.PremiumPlanId,
            SubscriptionId = null,
            EntitlementType = SubscriptionConstants.Entitlements.PremiumAccessType,
            Source = SubscriptionConstants.Entitlements.SourceManualAdmin,
            Status = SubscriptionConstants.Entitlements.StatusActive,
            StartsAtUtc = startsAtUtc,
            ExpiresAtUtc = expiresAtUtc,
            Reason = normalizedReason,
            CreatedAt = now,
            UpdatedAt = now
        };

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        dbContext.Entitlements.Add(entitlement);
        await dbContext.SaveChangesAsync(cancellationToken);

        var safeMetadataJson = JsonSerializer.Serialize(new Dictionary<string, object?>
        {
            [AdminPremiumGrantConstants.MetadataKeys.EntitlementId] = entitlement.Id,
            [AdminPremiumGrantConstants.MetadataKeys.DurationDays] = request.DurationDays,
            [AdminPremiumGrantConstants.MetadataKeys.StartsAtUtc] = entitlement.StartsAtUtc,
            [AdminPremiumGrantConstants.MetadataKeys.ExpiresAtUtc] = entitlement.ExpiresAtUtc,
            [AdminPremiumGrantConstants.MetadataKeys.Source] = entitlement.Source
        });

        await adminAuditService.RecordTargetUserActionAsync(
            adminUserId,
            targetUserId,
            AdminAuditConstants.ActionTypes.ManualPremiumGrant,
            normalizedReason,
            safeMetadataJson,
            cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        return new AdminManualPremiumGrantResult
        {
            Response = new AdminManualPremiumGrantResponse
            {
                EntitlementId = entitlement.Id,
                UserId = entitlement.UserId,
                PlanId = entitlement.PlanId,
                EntitlementType = entitlement.EntitlementType,
                Source = entitlement.Source,
                Status = entitlement.Status,
                StartsAtUtc = entitlement.StartsAtUtc,
                ExpiresAtUtc = entitlement.ExpiresAtUtc,
                Reason = entitlement.Reason,
                CreatedAt = entitlement.CreatedAt,
                UpdatedAt = entitlement.UpdatedAt,
                AuditWritten = true
            }
        };
    }

    private static AdminManualPremiumGrantResult BuildInvalidResult(string errorCode, string errorMessage)
    {
        return new AdminManualPremiumGrantResult
        {
            IsInvalid = true,
            ErrorCode = errorCode,
            ErrorMessage = errorMessage
        };
    }
}
