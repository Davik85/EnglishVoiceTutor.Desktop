using EnglishVoiceTutor.Api.Constants;
using EnglishVoiceTutor.Api.Contracts.Subscription;
using EnglishVoiceTutor.Api.Data;
using EnglishVoiceTutor.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace EnglishVoiceTutor.Api.Services.Subscriptions;

public sealed class TrialClaimService(
    AppDbContext dbContext,
    ISubscriptionStatusService subscriptionStatusService) : ITrialClaimService
{
    public async Task<TrialClaimResponse> ClaimTrialAsync(Guid userId, string source, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;

        var hasAnyTrialGrant = await dbContext.TrialGrants
            .AsNoTracking()
            .AnyAsync(trialGrant => trialGrant.UserId == userId, cancellationToken);

        if (hasAnyTrialGrant)
        {
            var existingStatus = await subscriptionStatusService.GetStatusAsync(userId, source, cancellationToken);
            return new TrialClaimResponse
            {
                UserId = userId,
                Claimed = false,
                AlreadyClaimed = true,
                TrialActive = existingStatus.TrialActive,
                TrialEndsAtUtc = existingStatus.TrialEndsAtUtc,
                Message = SubscriptionConstants.TrialAlreadyClaimedMessage,
                Status = existingStatus
            };
        }

        var trialGrant = new TrialGrantEntity
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            GrantedAtUtc = now,
            ExpiresAtUtc = now.AddDays(SubscriptionConstants.PremiumTrialDays),
            SourcePlatform = source,
            Status = SubscriptionConstants.Entitlements.StatusActive,
            CreatedAt = now
        };

        dbContext.TrialGrants.Add(trialGrant);
        await dbContext.SaveChangesAsync(cancellationToken);

        var updatedStatus = await subscriptionStatusService.GetStatusAsync(userId, source, cancellationToken);

        return new TrialClaimResponse
        {
            UserId = userId,
            Claimed = true,
            AlreadyClaimed = false,
            TrialActive = updatedStatus.TrialActive,
            TrialEndsAtUtc = updatedStatus.TrialEndsAtUtc,
            Message = SubscriptionConstants.TrialClaimedSuccessMessage,
            Status = updatedStatus
        };
    }
}
