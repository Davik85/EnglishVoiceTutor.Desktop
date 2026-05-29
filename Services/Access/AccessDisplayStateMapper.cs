using EnglishVoiceTutor.Desktop.Models;
using EnglishVoiceTutor.Desktop.Models.Access;

namespace EnglishVoiceTutor.Desktop.Services.Access;

public static class AccessDisplayStateMapper
{
    private const string LessonAccessDeniedErrorCode = "lesson_access_denied";
    private const string BlockedFreeLimitUsedDecision = "blocked_free_limit_used";
    private const string PastDueSubscriptionStatus = "past_due";
    private const string CanceledSubscriptionStatus = "canceled";
    private const string PausedSubscriptionStatus = "paused";
    private const string SignedOutMessage = "Sign in or create an account to start lessons and save your progress.";
    private const string FreeAllowanceAvailableMessage = "You have a free lesson available today.";
    private const string FreeAllowanceUsedMessage = "You have used today's free lesson. Upgrade options will be available soon, or you can come back tomorrow.";
    private const string TrialActiveMessage = "Your trial is active.";
    private const string PremiumActiveMessage = "Premium is active.";
    private const string PastDueMessage = "There may be a payment issue. Access follows your current account status.";
    private const string CanceledOrPausedMessage = "Your subscription is not active. Access follows your current account status.";
    private const string CheckoutUnavailableMessage = "Upgrade options are not available right now. Please try again later.";
    private const string UnknownOrErrorMessage = "We could not check your access right now. Please try again.";

    public static AccessDisplayModel MapSignedOut()
    {
        return new AccessDisplayModel(
            AccessDisplayState.SignedOut,
            SignedOutMessage,
            CanStartNewLesson: false,
            IsBackendDriven: false);
    }

    public static AccessDisplayModel MapUnknownOrError()
    {
        return new AccessDisplayModel(
            AccessDisplayState.UnknownOrError,
            UnknownOrErrorMessage,
            CanStartNewLesson: null,
            IsBackendDriven: false);
    }

    public static AccessDisplayModel MapCheckoutUnavailable()
    {
        return new AccessDisplayModel(
            AccessDisplayState.CheckoutUnavailable,
            CheckoutUnavailableMessage,
            CanStartNewLesson: null,
            IsBackendDriven: true);
    }

    public static AccessDisplayModel Map(
        bool isSignedIn,
        BackendLessonAccessDecisionResponse? lessonAccess,
        BackendSubscriptionStatusResponse? subscriptionStatus = null)
    {
        if (!isSignedIn)
        {
            return MapSignedOut();
        }

        if (lessonAccess is null && subscriptionStatus is null)
        {
            return MapUnknownOrError();
        }

        var premiumActive = lessonAccess?.PremiumActive ?? subscriptionStatus?.PremiumActive ?? false;
        var trialActive = lessonAccess?.TrialActive ?? subscriptionStatus?.TrialActive ?? false;
        var canStartNewLesson = lessonAccess?.CanStartNewLesson;
        var freeLessonRemainingToday = lessonAccess?.FreeLessonRemainingToday ?? subscriptionStatus?.FreeLessonRemainingToday;
        var freeLessonUsedToday = lessonAccess?.FreeLessonUsedToday ?? subscriptionStatus?.FreeLessonUsedToday;

        if (premiumActive)
        {
            return CreateBackendDriven(AccessDisplayState.PremiumActive, PremiumActiveMessage, canStartNewLesson);
        }

        if (trialActive)
        {
            return CreateBackendDriven(AccessDisplayState.TrialActive, TrialActiveMessage, canStartNewLesson);
        }

        if (IsPastDue(subscriptionStatus?.SubscriptionStatus))
        {
            return CreateBackendDriven(AccessDisplayState.PastDue, PastDueMessage, canStartNewLesson);
        }

        if (IsCanceledOrPaused(subscriptionStatus?.SubscriptionStatus))
        {
            return CreateBackendDriven(AccessDisplayState.CanceledOrPaused, CanceledOrPausedMessage, canStartNewLesson);
        }

        if (freeLessonRemainingToday > 0)
        {
            return CreateBackendDriven(AccessDisplayState.FreeAllowanceAvailable, FreeAllowanceAvailableMessage, canStartNewLesson);
        }

        if (lessonAccess?.CanStartNewLesson == false || freeLessonUsedToday == true || IsFreeAllowanceUsedDecision(lessonAccess?.Decision))
        {
            return CreateBackendDriven(AccessDisplayState.FreeAllowanceUsed, FreeAllowanceUsedMessage, canStartNewLesson);
        }

        return MapUnknownOrError();
    }

    public static AccessDisplayModel MapLessonAccessDenied(BackendLessonAccessDeniedResponse deniedResponse)
    {
        ArgumentNullException.ThrowIfNull(deniedResponse);

        if (deniedResponse.PremiumActive)
        {
            return CreateBackendDriven(AccessDisplayState.PremiumActive, PremiumActiveMessage, canStartNewLesson: false);
        }

        if (deniedResponse.TrialActive)
        {
            return CreateBackendDriven(AccessDisplayState.TrialActive, TrialActiveMessage, canStartNewLesson: false);
        }

        if (string.Equals(deniedResponse.Error, LessonAccessDeniedErrorCode, StringComparison.OrdinalIgnoreCase)
            || IsFreeAllowanceUsedDecision(deniedResponse.Decision)
            || deniedResponse.FreeLessonRemainingToday <= 0
            || deniedResponse.FreeLessonUsedToday)
        {
            return CreateBackendDriven(AccessDisplayState.FreeAllowanceUsed, FreeAllowanceUsedMessage, canStartNewLesson: false);
        }

        return MapUnknownOrError();
    }

    private static AccessDisplayModel CreateBackendDriven(AccessDisplayState state, string message, bool? canStartNewLesson)
    {
        return new AccessDisplayModel(
            state,
            message,
            canStartNewLesson,
            IsBackendDriven: true);
    }

    private static bool IsPastDue(string? subscriptionStatus)
    {
        return string.Equals(subscriptionStatus, PastDueSubscriptionStatus, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsCanceledOrPaused(string? subscriptionStatus)
    {
        return string.Equals(subscriptionStatus, CanceledSubscriptionStatus, StringComparison.OrdinalIgnoreCase)
            || string.Equals(subscriptionStatus, PausedSubscriptionStatus, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsFreeAllowanceUsedDecision(string? decision)
    {
        return string.Equals(decision, BlockedFreeLimitUsedDecision, StringComparison.OrdinalIgnoreCase);
    }
}
