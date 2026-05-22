using EnglishVoiceTutor.Api.Constants;
using EnglishVoiceTutor.Api.Contracts.Usage;
using EnglishVoiceTutor.Api.Options;
using Microsoft.Extensions.Options;

namespace EnglishVoiceTutor.Api.Services.Usage;

public sealed class FreeLimitGuardService(
    IFreeLimitStatusService freeLimitStatusService,
    IOptions<FreeLimitOptions> freeLimitOptions) : IFreeLimitGuardService
{
    private readonly FreeLimitOptions options = freeLimitOptions.Value;

    public async Task<FreeLimitExceededResponse?> CheckChatReplyLimitAsync(string? studyLanguage, CancellationToken cancellationToken)
    {
        if (!options.EnforcementEnabled)
        {
            return null;
        }

        var status = await freeLimitStatusService.GetDevFreeLimitStatusAsync(studyLanguage, cancellationToken);
        return status.ChatReplyLimitExceeded || status.ChatReplyRemaining <= 0
            ? CreateExceededResponse(status, UsageConstants.Operations.LessonChatReply, FreePlanLimitConstants.LimitTypeChatReplies, status.ChatReplyCount, status.ChatReplyLimit, status.ChatReplyRemaining)
            : null;
    }

    public async Task<FreeLimitExceededResponse?> CheckHintLimitAsync(string? studyLanguage, CancellationToken cancellationToken)
    {
        if (!options.EnforcementEnabled)
        {
            return null;
        }

        var status = await freeLimitStatusService.GetDevFreeLimitStatusAsync(studyLanguage, cancellationToken);
        return status.HintLimitExceeded || status.HintRemaining <= 0
            ? CreateExceededResponse(status, UsageConstants.Operations.LessonChatHint, FreePlanLimitConstants.LimitTypeHints, status.HintsUsed, status.HintLimit, status.HintRemaining)
            : null;
    }

    public async Task<FreeLimitExceededResponse?> CheckTranscriptionLimitAsync(string? studyLanguage, CancellationToken cancellationToken)
    {
        if (!options.EnforcementEnabled)
        {
            return null;
        }

        var status = await freeLimitStatusService.GetDevFreeLimitStatusAsync(studyLanguage, cancellationToken);
        return status.TranscriptionLimitExceeded || status.TranscriptionSecondsRemaining <= 0
            ? CreateExceededResponse(status, UsageConstants.Operations.AudioTranscribe, FreePlanLimitConstants.LimitTypeTranscriptionSeconds, status.TranscriptionSeconds, status.TranscriptionSecondsLimit, status.TranscriptionSecondsRemaining)
            : null;
    }

    public async Task<FreeLimitExceededResponse?> CheckTtsLimitAsync(string? studyLanguage, CancellationToken cancellationToken)
    {
        if (!options.EnforcementEnabled)
        {
            return null;
        }

        var status = await freeLimitStatusService.GetDevFreeLimitStatusAsync(studyLanguage, cancellationToken);
        return status.TtsLimitExceeded || status.TtsSecondsRemaining <= 0
            ? CreateExceededResponse(status, UsageConstants.Operations.AudioSpeech, FreePlanLimitConstants.LimitTypeTtsSeconds, status.TtsSeconds, status.TtsSecondsLimit, status.TtsSecondsRemaining)
            : null;
    }

    private static FreeLimitExceededResponse CreateExceededResponse(
        FreeLimitStatusResponse status,
        string operation,
        string limitType,
        int used,
        int limit,
        int remaining)
    {
        return new FreeLimitExceededResponse
        {
            Error = FreePlanLimitConstants.LimitReachedErrorMessage,
            Operation = operation,
            PlanId = status.PlanId,
            LimitType = limitType,
            Used = used,
            Limit = limit,
            Remaining = remaining,
            UsageDate = status.UsageDate,
            StudyLanguage = status.StudyLanguage,
            CheckedAtUtc = status.CheckedAtUtc,
            Source = status.Source
        };
    }
}
