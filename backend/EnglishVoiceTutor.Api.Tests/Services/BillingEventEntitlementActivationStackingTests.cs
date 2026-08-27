using System.Text.Json;
using EnglishVoiceTutor.Api.Constants;
using EnglishVoiceTutor.Api.Data;
using EnglishVoiceTutor.Api.Data.Entities;
using EnglishVoiceTutor.Api.Options;
using EnglishVoiceTutor.Api.Services.Billing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;

namespace EnglishVoiceTutor.Api.Tests.Services;

public sealed class BillingEventEntitlementActivationStackingTests
{
    [Fact]
    public async Task PaddleFixedPeriodStartsAfterTrialAndContiguousScheduledManualTail()
    {
        await using var db = CreateDb();
        var now = DateTimeOffset.UtcNow;
        var userId = Guid.NewGuid();
        var trialEnd = now.AddDays(5);
        var manualEnd = trialEnd.AddDays(10);
        var paidDuration = TimeSpan.FromDays(30);
        db.Users.Add(new UserEntity
        {
            Id = userId,
            Email = $"{userId:N}@example.test",
            PasswordHash = "hash",
            Status = "active",
            CreatedAt = now.AddDays(-2)
        });
        db.TrialGrants.Add(new TrialGrantEntity
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            GrantedAtUtc = now.AddDays(-1),
            ExpiresAtUtc = trialEnd,
            SourcePlatform = "test",
            Status = SubscriptionConstants.Entitlements.StatusActive,
            CreatedAt = now.AddDays(-1)
        });
        db.Subscriptions.Add(CreatePaddleSubscription(userId, now, "sub_test"));
        db.Entitlements.Add(new EntitlementEntity
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            PlanId = SubscriptionConstants.Plans.PremiumPlanId,
            EntitlementType = SubscriptionConstants.Entitlements.PremiumAccessType,
            Source = SubscriptionConstants.Entitlements.SourceManualAdmin,
            Status = SubscriptionConstants.Entitlements.StatusActive,
            StartsAtUtc = trialEnd,
            ExpiresAtUtc = manualEnd,
            CreatedAt = now.AddDays(-1),
            UpdatedAt = now.AddDays(-1)
        });
        var providerEventId = "evt_" + Guid.NewGuid().ToString("N");
        db.BillingEvents.Add(new BillingEventEntity
        {
            Id = Guid.NewGuid(),
            BillingProvider = SubscriptionConstants.BillingProviders.Paddle,
            EventType = SubscriptionConstants.BillingEventTypes.TransactionCompleted,
            ProviderEventId = providerEventId,
            ReceivedAtUtc = now,
            Status = SubscriptionConstants.BillingEventStatuses.ReconciliationPending,
            SafeMetadataJson = JsonSerializer.Serialize(new
            {
                internalUserId = userId,
                internalPlanId = SubscriptionConstants.Plans.PremiumPlanId,
                billingPeriodStartsAtUtc = now,
                billingPeriodEndsAtUtc = now.Add(paidDuration),
                paddlePriceId = "pri_test",
                paddleProductId = "pro_test",
                customDataApp = "language_voice_tutor",
                customDataProduct = "language_voice_tutor_pro",
                paddleTransactionId = "txn_test",
                paddleSubscriptionId = "sub_test"
            })
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await CreateService(db).ActivateProviderEventAsync(
            SubscriptionConstants.BillingProviders.Paddle,
            providerEventId,
            TestContext.Current.CancellationToken);

        Assert.Equal(1, result.ActivatedCount);
        var provider = await db.Entitlements.SingleAsync(
            item => item.Source == SubscriptionConstants.Entitlements.SourceProviderEvent,
            TestContext.Current.CancellationToken);
        Assert.Equal(manualEnd, provider.StartsAtUtc);
        Assert.Equal(manualEnd.Add(paidDuration), provider.ExpiresAtUtc);
    }

    [Fact]
    public async Task GappedFutureProviderDoesNotMoveNewPaddlePeriodPastTheCurrentTail()
    {
        await using var db = CreateDb();
        var now = DateTimeOffset.UtcNow;
        var userId = Guid.NewGuid();
        var currentEnd = now.AddDays(5);
        var gappedStart = currentEnd.AddDays(2);
        db.Users.Add(new UserEntity
        {
            Id = userId,
            Email = $"{userId:N}@example.test",
            PasswordHash = "hash",
            Status = "active",
            CreatedAt = now.AddDays(-2)
        });
        db.Entitlements.AddRange(
            new EntitlementEntity
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                PlanId = SubscriptionConstants.Plans.PremiumPlanId,
                EntitlementType = SubscriptionConstants.Entitlements.PremiumAccessType,
                Source = SubscriptionConstants.Entitlements.SourceManualAdmin,
                Status = SubscriptionConstants.Entitlements.StatusActive,
                StartsAtUtc = now.AddDays(-1),
                ExpiresAtUtc = currentEnd,
                CreatedAt = now.AddDays(-1),
                UpdatedAt = now.AddDays(-1)
            },
            new EntitlementEntity
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                PlanId = SubscriptionConstants.Plans.PremiumPlanId,
                EntitlementType = SubscriptionConstants.Entitlements.PremiumAccessType,
                Source = SubscriptionConstants.Entitlements.SourceProviderEvent,
                Status = SubscriptionConstants.Entitlements.StatusActive,
                StartsAtUtc = gappedStart,
                ExpiresAtUtc = gappedStart.AddDays(10),
                CreatedAt = now.AddDays(-1),
                UpdatedAt = now.AddDays(-1)
            });
        var providerEventId = await AddTransactionEventAsync(db, userId, now, TimeSpan.FromDays(30));

        var result = await CreateService(db).ActivateProviderEventAsync(
            SubscriptionConstants.BillingProviders.Paddle,
            providerEventId,
            TestContext.Current.CancellationToken);

        Assert.Equal(1, result.ActivatedCount);
        var newProvider = await db.Entitlements.SingleAsync(
            item => item.Source == SubscriptionConstants.Entitlements.SourceProviderEvent
                && item.StartsAtUtc == currentEnd,
            TestContext.Current.CancellationToken);
        Assert.Equal(currentEnd.AddDays(30), newProvider.ExpiresAtUtc);
        Assert.NotNull(newProvider.SubscriptionId);
    }

    [Fact]
    public async Task PaddleActivationMutatesOnlyExactPaddleEntitlementAfterProviderNeutralTail()
    {
        await using var db = CreateDb();
        var now = DateTimeOffset.UtcNow;
        var userId = Guid.NewGuid();
        var paddle = CreatePaddleSubscription(userId, now, "sub_test");
        var google = new SubscriptionEntity
        {
            Id = Guid.NewGuid(), UserId = userId, PlanId = SubscriptionConstants.Plans.PremiumPlanId,
            Status = SubscriptionConstants.SubscriptionStatuses.Active, Provider = SubscriptionConstants.BillingProviders.GooglePlay,
            ProviderSubscriptionId = "google_test", StartedAt = now.AddDays(-1), CreatedAt = now, UpdatedAt = now
        };
        var paddleEntitlement = CreateProviderEntitlement(userId, paddle.Id, now.AddDays(-1), now.AddDays(10), now);
        var googleEntitlement = CreateProviderEntitlement(userId, google.Id, now.AddDays(10), now.AddDays(30), now);
        db.Users.Add(new UserEntity { Id = userId, Email = $"{userId:N}@example.test", PasswordHash = "hash", Status = "active", CreatedAt = now });
        db.Subscriptions.AddRange(paddle, google);
        db.Entitlements.AddRange(paddleEntitlement, googleEntitlement);
        var providerEventId = await AddTransactionEventAsync(db, userId, now, TimeSpan.FromDays(30));

        var result = await CreateService(db).ActivateProviderEventAsync(
            SubscriptionConstants.BillingProviders.Paddle,
            providerEventId,
            TestContext.Current.CancellationToken);

        Assert.Equal(1, result.ActivatedCount);
        Assert.Equal(now.AddDays(60), paddleEntitlement.ExpiresAtUtc);
        Assert.Equal(now.AddDays(30), googleEntitlement.ExpiresAtUtc);
        Assert.Equal(google.Id, googleEntitlement.SubscriptionId);
    }

    [Fact]
    public async Task LegacyUnscopedProviderEntitlementIsNeverReusedForPaddleActivation()
    {
        await using var db = CreateDb();
        var now = DateTimeOffset.UtcNow;
        var userId = Guid.NewGuid();
        var legacy = CreateProviderEntitlement(userId, null, now.AddDays(-1), now.AddDays(10), now);
        db.Users.Add(new UserEntity { Id = userId, Email = $"{userId:N}@example.test", PasswordHash = "hash", Status = "active", CreatedAt = now });
        db.Entitlements.Add(legacy);
        var providerEventId = await AddTransactionEventAsync(db, userId, now, TimeSpan.FromDays(30));

        var result = await CreateService(db).ActivateProviderEventAsync(
            SubscriptionConstants.BillingProviders.Paddle,
            providerEventId,
            TestContext.Current.CancellationToken);

        Assert.Equal(1, result.ActivatedCount);
        Assert.Null(legacy.SubscriptionId);
        Assert.Equal(now.AddDays(10), legacy.ExpiresAtUtc);
        var paddleEntitlement = await db.Entitlements.SingleAsync(
            entitlement => entitlement.Id != legacy.Id,
            TestContext.Current.CancellationToken);
        Assert.NotNull(paddleEntitlement.SubscriptionId);
        Assert.Equal(now.AddDays(40), paddleEntitlement.ExpiresAtUtc);
    }

    [Fact]
    public async Task PaddleLifecycleExpiryMutatesOnlyExactPaddleSubscriptionEntitlement()
    {
        await using var db = CreateDb();
        var now = DateTimeOffset.UtcNow;
        var userId = Guid.NewGuid();
        var paddle = CreatePaddleSubscription(userId, now, "sub_test");
        var google = new SubscriptionEntity
        {
            Id = Guid.NewGuid(), UserId = userId, PlanId = SubscriptionConstants.Plans.PremiumPlanId,
            Status = SubscriptionConstants.SubscriptionStatuses.Active, Provider = SubscriptionConstants.BillingProviders.GooglePlay,
            ProviderSubscriptionId = "google_test", StartedAt = now, CreatedAt = now, UpdatedAt = now
        };
        var paddleEntitlement = CreateProviderEntitlement(userId, paddle.Id, now.AddDays(-1), now.AddDays(30), now);
        var googleEntitlement = CreateProviderEntitlement(userId, google.Id, now.AddDays(-1), now.AddDays(30), now);
        var legacyEntitlement = CreateProviderEntitlement(userId, null, now.AddDays(-1), now.AddDays(30), now);
        var providerEventId = "evt_" + Guid.NewGuid().ToString("N");
        db.Users.Add(new UserEntity { Id = userId, Email = $"{userId:N}@example.test", PasswordHash = "hash", Status = "active", CreatedAt = now });
        db.Subscriptions.AddRange(paddle, google);
        db.Entitlements.AddRange(paddleEntitlement, googleEntitlement, legacyEntitlement);
        db.BillingEvents.Add(new BillingEventEntity
        {
            Id = Guid.NewGuid(), BillingProvider = SubscriptionConstants.BillingProviders.Paddle,
            EventType = SubscriptionConstants.BillingEventTypes.SubscriptionCanceled,
            ProviderEventId = providerEventId, ReceivedAtUtc = now,
            Status = SubscriptionConstants.BillingEventStatuses.Received,
            SafeMetadataJson = JsonSerializer.Serialize(new
            {
                paddleEventId = providerEventId,
                eventType = SubscriptionConstants.BillingEventTypes.SubscriptionCanceled,
                paddleSubscriptionId = "sub_test",
                internalUserId = userId,
                internalPlanId = SubscriptionConstants.Plans.PremiumPlanId,
                effectiveAtUtc = now,
                occurredAtUtc = now
            })
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await new BillingEventSubscriptionSnapshotService(
            db,
            NullLogger<BillingEventSubscriptionSnapshotService>.Instance).ProcessProviderEventAsync(
                SubscriptionConstants.BillingProviders.Paddle,
                providerEventId,
                TestContext.Current.CancellationToken);

        Assert.Equal(1, result.ProviderEventEntitlementExpiredCount);
        Assert.Equal(now, paddleEntitlement.ExpiresAtUtc);
        Assert.Equal(now.AddDays(30), googleEntitlement.ExpiresAtUtc);
        Assert.Equal(now.AddDays(30), legacyEntitlement.ExpiresAtUtc);
        Assert.Null(legacyEntitlement.SubscriptionId);
    }

    private static async Task<string> AddTransactionEventAsync(
        AppDbContext db,
        Guid userId,
        DateTimeOffset now,
        TimeSpan paidDuration)
    {
        var providerEventId = "evt_" + Guid.NewGuid().ToString("N");
        var trackedPaddleSubscriptionExists = db.Subscriptions.Local.Any(
            subscription => subscription.Provider == SubscriptionConstants.BillingProviders.Paddle
                && subscription.ProviderSubscriptionId == "sub_test");
        if (!trackedPaddleSubscriptionExists
            && !await db.Subscriptions.AnyAsync(
                subscription => subscription.Provider == SubscriptionConstants.BillingProviders.Paddle
                    && subscription.ProviderSubscriptionId == "sub_test",
                TestContext.Current.CancellationToken))
        {
            db.Subscriptions.Add(CreatePaddleSubscription(userId, now, "sub_test"));
        }
        db.BillingEvents.Add(new BillingEventEntity
        {
            Id = Guid.NewGuid(),
            BillingProvider = SubscriptionConstants.BillingProviders.Paddle,
            EventType = SubscriptionConstants.BillingEventTypes.TransactionCompleted,
            ProviderEventId = providerEventId,
            ReceivedAtUtc = now,
            Status = SubscriptionConstants.BillingEventStatuses.ReconciliationPending,
            SafeMetadataJson = JsonSerializer.Serialize(new
            {
                internalUserId = userId,
                internalPlanId = SubscriptionConstants.Plans.PremiumPlanId,
                billingPeriodStartsAtUtc = now,
                billingPeriodEndsAtUtc = now.Add(paidDuration),
                paddlePriceId = "pri_test",
                paddleProductId = "pro_test",
                customDataApp = "language_voice_tutor",
                customDataProduct = "language_voice_tutor_pro",
                paddleTransactionId = "txn_test",
                paddleSubscriptionId = "sub_test"
            })
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return providerEventId;
    }

    private static SubscriptionEntity CreatePaddleSubscription(Guid userId, DateTimeOffset now, string providerSubscriptionId) => new()
    {
        Id = Guid.NewGuid(), UserId = userId, PlanId = SubscriptionConstants.Plans.PremiumPlanId,
        Status = SubscriptionConstants.SubscriptionStatuses.Active, Provider = SubscriptionConstants.BillingProviders.Paddle,
        ProviderSubscriptionId = providerSubscriptionId, StartedAt = now, CreatedAt = now, UpdatedAt = now
    };

    private static EntitlementEntity CreateProviderEntitlement(
        Guid userId,
        Guid? subscriptionId,
        DateTimeOffset startsAtUtc,
        DateTimeOffset expiresAtUtc,
        DateTimeOffset now) => new()
    {
        Id = Guid.NewGuid(), UserId = userId, SubscriptionId = subscriptionId,
        PlanId = SubscriptionConstants.Plans.PremiumPlanId,
        EntitlementType = SubscriptionConstants.Entitlements.PremiumAccessType,
        Source = SubscriptionConstants.Entitlements.SourceProviderEvent,
        Status = SubscriptionConstants.Entitlements.StatusActive,
        StartsAtUtc = startsAtUtc, ExpiresAtUtc = expiresAtUtc, CreatedAt = now, UpdatedAt = now
    };

    private static BillingEventEntitlementActivationService CreateService(AppDbContext db) => new(
        db,
        NullLogger<BillingEventEntitlementActivationService>.Instance,
        Microsoft.Extensions.Options.Options.Create(new PaddleBillingOptions
        {
            PremiumPriceId = "pri_test",
            PremiumProductId = "pro_test",
            ExpectedCustomDataApp = "language_voice_tutor",
            ExpectedCustomDataProduct = "language_voice_tutor_pro"
        }));

    private static AppDbContext CreateDb() => new(
        new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options);
}
