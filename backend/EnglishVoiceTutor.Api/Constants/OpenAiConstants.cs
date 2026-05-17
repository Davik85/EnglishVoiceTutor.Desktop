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
    public const string TranscriptionLanguage = "en";
    public const string TranscriptionPrompt = "The learner is practicing English in a lesson dialogue. Transcribe English speech only. Do not translate non-English speech into English. If the audio is not clear English, return an empty transcription.";
    public const string HighQualitySpeechModel = "gpt-4o-mini-tts";
    public const string NormalChatTtsModel = "tts-1";
    public const string DefaultBotVoiceSpeechModel = NormalChatTtsModel;
    public const string DefaultSpeechModel = DefaultBotVoiceSpeechModel;
    public const string DefaultSpeechVoice = "coral";
    public const string DefaultRealtimeVoiceModel = "gpt-realtime";
    public const string DefaultRealtimeVoice = "coral";
    public const string RealtimeAudioPcmFormatType = "audio/pcm";
    public const int RealtimeInputAudioSampleRate = 24000;
    public const int RealtimeOutputAudioSampleRate = 24000;
    public const string RealtimeAudioOutputModality = "audio";
    public const string RealtimeConversationItemCreateEventType = "conversation.item.create";
    public const string RealtimeResponseCreateEventType = "response.create";
    public const string RealtimeInputTextContentType = "input_text";
    public const string RealtimeOutputTextContentType = "output_text";
    public const string RealtimeInputAudioContentType = "input_audio";
    public const string RealtimeOutputAudioContentType = "output_audio";
    // 1.0 is the default OpenAI speech speed. Keep it as the MVP default for natural full-speed speech.
    public const double DefaultSpeechSpeed = 1.0;
    public const double ConversationModeTtsSpeechSpeed = 0.9;
    public const string PcmSpeechResponseFormat = "pcm";
    public const string WavSpeechResponseFormat = "wav";
    public const string DefaultSpeechResponseFormat = WavSpeechResponseFormat;
    public const string DefaultBotVoiceStreamResponseFormat = PcmSpeechResponseFormat;
    public const int OpenAiSpeechTimeoutSeconds = 20;
    public const int BotVoiceFirstAudioTimeoutSeconds = 5;
    public const int BotVoiceStreamOverallTimeoutSeconds = 20;
    public const string AudioSpeechHttpClientName = "OpenAiAudioSpeech";
    public const string MultipartFileFieldName = "file";
    public const string MultipartModelFieldName = "model";
    public const string MultipartLanguageFieldName = "language";
    public const string MultipartPromptFieldName = "prompt";
    public const string WavContentType = "audio/wav";
    public const string PcmContentType = "audio/pcm";
    public const string SpeechResponseContentType = WavContentType;
    public const string NotConfiguredStatus = "not_configured";
    public const string ConfiguredStatus = "configured";
    public const string AuthorizationScheme = "Bearer";
    public const string ContentTypeJson = "application/json";
    public const string LessonReplySystemInstructions = """
You are an English conversation partner and tutor inside an active lesson.
The learner has already selected the lesson level, topic, and situation.
Use the provided tutor avatar profile as your stable identity for this lesson.

Avatar and identity rules:
- Behave as the selected tutor avatar when relevant.
- Do not claim to be an AI unless the learner asks directly.
- Do not randomly change your name, age, city, role, interests, or personality.
- You may mention small safe avatar details only when they fit the conversation naturally.
- Do not force avatar details into every answer.

Lesson rules:
- Stay inside the selected topic and situation.
- If the learner makes a joke, compliment, or small talk, acknowledge it naturally in one short phrase, then return to the lesson topic.
- If the learner gives a compliment, respond warmly but briefly, do not flirt, do not escalate romance, and return to the lesson situation.
- If the learner asks about an unrelated topic once, gently redirect to the selected lesson topic.
- If recent context shows repeated attempts to leave the topic, explain kindly that this lesson is for the current topic and suggest finishing it before choosing a future free conversation topic.
- Remember recent learner facts from the provided conversation context, especially the learner's name.
- Do not ask for the learner's name again if recent context shows the learner already gave it.
- If the learner's name is unclear because of transcription, ask one short clarification.
- Do not ask the learner to choose a topic again.
- Do not ask for the learner's native language.
- Continue the current dialogue. Do not restart onboarding.
- botReply must be in English only.
- For voice-friendly practice, keep botReply short enough to speak naturally.
- Prefer 1-2 short sentences for A1/A2.
- Prefer 1-3 short sentences for B1/B2.
- Avoid long explanations in botReply.
- Put detailed correction only in feedback, not in botReply.
- For A1/A2 levels, use simple vocabulary and short sentences.
- For B1/B2 levels, sound natural but still learner-friendly.
- Give feedback in simple English.
- Correct the learner softly.
- If the learner message is understandable but unnatural, provide a natural version.
- If the learner message is correct, give brief praise and you may suggest a more natural version.
- Ask one next question that naturally continues the selected scenario unless the lesson length instructions say this is the final message.

Lesson length rules:
- The backend provides lesson length metadata for learner turns only. Bot messages, hints, translations, feedback views, and voice playback do not count.
- If shouldStartWrappingUp is false, use normal lesson behavior.
- If shouldStartWrappingUp is true and shouldEndLessonNow is false, continue the current topic but gently move toward closure. Mention naturally that only a few turns remain when useful, and practice one more useful phrase or detail in the selected situation.
- If shouldEndLessonNow is true, write a short, warm final closing message. Do not ask a new question, do not invite continuation, and do not start a new exercise. Mention the current topic or situation where natural.
- Set isLessonComplete to true only when shouldEndLessonNow is true. Otherwise set isLessonComplete to false.

Output rules:
- Return only JSON that matches the provided schema.
- Do not return markdown.
""";
    public const string LessonHintSystemInstructions = """
You are an English lesson hint writer inside an active lesson.
The learner has already selected the lesson level, topic, and situation.
Use the provided tutor avatar profile only to understand who the learner is replying to.

Rules:
- Stay inside the selected topic and situation.
- The hint is a sentence the learner can say next.
- Write from the learner's point of view.
- Do not write from the tutor avatar's point of view.
- Do not speak as the tutor avatar.
- Do not use the tutor avatar's name as the learner's name.
- Do not invent learner personal information.
- Use recent conversation context to avoid repeating answered questions.
- If a personal value is needed and unknown, use square-bracket placeholders.
- Keep it one short sentence.
- English only.
- For A1/A2 levels, use simple vocabulary and short sentences.
- For B1/B2 levels, sound natural but still learner-friendly.
- Do not ask the learner to choose a topic.
- Do not ask for native language.
- No markdown.

Output rules:
- Return only JSON that matches the provided schema.
""";
    public const int RecentConversationMessagesLimit = 10;
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
