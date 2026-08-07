using EnglishVoiceTutor.Api.Constants;
using EnglishVoiceTutor.Api.Data;
using EnglishVoiceTutor.Api.Data.Entities;
using EnglishVoiceTutor.Api.Services;
using EnglishVoiceTutor.Api.Services.Billing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;

namespace EnglishVoiceTutor.Api.Tests.Services;

public sealed class GooglePlayVerifiedPurchasePersistenceServiceTests
{
    [Fact]
    public async Task FirstValidPurchaseCreatesClaimSubscriptionAndExactLinkedEntitlement()
    {
        await using var db = CreateDb();
        var userId = await AddUserAsync(db);

        var result = await CreateService(db).PersistAsync(Request(userId), TestContext.Current.CancellationToken);

        Assert.Equal(GooglePlayVerifiedPurchasePersistenceResultCode.Applied, result.Code);
        var claim = Assert.Single(db.GooglePlayPurchaseClaims);
        var subscription = Assert.Single(db.Subscriptions);
        var entitlement = Assert.Single(db.Entitlements);
        Assert.Equal(userId, claim.UserId);
        Assert.Equal(SubscriptionConstants.BillingProviders.GooglePlay, subscription.Provider);
        Assert.Equal(claim.PurchaseTokenFingerprint, subscription.ProviderSubscriptionId);
        Assert.Equal(subscription.Id, entitlement.SubscriptionId);
        var secret = Assert.Single(db.GooglePlayPurchaseTokenSecrets);
        Assert.Equal(claim.Id, secret.GooglePlayPurchaseClaimId);
        Assert.NotEqual("raw-token", secret.ProtectedPurchaseToken);
        Assert.True(secret.AcknowledgementPending);
        AssertNoPaymentsOrBillingEvents(db);
    }

    [Fact]
    public async Task ExistingClaimWithoutSecretIsFilledAndReplayUpdatesOneSecretRow()
    {
        await using var db = CreateDb();
        var userId = await AddUserAsync(db);
        var fingerprint = Fingerprint("raw-token");
        db.GooglePlayPurchaseClaims.Add(new GooglePlayPurchaseClaimEntity { Id = Guid.NewGuid(), UserId = userId, PurchaseTokenFingerprint = fingerprint, ProductId = "server-product", CreatedAtUtc = Start, LastSeenAtUtc = Start });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        await CreateService(db).PersistAsync(Request(userId), TestContext.Current.CancellationToken);
        await CreateService(db).PersistAsync(Request(userId), TestContext.Current.CancellationToken);

        Assert.Single(db.GooglePlayPurchaseClaims);
        Assert.Single(db.GooglePlayPurchaseTokenSecrets);
    }

    [Fact]
    public async Task AcknowledgementStateKeepsProtectedSecretForFailuresAndClearsAfterSuccess()
    {
        await using var db = CreateDb();
        var userId = await AddUserAsync(db);
        var service = CreateService(db);
        await service.PersistAsync(Request(userId), TestContext.Current.CancellationToken);

        await service.UpdateAcknowledgementStateAsync("raw-token", true, GooglePlayRtdnSafeErrorCodes.ProviderUnavailable, TestContext.Current.CancellationToken);
        var secret = Assert.Single(db.GooglePlayPurchaseTokenSecrets);
        Assert.True(secret.AcknowledgementPending);
        Assert.Equal(GooglePlayRtdnSafeErrorCodes.ProviderUnavailable, secret.LastSafeResultCode);
        await service.UpdateAcknowledgementStateAsync("raw-token", true, GooglePlayRtdnSafeErrorCodes.ProviderRejected, TestContext.Current.CancellationToken);
        Assert.True(secret.AcknowledgementPending);
        Assert.Equal(GooglePlayRtdnSafeErrorCodes.ProviderRejected, secret.LastSafeResultCode);
        await service.UpdateAcknowledgementStateAsync("raw-token", false, null, TestContext.Current.CancellationToken);
        Assert.False(secret.AcknowledgementPending);
        Assert.Null(secret.LastSafeResultCode);
    }

