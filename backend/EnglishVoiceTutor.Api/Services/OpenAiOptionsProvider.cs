using EnglishVoiceTutor.Api.Constants;
using EnglishVoiceTutor.Api.Models;

namespace EnglishVoiceTutor.Api.Services;

public sealed class OpenAiOptionsProvider
{
    private const string ModelKey = "Model";
    private readonly IConfiguration _configuration;

    public OpenAiOptionsProvider(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public OpenAiOptions GetOptions()
    {
        var apiKey = Environment.GetEnvironmentVariable(OpenAiConstants.ApiKeyEnvironmentVariableName) ?? string.Empty;

        var configuredModel = _configuration
            .GetSection(OpenAiConstants.SectionName)
            .GetValue<string>(ModelKey);

        var model = string.IsNullOrWhiteSpace(configuredModel)
            ? OpenAiConstants.DefaultModel
            : configuredModel;

        return new OpenAiOptions
        {
            ApiKey = apiKey,
            Model = model
        };
    }
}
