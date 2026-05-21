using EnglishVoiceTutor.Api.Constants;
using EnglishVoiceTutor.Api.Contracts.Usage;
using EnglishVoiceTutor.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace EnglishVoiceTutor.Api.Services.Usage;

public sealed class FreeLimitStatusService(
    AppDbContext dbContext,
    DevUserProvider devUserProvider) : IFreeLimitStatusService
{
    public async Task<FreeLimitStatusResponse> GetDevFreeLimitStatusAsync(string? studyLanguage, CancellationToken cancellationToken)
    {
        var userId = devUserProvider.GetDevUserId();
        var usageDate = DateOnly.FromDateTime(DateTime.UtcNow);
        var resolvedStudyLanguage = await ResolveStudyLanguageAsync(userId, studyLanguage, cancellationToken);

        var counter = await dbContext.DailyUsageCounters
            .AsNoTracking()
            .Where(item => item.UserId == userId
                && item.UsageDate == usageDate
                && item.StudyLanguage == resolvedStudyLanguage)
            .FirstOrDefaultAsync(cancellationToken);

        var chatReplyCount = counter?.ChatReplyCount ?? 0;
        var hintsUsed = counter?.HintsUsed ?? 0;
        var transcriptionSeconds = counter?.TranscriptionSeconds ?? 0;
        var ttsSeconds = counter?.TtsSeconds ?? 0;
        var estimatedCost = counter?.EstimatedCost ?? 0m;

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
            CreatedAt = counter?.CreatedAt,
            CounterUpdatedAt = counter?.UpdatedAt,
            CheckedAtUtc = DateTimeOffset.UtcNow,
            Source = FreePlanLimitConstants.Source
        };
    }

    private async Task<string> ResolveStudyLanguageAsync(Guid userId, string? studyLanguage, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(studyLanguage))
        {
            return studyLanguage.Trim();
        }

        var userStudyLanguage = await dbContext.UserSettings
            .AsNoTracking()
            .Where(item => item.UserId == userId)
            .Select(item => item.StudyLanguage)
            .FirstOrDefaultAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(userStudyLanguage))
        {
            return userStudyLanguage.Trim();
        }

        return StudyLanguageConstants.DefaultStudyLanguage;
    }

    private static int Remaining(int limit, int used) => Math.Max(0, limit - used);

    private static decimal Remaining(decimal limit, decimal used) => Math.Max(0m, limit - used);
}