    [Fact]
    public async Task RawPurchaseTokenIsNotPersistedInBillingRecords()
    {
        await using var db = CreateDb();
        var userId = await AddUserAsync(db);

        await CreateService(db).PersistAsync(Request(userId, token: "raw-token"), TestContext.Current.CancellationToken);

        var claim = Assert.Single(db.GooglePlayPurchaseClaims);
        var subscription = Assert.Single(db.Subscriptions);
        Assert.NotEqual("raw-token", claim.PurchaseTokenFingerprint);
        Assert.Matches("^[0-9a-f]{64}$", claim.PurchaseTokenFingerprint);
        Assert.DoesNotContain("raw-token", subscription.ProviderSubscriptionId, StringComparison.Ordinal);
        Assert.DoesNotContain("raw-token", subscription.ProviderProductId, StringComparison.Ordinal);
        AssertNoPaymentsOrBillingEvents(db);
    }

    [Fact]
    public async Task SameUserSameTokenReplayIsIdempotent()
    {
        await using var db = CreateDb();
        var userId = await AddUserAsync(db);
        var service = CreateService(db);
        await service.PersistAsync(Request(userId), TestContext.Current.CancellationToken);

        var result = await service.PersistAsync(Request(userId), TestContext.Current.CancellationToken);

        Assert.Equal(GooglePlayVerifiedPurchasePersistenceResultCode.AlreadyCurrent, result.Code);
        Assert.Single(db.GooglePlayPurchaseClaims);
        Assert.Single(db.Subscriptions);
        Assert.Single(db.Entitlements);
        AssertNoPaymentsOrBillingEvents(db);
    }

    [Fact]
    public async Task ReplayUpdatesClaimLastSeenAtUtc()
    {
        await using var db = CreateDb();
        var userId = await AddUserAsync(db);
        var clock = new TestClock(Start);
        var service = CreateService(db, clock);
        await service.PersistAsync(Request(userId), TestContext.Current.CancellationToken);
        clock.UtcNow = Start.AddMinutes(5);

        await service.PersistAsync(Request(userId), TestContext.Current.CancellationToken);

        Assert.Equal(clock.UtcNow, Assert.Single(db.GooglePlayPurchaseClaims).LastSeenAtUtc);
        AssertNoPaymentsOrBillingEvents(db);
    }

    [Fact]
    public async Task LaterVerifiedPeriodExtendsExactSubscriptionAndEntitlement()
    {
        await using var db = CreateDb();
        var userId = await AddUserAsync(db);
        var service = CreateService(db);
        await service.PersistAsync(Request(userId), TestContext.Current.CancellationToken);
        var laterExpiry = End.AddDays(30);

        var result = await service.PersistAsync(Request(userId, expiry: laterExpiry), TestContext.Current.CancellationToken);

        Assert.Equal(GooglePlayVerifiedPurchasePersistenceResultCode.Applied, result.Code);
        Assert.Equal(laterExpiry, Assert.Single(db.Subscriptions).CurrentPeriodEndUtc);
        Assert.Equal(laterExpiry, Assert.Single(db.Entitlements).ExpiresAtUtc);
        AssertNoPaymentsOrBillingEvents(db);
    }

    [Fact]
    public async Task EqualVerifiedPeriodDoesNotDuplicateOrShortenRecords()
    {
        await using var db = CreateDb();
        var userId = await AddUserAsync(db);
        var service = CreateService(db);
        await service.PersistAsync(Request(userId), TestContext.Current.CancellationToken);

        var result = await service.PersistAsync(Request(userId), TestContext.Current.CancellationToken);

        Assert.Equal(GooglePlayVerifiedPurchasePersistenceResultCode.AlreadyCurrent, result.Code);
        Assert.Single(db.Subscriptions);
        Assert.Single(db.Entitlements);
        Assert.Equal(End, Assert.Single(db.Entitlements).ExpiresAtUtc);
        AssertNoPaymentsOrBillingEvents(db);
    }

