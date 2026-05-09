namespace EnglishVoiceTutor.Api.Constants;

public static class OpenAiConstants
{
    public const string SectionName = "OpenAI";
    public const string ApiKeyEnvironmentVariableName = "OPENAI_API_KEY";
    public const string DefaultModel = "gpt-5.2";
    public const string ResponsesEndpoint = "https://api.openai.com/v1/responses";
    public const string AudioTranscriptionsEndpoint = "https://api.openai.com/v1/audio/transcriptions";
    public const string AudioSpeechEndpoint = "https://api.openai.com/v1/audio/speech";
    public const string DefaultTranscriptionModel = "gpt-4o-mini-transcribe";
    public const string DefaultSpeechModel = "gpt-4o-mini-tts";
    public const string DefaultSpeechVoice = "coral";
    public const string MultipartFileFieldName = "file";
    public const string MultipartModelFieldName = "model";
    public const string WavContentType = "audio/wav";
    public const string SpeechResponseContentType = "audio/mpeg";
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
    public const string LessonHintSystemInstructions = """
You are an AI English conversation tutor inside an active lesson.
The learner has already selected the lesson level, topic, and situation.

Rules:
- Stay inside the selected topic and situation.
- The hint is a sentence the learner can say next.
- Write from the learner's point of view.
- Do not write from the bot's point of view.
- Do not use the bot's name as the learner's name.
- Do not invent personal information.
- If a personal value is needed and unknown, use square-bracket placeholders.
- Keep it one short sentence.
- English only.
- For A1/A2 levels, use simple vocabulary and short sentences.
- Do not ask the learner to choose a topic.
- Do not ask for native language.
- No markdown.

Output rules:
- Return only JSON that matches the provided schema.
""";
    public const string LessonReplyFallbackText = "I understood your answer. Let's continue practicing.";
    public const string JsonSchemaFormatType = "json_schema";
    public const string LessonChatResponseSchemaName = "lesson_chat_response";
    public const string LessonHintResponseSchemaName = "lesson_hint_response";
    public const string TranslationResponseSchemaName = "translation_response";
    public const string TranslationSystemInstructions = """
Translate the provided English text into the requested target language.

Rules:
- Preserve meaning.
- Keep tone natural and learner-friendly.
- Do not add explanations.
- Return only JSON matching the schema.
- No markdown.
""";
}
