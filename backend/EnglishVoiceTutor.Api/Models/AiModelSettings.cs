using EnglishVoiceTutor.Api.Constants;

namespace EnglishVoiceTutor.Api.Models;

public sealed record AiModelSettings(
    string LessonTutorChatModel,
    string FeedbackCorrectionModel,
    string LessonHintModel,
    string TranslationModel,
    string SpeechToTextModel,
    string LessonChatTextToSpeechModel,
    string ConversationModeTextToSpeechModel,
    string RealtimeVoiceModel)
{
    public static AiModelSettings Defaults { get; } = new(
        OpenAiConstants.DefaultModel,
        OpenAiConstants.DefaultModel,
        OpenAiConstants.DefaultModel,
        OpenAiConstants.DefaultModel,
        OpenAiConstants.DefaultTranscriptionModel,
        OpenAiConstants.NormalChatTtsModel,
        OpenAiConstants.ConversationModeTtsModel,
        OpenAiConstants.DefaultRealtimeVoiceModel);
}

public sealed record AiModelSettingsDocument(
    AiModelSettings Active,
    AiModelSettings Draft,
    DateTimeOffset UpdatedAtUtc,
    string? UpdatedBy,
    int Revision);

public sealed record AiModelSettingsResponse(
    AiModelSettings Active,
    AiModelSettings Draft,
    DateTimeOffset UpdatedAtUtc,
    string? UpdatedBy,
    int Revision,
    IReadOnlyList<string> Warnings);

public sealed record AiModelSettingsValidationResponse(bool IsValid, IReadOnlyList<string> Errors, IReadOnlyList<string> Warnings);