    [Fact]
    public async Task EarlierVerifiedPeriodDoesNotShortenRecords()
    {
        await using var db = CreateDb();
        var userId = await AddUserAsync(db);
        var service = CreateService(db);
        await service.PersistAsync(Request(userId), TestContext.Current.CancellationToken);

        var result = await service.PersistAsync(Request(userId, expiry: End.AddDays(-1)), TestContext.Current.CancellationToken);

        Assert.Equal(GooglePlayVerifiedPurchasePersistenceResultCode.AlreadyCurrent, result.Code);
        Assert.Equal(End, Assert.Single(db.Subscriptions).CurrentPeriodEndUtc);
        Assert.Equal(End, Assert.Single(db.Entitlements).ExpiresAtUtc);
        AssertNoPaymentsOrBillingEvents(db);
    }

    [Fact]
    public async Task OpenEndedLinkedEntitlementRemainsOpenEnded()
    {
        await using var db = CreateDb();
        var userId = await AddUserAsync(db);
        var service = CreateService(db);
        await service.PersistAsync(Request(userId), TestContext.Current.CancellationToken);
        Assert.Single(db.Entitlements).ExpiresAtUtc = null;
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        await service.PersistAsync(Request(userId, expiry: End.AddDays(1)), TestContext.Current.CancellationToken);

        Assert.Null(Assert.Single(db.Entitlements).ExpiresAtUtc);
        AssertNoPaymentsOrBillingEvents(db);
    }

    [Fact]
    public async Task AnotherUserCannotReuseTokenAndOwnerIsNotExposed()
    {
        await using var db = CreateDb();
        var owner = await AddUserAsync(db);
        var other = await AddUserAsync(db);
        var service = CreateService(db);
        await service.PersistAsync(Request(owner), TestContext.Current.CancellationToken);
        var lastSeen = Assert.Single(db.GooglePlayPurchaseClaims).LastSeenAtUtc;

        var result = await service.PersistAsync(Request(other), TestContext.Current.CancellationToken);

        Assert.Equal(GooglePlayVerifiedPurchasePersistenceResultCode.OwnershipConflict, result.Code);
        Assert.DoesNotContain(owner.ToString(), result.ToString(), StringComparison.Ordinal);
        Assert.Equal(lastSeen, Assert.Single(db.GooglePlayPurchaseClaims).LastSeenAtUtc);
        Assert.Equal(Assert.Single(db.GooglePlayPurchaseClaims).Id, Assert.Single(db.GooglePlayPurchaseTokenSecrets).GooglePlayPurchaseClaimId);
        AssertNoPaymentsOrBillingEvents(db);
    }

    [Fact]
    public async Task ChangedVerifiedProductIsRejectedWithoutChangingRecords()
    {
        await using var db = CreateDb();
        var userId = await AddUserAsync(db);
        var service = CreateService(db);
        await service.PersistAsync(Request(userId), TestContext.Current.CancellationToken);
        var subscription = Assert.Single(db.Subscriptions);
        var entitlement = Assert.Single(db.Entitlements);
        var lastSeen = Assert.Single(db.GooglePlayPurchaseClaims).LastSeenAtUtc;

        var result = await service.PersistAsync(Request(userId, product: "other-product"), TestContext.Current.CancellationToken);

        Assert.Equal(GooglePlayVerifiedPurchasePersistenceResultCode.ProductMismatch, result.Code);
        Assert.Equal("server-product", Assert.Single(db.GooglePlayPurchaseClaims).ProductId);
        Assert.Equal(lastSeen, Assert.Single(db.GooglePlayPurchaseClaims).LastSeenAtUtc);
        Assert.Equal("server-product", subscription.ProviderProductId);
        Assert.Equal(End, entitlement.ExpiresAtUtc);
        AssertNoPaymentsOrBillingEvents(db);
    }

