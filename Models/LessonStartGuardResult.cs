namespace EnglishVoiceTutor.Desktop.Models;

public sealed record LessonStartGuardResult(
    bool ShouldAllowStart,
    bool IsBackendDecisionAvailable,
    string Source,
    string Decision,
    string Reason,
    bool EnforcementEnabled,
    bool? CanStartNewLesson,
    bool? FreeLessonUsedToday,
    int? FreeLessonRemainingToday);
