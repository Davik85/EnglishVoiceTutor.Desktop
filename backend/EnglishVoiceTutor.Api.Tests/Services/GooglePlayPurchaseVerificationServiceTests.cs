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
    public async Task DisabledVerifierReturnsSafeNotConfiguredWithoutLoggingToken()
    {
        const string token = "fake-purchase-token-do-not-log";
        var logger = new RecordingLogger<GooglePlayPurchaseVerificationService>();
        var service = new GooglePlayPurchaseVerificationService(new DisabledGooglePlayPurchaseVerifier(), logger);

        var result = await service.VerifyAsync(Guid.NewGuid(), new GooglePlayPurchaseVerificationRequest { PurchaseToken = token }, TestContext.Current.CancellationToken);

        Assert.Equal(StatusCodes.Status503ServiceUnavailable, result.StatusCode);
        Assert.Equal("not_configured", result.Response.Result);
        Assert.Equal("Google Play purchase verification is not available yet.", result.Response.Message);
        Assert.False(result.Response.SubscriptionStatusRefreshRecommended);
        Assert.DoesNotContain(token, JsonSerializer.Serialize(result.Response), StringComparison.Ordinal);
        Assert.DoesNotContain(logger.Messages, message => message.Contains(token, StringComparison.Ordinal));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task MissingOrBlankTokenReturnsBadRequestWithoutCallingVerifier(string? token)
    {
        var verifier = new RecordingVerifier(GooglePlayPurchaseVerificationResultCode.Verified);
        var service = CreateService(verifier);

        var result = await service.VerifyAsync(Guid.NewGuid(), token is null ? null : new GooglePlayPurchaseVerificationRequest { PurchaseToken = token }, TestContext.Current.CancellationToken);

        Assert.Equal(StatusCodes.Status400BadRequest, result.StatusCode);
        Assert.Empty(verifier.Calls);
    }

    [Fact]
    public async Task OversizedTokenReturnsBadRequestWithoutCallingVerifier()
    {
        var verifier = new RecordingVerifier(GooglePlayPurchaseVerificationResultCode.Verified);
        var result = await CreateService(verifier).VerifyAsync(Guid.NewGuid(), new GooglePlayPurchaseVerificationRequest { PurchaseToken = new string('x', SubscriptionConstants.Billing.GooglePlayPurchaseTokenMaximumLength + 1) }, TestContext.Current.CancellationToken);

        Assert.Equal(StatusCodes.Status400BadRequest, result.StatusCode);
        Assert.Empty(verifier.Calls);
    }

    [Fact]
    public async Task ExactNonblankTokenAndAuthenticatedUserArePassedToVerifierWithoutMutation()
    {
        const string token = "  fake-token-with-significant-whitespace  ";
        var userId = Guid.NewGuid();
        var verifier = new RecordingVerifier(GooglePlayPurchaseVerificationResultCode.Verified);

        await CreateService(verifier).VerifyAsync(userId, new GooglePlayPurchaseVerificationRequest { PurchaseToken = token }, TestContext.Current.CancellationToken);

        var call = Assert.Single(verifier.Calls);
        Assert.Equal(userId, call.UserId);
        Assert.Equal(token, call.Token);
    }

    [Theory]
    [InlineData(GooglePlayPurchaseVerificationResultCode.Verified, "verified", true)]
    [InlineData(GooglePlayPurchaseVerificationResultCode.Pending, "pending", true)]
    [InlineData(GooglePlayPurchaseVerificationResultCode.AlreadyProcessed, "already_processed", true)]
    [InlineData(GooglePlayPurchaseVerificationResultCode.InvalidPurchase, "invalid_purchase", false)]
    [InlineData(GooglePlayPurchaseVerificationResultCode.UnsupportedProduct, "unsupported_product", false)]
    [InlineData(GooglePlayPurchaseVerificationResultCode.OwnershipConflict, "ownership_conflict", false)]
    public async Task FutureVerifierResultsMapToSafeResponsesWithoutPersistence(GooglePlayPurchaseVerificationResultCode code, string expectedResult, bool refreshRecommended)
    {
        var result = await CreateService(new RecordingVerifier(code)).VerifyAsync(Guid.NewGuid(), new GooglePlayPurchaseVerificationRequest { PurchaseToken = "fake-token" }, TestContext.Current.CancellationToken);

        Assert.Equal(StatusCodes.Status200OK, result.StatusCode);
        Assert.Equal(expectedResult, result.Response.Result);
        Assert.Equal(refreshRecommended, result.Response.SubscriptionStatusRefreshRecommended);
    }

    [Fact]
    public async Task ProviderExceptionMapsToTemporarilyUnavailableWithoutLoggingToken()
    {
        const string token = "fake-token-not-for-logs";
        var logger = new RecordingLogger<GooglePlayPurchaseVerificationService>();
        var service = new GooglePlayPurchaseVerificationService(new ThrowingVerifier(), logger);

        var result = await service.VerifyAsync(Guid.NewGuid(), new GooglePlayPurchaseVerificationRequest { PurchaseToken = token }, TestContext.Current.CancellationToken);

        Assert.Equal(StatusCodes.Status503ServiceUnavailable, result.StatusCode);
        Assert.Equal("temporarily_unavailable", result.Response.Result);
        Assert.DoesNotContain(token, JsonSerializer.Serialize(result.Response), StringComparison.Ordinal);
        Assert.DoesNotContain(logger.Messages, message => message.Contains(token, StringComparison.Ordinal));
    }

    [Fact]
    public async Task CancellationIsPropagated()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => CreateService(new RecordingVerifier(GooglePlayPurchaseVerificationResultCode.Verified)).VerifyAsync(Guid.NewGuid(), new GooglePlayPurchaseVerificationRequest { PurchaseToken = "fake-token" }, cancellation.Token));
    }

    private static GooglePlayPurchaseVerificationService CreateService(IGooglePlayPurchaseVerifier verifier) => new(verifier, new RecordingLogger<GooglePlayPurchaseVerificationService>());

    private sealed class RecordingVerifier(GooglePlayPurchaseVerificationResultCode code) : IGooglePlayPurchaseVerifier
    {
        public List<(Guid UserId, string Token)> Calls { get; } = [];
        public Task<GooglePlayPurchaseVerificationResult> VerifyAsync(Guid userId, string purchaseToken, CancellationToken cancellationToken)
        {
            Calls.Add((userId, purchaseToken));
            return Task.FromResult(new GooglePlayPurchaseVerificationResult(code));
        }
    }

    private sealed class ThrowingVerifier : IGooglePlayPurchaseVerifier
    {
        public Task<GooglePlayPurchaseVerificationResult> VerifyAsync(Guid userId, string purchaseToken, CancellationToken cancellationToken) => throw new InvalidOperationException("provider failure");
    }

    private sealed class RecordingLogger<T> : ILogger<T>
    {
        public List<string> Messages { get; } = [];
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) => Messages.Add(formatter(state, exception));
    }
}
