using EnglishVoiceTutor.Api.Constants;
using EnglishVoiceTutor.Api.Data;
using EnglishVoiceTutor.Api.Data.Entities;
using EnglishVoiceTutor.Api.Services;
using EnglishVoiceTutor.Api.Services.Billing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;

namespace EnglishVoiceTutor.Api.Tests.Services;

public sealed class GooglePlayLinkedPurchaseLifecycleTests
{
    private static readonly DateTimeOffset Start = new(2026, 8, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset End = Start.AddDays(30);

    [Fact]
    public async Task SameOwnerTransferSupersedesOldSecretAndRetiresOnlyItsProviderAccess()
    {
        await using var db = CreateDb();
        var userId = await AddUserAsync(db);
        var service = CreateService(db);
        await service.PersistAsync(Request(userId, "old-token", "old-product"), TestContext.Current.CancellationToken);

        var result = await service.PersistAsync(Request(userId, "new-token", "new-product", "old-token", End.AddDays(30)), TestContext.Current.CancellationToken);

        Assert.Equal(GooglePlayVerifiedPurchasePersistenceResultCode.Applied, result.Code);
        Assert.Equal(2, db.GooglePlayPurchaseClaims.Count());
        Assert.Equal(2, db.GooglePlayPurchaseTokenSecrets.Count());
        var oldClaim = db.GooglePlayPurchaseClaims.Single(item => item.ProductId == "old-product");
        var oldSecret = db.GooglePlayPurchaseTokenSecrets.Single(item => item.GooglePlayPurchaseClaimId == oldClaim.Id);
        Assert.NotNull(oldSecret.SupersededAtUtc);
        Assert.Null(oldSecret.NextProviderCheckAtUtc);
        Assert.False(oldSecret.AcknowledgementPending);
        var oldSubscription = db.Subscriptions.Single(item => item.ProviderSubscriptionId == Fingerprint("old-token"));
        Assert.Equal(SubscriptionConstants.SubscriptionStatuses.Expired, oldSubscription.Status);
        Assert.Equal(SubscriptionConstants.Entitlements.StatusInactive, db.Entitlements.Single(item => item.SubscriptionId == oldSubscription.Id).Status);
        Assert.Equal(SubscriptionConstants.SubscriptionStatuses.Active, db.Subscriptions.Single(item => item.ProviderSubscriptionId == Fingerprint("new-token")).Status);
    }

    [Fact]
    public async Task KnownLinkedClaimWithoutSecretFailsBeforeCreatingNewState()
    {
        await using var db = CreateDb();
        var userId = await AddUserAsync(db);
        var claim = new GooglePlayPurchaseClaimEntity { Id = Guid.NewGuid(), UserId = userId, ProductId = "old-product", PurchaseTokenFingerprint = Fingerprint("old-token"), CreatedAtUtc = Start, LastSeenAtUtc = Start };
        db.GooglePlayPurchaseClaims.Add(claim);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await CreateService(db).PersistAsync(Request(userId, "new-token", "new-product", "old-token"), TestContext.Current.CancellationToken);

        Assert.Equal(GooglePlayVerifiedPurchasePersistenceResultCode.ConsistencyConflict, result.Code);
        Assert.Single(db.GooglePlayPurchaseClaims);
        Assert.Empty(db.Subscriptions);
        Assert.Empty(db.Entitlements);
        Assert.Empty(db.GooglePlayPurchaseTokenSecrets);
    }

    [Fact]
    public async Task SupersededOldTokenCannotCreateBranchButAcceptedCurrentReplayIsIdempotent()
    {
        await using var db = CreateDb();
        var userId = await AddUserAsync(db);
        var service = CreateService(db);
        await service.PersistAsync(Request(userId, "old-token", "old-product"), TestContext.Current.CancellationToken);
        await service.PersistAsync(Request(userId, "new-token", "new-product", "old-token"), TestContext.Current.CancellationToken);

        var replay = await service.PersistAsync(Request(userId, "new-token", "new-product", "old-token"), TestContext.Current.CancellationToken);
        var branch = await service.PersistAsync(Request(userId, "branch-token", "new-product", "old-token"), TestContext.Current.CancellationToken);
        var oldReplay = await service.PersistAsync(Request(userId, "old-token", "old-product"), TestContext.Current.CancellationToken);

        Assert.Equal(GooglePlayVerifiedPurchasePersistenceResultCode.AlreadyCurrent, replay.Code);
        Assert.Equal(GooglePlayVerifiedPurchasePersistenceResultCode.ConsistencyConflict, branch.Code);
        Assert.Equal(GooglePlayVerifiedPurchasePersistenceResultCode.ConsistencyConflict, oldReplay.Code);
        Assert.Equal(2, db.GooglePlayPurchaseClaims.Count());
        Assert.Equal(2, db.Subscriptions.Count());
        Assert.Equal(2, db.GooglePlayPurchaseTokenSecrets.Count());
    }

    [Fact]
    public async Task DeferredActivationUpdatesExistingCurrentTokenProductWithoutDuplicates()
    {
        await using var db = CreateDb();
        var userId = await AddUserAsync(db);
        var service = CreateService(db);
        await service.PersistAsync(Request(userId, "old-token", "old-product"), TestContext.Current.CancellationToken);
        await service.PersistAsync(Request(userId, "new-token", "old-product", "old-token"), TestContext.Current.CancellationToken);

        var result = await service.PersistAsync(Request(userId, "new-token", "replacement-product", "old-token", End.AddDays(30)), TestContext.Current.CancellationToken);

        Assert.Equal(GooglePlayVerifiedPurchasePersistenceResultCode.Applied, result.Code);
        Assert.Equal(2, db.GooglePlayPurchaseClaims.Count());
        Assert.Equal("replacement-product", db.GooglePlayPurchaseClaims.Single(item => item.PurchaseTokenFingerprint == Fingerprint("new-token")).ProductId);
        var subscription = db.Subscriptions.Single(item => item.ProviderSubscriptionId == Fingerprint("new-token"));
        Assert.Equal("replacement-product", subscription.ProviderProductId);
        Assert.Equal(End.AddDays(30), subscription.CurrentPeriodEndUtc);
        Assert.Single(db.Entitlements.Where(item => item.SubscriptionId == subscription.Id));
    }

    [Fact]
    public async Task UnknownLinkedClaimCreatesOnlyTheCurrentPurchaseRecords()
    {
        await using var db = CreateDb();
        var userId = await AddUserAsync(db);

        var result = await CreateService(db).PersistAsync(Request(userId, "new-token", "new-product", "unknown-old-token"), TestContext.Current.CancellationToken);

        Assert.Equal(GooglePlayVerifiedPurchasePersistenceResultCode.Applied, result.Code);
        Assert.Single(db.GooglePlayPurchaseClaims);
        Assert.Single(db.Subscriptions);
        Assert.Single(db.Entitlements);
        Assert.Single(db.GooglePlayPurchaseTokenSecrets);
        Assert.DoesNotContain(db.GooglePlayPurchaseClaims, item => item.PurchaseTokenFingerprint == Fingerprint("unknown-old-token"));
    }

    [Fact]
    public async Task CrossAccountLinkedClaimRejectsWithoutChangingEitherOwnersRecords()
    {
        await using var db = CreateDb();
        var owner = await AddUserAsync(db);
        var other = await AddUserAsync(db);
        var service = CreateService(db);
        await service.PersistAsync(Request(owner, "old-token", "old-product"), TestContext.Current.CancellationToken);
        var oldSubscription = Assert.Single(db.Subscriptions);
        var oldEntitlement = Assert.Single(db.Entitlements);

        var result = await service.PersistAsync(Request(other, "new-token", "new-product", "old-token"), TestContext.Current.CancellationToken);

        Assert.Equal(GooglePlayVerifiedPurchasePersistenceResultCode.OwnershipConflict, result.Code);
        Assert.Single(db.GooglePlayPurchaseClaims);
        Assert.Single(db.GooglePlayPurchaseTokenSecrets);
        Assert.Equal(SubscriptionConstants.SubscriptionStatuses.Active, oldSubscription.Status);
        Assert.Equal(SubscriptionConstants.Entitlements.StatusActive, oldEntitlement.Status);
    }

    private static GooglePlayVerifiedPurchasePersistenceRequest Request(Guid userId, string token, string product, string? linkedToken = null, DateTimeOffset? expiry = null) =>
        new(userId, token, new GooglePlayVerifiedPurchase("com.example.test", product, Start, expiry ?? End, GooglePlayPurchaseAcknowledgementState.Pending, false) { LinkedPurchaseToken = linkedToken }, "protected-value");
    private static string Fingerprint(string token) => new GooglePlayPurchaseTokenFingerprintService().CreateFingerprint(token);
    private static GooglePlayVerifiedPurchasePersistenceService CreateService(AppDbContext db)
    {
        var clock = new TestClock();
        var fingerprint = new GooglePlayPurchaseTokenFingerprintService();
        return new(db, new GooglePlayPurchaseClaimService(db, fingerprint, clock), new ProviderSubscriptionPeriodPersistenceService(db, NullLogger<ProviderSubscriptionPeriodPersistenceService>.Instance), new GooglePlayPurchaseTokenSecretPersistenceService(db, clock), fingerprint, clock, NullLogger<GooglePlayVerifiedPurchasePersistenceService>.Instance);
    }
    private static async Task<Guid> AddUserAsync(AppDbContext db)
    {
        var id = Guid.NewGuid();
        db.Users.Add(new UserEntity { Id = id, Email = $"{id:N}@example.test", PasswordHash = "hash", Status = "active", CreatedAt = Start });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return id;
    }
    private static AppDbContext CreateDb() => new(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString("N")).ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning)).Options);
    private sealed class TestClock : IUtcClock { public DateTimeOffset UtcNow => Start.AddDays(1); }
}
