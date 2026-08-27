using EnglishVoiceTutor.Api.Constants;
using EnglishVoiceTutor.Api.Contracts.Admin;
using EnglishVoiceTutor.Api.Data;
using EnglishVoiceTutor.Api.Data.Entities;
using EnglishVoiceTutor.Api.Services.Admin;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace EnglishVoiceTutor.Api.Tests.Services.Admin;

public sealed class AdminPremiumGrantServiceTests
{
    [Fact]
    public async Task ActiveTrialIsTheStartForManualPremium()
    {
        await using var db = CreateDb();
        var adminUserId = await AddUserAsync(db);
        var targetUserId = await AddUserAsync(db);
        var trialEnd = DateTimeOffset.UtcNow.AddDays(7);
        await AddTrialAsync(db, targetUserId, trialEnd);

        var result = await GrantAsync(db, adminUserId, targetUserId, 30);

        Assert.NotNull(result.Response);
        Assert.Equal(SubscriptionConstants.Entitlements.SourceManualAdmin, result.Response.Source);
        Assert.Equal(trialEnd, result.Response.StartsAtUtc);
        Assert.Equal(trialEnd.AddDays(30), result.Response.ExpiresAtUtc);
    }

    [Fact]
    public async Task LaterActiveManualEntitlementRemainsTheStartForAnAdditionalGrant()
    {
        await using var db = CreateDb();
        var adminUserId = await AddUserAsync(db);
        var targetUserId = await AddUserAsync(db);
        var existingExpiry = DateTimeOffset.UtcNow.AddDays(45);
        await AddEntitlementAsync(db, targetUserId, existingExpiry, SubscriptionConstants.Entitlements.StatusActive);

        var result = await GrantAsync(db, adminUserId, targetUserId, 10);

        Assert.NotNull(result.Response);
        Assert.Equal(existingExpiry, result.Response.StartsAtUtc);
        Assert.Equal(existingExpiry.AddDays(10), result.Response.ExpiresAtUtc);
    }

    [Fact]
    public async Task ActiveProviderPremiumIsTheStartForManualPremium()
    {
        await using var db = CreateDb();
        var adminUserId = await AddUserAsync(db);
        var targetUserId = await AddUserAsync(db);
        var providerExpiry = DateTimeOffset.UtcNow.AddDays(30);
        await AddEntitlementAsync(
            db,
            targetUserId,
            providerExpiry,
            SubscriptionConstants.Entitlements.StatusActive,
            SubscriptionConstants.Entitlements.SourceProviderEvent);

        var result = await GrantAsync(db, adminUserId, targetUserId, 10);

        Assert.NotNull(result.Response);
        Assert.Equal(providerExpiry, result.Response.StartsAtUtc);
        Assert.Equal(providerExpiry.AddDays(10), result.Response.ExpiresAtUtc);
    }

    [Fact]
    public async Task ContiguousScheduledManualPremiumRemainsInTheCoverageTail()
    {
        await using var db = CreateDb();
        var adminUserId = await AddUserAsync(db);
        var targetUserId = await AddUserAsync(db);
        var now = DateTimeOffset.UtcNow;
        var currentEnd = now.AddDays(5);
        var scheduledEnd = currentEnd.AddDays(10);
        await AddEntitlementAsync(db, targetUserId, currentEnd, SubscriptionConstants.Entitlements.StatusActive);
        await AddEntitlementAsync(
            db,
            targetUserId,
            scheduledEnd,
            SubscriptionConstants.Entitlements.StatusActive,
            startsAtUtc: currentEnd);

        var result = await GrantAsync(db, adminUserId, targetUserId, 3);

        Assert.NotNull(result.Response);
        Assert.Equal(scheduledEnd, result.Response.StartsAtUtc);
        Assert.Equal(scheduledEnd.AddDays(3), result.Response.ExpiresAtUtc);
    }

    [Fact]
    public async Task InapplicableEntitlementsDoNotAffectTheGrantStart()
    {
        await using var db = CreateDb();
        var adminUserId = await AddUserAsync(db);
        var targetUserId = await AddUserAsync(db);
        var otherUserId = await AddUserAsync(db);
        var beforeGrant = DateTimeOffset.UtcNow;
        await AddEntitlementAsync(db, targetUserId, beforeGrant.AddDays(-1), SubscriptionConstants.Entitlements.StatusActive);
        await AddEntitlementAsync(db, targetUserId, beforeGrant.AddDays(50), SubscriptionConstants.Entitlements.StatusRevoked);
        await AddEntitlementAsync(db, targetUserId, beforeGrant.AddDays(60), SubscriptionConstants.Entitlements.StatusInactive);
        await AddEntitlementAsync(db, otherUserId, beforeGrant.AddDays(70), SubscriptionConstants.Entitlements.StatusActive);

        var result = await GrantAsync(db, adminUserId, targetUserId, 5);
        var afterGrant = DateTimeOffset.UtcNow;

        Assert.NotNull(result.Response);
        Assert.InRange(result.Response.StartsAtUtc, beforeGrant, afterGrant);
        Assert.Equal(result.Response.StartsAtUtc.AddDays(5), result.Response.ExpiresAtUtc);
    }

    private static Task<AdminManualPremiumGrantResult> GrantAsync(
        AppDbContext db,
        Guid adminUserId,
        Guid targetUserId,
        int durationDays) =>
        new AdminPremiumGrantService(db, new AdminAuditService(db)).GrantPremiumAsync(
            adminUserId,
            targetUserId,
            new AdminManualPremiumGrantRequest { DurationDays = durationDays, Reason = "billing regression test" },
            TestContext.Current.CancellationToken);

    private static async Task AddTrialAsync(AppDbContext db, Guid userId, DateTimeOffset expiresAtUtc)
    {
        db.TrialGrants.Add(new TrialGrantEntity
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            GrantedAtUtc = DateTimeOffset.UtcNow.AddDays(-1),
            ExpiresAtUtc = expiresAtUtc,
            SourcePlatform = "test",
            Status = SubscriptionConstants.Entitlements.StatusActive,
            CreatedAt = DateTimeOffset.UtcNow.AddDays(-1)
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    private static async Task AddEntitlementAsync(
        AppDbContext db,
        Guid userId,
        DateTimeOffset expiresAtUtc,
        string status,
        string source = SubscriptionConstants.Entitlements.SourceManualAdmin,
        DateTimeOffset? startsAtUtc = null)
    {
        var now = DateTimeOffset.UtcNow;
        db.Entitlements.Add(new EntitlementEntity
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            PlanId = SubscriptionConstants.Plans.PremiumPlanId,
            EntitlementType = SubscriptionConstants.Entitlements.PremiumAccessType,
            Source = source,
            Status = status,
            StartsAtUtc = startsAtUtc ?? now.AddDays(-1),
            ExpiresAtUtc = expiresAtUtc,
            CreatedAt = now,
            UpdatedAt = now
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    private static async Task<Guid> AddUserAsync(AppDbContext db)
    {
        var id = Guid.NewGuid();
        db.Users.Add(new UserEntity
        {
            Id = id,
            Email = $"{id:N}@example.test",
            PasswordHash = "hash",
            Status = "active",
            CreatedAt = DateTimeOffset.UtcNow
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return id;
    }

    private static AppDbContext CreateDb() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options);
}
