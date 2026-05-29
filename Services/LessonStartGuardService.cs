using EnglishVoiceTutor.Desktop.Models;
using EnglishVoiceTutor.Desktop.Services.Access;

namespace EnglishVoiceTutor.Desktop.Services;

public sealed class LessonStartGuardService
{
    private const string DefaultUnavailableValue = "unavailable";
    private const string LocalFallbackSource = "desktop_fallback";
    private readonly BackendLessonAccessDecisionClient backendLessonAccessDecisionClient;
    private readonly BackendSubscriptionStatusClient backendSubscriptionStatusClient;

    public LessonStartGuardService()
        : this(new BackendLessonAccessDecisionClient(), new BackendSubscriptionStatusClient())
    {
    }

    public LessonStartGuardService(BackendLessonAccessDecisionClient backendLessonAccessDecisionClient)
        : this(backendLessonAccessDecisionClient, new BackendSubscriptionStatusClient())
    {
    }

    public LessonStartGuardService(
        BackendLessonAccessDecisionClient backendLessonAccessDecisionClient,
        BackendSubscriptionStatusClient backendSubscriptionStatusClient)
    {
        this.backendLessonAccessDecisionClient = backendLessonAccessDecisionClient;
        this.backendSubscriptionStatusClient = backendSubscriptionStatusClient;
    }

    public async Task<LessonStartGuardResult> CheckAsync(string? backendBaseUrl, bool isSignedIn, CancellationToken cancellationToken = default)
    {
        var lessonAccessResult = await backendLessonAccessDecisionClient.GetAsync(backendBaseUrl, cancellationToken);
        var subscriptionStatusResult = isSignedIn
            ? await backendSubscriptionStatusClient.GetAsync(backendBaseUrl, cancellationToken)
            : null;
        var backendDecision = lessonAccessResult.Value;
        var subscriptionStatus = subscriptionStatusResult?.Value;

        var enforcementEnabled = backendDecision?.EnforcementEnabled ?? false;
        var canStartNewLesson = backendDecision?.CanStartNewLesson;
        var shouldAllowStart = !enforcementEnabled || canStartNewLesson != false;
        var accessDisplay = AccessDisplayStateMapper.Map(isSignedIn, backendDecision, subscriptionStatus);

        return new LessonStartGuardResult(
            shouldAllowStart,
            backendDecision is not null,
            backendDecision?.Source ?? LocalFallbackSource,
            backendDecision?.Decision ?? DefaultUnavailableValue,
            backendDecision?.Reason ?? DefaultUnavailableValue,
            enforcementEnabled,
            canStartNewLesson,
            backendDecision?.FreeLessonUsedToday,
            backendDecision?.FreeLessonRemainingToday,
            accessDisplay);
    }
}
