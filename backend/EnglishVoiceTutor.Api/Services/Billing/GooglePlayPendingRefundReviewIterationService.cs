using EnglishVoiceTutor.Api.Data;
using EnglishVoiceTutor.Api.Options;
using EnglishVoiceTutor.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace EnglishVoiceTutor.Api.Services.Billing;

public sealed class GooglePlayPendingRefundReviewIterationService(AppDbContext db, IGooglePlayPendingRefundReviewProtectionService protection, IGooglePlayPendingRefundFingerprintService fingerprints, IGooglePlayReviewRefundClient client, IUtcClock clock, IOptions<GooglePlayPendingRefundReviewOptions> optionsAccessor)
{
    public async Task RunOnceAsync(CancellationToken ct)
    {
        var options = optionsAccessor.Value; var now = clock.UtcNow; var stale = now.AddSeconds(-options.ProcessingLeaseSeconds);
        var rows = await db.GooglePlayPendingRefundReviews.Where(x => x.Status == GooglePlayPendingRefundReviewStatuses.Received || (x.Status == GooglePlayPendingRefundReviewStatuses.RetryableFailure && x.NextAttemptAtUtc <= now) || (x.Status == GooglePlayPendingRefundReviewStatuses.Processing && x.ProcessingStartedAtUtc <= stale)).OrderBy(x => x.Status == GooglePlayPendingRefundReviewStatuses.Received ? 0 : x.Status == GooglePlayPendingRefundReviewStatuses.RetryableFailure ? 1 : 2).ThenBy(x => x.ReviewDeadlineAtUtc).ThenBy(x => x.Id).Take(options.BatchSize).ToListAsync(ct);
        foreach (var row in rows) { ct.ThrowIfCancellationRequested(); await ProcessAsync(row.Id, now, options, ct); }
        var cleanup = await db.GooglePlayPendingRefundReviews.Where(x => (x.Status == GooglePlayPendingRefundReviewStatuses.Processed || x.Status == GooglePlayPendingRefundReviewStatuses.PermanentFailure) && x.ProtectedReviewPayload != null && x.ProtectedPayloadDeleteAfterUtc <= now).ToListAsync(ct);
        foreach (var row in cleanup) { row.ProtectedReviewPayload = null; row.UpdatedAtUtc = now; }
        if (cleanup.Count != 0) await db.SaveChangesAsync(ct);
    }
    private async Task ProcessAsync(Guid id, DateTimeOffset now, GooglePlayPendingRefundReviewOptions options, CancellationToken ct)
    {
        var row = await db.GooglePlayPendingRefundReviews.SingleOrDefaultAsync(x => x.Id == id, ct); if (row is null || row.Status is GooglePlayPendingRefundReviewStatuses.Processed or GooglePlayPendingRefundReviewStatuses.PermanentFailure || (row.Status == GooglePlayPendingRefundReviewStatuses.Processing && row.ProcessingStartedAtUtc > now.AddSeconds(-options.ProcessingLeaseSeconds))) return;
        row.Status = GooglePlayPendingRefundReviewStatuses.Processing; row.ProcessingStartedAtUtc = now; row.AttemptCount++; row.UpdatedAtUtc = now; await db.SaveChangesAsync(ct);
        var payload = protection.TryUnprotect(row.ProtectedReviewPayload ?? string.Empty);
        var payloadMatchesFingerprints = false;
        if (payload.Succeeded && payload.PendingRefundToken is not null && payload.OrderId is not null)
        {
            try
            {
                payloadMatchesFingerprints = fingerprints.CreatePendingRefundTokenFingerprint(payload.PendingRefundToken) == row.PendingRefundTokenFingerprint
                    && fingerprints.CreateOrderIdFingerprint(payload.OrderId) == row.OrderIdFingerprint;
            }
            catch (Exception) when (!ct.IsCancellationRequested) { }
        }
        if (!payloadMatchesFingerprints) { Fail(row, now, "protection_failure"); await db.SaveChangesAsync(ct); return; }
        var result = await client.ReviewAsync(row.PackageName, payload.OrderId!, payload.PendingRefundToken!, row.SampleContentProvided, row.RefundPreference, ct);
        if (result.Code == GooglePlayReviewRefundResultCode.Processed) { row.Status = GooglePlayPendingRefundReviewStatuses.Processed; row.ReviewedAtUtc = now; row.NextAttemptAtUtc = null; row.ProtectedReviewPayload = null; row.LastSafeResultCode = row.ReviewDeadlineAtUtc < now ? "deadline_overdue" : "processed"; row.UpdatedAtUtc = now; await db.SaveChangesAsync(ct); return; }
        if (result.Code == GooglePlayReviewRefundResultCode.PermanentFailure || row.AttemptCount >= options.MaximumAttempts) { Fail(row, now, result.Code == GooglePlayReviewRefundResultCode.PermanentFailure ? "provider_rejected" : "maximum_attempts"); await db.SaveChangesAsync(ct); return; }
        row.Status = GooglePlayPendingRefundReviewStatuses.RetryableFailure; row.NextAttemptAtUtc = now.AddSeconds(RetryDelaySeconds(row.AttemptCount, options)); row.LastSafeResultCode = row.ReviewDeadlineAtUtc < now ? "deadline_overdue" : "provider_unavailable"; row.UpdatedAtUtc = now; await db.SaveChangesAsync(ct);
    }
    private static void Fail(Data.Entities.GooglePlayPendingRefundReviewEntity row, DateTimeOffset now, string code) { row.Status = GooglePlayPendingRefundReviewStatuses.PermanentFailure; row.NextAttemptAtUtc = null; row.LastSafeResultCode = code; row.UpdatedAtUtc = now; }
    public static int RetryDelaySeconds(int attempts, GooglePlayPendingRefundReviewOptions options) => (int)Math.Min(options.MaximumRetrySeconds, (long)options.InitialRetrySeconds << Math.Clamp(attempts - 1, 0, 20));
}

public sealed class GooglePlayPendingRefundReviewWorker(IServiceScopeFactory scopes, IOptions<GooglePlayPendingRefundReviewOptions> options, ILogger<GooglePlayPendingRefundReviewWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken) { while (!stoppingToken.IsCancellationRequested) { try { using var scope = scopes.CreateScope(); await scope.ServiceProvider.GetRequiredService<GooglePlayPendingRefundReviewIterationService>().RunOnceAsync(stoppingToken); } catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { throw; } catch (Exception) { logger.LogWarning("Google Play pending-refund review iteration failed. ResultCode={ResultCode}.", "provider_unavailable"); } await Task.Delay(TimeSpan.FromSeconds(options.Value.PollIntervalSeconds), stoppingToken); } }
}