    [Fact]
    public async Task GateApprovedTestPurchaseUsesTheSameDurableBillingFoundation()
    {
        await using var db = CreateDb();
        var userId = await AddUserAsync(db);

        var result = await CreateService(db).PersistAsync(Request(userId, test: true), TestContext.Current.CancellationToken);

        Assert.Equal(GooglePlayVerifiedPurchasePersistenceResultCode.Applied, result.Code);
        Assert.Single(db.GooglePlayPurchaseClaims);
        Assert.Single(db.Subscriptions);
        Assert.Single(db.Entitlements);
    }

    [Theory]
    [InlineData("empty-user")]
    [InlineData("blank-token")]
    [InlineData("blank-product")]
    [InlineData("non-utc-start")]
    [InlineData("non-utc-expiry")]
    [InlineData("equal-period")]
    [InlineData("reversed-period")]
    public async Task InvalidInputCreatesNoBillingRecords(string scenario)
    {
        await using var db = CreateDb();
        var userId = await AddUserAsync(db);
        var request = scenario switch
        {
            "empty-user" => Request(Guid.Empty),
            "blank-token" => Request(userId, token: " "),
            "blank-product" => Request(userId, product: " "),
            "non-utc-start" => Request(userId, start: new DateTimeOffset(2026, 7, 27, 12, 0, 0, TimeSpan.FromHours(2))),
            "non-utc-expiry" => Request(userId, expiry: new DateTimeOffset(2026, 8, 26, 12, 0, 0, TimeSpan.FromHours(2))),
            "equal-period" => Request(userId, start: Start, expiry: Start),
            "reversed-period" => Request(userId, start: End, expiry: Start),
            _ => throw new InvalidOperationException()
        };

        var result = await CreateService(db).PersistAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(GooglePlayVerifiedPurchasePersistenceResultCode.InvalidInput, result.Code);
        AssertNoBillingRecords(db);
    }

    [Fact]
    public async Task ExistingGoogleSubscriptionOwnedByAnotherUserIsRejectedBeforeClaimWrite()
    {
        await using var db = CreateDb();
        var owner = await AddUserAsync(db);
        var userId = await AddUserAsync(db);
        var fingerprint = Fingerprint("raw-token");
        await AddSubscriptionAsync(db, owner, fingerprint);

        var result = await CreateService(db).PersistAsync(Request(userId), TestContext.Current.CancellationToken);

        Assert.Equal(GooglePlayVerifiedPurchasePersistenceResultCode.ConsistencyConflict, result.Code);
        Assert.Empty(db.GooglePlayPurchaseClaims);
        Assert.Single(db.Subscriptions);
        Assert.Empty(db.Entitlements);
        AssertNoPaymentsOrBillingEvents(db);
    }

    [Fact]
    public async Task ExistingGoogleSubscriptionWithWrongPlanIsRejectedWithoutWrites()
    {
        await using var db = CreateDb();
        var userId = await AddUserAsync(db);
        await AddSubscriptionAsync(db, userId, Fingerprint("raw-token"), planId: "free");

        var result = await CreateService(db).PersistAsync(Request(userId), TestContext.Current.CancellationToken);

        Assert.Equal(GooglePlayVerifiedPurchasePersistenceResultCode.ConsistencyConflict, result.Code);
        Assert.Empty(db.GooglePlayPurchaseClaims);
        Assert.Empty(db.Entitlements);
        AssertNoPaymentsOrBillingEvents(db);
    }

