using EnglishVoiceTutor.Api.Constants;
using EnglishVoiceTutor.Api.Models;

namespace EnglishVoiceTutor.Api.Services;

public sealed class OpenAiOptionsProvider
{
    private readonly IAiModelSettingsService _aiModelSettingsService;

    public OpenAiOptionsProvider(IAiModelSettingsService aiModelSettingsService)
    {
        _aiModelSettingsService = aiModelSettingsService;
    }

    public OpenAiOptions GetOptions()
    {
        var apiKey = Environment.GetEnvironmentVariable(OpenAiConstants.ApiKeyEnvironmentVariableName) ?? string.Empty;

        var model = _aiModelSettingsService.GetActiveSettings().LessonTutorChatModel;

        return new OpenAiOptions
        {
            ApiKey = apiKey,
            Model = model
        };
    }
}
