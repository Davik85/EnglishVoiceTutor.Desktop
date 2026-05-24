using EnglishVoiceTutor.Api.Constants;
using EnglishVoiceTutor.Api.Data;
using EnglishVoiceTutor.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace EnglishVoiceTutor.Api.Services.Subscriptions;

public sealed class SubscriptionPlanCatalogService(AppDbContext dbContext) : ISubscriptionPlanCatalogService
{
    public async Task EnsureDefaultPlansAsync(CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;

        await EnsurePlanAsync(
            SubscriptionConstants.Plans.FreePlanId,
            SubscriptionConstants.Plans.FreePlanName,
            SubscriptionConstants.Plans.FreeTier,
            now,
            cancellationToken);

        await EnsurePlanAsync(
            SubscriptionConstants.Plans.PremiumPlanId,
            SubscriptionConstants.Plans.PremiumPlanName,
            SubscriptionConstants.Plans.PremiumTier,
            now,
            cancellationToken);
    }

    private async Task EnsurePlanAsync(
        string planId,
        string displayName,
        string tier,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var existingPlan = await dbContext.Plans
            .SingleOrDefaultAsync(plan => plan.PlanId == planId, cancellationToken);

        if (existingPlan is null)
        {
            dbContext.Plans.Add(new PlanEntity
            {
                Id = Guid.NewGuid(),
                PlanId = planId,
                DisplayName = displayName,
                Tier = tier,
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now
            });

            return;
        }

        var changed = false;

        if (existingPlan.DisplayName != displayName)
        {
            existingPlan.DisplayName = displayName;
            changed = true;
        }

        if (existingPlan.Tier != tier)
        {
            existingPlan.Tier = tier;
            changed = true;
        }

        if (!existingPlan.IsActive)
        {
            existingPlan.IsActive = true;
            changed = true;
        }

        if (changed)
        {
            existingPlan.UpdatedAt = now;
        }
    }
}
