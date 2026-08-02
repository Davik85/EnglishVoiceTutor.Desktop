using EnglishVoiceTutor.Api.Data;
using EnglishVoiceTutor.Api.Data.Entities;
using EnglishVoiceTutor.Api.Options;
using EnglishVoiceTutor.Api.Services;
using EnglishVoiceTutor.Api.Services.Billing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace EnglishVoiceTutor.Api.Tests.Services;

public sealed class GooglePlayReconciliationIterationServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 2, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task DueSelectionHonorsAcknowledgementSchedulesFinalWindowAttemptsAndBatch()
    {
        await using var db = CreateDb();
        await AddSecretAsync(db, acknowledgementPending: true, next: null, attempts: 0, id: "a");
        await AddSecretAsync(db, acknowledgementPending: true, next: Now.AddMinutes(1), attempts: 0, id: "b");
        await AddSecretAsync(db, acknowledgementPending: false, next: Now.AddMinutes(1), attempts: 0, id: "c");
        await AddSecretAsync(db, acknowledgementPending: false, next: Now, attempts: 0, id: "d", final: Now.AddMinutes(-1));
        await AddSecretAsync(db, acknowledgementPending: false, next: Now, attempts: 3, id: "e");
        await AddSecretAsync(db, acknowledgementPending: false, next: Now, attempts: 0, id: "f");
        var service = new GooglePlayPurchaseTokenSecretPersistenceService(db, new TestClock());

        var first = await service.GetDueReconciliationBatchAsync(Now, 3, 1, TestContext.Current.CancellationToken);
        var all = await service.GetDueReconciliationBatchAsync(Now, 3, 10, TestContext.Current.CancellationToken);

        Assert.Single(first);
        Assert.True(first[0].AcknowledgementPending);
        Assert.Equal(2, all.Count);
        Assert.Contains(all, item => item.AcknowledgementPending && item.NextProviderCheckAtUtc is null);
        Assert.Contains(all, item => !item.AcknowledgementPending && item.NextProviderCheckAtUtc == Now);
    }

    [Fact]
    public async Task TemporaryResultSchedulesItsExactBoundedRetry()
    {
        await using var db = CreateDb();
        var temporary = await AddSecretAsync(db, acknowledgementPending: true, next: null, attempts: 0, id: "temporary");
        var processor = new RecordingProcessor(GooglePlayPurchaseProcessingResultCode.TemporarilyUnavailable);
        var iteration = CreateIteration(db, processor, maximumAttempts: 3, initialRetry: 10, maximumRetry: 15);

        await iteration.RunOnceAsync(TestContext.Current.CancellationToken);

        var updatedTemporary = await db.GooglePlayPurchaseTokenSecrets.SingleAsync(item => item.Id == temporary.Id, TestContext.Current.CancellationToken);
        Assert.Equal(1, updatedTemporary.ReconciliationAttemptCount);
        Assert.Equal(Now.AddSeconds(10), updatedTemporary.NextProviderCheckAtUtc);
        Assert.False(updatedTemporary.AcknowledgementPending);
        Assert.Equal(GooglePlayRtdnSafeErrorCodes.ProviderUnavailable, updatedTemporary.LastSafeResultCode);
        var early = await new GooglePlayPurchaseTokenSecretPersistenceService(db, new TestClock()).GetDueReconciliationBatchAsync(Now.AddSeconds(9), 3, 10, TestContext.Current.CancellationToken);
        Assert.DoesNotContain(early, item => item.Id == temporary.Id);
        Assert.Equal(15, GooglePlayReconciliationIterationService.RetryDelaySeconds(10, Options(initialRetry: 10, maximumRetry: 15)));
    }

    [Fact]
    public async Task FinalAcknowledgementPendingAttemptStopsFutureAutomaticProcessing()
    {
        await using var db = CreateDb();
        var acknowledgement = await AddSecretAsync(db, acknowledgementPending: true, next: null, attempts: 2, id: "ack");
        var processor = new RecordingProcessor(GooglePlayPurchaseProcessingResultCode.AcknowledgementPending);

        await CreateIteration(db, processor, maximumAttempts: 3).RunOnceAsync(TestContext.Current.CancellationToken);

        var updated = await db.GooglePlayPurchaseTokenSecrets.SingleAsync(item => item.Id == acknowledgement.Id, TestContext.Current.CancellationToken);
        Assert.Equal(3, updated.ReconciliationAttemptCount);
        Assert.Null(updated.NextProviderCheckAtUtc);
        Assert.True(updated.AcknowledgementPending);
        Assert.Equal(GooglePlayRtdnSafeErrorCodes.ProviderUnavailable, updated.LastSafeResultCode);
        Assert.DoesNotContain(await new GooglePlayPurchaseTokenSecretPersistenceService(db, new TestClock()).GetDueReconciliationBatchAsync(Now.AddDays(1), 3, 10, TestContext.Current.CancellationToken), item => item.Id == acknowledgement.Id);
    }

    [Fact]
    public async Task VerifiedClearsTokenRetryMetadataAndTestRtdnCompletesWithoutProcessorCall()
    {
        await using var db = CreateDb();
        var secret = await AddSecretAsync(db, acknowledgementPending: true, next: null, attempts: 2, id: "verified");
        db.GooglePlayRtdnEvents.Add(new GooglePlayRtdnEventEntity { Id = Guid.NewGuid(), Provider = "google_play", PubSubMessageId = "test", PubSubSubscription = "sub", PackageName = "pkg", NotificationKind = "test_notification", Status = GooglePlayRtdnEventStatuses.Received, ReceivedAtUtc = Now });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var processor = new RecordingProcessor(GooglePlayPurchaseProcessingResultCode.Verified);

        await CreateIteration(db, processor).RunOnceAsync(TestContext.Current.CancellationToken);

        var updated = await db.GooglePlayPurchaseTokenSecrets.SingleAsync(item => item.Id == secret.Id, TestContext.Current.CancellationToken);
        Assert.False(updated.AcknowledgementPending);
        Assert.Null(updated.NextProviderCheckAtUtc);
        Assert.Equal(0, updated.ReconciliationAttemptCount);
        Assert.Equal(1, processor.Calls);
        Assert.Equal(GooglePlayRtdnEventStatuses.Processed, Assert.Single(db.GooglePlayRtdnEvents).Status);
    }

    [Fact]
    public async Task PurchaseRtdnUsesProcessorAndStaleLeaseIsReclaimedButFreshLeaseIsNot()
    {
        await using var db = CreateDb();
        var secret = await AddSecretAsync(db, acknowledgementPending: false, next: Now.AddDays(1), attempts: 0, id: "event");
        db.GooglePlayRtdnEvents.AddRange(
            Event("stale", secret.PurchaseTokenFingerprint, GooglePlayRtdnEventStatuses.Processing, Now.AddMinutes(-10)),
            Event("fresh", secret.PurchaseTokenFingerprint, GooglePlayRtdnEventStatuses.Processing, Now.AddMinutes(-1)));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var processor = new RecordingProcessor(GooglePlayPurchaseProcessingResultCode.Verified);

        await CreateIteration(db, processor, leaseSeconds: 300).RunOnceAsync(TestContext.Current.CancellationToken);

        Assert.Equal(1, processor.Calls);
        Assert.Equal(GooglePlayRtdnEventStatuses.Processed, (await db.GooglePlayRtdnEvents.SingleAsync(item => item.PubSubMessageId == "stale", TestContext.Current.CancellationToken)).Status);
        Assert.Equal(GooglePlayRtdnEventStatuses.Processing, (await db.GooglePlayRtdnEvents.SingleAsync(item => item.PubSubMessageId == "fresh", TestContext.Current.CancellationToken)).Status);
    }

    [Fact]
    public async Task FutureAcknowledgementAndMaximumAttemptTokensAreNotProcessed()
    {
        await using var db = CreateDb();
        await AddSecretAsync(db, acknowledgementPending: true, next: Now.AddMinutes(1), attempts: 0, id: "future-ack");
        await AddSecretAsync(db, acknowledgementPending: true, next: null, attempts: 3, id: "exhausted");
        var processor = new RecordingProcessor(GooglePlayPurchaseProcessingResultCode.Verified);
        await CreateIteration(db, processor, maximumAttempts: 3).RunOnceAsync(TestContext.Current.CancellationToken);
        Assert.Equal(0, processor.Calls);
    }

    [Fact]
    public async Task RtdnTemporaryPermanentMissingSecretAndUnprotectFailureAreSafe()
    {
        await using var db = CreateDb();
        var secret = await AddSecretAsync(db, acknowledgementPending: false, next: Now.AddDays(1), attempts: 0, id: "rtdn");
        db.GooglePlayRtdnEvents.AddRange(Event("temporary", secret.PurchaseTokenFingerprint, GooglePlayRtdnEventStatuses.Received, Now), Event("permanent", secret.PurchaseTokenFingerprint, GooglePlayRtdnEventStatuses.Received, Now), Event("missing", "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa", GooglePlayRtdnEventStatuses.Received, Now));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        await CreateIteration(db, new RecordingProcessor(GooglePlayPurchaseProcessingResultCode.TemporarilyUnavailable, GooglePlayPurchaseProcessingResultCode.InvalidPurchase), maximumAttempts: 3).RunOnceAsync(TestContext.Current.CancellationToken);
        Assert.Contains(db.GooglePlayRtdnEvents, item => item.Status == GooglePlayRtdnEventStatuses.RetryableFailure && item.NextAttemptAtUtc > Now);
        Assert.Contains(db.GooglePlayRtdnEvents, item => item.Status == GooglePlayRtdnEventStatuses.PermanentFailure);
        Assert.Contains(db.GooglePlayRtdnEvents, item => item.PubSubMessageId == "missing" && item.Status == GooglePlayRtdnEventStatuses.RetryableFailure);

        var failed = Event("unprotect", secret.PurchaseTokenFingerprint, GooglePlayRtdnEventStatuses.Received, Now); db.GooglePlayRtdnEvents.Add(failed); await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        await CreateIteration(db, new RecordingProcessor(GooglePlayPurchaseProcessingResultCode.Verified), protection: new FailedProtection()).RunOnceAsync(TestContext.Current.CancellationToken);
        Assert.Equal(GooglePlayRtdnEventStatuses.PermanentFailure, (await db.GooglePlayRtdnEvents.SingleAsync(item => item.Id == failed.Id, TestContext.Current.CancellationToken)).Status);
    }

    [Fact]
    public async Task RequestedCancellationIsRethrown()
    {
        await using var db = CreateDb();
        await AddSecretAsync(db, acknowledgementPending: true, next: null, attempts: 0, id: "cancel");
        using var cancellation = new CancellationTokenSource(); cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => CreateIteration(db, new RecordingProcessor(GooglePlayPurchaseProcessingResultCode.Verified)).RunOnceAsync(cancellation.Token));
    }

    private static GooglePlayReconciliationIterationService CreateIteration(AppDbContext db, IGooglePlayPurchaseProcessor processor, int maximumAttempts = 10, int initialRetry = 60, int maximumRetry = 3600, int leaseSeconds = 300, IGooglePlayPurchaseTokenProtectionService? protection = null) => new(db, new GooglePlayRtdnEventPersistenceService(db, new TestClock()), new GooglePlayPurchaseTokenSecretPersistenceService(db, new TestClock()), protection ?? new FakeProtection(), processor, new TestClock(), Microsoft.Extensions.Options.Options.Create(Options(maximumAttempts, initialRetry, maximumRetry, leaseSeconds)), NullLogger<GooglePlayReconciliationIterationService>.Instance);
    private static GooglePlayReconciliationOptions Options(int maximumAttempts = 10, int initialRetry = 60, int maximumRetry = 3600, int leaseSeconds = 300) => new() { BatchSize = 20, MaximumAttempts = maximumAttempts, InitialRetrySeconds = initialRetry, MaximumRetrySeconds = maximumRetry, ProcessingLeaseSeconds = leaseSeconds };
    private static GooglePlayRtdnEventEntity Event(string id, string fingerprint, string status, DateTimeOffset processingStarted) => new() { Id = Guid.NewGuid(), Provider = "google_play", PubSubMessageId = id, PubSubSubscription = "sub", PackageName = "pkg", NotificationKind = "subscription_notification", PurchaseTokenFingerprint = fingerprint, Status = status, ReceivedAtUtc = Now, ProcessingStartedAtUtc = processingStarted };
    private static async Task<GooglePlayPurchaseTokenSecretEntity> AddSecretAsync(AppDbContext db, bool acknowledgementPending, DateTimeOffset? next, int attempts, string id, DateTimeOffset? final = null)
    {
        var user = new UserEntity { Id = Guid.NewGuid(), Email = $"{id}@example.test", PasswordHash = "hash", Status = "active", CreatedAt = Now };
        var fingerprint = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(id))).ToLowerInvariant();
        var claim = new GooglePlayPurchaseClaimEntity { Id = Guid.NewGuid(), UserId = user.Id, PurchaseTokenFingerprint = fingerprint, ProductId = "product", CreatedAtUtc = Now, LastSeenAtUtc = Now };
        var secret = new GooglePlayPurchaseTokenSecretEntity { Id = Guid.NewGuid(), GooglePlayPurchaseClaimId = claim.Id, PurchaseTokenFingerprint = fingerprint, ProtectedPurchaseToken = "protected-value", ProtectionFormatVersion = "v1", CreatedAtUtc = Now, UpdatedAtUtc = Now, AcknowledgementPending = acknowledgementPending, NextProviderCheckAtUtc = next, ReconciliationAttemptCount = attempts, FinalRecheckUntilUtc = final };
        db.AddRange(user, claim, secret); await db.SaveChangesAsync(TestContext.Current.CancellationToken); return secret;
    }
    private static AppDbContext CreateDb() => new(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString("N")).Options);
    private sealed class TestClock : IUtcClock { public DateTimeOffset UtcNow => Now; }
    private sealed class FakeProtection : IGooglePlayPurchaseTokenProtectionService { public string Protect(string purchaseToken) => "protected"; public GooglePlayPurchaseTokenUnprotectResult TryUnprotect(string protectedPurchaseToken) => new(true, "raw-token"); }
    private sealed class FailedProtection : IGooglePlayPurchaseTokenProtectionService { public string Protect(string purchaseToken) => "protected"; public GooglePlayPurchaseTokenUnprotectResult TryUnprotect(string protectedPurchaseToken) => GooglePlayPurchaseTokenUnprotectResult.Failure; }
    private sealed class RecordingProcessor(params GooglePlayPurchaseProcessingResultCode[] results) : IGooglePlayPurchaseProcessor { private int index; public int Calls { get; private set; } public Task<GooglePlayPurchaseProcessingResult> ProcessAsync(Guid userId, string purchaseToken, CancellationToken cancellationToken) { Calls++; return Task.FromResult(new GooglePlayPurchaseProcessingResult(results[Math.Min(index++, results.Length - 1)])); } }
}
