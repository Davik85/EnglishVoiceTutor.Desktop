using EnglishVoiceTutor.Api.Options;
using EnglishVoiceTutor.Api.Services.Billing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EnglishVoiceTutor.Api.Tests.Services;

public sealed class GooglePlayPurchaseVerifierTests
{
    [Fact]
    public async Task DisabledOrIncompleteConfigurationReturnsNotConfiguredWithoutCallingClient()
    {
        foreach (var options in new[]
        {
            Options(enabled: false),
            Options(enabled: true, packageName: ""),
            Options(enabled: true, allowedProductIds: [])
        })
        {
            var client = new RecordingClient();
            var result = await CreateVerifier(client, options).VerifyAsync(Guid.NewGuid(), "fake-token", TestContext.Current.CancellationToken);
            Assert.Equal(GooglePlayPurchaseVerificationResultCode.NotConfigured, result.Code);
            Assert.Empty(client.Calls);
        }
    }

    [Fact]
    public async Task ExactPackageAndTokenArePassedToClientWithoutTrimming()
    {
        const string token = "  fake-token-with-whitespace  ";
        var client = new RecordingClient(Snapshot("SUBSCRIPTION_STATE_PENDING"));

        await CreateVerifier(client, Options()).VerifyAsync(Guid.NewGuid(), token, TestContext.Current.CancellationToken);

        var call = Assert.Single(client.Calls);
        Assert.Equal("com.example.test", call.PackageName);
        Assert.Equal(token, call.Token);
    }

    [Fact]
    public async Task ActiveAllowedProductWithPendingAcknowledgementReturnsCompleteVerifiedMetadata()
    {
        var result = await CreateVerifier(new RecordingClient(Snapshot("SUBSCRIPTION_STATE_ACTIVE", startTime: Timestamp("2026-07-27T10:00:00+02:00"), lineItems: [LineItem("server-product", Timestamp("2026-08-27T11:00:00+02:00"))])), Options()).VerifyAsync(Guid.NewGuid(), "fake-token", TestContext.Current.CancellationToken);

        Assert.Equal(GooglePlayPurchaseVerificationResultCode.Verified, result.Code);
        Assert.Equal("server-product", result.VerifiedPurchase!.ProductId);
        Assert.Equal(Timestamp("2026-07-27T08:00:00Z"), result.VerifiedPurchase.StartedAtUtc);
        Assert.Equal(Timestamp("2026-08-27T09:00:00Z"), result.VerifiedPurchase.ExpiresAtUtc);
        Assert.Equal(GooglePlayPurchaseAcknowledgementState.Pending, result.VerifiedPurchase.AcknowledgementState);
        Assert.False(result.VerifiedPurchase.IsTestPurchase);
        Assert.Equal(TimeSpan.Zero, result.VerifiedPurchase.StartedAtUtc.Offset);
        Assert.Equal(TimeSpan.Zero, result.VerifiedPurchase.ExpiresAtUtc.Offset);
    }

    [Fact]
    public async Task ActiveAllowedProductWithAcknowledgedTestPurchasePreservesMetadata()
    {
        var result = await CreateVerifier(new RecordingClient(Snapshot("SUBSCRIPTION_STATE_ACTIVE", Timestamp("2026-07-27T10:00:00Z"), [LineItem("server-product", Timestamp("2026-08-27T10:00:00Z"))], GooglePlayPurchaseAcknowledgementState.Acknowledged, isTestPurchase: true)), Options()).VerifyAsync(Guid.NewGuid(), "fake-token", TestContext.Current.CancellationToken);

        Assert.Equal(GooglePlayPurchaseVerificationResultCode.Verified, result.Code);
        Assert.Equal(GooglePlayPurchaseAcknowledgementState.Acknowledged, result.VerifiedPurchase!.AcknowledgementState);
        Assert.True(result.VerifiedPurchase.IsTestPurchase);
    }

    [Fact]
    public async Task PendingReturnsNoVerifiedMetadata()
    {
        var result = await CreateVerifier(new RecordingClient(Snapshot("SUBSCRIPTION_STATE_PENDING")), Options()).VerifyAsync(Guid.NewGuid(), "fake-token", TestContext.Current.CancellationToken);

        Assert.Equal(GooglePlayPurchaseVerificationResultCode.Pending, result.Code);
        Assert.Null(result.VerifiedPurchase);
    }

