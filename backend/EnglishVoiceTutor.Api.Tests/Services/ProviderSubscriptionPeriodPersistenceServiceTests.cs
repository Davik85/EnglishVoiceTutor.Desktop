using EnglishVoiceTutor.Api.Constants;
using EnglishVoiceTutor.Api.Data;
using EnglishVoiceTutor.Api.Data.Entities;
using EnglishVoiceTutor.Api.Services.Billing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;

namespace EnglishVoiceTutor.Api.Tests.Services;

public sealed class ProviderSubscriptionPeriodPersistenceServiceTests
{
    [Fact]
    public async Task PaidPeriodCreatesEntitlementLinkedToExactSubscription()
    {
        await using var db = CreateDb();
        var fixture = await SeedSubscriptionAsync(db);

        var result = await CreateService(db).ApplyAsync(Request(fixture.UserId, fixture.SubscriptionId), TestContext.Current.CancellationToken);

        Assert.Equal(ProviderSubscriptionPeriodPersistenceResultCode.Applied, result.Code);
        var entitlement = await db.Entitlements.SingleAsync(TestContext.Current.CancellationToken);
        Assert.Equal(fixture.SubscriptionId, entitlement.SubscriptionId);
        Assert.Equal(SubscriptionConstants.Entitlements.SourceProviderEvent, entitlement.Source);
    }

