using EnglishVoiceTutor.Api.Data;
using EnglishVoiceTutor.Api.Data.Entities;
using EnglishVoiceTutor.Api.Services;
using EnglishVoiceTutor.Api.Services.Billing;
using Microsoft.EntityFrameworkCore;

namespace EnglishVoiceTutor.Api.Tests.Services;

public sealed class GooglePlayRtdnPersistenceTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 2, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task OneSecretRowPerClaimAndExistingClaimsWithoutSecretsRemainValid()
    {
        await using var db = CreateDb();
        var claim = await AddClaimAsync(db, Fingerprint("one"));
        var service = new GooglePlayPurchaseTokenSecretPersistenceService(db, new TestClock(Now));

        Assert.Equal(GooglePlayPurchaseTokenSecretPersistenceResultCode.Stored, (await service.CreateOrUpdateAsync(new(claim.Id, claim.PurchaseTokenFingerprint, "protected-one", "v1", true), TestContext.Current.CancellationToken)).Code);
        Assert.Equal(GooglePlayPurchaseTokenSecretPersistenceResultCode.Stored, (await service.CreateOrUpdateAsync(new(claim.Id, claim.PurchaseTokenFingerprint, "protected-two", "v1", false), TestContext.Current.CancellationToken)).Code);

        var secret = Assert.Single(db.GooglePlayPurchaseTokenSecrets);
        Assert.Equal("protected-two", secret.ProtectedPurchaseToken);
        Assert.False(secret.AcknowledgementPending);
        Assert.Single(db.GooglePlayPurchaseClaims);
        Assert.NotNull(await db.GooglePlayPurchaseClaims.SingleAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public void SecretFingerprintIsUniquelyIndexedAndRawTokensHaveNoPersistenceField()
    {
        using var db = CreateDb();
        var entity = db.Model.FindEntityType(typeof(GooglePlayPurchaseTokenSecretEntity))!;
        Assert.Contains(entity.GetIndexes(), index => index.IsUnique && index.Properties.Single().Name == nameof(GooglePlayPurchaseTokenSecretEntity.PurchaseTokenFingerprint));
        Assert.DoesNotContain(typeof(GooglePlayPurchaseTokenSecretEntity).GetProperties(), property => property.Name.Contains("Raw", StringComparison.OrdinalIgnoreCase) || property.Name == "PurchaseToken");
    }

    [Fact]
    public async Task DuplicateReceiptIsIdempotentButSameMessageOnAnotherSubscriptionIsAllowed()
    {
        await using var db = CreateDb();
        var service = new GooglePlayRtdnEventPersistenceService(db, new TestClock(Now));
        var first = await service.RecordReceiptAsync(Receipt(), TestContext.Current.CancellationToken);
        var duplicate = await service.RecordReceiptAsync(Receipt(), TestContext.Current.CancellationToken);
        var otherSubscription = await service.RecordReceiptAsync(Receipt(subscription: "projects/example/subscriptions/other"), TestContext.Current.CancellationToken);

        Assert.Equal(GooglePlayRtdnReceiptResultCode.Received, first.Code);
        Assert.Equal(GooglePlayRtdnReceiptResultCode.Duplicate, duplicate.Code);
        Assert.Equal(first.EventId, duplicate.EventId);
        Assert.Equal(GooglePlayRtdnReceiptResultCode.Received, otherSubscription.Code);
        Assert.Equal(2, await db.GooglePlayRtdnEvents.CountAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task RetryAndPermanentFailureStoreOnlyBoundedSafeStateAndBatchExcludesProcessedRecords()
    {
        await using var db = CreateDb();
        var service = new GooglePlayRtdnEventPersistenceService(db, new TestClock(Now));
        var retry = (await service.RecordReceiptAsync(Receipt(messageId: "retry"), TestContext.Current.CancellationToken)).EventId!.Value;
        var processed = (await service.RecordReceiptAsync(Receipt(messageId: "processed"), TestContext.Current.CancellationToken)).EventId!.Value;
        var permanent = (await service.RecordReceiptAsync(Receipt(messageId: "permanent"), TestContext.Current.CancellationToken)).EventId!.Value;

        Assert.True(await service.RecordRetryableFailureAsync(retry, Now.AddMinutes(5), GooglePlayRtdnSafeErrorCodes.ProviderUnavailable, TestContext.Current.CancellationToken));
        Assert.True(await service.MarkProcessedAsync(processed, TestContext.Current.CancellationToken));
        Assert.True(await service.RecordPermanentFailureAsync(permanent, GooglePlayRtdnSafeErrorCodes.ProviderRejected, TestContext.Current.CancellationToken));
        Assert.False(await service.RecordPermanentFailureAsync(processed, "raw provider payload", TestContext.Current.CancellationToken));

        var retryEvent = await db.GooglePlayRtdnEvents.SingleAsync(item => item.Id == retry, TestContext.Current.CancellationToken);
        Assert.Equal(1, retryEvent.AttemptCount);
        Assert.Equal(Now.AddMinutes(5), retryEvent.NextAttemptAtUtc);
        Assert.Equal(GooglePlayRtdnSafeErrorCodes.ProviderUnavailable, retryEvent.SafeErrorCode);
        var permanentEvent = await db.GooglePlayRtdnEvents.SingleAsync(item => item.Id == permanent, TestContext.Current.CancellationToken);
        Assert.Equal(GooglePlayRtdnEventStatuses.PermanentFailure, permanentEvent.Status);
        Assert.Equal(GooglePlayRtdnSafeErrorCodes.ProviderRejected, permanentEvent.SafeErrorCode);
        var batch = await service.GetRetryBatchAsync(Now.AddMinutes(5), 10, TestContext.Current.CancellationToken);
        Assert.Single(batch);
        Assert.Equal(retry, batch[0].Id);
        Assert.DoesNotContain(batch, item => item.Status == GooglePlayRtdnEventStatuses.Processed);
        Assert.DoesNotContain(typeof(GooglePlayRtdnEventEntity).GetProperties(), property => property.Name.Contains("Payload", StringComparison.OrdinalIgnoreCase) || property.Name.Contains("Authorization", StringComparison.OrdinalIgnoreCase) || property.Name.Contains("Jwt", StringComparison.OrdinalIgnoreCase));
    }

    private static GooglePlayRtdnReceipt Receipt(string messageId = "message-1", string subscription = "projects/example/subscriptions/primary") => new("google_play", messageId, subscription, "com.example.app", "subscription_notification", Fingerprint("token"), Now.AddMinutes(-1));
    private static string Fingerprint(string value) => Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
    private static AppDbContext CreateDb() => new(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString("N")).Options);
    private static async Task<GooglePlayPurchaseClaimEntity> AddClaimAsync(AppDbContext db, string fingerprint) { var userId = Guid.NewGuid(); db.Users.Add(new UserEntity { Id = userId, Email = $"{userId:N}@example.test", PasswordHash = "hash", Status = "active", CreatedAt = Now }); var claim = new GooglePlayPurchaseClaimEntity { Id = Guid.NewGuid(), UserId = userId, PurchaseTokenFingerprint = fingerprint, ProductId = "product", CreatedAtUtc = Now, LastSeenAtUtc = Now }; db.GooglePlayPurchaseClaims.Add(claim); await db.SaveChangesAsync(TestContext.Current.CancellationToken); return claim; }
    private sealed class TestClock(DateTimeOffset value) : IUtcClock { public DateTimeOffset UtcNow { get; } = value; }
}
