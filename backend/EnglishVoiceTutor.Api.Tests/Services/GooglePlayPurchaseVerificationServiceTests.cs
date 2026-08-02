using System.Text.Json;
using EnglishVoiceTutor.Api.Constants;
using EnglishVoiceTutor.Api.Contracts.Billing;
using EnglishVoiceTutor.Api.Services.Billing;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace EnglishVoiceTutor.Api.Tests.Services;

public sealed class GooglePlayPurchaseVerificationServiceTests
{
    [Fact]
    public async Task DisabledVerifierReturnsNotConfiguredWithoutCallingPersistenceOrLoggingToken()
    {
        const string token = "fake-purchase-token-do-not-log";
        var persistence = new RecordingPersistence();
        var logger = new RecordingLogger<GooglePlayPurchaseVerificationService>();

        var result = await CreateService(new DisabledGooglePlayPurchaseVerifier(), persistence, logger: logger).VerifyAsync(Guid.NewGuid(), Request(token), TestContext.Current.CancellationToken);

        Assert.Equal(StatusCodes.Status503ServiceUnavailable, result.StatusCode);
        Assert.Equal("not_configured", result.Response.Result);
        Assert.Empty(persistence.Calls);
        AssertSafe(token, result, logger);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task MissingOrBlankTokenReturnsBadRequestWithoutCallingVerifierOrPersistence(string? token)
    {
        var verifier = new RecordingVerifier(Pending());
        var persistence = new RecordingPersistence();

        var result = await CreateService(verifier, persistence).VerifyAsync(Guid.NewGuid(), token is null ? null : Request(token), TestContext.Current.CancellationToken);

        Assert.Equal(StatusCodes.Status400BadRequest, result.StatusCode);
        Assert.Empty(verifier.Calls);
        Assert.Empty(persistence.Calls);
    }

    [Fact]
    public async Task OversizedTokenReturnsBadRequestWithoutCallingVerifierOrPersistence()
    {
        var verifier = new RecordingVerifier(Pending());
        var persistence = new RecordingPersistence();

        var result = await CreateService(verifier, persistence).VerifyAsync(Guid.NewGuid(), Request(new string('x', SubscriptionConstants.Billing.GooglePlayPurchaseTokenMaximumLength + 1)), TestContext.Current.CancellationToken);

        Assert.Equal(StatusCodes.Status400BadRequest, result.StatusCode);
        Assert.Empty(verifier.Calls);
        Assert.Empty(persistence.Calls);
    }

    [Fact]
    public async Task VerifiedPurchasePersistsExactAuthenticatedUserTokenAndTrustedMetadata()
    {
        const string token = "  fake-token-with-significant-whitespace  ";
        var userId = Guid.NewGuid();
        var trustedPurchase = VerifiedPurchase("server-verified-product", GooglePlayPurchaseAcknowledgementState.Acknowledged);
        var verifier = new RecordingVerifier(new GooglePlayPurchaseVerificationResult(GooglePlayPurchaseVerificationResultCode.Verified, trustedPurchase));
        var persistence = new RecordingPersistence(GooglePlayVerifiedPurchasePersistenceResultCode.Applied);

        var result = await CreateService(verifier, persistence).VerifyAsync(userId, Request(token), TestContext.Current.CancellationToken);

        var call = Assert.Single(persistence.Calls);
        Assert.Equal(userId, call.UserId);
        Assert.Equal(token, call.PurchaseToken);
        Assert.Same(trustedPurchase, call.VerifiedPurchase);
        Assert.Equal("verified", result.Response.Result);
    }

    [Fact]
    public async Task PendingAcknowledgementOccursOnlyAfterDurablePersistence()
    {
        var trustedPurchase = VerifiedPurchase("server-product", GooglePlayPurchaseAcknowledgementState.Pending);
        var persistence = new RecordingPersistence();
        var client = new RecordingSubscriptionsClient();

        var result = await CreateService(new RecordingVerifier(new GooglePlayPurchaseVerificationResult(GooglePlayPurchaseVerificationResultCode.Verified, trustedPurchase)), persistence, client).VerifyAsync(Guid.NewGuid(), Request("fake-token"), TestContext.Current.CancellationToken);

        Assert.Same(trustedPurchase, Assert.Single(persistence.Calls).VerifiedPurchase);
        Assert.Equal((trustedPurchase.PackageName, trustedPurchase.ProductId, "fake-token"), Assert.Single(client.AcknowledgementCalls));
        Assert.Equal("verified", result.Response.Result);
    }

    [Fact]
    public async Task AlreadyAcknowledgedPurchaseDoesNotCallAcknowledgement()
    {
        var client = new RecordingSubscriptionsClient();

        var result = await CreateService(new RecordingVerifier(Verified("server-product", GooglePlayPurchaseAcknowledgementState.Acknowledged)), new RecordingPersistence(), client).VerifyAsync(Guid.NewGuid(), Request("fake-token"), TestContext.Current.CancellationToken);

        Assert.Empty(client.AcknowledgementCalls);
        Assert.Equal("verified", result.Response.Result);
    }

    [Fact]
    public async Task AcknowledgementFailureIsRetryableAfterDurablePersistence()
    {
        var persistence = new RecordingPersistence();
        var client = new RecordingSubscriptionsClient(new GooglePlaySubscriptionsV2ClientException(GooglePlaySubscriptionsV2ClientFailure.TemporarilyUnavailable));

        var result = await CreateService(new RecordingVerifier(Verified("server-product")), persistence, client).VerifyAsync(Guid.NewGuid(), Request("fake-token"), TestContext.Current.CancellationToken);

        Assert.Single(persistence.Calls);
        Assert.Single(client.AcknowledgementCalls);
        Assert.Equal(StatusCodes.Status503ServiceUnavailable, result.StatusCode);
        Assert.Equal("acknowledgement_pending", result.Response.Result);
        Assert.True(result.Response.SubscriptionStatusRefreshRecommended);
    }

    [Theory]
    [InlineData(GooglePlayVerifiedPurchasePersistenceResultCode.Applied, 200, "verified", true)]
    [InlineData(GooglePlayVerifiedPurchasePersistenceResultCode.AlreadyCurrent, 200, "verified", true)]
    [InlineData(GooglePlayVerifiedPurchasePersistenceResultCode.OwnershipConflict, 200, "ownership_conflict", false)]
    [InlineData(GooglePlayVerifiedPurchasePersistenceResultCode.ProductMismatch, 200, "invalid_purchase", false)]
    [InlineData(GooglePlayVerifiedPurchasePersistenceResultCode.ConsistencyConflict, 200, "invalid_purchase", false)]
    [InlineData(GooglePlayVerifiedPurchasePersistenceResultCode.TestPurchaseNotSupported, 200, "unsupported_product", false)]
    [InlineData(GooglePlayVerifiedPurchasePersistenceResultCode.InvalidInput, 503, "temporarily_unavailable", false)]
    [InlineData(GooglePlayVerifiedPurchasePersistenceResultCode.TemporarilyUnavailable, 503, "temporarily_unavailable", false)]
    public async Task PersistenceResultsMapSafely(GooglePlayVerifiedPurchasePersistenceResultCode code, int statusCode, string publicResult, bool refresh)
    {
        var result = await CreateService(new RecordingVerifier(Verified("server-product")), new RecordingPersistence(code)).VerifyAsync(Guid.NewGuid(), Request("fake-token"), TestContext.Current.CancellationToken);

        Assert.Equal(statusCode, result.StatusCode);
        Assert.Equal(publicResult, result.Response.Result);
        Assert.Equal(refresh, result.Response.SubscriptionStatusRefreshRecommended);
    }

    [Fact]
    public async Task UnexpectedPersistenceResultFailsClosed()
    {
        var result = await CreateService(new RecordingVerifier(Verified("server-product")), new RecordingPersistence((GooglePlayVerifiedPurchasePersistenceResultCode)999)).VerifyAsync(Guid.NewGuid(), Request("fake-token"), TestContext.Current.CancellationToken);

        Assert.Equal(StatusCodes.Status503ServiceUnavailable, result.StatusCode);
        Assert.Equal("temporarily_unavailable", result.Response.Result);
    }

    [Fact]
    public async Task MissingTrustedPurchaseFailsClosedWithoutPersistence()
    {
        var persistence = new RecordingPersistence();
        var result = await CreateService(new RecordingVerifier(new GooglePlayPurchaseVerificationResult(GooglePlayPurchaseVerificationResultCode.Verified)), persistence).VerifyAsync(Guid.NewGuid(), Request("fake-token"), TestContext.Current.CancellationToken);

        Assert.Equal(StatusCodes.Status503ServiceUnavailable, result.StatusCode);
        Assert.Equal("temporarily_unavailable", result.Response.Result);
        Assert.Empty(persistence.Calls);
    }

    [Theory]
    [InlineData(GooglePlayPurchaseVerificationResultCode.Pending, "pending")]
    [InlineData(GooglePlayPurchaseVerificationResultCode.InvalidPurchase, "invalid_purchase")]
    [InlineData(GooglePlayPurchaseVerificationResultCode.UnsupportedProduct, "unsupported_product")]
    [InlineData(GooglePlayPurchaseVerificationResultCode.NotConfigured, "not_configured")]
    [InlineData(GooglePlayPurchaseVerificationResultCode.TemporarilyUnavailable, "temporarily_unavailable")]
    public async Task NonVerifiedProviderResultsDoNotCallPersistence(GooglePlayPurchaseVerificationResultCode code, string expectedResult)
    {
        var persistence = new RecordingPersistence();
        var result = await CreateService(new RecordingVerifier(new GooglePlayPurchaseVerificationResult(code)), persistence).VerifyAsync(Guid.NewGuid(), Request("fake-token"), TestContext.Current.CancellationToken);

        Assert.Equal(expectedResult, result.Response.Result);
        Assert.Empty(persistence.Calls);
    }

    [Fact]
    public async Task ProviderExceptionMapsToTemporarilyUnavailableWithoutPersistence()
    {
        var persistence = new RecordingPersistence();
        var result = await CreateService(new ThrowingVerifier(), persistence).VerifyAsync(Guid.NewGuid(), Request("fake-token"), TestContext.Current.CancellationToken);

        Assert.Equal(StatusCodes.Status503ServiceUnavailable, result.StatusCode);
        Assert.Equal("temporarily_unavailable", result.Response.Result);
        Assert.Empty(persistence.Calls);
    }

    [Fact]
    public async Task PersistenceExceptionMapsToTemporarilyUnavailableWithoutDetails()
    {
        const string token = "fake-token";
        var logger = new RecordingLogger<GooglePlayPurchaseVerificationService>();
        var result = await CreateService(new RecordingVerifier(Verified("server-product")), new ThrowingPersistence(), logger: logger).VerifyAsync(Guid.NewGuid(), Request(token), TestContext.Current.CancellationToken);

        Assert.Equal(StatusCodes.Status503ServiceUnavailable, result.StatusCode);
        Assert.Equal("temporarily_unavailable", result.Response.Result);
        AssertSafe(token, result, logger);
    }

    [Fact]
    public async Task ProviderAndPersistenceCancellationPropagate()
    {
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => CreateService(new CancelingVerifier(), new RecordingPersistence()).VerifyAsync(Guid.NewGuid(), Request("fake-token"), TestContext.Current.CancellationToken));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => CreateService(new RecordingVerifier(Verified("server-product")), new CancelingPersistence()).VerifyAsync(Guid.NewGuid(), Request("fake-token"), TestContext.Current.CancellationToken));
    }

    private static GooglePlayPurchaseVerificationRequest Request(string token) => new() { PurchaseToken = token };
    private static GooglePlayPurchaseVerificationResult Pending() => new(GooglePlayPurchaseVerificationResultCode.Pending);
    private static GooglePlayPurchaseVerificationResult Verified(string productId, GooglePlayPurchaseAcknowledgementState acknowledgementState = GooglePlayPurchaseAcknowledgementState.Pending) => new(GooglePlayPurchaseVerificationResultCode.Verified, VerifiedPurchase(productId, acknowledgementState));
    private static GooglePlayVerifiedPurchase VerifiedPurchase(string productId, GooglePlayPurchaseAcknowledgementState acknowledgementState = GooglePlayPurchaseAcknowledgementState.Pending) => new("com.example.test", productId, new DateTimeOffset(2026, 7, 27, 10, 0, 0, TimeSpan.Zero), new DateTimeOffset(2026, 8, 27, 10, 0, 0, TimeSpan.Zero), acknowledgementState, false);
    private static GooglePlayPurchaseVerificationService CreateService(IGooglePlayPurchaseVerifier verifier, IGooglePlayVerifiedPurchasePersistenceService persistence, RecordingSubscriptionsClient? subscriptionsClient = null, RecordingLogger<GooglePlayPurchaseVerificationService>? logger = null) => new(new GooglePlayPurchaseProcessor(verifier, persistence, new Protector(), subscriptionsClient ?? new RecordingSubscriptionsClient(), Microsoft.Extensions.Logging.Abstractions.NullLogger<GooglePlayPurchaseProcessor>.Instance), logger ?? new RecordingLogger<GooglePlayPurchaseVerificationService>());
    private static void AssertSafe(string token, GooglePlayPurchaseVerificationServiceResult result, RecordingLogger<GooglePlayPurchaseVerificationService> logger)
    {
        var publicValues = JsonSerializer.Serialize(result.Response);
        Assert.DoesNotContain(token, publicValues, StringComparison.Ordinal);
        Assert.DoesNotContain("fingerprint", publicValues, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(logger.Messages, message => message.Contains(token, StringComparison.Ordinal) || message.Contains("fingerprint", StringComparison.OrdinalIgnoreCase));
    }

    private sealed class Protector : IGooglePlayPurchaseTokenProtectionService { public string Protect(string purchaseToken) => "protected-token"; public GooglePlayPurchaseTokenUnprotectResult TryUnprotect(string protectedPurchaseToken) => GooglePlayPurchaseTokenUnprotectResult.Failure; }

    private sealed class RecordingVerifier(GooglePlayPurchaseVerificationResult result) : IGooglePlayPurchaseVerifier
    {
        public List<(Guid UserId, string Token)> Calls { get; } = [];
        public Task<GooglePlayPurchaseVerificationResult> VerifyAsync(Guid userId, string purchaseToken, CancellationToken cancellationToken) { Calls.Add((userId, purchaseToken)); return Task.FromResult(result); }
    }

    private sealed class ThrowingVerifier : IGooglePlayPurchaseVerifier { public Task<GooglePlayPurchaseVerificationResult> VerifyAsync(Guid userId, string purchaseToken, CancellationToken cancellationToken) => throw new InvalidOperationException("private verifier failure"); }
    private sealed class CancelingVerifier : IGooglePlayPurchaseVerifier { public Task<GooglePlayPurchaseVerificationResult> VerifyAsync(Guid userId, string purchaseToken, CancellationToken cancellationToken) => throw new OperationCanceledException(); }
    private sealed class RecordingPersistence(GooglePlayVerifiedPurchasePersistenceResultCode code = GooglePlayVerifiedPurchasePersistenceResultCode.Applied) : IGooglePlayVerifiedPurchasePersistenceService { public List<GooglePlayVerifiedPurchasePersistenceRequest> Calls { get; } = []; public Task<GooglePlayVerifiedPurchasePersistenceResult> PersistAsync(GooglePlayVerifiedPurchasePersistenceRequest request, CancellationToken cancellationToken) { Calls.Add(request); return Task.FromResult(new GooglePlayVerifiedPurchasePersistenceResult(code)); } public Task UpdateAcknowledgementStateAsync(string purchaseToken, bool acknowledgementPending, string? safeResultCode, CancellationToken cancellationToken) => Task.CompletedTask; }
    private sealed class ThrowingPersistence : IGooglePlayVerifiedPurchasePersistenceService { public Task<GooglePlayVerifiedPurchasePersistenceResult> PersistAsync(GooglePlayVerifiedPurchasePersistenceRequest request, CancellationToken cancellationToken) => throw new InvalidOperationException("private persistence failure"); public Task UpdateAcknowledgementStateAsync(string purchaseToken, bool acknowledgementPending, string? safeResultCode, CancellationToken cancellationToken) => Task.CompletedTask; }
    private sealed class CancelingPersistence : IGooglePlayVerifiedPurchasePersistenceService { public Task<GooglePlayVerifiedPurchasePersistenceResult> PersistAsync(GooglePlayVerifiedPurchasePersistenceRequest request, CancellationToken cancellationToken) => throw new OperationCanceledException(); public Task UpdateAcknowledgementStateAsync(string purchaseToken, bool acknowledgementPending, string? safeResultCode, CancellationToken cancellationToken) => Task.CompletedTask; }
    private sealed class RecordingSubscriptionsClient(Exception? acknowledgementException = null) : IGooglePlaySubscriptionsV2Client { public List<(string PackageName, string ProductId, string Token)> AcknowledgementCalls { get; } = []; public Task<GooglePlaySubscriptionV2Snapshot?> GetAsync(string packageName, string purchaseToken, CancellationToken cancellationToken) => throw new NotSupportedException(); public Task AcknowledgeAsync(string packageName, string productId, string purchaseToken, CancellationToken cancellationToken) { AcknowledgementCalls.Add((packageName, productId, purchaseToken)); if (acknowledgementException is not null) throw acknowledgementException; return Task.CompletedTask; } }
    private sealed class RecordingLogger<T> : ILogger<T> { public List<string> Messages { get; } = []; public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null; public bool IsEnabled(LogLevel logLevel) => true; public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) => Messages.Add(formatter(state, exception)); }
}
