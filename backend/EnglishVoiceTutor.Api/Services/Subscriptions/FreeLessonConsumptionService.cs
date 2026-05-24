using EnglishVoiceTutor.Api.Constants;
using EnglishVoiceTutor.Api.Data;
using EnglishVoiceTutor.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace EnglishVoiceTutor.Api.Services.Subscriptions;

public sealed class FreeLessonConsumptionService(AppDbContext dbContext) : IFreeLessonConsumptionService
{
    public async Task TryRecordConsumptionAsync(Guid sessionId, Guid userId, string studyLanguage, CancellationToken cancellationToken)
    {
        var sessionExists = await dbContext.LessonSessions
            .AsNoTracking()
            .AnyAsync(session => session.Id == sessionId && session.UserId == userId, cancellationToken);

        if (!sessionExists)
        {
            return;
        }

        var validUserMessageCount = await dbContext.LessonMessages
            .AsNoTracking()
            .Where(message =>
                message.SessionId == sessionId &&
                message.Role == LessonMessageConstants.User &&
                message.IsValidLessonTurn)
            .CountAsync(cancellationToken);

        if (validUserMessageCount < SubscriptionConstants.FreeLessonConsumptionMessageThreshold)
        {
            return;
        }

        var now = DateTimeOffset.UtcNow;
        var todayUtc = DateOnly.FromDateTime(now.UtcDateTime);

        var alreadyConsumedToday = await dbContext.DailyFreeLessonUsages
            .AsNoTracking()
            .AnyAsync(usage => usage.UserId == userId && usage.UsageDate == todayUtc, cancellationToken);

        if (alreadyConsumedToday)
        {
            return;
        }

        var usage = new DailyFreeLessonUsageEntity
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            UsageDate = todayUtc,
            StudyLanguage = StudyLanguageConstants.ToCanonicalValue(studyLanguage),
            LessonSessionId = sessionId,
            UserMessageCountAtConsumption = SubscriptionConstants.FreeLessonConsumptionMessageThreshold,
            ConsumedAtUtc = now,
            CreatedAt = now
        };

        dbContext.DailyFreeLessonUsages.Add(usage);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // Idempotent best-effort behavior: duplicate race conditions should not fail lesson flow.
        }
    }
}
