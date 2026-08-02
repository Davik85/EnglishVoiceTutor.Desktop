using System.Text;
using System.Text.Json;
using EnglishVoiceTutor.Api.Data;
using EnglishVoiceTutor.Api.Options;
using EnglishVoiceTutor.Api.Services;
using EnglishVoiceTutor.Api.Services.Billing;
using Microsoft.EntityFrameworkCore;

namespace EnglishVoiceTutor.Api.Tests.Services;

public sealed class GooglePlayRtdnPushReceiptServiceTests
{
    private const string Subscription = "projects/example/subscriptions/rtdn";
    private const string PackageName = "com.example.app";
    private const string PurchaseToken = "raw-purchase-token-must-not-persist";
    private const string PendingRefundToken = "pending-refund-token-must-not-persist";

    [Fact]
    public void DisabledConfigurationRemainsDisabledAndEnabledIncompleteConfigurationFails()
    {
        Assert.False(new GooglePlayRtdnOptions().Enabled);
        Assert.Throws<InvalidOperationException>(() => new GooglePlayRtdnOptions { Enabled = true }.ValidateForEnabledMode());
    }

    [Theory]
    [InlineData("subscriptionNotification", "purchaseToken", "subscription_notification")]
    [InlineData("oneTimeProductNotification", "purchaseToken", "one_time_product_notification")]
    [InlineData("voidedPurchaseNotification", "purchaseToken", "voided_purchase_notification")]
    public async Task PurchaseTokenNotificationStoresOnlyFingerprint(string kind, string tokenField, string expectedKind)
    {
        await using var db = CreateDb();
        var service = CreateService(db);
        var response = await service.ReceiveAsync(["Bearer valid-jwt"], Envelope(Notification(kind, tokenField, PurchaseToken)), TestContext.Current.CancellationToken);

        Assert.Equal(GooglePlayRtdnPushReceiptStatus.NoContent, response.Status);
        var stored = Assert.Single(db.GooglePlayRtdnEvents);
        Assert.Equal(expectedKind, stored.NotificationKind);
        Assert.Equal(new GooglePlayPurchaseTokenFingerprintService().CreateFingerprint(PurchaseToken), stored.PurchaseTokenFingerprint);
        AssertSafe(stored, response, PurchaseToken);
    }

    [Fact]
    public async Task TestNotificationStoresNullFingerprint()
    {
        await using var db = CreateDb();
        var service = CreateService(db);
        var response = await service.ReceiveAsync(["Bearer valid-jwt"], Envelope(Notification("testNotification", null, null)), TestContext.Current.CancellationToken);

        Assert.Equal(GooglePlayRtdnPushReceiptStatus.NoContent, response.Status);
        var stored = Assert.Single(db.GooglePlayRtdnEvents);
        Assert.Equal("test_notification", stored.NotificationKind);
        Assert.Null(stored.PurchaseTokenFingerprint);
    }

    [Fact]
    public async Task PendingRefundWithTokenRemainsRetryableWithoutPersistence()
    {
        await using var db = CreateDb();
        var service = CreateService(db);

        var response = await service.ReceiveAsync(["Bearer valid-jwt"], Envelope(Notification("pendingRefundReviewNotification", "pendingRefundToken", PendingRefundToken)), TestContext.Current.CancellationToken);

        Assert.Equal(GooglePlayRtdnPushReceiptStatus.TemporarilyUnavailable, response.Status);
        Assert.Empty(db.GooglePlayRtdnEvents);
        AssertSafe(db.GooglePlayRtdnEvents.ToArray(), response, PendingRefundToken);
    }

    [Fact]
    public async Task PendingRefundWithoutPendingRefundTokenIsRejected()
    {
        await using var db = CreateDb();
        var service = CreateService(db);

        var result = await service.ReceiveAsync(["Bearer valid-jwt"], Envelope(Notification("pendingRefundReviewNotification", null, null)), TestContext.Current.CancellationToken);

        Assert.Equal(GooglePlayRtdnPushReceiptStatus.BadRequest, result.Status);
        Assert.Empty(db.GooglePlayRtdnEvents);
    }

