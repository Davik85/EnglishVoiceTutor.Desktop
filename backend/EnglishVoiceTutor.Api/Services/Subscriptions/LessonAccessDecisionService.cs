using EnglishVoiceTutor.Api.Constants;
using EnglishVoiceTutor.Api.Contracts.Subscription;

namespace EnglishVoiceTutor.Api.Services.Subscriptions;

public sealed class LessonAccessDecisionService(ISubscriptionStatusService subscriptionStatusService) : ILessonAccessDecisionService
{
    public async Task<LessonAccessDecisionResponse> GetDecisionAsync(Guid userId, string source, CancellationToken cancellationToken)
    {
        var status = await subscriptionStatusService.GetStatusAsync(userId, source, cancellationToken);

        var response = new LessonAccessDecisionResponse
        {
            UserId = status.UserId,
            PremiumActive = status.PremiumActive,
            TrialActive = status.TrialActive,
            FreeLessonUsedToday = status.FreeLessonUsedToday,
            FreeLessonRemainingToday = status.FreeLessonRemainingToday,
            EnforcementEnabled = status.EnforcementEnabled,
            Source = status.Source,
            CheckedAtUtc = status.CheckedAtUtc
        };

        if (status.PremiumActive)
        {
            response.CanStartNewLesson = true;
            response.Decision = SubscriptionConstants.LessonAccessDecisions.AllowedPremium;
            response.Reason = SubscriptionConstants.LessonAccessReasons.PremiumActive;
            return response;
        }

        if (status.TrialActive)
        {
            response.CanStartNewLesson = true;
            response.Decision = SubscriptionConstants.LessonAccessDecisions.AllowedTrial;
            response.Reason = SubscriptionConstants.LessonAccessReasons.TrialActive;
            return response;
        }

        if (status.FreeLessonRemainingToday > 0)
        {
            response.CanStartNewLesson = true;
            response.Decision = SubscriptionConstants.LessonAccessDecisions.AllowedFreeRemaining;
            response.Reason = SubscriptionConstants.LessonAccessReasons.FreeLessonRemaining;
            return response;
        }

        if (!status.EnforcementEnabled)
        {
            response.CanStartNewLesson = true;
            response.Decision = SubscriptionConstants.LessonAccessDecisions.AllowedEnforcementDisabled;
            response.Reason = SubscriptionConstants.LessonAccessReasons.FreeLimitUsedButEnforcementDisabled;
            return response;
        }

        response.CanStartNewLesson = false;
        response.Decision = SubscriptionConstants.LessonAccessDecisions.BlockedFreeLimitUsed;
        response.Reason = SubscriptionConstants.LessonAccessReasons.FreeLimitUsed;
        return response;
    }
}
