namespace EnglishVoiceTutor.Api.Options;

public sealed class AiModelSettingsOptions
{
    public const string SectionName = "AiModelSettings";

    public string StorageJsonPath { get; set; } = "site/content/ai-model-settings.json";
}