    [Theory]
    [InlineData("missing")]
    [InlineData("malformed")]
    public async Task MissingOrMalformedBearerHeaderIsRejectedBeforePersistence(string scenario)
    {
        await using var db = CreateDb();
        var tokenValidator = new RecordingTokenValidator(GooglePlayPubSubOidcValidationResult.Valid);
        var service = CreateService(db, tokenValidator);
        IReadOnlyList<string?> headers = scenario == "missing" ? [] : ["Basic value"];

        var result = await service.ReceiveAsync(headers, Envelope(Notification("subscriptionNotification", "purchaseToken", PurchaseToken)), TestContext.Current.CancellationToken);

        Assert.Equal(GooglePlayRtdnPushReceiptStatus.Unauthorized, result.Status);
        Assert.Equal(0, tokenValidator.CallCount);
        Assert.Empty(db.GooglePlayRtdnEvents);
    }

    [Fact]
    public async Task DuplicateAndMultiplePayloadValidationArePreserved()
    {
        await using var db = CreateDb();
        var service = CreateService(db);
        var body = Envelope(Notification("subscriptionNotification", "purchaseToken", PurchaseToken));
        Assert.Equal(GooglePlayRtdnPushReceiptStatus.NoContent, (await service.ReceiveAsync(["Bearer valid-jwt"], body, TestContext.Current.CancellationToken)).Status);
        Assert.Equal(GooglePlayRtdnPushReceiptStatus.NoContent, (await service.ReceiveAsync(["Bearer valid-jwt"], body, TestContext.Current.CancellationToken)).Status);
        Assert.Single(db.GooglePlayRtdnEvents);

        var multiple = Envelope(new { packageName = PackageName, subscriptionNotification = new { purchaseToken = PurchaseToken }, pendingRefundReviewNotification = new { pendingRefundToken = PendingRefundToken } });
        Assert.Equal(GooglePlayRtdnPushReceiptStatus.BadRequest, (await service.ReceiveAsync(["Bearer valid-jwt"], multiple, TestContext.Current.CancellationToken)).Status);
        Assert.Single(db.GooglePlayRtdnEvents);
    }

    private static GooglePlayRtdnPushReceiptService CreateService(AppDbContext db, IGooglePlayPubSubOidcTokenValidator? validator = null) => new(
        Microsoft.Extensions.Options.Options.Create(new GooglePlayRtdnOptions { Enabled = true, ExpectedAudience = "https://example.test/rtdn", ExpectedServiceAccountEmail = "push@example.test", ExpectedPubSubSubscription = Subscription }),
        Microsoft.Extensions.Options.Options.Create(new GooglePlayBillingOptions { PackageName = PackageName }),
        validator ?? new RecordingTokenValidator(GooglePlayPubSubOidcValidationResult.Valid),
        new GooglePlayRtdnEventPersistenceService(db, new TestClock()),
        new GooglePlayPurchaseTokenFingerprintService());

    private static object Notification(string kind, string? tokenField, string? token) => kind switch
    {
        "testNotification" => new { packageName = PackageName, testNotification = new { version = "1.0" } },
        _ when tokenField is null => new Dictionary<string, object?> { ["packageName"] = PackageName, [kind] = new { version = "1.0" } },
        _ => new Dictionary<string, object?> { ["packageName"] = PackageName, [kind] = new Dictionary<string, string?> { [tokenField] = token } }
    };
    private static string Envelope(object notification) => JsonSerializer.Serialize(new { subscription = Subscription, message = new { messageId = "message-1", data = Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(notification))), publishTime = "2026-08-02T12:00:00Z" } });
    private static AppDbContext CreateDb() => new(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString("N")).Options);
    private static void AssertSafe(object stored, object response, string secret)
    {
        if (string.IsNullOrEmpty(secret)) return;
        Assert.DoesNotContain(secret, JsonSerializer.Serialize(stored), StringComparison.Ordinal);
        Assert.DoesNotContain(secret, JsonSerializer.Serialize(response), StringComparison.Ordinal);
    }

    private sealed class RecordingTokenValidator(GooglePlayPubSubOidcValidationResult result) : IGooglePlayPubSubOidcTokenValidator
    {
        public int CallCount { get; private set; }
        public Task<GooglePlayPubSubOidcValidationResult> ValidateAsync(string token, CancellationToken cancellationToken) { CallCount++; return Task.FromResult(result); }
    }
    private sealed class TestClock : IUtcClock { public DateTimeOffset UtcNow => new(2026, 8, 2, 12, 0, 0, TimeSpan.Zero); }
}