    [Theory]
    [InlineData("SUBSCRIPTION_STATE_PAUSED")]
    [InlineData("SUBSCRIPTION_STATE_IN_GRACE_PERIOD")]
    [InlineData("SUBSCRIPTION_STATE_ON_HOLD")]
    [InlineData("SUBSCRIPTION_STATE_CANCELED")]
    [InlineData("SUBSCRIPTION_STATE_EXPIRED")]
    [InlineData("")]
    public async Task UnsupportedLifecycleStatesFailClosed(string state)
    {
        var result = await CreateVerifier(new RecordingClient(Snapshot(state)), Options()).VerifyAsync(Guid.NewGuid(), "fake-token", TestContext.Current.CancellationToken);

        Assert.Equal(GooglePlayPurchaseVerificationResultCode.InvalidPurchase, result.Code);
        Assert.Null(result.VerifiedPurchase);
    }

    [Fact]
    public async Task MissingUnsupportedOrMultipleProductsFailClosed()
    {
        foreach (var lineItems in new[] { Array.Empty<GooglePlaySubscriptionLineItemSnapshot>(), new[] { LineItem("   ", Timestamp("2026-08-27T10:00:00Z")) }, new[] { LineItem("unsupported-product", Timestamp("2026-08-27T10:00:00Z")) }, new[] { LineItem("server-product", Timestamp("2026-08-27T10:00:00Z")), LineItem("other-product", Timestamp("2026-08-27T10:00:00Z")) } })
        {
            var result = await CreateVerifier(new RecordingClient(Snapshot("SUBSCRIPTION_STATE_ACTIVE", Timestamp("2026-07-27T10:00:00Z"), lineItems)), Options()).VerifyAsync(Guid.NewGuid(), "fake-token", TestContext.Current.CancellationToken);
            Assert.Equal(lineItems.Length == 0 || string.IsNullOrWhiteSpace(lineItems[0].ProductId) ? GooglePlayPurchaseVerificationResultCode.TemporarilyUnavailable : GooglePlayPurchaseVerificationResultCode.UnsupportedProduct, result.Code);
        }
    }

    [Fact]
    public async Task DuplicateIdenticalProductAndExpiryIsOneAllowedProduct()
    {
        var result = await CreateVerifier(new RecordingClient(Snapshot("SUBSCRIPTION_STATE_ACTIVE", Timestamp("2026-07-27T10:00:00Z"), [LineItem("server-product", Timestamp("2026-08-27T10:00:00Z")), LineItem("server-product", Timestamp("2026-08-27T10:00:00Z"))])), Options()).VerifyAsync(Guid.NewGuid(), "fake-token", TestContext.Current.CancellationToken);
        Assert.Equal(GooglePlayPurchaseVerificationResultCode.Verified, result.Code);
    }

    [Theory]
    [InlineData(true, false, false)]
    [InlineData(false, true, false)]
    [InlineData(false, false, true)]
    public async Task MissingOrMalformedActivePeriodMetadataFailsClosed(bool missingStart, bool missingExpiry, bool ambiguousExpiry)
    {
        DateTimeOffset? start = missingStart ? null : Timestamp("2026-07-27T10:00:00Z");
        DateTimeOffset? expiry = missingExpiry ? null : Timestamp("2026-08-27T10:00:00Z");
        var lineItems = ambiguousExpiry
            ? new[] { LineItem("server-product", expiry), LineItem("server-product", Timestamp("2026-09-27T10:00:00Z")) }
            : new[] { LineItem("server-product", expiry) };

        var result = await CreateVerifier(new RecordingClient(Snapshot("SUBSCRIPTION_STATE_ACTIVE", start, lineItems)), Options()).VerifyAsync(Guid.NewGuid(), "fake-token", TestContext.Current.CancellationToken);

        Assert.Equal(GooglePlayPurchaseVerificationResultCode.TemporarilyUnavailable, result.Code);
        Assert.Null(result.VerifiedPurchase);
    }

    [Theory]
    [InlineData("2026-07-27T10:00:00Z")]
    [InlineData("2026-07-27T09:59:59Z")]
    public async Task ExpiryEqualToOrEarlierThanStartFailsClosed(string expiryTimestamp)
    {
        var result = await CreateVerifier(new RecordingClient(Snapshot(
            "SUBSCRIPTION_STATE_ACTIVE",
            Timestamp("2026-07-27T10:00:00Z"),
            [LineItem("server-product", Timestamp(expiryTimestamp))])), Options()).VerifyAsync(Guid.NewGuid(), "fake-token", TestContext.Current.CancellationToken);

        Assert.Equal(GooglePlayPurchaseVerificationResultCode.TemporarilyUnavailable, result.Code);
        Assert.Null(result.VerifiedPurchase);
    }

