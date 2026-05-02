namespace EnglishVoiceTutor.Api.Constants;

public static class OpenAiConstants
{
    public const string SectionName = "OpenAI";
    public const string ApiKeyEnvironmentVariableName = "OPENAI_API_KEY";
    public const string DefaultModel = "gpt-5.2";
    public const string ResponsesEndpoint = "https://api.openai.com/v1/responses";
    public const string NotConfiguredStatus = "not_configured";
    public const string ConfiguredStatus = "configured";
    public const string AuthorizationScheme = "Bearer";
    public const string ContentTypeJson = "application/json";
    public const string ResponseOutputTextPropertyName = "output_text";
    public const string LessonReplySystemInstructions = "You are a friendly English speaking tutor. Keep replies short and gently correct the learner. Return only JSON without markdown. The JSON must include: botReply and feedback with shortText, correctedVersion, grammarTip, vocabularyTip, cultureTip, naturalVersion.";
    public const string LessonReplyFallbackText = "I understood your answer. Let's continue practicing.";
}
