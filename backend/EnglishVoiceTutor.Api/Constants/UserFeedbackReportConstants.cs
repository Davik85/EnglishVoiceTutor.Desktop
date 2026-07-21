namespace EnglishVoiceTutor.Api.Constants;

public static class UserFeedbackReportConstants
{
    public const string SuggestionCategory = "suggestion";
    public const string AppIssueCategory = "app_issue";
    public const string AiResponseCategory = "ai_response";
    public const string AccountDeletionCategory = "account_deletion";

    public const string NewStatus = "new";
    public const string ReviewedStatus = "reviewed";
    public const string NeedsInformationStatus = "needs_information";
    public const string ProcessingStatus = "processing";
    public const string ResolvedStatus = "resolved";
    public const string RejectedStatus = "rejected";

    public static readonly HashSet<string> Categories =
    [SuggestionCategory, AppIssueCategory, AiResponseCategory, AccountDeletionCategory];

    public static readonly HashSet<string> Statuses =
    [NewStatus, ReviewedStatus, NeedsInformationStatus, ProcessingStatus, ResolvedStatus, RejectedStatus];

    public static readonly HashSet<string> ActiveAccountDeletionStatuses =
    [NewStatus, ReviewedStatus, NeedsInformationStatus, ProcessingStatus];
}
