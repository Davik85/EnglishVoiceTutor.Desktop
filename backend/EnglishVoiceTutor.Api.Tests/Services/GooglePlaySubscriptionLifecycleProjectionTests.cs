using EnglishVoiceTutor.Api.Constants;
using EnglishVoiceTutor.Api.Data;
using EnglishVoiceTutor.Api.Data.Entities;
using EnglishVoiceTutor.Api.Options;
using EnglishVoiceTutor.Api.Services;
using EnglishVoiceTutor.Api.Services.Billing;
using EnglishVoiceTutor.Api.Services.Subscriptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;

namespace EnglishVoiceTutor.Api.Tests.Services;

public sealed class GooglePlaySubscriptionLifecycleProjectionTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 7, 12, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset StartedAt = Now.AddDays(-30);
    private static readonly DateTimeOffset FutureExpiry = Now.AddDays(30);
    private static readonly DateTimeOffset PastExpiry = Now.AddDays(-1);

    [Fact]
    public async Task ActiveGoogleAndPaddleRemainIndependentAndReplayIsIdempotent()
    {
        await using var db = CreateDb();
        var userId = await AddUserAsync(db);
        var service = CreateService(db);
        var first = await service.PersistAsync(Request(userId, GooglePlaySubscriptionLifecycleState.Active), TestContext.Current.CancellationToken);
        var paddle = await AddProviderEntitlementAsync(db, userId, SubscriptionConstants.BillingProviders.Paddle, FutureExpiry.AddDays(10));
        var replay = await service.PersistAsync(Request(userId, GooglePlaySubscriptionLifecycleState.Active), TestContext.Current.CancellationToken);

        Assert.Equal(GooglePlayVerifiedPurchasePersistenceResultCode.Applied, first.Code);
        Assert.Equal(GooglePlayVerifiedPurchasePersistenceResultCode.AlreadyCurrent, replay.Code);
        Assert.Equal(2, db.Entitlements.Count(item => item.Status == SubscriptionConstants.Entitlements.StatusActive));
        Assert.Equal(SubscriptionConstants.Entitlements.StatusActive, paddle.Entitlement.Status);
        Assert.Equal(FutureExpiry.AddDays(10), paddle.Entitlement.ExpiresAtUtc);
    }

    [Fact]
    public async Task GracePeriodRetainsExactGoogleEntitlement()
    {
        await using var db = CreateDb();
        var userId = await AddUserAsync(db);
        var service = CreateService(db);
        await service.PersistAsync(Request(userId, GooglePlaySubscriptionLifecycleState.Active), TestContext.Current.CancellationToken);

        var result = await service.PersistAsync(Request(userId, GooglePlaySubscriptionLifecycleState.InGracePeriod), TestContext.Current.CancellationToken);

        Assert.Equal(GooglePlayVerifiedPurchasePersistenceResultCode.Applied, result.Code);
        Assert.Equal(SubscriptionConstants.SubscriptionStatuses.PastDue, Assert.Single(db.Subscriptions).Status);
        Assert.Equal(SubscriptionConstants.Entitlements.StatusActive, Assert.Single(db.Entitlements).Status);
        Assert.True((await StatusAsync(db, userId)).PremiumActive);
    }

    [Fact]
    public async Task UserCancellationWithFutureExpiryRetainsAccessAndCancellationMetadata()
    {
        await using var db = CreateDb();
        var userId = await AddUserAsync(db);
        var service = CreateService(db);
        await service.PersistAsync(Request(userId, GooglePlaySubscriptionLifecycleState.Active), TestContext.Current.CancellationToken);

        await service.PersistAsync(Request(userId, GooglePlaySubscriptionLifecycleState.Canceled), TestContext.Current.CancellationToken);

        var subscription = Assert.Single(db.Subscriptions);
        Assert.Equal(SubscriptionConstants.SubscriptionStatuses.Canceled, subscription.Status);
        Assert.True(subscription.CancelAtPeriodEnd);
        Assert.Equal(SubscriptionConstants.ScheduledChangeActions.Cancel, subscription.ScheduledChangeAction);
        Assert.Equal(FutureExpiry, subscription.ScheduledChangeEffectiveAtUtc);
        Assert.Equal(SubscriptionConstants.Entitlements.StatusActive, Assert.Single(db.Entitlements).Status);
        Assert.True((await StatusAsync(db, userId)).PremiumActive);
    }

    [Fact]
    public async Task PastCancellationRemovesOnlyGoogleWhilePaddleRemainsEffective()
    {
        await using var db = CreateDb();
        var userId = await AddUserAsync(db);
        var service = CreateService(db);
        await service.PersistAsync(Request(userId, GooglePlaySubscriptionLifecycleState.Active), TestContext.Current.CancellationToken);
        var paddle = await AddProviderEntitlementAsync(db, userId, SubscriptionConstants.BillingProviders.Paddle, FutureExpiry.AddDays(10));

        await service.PersistAsync(Request(userId, GooglePlaySubscriptionLifecycleState.Canceled, PastExpiry), TestContext.Current.CancellationToken);

        var google = await ExactGoogleEntitlementAsync(db);
        Assert.Equal(SubscriptionConstants.Entitlements.StatusExpired, google.Status);
        Assert.Equal(SubscriptionConstants.Entitlements.StatusActive, paddle.Entitlement.Status);
        var status = await StatusAsync(db, userId);
        Assert.True(status.PremiumActive);
        Assert.Equal(SubscriptionConstants.BillingProviders.Paddle, status.BillingProvider);
    }

    [Fact]
    public async Task OnHoldWithoutAnotherEntitlementRemovesGooglePremium()
    {
        await using var db = CreateDb();
        var userId = await AddUserAsync(db);
        var service = CreateService(db);
        await service.PersistAsync(Request(userId, GooglePlaySubscriptionLifecycleState.Active), TestContext.Current.CancellationToken);

        await service.PersistAsync(Request(userId, GooglePlaySubscriptionLifecycleState.OnHold), TestContext.Current.CancellationToken);

        Assert.Equal(SubscriptionConstants.Entitlements.StatusInactive, Assert.Single(db.Entitlements).Status);
        Assert.False((await StatusAsync(db, userId)).PremiumActive);
    }

    [Fact]
    public async Task OnHoldLeavesActivePaddlePremiumEffective()
    {
        await using var db = CreateDb();
        var userId = await AddUserAsync(db);
        var service = CreateService(db);
        await service.PersistAsync(Request(userId, GooglePlaySubscriptionLifecycleState.Active), TestContext.Current.CancellationToken);
        var paddle = await AddProviderEntitlementAsync(db, userId, SubscriptionConstants.BillingProviders.Paddle, FutureExpiry.AddDays(10));

        await service.PersistAsync(Request(userId, GooglePlaySubscriptionLifecycleState.OnHold), TestContext.Current.CancellationToken);

        Assert.Equal(SubscriptionConstants.Entitlements.StatusInactive, (await ExactGoogleEntitlementAsync(db)).Status);
        Assert.Equal(SubscriptionConstants.Entitlements.StatusActive, paddle.Entitlement.Status);
        var status = await StatusAsync(db, userId);
        Assert.True(status.PremiumActive);
        Assert.Equal(SubscriptionConstants.BillingProviders.Paddle, status.BillingProvider);
    }

    [Theory]
    [InlineData(GooglePlaySubscriptionLifecycleState.OnHold)]
    [InlineData(GooglePlaySubscriptionLifecycleState.Paused)]
    public async Task RecoveryFromSuspensionRestoresOnlyTheSameGoogleEntitlement(GooglePlaySubscriptionLifecycleState suspendedState)
    {
        await using var db = CreateDb();
        var userId = await AddUserAsync(db);
        var service = CreateService(db);
        await service.PersistAsync(Request(userId, GooglePlaySubscriptionLifecycleState.Active), TestContext.Current.CancellationToken);
        await service.PersistAsync(Request(userId, suspendedState), TestContext.Current.CancellationToken);

        var recovery = await service.PersistAsync(Request(userId, GooglePlaySubscriptionLifecycleState.Active, FutureExpiry.AddDays(30)), TestContext.Current.CancellationToken);

        Assert.Equal(GooglePlayVerifiedPurchasePersistenceResultCode.Applied, recovery.Code);
        var entitlement = Assert.Single(db.Entitlements);
        Assert.Equal(SubscriptionConstants.Entitlements.StatusActive, entitlement.Status);
        Assert.Equal(FutureExpiry.AddDays(30), entitlement.ExpiresAtUtc);
        Assert.Equal(SubscriptionConstants.SubscriptionStatuses.Active, Assert.Single(db.Subscriptions).Status);
    }

    [Theory]
    [InlineData(GooglePlaySubscriptionLifecycleState.OnHold, SubscriptionConstants.Entitlements.SourceManualAdmin)]
    [InlineData(GooglePlaySubscriptionLifecycleState.Paused, SubscriptionConstants.Entitlements.SourceManualAdmin)]
    [InlineData(GooglePlaySubscriptionLifecycleState.Expired, SubscriptionConstants.Entitlements.SourceManualAdmin)]
    [InlineData(GooglePlaySubscriptionLifecycleState.Canceled, SubscriptionConstants.Entitlements.SourceManualAdmin)]
    [InlineData(GooglePlaySubscriptionLifecycleState.Revoked, SubscriptionConstants.Entitlements.SourceManualAdmin)]
    [InlineData(GooglePlaySubscriptionLifecycleState.OnHold, SubscriptionConstants.Entitlements.SourceTrial)]
    [InlineData(GooglePlaySubscriptionLifecycleState.Paused, SubscriptionConstants.Entitlements.SourceTrial)]
    [InlineData(GooglePlaySubscriptionLifecycleState.Expired, SubscriptionConstants.Entitlements.SourceTrial)]
    [InlineData(GooglePlaySubscriptionLifecycleState.Canceled, SubscriptionConstants.Entitlements.SourceTrial)]
    [InlineData(GooglePlaySubscriptionLifecycleState.Revoked, SubscriptionConstants.Entitlements.SourceTrial)]
    public async Task ManualAndTrialAccessSurviveGoogleTerminalStates(GooglePlaySubscriptionLifecycleState lifecycleState, string survivingSource)
    {
        await using var db = CreateDb();
        var userId = await AddUserAsync(db);
        var service = CreateService(db);
        await service.PersistAsync(Request(userId, GooglePlaySubscriptionLifecycleState.Active), TestContext.Current.CancellationToken);
        if (survivingSource == SubscriptionConstants.Entitlements.SourceManualAdmin)
            await AddStandaloneEntitlementAsync(db, userId, survivingSource, FutureExpiry.AddDays(20));
        else
            await AddTrialAsync(db, userId, FutureExpiry.AddDays(20));

        var expiry = lifecycleState is GooglePlaySubscriptionLifecycleState.Expired or GooglePlaySubscriptionLifecycleState.Canceled ? PastExpiry : FutureExpiry;
        await service.PersistAsync(Request(userId, lifecycleState, expiry), TestContext.Current.CancellationToken);

        Assert.NotEqual(SubscriptionConstants.Entitlements.StatusActive, (await ExactGoogleEntitlementAsync(db)).Status);
        var status = await StatusAsync(db, userId);
        if (survivingSource == SubscriptionConstants.Entitlements.SourceManualAdmin) Assert.True(status.PremiumActive);
        else Assert.True(status.TrialActive);
    }

    [Fact]
    public async Task ConfirmedRevocationWithInvalidPurchaseLeavesExistingEntitlementsUnchanged()
    {
        await using var db = CreateDb();
        var userId = await AddUserAsync(db);
        var persistence = CreateService(db);
        await persistence.PersistAsync(Request(userId, GooglePlaySubscriptionLifecycleState.Active), TestContext.Current.CancellationToken);
        var paddle = await AddProviderEntitlementAsync(db, userId, SubscriptionConstants.BillingProviders.Paddle, FutureExpiry.AddDays(10));
        var manual = await AddStandaloneEntitlementAsync(db, userId, SubscriptionConstants.Entitlements.SourceManualAdmin, FutureExpiry.AddDays(20));
        await AddTrialAsync(db, userId, FutureExpiry.AddDays(20));
        var processor = new GooglePlayPurchaseProcessor(
            new StaticVerifier(GooglePlayPurchaseVerificationResultCode.InvalidPurchase),
            persistence,
            new Protection(),
            new NoopSubscriptionsClient(),
            NullLogger<GooglePlayPurchaseProcessor>.Instance);

        var result = await processor.ProcessAsync(
            userId,
            "raw-token",
            new GooglePlayPurchaseProcessingContext(ProviderConfirmedRevocation: true),
            TestContext.Current.CancellationToken);

        Assert.Equal(GooglePlayPurchaseProcessingResultCode.InvalidPurchase, result.Code);
        Assert.Equal(SubscriptionConstants.Entitlements.StatusActive, (await ExactGoogleEntitlementAsync(db)).Status);
        Assert.Equal(SubscriptionConstants.Entitlements.StatusActive, paddle.Entitlement.Status);
        Assert.Equal(SubscriptionConstants.Entitlements.StatusActive, manual.Status);
        Assert.Equal(SubscriptionConstants.Entitlements.StatusActive, Assert.Single(db.TrialGrants).Status);
        Assert.True((await StatusAsync(db, userId)).PremiumActive);
    }

    [Fact]
    public async Task FullVoidRefundWithTemporaryProviderFailureLeavesExistingAccessActive()
    {
        await using var db = CreateDb();
        var userId = await AddUserAsync(db);
        var persistence = CreateService(db);
        await persistence.PersistAsync(Request(userId, GooglePlaySubscriptionLifecycleState.Active), TestContext.Current.CancellationToken);
        var processor = new GooglePlayPurchaseProcessor(
            new StaticVerifier(GooglePlayPurchaseVerificationResultCode.TemporarilyUnavailable),
            persistence,
            new Protection(),
            new NoopSubscriptionsClient(),
            NullLogger<GooglePlayPurchaseProcessor>.Instance);

        var result = await processor.ProcessAsync(
            userId,
            "raw-token",
            new GooglePlayPurchaseProcessingContext(ProviderConfirmedRevocation: false),
            TestContext.Current.CancellationToken);

        Assert.Equal(GooglePlayPurchaseProcessingResultCode.TemporarilyUnavailable, result.Code);
        Assert.Equal(SubscriptionConstants.Entitlements.StatusActive, (await ExactGoogleEntitlementAsync(db)).Status);
    }

    [Fact]
    public async Task ConfirmedRevocationWithTemporaryProviderFailureLeavesExistingAccessActiveAndRetryable()
    {
        await using var db = CreateDb();
        var userId = await AddUserAsync(db);
        var persistence = CreateService(db);
        await persistence.PersistAsync(Request(userId, GooglePlaySubscriptionLifecycleState.Active), TestContext.Current.CancellationToken);
        var processor = new GooglePlayPurchaseProcessor(
            new StaticVerifier(GooglePlayPurchaseVerificationResultCode.TemporarilyUnavailable),
            persistence,
            new Protection(),
            new NoopSubscriptionsClient(),
            NullLogger<GooglePlayPurchaseProcessor>.Instance);

        var result = await processor.ProcessAsync(
            userId,
            "raw-token",
            new GooglePlayPurchaseProcessingContext(ProviderConfirmedRevocation: true),
            TestContext.Current.CancellationToken);

        Assert.Equal(GooglePlayPurchaseProcessingResultCode.TemporarilyUnavailable, result.Code);
        Assert.Equal(SubscriptionConstants.Entitlements.StatusActive, (await ExactGoogleEntitlementAsync(db)).Status);
    }

    [Theory]
    [InlineData(GooglePlaySubscriptionLifecycleState.Active, SubscriptionConstants.SubscriptionStatuses.Active, SubscriptionConstants.Entitlements.StatusActive)]
    [InlineData(GooglePlaySubscriptionLifecycleState.Canceled, SubscriptionConstants.SubscriptionStatuses.Canceled, SubscriptionConstants.Entitlements.StatusActive)]
    [InlineData(GooglePlaySubscriptionLifecycleState.Expired, SubscriptionConstants.SubscriptionStatuses.Expired, SubscriptionConstants.Entitlements.StatusExpired)]
    public async Task FullVoidRefundUsesFreshLifecycleWithoutRevocation(
        GooglePlaySubscriptionLifecycleState freshLifecycle,
        string expectedSubscriptionStatus,
        string expectedGoogleEntitlementStatus)
    {
        await using var db = CreateDb();
        var userId = await AddUserAsync(db);
        var persistence = CreateService(db);
        await persistence.PersistAsync(Request(userId, GooglePlaySubscriptionLifecycleState.Active), TestContext.Current.CancellationToken);
        var paddle = await AddProviderEntitlementAsync(db, userId, SubscriptionConstants.BillingProviders.Paddle, FutureExpiry.AddDays(10));
        var manual = await AddStandaloneEntitlementAsync(db, userId, SubscriptionConstants.Entitlements.SourceManualAdmin, FutureExpiry.AddDays(20));
        await AddTrialAsync(db, userId, FutureExpiry.AddDays(20));
        var verifiedExpiry = freshLifecycle == GooglePlaySubscriptionLifecycleState.Expired ? PastExpiry : FutureExpiry;
        var verified = new GooglePlayPurchaseVerificationResult(
            GooglePlayPurchaseVerificationResultCode.Verified,
            new GooglePlayVerifiedPurchase("com.example.test", "server-product", StartedAt, verifiedExpiry, GooglePlayPurchaseAcknowledgementState.Acknowledged, false, freshLifecycle));
        var processor = new GooglePlayPurchaseProcessor(
            new StaticVerifier(verified),
            persistence,
            new Protection(),
            new NoopSubscriptionsClient(),
            NullLogger<GooglePlayPurchaseProcessor>.Instance);

        var result = await processor.ProcessAsync(
            userId,
            "raw-token",
            new GooglePlayPurchaseProcessingContext(ProviderConfirmedRevocation: false),
            TestContext.Current.CancellationToken);

        Assert.Equal(GooglePlayPurchaseProcessingResultCode.Verified, result.Code);
        Assert.Equal(expectedSubscriptionStatus, Assert.Single(db.Subscriptions.Where(item => item.Provider == SubscriptionConstants.BillingProviders.GooglePlay)).Status);
        var google = await ExactGoogleEntitlementAsync(db);
        Assert.Equal(expectedGoogleEntitlementStatus, google.Status);
        Assert.NotEqual(SubscriptionConstants.Entitlements.StatusRevoked, google.Status);
        Assert.Equal(verifiedExpiry, google.ExpiresAtUtc);
        Assert.Equal(SubscriptionConstants.Entitlements.StatusActive, paddle.Entitlement.Status);
        Assert.Equal(SubscriptionConstants.Entitlements.StatusActive, manual.Status);
        Assert.Equal(SubscriptionConstants.Entitlements.StatusActive, Assert.Single(db.TrialGrants).Status);
    }

    [Theory]
    [InlineData(GooglePlaySubscriptionLifecycleState.Active, SubscriptionConstants.SubscriptionStatuses.Active)]
    [InlineData(GooglePlaySubscriptionLifecycleState.InGracePeriod, SubscriptionConstants.SubscriptionStatuses.PastDue)]
    [InlineData(GooglePlaySubscriptionLifecycleState.Canceled, SubscriptionConstants.SubscriptionStatuses.Canceled)]
    public async Task ConfirmedRevocationCannotOverrideFreshEntitlementRetainingState(
        GooglePlaySubscriptionLifecycleState freshLifecycle,
        string expectedSubscriptionStatus)
    {
        await using var db = CreateDb();
        var userId = await AddUserAsync(db);
        var persistence = CreateService(db);
        await persistence.PersistAsync(Request(userId, GooglePlaySubscriptionLifecycleState.Active), TestContext.Current.CancellationToken);
        var paddle = await AddProviderEntitlementAsync(db, userId, SubscriptionConstants.BillingProviders.Paddle, FutureExpiry.AddDays(10));
        var manual = await AddStandaloneEntitlementAsync(db, userId, SubscriptionConstants.Entitlements.SourceManualAdmin, FutureExpiry.AddDays(20));
        await AddTrialAsync(db, userId, FutureExpiry.AddDays(20));
        var verified = new GooglePlayPurchaseVerificationResult(
            GooglePlayPurchaseVerificationResultCode.Verified,
            new GooglePlayVerifiedPurchase("com.example.test", "server-product", StartedAt, FutureExpiry, GooglePlayPurchaseAcknowledgementState.Acknowledged, false, freshLifecycle));
        var processor = new GooglePlayPurchaseProcessor(
            new StaticVerifier(verified),
            persistence,
            new Protection(),
            new NoopSubscriptionsClient(),
            NullLogger<GooglePlayPurchaseProcessor>.Instance);

        var result = await processor.ProcessAsync(
            userId,
            "raw-token",
            new GooglePlayPurchaseProcessingContext(ProviderConfirmedRevocation: true),
            TestContext.Current.CancellationToken);

        Assert.Equal(GooglePlayPurchaseProcessingResultCode.Verified, result.Code);
        Assert.Equal(expectedSubscriptionStatus, Assert.Single(db.Subscriptions.Where(item => item.Provider == SubscriptionConstants.BillingProviders.GooglePlay)).Status);
        var google = await ExactGoogleEntitlementAsync(db);
        Assert.Equal(SubscriptionConstants.Entitlements.StatusActive, google.Status);
        Assert.Equal(FutureExpiry, google.ExpiresAtUtc);
        Assert.Equal(SubscriptionConstants.Entitlements.StatusActive, paddle.Entitlement.Status);
        Assert.Equal(SubscriptionConstants.Entitlements.StatusActive, manual.Status);
        Assert.Equal(SubscriptionConstants.Entitlements.StatusActive, Assert.Single(db.TrialGrants).Status);
    }

    [Fact]
    public async Task ConfirmedRevocationRefinesFreshExpiredStateToExactRevokedEntitlement()
    {
        await using var db = CreateDb();
        var userId = await AddUserAsync(db);
        var persistence = CreateService(db);
        await persistence.PersistAsync(Request(userId, GooglePlaySubscriptionLifecycleState.Active), TestContext.Current.CancellationToken);
        var paddle = await AddProviderEntitlementAsync(db, userId, SubscriptionConstants.BillingProviders.Paddle, FutureExpiry.AddDays(10));
        var manual = await AddStandaloneEntitlementAsync(db, userId, SubscriptionConstants.Entitlements.SourceManualAdmin, FutureExpiry.AddDays(20));
        await AddTrialAsync(db, userId, FutureExpiry.AddDays(20));
        var verified = new GooglePlayPurchaseVerificationResult(
            GooglePlayPurchaseVerificationResultCode.Verified,
            new GooglePlayVerifiedPurchase("com.example.test", "server-product", StartedAt, PastExpiry, GooglePlayPurchaseAcknowledgementState.Acknowledged, false, GooglePlaySubscriptionLifecycleState.Expired));
        var processor = new GooglePlayPurchaseProcessor(
            new StaticVerifier(verified),
            persistence,
            new Protection(),
            new NoopSubscriptionsClient(),
            NullLogger<GooglePlayPurchaseProcessor>.Instance);

        var context = new GooglePlayPurchaseProcessingContext(ProviderConfirmedRevocation: true);
        var result = await processor.ProcessAsync(
            userId,
            "raw-token",
            context,
            TestContext.Current.CancellationToken);
        var replay = await processor.ProcessAsync(userId, "raw-token", context, TestContext.Current.CancellationToken);
        var attemptedRecovery = await persistence.PersistAsync(Request(userId, GooglePlaySubscriptionLifecycleState.Active, FutureExpiry.AddDays(30)), TestContext.Current.CancellationToken);

        Assert.Equal(GooglePlayPurchaseProcessingResultCode.Verified, result.Code);
        Assert.Equal(GooglePlayPurchaseProcessingResultCode.Verified, replay.Code);
        Assert.Equal(GooglePlayVerifiedPurchasePersistenceResultCode.ConsistencyConflict, attemptedRecovery.Code);
        Assert.Equal(SubscriptionConstants.SubscriptionStatuses.Expired, Assert.Single(db.Subscriptions.Where(item => item.Provider == SubscriptionConstants.BillingProviders.GooglePlay)).Status);
        Assert.Equal(SubscriptionConstants.Entitlements.StatusRevoked, (await ExactGoogleEntitlementAsync(db)).Status);
        Assert.Equal(SubscriptionConstants.Entitlements.StatusActive, paddle.Entitlement.Status);
        Assert.Equal(SubscriptionConstants.Entitlements.StatusActive, manual.Status);
        Assert.Equal(SubscriptionConstants.Entitlements.StatusActive, Assert.Single(db.TrialGrants).Status);
    }

    [Fact]
    public async Task ConfirmedRevocationFreshExpiredPathPreservesCrossAccountTokenOwnership()
    {
        await using var db = CreateDb();
        var owner = await AddUserAsync(db);
        var other = await AddUserAsync(db);
        var persistence = CreateService(db);
        await persistence.PersistAsync(Request(owner, GooglePlaySubscriptionLifecycleState.Active), TestContext.Current.CancellationToken);
        var verified = new GooglePlayPurchaseVerificationResult(
            GooglePlayPurchaseVerificationResultCode.Verified,
            new GooglePlayVerifiedPurchase("com.example.test", "server-product", StartedAt, PastExpiry, GooglePlayPurchaseAcknowledgementState.Acknowledged, false, GooglePlaySubscriptionLifecycleState.Expired));
        var processor = new GooglePlayPurchaseProcessor(
            new StaticVerifier(verified),
            persistence,
            new Protection(),
            new NoopSubscriptionsClient(),
            NullLogger<GooglePlayPurchaseProcessor>.Instance);

        var result = await processor.ProcessAsync(
            other,
            "raw-token",
            new GooglePlayPurchaseProcessingContext(ProviderConfirmedRevocation: true),
            TestContext.Current.CancellationToken);

        Assert.Equal(GooglePlayPurchaseProcessingResultCode.OwnershipConflict, result.Code);
        Assert.Equal(SubscriptionConstants.Entitlements.StatusActive, (await ExactGoogleEntitlementAsync(db)).Status);
    }

    private static GooglePlayVerifiedPurchasePersistenceRequest Request(Guid userId, GooglePlaySubscriptionLifecycleState lifecycleState, DateTimeOffset? expiry = null) =>
        new(userId, "raw-token", new GooglePlayVerifiedPurchase("com.example.test", "server-product", StartedAt, expiry ?? FutureExpiry, GooglePlayPurchaseAcknowledgementState.Acknowledged, false, lifecycleState), "protected-token");

    private static GooglePlayVerifiedPurchasePersistenceService CreateService(AppDbContext db)
    {
        var clock = new TestClock(Now);
        var fingerprints = new GooglePlayPurchaseTokenFingerprintService();
        return new(db, new GooglePlayPurchaseClaimService(db, fingerprints, clock), new GooglePlayPurchaseTokenSecretPersistenceService(db, clock), fingerprints, clock, NullLogger<GooglePlayVerifiedPurchasePersistenceService>.Instance);
    }

    private static async Task<Guid> AddUserAsync(AppDbContext db)
    {
        var id = Guid.NewGuid();
        db.Users.Add(new UserEntity { Id = id, Email = $"{id:N}@example.test", PasswordHash = "hash", Status = "active", CreatedAt = Now });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return id;
    }

    private static async Task<(SubscriptionEntity Subscription, EntitlementEntity Entitlement)> AddProviderEntitlementAsync(AppDbContext db, Guid userId, string provider, DateTimeOffset expiry)
    {
        var subscription = new SubscriptionEntity { Id = Guid.NewGuid(), UserId = userId, PlanId = SubscriptionConstants.Plans.PremiumPlanId, Status = SubscriptionConstants.SubscriptionStatuses.Active, Provider = provider, ProviderSubscriptionId = Guid.NewGuid().ToString("N"), StartedAt = StartedAt, CurrentPeriodEndUtc = expiry, ExpiresAt = expiry, CreatedAt = Now, UpdatedAt = Now };
        var entitlement = new EntitlementEntity { Id = Guid.NewGuid(), UserId = userId, SubscriptionId = subscription.Id, PlanId = SubscriptionConstants.Plans.PremiumPlanId, EntitlementType = SubscriptionConstants.Entitlements.PremiumAccessType, Source = SubscriptionConstants.Entitlements.SourceProviderEvent, Status = SubscriptionConstants.Entitlements.StatusActive, StartsAtUtc = StartedAt, ExpiresAtUtc = expiry, CreatedAt = Now, UpdatedAt = Now };
        db.Subscriptions.Add(subscription);
        db.Entitlements.Add(entitlement);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return (subscription, entitlement);
    }

    private static async Task<EntitlementEntity> AddStandaloneEntitlementAsync(AppDbContext db, Guid userId, string source, DateTimeOffset expiry)
    {
        var entitlement = new EntitlementEntity { Id = Guid.NewGuid(), UserId = userId, PlanId = SubscriptionConstants.Plans.PremiumPlanId, EntitlementType = SubscriptionConstants.Entitlements.PremiumAccessType, Source = source, Status = SubscriptionConstants.Entitlements.StatusActive, StartsAtUtc = StartedAt, ExpiresAtUtc = expiry, CreatedAt = Now, UpdatedAt = Now };
        db.Entitlements.Add(entitlement);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return entitlement;
    }

    private static async Task AddTrialAsync(AppDbContext db, Guid userId, DateTimeOffset expiry)
    {
        db.TrialGrants.Add(new TrialGrantEntity { Id = Guid.NewGuid(), UserId = userId, GrantedAtUtc = StartedAt, ExpiresAtUtc = expiry, SourcePlatform = "test", Status = SubscriptionConstants.Entitlements.StatusActive, CreatedAt = Now });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    private static Task<EntitlementEntity> ExactGoogleEntitlementAsync(AppDbContext db) => db.Entitlements.SingleAsync(item => item.Subscription != null && item.Subscription.Provider == SubscriptionConstants.BillingProviders.GooglePlay, TestContext.Current.CancellationToken);

    private static Task<EnglishVoiceTutor.Api.Contracts.Subscription.SubscriptionStatusResponse> StatusAsync(AppDbContext db, Guid userId) =>
        new SubscriptionStatusService(db, Microsoft.Extensions.Options.Options.Create(new SubscriptionEnforcementOptions())).GetStatusAsync(userId, "test", TestContext.Current.CancellationToken);

    private static AppDbContext CreateDb() => new(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString("N")).ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning)).Options);
    private sealed class TestClock(DateTimeOffset now) : IUtcClock { public DateTimeOffset UtcNow => now; }
    private sealed class StaticVerifier : IGooglePlayPurchaseVerifier
    {
        private readonly GooglePlayPurchaseVerificationResult result;
        public StaticVerifier(GooglePlayPurchaseVerificationResultCode code) : this(new GooglePlayPurchaseVerificationResult(code)) { }
        public StaticVerifier(GooglePlayPurchaseVerificationResult result) => this.result = result;
        public Task<GooglePlayPurchaseVerificationResult> VerifyAsync(Guid userId, string purchaseToken, CancellationToken cancellationToken) => Task.FromResult(result);
    }
    private sealed class Protection : IGooglePlayPurchaseTokenProtectionService { public string Protect(string purchaseToken) => "protected"; public GooglePlayPurchaseTokenUnprotectResult TryUnprotect(string protectedPurchaseToken) => new(true, "raw-token"); }
    private sealed class NoopSubscriptionsClient : IGooglePlaySubscriptionsV2Client { public Task<GooglePlaySubscriptionV2Snapshot?> GetAsync(string packageName, string purchaseToken, CancellationToken cancellationToken) => throw new NotSupportedException(); public Task AcknowledgeAsync(string packageName, string productId, string purchaseToken, CancellationToken cancellationToken) => Task.CompletedTask; }
}
