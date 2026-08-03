using EnglishVoiceTutor.Api.Data;
using EnglishVoiceTutor.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace EnglishVoiceTutor.Api.Services.Billing;

public static class GooglePlayPendingRefundReviewStatuses { public const string Received = "received"; public const string Processing = "processing"; public const string RetryableFailure = "retryable_failure"; public const string Processed = "processed"; public const string PermanentFailure = "permanent_failure"; }
public sealed record GooglePlayPendingRefundReceipt(string PubSubMessageId, string PackageName, string TokenFingerprint, string OrderFingerprint, string ProtectedPayload, string NotificationVersion, int RefundReason, DateTimeOffset EventTimeUtc, string RefundPreference, bool SampleContentProvided, DateTimeOffset DeleteAfterUtc);
public enum GooglePlayPendingRefundReceiptResultCode { Received, Duplicate, TemporarilyUnavailable }
public sealed record GooglePlayPendingRefundReceiptResult(GooglePlayPendingRefundReceiptResultCode Code);
public sealed class GooglePlayPendingRefundReviewPersistenceService(AppDbContext db, IUtcClock clock)
{
    public async Task<GooglePlayPendingRefundReceiptResult> RecordAsync(GooglePlayPendingRefundReceipt receipt, CancellationToken ct)
    {
        try { if (await db.GooglePlayPendingRefundReviews.AnyAsync(x => x.PubSubMessageId == receipt.PubSubMessageId || x.PendingRefundTokenFingerprint == receipt.TokenFingerprint, ct)) return new(GooglePlayPendingRefundReceiptResultCode.Duplicate); var now = clock.UtcNow; db.GooglePlayPendingRefundReviews.Add(new() { Id = Guid.NewGuid(), PubSubMessageId = receipt.PubSubMessageId, PackageName = receipt.PackageName, PendingRefundTokenFingerprint = receipt.TokenFingerprint, OrderIdFingerprint = receipt.OrderFingerprint, ProtectedReviewPayload = receipt.ProtectedPayload, ProtectionFormatVersion = GooglePlayPendingRefundReviewProtectionService.ProtectionFormatVersion, NotificationVersion = receipt.NotificationVersion, RefundReason = receipt.RefundReason, EventTimeUtc = receipt.EventTimeUtc, ReceivedAtUtc = now, ReviewDeadlineAtUtc = receipt.EventTimeUtc.AddHours(24), Status = GooglePlayPendingRefundReviewStatuses.Received, RefundPreference = receipt.RefundPreference, SampleContentProvided = receipt.SampleContentProvided, ProtectedPayloadDeleteAfterUtc = receipt.DeleteAfterUtc, CreatedAtUtc = now, UpdatedAtUtc = now }); await db.SaveChangesAsync(ct); return new(GooglePlayPendingRefundReceiptResultCode.Received); }
        catch (DbUpdateException)
        {
            try
            {
                var duplicate = await db.GooglePlayPendingRefundReviews.AnyAsync(
                    item => item.PubSubMessageId == receipt.PubSubMessageId || item.PendingRefundTokenFingerprint == receipt.TokenFingerprint,
                    ct);
                return new(duplicate ? GooglePlayPendingRefundReceiptResultCode.Duplicate : GooglePlayPendingRefundReceiptResultCode.TemporarilyUnavailable);
            }
            catch (Exception) when (!ct.IsCancellationRequested)
            {
                return new(GooglePlayPendingRefundReceiptResultCode.TemporarilyUnavailable);
            }
        }
        catch (Exception) when (!ct.IsCancellationRequested) { return new(GooglePlayPendingRefundReceiptResultCode.TemporarilyUnavailable); }
    }
}
