using EnglishVoiceTutor.Api.Constants;

namespace EnglishVoiceTutor.Api.Contracts.Subscription;

public sealed class LessonAccessDecisionResponse
{
    public Guid UserId { get; set; }
    public bool CanStartNewLesson { get; set; }
    public bool PremiumActive { get; set; }
    public bool TrialActive { get; set; }
    public bool FreeLessonUsedToday { get; set; }
    public int FreeLessonRemainingToday { get; set; }
    public bool EnforcementEnabled { get; set; } = SubscriptionConstants.EnforcementEnabled;
    public string Decision { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public DateTimeOffset CheckedAtUtc { get; set; }
}
