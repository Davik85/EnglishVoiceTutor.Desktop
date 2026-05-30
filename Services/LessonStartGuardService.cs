using EnglishVoiceTutor.Desktop.Models;
using EnglishVoiceTutor.Desktop.Models.Access;
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
        if (!isSignedIn)
        {
            return CreateBlockedResult(AccessDisplayStateMapper.MapSignedOut());
        }

        var lessonAccessResult = await backendLessonAccessDecisionClient.GetAsync(backendBaseUrl, cancellationToken);
        var subscriptionStatusResult = await backendSubscriptionStatusClient.GetAsync(backendBaseUrl, cancellationToken);
        var backendDecision = lessonAccessResult.Value;
        var subscriptionStatus = subscriptionStatusResult.Value;

        if (backendDecision is null)
        {
            return CreateBlockedResult(AccessDisplayStateMapper.MapUnknownOrError());
        }

        var enforcementEnabled = backendDecision.EnforcementEnabled;
        var canStartNewLesson = backendDecision.CanStartNewLesson;
        var accessDisplay = AccessDisplayStateMapper.Map(isSignedIn, backendDecision, subscriptionStatus);

        return new LessonStartGuardResult(
            canStartNewLesson,
            IsBackendDecisionAvailable: true,
            backendDecision.Source,
            backendDecision.Decision,
            backendDecision.Reason,
            enforcementEnabled,
            canStartNewLesson,
            backendDecision.FreeLessonUsedToday,
            backendDecision.FreeLessonRemainingToday,
            accessDisplay);
    }

    private static LessonStartGuardResult CreateBlockedResult(AccessDisplayModel accessDisplay)
    {
        return new LessonStartGuardResult(
            ShouldAllowStart: false,
            IsBackendDecisionAvailable: false,
            LocalFallbackSource,
            DefaultUnavailableValue,
            DefaultUnavailableValue,
            EnforcementEnabled: false,
            CanStartNewLesson: null,
            FreeLessonUsedToday: null,
            FreeLessonRemainingToday: null,
            accessDisplay);
    }
}
