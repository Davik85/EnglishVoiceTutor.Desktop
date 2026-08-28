using System.Text.Json;
using EnglishVoiceTutor.Api.Constants;
using EnglishVoiceTutor.Api.Data;
using EnglishVoiceTutor.Api.Data.Entities;
using EnglishVoiceTutor.Api.Options;
using EnglishVoiceTutor.Api.Services.Billing;
using EnglishVoiceTutor.Api.Services.Subscriptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;

namespace EnglishVoiceTutor.Api.Tests.Services;

public sealed class PaddleSubscriptionScheduledChangePersistenceTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;

    [Fact]
    public async Task AuthoritativeRemovalClearsScheduledCancellationBlocksPurchaseAndPreservesCoverage()
    {
        await using var db = CreateDb();
        var userId = await AddUserAndPaddleSubscriptionAsync(db, cancelAtPeriodEnd: false);
        var periodEnd = Now.AddDays(30);
        var subscription = await db.Subscriptions.SingleAsync(TestContext.Current.CancellationToken);
        db.Entitlements.Add(CreateEntitlement(userId, subscription.Id, periodEnd));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var scheduledAt = Now.AddDays(20);
        await AddNormalizeAndProcessAsync(db, userId, "evt_cancel", Now, CreateScheduledChange("cancel", scheduledAt));

        var scheduledStatus = await GetStatusAsync(db, userId);
        Assert.True(subscription.CancelAtPeriodEnd);
        Assert.Equal(SubscriptionConstants.ScheduledChangeActions.Cancel, subscription.ScheduledChangeAction);
        Assert.Equal(scheduledAt, subscription.ScheduledChangeEffectiveAtUtc);
        Assert.True(scheduledStatus.GooglePlayPurchaseAllowed);
        Assert.Equal(periodEnd, scheduledStatus.PremiumCoverageEndsAtUtc);

        await AddNormalizeAndProcessAsync(db, userId, "evt_remove", Now.AddMinutes(1), scheduledChange: null);

        var renewedStatus = await GetStatusAsync(db, userId);
        Assert.False(subscription.CancelAtPeriodEnd);
        Assert.Null(subscription.ScheduledChangeAction);
        Assert.Null(subscription.ScheduledChangeEffectiveAtUtc);
        Assert.False(renewedStatus.GooglePlayPurchaseAllowed);
        Assert.Equal(SubscriptionConstants.GooglePlayPurchaseGate.ExternalAutoRenewActive, renewedStatus.GooglePlayPurchaseBlockReasonCode);
        Assert.Equal(SubscriptionConstants.BillingProviders.Paddle, renewedStatus.GooglePlayPurchaseBlockingProvider);
        Assert.Equal(periodEnd, renewedStatus.PremiumCoverageEndsAtUtc);
    }

    [Fact]
    public async Task OlderAuthoritativeRemovalCannotClearNewerScheduledCancellation()
    {
        await using var db = CreateDb();
        var userId = await AddUserAndPaddleSubscriptionAsync(db, cancelAtPeriodEnd: false);
        var scheduledAt = Now.AddDays(20);

        await AddNormalizeAndProcessAsync(db, userId, "evt_newer_cancel", Now.AddMinutes(2), CreateScheduledChange("cancel", scheduledAt));
        var olderResult = await AddNormalizeAndProcessAsync(db, userId, "evt_older_remove", Now.AddMinutes(1), scheduledChange: null);

        var subscription = await db.Subscriptions.SingleAsync(TestContext.Current.CancellationToken);
        Assert.Equal(1, olderResult.IgnoredOlderCount);
        Assert.True(subscription.CancelAtPeriodEnd);
        Assert.Equal(SubscriptionConstants.ScheduledChangeActions.Cancel, subscription.ScheduledChangeAction);
        Assert.Equal(scheduledAt, subscription.ScheduledChangeEffectiveAtUtc);
    }

    [Theory]
    [InlineData(SubscriptionConstants.ScheduledChangeActions.Cancel, true)]
    [InlineData(SubscriptionConstants.ScheduledChangeActions.Pause, false)]
    [InlineData(SubscriptionConstants.ScheduledChangeActions.Resume, false)]
    public async Task CompleteSnapshotPersistsSupportedScheduledChange(string action, bool cancelAtPeriodEnd)
    {
        await using var db = CreateDb();
        var userId = await AddUserAndPaddleSubscriptionAsync(db, cancelAtPeriodEnd: false);
        var effectiveAt = Now.AddDays(10);

        await AddNormalizeAndProcessAsync(db, userId, $"evt_{action}", Now, CreateScheduledChange(action, effectiveAt));

        var subscription = await db.Subscriptions.SingleAsync(TestContext.Current.CancellationToken);
        Assert.Equal(cancelAtPeriodEnd, subscription.CancelAtPeriodEnd);
        Assert.Equal(action, subscription.ScheduledChangeAction);
        Assert.Equal(effectiveAt, subscription.ScheduledChangeEffectiveAtUtc);
    }

    [Fact]
    public async Task MissingScheduledChangeEvidenceFailsClosedWithoutClearingPersistedState()
    {
        await using var db = CreateDb();
        var userId = await AddUserAndPaddleSubscriptionAsync(db, cancelAtPeriodEnd: true);
        var subscription = await db.Subscriptions.SingleAsync(TestContext.Current.CancellationToken);
        var priorEffectiveAt = subscription.ScheduledChangeEffectiveAtUtc;

        var result = await AddNormalizeAndProcessAsync(
            db,
            userId,
            "evt_incomplete",
            Now.AddMinutes(1),
            scheduledChange: null,
            includeScheduledChange: false);

        Assert.Equal(1, result.BlockedCount);
        Assert.True(subscription.CancelAtPeriodEnd);
        Assert.Equal(SubscriptionConstants.ScheduledChangeActions.Cancel, subscription.ScheduledChangeAction);
        Assert.Equal(priorEffectiveAt, subscription.ScheduledChangeEffectiveAtUtc);
        var billingEvent = await db.BillingEvents.SingleAsync(TestContext.Current.CancellationToken);
        Assert.Equal(SubscriptionConstants.BillingEventStatuses.ReconciliationBlocked, billingEvent.Status);
        Assert.Equal(SubscriptionConstants.SubscriptionLifecycleSnapshot.IncompleteScheduledChangeSnapshotMessage, billingEvent.ErrorMessage);
    }

    private static async Task<BillingEventSubscriptionSnapshotResult> AddNormalizeAndProcessAsync(
        AppDbContext db,
        Guid userId,
        string eventId,
        DateTimeOffset occurredAtUtc,
        object? scheduledChange,
        bool includeScheduledChange = true)
    {
        var data = new Dictionary<string, object?>
        {
            ["status"] = SubscriptionConstants.SubscriptionStatuses.Active,
            ["current_billing_period"] = new
            {
                starts_at = Now.AddDays(-1),
                ends_at = Now.AddDays(30)
            }
        };
        if (includeScheduledChange)
        {
            data["scheduled_change"] = scheduledChange;
        }

        db.PaddleWebhookEvents.Add(new PaddleWebhookEventEntity
        {
            Id = Guid.NewGuid(),
            PaddleEventId = eventId,
            EventType = SubscriptionConstants.BillingEventTypes.SubscriptionUpdated,
            OccurredAtUtc = occurredAtUtc,
            ReceivedAtUtc = occurredAtUtc,
            ProcessingStatus = SubscriptionConstants.BillingEventStatuses.Received,
            PaddleSubscriptionId = "sub_test",
            PaddleCustomerId = "ctm_test",
            InternalUserId = userId,
            InternalPlanId = SubscriptionConstants.Plans.PremiumPlanId,
            RawPayload = JsonSerializer.Serialize(new { data }),
            CreatedAt = occurredAtUtc,
            UpdatedAt = occurredAtUtc
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var normalization = await new PaddleWebhookEventNormalizer(
            db,
            NullLogger<PaddleWebhookEventNormalizer>.Instance).NormalizeEventAsync(
                eventId,
                TestContext.Current.CancellationToken);
        Assert.Equal(1, normalization.NormalizedCount);

        return await new BillingEventSubscriptionSnapshotService(
            db,
            NullLogger<BillingEventSubscriptionSnapshotService>.Instance).ProcessProviderEventAsync(
                SubscriptionConstants.BillingProviders.Paddle,
                eventId,
                TestContext.Current.CancellationToken);
    }

    private static object CreateScheduledChange(string action, DateTimeOffset effectiveAtUtc) => new
    {
        action,
        effective_at = effectiveAtUtc
    };

    private static async Task<Guid> AddUserAndPaddleSubscriptionAsync(AppDbContext db, bool cancelAtPeriodEnd)
    {
        var userId = Guid.NewGuid();
        db.Users.Add(new UserEntity
        {
            Id = userId,
            Email = $"{userId:N}@example.test",
            PasswordHash = "hash",
            Status = "active",
            CreatedAt = Now.AddDays(-2)
        });
        db.Subscriptions.Add(new SubscriptionEntity
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            PlanId = SubscriptionConstants.Plans.PremiumPlanId,
            Status = SubscriptionConstants.SubscriptionStatuses.Active,
            Provider = SubscriptionConstants.BillingProviders.Paddle,
            ProviderSubscriptionId = "sub_test",
            StartedAt = Now.AddDays(-1),
            CurrentPeriodStartUtc = Now.AddDays(-1),
            CurrentPeriodEndUtc = Now.AddDays(30),
            ExpiresAt = Now.AddDays(30),
            CancelAtPeriodEnd = cancelAtPeriodEnd,
            ScheduledChangeAction = cancelAtPeriodEnd ? SubscriptionConstants.ScheduledChangeActions.Cancel : null,
            ScheduledChangeEffectiveAtUtc = cancelAtPeriodEnd ? Now.AddDays(20) : null,
            LastProviderEventOccurredAtUtc = Now,
            CreatedAt = Now.AddDays(-1),
            UpdatedAt = Now
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return userId;
    }

    private static EntitlementEntity CreateEntitlement(Guid userId, Guid subscriptionId, DateTimeOffset expiresAtUtc) => new()
    {
        Id = Guid.NewGuid(),
        UserId = userId,
        SubscriptionId = subscriptionId,
        PlanId = SubscriptionConstants.Plans.PremiumPlanId,
        EntitlementType = SubscriptionConstants.Entitlements.PremiumAccessType,
        Source = SubscriptionConstants.Entitlements.SourceProviderEvent,
        Status = SubscriptionConstants.Entitlements.StatusActive,
        StartsAtUtc = Now.AddDays(-1),
        ExpiresAtUtc = expiresAtUtc,
        CreatedAt = Now.AddDays(-1),
        UpdatedAt = Now
    };

    private static async Task<EnglishVoiceTutor.Api.Contracts.Subscription.SubscriptionStatusResponse> GetStatusAsync(AppDbContext db, Guid userId) =>
        await new SubscriptionStatusService(db, Microsoft.Extensions.Options.Options.Create(new SubscriptionEnforcementOptions()))
            .GetStatusAsync(userId, "test", TestContext.Current.CancellationToken);

    private static AppDbContext CreateDb() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options);
}
