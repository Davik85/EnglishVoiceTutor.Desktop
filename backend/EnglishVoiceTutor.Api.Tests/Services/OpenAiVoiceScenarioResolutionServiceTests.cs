using System.Text.Json;
using EnglishVoiceTutor.Api.Models;
using EnglishVoiceTutor.Api.Services;

namespace EnglishVoiceTutor.Api.Tests.Services;

public sealed class OpenAiVoiceScenarioResolutionServiceTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static TheoryData<string, string, string, string?> DynamicFixtures()
    {
        var data = new TheoryData<string, string, string, string?>();
        AddGroup(data, "introductions", "Meeting a new neighbor", "neighbor", "Meeting a friend in a park");
        AddGroup(data, "transport", "Reporting delayed luggage", "delayed luggage", "Renting a bicycle by the river");
        AddGroup(data, "workplace", "Presenting a weekly update", "weekly update", "Requesting a different office chair");
        AddGroup(data, "service", "Returning a damaged purchase", "damaged purchase", "Booking a haircut for Saturday");
        AddGroup(data, "non-english", "Comprar un billete de tren", "billete tren", "Pedir una cita médica mañana");
        return data;
    }

    public static TheoryData<VoiceScenarioResolutionResponse> ContradictoryResponses()
    {
        var data = new TheoryData<VoiceScenarioResolutionResponse>();
        data.Add(new VoiceScenarioResolutionResponse
        {
            Decision = "published_context", MatchedContextId = "context-a", Confidence = .9,
            CandidateContextIds = [], NormalizedFreeContext = "A different safe situation"
        });
        data.Add(new VoiceScenarioResolutionResponse
        {
            Decision = "free_context", Confidence = .8, CandidateContextIds = [],
            NormalizedFreeContext = "A different safe situation", ClarificationText = "Which one?"
        });
        data.Add(new VoiceScenarioResolutionResponse
        {
            Decision = "clarify", Confidence = .5, CandidateContextIds = ["context-a"],
            ClarificationText = "Which situation?"
        });
        data.Add(new VoiceScenarioResolutionResponse
        {
            Decision = "unsafe", Confidence = .9, CandidateContextIds = [],
            NormalizedFreeContext = "A selected situation"
        });
        return data;
    }

    [Theory]
    [MemberData(nameof(DynamicFixtures))]
    public void UsesDynamicCandidateContractAcrossUnrelatedFixtures(
        string group, string recognizedText, string decision, string? matchedId)
    {
        var candidates = Candidates(group);
        var output = new VoiceScenarioResolutionResponse
        {
            Decision = decision,
            MatchedContextId = matchedId,
            Confidence = .88,
            CandidateContextIds = decision == "clarify" ? [candidates[0].Id, candidates[1].Id] : [],
            NormalizedFreeContext = decision == "free_context" ? recognizedText : null,
            ClarificationText = decision == "clarify" ? "Please choose the closest situation." : null
        };

        var result = Parse(output, candidates.Select(candidate => candidate.Id));
        var providerRequest = OpenAiVoiceScenarioResolutionService.CreateProviderRequest(
            Request(group, recognizedText, candidates), "mocked-model");
        var suppliedRequest = JsonSerializer.Deserialize<VoiceScenarioResolutionRequest>(providerRequest.Input, JsonOptions);

        Assert.Equal(decision, result.Decision);
        Assert.Equal(matchedId, result.MatchedContextId);
        Assert.NotNull(suppliedRequest);
        Assert.Equal(recognizedText, suppliedRequest!.RecognizedText);
        Assert.All(candidates, candidate => Assert.Contains(suppliedRequest.Candidates, supplied => supplied.Id == candidate.Id));
    }

    [Fact]
    public void PublishedContextWithValidAllowedIdSucceeds()
    {
        var result = Parse(Published("context-a"), AllowedIds());

        Assert.Equal("published_context", result.Decision);
        Assert.Equal("context-a", result.MatchedContextId);
    }

    [Fact]
    public void PublishedContextWithUnknownIdFails()
    {
        Assert.Throws<InvalidDataException>(() => Parse(Published("unknown-context"), AllowedIds()));
    }

    [Fact]
    public void FreeContextWithValidNormalizedContextSucceeds()
    {
        var result = Parse(Free("Ordering a replacement for a damaged item"), AllowedIds());

        Assert.Equal("free_context", result.Decision);
        Assert.Equal("Ordering a replacement for a damaged item", result.NormalizedFreeContext);
    }

    [Fact]
    public void FreeContextWithoutRequiredSafeContextFails()
    {
        Assert.Throws<InvalidDataException>(() => Parse(Free("   "), AllowedIds()));
    }

    [Fact]
    public void ClarifyWithRequiredClarificationDataSucceeds()
    {
        var result = Parse(Clarify(), AllowedIds());

        Assert.Equal("clarify", result.Decision);
        Assert.Equal(["context-a", "context-b"], result.CandidateContextIds);
        Assert.False(string.IsNullOrWhiteSpace(result.ClarificationText));
    }

    [Fact]
    public void ClarifyCannotCarryPublishedMatch()
    {
        var response = new VoiceScenarioResolutionResponse
        {
            Decision = "clarify",
            MatchedContextId = "context-a",
            Confidence = .48,
            CandidateContextIds = ["context-a", "context-b"],
            ClarificationText = "Which of these two situations did you mean?"
        };

        Assert.Throws<InvalidDataException>(() => Parse(response, AllowedIds()));
    }

    [Fact]
    public void UnsafeWithEmptyNonSelectionShapeSucceeds()
    {
        var result = Parse(Unsafe(), AllowedIds());

        Assert.Equal("unsafe", result.Decision);
        Assert.Null(result.MatchedContextId);
        Assert.Empty(result.CandidateContextIds);
        Assert.Null(result.NormalizedFreeContext);
        Assert.Null(result.ClarificationText);
    }

    [Fact]
    public void UnsafeWithSelectionDataFails()
    {
        var response = new VoiceScenarioResolutionResponse
        {
            Decision = "unsafe",
            MatchedContextId = "context-a",
            Confidence = .97,
            CandidateContextIds = []
        };

        Assert.Throws<InvalidDataException>(() => Parse(response, AllowedIds()));
    }

    [Fact]
    public void StructuredOutputSchemaBranchesMatchValidationContract()
    {
        var request = OpenAiVoiceScenarioResolutionService.CreateProviderRequest(
            Request("schema", "choose a situation", Candidates("schema")), "mocked-model");
        var schema = request.Text!.Format!.Schema;
        var branches = schema.GetProperty("properties").GetProperty("result").GetProperty("anyOf").EnumerateArray().ToArray();

        Assert.Equal(["result"], schema.GetProperty("required").EnumerateArray().Select(item => item.GetString()));
        Assert.Equal(4, branches.Length);
        Assert.Equal(
            ["published_context", "free_context", "clarify", "unsafe"],
            branches.Select(Decision));
        Assert.All(branches, branch =>
        {
            Assert.False(branch.GetProperty("additionalProperties").GetBoolean());
            Assert.Equal(6, branch.GetProperty("required").GetArrayLength());
        });

        Assert.Equal(0, Properties(branches[0]).GetProperty("candidateContextIds").GetProperty("maxItems").GetInt32());
        Assert.Equal("null", Properties(branches[0]).GetProperty("normalizedFreeContext").GetProperty("type").GetString());
        Assert.Equal("string", Properties(branches[1]).GetProperty("normalizedFreeContext").GetProperty("type").GetString());
        Assert.Equal(2, Properties(branches[2]).GetProperty("candidateContextIds").GetProperty("anyOf")[1].GetProperty("minItems").GetInt32());
        Assert.Equal("string", Properties(branches[2]).GetProperty("clarificationText").GetProperty("type").GetString());
        Assert.Equal("null", Properties(branches[3]).GetProperty("clarificationText").GetProperty("type").GetString());

        Parse(Published("context-a"), AllowedIds());
        Parse(Free("A safe normalized context"), AllowedIds());
        Parse(Clarify(), AllowedIds());
        Parse(Unsafe(), AllowedIds());
    }

    [Theory]
    [MemberData(nameof(ContradictoryResponses))]
    public void ContradictoryDecisionShapesFail(VoiceScenarioResolutionResponse response)
    {
        Assert.Throws<InvalidDataException>(() => Parse(response, AllowedIds()));
    }

    [Fact]
    public void MalformedSerializedProviderResponseFails()
    {
        Assert.Throws<InvalidDataException>(() =>
            OpenAiVoiceScenarioResolutionService.DeserializeAndValidateResponse(
                "{\"result\":{\"decision\":", AllowedIds()));
    }

    [Fact]
    public void NullCandidateListFailsAsInvalidProviderData()
    {
        const string output = """
        {"result":{"decision":"unsafe","matchedContextId":null,"confidence":0.2,"candidateContextIds":null,"normalizedFreeContext":null,"clarificationText":null}}
        """;

        Assert.Throws<InvalidDataException>(() =>
            OpenAiVoiceScenarioResolutionService.DeserializeAndValidateResponse(output, AllowedIds()));
    }

    [Fact]
    public void ProductionResolverContainsNoConcreteFixturePhrases()
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "EnglishVoiceTutor.Api"));
        var source = File.ReadAllText(Path.Combine(root, "Services", "OpenAiVoiceScenarioResolutionService.cs"));
        foreach (var phrase in new[] { "language school", "meeting neighbor", "meeting a friend in a park", "hobby club", "doctor", "manager" })
            Assert.DoesNotContain(phrase, source, StringComparison.OrdinalIgnoreCase);
    }

    private static void AddGroup(TheoryData<string, string, string, string?> data, string group, string title, string partial, string free)
    {
        var id = $"{group}-a";
        data.Add(group, title, "published_context", id);
        data.Add(group, partial, "published_context", id);
        data.Add(group, $"missing {string.Join(' ', title.Split(' ').Skip(1))}", "published_context", id);
        data.Add(group, title.Replace(title.Split(' ').Last(), "misheard"), "published_context", id);
        data.Add(group, string.Join(' ', title.Split(' ').Reverse()), "published_context", id);
        data.Add(group, $"me {partial} please", "published_context", id);
        data.Add(group, free, "free_context", null);
        data.Add(group, "practice something", "clarify", null);
        data.Add(group, "the common option", "clarify", null);
    }

    private static VoiceScenarioResolutionResponse Parse(
        VoiceScenarioResolutionResponse response,
        IEnumerable<string> allowedIds) =>
        OpenAiVoiceScenarioResolutionService.DeserializeAndValidateResponse(
            JsonSerializer.Serialize(new { result = response }, JsonOptions),
            allowedIds.ToHashSet(StringComparer.Ordinal));

    private static VoiceScenarioResolutionResponse Published(string matchedId) => new()
    {
        Decision = "published_context",
        MatchedContextId = matchedId,
        Confidence = .92,
        CandidateContextIds = [],
        NormalizedFreeContext = null,
        ClarificationText = null
    };

    private static VoiceScenarioResolutionResponse Free(string normalizedContext) => new()
    {
        Decision = "free_context",
        MatchedContextId = null,
        Confidence = .83,
        CandidateContextIds = [],
        NormalizedFreeContext = normalizedContext,
        ClarificationText = null
    };

    private static VoiceScenarioResolutionResponse Clarify() => new()
    {
        Decision = "clarify",
        MatchedContextId = null,
        Confidence = .48,
        CandidateContextIds = ["context-a", "context-b"],
        NormalizedFreeContext = null,
        ClarificationText = "Which of these two situations did you mean?"
    };

    private static VoiceScenarioResolutionResponse Unsafe() => new()
    {
        Decision = "unsafe",
        MatchedContextId = null,
        Confidence = .97,
        CandidateContextIds = [],
        NormalizedFreeContext = null,
        ClarificationText = null
    };

    private static HashSet<string> AllowedIds() => ["context-a", "context-b"];

    private static string Decision(JsonElement branch) =>
        Properties(branch).GetProperty("decision").GetProperty("enum")[0].GetString()!;

    private static JsonElement Properties(JsonElement branch) => branch.GetProperty("properties");

    private static List<VoiceScenarioCandidate> Candidates(string group)
    {
        var titles = group switch
        {
            "introductions" => ("Meeting a new neighbor", "Meeting someone at a hobby club"),
            "transport" => ("Reporting delayed luggage", "Changing a reserved seat"),
            "workplace" => ("Presenting a weekly update", "Discussing a project deadline"),
            "service" => ("Returning a damaged purchase", "Asking about product availability"),
            "non-english" => ("Comprar un billete de tren", "Preguntar por el andén"),
            _ => ("Primary supplied situation", "Alternative supplied situation")
        };
        return
        [
            new() { Id = $"{group}-a", Title = titles.Item1, Description = "First learner-facing description" },
            new() { Id = $"{group}-b", Title = titles.Item2, Description = "Second learner-facing description" }
        ];
    }

    private static VoiceScenarioResolutionRequest Request(string group, string text, IReadOnlyList<VoiceScenarioCandidate> candidates) => new()
    {
        StudyLanguage = group == "non-english" ? "Spanish" : "English",
        LearnerLevel = "A2",
        TopicId = $"topic-{group}",
        SubtopicId = $"subtopic-{group}",
        RuntimeScenarioId = $"scenario-{group}",
        RuntimeVersion = 4,
        RecognizedText = text,
        IsInitialScenarioSelectionTurn = true,
        Candidates = candidates
    };
}
