using EnglishVoiceTutor.Api.Data;
using EnglishVoiceTutor.Api.Data.Entities;
using EnglishVoiceTutor.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace EnglishVoiceTutor.Api.Services.Billing;

public static class GooglePlayRtdnEventStatuses { public const string Received = "received"; public const string Processing = "processing"; public const string Processed = "processed"; public const string RetryableFailure = "retryable_failure"; public const string PermanentFailure = "permanent_failure"; }
public static class GooglePlayRtdnSafeErrorCodes
{
    public const string ProviderUnavailable = "provider_unavailable";
    public const string InvalidNotification = "invalid_notification";
    public const string ProviderRejected = "provider_rejected";
    public static string? Normalize(string? value) => value is ProviderUnavailable or InvalidNotification or ProviderRejected ? value : null;
}
public enum GooglePlayRtdnReceiptResultCode { Received, Duplicate, InvalidInput, TemporarilyUnavailable }
public sealed record GooglePlayRtdnReceiptResult(GooglePlayRtdnReceiptResultCode Code, Guid? EventId);
public sealed record GooglePlayRtdnReceipt(string Provider, string PubSubMessageId, string PubSubSubscription, string PackageName, string NotificationKind, string? PurchaseTokenFingerprint, DateTimeOffset? PublishedAtUtc);

public sealed class GooglePlayRtdnEventPersistenceService(AppDbContext dbContext, IUtcClock utcClock)
{
    public async Task<GooglePlayRtdnReceiptResult> RecordReceiptAsync(GooglePlayRtdnReceipt receipt, CancellationToken cancellationToken)
    {
        if (!IsValid(receipt)) return new(GooglePlayRtdnReceiptResultCode.InvalidInput, null);
        try
        {
            var existing = await dbContext.GooglePlayRtdnEvents.SingleOrDefaultAsync(item => item.Provider == receipt.Provider && item.PubSubSubscription == receipt.PubSubSubscription && item.PubSubMessageId == receipt.PubSubMessageId, cancellationToken);
            if (existing is not null) return new(GooglePlayRtdnReceiptResultCode.Duplicate, existing.Id);
            var entity = new GooglePlayRtdnEventEntity { Id = Guid.NewGuid(), Provider = receipt.Provider, PubSubMessageId = receipt.PubSubMessageId, PubSubSubscription = receipt.PubSubSubscription, PackageName = receipt.PackageName, NotificationKind = receipt.NotificationKind, PurchaseTokenFingerprint = receipt.PurchaseTokenFingerprint, Status = GooglePlayRtdnEventStatuses.Received, ReceivedAtUtc = utcClock.UtcNow, PublishedAtUtc = receipt.PublishedAtUtc };
            dbContext.GooglePlayRtdnEvents.Add(entity);
            await dbContext.SaveChangesAsync(cancellationToken);
            return new(GooglePlayRtdnReceiptResultCode.Received, entity.Id);
        }
        catch (DbUpdateException) { return new(GooglePlayRtdnReceiptResultCode.Duplicate, null); }
        catch (Exception) when (!cancellationToken.IsCancellationRequested) { return new(GooglePlayRtdnReceiptResultCode.TemporarilyUnavailable, null); }
    }

    public async Task<bool> MarkProcessingAsync(Guid eventId, CancellationToken cancellationToken) => await TransitionAsync(eventId, GooglePlayRtdnEventStatuses.Processing, cancellationToken);
    public async Task<bool> MarkProcessedAsync(Guid eventId, CancellationToken cancellationToken) => await TransitionAsync(eventId, GooglePlayRtdnEventStatuses.Processed, cancellationToken);

    public async Task<bool> RecordRetryableFailureAsync(Guid eventId, DateTimeOffset nextAttemptAtUtc, string safeErrorCode, CancellationToken cancellationToken)
    {
        var entity = await dbContext.GooglePlayRtdnEvents.SingleOrDefaultAsync(item => item.Id == eventId, cancellationToken);
        var normalized = GooglePlayRtdnSafeErrorCodes.Normalize(safeErrorCode);
        if (entity is null || normalized is null) return false;
        entity.Status = GooglePlayRtdnEventStatuses.RetryableFailure; entity.AttemptCount++; entity.NextAttemptAtUtc = nextAttemptAtUtc; entity.SafeErrorCode = normalized;
        await dbContext.SaveChangesAsync(cancellationToken); return true;
    }

    public async Task<bool> RecordPermanentFailureAsync(Guid eventId, string safeErrorCode, CancellationToken cancellationToken)
    {
        var entity = await dbContext.GooglePlayRtdnEvents.SingleOrDefaultAsync(item => item.Id == eventId, cancellationToken);
        var normalized = GooglePlayRtdnSafeErrorCodes.Normalize(safeErrorCode);
        if (entity is null || normalized is null) return false;
        entity.Status = GooglePlayRtdnEventStatuses.PermanentFailure; entity.ProcessedAtUtc = utcClock.UtcNow; entity.NextAttemptAtUtc = null; entity.SafeErrorCode = normalized;
        await dbContext.SaveChangesAsync(cancellationToken); return true;
    }

    public Task<List<GooglePlayRtdnEventEntity>> GetRetryBatchAsync(DateTimeOffset now, int maximumCount, CancellationToken cancellationToken) => dbContext.GooglePlayRtdnEvents.Where(item => item.Status == GooglePlayRtdnEventStatuses.RetryableFailure && item.NextAttemptAtUtc <= now).OrderBy(item => item.NextAttemptAtUtc).Take(Math.Clamp(maximumCount, 1, 100)).ToListAsync(cancellationToken);

    private async Task<bool> TransitionAsync(Guid eventId, string status, CancellationToken cancellationToken)
    {
        var entity = await dbContext.GooglePlayRtdnEvents.SingleOrDefaultAsync(item => item.Id == eventId, cancellationToken);
        if (entity is null) return false;
        entity.Status = status;
        if (status == GooglePlayRtdnEventStatuses.Processing) { entity.ProcessingStartedAtUtc = utcClock.UtcNow; entity.AttemptCount++; }
        if (status == GooglePlayRtdnEventStatuses.Processed) { entity.ProcessedAtUtc = utcClock.UtcNow; entity.NextAttemptAtUtc = null; }
        await dbContext.SaveChangesAsync(cancellationToken); return true;
    }

    private static bool IsValid(GooglePlayRtdnReceipt item) => !string.IsNullOrWhiteSpace(item.Provider) && item.Provider.Length <= EntityConstants.Lengths.GooglePlayRtdnProviderMaxLength && !string.IsNullOrWhiteSpace(item.PubSubMessageId) && item.PubSubMessageId.Length <= EntityConstants.Lengths.GooglePlayRtdnMessageIdMaxLength && !string.IsNullOrWhiteSpace(item.PubSubSubscription) && item.PubSubSubscription.Length <= EntityConstants.Lengths.GooglePlayRtdnSubscriptionMaxLength && !string.IsNullOrWhiteSpace(item.PackageName) && item.PackageName.Length <= EntityConstants.Lengths.GooglePlayRtdnPackageNameMaxLength && !string.IsNullOrWhiteSpace(item.NotificationKind) && item.NotificationKind.Length <= EntityConstants.Lengths.GooglePlayRtdnNotificationKindMaxLength && (item.PurchaseTokenFingerprint is null || (item.PurchaseTokenFingerprint.Length == EntityConstants.Lengths.GooglePlayPurchaseTokenFingerprintLength && item.PurchaseTokenFingerprint.All(char.IsAsciiHexDigit)));
}
