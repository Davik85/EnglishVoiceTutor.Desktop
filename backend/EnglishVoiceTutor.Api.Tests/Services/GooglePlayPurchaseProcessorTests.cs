using EnglishVoiceTutor.Api.Services.Billing;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EnglishVoiceTutor.Api.Tests.Services;

public sealed class GooglePlayPurchaseProcessorTests
{
    [Fact]
    public void GooglePlayRegistrationRequiresActualProtectionServiceOnlyWhenEnabled()
    {
        var disabled = new ServiceCollection();
        disabled.AddGooglePlayBilling(Configuration(("GooglePlayBilling:Enabled", "false"), ("BackendDataProtection:Enabled", "false")));
        Assert.DoesNotContain(disabled, descriptor => descriptor.ServiceType == typeof(IGooglePlayAndroidPublisherServiceFactory));

        var missingProtectionService = new ServiceCollection();
        Assert.Throws<InvalidOperationException>(() => missingProtectionService.AddGooglePlayBilling(Configuration(("GooglePlayBilling:Enabled", "true"), ("BackendDataProtection:Enabled", "true"))));

        var configured = new ServiceCollection();
        configured.AddSingleton<IGooglePlayPurchaseTokenProtectionService>(new Protector());
        configured.AddGooglePlayBilling(Configuration(("GooglePlayBilling:Enabled", "true"), ("BackendDataProtection:Enabled", "true")));
        Assert.Contains(configured, descriptor => descriptor.ServiceType == typeof(IGooglePlayAndroidPublisherServiceFactory));
    }
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
        var processor = new GooglePlayPurchaseProcessor(new Verifier(sequence, GooglePlayPurchaseVerificationResultCode.Verified, false), persistence, new Protector(), client, NullLogger<GooglePlayPurchaseProcessor>.Instance);
        var userId = Guid.NewGuid();

        var first = await processor.ProcessAsync(userId, "fake-token", TestContext.Current.CancellationToken);
        var second = await processor.ProcessAsync(userId, "fake-token", TestContext.Current.CancellationToken);

