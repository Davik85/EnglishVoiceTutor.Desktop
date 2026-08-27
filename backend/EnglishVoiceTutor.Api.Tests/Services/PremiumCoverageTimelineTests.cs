using EnglishVoiceTutor.Api.Constants;
using EnglishVoiceTutor.Api.Data;
using EnglishVoiceTutor.Api.Data.Entities;
using EnglishVoiceTutor.Api.Services.Subscriptions;
using Microsoft.EntityFrameworkCore;

namespace EnglishVoiceTutor.Api.Tests.Services;

public sealed class PremiumCoverageTimelineTests
{
    private static readonly DateTimeOffset Reference = new(2026, 8, 27, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task OverlappingPremiumRowsExtendToLatestEndWithoutAddingOverlapTwice()
    {
        await using var db = CreateDb();
        var userId = Guid.NewGuid();
        AddEntitlement(db, userId, Reference.AddDays(-1), Reference.AddDays(10));
        AddEntitlement(db, userId, Reference.AddDays(5), Reference.AddDays(20));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var coverage = await PremiumCoverageTimeline.CalculateAsync(
            db,
            userId,
            Reference,
            TestContext.Current.CancellationToken);

        Assert.True(coverage.HasCoverage);
        Assert.Equal(Reference.AddDays(20), coverage.EndsAtUtc);
    }

    [Fact]
    public async Task GappedFuturePremiumDoesNotExtendCurrentContinuousTail()
    {
        await using var db = CreateDb();
        var userId = Guid.NewGuid();
        AddEntitlement(db, userId, Reference.AddDays(-1), Reference.AddDays(5));
        AddEntitlement(db, userId, Reference.AddDays(6), Reference.AddDays(20));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var coverage = await PremiumCoverageTimeline.CalculateAsync(
            db,
            userId,
            Reference,
            TestContext.Current.CancellationToken);

        Assert.Equal(Reference.AddDays(5), coverage.EndsAtUtc);
    }

    [Fact]
    public async Task RevokedInactiveExpiredOtherUserAndOtherPlanRowsDoNotExtendTail()
    {
        await using var db = CreateDb();
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        AddEntitlement(db, userId, Reference.AddDays(-1), Reference.AddDays(5));
        AddEntitlement(db, userId, Reference.AddDays(5), Reference.AddDays(30), status: SubscriptionConstants.Entitlements.StatusRevoked);
        AddEntitlement(db, userId, Reference.AddDays(5), Reference.AddDays(40), status: SubscriptionConstants.Entitlements.StatusInactive);
        AddEntitlement(db, userId, Reference.AddDays(-10), Reference.AddDays(-1));
        AddEntitlement(db, otherUserId, Reference.AddDays(-1), Reference.AddDays(50));
        AddEntitlement(db, userId, Reference.AddDays(5), Reference.AddDays(60), planId: SubscriptionConstants.Plans.TrialPlanId);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var coverage = await PremiumCoverageTimeline.CalculateAsync(
            db,
            userId,
            Reference,
            TestContext.Current.CancellationToken);

        Assert.Equal(Reference.AddDays(5), coverage.EndsAtUtc);
    }

    private static void AddEntitlement(
        AppDbContext db,
        Guid userId,
        DateTimeOffset startsAtUtc,
        DateTimeOffset expiresAtUtc,
        string status = SubscriptionConstants.Entitlements.StatusActive,
        string planId = SubscriptionConstants.Plans.PremiumPlanId)
    {
        db.Entitlements.Add(new EntitlementEntity
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            PlanId = planId,
            EntitlementType = SubscriptionConstants.Entitlements.PremiumAccessType,
            Source = SubscriptionConstants.Entitlements.SourceManualAdmin,
            Status = status,
            StartsAtUtc = startsAtUtc,
            ExpiresAtUtc = expiresAtUtc,
            CreatedAt = Reference.AddDays(-2),
            UpdatedAt = Reference.AddDays(-2)
        });
    }

    private static AppDbContext CreateDb() => new(
        new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options);
}
