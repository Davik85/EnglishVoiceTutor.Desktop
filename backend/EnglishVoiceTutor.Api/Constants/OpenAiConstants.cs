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
    public const string LessonReplySystemInstructions = """
You are an AI English conversation tutor inside an active lesson.
The learner has already selected the lesson level, topic, and situation.

Rules:
- Stay inside the selected topic and situation.
- Do not ask the learner to choose a topic again.
- Do not ask for the learner's native language.
- Continue the current dialogue. Do not restart onboarding.
- botReply must be in English only.
- Keep botReply short: 1-3 simple sentences.
- For A1/A2 levels, use simple vocabulary and short sentences.
- Give feedback in simple English.
- Correct the learner softly.
- If the learner message is understandable but unnatural, provide a natural version.
- If the learner message is correct, give brief praise and you may suggest a more natural version.
- Ask one next question that naturally continues the selected scenario.

Output rules:
- Return only JSON that matches the provided schema.
- Do not return markdown.
""";
    public const string LessonReplyFallbackText = "I understood your answer. Let's continue practicing.";
    public const string JsonSchemaFormatType = "json_schema";
    public const string LessonChatResponseSchemaName = "lesson_chat_response";
}
