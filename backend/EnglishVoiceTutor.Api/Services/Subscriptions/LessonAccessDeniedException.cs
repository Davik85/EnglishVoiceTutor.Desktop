namespace EnglishVoiceTutor.Api.Services.Subscriptions;

public sealed class LessonAccessDeniedException : Exception
{
    public LessonAccessDeniedException(
        string decision,
        string reason,
        bool enforcementEnabled,
        bool freeLessonUsedToday,
        int freeLessonRemainingToday,
        bool premiumActive,
        bool trialActive)
        : base("Lesson start is not allowed for this account at the moment.")
    {
        Decision = decision;
        Reason = reason;
        EnforcementEnabled = enforcementEnabled;
        FreeLessonUsedToday = freeLessonUsedToday;
        FreeLessonRemainingToday = freeLessonRemainingToday;
        PremiumActive = premiumActive;
        TrialActive = trialActive;
    }

    public string Decision { get; }
    public string Reason { get; }
    public bool EnforcementEnabled { get; }
    public bool FreeLessonUsedToday { get; }
    public int FreeLessonRemainingToday { get; }
    public bool PremiumActive { get; }
    public bool TrialActive { get; }
}
