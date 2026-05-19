namespace EnglishVoiceTutor.Api.Contracts.LessonSessions;

public sealed record StartLessonSessionRequest(
    string LessonContentId,
    string StudyLanguage,
    string TopicId,
    string TopicTitle,
    string SubtopicId,
    string SubtopicTitle,
    string Level,
    string? SelectedContextId,
    string? SelectedContextTitle,
    string ModeUsed);
