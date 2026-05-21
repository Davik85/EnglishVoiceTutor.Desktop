using EnglishVoiceTutor.Api.Constants;
using EnglishVoiceTutor.Api.Contracts.Usage;
using EnglishVoiceTutor.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace EnglishVoiceTutor.Api.Services.Usage;

public sealed class FreeLimitStatusService(
    AppDbContext dbContext,
    DevUserProvider devUserProvider,
    UsageStudyLanguageNormalizer usageStudyLanguageNormalizer) : IFreeLimitStatusService
{
    public async Task<FreeLimitStatusResponse> GetDevFreeLimitStatusAsync(string? studyLanguage, CancellationToken cancellationToken)
    {
        var userId = devUserProvider.GetDevUserId();
        var usageDate = DateOnly.FromDateTime(DateTime.UtcNow);
        var resolvedStudyLanguage = await ResolveStudyLanguageAsync(userId, studyLanguage, cancellationToken);

        var aliases = usageStudyLanguageNormalizer.GetAliasesForCanonical(resolvedStudyLanguage);

        var counters = await dbContext.DailyUsageCounters
            .AsNoTracking()
            .Where(item => item.UserId == userId
                && item.UsageDate == usageDate
                && aliases.Contains(item.StudyLanguage))
            .ToListAsync(cancellationToken);

        var chatReplyCount = counters.Sum(item => item.ChatReplyCount);
        var hintsUsed = counters.Sum(item => item.HintsUsed);
        var transcriptionSeconds = counters.Sum(item => item.TranscriptionSeconds);
        var ttsSeconds = counters.Sum(item => item.TtsSeconds);
        var estimatedCost = counters.Sum(item => item.EstimatedCost);

        var createdAt = counters.Count == 0 ? null : counters.Min(item => item.CreatedAt);
        var counterUpdatedAt = counters.Count == 0 ? null : counters.Max(item => item.UpdatedAt);

        return new FreeLimitStatusResponse
        {
            UserId = userId,
            UsageDate = usageDate,
            StudyLanguage = resolvedStudyLanguage,
            PlanId = FreePlanLimitConstants.PlanId,
            ChatReplyCount = chatReplyCount,
            ChatReplyLimit = FreePlanLimitConstants.ChatReplyLimitPerDay,
            ChatReplyRemaining = Remaining(FreePlanLimitConstants.ChatReplyLimitPerDay, chatReplyCount),
            ChatReplyLimitExceeded = chatReplyCount >= FreePlanLimitConstants.ChatReplyLimitPerDay,
            HintsUsed = hintsUsed,
            HintLimit = FreePlanLimitConstants.HintLimitPerDay,
            HintRemaining = Remaining(FreePlanLimitConstants.HintLimitPerDay, hintsUsed),
            HintLimitExceeded = hintsUsed >= FreePlanLimitConstants.HintLimitPerDay,
            TranscriptionSeconds = transcriptionSeconds,
            TranscriptionSecondsLimit = FreePlanLimitConstants.TranscriptionSecondsLimitPerDay,
            TranscriptionSecondsRemaining = Remaining(FreePlanLimitConstants.TranscriptionSecondsLimitPerDay, transcriptionSeconds),
            TranscriptionLimitExceeded = transcriptionSeconds >= FreePlanLimitConstants.TranscriptionSecondsLimitPerDay,
            TtsSeconds = ttsSeconds,
            TtsSecondsLimit = FreePlanLimitConstants.TtsSecondsLimitPerDay,
            TtsSecondsRemaining = Remaining(FreePlanLimitConstants.TtsSecondsLimitPerDay, ttsSeconds),
            TtsLimitExceeded = ttsSeconds >= FreePlanLimitConstants.TtsSecondsLimitPerDay,
            EstimatedCost = estimatedCost,
            EstimatedCostLimit = FreePlanLimitConstants.EstimatedCostLimitPerDay,
            EstimatedCostRemaining = Remaining(FreePlanLimitConstants.EstimatedCostLimitPerDay, estimatedCost),
            EstimatedCostLimitExceeded = estimatedCost >= FreePlanLimitConstants.EstimatedCostLimitPerDay,
            CreatedAt = createdAt,
            CounterUpdatedAt = counterUpdatedAt,
            CheckedAtUtc = DateTimeOffset.UtcNow,
            Source = FreePlanLimitConstants.Source
        };
    }

    private async Task<string> ResolveStudyLanguageAsync(Guid userId, string? studyLanguage, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(studyLanguage))
        {
            return usageStudyLanguageNormalizer.NormalizeOrDefault(studyLanguage, StudyLanguageConstants.DefaultStudyLanguage);
        }

        var userStudyLanguage = await dbContext.UserSettings
            .AsNoTracking()
            .Where(item => item.UserId == userId)
            .Select(item => item.StudyLanguage)
            .FirstOrDefaultAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(userStudyLanguage))
        {
            return usageStudyLanguageNormalizer.NormalizeOrDefault(userStudyLanguage, StudyLanguageConstants.DefaultStudyLanguage);
        }

        return StudyLanguageConstants.DefaultStudyLanguage;
    }

    private static int Remaining(int limit, int used) => Math.Max(0, limit - used);

    private static decimal Remaining(decimal limit, decimal used) => Math.Max(0m, limit - used);
}
