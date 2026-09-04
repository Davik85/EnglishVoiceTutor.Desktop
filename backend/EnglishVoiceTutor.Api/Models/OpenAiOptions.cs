using EnglishVoiceTutor.Api.Constants;

namespace EnglishVoiceTutor.Api.Models;

public sealed class OpenAiOptions
{
    public string ApiKey { get; init; } = string.Empty;
    public string Model { get; init; } = OpenAiConstants.DefaultModel;
    public string LessonTutorChatModel { get; init; } = OpenAiConstants.DefaultModel;
    public string FeedbackCorrectionModel { get; init; } = OpenAiConstants.DefaultModel;
    public string LessonHintModel { get; init; } = OpenAiConstants.DefaultModel;
    public string TranslationModel { get; init; } = OpenAiConstants.DefaultModel;
    public bool LessonTutorChatOmitTemperature { get; init; }
    public bool FeedbackCorrectionOmitTemperature { get; init; }
    public bool LessonHintOmitTemperature { get; init; }
    public bool TranslationOmitTemperature { get; init; }
}
