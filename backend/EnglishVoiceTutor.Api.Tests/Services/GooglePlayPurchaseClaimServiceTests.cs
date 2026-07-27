using EnglishVoiceTutor.Api.Data;
using EnglishVoiceTutor.Api.Data.Entities;
using EnglishVoiceTutor.Api.Services;
using EnglishVoiceTutor.Api.Services.Billing;
using Microsoft.EntityFrameworkCore;

namespace EnglishVoiceTutor.Api.Tests.Services;

public sealed class GooglePlayPurchaseClaimServiceTests
{
    [Fact]
    public async Task FirstClaimStoresOnlyFingerprintAndServerVerifiedProduct()
    {
        await using var db = CreateDb();
        var userId = await AddUserAsync(db);
        var result = await CreateService(db).ClaimAsync(userId, "fake-token-one", "server-verified-product", TestContext.Current.CancellationToken);

        var claim = Assert.Single(db.GooglePlayPurchaseClaims);
        Assert.Equal(GooglePlayPurchaseClaimResultCode.Claimed, result.Code);
        Assert.Equal("server-verified-product", claim.ProductId);
        Assert.NotEqual("fake-token-one", claim.PurchaseTokenFingerprint);
        Assert.Matches("^[0-9a-f]{64}$", claim.PurchaseTokenFingerprint);
        Assert.Empty(db.Subscriptions);
        Assert.Empty(db.Entitlements);
        Assert.Empty(db.Payments);
        Assert.Empty(db.BillingEvents);
    }

    [Fact]
    public async Task SameUserRetryIsIdempotentAndUpdatesLastSeen()
    {
        await using var db = CreateDb();
        var userId = await AddUserAsync(db);
        var clock = new TestClock(DateTimeOffset.Parse("2026-07-27T00:00:00Z"));
        var service = CreateService(db, clock);

        var first = await service.ClaimAsync(userId, "fake-token-one", "server-product", TestContext.Current.CancellationToken);
        clock.UtcNow = clock.UtcNow.AddMinutes(5);
        var retry = await service.ClaimAsync(userId, "fake-token-one", "different-server-product", TestContext.Current.CancellationToken);

        var claim = Assert.Single(db.GooglePlayPurchaseClaims);
        Assert.Equal(GooglePlayPurchaseClaimResultCode.Claimed, first.Code);
        Assert.Equal(GooglePlayPurchaseClaimResultCode.AlreadyOwned, retry.Code);
        Assert.Equal(DateTimeOffset.Parse("2026-07-27T00:05:00Z"), claim.LastSeenAtUtc);
        Assert.Equal("server-product", claim.ProductId);
    }

    [Fact]
    public async Task AnotherUserCannotClaimExistingFingerprintOrLearnOwner()
    {
        await using var db = CreateDb();
        var ownerId = await AddUserAsync(db);
        var otherId = await AddUserAsync(db);
        var service = CreateService(db);
        await service.ClaimAsync(ownerId, "fake-token-one", "server-product", TestContext.Current.CancellationToken);

        var result = await service.ClaimAsync(otherId, "fake-token-one", "other-server-product", TestContext.Current.CancellationToken);

        var claim = Assert.Single(db.GooglePlayPurchaseClaims);
        Assert.Equal(GooglePlayPurchaseClaimResultCode.OwnershipConflict, result.Code);
        Assert.Equal(ownerId, claim.UserId);
        Assert.Equal("server-product", claim.ProductId);
        Assert.DoesNotContain(ownerId.ToString(), result.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task DifferentTokensCanBelongToSameUser()
    {
        await using var db = CreateDb();
        var userId = await AddUserAsync(db);
        var service = CreateService(db);

        await service.ClaimAsync(userId, "fake-token-one", "server-product-one", TestContext.Current.CancellationToken);
        await service.ClaimAsync(userId, "fake-token-two", "server-product-two", TestContext.Current.CancellationToken);

        Assert.Equal(2, await db.GooglePlayPurchaseClaims.CountAsync(TestContext.Current.CancellationToken));
    }

    [Theory]
    [InlineData(true, "fake-token", "server-product")]
    [InlineData(false, "   ", "server-product")]
    [InlineData(false, "fake-token", "   ")]
    public async Task InvalidInputDoesNotWrite(bool emptyUserId, string token, string productId)
    {
        await using var db = CreateDb();
        var userId = emptyUserId ? Guid.Empty : await AddUserAsync(db);

        var result = await CreateService(db).ClaimAsync(userId, token, productId, TestContext.Current.CancellationToken);

        Assert.Equal(GooglePlayPurchaseClaimResultCode.InvalidInput, result.Code);
        Assert.Empty(db.GooglePlayPurchaseClaims);
    }

    [Fact]
    public async Task DatabaseFailureMapsToTemporaryUnavailable()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString("N")).Options;
        await using var db = new AppDbContext(options);
        await db.DisposeAsync();

        var result = await CreateService(db).ClaimAsync(Guid.NewGuid(), "fake-token", "server-product", TestContext.Current.CancellationToken);

        Assert.Equal(GooglePlayPurchaseClaimResultCode.TemporarilyUnavailable, result.Code);
    }

    private static AppDbContext CreateDb() => new(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString("N")).Options);
    private static GooglePlayPurchaseClaimService CreateService(AppDbContext db, TestClock? clock = null) => new(db, new GooglePlayPurchaseTokenFingerprintService(), clock ?? new TestClock(DateTimeOffset.UtcNow));
    private static async Task<Guid> AddUserAsync(AppDbContext db)
    {
        var userId = Guid.NewGuid();
        db.Users.Add(new UserEntity { Id = userId, Email = $"{userId:N}@example.test", PasswordHash = "hash", Status = "active", CreatedAt = DateTimeOffset.UtcNow });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return userId;
    }

    private sealed class TestClock(DateTimeOffset utcNow) : IUtcClock { public DateTimeOffset UtcNow { get; set; } = utcNow; }
}
