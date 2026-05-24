namespace EnglishVoiceTutor.Desktop.Models;

public sealed class BackendLessonAccessDecisionResponse
{
    public Guid UserId { get; init; }
    public bool CanStartNewLesson { get; init; }
    public bool PremiumActive { get; init; }
    public bool TrialActive { get; init; }
    public bool FreeLessonUsedToday { get; init; }
    public int FreeLessonRemainingToday { get; init; }
    public bool EnforcementEnabled { get; init; }
    public string Decision { get; init; } = string.Empty;
    public string Reason { get; init; } = string.Empty;
    public string Source { get; init; } = string.Empty;
    public DateTimeOffset CheckedAtUtc { get; init; }
}
