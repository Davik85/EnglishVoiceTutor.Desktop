namespace EnglishVoiceTutor.Api.Data;

public static class CmsContentConstants
{
    public static class StaticImport
    {
        public const string ContentPackSlug = "static-json-v1";
        public const string ContentPackName = "Static JSON Baseline";
        public const string ContentPackDescription = "Imported baseline from packaged static lesson, prompt, and tutor content.";
        public const string BaseStaticContentVersion = "static-json-v1";
        public const string ImportReason = "Step 5D-2 static JSON CMS import foundation.";
        public const string ContentRootFolder = "Content";
        public const string LessonsFolder = "Lessons";
        public const string PromptsFolder = "Prompts";
        public const string TutorsFolder = "Tutors";
        public const string StudyLanguagesFolder = "StudyLanguages";
        public const string StudyLanguagesFileName = "study_languages.json";
        public const string LessonTutorBasePromptFileName = "lesson_tutor_base_prompt.txt";
        public const string LessonSetupRulesPromptFileName = "lesson_setup_rules.txt";
        public const string LessonResponseRulesPromptFileName = "lesson_response_rules.txt";
    }

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
        public const string ImportCreated = "ImportCreated";
        public const string ImportUpdated = "ImportUpdated";
        public const string ImportSkipped = "ImportSkipped";
        public const string ImportPublished = "ImportPublished";
        public const string ValidationRun = "ValidationRun";
        public const string Published = "Published";
        public const string RollbackPublished = "RollbackPublished";
        public const string DraftDiscarded = "DraftDiscarded";
    }
}
