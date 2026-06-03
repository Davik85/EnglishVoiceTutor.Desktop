namespace EnglishVoiceTutor.Api.Data;

public static class CmsContentConstants
{
    public static class ContentPackStatuses
    {
        public const string Draft = "Draft";
        public const string Published = "Published";
        public const string Archived = "Archived";
    }

    public static class ContentVersionPublishStatuses
    {
        public const string Draft = "Draft";
        public const string Published = "Published";
        public const string Superseded = "Superseded";
        public const string RolledBack = "RolledBack";
    }

    public static class PromptTemplateKeys
    {
        public const string LessonTutorBase = "lesson_tutor_base";
        public const string LessonSetupRules = "lesson_setup_rules";
        public const string LessonResponseRules = "lesson_response_rules";
        public const string Hint = "hint";
        public const string Feedback = "feedback";
        public const string Summary = "summary";
    }

    public static class ContentAuditActions
    {
        public const string DraftCreated = "DraftCreated";
        public const string DraftUpdated = "DraftUpdated";
        public const string ValidationRun = "ValidationRun";
        public const string Published = "Published";
        public const string RollbackPublished = "RollbackPublished";
        public const string DraftDiscarded = "DraftDiscarded";
    }
}
