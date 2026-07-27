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
    public async Task DisabledVerifierReturnsNotConfiguredWithoutCallingClaimServiceOrLoggingToken()
    {
        const string token = "fake-purchase-token-do-not-log";
        var claims = new RecordingClaimService();
        var logger = new RecordingLogger<GooglePlayPurchaseVerificationService>();
        var service = CreateService(new DisabledGooglePlayPurchaseVerifier(), claims, logger);

        var result = await service.VerifyAsync(Guid.NewGuid(), new GooglePlayPurchaseVerificationRequest { PurchaseToken = token }, TestContext.Current.CancellationToken);

        Assert.Equal(StatusCodes.Status503ServiceUnavailable, result.StatusCode);
        Assert.Equal("not_configured", result.Response.Result);
        Assert.Empty(claims.Calls);
        Assert.DoesNotContain(token, JsonSerializer.Serialize(result.Response), StringComparison.Ordinal);
        Assert.DoesNotContain(logger.Messages, message => message.Contains(token, StringComparison.Ordinal));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task MissingOrBlankTokenReturnsBadRequestWithoutCallingVerifierOrClaimService(string? token)
    {
        var verifier = new RecordingVerifier(Pending());
        var claims = new RecordingClaimService();

        var result = await CreateService(verifier, claims).VerifyAsync(Guid.NewGuid(), token is null ? null : new GooglePlayPurchaseVerificationRequest { PurchaseToken = token }, TestContext.Current.CancellationToken);

        Assert.Equal(StatusCodes.Status400BadRequest, result.StatusCode);
        Assert.Empty(verifier.Calls);
        Assert.Empty(claims.Calls);
    }

    [Fact]
    public async Task OversizedTokenReturnsBadRequestWithoutCallingVerifierOrClaimService()
    {
        var verifier = new RecordingVerifier(Pending());
        var claims = new RecordingClaimService();
        var result = await CreateService(verifier, claims).VerifyAsync(Guid.NewGuid(), new GooglePlayPurchaseVerificationRequest { PurchaseToken = new string('x', SubscriptionConstants.Billing.GooglePlayPurchaseTokenMaximumLength + 1) }, TestContext.Current.CancellationToken);

        Assert.Equal(StatusCodes.Status400BadRequest, result.StatusCode);
        Assert.Empty(verifier.Calls);
        Assert.Empty(claims.Calls);
    }

    [Fact]
    public async Task VerifiedPurchasePassesExactUserTokenAndServerProductToClaimService()
    {
        const string token = "  fake-token-with-significant-whitespace  ";
        var userId = Guid.NewGuid();
        var verifier = new RecordingVerifier(Verified("server-verified-product"));
        var claims = new RecordingClaimService(GooglePlayPurchaseClaimResultCode.Claimed);

        var result = await CreateService(verifier, claims).VerifyAsync(userId, new GooglePlayPurchaseVerificationRequest { PurchaseToken = token }, TestContext.Current.CancellationToken);

        var verifierCall = Assert.Single(verifier.Calls);
        var claimCall = Assert.Single(claims.Calls);
        Assert.Equal(userId, verifierCall.UserId);
        Assert.Equal(token, verifierCall.Token);
        Assert.Equal(userId, claimCall.UserId);
        Assert.Equal(token, claimCall.Token);
        Assert.Equal("server-verified-product", claimCall.ProductId);
        Assert.Equal("verified", result.Response.Result);
        var publicResponse = JsonSerializer.Serialize(result.Response);
        foreach (var privateMetadata in new[] { "StartedAtUtc", "ExpiresAtUtc", "AcknowledgementState", "IsTestPurchase" }) Assert.DoesNotContain(privateMetadata, publicResponse, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(GooglePlayPurchaseClaimResultCode.Claimed, 200, "verified", true)]
    [InlineData(GooglePlayPurchaseClaimResultCode.AlreadyOwned, 200, "already_processed", true)]
    [InlineData(GooglePlayPurchaseClaimResultCode.OwnershipConflict, 200, "ownership_conflict", false)]
    [InlineData(GooglePlayPurchaseClaimResultCode.TemporarilyUnavailable, 503, "temporarily_unavailable", false)]
    [InlineData(GooglePlayPurchaseClaimResultCode.InvalidInput, 503, "temporarily_unavailable", false)]
    public async Task VerifiedPurchaseMapsClaimResultSafely(GooglePlayPurchaseClaimResultCode claimCode, int statusCode, string publicResult, bool refresh)
    {
        var claims = new RecordingClaimService(claimCode);
        var result = await CreateService(new RecordingVerifier(Verified("server-product")), claims).VerifyAsync(Guid.NewGuid(), new GooglePlayPurchaseVerificationRequest { PurchaseToken = "fake-token" }, TestContext.Current.CancellationToken);

        Assert.Equal(statusCode, result.StatusCode);
        Assert.Equal(publicResult, result.Response.Result);
        Assert.Equal(refresh, result.Response.SubscriptionStatusRefreshRecommended);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task MissingOrBlankVerifiedProductFailsClosedWithoutClaim(string? productId)
    {
        var claims = new RecordingClaimService();
        var result = await CreateService(new RecordingVerifier(new GooglePlayPurchaseVerificationResult(GooglePlayPurchaseVerificationResultCode.Verified, productId is null ? null : VerifiedPurchase(productId))), claims).VerifyAsync(Guid.NewGuid(), new GooglePlayPurchaseVerificationRequest { PurchaseToken = "fake-token" }, TestContext.Current.CancellationToken);

        Assert.Equal(StatusCodes.Status503ServiceUnavailable, result.StatusCode);
        Assert.Equal("temporarily_unavailable", result.Response.Result);
        Assert.Empty(claims.Calls);
    }

    [Theory]
    [InlineData(GooglePlayPurchaseVerificationResultCode.Pending, "pending")]
    [InlineData(GooglePlayPurchaseVerificationResultCode.InvalidPurchase, "invalid_purchase")]
    [InlineData(GooglePlayPurchaseVerificationResultCode.UnsupportedProduct, "unsupported_product")]
    [InlineData(GooglePlayPurchaseVerificationResultCode.NotConfigured, "not_configured")]
    public async Task NonVerifiedProviderResultsDoNotCallClaimService(GooglePlayPurchaseVerificationResultCode code, string expectedResult)
    {
        var claims = new RecordingClaimService();
        var result = await CreateService(new RecordingVerifier(new GooglePlayPurchaseVerificationResult(code)), claims).VerifyAsync(Guid.NewGuid(), new GooglePlayPurchaseVerificationRequest { PurchaseToken = "fake-token" }, TestContext.Current.CancellationToken);

        Assert.Equal(expectedResult, result.Response.Result);
        Assert.Empty(claims.Calls);
    }

    [Fact]
    public async Task ProviderExceptionMapsToTemporarilyUnavailableWithoutCallingClaimService()
    {
        var claims = new RecordingClaimService();
        var result = await CreateService(new ThrowingVerifier(), claims).VerifyAsync(Guid.NewGuid(), new GooglePlayPurchaseVerificationRequest { PurchaseToken = "fake-token" }, TestContext.Current.CancellationToken);

        Assert.Equal(StatusCodes.Status503ServiceUnavailable, result.StatusCode);
        Assert.Equal("temporarily_unavailable", result.Response.Result);
        Assert.Empty(claims.Calls);
    }

    [Fact]
    public async Task ProviderAndClaimCancellationPropagate()
    {
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => CreateService(new CancelingVerifier(), new RecordingClaimService()).VerifyAsync(Guid.NewGuid(), new GooglePlayPurchaseVerificationRequest { PurchaseToken = "fake-token" }, TestContext.Current.CancellationToken));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => CreateService(new RecordingVerifier(Verified("server-product")), new CancelingClaimService()).VerifyAsync(Guid.NewGuid(), new GooglePlayPurchaseVerificationRequest { PurchaseToken = "fake-token" }, TestContext.Current.CancellationToken));
    }

    private static GooglePlayPurchaseVerificationResult Pending() => new(GooglePlayPurchaseVerificationResultCode.Pending);
    private static GooglePlayPurchaseVerificationResult Verified(string productId) => new(GooglePlayPurchaseVerificationResultCode.Verified, VerifiedPurchase(productId));
    private static GooglePlayVerifiedPurchase VerifiedPurchase(string productId) => new(productId, new DateTimeOffset(2026, 7, 27, 10, 0, 0, TimeSpan.Zero), new DateTimeOffset(2026, 8, 27, 10, 0, 0, TimeSpan.Zero), GooglePlayPurchaseAcknowledgementState.Pending, false);
    private static GooglePlayPurchaseVerificationService CreateService(IGooglePlayPurchaseVerifier verifier, IGooglePlayPurchaseClaimService claims, RecordingLogger<GooglePlayPurchaseVerificationService>? logger = null) => new(verifier, claims, logger ?? new RecordingLogger<GooglePlayPurchaseVerificationService>());

    private sealed class RecordingVerifier(GooglePlayPurchaseVerificationResult result) : IGooglePlayPurchaseVerifier
    {
        public List<(Guid UserId, string Token)> Calls { get; } = [];
        public Task<GooglePlayPurchaseVerificationResult> VerifyAsync(Guid userId, string purchaseToken, CancellationToken cancellationToken) { Calls.Add((userId, purchaseToken)); return Task.FromResult(result); }
    }

    private sealed class ThrowingVerifier : IGooglePlayPurchaseVerifier { public Task<GooglePlayPurchaseVerificationResult> VerifyAsync(Guid userId, string purchaseToken, CancellationToken cancellationToken) => throw new InvalidOperationException(); }
    private sealed class CancelingVerifier : IGooglePlayPurchaseVerifier { public Task<GooglePlayPurchaseVerificationResult> VerifyAsync(Guid userId, string purchaseToken, CancellationToken cancellationToken) => throw new OperationCanceledException(); }

    private sealed class RecordingClaimService(GooglePlayPurchaseClaimResultCode code = GooglePlayPurchaseClaimResultCode.Claimed) : IGooglePlayPurchaseClaimService
    {
        public List<(Guid UserId, string Token, string ProductId)> Calls { get; } = [];
        public Task<GooglePlayPurchaseClaimResult> ClaimAsync(Guid userId, string purchaseToken, string productId, CancellationToken cancellationToken) { Calls.Add((userId, purchaseToken, productId)); return Task.FromResult(new GooglePlayPurchaseClaimResult(code)); }
    }

    private sealed class CancelingClaimService : IGooglePlayPurchaseClaimService { public Task<GooglePlayPurchaseClaimResult> ClaimAsync(Guid userId, string purchaseToken, string productId, CancellationToken cancellationToken) => throw new OperationCanceledException(); }
    private sealed class RecordingLogger<T> : ILogger<T> { public List<string> Messages { get; } = []; public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null; public bool IsEnabled(LogLevel logLevel) => true; public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) => Messages.Add(formatter(state, exception)); }
}
