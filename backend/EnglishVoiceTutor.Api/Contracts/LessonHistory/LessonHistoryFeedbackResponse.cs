namespace EnglishVoiceTutor.Api.Contracts.LessonHistory;

public sealed record LessonHistoryFeedbackResponse(
    Guid Id,
    Guid SessionId,
    Guid MessageId,
    string FeedbackType,
    string? CorrectedText,
    string? Explanation,
    string? GrammarTip,
    string? VocabularyTip,
    string? CultureTip,
    string? Praise,
    DateTimeOffset CreatedAt);