    [Fact]
    public async Task SamePeriodIsIdempotentWithoutDuplicateEntitlement()
    {
        await using var db = CreateDb();
        var fixture = await SeedSubscriptionAsync(db);
        var service = CreateService(db);

        await service.ApplyAsync(Request(fixture.UserId, fixture.SubscriptionId), TestContext.Current.CancellationToken);
        var result = await service.ApplyAsync(Request(fixture.UserId, fixture.SubscriptionId), TestContext.Current.CancellationToken);

        Assert.Equal(ProviderSubscriptionPeriodPersistenceResultCode.AlreadyCurrent, result.Code);
        Assert.Equal(1, await db.Entitlements.CountAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task LaterExpiryExtendsOnlyTargetSubscriptionAndEntitlement()
    {
        await using var db = CreateDb();
        var fixture = await SeedSubscriptionAsync(db);
        var service = CreateService(db);
        await service.ApplyAsync(Request(fixture.UserId, fixture.SubscriptionId), TestContext.Current.CancellationToken);
        var laterExpiry = Expiry.AddDays(30);

        var result = await service.ApplyAsync(Request(fixture.UserId, fixture.SubscriptionId, expiry: laterExpiry), TestContext.Current.CancellationToken);

        Assert.Equal(ProviderSubscriptionPeriodPersistenceResultCode.Applied, result.Code);
        Assert.Equal(laterExpiry, (await db.Subscriptions.SingleAsync(TestContext.Current.CancellationToken)).CurrentPeriodEndUtc);
        Assert.Equal(laterExpiry, (await db.Entitlements.SingleAsync(TestContext.Current.CancellationToken)).ExpiresAtUtc);
    }

    [Fact]
    public async Task EarlierExpiryDoesNotShortenTargetRecords()
    {
        await using var db = CreateDb();
        var fixture = await SeedSubscriptionAsync(db);
        var service = CreateService(db);
        await service.ApplyAsync(Request(fixture.UserId, fixture.SubscriptionId), TestContext.Current.CancellationToken);

        var result = await service.ApplyAsync(Request(fixture.UserId, fixture.SubscriptionId, expiry: Expiry.AddDays(-1)), TestContext.Current.CancellationToken);

        Assert.Equal(ProviderSubscriptionPeriodPersistenceResultCode.AlreadyCurrent, result.Code);
        Assert.Equal(Expiry, (await db.Subscriptions.SingleAsync(TestContext.Current.CancellationToken)).CurrentPeriodEndUtc);
        Assert.Equal(Expiry, (await db.Entitlements.SingleAsync(TestContext.Current.CancellationToken)).ExpiresAtUtc);
    }

    [Fact]
    public async Task IncomingPeriodDoesNotShortenExistingLaterSubscriptionExpiry()
    {
        await using var db = CreateDb();
        var fixture = await SeedSubscriptionAsync(db);
        var subscription = await db.Subscriptions.SingleAsync(TestContext.Current.CancellationToken);
        subscription.ExpiresAt = Expiry.AddDays(10);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await CreateService(db).ApplyAsync(Request(fixture.UserId, fixture.SubscriptionId), TestContext.Current.CancellationToken);

        Assert.Equal(ProviderSubscriptionPeriodPersistenceResultCode.Applied, result.Code);
        Assert.Equal(Expiry.AddDays(10), (await db.Subscriptions.SingleAsync(TestContext.Current.CancellationToken)).ExpiresAt);
    }

    [Fact]
    public async Task ApplyingGoogleShapedSubscriptionLeavesPaddleSubscriptionAndEntitlementUntouched()
    {
        await using var db = CreateDb();
        var userId = Guid.NewGuid();
        var google = await SeedSubscriptionAsync(db, userId, SubscriptionConstants.BillingProviders.GooglePlay, "google-subscription");
        var paddle = await SeedSubscriptionAsync(db, userId, SubscriptionConstants.BillingProviders.Paddle, "paddle-subscription");
        var paddleEntitlement = new EntitlementEntity { Id = Guid.NewGuid(), UserId = userId, SubscriptionId = paddle.SubscriptionId, PlanId = SubscriptionConstants.Plans.PremiumPlanId, EntitlementType = SubscriptionConstants.Entitlements.PremiumAccessType, Source = SubscriptionConstants.Entitlements.SourceProviderEvent, Status = SubscriptionConstants.Entitlements.StatusActive, StartsAtUtc = Start, ExpiresAtUtc = Expiry.AddDays(10), CreatedAt = Start, UpdatedAt = Start };
        db.Entitlements.Add(paddleEntitlement);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        await CreateService(db).ApplyAsync(Request(userId, google.SubscriptionId), TestContext.Current.CancellationToken);

        Assert.Null((await db.Subscriptions.SingleAsync(item => item.Id == paddle.SubscriptionId, TestContext.Current.CancellationToken)).CurrentPeriodEndUtc);
        var unchanged = await db.Entitlements.SingleAsync(item => item.Id == paddleEntitlement.Id, TestContext.Current.CancellationToken);
        Assert.Equal(Expiry.AddDays(10), unchanged.ExpiresAtUtc);
        Assert.Equal(paddle.SubscriptionId, unchanged.SubscriptionId);
    }

    [Fact]
    public async Task UnscopedEntitlementIsNotSelectedOrModified()
    {
        await using var db = CreateDb();
        var fixture = await SeedSubscriptionAsync(db);
        var unscoped = new EntitlementEntity { Id = Guid.NewGuid(), UserId = fixture.UserId, SubscriptionId = null, PlanId = SubscriptionConstants.Plans.PremiumPlanId, EntitlementType = SubscriptionConstants.Entitlements.PremiumAccessType, Source = SubscriptionConstants.Entitlements.SourceProviderEvent, Status = SubscriptionConstants.Entitlements.StatusActive, StartsAtUtc = Start, ExpiresAtUtc = Expiry.AddDays(10), CreatedAt = Start, UpdatedAt = Start };
        db.Entitlements.Add(unscoped);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        await CreateService(db).ApplyAsync(Request(fixture.UserId, fixture.SubscriptionId), TestContext.Current.CancellationToken);

        Assert.Equal(Expiry.AddDays(10), (await db.Entitlements.SingleAsync(item => item.Id == unscoped.Id, TestContext.Current.CancellationToken)).ExpiresAtUtc);
        Assert.Equal(2, await db.Entitlements.CountAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task OpenEndedTargetEntitlementIsPreservedWhileSubscriptionPeriodUpdates()
    {
        await using var db = CreateDb();
        var fixture = await SeedSubscriptionAsync(db);
        var targetEntitlement = new EntitlementEntity { Id = Guid.NewGuid(), UserId = fixture.UserId, SubscriptionId = fixture.SubscriptionId, PlanId = SubscriptionConstants.Plans.PremiumPlanId, EntitlementType = SubscriptionConstants.Entitlements.PremiumAccessType, Source = SubscriptionConstants.Entitlements.SourceProviderEvent, Status = SubscriptionConstants.Entitlements.StatusActive, StartsAtUtc = Start, ExpiresAtUtc = null, CreatedAt = Start, UpdatedAt = Start };
        var unrelatedEntitlement = new EntitlementEntity { Id = Guid.NewGuid(), UserId = fixture.UserId, SubscriptionId = null, PlanId = SubscriptionConstants.Plans.PremiumPlanId, EntitlementType = SubscriptionConstants.Entitlements.PremiumAccessType, Source = SubscriptionConstants.Entitlements.SourceProviderEvent, Status = SubscriptionConstants.Entitlements.StatusActive, StartsAtUtc = Start, ExpiresAtUtc = Expiry.AddDays(10), CreatedAt = Start, UpdatedAt = Start };
        db.Entitlements.AddRange(targetEntitlement, unrelatedEntitlement);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await CreateService(db).ApplyAsync(Request(fixture.UserId, fixture.SubscriptionId), TestContext.Current.CancellationToken);

        Assert.Equal(ProviderSubscriptionPeriodPersistenceResultCode.Applied, result.Code);
        Assert.Equal(Expiry, (await db.Subscriptions.SingleAsync(item => item.Id == fixture.SubscriptionId, TestContext.Current.CancellationToken)).CurrentPeriodEndUtc);
        Assert.Null((await db.Entitlements.SingleAsync(item => item.Id == targetEntitlement.Id, TestContext.Current.CancellationToken)).ExpiresAtUtc);
        Assert.Equal(Expiry.AddDays(10), (await db.Entitlements.SingleAsync(item => item.Id == unrelatedEntitlement.Id, TestContext.Current.CancellationToken)).ExpiresAtUtc);
        Assert.Equal(2, await db.Entitlements.CountAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task OtherUserSubscriptionIsRejectedWithoutChanges()
    {
        await using var db = CreateDb();
        var fixture = await SeedSubscriptionAsync(db);

        var result = await CreateService(db).ApplyAsync(Request(Guid.NewGuid(), fixture.SubscriptionId), TestContext.Current.CancellationToken);

        Assert.Equal(ProviderSubscriptionPeriodPersistenceResultCode.SubscriptionOwnershipConflict, result.Code);
        Assert.Empty(await db.Entitlements.ToListAsync(TestContext.Current.CancellationToken));
    }

    [Theory]
    [InlineData("manual")]
    [InlineData("internal_trial")]
    [InlineData("none")]
    public async Task UnsupportedProvidersAreRejectedWithoutChanges(string provider)
    {
        await using var db = CreateDb();
        var fixture = await SeedSubscriptionAsync(db, provider: provider);

        var result = await CreateService(db).ApplyAsync(Request(fixture.UserId, fixture.SubscriptionId), TestContext.Current.CancellationToken);

        Assert.Equal(ProviderSubscriptionPeriodPersistenceResultCode.UnsupportedSubscription, result.Code);
        Assert.Empty(await db.Entitlements.ToListAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task TestPurchaseIsRejectedWithoutPersistence()
    {
        await using var db = CreateDb();
        var fixture = await SeedSubscriptionAsync(db);

        var result = await CreateService(db).ApplyAsync(Request(fixture.UserId, fixture.SubscriptionId, isTestPurchase: true), TestContext.Current.CancellationToken);

        Assert.Equal(ProviderSubscriptionPeriodPersistenceResultCode.TestPurchaseNotSupported, result.Code);
        Assert.Null((await db.Subscriptions.SingleAsync(TestContext.Current.CancellationToken)).CurrentPeriodEndUtc);
        Assert.Empty(await db.Entitlements.ToListAsync(TestContext.Current.CancellationToken));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task InvalidOrNonUtcPeriodsAreRejectedWithoutPersistence(bool reversed)
    {
        await using var db = CreateDb();
        var fixture = await SeedSubscriptionAsync(db);
        var request = reversed
            ? Request(fixture.UserId, fixture.SubscriptionId, start: Expiry, expiry: Start)
            : Request(fixture.UserId, fixture.SubscriptionId, start: new DateTimeOffset(2026, 7, 27, 12, 0, 0, TimeSpan.FromHours(2)));

        var result = await CreateService(db).ApplyAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(ProviderSubscriptionPeriodPersistenceResultCode.InvalidInput, result.Code);
        Assert.Empty(await db.Entitlements.ToListAsync(TestContext.Current.CancellationToken));
    }

    private static readonly DateTimeOffset Start = new(2026, 7, 27, 10, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Expiry = Start.AddDays(30);
    private static IProviderSubscriptionPeriodPersistenceService CreateService(AppDbContext db) => new ProviderSubscriptionPeriodPersistenceService(db, NullLogger<ProviderSubscriptionPeriodPersistenceService>.Instance);
    private static ProviderSubscriptionPeriodPersistenceRequest Request(Guid userId, Guid subscriptionId, DateTimeOffset? start = null, DateTimeOffset? expiry = null, bool isTestPurchase = false) => new(userId, subscriptionId, "verified-product", start ?? Start, expiry ?? Expiry, isTestPurchase);

    private static async Task<Fixture> SeedSubscriptionAsync(AppDbContext db, Guid? userId = null, string provider = SubscriptionConstants.BillingProviders.GooglePlay, string providerSubscriptionId = "provider-subscription")
    {
        var actualUserId = userId ?? Guid.NewGuid();
        if (!await db.Users.AnyAsync(item => item.Id == actualUserId, TestContext.Current.CancellationToken)) db.Users.Add(new UserEntity { Id = actualUserId, Email = $"{actualUserId:N}@example.test", PasswordHash = "hash", Status = "active", CreatedAt = Start });
        var subscriptionId = Guid.NewGuid();
        db.Subscriptions.Add(new SubscriptionEntity { Id = subscriptionId, UserId = actualUserId, PlanId = SubscriptionConstants.Plans.PremiumPlanId, Status = SubscriptionConstants.SubscriptionStatuses.Active, Provider = provider, ProviderSubscriptionId = providerSubscriptionId, StartedAt = Start, CreatedAt = Start, UpdatedAt = Start });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return new Fixture(actualUserId, subscriptionId);
    }

    private static AppDbContext CreateDb() => new(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString("N")).ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning)).Options);
    private sealed record Fixture(Guid UserId, Guid SubscriptionId);
}
