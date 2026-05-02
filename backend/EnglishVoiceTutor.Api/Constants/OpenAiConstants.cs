namespace EnglishVoiceTutor.Api.Constants;

public static class OpenAiConstants
{
    public const string SectionName = "OpenAI";
    public const string ApiKeyEnvironmentVariableName = "OPENAI_API_KEY";
    public const string DefaultModel = "gpt-5.2";
    public const string ResponsesEndpoint = "https://api.openai.com/v1/responses";
    public const string NotConfiguredStatus = "not_configured";
    public const string ConfiguredStatus = "configured";
}
