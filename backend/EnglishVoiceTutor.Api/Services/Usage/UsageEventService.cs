using EnglishVoiceTutor.Api.Data;
using EnglishVoiceTutor.Api.Data.Entities;
using EnglishVoiceTutor.Api.Constants;
using Microsoft.EntityFrameworkCore;

namespace EnglishVoiceTutor.Api.Services.Usage;

public sealed class UsageEventService(AppDbContext dbContext, ILogger<UsageEventService> logger) : IUsageEventService
{
    private const string UnknownStudyLanguage = "unknown";

    public async Task TryRecordAsync(UsageEventRecord record, CancellationToken cancellationToken = default)
    {
        if (record.UserId is null || string.IsNullOrWhiteSpace(record.Operation) || string.IsNullOrWhiteSpace(record.Status))
        {
            logger.LogWarning("Skipped usage event persistence due to missing required fields. Operation={Operation}; Status={Status}; HasUserId={HasUserId}.", record.Operation, record.Status, record.UserId.HasValue);
            return;
        }

        try
        {
            var normalizedStatus = NormalizeStatus(record.Status);
            var createdAt = record.CreatedAt ?? DateTimeOffset.UtcNow;
            var usageEvent = new UsageEventEntity
            {
                Id = Guid.NewGuid(),
                UserId = record.UserId.Value,
                SessionId = record.SessionId,
                Operation = record.Operation,
                Model = record.Model,
                StudyLanguage = NormalizeOptional(record.StudyLanguage),
                Status = normalizedStatus,
                InputTokens = record.InputTokens,
                OutputTokens = record.OutputTokens,
                AudioInputTokens = record.AudioInputTokens,
                AudioOutputTokens = record.AudioOutputTokens,
                AudioDurationMs = record.EstimatedDurationSeconds.HasValue ? (int?)Math.Round(record.EstimatedDurationSeconds.Value * 1000m) : null,
                InputChars = record.InputCharacters,
                OutputBytes = record.OutputBytes,
                EstimatedCost = record.EstimatedCost,
                CreatedAt = createdAt
            };

            dbContext.UsageEvents.Add(usageEvent);
            await dbContext.SaveChangesAsync(cancellationToken);

            await TryUpdateDailyCounterAsync(usageEvent, cancellationToken);
        }
        catch (Exception exception) when (exception is DbUpdateException or DbUpdateConcurrencyException or InvalidOperationException)
        {
            logger.LogWarning("Failed to persist usage event. Operation={Operation}; Status={Status}; UserId={UserId}; SessionId={SessionId}; Error={ErrorType}.", record.Operation, record.Status, record.UserId, record.SessionId, exception.GetType().Name);
        }
    }

    private async Task TryUpdateDailyCounterAsync(UsageEventEntity usageEvent, CancellationToken cancellationToken)
    {
        try
        {
            var usageDate = DateOnly.FromDateTime(usageEvent.CreatedAt.UtcDateTime.Date);
            var studyLanguage = string.IsNullOrWhiteSpace(usageEvent.StudyLanguage) ? UnknownStudyLanguage : usageEvent.StudyLanguage.Trim();
            var now = DateTimeOffset.UtcNow;

            var counter = await dbContext.DailyUsageCounters
                .SingleOrDefaultAsync(item => item.UserId == usageEvent.UserId
                    && item.UsageDate == usageDate
                    && item.StudyLanguage == studyLanguage,
                    cancellationToken);

            if (counter is null)
            {
                counter = new DailyUsageCounterEntity
                {
                    Id = Guid.NewGuid(),
                    UserId = usageEvent.UserId,
                    UsageDate = usageDate,
                    StudyLanguage = studyLanguage,
                    CreatedAt = now,
                    UpdatedAt = now
                };

                dbContext.DailyUsageCounters.Add(counter);
            }

            if (string.Equals(usageEvent.Status, UsageConstants.Statuses.Success, StringComparison.OrdinalIgnoreCase))
            {
                switch (usageEvent.Operation)
                {
                    case UsageConstants.Operations.LessonChatReply:
                        counter.ChatReplyCount += 1;
                        break;
                    case UsageConstants.Operations.LessonChatHint:
                        counter.HintsUsed += 1;
                        break;
                    case UsageConstants.Operations.LessonChatFeedback:
                        counter.FeedbackRequests += 1;
                        break;
                    case UsageConstants.Operations.AudioTranscription:
                        counter.TranscriptionSeconds += ConvertMillisecondsToSeconds(usageEvent.AudioDurationMs);
                        break;
                    case UsageConstants.Operations.Tts:
                        counter.TtsSeconds += ConvertMillisecondsToSeconds(usageEvent.AudioDurationMs);
                        break;
                }
            }

            counter.EstimatedCost += usageEvent.EstimatedCost;
            counter.UpdatedAt = now;

            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (Exception exception) when (exception is DbUpdateException or DbUpdateConcurrencyException or InvalidOperationException)
        {
            logger.LogWarning("Failed to update daily usage counter. UsageEventId={UsageEventId}; Operation={Operation}; UserId={UserId}; Error={ErrorType}.", usageEvent.Id, usageEvent.Operation, usageEvent.UserId, exception.GetType().Name);
        }
    }

    private static int ConvertMillisecondsToSeconds(int? milliseconds)
    {
        if (!milliseconds.HasValue || milliseconds.Value <= 0)
        {
            return 0;
        }

        return (int)Math.Round(milliseconds.Value / 1000m, MidpointRounding.AwayFromZero);
    }

    private static string NormalizeStatus(string status)
    {
        if (string.Equals(status, UsageConstants.Statuses.Success, StringComparison.OrdinalIgnoreCase))
        {
            return UsageConstants.Statuses.Success;
        }

        if (string.Equals(status, UsageConstants.Statuses.Failed, StringComparison.OrdinalIgnoreCase))
        {
            return UsageConstants.Statuses.Failed;
        }

        if (string.Equals(status, UsageConstants.Statuses.Skipped, StringComparison.OrdinalIgnoreCase))
        {
            return UsageConstants.Statuses.Skipped;
        }

        return UsageConstants.Statuses.Skipped;
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
