using System.Text.Json.Serialization;

namespace EnglishVoiceTutor.Api.Models;

public sealed class VoiceScenarioResolutionRequest
{
    public string StudyLanguage { get; init; } = string.Empty;
    public string LearnerLevel { get; init; } = string.Empty;
    public string TopicId { get; init; } = string.Empty;
    public string SubtopicId { get; init; } = string.Empty;
    public string RuntimeScenarioId { get; init; } = string.Empty;
    public int? RuntimeVersion { get; init; }
    public string RecognizedText { get; init; } = string.Empty;
    public bool IsInitialScenarioSelectionTurn { get; init; }
    public IReadOnlyList<VoiceScenarioCandidate> Candidates { get; init; } = [];
}

public sealed class VoiceScenarioCandidate
{
    public string Id { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
}

public sealed class VoiceScenarioResolutionResponse
{
    [JsonPropertyName("decision")]
    public string Decision { get; init; } = string.Empty;

    [JsonPropertyName("matchedContextId")]
    public string? MatchedContextId { get; init; }

    [JsonPropertyName("confidence")]
    public double Confidence { get; init; }

    [JsonPropertyName("candidateContextIds")]
    public IReadOnlyList<string> CandidateContextIds { get; init; } = [];

    [JsonPropertyName("normalizedFreeContext")]
    public string? NormalizedFreeContext { get; init; }

    [JsonPropertyName("clarificationText")]
    public string? ClarificationText { get; init; }
}
