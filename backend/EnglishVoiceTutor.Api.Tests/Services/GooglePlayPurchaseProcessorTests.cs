using EnglishVoiceTutor.Api.Services.Billing;
using Microsoft.Extensions.Logging.Abstractions;

namespace EnglishVoiceTutor.Api.Tests.Services;

public sealed class GooglePlayPurchaseProcessorTests
{
    [Fact]
    public async Task PendingAcknowledgementRunsVerifyThenPersistThenAcknowledge()
    {
        var sequence = new List<string>();
        var result = await Create(sequence).ProcessAsync(Guid.NewGuid(), "fake-token", TestContext.Current.CancellationToken);

        Assert.Equal(GooglePlayPurchaseProcessingResultCode.Verified, result.Code);
        Assert.Equal(["verify", "persist", "acknowledge"], sequence);
    }

    [Theory]
    [InlineData(GooglePlayVerifiedPurchasePersistenceResultCode.OwnershipConflict)]
    [InlineData(GooglePlayVerifiedPurchasePersistenceResultCode.ProductMismatch)]
    [InlineData(GooglePlayVerifiedPurchasePersistenceResultCode.ConsistencyConflict)]
    [InlineData(GooglePlayVerifiedPurchasePersistenceResultCode.TemporarilyUnavailable)]
    public async Task FailedOrConflictingPersistenceNeverAcknowledges(GooglePlayVerifiedPurchasePersistenceResultCode persistenceCode)
    {
        var sequence = new List<string>();
        await Create(sequence, persistenceCode: persistenceCode).ProcessAsync(Guid.NewGuid(), "fake-token", TestContext.Current.CancellationToken);

        Assert.DoesNotContain("acknowledge", sequence);
    }

    [Theory]
    [InlineData(GooglePlayPurchaseVerificationResultCode.Pending)]
    [InlineData(GooglePlayPurchaseVerificationResultCode.UnsupportedProduct)]
    [InlineData(GooglePlayPurchaseVerificationResultCode.InvalidPurchase)]
    [InlineData(GooglePlayPurchaseVerificationResultCode.TemporarilyUnavailable)]
    public async Task NonVerifiedPurchaseNeverPersistsOrAcknowledges(GooglePlayPurchaseVerificationResultCode verificationCode)
    {
        var sequence = new List<string>();
        await Create(sequence, verificationCode: verificationCode).ProcessAsync(Guid.NewGuid(), "fake-token", TestContext.Current.CancellationToken);

        Assert.Equal(["verify"], sequence);
    }

    [Fact]
    public async Task AlreadyAcknowledgedPurchaseDoesNotAcknowledge()
    {
        var sequence = new List<string>();
        await Create(sequence, acknowledged: true).ProcessAsync(Guid.NewGuid(), "fake-token", TestContext.Current.CancellationToken);

        Assert.Equal(["verify", "persist"], sequence);
    }

    [Theory]
    [InlineData(GooglePlaySubscriptionsV2ClientFailure.TemporarilyUnavailable, GooglePlayPurchaseProcessingResultCode.AcknowledgementPending)]
    [InlineData(GooglePlaySubscriptionsV2ClientFailure.InvalidPurchase, GooglePlayPurchaseProcessingResultCode.AcknowledgementInconsistent)]
    public async Task AcknowledgementFailuresRetainDistinctInternalCategories(GooglePlaySubscriptionsV2ClientFailure failure, GooglePlayPurchaseProcessingResultCode expected)
    {
        var sequence = new List<string>();
        var result = await Create(sequence, acknowledgementFailure: failure).ProcessAsync(Guid.NewGuid(), "fake-token", TestContext.Current.CancellationToken);

        Assert.Equal(expected, result.Code);
        Assert.Equal(["verify", "persist", "acknowledge"], sequence);
    }

    [Fact]
    public async Task TemporaryAcknowledgementFailureCanRetryWithoutDuplicatePersistenceCreation()
    {
        var sequence = new List<string>();
        var persistence = new IdempotentPersistence(sequence);
        var client = new RetryingClient(sequence);
        var processor = new GooglePlayPurchaseProcessor(new Verifier(sequence, GooglePlayPurchaseVerificationResultCode.Verified, false), persistence, client, NullLogger<GooglePlayPurchaseProcessor>.Instance);
        var userId = Guid.NewGuid();

        var first = await processor.ProcessAsync(userId, "fake-token", TestContext.Current.CancellationToken);
        var second = await processor.ProcessAsync(userId, "fake-token", TestContext.Current.CancellationToken);

        Assert.Equal(GooglePlayPurchaseProcessingResultCode.AcknowledgementPending, first.Code);
        Assert.Equal(GooglePlayPurchaseProcessingResultCode.Verified, second.Code);
        Assert.Equal(1, persistence.CreatedCount);
        Assert.Equal(2, client.Calls);
    }

