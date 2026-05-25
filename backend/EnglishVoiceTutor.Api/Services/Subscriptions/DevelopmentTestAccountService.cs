using EnglishVoiceTutor.Api.Constants;
using EnglishVoiceTutor.Api.Data;
using EnglishVoiceTutor.Api.Data.Entities;
using EnglishVoiceTutor.Api.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace EnglishVoiceTutor.Api.Services.Subscriptions;

public sealed class DevelopmentTestAccountService(
    AppDbContext dbContext,
    IHostEnvironment hostEnvironment,
    IOptions<DevelopmentTestAccountOptions> options,
    ILogger<DevelopmentTestAccountService> logger) : IDevelopmentTestAccountService
{
    public async Task EnsureUnlimitedPremiumAccessIfConfiguredAsync(Guid userId, string email, CancellationToken cancellationToken)
    {
        if (!hostEnvironment.IsDevelopment())
        {
            return;
        }

        var normalizedEmail = NormalizeEmail(email);
        if (!IsConfiguredUnlimitedPremiumEmail(normalizedEmail))
        {
            return;
        }

        var now = DateTimeOffset.UtcNow;
        var grantedOrUpdated = false;

        var existingEntitlement = await dbContext.Entitlements
            .AsTracking()
            .SingleOrDefaultAsync(entitlement =>
                entitlement.UserId == userId &&
                entitlement.EntitlementType == SubscriptionConstants.Entitlements.PremiumAccessType &&
                entitlement.Source == SubscriptionConstants.DevelopmentTestAccountPremiumSource,
                cancellationToken);

        if (existingEntitlement is null)
        {
            dbContext.Entitlements.Add(new EntitlementEntity
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                PlanId = SubscriptionConstants.Plans.PremiumPlanId,
                EntitlementType = SubscriptionConstants.Entitlements.PremiumAccessType,
                Source = SubscriptionConstants.DevelopmentTestAccountPremiumSource,
                Status = SubscriptionConstants.Entitlements.StatusActive,
                StartsAtUtc = now,
                ExpiresAtUtc = null,
                Reason = SubscriptionConstants.DevelopmentTestAccountPremiumReason,
                CreatedAt = now,
                UpdatedAt = now
            });

            grantedOrUpdated = true;
        }
        else
        {
            if (existingEntitlement.Status != SubscriptionConstants.Entitlements.StatusActive)
            {
                existingEntitlement.Status = SubscriptionConstants.Entitlements.StatusActive;
                grantedOrUpdated = true;
            }

            if (existingEntitlement.PlanId != SubscriptionConstants.Plans.PremiumPlanId)
            {
                existingEntitlement.PlanId = SubscriptionConstants.Plans.PremiumPlanId;
                grantedOrUpdated = true;
            }

            if (existingEntitlement.ExpiresAtUtc is not null)
            {
                existingEntitlement.ExpiresAtUtc = null;
                grantedOrUpdated = true;
            }

            if (grantedOrUpdated)
            {
                existingEntitlement.UpdatedAt = now;
            }
        }

        if (grantedOrUpdated)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        logger.LogInformation(
            "Development test account premium entitlement check finished. UserId={UserId}; GrantedOrUpdated={GrantedOrUpdated}",
            userId,
            grantedOrUpdated);
    }

    private bool IsConfiguredUnlimitedPremiumEmail(string normalizedEmail)
    {
        return options.Value.UnlimitedPremiumEmails
            .Select(NormalizeEmail)
            .Any(configuredEmail => configuredEmail == normalizedEmail);
    }

    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();
}
