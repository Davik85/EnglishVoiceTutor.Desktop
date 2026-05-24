using EnglishVoiceTutor.Desktop.Models;

namespace EnglishVoiceTutor.Desktop.Services;

public sealed class LessonStartGuardService
{
    private const string DefaultUnavailableValue = "unavailable";
    private const string LocalFallbackSource = "desktop_fallback";
    private readonly BackendLessonAccessDecisionClient backendLessonAccessDecisionClient;

    public LessonStartGuardService()
        : this(new BackendLessonAccessDecisionClient())
    {
    }

    public LessonStartGuardService(BackendLessonAccessDecisionClient backendLessonAccessDecisionClient)
    {
        this.backendLessonAccessDecisionClient = backendLessonAccessDecisionClient;
    }

    public async Task<LessonStartGuardResult> CheckAsync(string? backendBaseUrl, CancellationToken cancellationToken = default)
    {
        var result = await backendLessonAccessDecisionClient.GetAsync(backendBaseUrl, cancellationToken);
        var backendDecision = result.Value;

        var enforcementEnabled = backendDecision?.EnforcementEnabled ?? false;
        var canStartNewLesson = backendDecision?.CanStartNewLesson;
        var shouldAllowStart = !enforcementEnabled || canStartNewLesson != false;

        return new LessonStartGuardResult(
            shouldAllowStart,
            backendDecision is not null,
            backendDecision?.Source ?? LocalFallbackSource,
            backendDecision?.Decision ?? DefaultUnavailableValue,
            backendDecision?.Reason ?? DefaultUnavailableValue,
            enforcementEnabled,
            canStartNewLesson,
            backendDecision?.FreeLessonUsedToday,
            backendDecision?.FreeLessonRemainingToday);
    }
}
