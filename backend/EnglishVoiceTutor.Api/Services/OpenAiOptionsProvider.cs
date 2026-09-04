using EnglishVoiceTutor.Api.Constants;
using EnglishVoiceTutor.Api.Models;

namespace EnglishVoiceTutor.Api.Services;

public sealed class OpenAiOptionsProvider
{
    private readonly IAiModelSettingsService _aiModelSettingsService;
    private readonly Func<string> _apiKeyProvider;

    public OpenAiOptionsProvider(IAiModelSettingsService aiModelSettingsService)
        : this(
            aiModelSettingsService,
            () => Environment.GetEnvironmentVariable(OpenAiConstants.ApiKeyEnvironmentVariableName) ?? string.Empty)
    {
    }

    internal OpenAiOptionsProvider(IAiModelSettingsService aiModelSettingsService, Func<string> apiKeyProvider)
    {
        _aiModelSettingsService = aiModelSettingsService;
        _apiKeyProvider = apiKeyProvider;
    }

    public OpenAiOptions GetOptions()
    {
        var settings = _aiModelSettingsService.GetActiveSettings();

        return new OpenAiOptions
        {
            ApiKey = _apiKeyProvider(),
            Model = settings.LessonTutorChatModel,
            LessonTutorChatOmitTemperature = settings.LessonTutorChatOmitTemperature,
            FeedbackCorrectionOmitTemperature = settings.FeedbackCorrectionOmitTemperature,
            LessonHintOmitTemperature = settings.LessonHintOmitTemperature,
            TranslationOmitTemperature = settings.TranslationOmitTemperature
        };
    }
}