    [Fact]
    public async Task ExistingGoogleSubscriptionWithDifferentProductIsRejectedWithoutWrites()
    {
        await using var db = CreateDb();
        var userId = await AddUserAsync(db);
        var subscription = await AddSubscriptionAsync(db, userId, Fingerprint("raw-token"), product: "other-product");

        var result = await CreateService(db).PersistAsync(Request(userId), TestContext.Current.CancellationToken);

        Assert.Equal(GooglePlayVerifiedPurchasePersistenceResultCode.ConsistencyConflict, result.Code);
        Assert.Equal("other-product", subscription.ProviderProductId);
        Assert.Empty(db.GooglePlayPurchaseClaims);
        Assert.Empty(db.Entitlements);
        AssertNoPaymentsOrBillingEvents(db);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task ExistingGoogleSubscriptionWithBlankProductIsFilled(string? existingProduct)
    {
        await using var db = CreateDb();
        var userId = await AddUserAsync(db);
        var subscription = await AddSubscriptionAsync(db, userId, Fingerprint("raw-token"), product: existingProduct);

        var result = await CreateService(db).PersistAsync(Request(userId), TestContext.Current.CancellationToken);

        Assert.Equal(GooglePlayVerifiedPurchasePersistenceResultCode.Applied, result.Code);
        Assert.Equal("server-product", subscription.ProviderProductId);
        Assert.Single(db.GooglePlayPurchaseClaims);
        Assert.Single(db.Entitlements);
        AssertNoPaymentsOrBillingEvents(db);
    }

    [Fact]
    public async Task ExistingPaddleSubscriptionAndEntitlementRemainUnchanged()
    {
        await using var db = CreateDb();
        var userId = await AddUserAsync(db);
        var paddle = await AddSubscriptionAsync(db, userId, "paddle-subscription", provider: SubscriptionConstants.BillingProviders.Paddle, product: "paddle-product");
        var paddleEntitlement = await AddEntitlementAsync(db, userId, paddle.Id, End.AddDays(10));

        await CreateService(db).PersistAsync(Request(userId), TestContext.Current.CancellationToken);

        Assert.Equal("paddle-subscription", paddle.ProviderSubscriptionId);
        Assert.Equal(End.AddDays(10), paddleEntitlement.ExpiresAtUtc);
        Assert.Equal(paddle.Id, paddleEntitlement.SubscriptionId);
        AssertNoPaymentsOrBillingEvents(db);
    }

    [Fact]
    public async Task ExistingUnscopedEntitlementRemainsUnchanged()
    {
        await using var db = CreateDb();
        var userId = await AddUserAsync(db);
        var unscoped = await AddEntitlementAsync(db, userId, null, End.AddDays(10));

        await CreateService(db).PersistAsync(Request(userId), TestContext.Current.CancellationToken);

        Assert.Equal(End.AddDays(10), unscoped.ExpiresAtUtc);
        Assert.Null(unscoped.SubscriptionId);
        Assert.Equal(2, db.Entitlements.Count());
        AssertNoPaymentsOrBillingEvents(db);
    }

    [Fact]
    public async Task PendingAcknowledgementPersistsWithoutAcknowledgementAction()
    {
        await using var db = CreateDb();
        var userId = await AddUserAsync(db);

        var result = await CreateService(db).PersistAsync(Request(userId, acknowledgement: GooglePlayPurchaseAcknowledgementState.Pending), TestContext.Current.CancellationToken);

        Assert.Equal(GooglePlayVerifiedPurchasePersistenceResultCode.Applied, result.Code);
        Assert.Single(db.GooglePlayPurchaseClaims);
        AssertNoPaymentsOrBillingEvents(db);
    }

    [Fact]
    public async Task AcknowledgedPurchasePersistsAndReplayRemainsIdempotent()
    {
        await using var db = CreateDb();
        var userId = await AddUserAsync(db);
        var service = CreateService(db);

        var first = await service.PersistAsync(Request(userId, acknowledgement: GooglePlayPurchaseAcknowledgementState.Acknowledged), TestContext.Current.CancellationToken);
        var replay = await service.PersistAsync(Request(userId, acknowledgement: GooglePlayPurchaseAcknowledgementState.Acknowledged), TestContext.Current.CancellationToken);

        Assert.Equal(GooglePlayVerifiedPurchasePersistenceResultCode.Applied, first.Code);
        Assert.Equal(GooglePlayVerifiedPurchasePersistenceResultCode.AlreadyCurrent, replay.Code);
        Assert.Single(db.GooglePlayPurchaseClaims);
        AssertNoPaymentsOrBillingEvents(db);
    }

    private static readonly DateTimeOffset Start = new(2026, 7, 27, 10, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset End = Start.AddDays(30);

    private static GooglePlayVerifiedPurchasePersistenceRequest Request(Guid userId, string product = "server-product", string token = "raw-token", DateTimeOffset? start = null, DateTimeOffset? expiry = null, bool test = false, GooglePlayPurchaseAcknowledgementState acknowledgement = GooglePlayPurchaseAcknowledgementState.Pending) => new(userId, token, new GooglePlayVerifiedPurchase("com.example.test", product, start ?? Start, expiry ?? End, acknowledgement, test), "protected-test-value");
    private static string Fingerprint(string token) => new GooglePlayPurchaseTokenFingerprintService().CreateFingerprint(token);
    private static GooglePlayVerifiedPurchasePersistenceService CreateService(AppDbContext db, TestClock? clock = null) { var actualClock = clock ?? new TestClock(Start); var fingerprint = new GooglePlayPurchaseTokenFingerprintService(); var claim = new GooglePlayPurchaseClaimService(db, fingerprint, actualClock); var secrets = new GooglePlayPurchaseTokenSecretPersistenceService(db, actualClock); return new(db, claim, secrets, fingerprint, actualClock, NullLogger<GooglePlayVerifiedPurchasePersistenceService>.Instance); }
    private static async Task<Guid> AddUserAsync(AppDbContext db) { var id = Guid.NewGuid(); db.Users.Add(new UserEntity { Id = id, Email = $"{id:N}@example.test", PasswordHash = "hash", Status = "active", CreatedAt = Start }); await db.SaveChangesAsync(TestContext.Current.CancellationToken); return id; }
    private static async Task<SubscriptionEntity> AddSubscriptionAsync(AppDbContext db, Guid userId, string providerSubscriptionId, string? product = "server-product", string provider = SubscriptionConstants.BillingProviders.GooglePlay, string planId = SubscriptionConstants.Plans.PremiumPlanId) { var subscription = new SubscriptionEntity { Id = Guid.NewGuid(), UserId = userId, PlanId = planId, Status = SubscriptionConstants.SubscriptionStatuses.Active, Provider = provider, ProviderSubscriptionId = providerSubscriptionId, ProviderProductId = product, StartedAt = Start, CreatedAt = Start, UpdatedAt = Start }; db.Subscriptions.Add(subscription); await db.SaveChangesAsync(TestContext.Current.CancellationToken); return subscription; }
    private static async Task<EntitlementEntity> AddEntitlementAsync(AppDbContext db, Guid userId, Guid? subscriptionId, DateTimeOffset? expiry) { var entitlement = new EntitlementEntity { Id = Guid.NewGuid(), UserId = userId, SubscriptionId = subscriptionId, PlanId = SubscriptionConstants.Plans.PremiumPlanId, EntitlementType = SubscriptionConstants.Entitlements.PremiumAccessType, Source = SubscriptionConstants.Entitlements.SourceProviderEvent, Status = SubscriptionConstants.Entitlements.StatusActive, StartsAtUtc = Start, ExpiresAtUtc = expiry, CreatedAt = Start, UpdatedAt = Start }; db.Entitlements.Add(entitlement); await db.SaveChangesAsync(TestContext.Current.CancellationToken); return entitlement; }
    private static void AssertNoBillingRecords(AppDbContext db) { Assert.Empty(db.GooglePlayPurchaseClaims); Assert.Empty(db.Subscriptions); Assert.Empty(db.Entitlements); AssertNoPaymentsOrBillingEvents(db); }
    private static void AssertNoPaymentsOrBillingEvents(AppDbContext db) { Assert.Empty(db.Payments); Assert.Empty(db.BillingEvents); }
    private static AppDbContext CreateDb() => new(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString("N")).ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning)).Options);
    private sealed class TestClock(DateTimeOffset now) : IUtcClock { public DateTimeOffset UtcNow { get; set; } = now; }
}