        Assert.Equal(GooglePlayPurchaseProcessingResultCode.AcknowledgementPending, first.Code);
        Assert.Equal(GooglePlayPurchaseProcessingResultCode.Verified, second.Code);
        Assert.Equal(1, persistence.CreatedCount);
        Assert.Equal(2, client.Calls);
    }

    [Fact]
    public async Task ProtectionFailureDoesNotPersistOrAcknowledge()
    {
        var sequence = new List<string>();
        var processor = new GooglePlayPurchaseProcessor(new Verifier(sequence, GooglePlayPurchaseVerificationResultCode.Verified, false), new Persistence(sequence, GooglePlayVerifiedPurchasePersistenceResultCode.Applied), new ThrowingProtector(), new Client(sequence, null), NullLogger<GooglePlayPurchaseProcessor>.Instance);

        var result = await processor.ProcessAsync(Guid.NewGuid(), "fake-token", TestContext.Current.CancellationToken);

        Assert.Equal(GooglePlayPurchaseProcessingResultCode.TemporarilyUnavailable, result.Code);
        Assert.Equal(["verify"], sequence);
    }

    [Fact]
    public async Task AcknowledgementOutcomesUpdateOnlySafePendingState()
    {
        var sequence = new List<string>();
        var persistence = new AcknowledgementPersistence(sequence);
        var processor = new GooglePlayPurchaseProcessor(new Verifier(sequence, GooglePlayPurchaseVerificationResultCode.Verified, false), persistence, new Protector(), new Client(sequence, GooglePlaySubscriptionsV2ClientFailure.TemporarilyUnavailable), NullLogger<GooglePlayPurchaseProcessor>.Instance);

        var result = await processor.ProcessAsync(Guid.NewGuid(), "fake-token", TestContext.Current.CancellationToken);

        Assert.Equal(GooglePlayPurchaseProcessingResultCode.AcknowledgementPending, result.Code);
        Assert.Equal((true, GooglePlayRtdnSafeErrorCodes.ProviderUnavailable), Assert.Single(persistence.AcknowledgementUpdates));
    }

    [Fact]
    public async Task AcknowledgementCancellationIsRethrownWithoutAStateUpdate()
    {
        var sequence = new List<string>();
        var persistence = new AcknowledgementPersistence(sequence);
        var client = new CancelingClient(sequence);
        var processor = new GooglePlayPurchaseProcessor(new Verifier(sequence, GooglePlayPurchaseVerificationResultCode.Verified, false), persistence, new Protector(), client, NullLogger<GooglePlayPurchaseProcessor>.Instance);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => processor.ProcessAsync(Guid.NewGuid(), "fake-token", TestContext.Current.CancellationToken));

        Assert.Equal(["verify", "persist", "acknowledge"], sequence);
        Assert.Empty(persistence.AcknowledgementUpdates);
        Assert.Equal(1, client.Calls);
    }

    [Fact]
    public async Task TrialDeferralRunsOnlyAfterAcknowledgementCompletes()
    {
        var sequence = new List<string>();
        var processor = new GooglePlayPurchaseProcessor(
            new Verifier(sequence, GooglePlayPurchaseVerificationResultCode.Verified, false),
            new Persistence(sequence, GooglePlayVerifiedPurchasePersistenceResultCode.Applied),
            new Protector(),
            new Client(sequence, null),
            NullLogger<GooglePlayPurchaseProcessor>.Instance,
            new Deferral(sequence, GooglePlayTrialDeferralResultCode.Completed));

        var result = await processor.ProcessAsync(Guid.NewGuid(), "fake-token", TestContext.Current.CancellationToken);

        Assert.Equal(GooglePlayPurchaseProcessingResultCode.Verified, result.Code);
        Assert.Equal(["verify", "persist", "acknowledge", "defer"], sequence);
    }

    [Fact]
    public async Task AcknowledgementOutageNeverIssuesTrialDeferral()
    {
        var sequence = new List<string>();
        var processor = new GooglePlayPurchaseProcessor(
            new Verifier(sequence, GooglePlayPurchaseVerificationResultCode.Verified, false),
            new Persistence(sequence, GooglePlayVerifiedPurchasePersistenceResultCode.Applied),
            new Protector(),
            new Client(sequence, GooglePlaySubscriptionsV2ClientFailure.TemporarilyUnavailable),
            NullLogger<GooglePlayPurchaseProcessor>.Instance,
            new Deferral(sequence, GooglePlayTrialDeferralResultCode.Completed));

        var result = await processor.ProcessAsync(Guid.NewGuid(), "fake-token", TestContext.Current.CancellationToken);

        Assert.Equal(GooglePlayPurchaseProcessingResultCode.AcknowledgementPending, result.Code);
        Assert.Equal(["verify", "persist", "acknowledge"], sequence);
    }

    private static GooglePlayPurchaseProcessor Create(List<string> sequence, GooglePlayPurchaseVerificationResultCode verificationCode = GooglePlayPurchaseVerificationResultCode.Verified, GooglePlayVerifiedPurchasePersistenceResultCode persistenceCode = GooglePlayVerifiedPurchasePersistenceResultCode.Applied, bool acknowledged = false, GooglePlaySubscriptionsV2ClientFailure? acknowledgementFailure = null) => new(new Verifier(sequence, verificationCode, acknowledged), new Persistence(sequence, persistenceCode), new Protector(), new Client(sequence, acknowledgementFailure), NullLogger<GooglePlayPurchaseProcessor>.Instance);
    private static IConfiguration Configuration(params (string Key, string Value)[] values) => new ConfigurationBuilder().AddInMemoryCollection(values.ToDictionary(value => value.Key, value => (string?)value.Value)).Build();
    private static GooglePlayVerifiedPurchase Purchase(bool acknowledged = false) => new("com.example.test", "server-product", DateTimeOffset.Parse("2026-07-27T10:00:00Z"), DateTimeOffset.Parse("2026-08-27T10:00:00Z"), acknowledged ? GooglePlayPurchaseAcknowledgementState.Acknowledged : GooglePlayPurchaseAcknowledgementState.Pending, false);
    private sealed class Verifier(List<string> sequence, GooglePlayPurchaseVerificationResultCode code, bool acknowledged) : IGooglePlayPurchaseVerifier { public Task<GooglePlayPurchaseVerificationResult> VerifyAsync(Guid userId, string purchaseToken, CancellationToken cancellationToken) { sequence.Add("verify"); return Task.FromResult(new GooglePlayPurchaseVerificationResult(code, code == GooglePlayPurchaseVerificationResultCode.Verified ? Purchase(acknowledged) : null)); } }
    private sealed class Protector : IGooglePlayPurchaseTokenProtectionService { public string Protect(string purchaseToken) => "protected-token"; public GooglePlayPurchaseTokenUnprotectResult TryUnprotect(string protectedPurchaseToken) => GooglePlayPurchaseTokenUnprotectResult.Failure; }
    private sealed class ThrowingProtector : IGooglePlayPurchaseTokenProtectionService { public string Protect(string purchaseToken) => throw new InvalidOperationException(); public GooglePlayPurchaseTokenUnprotectResult TryUnprotect(string protectedPurchaseToken) => GooglePlayPurchaseTokenUnprotectResult.Failure; }
    private sealed class Persistence(List<string> sequence, GooglePlayVerifiedPurchasePersistenceResultCode code) : IGooglePlayVerifiedPurchasePersistenceService { public Task<GooglePlayVerifiedPurchasePersistenceResult> PersistAsync(GooglePlayVerifiedPurchasePersistenceRequest request, CancellationToken cancellationToken) { sequence.Add("persist"); return Task.FromResult(new GooglePlayVerifiedPurchasePersistenceResult(code)); } public Task UpdateAcknowledgementStateAsync(string purchaseToken, bool acknowledgementPending, string? safeResultCode, CancellationToken cancellationToken) => Task.CompletedTask; }
    private sealed class Client(List<string> sequence, GooglePlaySubscriptionsV2ClientFailure? failure) : IGooglePlaySubscriptionsV2Client { public Task<GooglePlaySubscriptionV2Snapshot?> GetAsync(string packageName, string purchaseToken, CancellationToken cancellationToken) => throw new NotSupportedException(); public Task AcknowledgeAsync(string packageName, string productId, string purchaseToken, CancellationToken cancellationToken) { sequence.Add("acknowledge"); if (failure.HasValue) throw new GooglePlaySubscriptionsV2ClientException(failure.Value); return Task.CompletedTask; } }
    private sealed class IdempotentPersistence(List<string> sequence) : IGooglePlayVerifiedPurchasePersistenceService { public int CreatedCount { get; private set; } public Task<GooglePlayVerifiedPurchasePersistenceResult> PersistAsync(GooglePlayVerifiedPurchasePersistenceRequest request, CancellationToken cancellationToken) { sequence.Add("persist"); if (CreatedCount == 0) { CreatedCount++; return Task.FromResult(new GooglePlayVerifiedPurchasePersistenceResult(GooglePlayVerifiedPurchasePersistenceResultCode.Applied)); } return Task.FromResult(new GooglePlayVerifiedPurchasePersistenceResult(GooglePlayVerifiedPurchasePersistenceResultCode.AlreadyCurrent)); } public Task UpdateAcknowledgementStateAsync(string purchaseToken, bool acknowledgementPending, string? safeResultCode, CancellationToken cancellationToken) => Task.CompletedTask; }
    private sealed class AcknowledgementPersistence(List<string> sequence) : IGooglePlayVerifiedPurchasePersistenceService { public List<(bool Pending, string? Code)> AcknowledgementUpdates { get; } = []; public Task<GooglePlayVerifiedPurchasePersistenceResult> PersistAsync(GooglePlayVerifiedPurchasePersistenceRequest request, CancellationToken cancellationToken) { sequence.Add("persist"); return Task.FromResult(new GooglePlayVerifiedPurchasePersistenceResult(GooglePlayVerifiedPurchasePersistenceResultCode.Applied)); } public Task UpdateAcknowledgementStateAsync(string purchaseToken, bool acknowledgementPending, string? safeResultCode, CancellationToken cancellationToken) { AcknowledgementUpdates.Add((acknowledgementPending, safeResultCode)); return Task.CompletedTask; } }
    private sealed class CancelingClient(List<string> sequence) : IGooglePlaySubscriptionsV2Client { public int Calls { get; private set; } public Task<GooglePlaySubscriptionV2Snapshot?> GetAsync(string packageName, string purchaseToken, CancellationToken cancellationToken) => throw new NotSupportedException(); public Task AcknowledgeAsync(string packageName, string productId, string purchaseToken, CancellationToken cancellationToken) { Calls++; sequence.Add("acknowledge"); throw new OperationCanceledException(); } }
    private sealed class RetryingClient(List<string> sequence) : IGooglePlaySubscriptionsV2Client { public int Calls { get; private set; } public Task<GooglePlaySubscriptionV2Snapshot?> GetAsync(string packageName, string purchaseToken, CancellationToken cancellationToken) => throw new NotSupportedException(); public Task AcknowledgeAsync(string packageName, string productId, string purchaseToken, CancellationToken cancellationToken) { sequence.Add("acknowledge"); if (++Calls == 1) throw new GooglePlaySubscriptionsV2ClientException(GooglePlaySubscriptionsV2ClientFailure.TemporarilyUnavailable); return Task.CompletedTask; } }
    private sealed class Deferral(List<string> sequence, GooglePlayTrialDeferralResultCode code) : IGooglePlayTrialDeferralService { public Task<GooglePlayTrialDeferralResult> ProcessAsync(Guid userId, string purchaseToken, string protectedPurchaseToken, CancellationToken cancellationToken) { sequence.Add("defer"); return Task.FromResult(new GooglePlayTrialDeferralResult(code)); } }
}
