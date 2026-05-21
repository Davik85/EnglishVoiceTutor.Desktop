using EnglishVoiceTutor.Api.Contracts.Usage;

namespace EnglishVoiceTutor.Api.Services.Usage;

public interface IFreeLimitGuardService
{
    Task<FreeLimitExceededResponse?> CheckChatReplyLimitAsync(string? studyLanguage, CancellationToken cancellationToken);
    Task<FreeLimitExceededResponse?> CheckHintLimitAsync(string? studyLanguage, CancellationToken cancellationToken);
    Task<FreeLimitExceededResponse?> CheckTranscriptionLimitAsync(string? studyLanguage, CancellationToken cancellationToken);
    Task<FreeLimitExceededResponse?> CheckTtsLimitAsync(string? studyLanguage, CancellationToken cancellationToken);
}