    [Fact]
    public async Task UnknownOrUnspecifiedAcknowledgementFailsClosed()
    {
        var result = await CreateVerifier(new RecordingClient(Snapshot("SUBSCRIPTION_STATE_ACTIVE", Timestamp("2026-07-27T10:00:00Z"), [LineItem("server-product", Timestamp("2026-08-27T10:00:00Z"))], acknowledgementState: null)), Options()).VerifyAsync(Guid.NewGuid(), "fake-token", TestContext.Current.CancellationToken);

        Assert.Equal(GooglePlayPurchaseVerificationResultCode.TemporarilyUnavailable, result.Code);
        Assert.Null(result.VerifiedPurchase);
    }

    [Fact]
    public async Task UndefinedAcknowledgementStateFailsClosed()
    {
        var result = await CreateVerifier(new RecordingClient(Snapshot(
            "SUBSCRIPTION_STATE_ACTIVE",
            Timestamp("2026-07-27T10:00:00Z"),
            [LineItem("server-product", Timestamp("2026-08-27T10:00:00Z"))],
            (GooglePlayPurchaseAcknowledgementState)999)), Options()).VerifyAsync(Guid.NewGuid(), "fake-token", TestContext.Current.CancellationToken);

        Assert.Equal(GooglePlayPurchaseVerificationResultCode.TemporarilyUnavailable, result.Code);
        Assert.Null(result.VerifiedPurchase);
    }

