using System.Text.Json;
using EnglishVoiceTutor.Api.Constants;
using EnglishVoiceTutor.Api.Data;
using EnglishVoiceTutor.Api.Data.Entities;
using EnglishVoiceTutor.Api.Options;
using EnglishVoiceTutor.Api.Services.Subscriptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace EnglishVoiceTutor.Api.Tests.Services;

public sealed class GooglePlayPurchaseGateTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;

    [Fact]
    public async Task TrialOnlyAllowsGooglePlayPurchase()
    {
        await using var db = CreateDb();
        var userId = await AddUserAsync(db);
        db.TrialGrants.Add(new TrialGrantEntity
        {
            Id = Guid.NewGuid(), UserId = userId, GrantedAtUtc = Now.AddDays(-1), ExpiresAtUtc = Now.AddDays(7),
            SourcePlatform = "test", Status = SubscriptionConstants.Entitlements.StatusActive, CreatedAt = Now.AddDays(-1)
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var status = await GetStatusAsync(db, userId);

        Assert.True(status.GooglePlayPurchaseAllowed);
        Assert.Equal(SubscriptionConstants.GooglePlayPurchaseGate.None, status.GooglePlayPurchaseBlockReasonCode);
        Assert.Null(status.GooglePlayPurchaseBlockingProvider);
    }

    [Fact]
    public async Task ManualPremiumOnlyAllowsGooglePlayPurchase()
    {
        await using var db = CreateDb();
        var userId = await AddUserAsync(db);
        await AddEntitlementAsync(db, userId, null, Now.AddDays(30), SubscriptionConstants.Entitlements.SourceManualAdmin);

        var status = await GetStatusAsync(db, userId);

        Assert.True(status.GooglePlayPurchaseAllowed);
        Assert.True(status.PremiumActive);
        Assert.Equal(Now.AddDays(30), status.PremiumCoverageEndsAtUtc);
    }

    [Fact]
    public async Task TrialAndScheduledManualPremiumAllowGooglePlayPurchaseWithoutChangingCoverage()
    {
        await using var db = CreateDb();
        var userId = await AddUserAsync(db);
        var trialEnd = Now.AddDays(7);
        var manualEnd = trialEnd.AddDays(30);
        db.TrialGrants.Add(new TrialGrantEntity
        {
            Id = Guid.NewGuid(), UserId = userId, GrantedAtUtc = Now.AddDays(-1), ExpiresAtUtc = trialEnd,
            SourcePlatform = "test", Status = SubscriptionConstants.Entitlements.StatusActive, CreatedAt = Now.AddDays(-1)
        });
        await AddEntitlementAsync(db, userId, null, manualEnd, SubscriptionConstants.Entitlements.SourceManualAdmin, trialEnd);

        var status = await GetStatusAsync(db, userId);

        Assert.True(status.GooglePlayPurchaseAllowed);
        Assert.Equal(manualEnd, status.PremiumEndsAtUtc);
        Assert.Equal(manualEnd, status.PremiumCoverageEndsAtUtc);
    }

    [Theory]
    [InlineData(SubscriptionConstants.BillingProviders.Paddle, SubscriptionConstants.SubscriptionStatuses.Active)]
    [InlineData(SubscriptionConstants.BillingProviders.Paddle, SubscriptionConstants.SubscriptionStatuses.PastDue)]
    [InlineData(SubscriptionConstants.BillingProviders.GooglePlay, SubscriptionConstants.SubscriptionStatuses.Active)]
    [InlineData(SubscriptionConstants.BillingProviders.GooglePlay, SubscriptionConstants.SubscriptionStatuses.PastDue)]
    [InlineData(SubscriptionConstants.BillingProviders.GooglePlay, SubscriptionConstants.SubscriptionStatuses.Paused)]
    public async Task RecoverableExternalRenewalOwnerBlocksGooglePlayPurchase(string provider, string subscriptionStatus)
    {
        await using var db = CreateDb();
        var userId = await AddUserAsync(db);
        await AddSubscriptionAsync(db, userId, provider, subscriptionStatus);

        var status = await GetStatusAsync(db, userId);

        Assert.False(status.GooglePlayPurchaseAllowed);
        Assert.Equal(SubscriptionConstants.GooglePlayPurchaseGate.ExternalAutoRenewActive, status.GooglePlayPurchaseBlockReasonCode);
        Assert.Equal(provider, status.GooglePlayPurchaseBlockingProvider);
    }

    [Fact]
    public async Task AuthoritativePaddleFutureScheduledCancellationAllowsPurchaseAndPreservesPaidCoverage()
    {
        await using var db = CreateDb();
        var userId = await AddUserAsync(db);
        var periodEnd = Now.AddDays(20);
        var paddle = await AddSubscriptionAsync(
            db,
            userId,
            SubscriptionConstants.BillingProviders.Paddle,
            SubscriptionConstants.SubscriptionStatuses.Active,
            cancelAtPeriodEnd: true,
            scheduledChangeAction: SubscriptionConstants.ScheduledChangeActions.Cancel,
            scheduledChangeEffectiveAtUtc: periodEnd,
            expiresAtUtc: periodEnd,
            lastProviderEventId: "evt_cancel");
        await AddPaddleBillingEventAsync(db, paddle, "evt_cancel", periodEnd, scheduledChangeSnapshotComplete: true);
        await AddEntitlementAsync(db, userId, paddle.Id, periodEnd, SubscriptionConstants.Entitlements.SourceProviderEvent);

        var status = await GetStatusAsync(db, userId);

        Assert.True(status.GooglePlayPurchaseAllowed);
        Assert.Equal(periodEnd, status.PremiumEndsAtUtc);
        Assert.Equal(periodEnd, status.PremiumCoverageEndsAtUtc);
        Assert.Equal(SubscriptionConstants.RenewalStatuses.CancellationScheduled, status.RenewalStatus);
    }

    [Fact]
    public async Task PaddleTrialingWithAuthoritativeScheduledCancellationAllowsPurchase()
    {
        await using var db = CreateDb();
        var userId = await AddUserAsync(db);
        var paddle = await AddSubscriptionAsync(
            db,
            userId,
            SubscriptionConstants.BillingProviders.Paddle,
            SubscriptionConstants.SubscriptionStatuses.Trialing,
            cancelAtPeriodEnd: true,
            scheduledChangeAction: SubscriptionConstants.ScheduledChangeActions.Cancel,
            scheduledChangeEffectiveAtUtc: Now.AddDays(7),
            lastProviderEventId: "evt_trial_cancel");
        await AddPaddleBillingEventAsync(db, paddle, "evt_trial_cancel", Now.AddDays(7), scheduledChangeSnapshotComplete: true);

        var status = await GetStatusAsync(db, userId);

        Assert.True(status.GooglePlayPurchaseAllowed);
        Assert.Equal(SubscriptionConstants.GooglePlayPurchaseGate.None, status.GooglePlayPurchaseBlockReasonCode);
    }

    [Theory]
    [InlineData(SubscriptionConstants.SubscriptionStatuses.PastDue)]
    [InlineData(SubscriptionConstants.SubscriptionStatuses.Paused)]
    public async Task PaddleRecoveryStateWithAuthoritativeScheduledCancellationStillBlocks(string subscriptionStatus)
    {
        await using var db = CreateDb();
        var userId = await AddUserAsync(db);
        var paddle = await AddSubscriptionAsync(
            db,
            userId,
            SubscriptionConstants.BillingProviders.Paddle,
            subscriptionStatus,
            cancelAtPeriodEnd: true,
            scheduledChangeAction: SubscriptionConstants.ScheduledChangeActions.Cancel,
            scheduledChangeEffectiveAtUtc: Now.AddDays(10),
            lastProviderEventId: "evt_recovery_cancel");
        await AddPaddleBillingEventAsync(db, paddle, "evt_recovery_cancel", Now.AddDays(10), scheduledChangeSnapshotComplete: true);

        var status = await GetStatusAsync(db, userId);

        Assert.False(status.GooglePlayPurchaseAllowed);
        Assert.Equal(SubscriptionConstants.GooglePlayPurchaseGate.ExternalAutoRenewActive, status.GooglePlayPurchaseBlockReasonCode);
        Assert.Equal(SubscriptionConstants.BillingProviders.Paddle, status.GooglePlayPurchaseBlockingProvider);
    }

    [Fact]
    public async Task LegacyPaddleStickyCancellationWithoutCompleteEventEvidenceFailsClosed()
    {
        await using var db = CreateDb();
        var userId = await AddUserAsync(db);
        await AddSubscriptionAsync(
            db,
            userId,
            SubscriptionConstants.BillingProviders.Paddle,
            SubscriptionConstants.SubscriptionStatuses.Active,
            cancelAtPeriodEnd: true,
            scheduledChangeAction: SubscriptionConstants.ScheduledChangeActions.Cancel,
            scheduledChangeEffectiveAtUtc: Now.AddDays(10));

        var status = await GetStatusAsync(db, userId);

        Assert.False(status.GooglePlayPurchaseAllowed);
        Assert.Equal(SubscriptionConstants.GooglePlayPurchaseGate.RenewalOwnershipAmbiguous, status.GooglePlayPurchaseBlockReasonCode);
    }

    [Fact]
    public async Task LegacyRemovalEventWithoutCompleteSnapshotMarkerCannotProveStickyCancellation()
    {
        await using var db = CreateDb();
        var userId = await AddUserAsync(db);
        var paddle = await AddSubscriptionAsync(
            db,
            userId,
            SubscriptionConstants.BillingProviders.Paddle,
            SubscriptionConstants.SubscriptionStatuses.Active,
            cancelAtPeriodEnd: true,
            scheduledChangeAction: SubscriptionConstants.ScheduledChangeActions.Cancel,
            scheduledChangeEffectiveAtUtc: Now.AddDays(10),
            lastProviderEventId: "evt_legacy_remove");
        await AddPaddleBillingEventAsync(
            db,
            paddle,
            "evt_legacy_remove",
            scheduledChangeEffectiveAtUtc: null,
            scheduledChangeSnapshotComplete: null,
            scheduledChangeAction: null);

        var status = await GetStatusAsync(db, userId);

        Assert.False(status.GooglePlayPurchaseAllowed);
        Assert.Equal(SubscriptionConstants.GooglePlayPurchaseGate.RenewalOwnershipAmbiguous, status.GooglePlayPurchaseBlockReasonCode);
    }

    [Fact]
    public async Task AuthoritativePaddleScheduledCancellationAtOrBeforeCheckTimeFailsClosed()
    {
        await using var db = CreateDb();
        var userId = await AddUserAsync(db);
        var effectiveAt = Now.AddMinutes(-1);
        var paddle = await AddSubscriptionAsync(
            db,
            userId,
            SubscriptionConstants.BillingProviders.Paddle,
            SubscriptionConstants.SubscriptionStatuses.Active,
            cancelAtPeriodEnd: true,
            scheduledChangeAction: SubscriptionConstants.ScheduledChangeActions.Cancel,
            scheduledChangeEffectiveAtUtc: effectiveAt,
            lastProviderEventId: "evt_past_cancel");
        await AddPaddleBillingEventAsync(db, paddle, "evt_past_cancel", effectiveAt, scheduledChangeSnapshotComplete: true);

        var status = await GetStatusAsync(db, userId);

        Assert.False(status.GooglePlayPurchaseAllowed);
        Assert.Equal(SubscriptionConstants.GooglePlayPurchaseGate.RenewalOwnershipAmbiguous, status.GooglePlayPurchaseBlockReasonCode);
    }

    [Theory]
    [InlineData(SubscriptionConstants.SubscriptionStatuses.Expired)]
    [InlineData(SubscriptionConstants.SubscriptionStatuses.Canceled)]
    [InlineData(SubscriptionConstants.SubscriptionStatuses.Chargeback)]
    public async Task TerminalExternalSubscriptionDoesNotBlockGooglePlayPurchase(string subscriptionStatus)
    {
        await using var db = CreateDb();
        var userId = await AddUserAsync(db);
        await AddSubscriptionAsync(db, userId, SubscriptionConstants.BillingProviders.Paddle, subscriptionStatus);

        var status = await GetStatusAsync(db, userId);

        Assert.True(status.GooglePlayPurchaseAllowed);
    }

    [Fact]
    public async Task MultipleRenewalOwnersFailClosedAsAmbiguous()
    {
        await using var db = CreateDb();
        var userId = await AddUserAsync(db);
        await AddSubscriptionAsync(db, userId, SubscriptionConstants.BillingProviders.Paddle, SubscriptionConstants.SubscriptionStatuses.Active);
        await AddSubscriptionAsync(db, userId, SubscriptionConstants.BillingProviders.GooglePlay, SubscriptionConstants.SubscriptionStatuses.Active);

        var status = await GetStatusAsync(db, userId);

        Assert.False(status.GooglePlayPurchaseAllowed);
        Assert.Equal(SubscriptionConstants.GooglePlayPurchaseGate.MultipleExternalAutoRenewOwners, status.GooglePlayPurchaseBlockReasonCode);
        Assert.Null(status.GooglePlayPurchaseBlockingProvider);
    }

    [Theory]
    [InlineData(null, SubscriptionConstants.SubscriptionStatuses.Active, false, null, null)]
    [InlineData(null, SubscriptionConstants.SubscriptionStatuses.Expired, false, null, null)]
    [InlineData("", SubscriptionConstants.SubscriptionStatuses.Active, false, null, null)]
    [InlineData("provider-id", SubscriptionConstants.SubscriptionStatuses.Unknown, false, null, null)]
    [InlineData("provider-id", SubscriptionConstants.SubscriptionStatuses.Active, true, "pause", null)]
    [InlineData("provider-id", SubscriptionConstants.SubscriptionStatuses.Active, true, "cancel", SubscriptionConstants.BillingEventTypes.SubscriptionResumed)]
    public async Task IncompleteOrConflictingRenewalOwnershipFailsClosed(
        string? providerSubscriptionId,
        string subscriptionStatus,
        bool cancelAtPeriodEnd,
        string? scheduledChangeAction,
        string? lastProviderEventType)
    {
        await using var db = CreateDb();
        var userId = await AddUserAsync(db);
        await AddSubscriptionAsync(
            db,
            userId,
            SubscriptionConstants.BillingProviders.Paddle,
            subscriptionStatus,
            providerSubscriptionId,
            cancelAtPeriodEnd,
            scheduledChangeAction,
            lastProviderEventType: lastProviderEventType);

        var status = await GetStatusAsync(db, userId);

        Assert.False(status.GooglePlayPurchaseAllowed);
        Assert.Equal(SubscriptionConstants.GooglePlayPurchaseGate.RenewalOwnershipAmbiguous, status.GooglePlayPurchaseBlockReasonCode);
        Assert.Null(status.GooglePlayPurchaseBlockingProvider);
    }

    private static async Task<EnglishVoiceTutor.Api.Contracts.Subscription.SubscriptionStatusResponse> GetStatusAsync(AppDbContext db, Guid userId) =>
        await new SubscriptionStatusService(db, Microsoft.Extensions.Options.Options.Create(new SubscriptionEnforcementOptions()))
            .GetStatusAsync(userId, "test", TestContext.Current.CancellationToken);

    private static async Task<Guid> AddUserAsync(AppDbContext db)
    {
        var userId = Guid.NewGuid();
        db.Users.Add(new UserEntity
        {
            Id = userId, Email = $"{userId:N}@example.test", PasswordHash = "hash", Status = "active", CreatedAt = Now
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return userId;
    }

    private static async Task<SubscriptionEntity> AddSubscriptionAsync(
        AppDbContext db,
        Guid userId,
        string provider,
        string status,
        string? providerSubscriptionId = "provider-id",
        bool cancelAtPeriodEnd = false,
        string? scheduledChangeAction = null,
        DateTimeOffset? scheduledChangeEffectiveAtUtc = null,
        DateTimeOffset? expiresAtUtc = null,
        string? lastProviderEventType = null,
        string? lastProviderEventId = null)
    {
        var subscription = new SubscriptionEntity
        {
            Id = Guid.NewGuid(), UserId = userId, PlanId = SubscriptionConstants.Plans.PremiumPlanId,
            Status = status, Provider = provider, ProviderSubscriptionId = providerSubscriptionId,
            StartedAt = Now.AddDays(-1), CurrentPeriodEndUtc = expiresAtUtc ?? Now.AddDays(30), ExpiresAt = expiresAtUtc ?? Now.AddDays(30),
            CancelAtPeriodEnd = cancelAtPeriodEnd, ScheduledChangeAction = scheduledChangeAction,
            ScheduledChangeEffectiveAtUtc = scheduledChangeEffectiveAtUtc,
            LastProviderEventId = lastProviderEventId,
            LastProviderEventType = lastProviderEventType ?? (lastProviderEventId is null ? null : SubscriptionConstants.BillingEventTypes.SubscriptionUpdated),
            CreatedAt = Now, UpdatedAt = Now
        };
        db.Subscriptions.Add(subscription);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return subscription;
    }

    private static async Task AddPaddleBillingEventAsync(
        AppDbContext db,
        SubscriptionEntity subscription,
        string providerEventId,
        DateTimeOffset? scheduledChangeEffectiveAtUtc,
        bool? scheduledChangeSnapshotComplete,
        string? scheduledChangeAction = SubscriptionConstants.ScheduledChangeActions.Cancel)
    {
        db.BillingEvents.Add(new BillingEventEntity
        {
            Id = Guid.NewGuid(),
            BillingProvider = SubscriptionConstants.BillingProviders.Paddle,
            EventType = SubscriptionConstants.BillingEventTypes.SubscriptionUpdated,
            ProviderEventId = providerEventId,
            ReceivedAtUtc = Now,
            ProcessedAtUtc = Now,
            Status = SubscriptionConstants.BillingEventStatuses.Processed,
            SafeMetadataJson = JsonSerializer.Serialize(new
            {
                paddleEventId = providerEventId,
                eventType = SubscriptionConstants.BillingEventTypes.SubscriptionUpdated,
                paddleSubscriptionId = subscription.ProviderSubscriptionId,
                internalUserId = subscription.UserId,
                scheduledChangeSnapshotComplete,
                scheduledChangeAction,
                scheduledChangeEffectiveAtUtc
            })
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    private static async Task AddEntitlementAsync(
        AppDbContext db,
        Guid userId,
        Guid? subscriptionId,
        DateTimeOffset expiresAtUtc,
        string source,
        DateTimeOffset? startsAtUtc = null)
    {
        db.Entitlements.Add(new EntitlementEntity
        {
            Id = Guid.NewGuid(), UserId = userId, SubscriptionId = subscriptionId, PlanId = SubscriptionConstants.Plans.PremiumPlanId,
            EntitlementType = SubscriptionConstants.Entitlements.PremiumAccessType, Source = source,
            Status = SubscriptionConstants.Entitlements.StatusActive, StartsAtUtc = startsAtUtc ?? Now.AddDays(-1), ExpiresAtUtc = expiresAtUtc,
            CreatedAt = Now, UpdatedAt = Now
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    private static AppDbContext CreateDb() =>
        new(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString("N")).Options);
}
