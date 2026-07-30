using EnglishVoiceTutor.Api.Constants;
using EnglishVoiceTutor.Api.Data;
using EnglishVoiceTutor.Api.Data.Entities;
using EnglishVoiceTutor.Api.Options;
using EnglishVoiceTutor.Api.Services.Subscriptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace EnglishVoiceTutor.Api.Tests.Services;

public sealed class SubscriptionStatusServiceTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;

    [Fact]
    public async Task PaddleOnlyStatusIsUnchanged()
    {
        await using var db = CreateDb();
        var userId = await AddUserAsync(db);
        var paddle = await AddSubscriptionAsync(db, userId, SubscriptionConstants.BillingProviders.Paddle, Now.AddDays(30));
        await AddEntitlementAsync(db, userId, paddle.Id, Now.AddDays(30));

        var status = await GetStatusAsync(db, userId);

        Assert.True(status.PremiumActive);
        Assert.Equal(SubscriptionConstants.BillingProviders.Paddle, status.BillingProvider);
        Assert.Equal(Now.AddDays(30), status.PremiumEntitlementExpiresAtUtc);
    }

    [Fact]
    public async Task LinkedGooglePlayEntitlementSelectsGooglePlayMetadata()
    {
        await using var db = CreateDb();
        var userId = await AddUserAsync(db);
        var google = await AddSubscriptionAsync(db, userId, SubscriptionConstants.BillingProviders.GooglePlay, Now.AddDays(30), cancelAtPeriodEnd: true);
        await AddEntitlementAsync(db, userId, google.Id, Now.AddDays(30));

        var status = await GetStatusAsync(db, userId);

        Assert.Equal(SubscriptionConstants.BillingProviders.GooglePlay, status.BillingProvider);
        Assert.True(status.CancelAtPeriodEnd);
        Assert.Equal(SubscriptionConstants.SubscriptionStatuses.Active, status.SubscriptionStatus);
    }

    [Fact]
    public async Task LongerPaddleCoverageBeatsShorterNewerGoogleCoverage()
    {
        await using var db = CreateDb();
        var userId = await AddUserAsync(db);
        var paddle = await AddSubscriptionAsync(db, userId, SubscriptionConstants.BillingProviders.Paddle, Now.AddDays(40));
        var google = await AddSubscriptionAsync(db, userId, SubscriptionConstants.BillingProviders.GooglePlay, Now.AddDays(10), updatedAt: Now.AddDays(1));
        await AddEntitlementAsync(db, userId, paddle.Id, Now.AddDays(40));
        await AddEntitlementAsync(db, userId, google.Id, Now.AddDays(10));

        var status = await GetStatusAsync(db, userId);

        Assert.Equal(SubscriptionConstants.BillingProviders.Paddle, status.BillingProvider);
        Assert.Equal(Now.AddDays(40), status.PaidAccessUntilUtc);
        Assert.Equal(Now.AddDays(40), status.PremiumCoverageEndsAtUtc);
    }

    [Fact]
    public async Task LongerGooglePlayCoverageBeatsShorterPaddleCoverage()
    {
        await using var db = CreateDb();
        var userId = await AddUserAsync(db);
        var paddle = await AddSubscriptionAsync(db, userId, SubscriptionConstants.BillingProviders.Paddle, Now.AddDays(10));
        var google = await AddSubscriptionAsync(db, userId, SubscriptionConstants.BillingProviders.GooglePlay, Now.AddDays(40));
        await AddEntitlementAsync(db, userId, paddle.Id, Now.AddDays(10));
        await AddEntitlementAsync(db, userId, google.Id, Now.AddDays(40));

        var status = await GetStatusAsync(db, userId);

        Assert.Equal(SubscriptionConstants.BillingProviders.GooglePlay, status.BillingProvider);
        Assert.Equal(Now.AddDays(40), status.PremiumEntitlementExpiresAtUtc);
    }

    [Fact]
    public async Task EqualCoverageDoesNotFlipToNewerGooglePlaySubscription()
    {
        await using var db = CreateDb();
        var userId = await AddUserAsync(db);
        var expiry = Now.AddDays(30);
        var paddle = await AddSubscriptionAsync(db, userId, SubscriptionConstants.BillingProviders.Paddle, expiry, createdAt: Now.AddDays(-2));
        var google = await AddSubscriptionAsync(db, userId, SubscriptionConstants.BillingProviders.GooglePlay, expiry, updatedAt: Now.AddDays(2));
        await AddEntitlementAsync(db, userId, paddle.Id, expiry, createdAt: Now.AddDays(-2));
        await AddEntitlementAsync(db, userId, google.Id, expiry, createdAt: Now.AddDays(-1));

        var status = await GetStatusAsync(db, userId);

        Assert.Equal(SubscriptionConstants.BillingProviders.Paddle, status.BillingProvider);
    }

    [Fact]
    public async Task OpenEndedPaddleIsNotReplacedByFiniteGooglePlay()
    {
        await using var db = CreateDb();
        var userId = await AddUserAsync(db);
        var paddle = await AddSubscriptionAsync(db, userId, SubscriptionConstants.BillingProviders.Paddle, null);
        var google = await AddSubscriptionAsync(db, userId, SubscriptionConstants.BillingProviders.GooglePlay, Now.AddDays(30));
        await AddEntitlementAsync(db, userId, paddle.Id, null);
        await AddEntitlementAsync(db, userId, google.Id, Now.AddDays(30));

        var status = await GetStatusAsync(db, userId);

        Assert.Equal(SubscriptionConstants.BillingProviders.Paddle, status.BillingProvider);
        Assert.Null(status.PaidAccessUntilUtc);
    }

    [Fact]
    public async Task OpenEndedLinkedGooglePlayWinsOverFinitePaddle()
    {
        await using var db = CreateDb();
        var userId = await AddUserAsync(db);
        var paddle = await AddSubscriptionAsync(db, userId, SubscriptionConstants.BillingProviders.Paddle, Now.AddDays(30));
        var google = await AddSubscriptionAsync(db, userId, SubscriptionConstants.BillingProviders.GooglePlay, null);
        await AddEntitlementAsync(db, userId, paddle.Id, Now.AddDays(30));
        await AddEntitlementAsync(db, userId, google.Id, null);

        var status = await GetStatusAsync(db, userId);

        Assert.Equal(SubscriptionConstants.BillingProviders.GooglePlay, status.BillingProvider);
        Assert.Null(status.PremiumEntitlementExpiresAtUtc);
    }

    [Fact]
    public async Task ExpiredAndFutureGooglePlayEntitlementsDoNotAffectCurrentPaddle()
    {
        await using var db = CreateDb();
        var userId = await AddUserAsync(db);
        var paddle = await AddSubscriptionAsync(db, userId, SubscriptionConstants.BillingProviders.Paddle, Now.AddDays(30));
        var expiredGoogle = await AddSubscriptionAsync(db, userId, SubscriptionConstants.BillingProviders.GooglePlay, Now.AddDays(-1));
        var futureGoogle = await AddSubscriptionAsync(db, userId, SubscriptionConstants.BillingProviders.GooglePlay, Now.AddDays(60));
        await AddEntitlementAsync(db, userId, paddle.Id, Now.AddDays(30));
        await AddEntitlementAsync(db, userId, expiredGoogle.Id, Now.AddDays(-1), startsAt: Now.AddDays(-30));
        await AddEntitlementAsync(db, userId, futureGoogle.Id, Now.AddDays(60), startsAt: Now.AddDays(1));

        var status = await GetStatusAsync(db, userId);

        Assert.Equal(SubscriptionConstants.BillingProviders.Paddle, status.BillingProvider);
        Assert.Equal(Now.AddDays(30), status.PremiumEntitlementExpiresAtUtc);
    }

    [Fact]
    public async Task OrphanGooglePlaySubscriptionDoesNotAffectPremiumOrProviderDisplay()
    {
        await using var db = CreateDb();
        var userId = await AddUserAsync(db);
        await AddSubscriptionAsync(db, userId, SubscriptionConstants.BillingProviders.GooglePlay, Now.AddDays(30));

        var status = await GetStatusAsync(db, userId);

        Assert.False(status.PremiumActive);
        Assert.Equal(SubscriptionConstants.BillingProviders.None, status.BillingProvider);
        Assert.Equal(SubscriptionConstants.Plans.FreePlanId, status.PlanId);
    }

    [Fact]
    public async Task WinningEntitlementUsesOnlyItsLinkedSubscriptionAndIgnoresOtherUsers()
    {
        await using var db = CreateDb();
        var userId = await AddUserAsync(db);
        var otherUserId = await AddUserAsync(db);
        var paddle = await AddSubscriptionAsync(db, userId, SubscriptionConstants.BillingProviders.Paddle, Now.AddDays(20), cancelAtPeriodEnd: true);
        var otherGoogle = await AddSubscriptionAsync(db, otherUserId, SubscriptionConstants.BillingProviders.GooglePlay, Now.AddDays(90));
        await AddEntitlementAsync(db, userId, paddle.Id, Now.AddDays(20));
        await AddEntitlementAsync(db, otherUserId, otherGoogle.Id, Now.AddDays(90));

        var status = await GetStatusAsync(db, userId);

        Assert.Equal(SubscriptionConstants.BillingProviders.Paddle, status.BillingProvider);
        Assert.True(status.CancelAtPeriodEnd);
        Assert.Equal(Now.AddDays(20), status.CurrentPeriodEndUtc);
    }

    [Fact]
    public async Task UnscopedLegacyPaddleEntitlementUsesMatchingPaddleSnapshotInsteadOfNewerGooglePlay()
    {
        await using var db = CreateDb();
        var userId = await AddUserAsync(db);
        var expiry = Now.AddDays(30);
        await AddSubscriptionAsync(db, userId, SubscriptionConstants.BillingProviders.Paddle, expiry, createdAt: Now.AddDays(-2));
        await AddSubscriptionAsync(db, userId, SubscriptionConstants.BillingProviders.GooglePlay, expiry, updatedAt: Now.AddDays(1));
        await AddEntitlementAsync(db, userId, null, expiry, createdAt: Now.AddDays(-2));

        var status = await GetStatusAsync(db, userId);

        Assert.True(status.PremiumActive);
        Assert.Equal(SubscriptionConstants.BillingProviders.Paddle, status.BillingProvider);
    }

    [Fact]
    public async Task OldUnscopedEntitlementWithOnlyNewerShorterGooglePlaySnapshotExposesNoProviderMetadata()
    {
        await using var db = CreateDb();
        var userId = await AddUserAsync(db);
        var entitlementCreatedAt = Now.AddDays(-10);
        await AddSubscriptionAsync(db, userId, SubscriptionConstants.BillingProviders.GooglePlay, Now.AddDays(5), cancelAtPeriodEnd: true, createdAt: Now.AddDays(-1));
        await AddEntitlementAsync(db, userId, null, Now.AddDays(30), createdAt: entitlementCreatedAt);

        var status = await GetStatusAsync(db, userId);

        Assert.True(status.PremiumActive);
        Assert.Equal(Now.AddDays(30), status.PremiumEntitlementExpiresAtUtc);
        Assert.Equal(SubscriptionConstants.BillingProviders.None, status.BillingProvider);
        Assert.False(status.ProviderSubscriptionPresent);
        Assert.False(status.CancelAtPeriodEnd);
    }

    [Fact]
    public async Task HistoricalPaddleSnapshotWinsLegacyFallbackOverNewerGooglePlay()
    {
        await using var db = CreateDb();
        var userId = await AddUserAsync(db);
        var entitlementCreatedAt = Now.AddDays(-10);
        await AddSubscriptionAsync(db, userId, SubscriptionConstants.BillingProviders.Paddle, Now.AddDays(5), createdAt: Now.AddDays(-11));
        await AddSubscriptionAsync(db, userId, SubscriptionConstants.BillingProviders.GooglePlay, Now.AddDays(10), createdAt: Now.AddDays(-1));
        await AddEntitlementAsync(db, userId, null, Now.AddDays(30), createdAt: entitlementCreatedAt);

        var status = await GetStatusAsync(db, userId);

        Assert.Equal(SubscriptionConstants.BillingProviders.Paddle, status.BillingProvider);
    }

    [Theory]
    [InlineData(SubscriptionConstants.BillingProviders.None)]
    [InlineData(SubscriptionConstants.BillingProviders.Manual)]
    [InlineData(SubscriptionConstants.BillingProviders.InternalTrial)]
    public async Task NonPaidProviderSnapshotsAreNeverUsedForUnscopedEntitlements(string provider)
    {
        await using var db = CreateDb();
        var userId = await AddUserAsync(db);
        var expiry = Now.AddDays(30);
        await AddSubscriptionAsync(db, userId, provider, expiry, cancelAtPeriodEnd: true, createdAt: Now.AddDays(-2));
        await AddEntitlementAsync(db, userId, null, expiry, createdAt: Now.AddDays(-1));

        var status = await GetStatusAsync(db, userId);

        Assert.True(status.PremiumActive);
        Assert.Equal(SubscriptionConstants.BillingProviders.None, status.BillingProvider);
        Assert.False(status.ProviderSubscriptionPresent);
        Assert.False(status.CancelAtPeriodEnd);
    }

    [Theory]
    [InlineData(SubscriptionConstants.BillingProviders.Paddle)]
    [InlineData(SubscriptionConstants.BillingProviders.GooglePlay)]
    public async Task LinkedSubscriptionMetadataRemainsExactEvenWhenItsPeriodIsShorterThanEntitlement(string provider)
    {
        await using var db = CreateDb();
        var userId = await AddUserAsync(db);
        var subscription = await AddSubscriptionAsync(db, userId, provider, Now.AddDays(5), cancelAtPeriodEnd: true);
        await AddEntitlementAsync(db, userId, subscription.Id, Now.AddDays(30));

        var status = await GetStatusAsync(db, userId);

        Assert.Equal(provider, status.BillingProvider);
        Assert.True(status.CancelAtPeriodEnd);
        Assert.Equal(Now.AddDays(30), status.PremiumEntitlementExpiresAtUtc);
        Assert.Equal(Now.AddDays(30), status.PaidAccessUntilUtc);
        Assert.Equal(Now.AddDays(30), status.PremiumCoverageEndsAtUtc);
    }

    [Theory]
    [InlineData(SubscriptionConstants.Entitlements.SourceManualAdmin)]
    [InlineData(SubscriptionConstants.Entitlements.SourceTrial)]
    public async Task ManualAndTrialEntitlementsRemainPremiumWithoutProviderSnapshot(string source)
    {
        await using var db = CreateDb();
        var userId = await AddUserAsync(db);
        await AddEntitlementAsync(db, userId, null, Now.AddDays(10), source: source);

        var status = await GetStatusAsync(db, userId);

        Assert.True(status.PremiumActive);
        Assert.Equal(SubscriptionConstants.BillingProviders.None, status.BillingProvider);
    }

    [Fact]
    public async Task TrialOnlyStatusRemainsUnchanged()
    {
        await using var db = CreateDb();
        var userId = await AddUserAsync(db);
        var trialEnd = Now.AddDays(7);
        await AddTrialAsync(db, userId, trialEnd);

        var status = await GetStatusAsync(db, userId);

        Assert.True(status.TrialActive);
        Assert.False(status.PremiumActive);
        Assert.Equal(trialEnd, status.TrialEndsAtUtc);
        Assert.Equal(trialEnd, status.PremiumCoverageEndsAtUtc);
        Assert.Equal(SubscriptionConstants.BillingProviders.None, status.BillingProvider);
        Assert.False(status.CanRequestCancelRenewal);
    }

    [Fact]
    public async Task ManualOnlyStatusRemainsUnchanged()
    {
        await using var db = CreateDb();
        var userId = await AddUserAsync(db);
        var manualEnd = Now.AddDays(30);
        await AddEntitlementAsync(db, userId, null, manualEnd, source: SubscriptionConstants.Entitlements.SourceManualAdmin);

        var status = await GetStatusAsync(db, userId);

        Assert.True(status.PremiumActive);
        Assert.False(status.TrialActive);
        Assert.Equal(manualEnd, status.PremiumCoverageEndsAtUtc);
        Assert.Equal(manualEnd, status.PaidAccessUntilUtc);
        Assert.Equal(SubscriptionConstants.BillingProviders.None, status.BillingProvider);
        Assert.False(status.ProviderSubscriptionPresent);
        Assert.False(status.HasActivePaidProviderSubscription);
        Assert.False(status.CanRequestCancelRenewal);
    }

    [Fact]
    public async Task TrialFollowedByFutureManualPremiumReturnsFinalManualExpiry()
    {
        await using var db = CreateDb();
        var userId = await AddUserAsync(db);
        var trialEnd = Now.AddDays(7);
        var manualEnd = trialEnd.AddDays(30);
        await AddTrialAsync(db, userId, trialEnd);
        await AddEntitlementAsync(
            db,
            userId,
            null,
            manualEnd,
            startsAt: trialEnd,
            source: SubscriptionConstants.Entitlements.SourceManualAdmin);

        var status = await GetStatusAsync(db, userId);

        Assert.True(status.TrialActive);
        Assert.Equal(trialEnd, status.TrialEndsAtUtc);
        Assert.True(status.HasFuturePremiumEntitlement);
        Assert.Equal(trialEnd, status.FuturePremiumStartsAtUtc);
        Assert.Equal(manualEnd, status.FuturePremiumExpiresAtUtc);
        Assert.Equal(manualEnd, status.PremiumEndsAtUtc);
        Assert.Equal(manualEnd, status.PremiumCoverageEndsAtUtc);
        Assert.Equal(SubscriptionConstants.BillingProviders.None, status.BillingProvider);
        Assert.False(status.ProviderSubscriptionPresent);
        Assert.False(status.HasActivePaidProviderSubscription);
        Assert.False(status.CanRequestCancelRenewal);
    }

    [Fact]
    public async Task TrialWithOverlappingManualPremiumReturnsLaterExpiryWithoutMutation()
    {
        await using var db = CreateDb();
        var userId = await AddUserAsync(db);
        var trialEnd = Now.AddDays(7);
        var manualStart = Now.AddDays(-1);
        var manualEnd = Now.AddDays(90);
        await AddTrialAsync(db, userId, trialEnd);
        await AddEntitlementAsync(
            db,
            userId,
            null,
            manualEnd,
            startsAt: manualStart,
            source: SubscriptionConstants.Entitlements.SourceManualAdmin);
        db.ChangeTracker.Clear();

        var status = await GetStatusAsync(db, userId);
        var storedManual = await db.Entitlements.AsNoTracking().SingleAsync(TestContext.Current.CancellationToken);

        Assert.True(status.PremiumActive);
        Assert.True(status.TrialActive);
        Assert.Equal(trialEnd, status.TrialEndsAtUtc);
        Assert.Equal(manualEnd, status.PremiumEntitlementExpiresAtUtc);
        Assert.Equal(manualEnd, status.PaidAccessUntilUtc);
        Assert.Equal(manualEnd, status.PremiumCoverageEndsAtUtc);
        Assert.Equal(manualStart, storedManual.StartsAtUtc);
        Assert.Equal(manualEnd, storedManual.ExpiresAtUtc);
        Assert.False(db.ChangeTracker.HasChanges());
    }

    private static async Task<EnglishVoiceTutor.Api.Contracts.Subscription.SubscriptionStatusResponse> GetStatusAsync(AppDbContext db, Guid userId) =>
        await new SubscriptionStatusService(db, Microsoft.Extensions.Options.Options.Create(new SubscriptionEnforcementOptions())).GetStatusAsync(userId, "test", TestContext.Current.CancellationToken);

    private static async Task<Guid> AddUserAsync(AppDbContext db)
    {
        var id = Guid.NewGuid();
        db.Users.Add(new UserEntity { Id = id, Email = $"{id:N}@example.test", PasswordHash = "hash", Status = "active", CreatedAt = Now });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return id;
    }

    private static async Task<SubscriptionEntity> AddSubscriptionAsync(AppDbContext db, Guid userId, string provider, DateTimeOffset? expiresAtUtc, bool cancelAtPeriodEnd = false, DateTimeOffset? createdAt = null, DateTimeOffset? updatedAt = null)
    {
        var subscription = new SubscriptionEntity
        {
            Id = Guid.NewGuid(), UserId = userId, PlanId = SubscriptionConstants.Plans.PremiumPlanId,
            Status = SubscriptionConstants.SubscriptionStatuses.Active, Provider = provider, ProviderSubscriptionId = Guid.NewGuid().ToString("N"),
            StartedAt = Now.AddDays(-1), CurrentPeriodEndUtc = expiresAtUtc, ExpiresAt = expiresAtUtc, CancelAtPeriodEnd = cancelAtPeriodEnd,
            CreatedAt = createdAt ?? Now, UpdatedAt = updatedAt ?? createdAt ?? Now
        };
        db.Subscriptions.Add(subscription);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return subscription;
    }

    private static async Task AddEntitlementAsync(AppDbContext db, Guid userId, Guid? subscriptionId, DateTimeOffset? expiresAtUtc, DateTimeOffset? startsAt = null, string? source = null, DateTimeOffset? createdAt = null)
    {
        var created = createdAt ?? Now;
        db.Entitlements.Add(new EntitlementEntity
        {
            Id = Guid.NewGuid(), UserId = userId, SubscriptionId = subscriptionId, PlanId = SubscriptionConstants.Plans.PremiumPlanId,
            EntitlementType = SubscriptionConstants.Entitlements.PremiumAccessType, Source = source ?? SubscriptionConstants.Entitlements.SourceProviderEvent,
            Status = SubscriptionConstants.Entitlements.StatusActive, StartsAtUtc = startsAt ?? Now.AddDays(-1), ExpiresAtUtc = expiresAtUtc,
            CreatedAt = created, UpdatedAt = created
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    private static async Task AddTrialAsync(AppDbContext db, Guid userId, DateTimeOffset expiresAtUtc)
    {
        db.TrialGrants.Add(new TrialGrantEntity
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            GrantedAtUtc = Now.AddDays(-1),
            ExpiresAtUtc = expiresAtUtc,
            SourcePlatform = "test",
            Status = SubscriptionConstants.Entitlements.StatusActive,
            CreatedAt = Now.AddDays(-1)
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    private static AppDbContext CreateDb() => new(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString("N")).Options);
}