    [Fact]
    public async Task LinkedPurchaseTokenPresenceFailsClosedWithoutExposure()
    {
        var result = await CreateVerifier(new RecordingClient(Snapshot("SUBSCRIPTION_STATE_ACTIVE", Timestamp("2026-07-27T10:00:00Z"), [LineItem("server-product", Timestamp("2026-08-27T10:00:00Z"))], hasLinkedPurchaseToken: true)), Options()).VerifyAsync(Guid.NewGuid(), "fake-token", TestContext.Current.CancellationToken);
        Assert.Equal(GooglePlayPurchaseVerificationResultCode.TemporarilyUnavailable, result.Code);
        Assert.DoesNotContain("linked", result.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void SanitizedContractsExcludeTokensPayloadOrderAndCredentialData()
    {
        Assert.Equal(
            ["AcknowledgementState", "HasLinkedPurchaseToken", "IsTestPurchase", "LineItems", "StartTimeUtc", "SubscriptionState"],
            typeof(GooglePlaySubscriptionV2Snapshot).GetProperties().Select(property => property.Name).OrderBy(name => name).ToArray());
        Assert.Equal(
            ["ExpiryTimeUtc", "ProductId"],
            typeof(GooglePlaySubscriptionLineItemSnapshot).GetProperties().Select(property => property.Name).OrderBy(name => name).ToArray());
        Assert.Equal(
            ["AcknowledgementState", "ExpiresAtUtc", "IsTestPurchase", "ProductId", "StartedAtUtc"],
            typeof(GooglePlayVerifiedPurchase).GetProperties().Select(property => property.Name).OrderBy(name => name).ToArray());
    }

    [Theory]
    [InlineData(GooglePlaySubscriptionsV2ClientFailure.InvalidPurchase, GooglePlayPurchaseVerificationResultCode.InvalidPurchase)]
    [InlineData(GooglePlaySubscriptionsV2ClientFailure.TemporarilyUnavailable, GooglePlayPurchaseVerificationResultCode.TemporarilyUnavailable)]
    public async Task SanitizedProviderFailuresMapSafely(GooglePlaySubscriptionsV2ClientFailure failure, GooglePlayPurchaseVerificationResultCode expected)
    {
        var result = await CreateVerifier(new FailingClient(failure), Options()).VerifyAsync(Guid.NewGuid(), "fake-token", TestContext.Current.CancellationToken);
        Assert.Equal(expected, result.Code);
    }

    [Fact]
    public async Task CancellationPropagatesAndNoTokenAppearsInLogs()
    {
        const string token = "fake-token-not-for-logs";
        var logger = new RecordingLogger<GooglePlayPurchaseVerifier>();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => CreateVerifier(new CancelingClient(), Options(), logger).VerifyAsync(Guid.NewGuid(), token, TestContext.Current.CancellationToken));
        Assert.DoesNotContain(logger.Messages, message => message.Contains(token, StringComparison.Ordinal));
    }

    [Fact]
    public void ProductionRegistrationRemainsDisabledAndNoCredentialsAreConfigured()
    {
        var program = ReadRepositoryFile("backend/EnglishVoiceTutor.Api/Program.cs");
        var registration = ReadRepositoryFile("backend/EnglishVoiceTutor.Api/Services/Billing/GooglePlayBillingServiceCollectionExtensions.cs");
        var settings = ReadRepositoryFile("backend/EnglishVoiceTutor.Api/appsettings.json");
        Assert.Contains("AddGooglePlayBilling(builder.Configuration)", program, StringComparison.Ordinal);
        Assert.Contains("AddScoped<IGooglePlayPurchaseVerifier, DisabledGooglePlayPurchaseVerifier>()", registration, StringComparison.Ordinal);
        Assert.Contains("AddScoped<IGooglePlayPurchaseVerifier, GooglePlayPurchaseVerifier>()", registration, StringComparison.Ordinal);
        Assert.DoesNotContain("GoogleCredential.FromFile", registration, StringComparison.Ordinal);
        Assert.DoesNotContain("GoogleCredential.FromStream", registration, StringComparison.Ordinal);
        Assert.DoesNotContain("AndroidPublisherService", program, StringComparison.Ordinal);
        foreach (var forbidden in new[] { "private_key", "client_email", "serviceAccount", "credential" }) Assert.DoesNotContain(forbidden, settings, StringComparison.OrdinalIgnoreCase);
    }

    private static GooglePlayPurchaseVerifier CreateVerifier(IGooglePlaySubscriptionsV2Client client, GooglePlayBillingOptions options, RecordingLogger<GooglePlayPurchaseVerifier>? logger = null) => new(client, Microsoft.Extensions.Options.Options.Create(options), logger ?? new RecordingLogger<GooglePlayPurchaseVerifier>());
    private static GooglePlayBillingOptions Options(bool enabled = true, string packageName = "com.example.test", List<string>? allowedProductIds = null) => new() { Enabled = enabled, PackageName = packageName, AllowedProductIds = allowedProductIds ?? ["server-product"] };
    private static GooglePlaySubscriptionV2Snapshot Snapshot(string? state, DateTimeOffset? startTime = null, IReadOnlyList<GooglePlaySubscriptionLineItemSnapshot>? lineItems = null, GooglePlayPurchaseAcknowledgementState? acknowledgementState = GooglePlayPurchaseAcknowledgementState.Pending, bool isTestPurchase = false, bool hasLinkedPurchaseToken = false) => new(state, startTime, lineItems ?? [], acknowledgementState, isTestPurchase, hasLinkedPurchaseToken);
    private static GooglePlaySubscriptionLineItemSnapshot LineItem(string? productId, DateTimeOffset? expiryTime) => new(productId, expiryTime);
    private static DateTimeOffset Timestamp(string value) => DateTimeOffset.Parse(value, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.AssumeUniversal).ToUniversalTime();

    private sealed class RecordingClient(GooglePlaySubscriptionV2Snapshot? snapshot = null) : IGooglePlaySubscriptionsV2Client
    {
        public List<(string PackageName, string Token)> Calls { get; } = [];
        public Task<GooglePlaySubscriptionV2Snapshot?> GetAsync(string packageName, string purchaseToken, CancellationToken cancellationToken) { Calls.Add((packageName, purchaseToken)); return Task.FromResult(snapshot); }
    }
    private sealed class FailingClient(GooglePlaySubscriptionsV2ClientFailure failure) : IGooglePlaySubscriptionsV2Client { public Task<GooglePlaySubscriptionV2Snapshot?> GetAsync(string packageName, string purchaseToken, CancellationToken cancellationToken) => throw new GooglePlaySubscriptionsV2ClientException(failure); }
    private sealed class CancelingClient : IGooglePlaySubscriptionsV2Client { public Task<GooglePlaySubscriptionV2Snapshot?> GetAsync(string packageName, string purchaseToken, CancellationToken cancellationToken) => throw new OperationCanceledException(); }
    private sealed class RecordingLogger<T> : ILogger<T> { public List<string> Messages { get; } = []; public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null; public bool IsEnabled(LogLevel logLevel) => true; public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) => Messages.Add(formatter(state, exception)); }
    private static string ReadRepositoryFile(string relativePath) { var directory = new DirectoryInfo(AppContext.BaseDirectory); while (directory is not null && !File.Exists(Path.Combine(directory.FullName, relativePath))) directory = directory.Parent; Assert.NotNull(directory); return File.ReadAllText(Path.Combine(directory!.FullName, relativePath)); }
}