    private static GooglePlayPurchaseProcessor Create(List<string> sequence, GooglePlayPurchaseVerificationResultCode verificationCode = GooglePlayPurchaseVerificationResultCode.Verified, GooglePlayVerifiedPurchasePersistenceResultCode persistenceCode = GooglePlayVerifiedPurchasePersistenceResultCode.Applied, bool acknowledged = false, GooglePlaySubscriptionsV2ClientFailure? acknowledgementFailure = null) => new(new Verifier(sequence, verificationCode, acknowledged), new Persistence(sequence, persistenceCode), new Client(sequence, acknowledgementFailure), NullLogger<GooglePlayPurchaseProcessor>.Instance);
    private static GooglePlayVerifiedPurchase Purchase(bool acknowledged = false) => new("com.example.test", "server-product", DateTimeOffset.Parse("2026-07-27T10:00:00Z"), DateTimeOffset.Parse("2026-08-27T10:00:00Z"), acknowledged ? GooglePlayPurchaseAcknowledgementState.Acknowledged : GooglePlayPurchaseAcknowledgementState.Pending, false);
    private sealed class Verifier(List<string> sequence, GooglePlayPurchaseVerificationResultCode code, bool acknowledged) : IGooglePlayPurchaseVerifier { public Task<GooglePlayPurchaseVerificationResult> VerifyAsync(Guid userId, string purchaseToken, CancellationToken cancellationToken) { sequence.Add("verify"); return Task.FromResult(new GooglePlayPurchaseVerificationResult(code, code == GooglePlayPurchaseVerificationResultCode.Verified ? Purchase(acknowledged) : null)); } }
    private sealed class Persistence(List<string> sequence, GooglePlayVerifiedPurchasePersistenceResultCode code) : IGooglePlayVerifiedPurchasePersistenceService { public Task<GooglePlayVerifiedPurchasePersistenceResult> PersistAsync(GooglePlayVerifiedPurchasePersistenceRequest request, CancellationToken cancellationToken) { sequence.Add("persist"); return Task.FromResult(new GooglePlayVerifiedPurchasePersistenceResult(code)); } }
    private sealed class Client(List<string> sequence, GooglePlaySubscriptionsV2ClientFailure? failure) : IGooglePlaySubscriptionsV2Client { public Task<GooglePlaySubscriptionV2Snapshot?> GetAsync(string packageName, string purchaseToken, CancellationToken cancellationToken) => throw new NotSupportedException(); public Task AcknowledgeAsync(string packageName, string productId, string purchaseToken, CancellationToken cancellationToken) { sequence.Add("acknowledge"); if (failure.HasValue) throw new GooglePlaySubscriptionsV2ClientException(failure.Value); return Task.CompletedTask; } }
    private sealed class IdempotentPersistence(List<string> sequence) : IGooglePlayVerifiedPurchasePersistenceService { public int CreatedCount { get; private set; } public Task<GooglePlayVerifiedPurchasePersistenceResult> PersistAsync(GooglePlayVerifiedPurchasePersistenceRequest request, CancellationToken cancellationToken) { sequence.Add("persist"); if (CreatedCount == 0) { CreatedCount++; return Task.FromResult(new GooglePlayVerifiedPurchasePersistenceResult(GooglePlayVerifiedPurchasePersistenceResultCode.Applied)); } return Task.FromResult(new GooglePlayVerifiedPurchasePersistenceResult(GooglePlayVerifiedPurchasePersistenceResultCode.AlreadyCurrent)); } }
    private sealed class RetryingClient(List<string> sequence) : IGooglePlaySubscriptionsV2Client { public int Calls { get; private set; } public Task<GooglePlaySubscriptionV2Snapshot?> GetAsync(string packageName, string purchaseToken, CancellationToken cancellationToken) => throw new NotSupportedException(); public Task AcknowledgeAsync(string packageName, string productId, string purchaseToken, CancellationToken cancellationToken) { sequence.Add("acknowledge"); if (++Calls == 1) throw new GooglePlaySubscriptionsV2ClientException(GooglePlaySubscriptionsV2ClientFailure.TemporarilyUnavailable); return Task.CompletedTask; } }
}
