using System.Text.Json.Serialization;

namespace EnglishVoiceTutor.Desktop.Models;

public sealed record BackendLessonAccessDeniedResponse
{
    [JsonPropertyName("error")]
    public string Error { get; init; } = string.Empty;

    [JsonPropertyName("decision")]
    public string Decision { get; init; } = string.Empty;

    [JsonPropertyName("reason")]
    public string Reason { get; init; } = string.Empty;

    [JsonPropertyName("enforcementEnabled")]
    public bool EnforcementEnabled { get; init; }

    [JsonPropertyName("freeLessonUsedToday")]
    public bool FreeLessonUsedToday { get; init; }

    [JsonPropertyName("freeLessonRemainingToday")]
    public int FreeLessonRemainingToday { get; init; }

    [JsonPropertyName("premiumActive")]
    public bool PremiumActive { get; init; }

    [JsonPropertyName("trialActive")]
    public bool TrialActive { get; init; }
}
