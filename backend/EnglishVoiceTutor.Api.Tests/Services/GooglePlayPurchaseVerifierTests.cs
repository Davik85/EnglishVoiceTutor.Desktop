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
    public async Task ActiveSingleAllowedProductReturnsVerifiedProviderMetadata()
    {
        var result = await CreateVerifier(new RecordingClient(Snapshot("SUBSCRIPTION_STATE_ACTIVE", ["server-product"])), Options()).VerifyAsync(Guid.NewGuid(), "fake-token", TestContext.Current.CancellationToken);

        Assert.Equal(GooglePlayPurchaseVerificationResultCode.Verified, result.Code);
        Assert.Equal("server-product", result.VerifiedPurchase!.ProductId);
    }

    [Fact]
    public async Task PendingReturnsNoVerifiedMetadata()
    {
        var result = await CreateVerifier(new RecordingClient(Snapshot("SUBSCRIPTION_STATE_PENDING", ["server-product"])), Options()).VerifyAsync(Guid.NewGuid(), "fake-token", TestContext.Current.CancellationToken);

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
        var result = await CreateVerifier(new RecordingClient(Snapshot(state, ["server-product"])), Options()).VerifyAsync(Guid.NewGuid(), "fake-token", TestContext.Current.CancellationToken);

        Assert.Equal(GooglePlayPurchaseVerificationResultCode.InvalidPurchase, result.Code);
        Assert.Null(result.VerifiedPurchase);
    }

    [Fact]
    public async Task MissingUnsupportedOrMultipleProductsFailClosed()
    {
        foreach (var productIds in new[] { Array.Empty<string>(), new[] { "   " }, new[] { "unsupported-product" }, new[] { "server-product", "other-product" } })
        {
            var result = await CreateVerifier(new RecordingClient(Snapshot("SUBSCRIPTION_STATE_ACTIVE", productIds)), Options()).VerifyAsync(Guid.NewGuid(), "fake-token", TestContext.Current.CancellationToken);
            Assert.Equal(productIds.Length == 0 || productIds.All(string.IsNullOrWhiteSpace) ? GooglePlayPurchaseVerificationResultCode.InvalidPurchase : GooglePlayPurchaseVerificationResultCode.UnsupportedProduct, result.Code);
        }
    }

    [Fact]
    public async Task DuplicateIdenticalProductIsOneAllowedProduct()
    {
        var result = await CreateVerifier(new RecordingClient(Snapshot("SUBSCRIPTION_STATE_ACTIVE", ["server-product", "server-product"])), Options()).VerifyAsync(Guid.NewGuid(), "fake-token", TestContext.Current.CancellationToken);
        Assert.Equal(GooglePlayPurchaseVerificationResultCode.Verified, result.Code);
    }

    [Fact]
    public async Task LinkedPurchaseTokenPresenceFailsClosedWithoutExposure()
    {
        var result = await CreateVerifier(new RecordingClient(Snapshot("SUBSCRIPTION_STATE_ACTIVE", ["server-product"], hasLinkedPurchaseToken: true)), Options()).VerifyAsync(Guid.NewGuid(), "fake-token", TestContext.Current.CancellationToken);
        Assert.Equal(GooglePlayPurchaseVerificationResultCode.TemporarilyUnavailable, result.Code);
        Assert.DoesNotContain("linked", result.ToString(), StringComparison.OrdinalIgnoreCase);
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
        var settings = ReadRepositoryFile("backend/EnglishVoiceTutor.Api/appsettings.json");
        Assert.Contains("AddScoped<IGooglePlayPurchaseVerifier, DisabledGooglePlayPurchaseVerifier>()", program, StringComparison.Ordinal);
        Assert.DoesNotContain("AddScoped<IGooglePlayPurchaseVerifier, GooglePlayPurchaseVerifier>()", program, StringComparison.Ordinal);
        Assert.DoesNotContain("AddScoped<IGooglePlaySubscriptionsV2Client, GooglePlaySubscriptionsV2Client>()", program, StringComparison.Ordinal);
        Assert.DoesNotContain("AndroidPublisherService", program, StringComparison.Ordinal);
        foreach (var forbidden in new[] { "private_key", "client_email", "serviceAccount", "credential" }) Assert.DoesNotContain(forbidden, settings, StringComparison.OrdinalIgnoreCase);
    }

    private static GooglePlayPurchaseVerifier CreateVerifier(IGooglePlaySubscriptionsV2Client client, GooglePlayBillingOptions options, RecordingLogger<GooglePlayPurchaseVerifier>? logger = null) => new(client, Microsoft.Extensions.Options.Options.Create(options), logger ?? new RecordingLogger<GooglePlayPurchaseVerifier>());
    private static GooglePlayBillingOptions Options(bool enabled = true, string packageName = "com.example.test", List<string>? allowedProductIds = null) => new() { Enabled = enabled, PackageName = packageName, AllowedProductIds = allowedProductIds ?? ["server-product"] };
    private static GooglePlaySubscriptionV2Snapshot Snapshot(string? state, IReadOnlyList<string>? products = null, bool hasLinkedPurchaseToken = false) => new(state, products ?? [], "ACKNOWLEDGEMENT_STATE_PENDING", hasLinkedPurchaseToken);

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
