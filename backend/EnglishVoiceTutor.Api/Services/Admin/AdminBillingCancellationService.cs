using System.Text.Json;
using EnglishVoiceTutor.Api.Constants;
using EnglishVoiceTutor.Api.Contracts.Admin;
using EnglishVoiceTutor.Api.Data;
using EnglishVoiceTutor.Api.Services.Billing;
using Microsoft.EntityFrameworkCore;

namespace EnglishVoiceTutor.Api.Services.Admin;

public sealed class AdminBillingCancellationService(
    AppDbContext dbContext,
    IBillingSubscriptionCancellationService cancellationService,
    IAdminAuditService adminAuditService) : IAdminBillingCancellationService
{
    public async Task<AdminBillingCancelRenewalResult> CancelRenewalAsync(
        Guid adminUserId,
        Guid targetUserId,
        AdminBillingCancelRenewalRequest request,
        CancellationToken cancellationToken)
    {
        if (targetUserId == Guid.Empty)
            return Invalid("target_user_required", "Target user id is required.");
        if (string.IsNullOrWhiteSpace(request.Reason))
            return Invalid("reason_required", "Reason is required.");

        var reason = request.Reason.Trim();
        if (reason.Length > EntityConstants.Lengths.EntitlementReasonMaxLength)
            return Invalid("reason_too_long", "Reason exceeds the maximum allowed length.");

        var targetUserExists = await dbContext.Users.AsNoTracking().AnyAsync(user => user.Id == targetUserId, cancellationToken);
        if (!targetUserExists)
            return new AdminBillingCancelRenewalResult { IsNotFound = true, ErrorCode = "target_user_not_found", ErrorMessage = "Selected user was not found." };

        var cancelResult = await cancellationService.CancelUserSubscriptionRenewalAsync(targetUserId, cancellationToken);
        var resultCode = MapResultCode(cancelResult);
        var response = new AdminBillingCancelRenewalResponse
        {
            UserId = targetUserId,
            ResultCode = resultCode,
            Accepted = cancelResult.Accepted,
            Success = cancelResult.Success,
            AlreadyCanceling = cancelResult.AlreadyCanceling,
            Provider = cancelResult.Provider,
            SubscriptionStatus = cancelResult.SubscriptionStatus,
            CancelAtPeriodEnd = cancelResult.CancelAtPeriodEnd,
            ScheduledChangeAction = cancelResult.CancelAtPeriodEnd ? SubscriptionConstants.ScheduledChangeActions.Cancel : null,
            ScheduledChangeEffectiveAtUtc = cancelResult.ScheduledChangeEffectiveAtUtc,
            CurrentPeriodEndUtc = cancelResult.CurrentPeriodEndUtc,
            AuditWritten = true,
            ProviderErrorCode = cancelResult.ProviderErrorCode,
            ProviderErrorMessageSafe = cancelResult.ProviderErrorMessageSafe,
            ProviderHttpStatusCode = cancelResult.ProviderHttpStatusCode,
            ProviderRequestId = cancelResult.ProviderRequestId,
            CancellationAttemptedAtUtc = cancelResult.CancellationAttemptedAtUtc,
            ProviderSubscriptionPresent = cancelResult.ProviderSubscriptionPresent,
            ProviderSubscriptionIdLast4 = cancelResult.ProviderSubscriptionIdLast4,
            ProviderSubscriptionIdHash = cancelResult.ProviderSubscriptionIdHash
        };

        var safeMetadataJson = JsonSerializer.Serialize(new Dictionary<string, object?>
        {
            ["resultCode"] = resultCode,
            ["provider"] = cancelResult.Provider,
            ["subscriptionStatus"] = cancelResult.SubscriptionStatus,
            ["cancelAtPeriodEnd"] = cancelResult.CancelAtPeriodEnd,
            ["scheduledChangeAction"] = response.ScheduledChangeAction,
            ["scheduledChangeEffectiveAtUtc"] = cancelResult.ScheduledChangeEffectiveAtUtc,
            ["currentPeriodEndUtc"] = cancelResult.CurrentPeriodEndUtc,
            ["providerErrorCode"] = cancelResult.ProviderErrorCode,
            ["providerErrorMessageSafe"] = cancelResult.ProviderErrorMessageSafe,
            ["providerHttpStatusCode"] = cancelResult.ProviderHttpStatusCode,
            ["providerRequestId"] = cancelResult.ProviderRequestId,
            ["cancellationAttemptedAtUtc"] = cancelResult.CancellationAttemptedAtUtc,
            ["providerSubscriptionPresent"] = cancelResult.ProviderSubscriptionPresent,
            ["providerSubscriptionIdLast4"] = cancelResult.ProviderSubscriptionIdLast4,
            ["providerSubscriptionIdHash"] = cancelResult.ProviderSubscriptionIdHash
        });

        await adminAuditService.RecordTargetUserActionAsync(
            adminUserId,
            targetUserId,
            AdminAuditConstants.ActionTypes.AdminBillingCancelRenewalCompleted,
            reason,
            safeMetadataJson,
            cancellationToken);

        return new AdminBillingCancelRenewalResult { Response = response };
    }

    private static string MapResultCode(EnglishVoiceTutor.Api.Contracts.Billing.CancelBillingSubscriptionResponse response)
    {
        if (response.Success || response.Accepted) return response.AlreadyCanceling ? "already_scheduled" : "cancellation_scheduled";
        if (string.Equals(response.SubscriptionStatus, SubscriptionConstants.SubscriptionStatuses.None, StringComparison.OrdinalIgnoreCase)) return "no_paid_subscription";
        if (response.Message.Contains("not configured", StringComparison.OrdinalIgnoreCase) || response.Message.Contains("not available", StringComparison.OrdinalIgnoreCase)) return "provider_not_configured";
        if (!string.IsNullOrWhiteSpace(response.Message)) return "provider_error";
        return "unknown";
    }

    private static AdminBillingCancelRenewalResult Invalid(string code, string message) => new() { IsInvalid = true, ErrorCode = code, ErrorMessage = message };
}
