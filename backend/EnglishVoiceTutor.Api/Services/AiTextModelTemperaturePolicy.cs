namespace EnglishVoiceTutor.Api.Services;

internal enum AiTextModelRole
{
    LessonTutorChat,
    FeedbackCorrection,
    LessonHint,
    Translation
}

internal static class AiTextModelTemperaturePolicy
{
    public static double? Resolve(AiTextModelRole role, string modelId, bool omitTemperature)
    {
        if (omitTemperature)
        {
            return null;
        }

        return role switch
        {
            AiTextModelRole.LessonTutorChat or AiTextModelRole.FeedbackCorrection =>
                IsGpt55Model(modelId) ? null : 0.3,
            AiTextModelRole.LessonHint or AiTextModelRole.Translation => null,
            _ => throw new ArgumentOutOfRangeException(nameof(role), role, "Unknown AI text model role.")
        };
    }

    private static bool IsGpt55Model(string modelId) =>
        modelId.Trim().StartsWith("gpt-5.5", StringComparison.OrdinalIgnoreCase);
}
