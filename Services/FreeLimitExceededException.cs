namespace EnglishVoiceTutor.Desktop.Services;

public sealed class FreeLimitExceededException : Exception
{
    public FreeLimitExceededException(
        string operation,
        string limitType,
        int used,
        int limit,
        int remaining,
        string studyLanguage,
        string userFacingMessage)
        : base(userFacingMessage)
    {
        Operation = operation;
        LimitType = limitType;
        Used = used;
        Limit = limit;
        Remaining = remaining;
        StudyLanguage = studyLanguage;
        UserFacingMessage = userFacingMessage;
    }

    public string Operation { get; }
    public string LimitType { get; }
    public int Used { get; }
    public int Limit { get; }
    public int Remaining { get; }
    public string StudyLanguage { get; }
    public string UserFacingMessage { get; }
}
