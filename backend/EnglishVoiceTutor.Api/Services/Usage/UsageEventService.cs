using EnglishVoiceTutor.Api.Data;
using EnglishVoiceTutor.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace EnglishVoiceTutor.Api.Services.Usage;

public sealed class UsageEventService(AppDbContext dbContext, ILogger<UsageEventService> logger) : IUsageEventService
{
    public async Task TryRecordAsync(UsageEventRecord record, CancellationToken cancellationToken = default)
    {
        if (record.UserId is null || string.IsNullOrWhiteSpace(record.Operation) || string.IsNullOrWhiteSpace(record.Status))
        {
            logger.LogWarning("Skipped usage event persistence due to missing required fields. Operation={Operation}; Status={Status}; HasUserId={HasUserId}.", record.Operation, record.Status, record.UserId.HasValue);
            return;
        }

        try
        {
            dbContext.UsageEvents.Add(new UsageEventEntity
            {
                Id = Guid.NewGuid(),
                UserId = record.UserId.Value,
                SessionId = record.SessionId,
                Operation = record.Operation,
                Model = record.Model,
                InputTokens = record.InputTokens,
                OutputTokens = record.OutputTokens,
                AudioInputTokens = record.AudioInputTokens,
                AudioOutputTokens = record.AudioOutputTokens,
                AudioDurationMs = record.EstimatedDurationSeconds.HasValue ? (int?)Math.Round(record.EstimatedDurationSeconds.Value * 1000m) : null,
                InputChars = record.InputCharacters,
                OutputBytes = record.OutputBytes,
                EstimatedCost = record.EstimatedCost,
                CreatedAt = record.CreatedAt ?? DateTimeOffset.UtcNow
            });

            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (Exception exception) when (exception is DbUpdateException or DbUpdateConcurrencyException or InvalidOperationException)
        {
            logger.LogWarning("Failed to persist usage event. Operation={Operation}; Status={Status}; UserId={UserId}; SessionId={SessionId}; Error={ErrorType}.", record.Operation, record.Status, record.UserId, record.SessionId, exception.GetType().Name);
        }
    }
}
