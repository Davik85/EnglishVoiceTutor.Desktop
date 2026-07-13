using System.Net;
using System.Text;
using System.Text.Json;
using EnglishVoiceTutor.Api.Constants;
using EnglishVoiceTutor.Api.Models;
using EnglishVoiceTutor.Api.Services;

namespace EnglishVoiceTutor.Api.Tests.Services;

public sealed class OpenAiVoiceScenarioResolutionServiceTests : IDisposable
{
    private readonly string? _originalApiKey = Environment.GetEnvironmentVariable(OpenAiConstants.ApiKeyEnvironmentVariableName);

    public OpenAiVoiceScenarioResolutionServiceTests() =>
        Environment.SetEnvironmentVariable(OpenAiConstants.ApiKeyEnvironmentVariableName, "test-voice-resolution-key");

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

    [Theory]
    [MemberData(nameof(DynamicFixtures))]
    public async Task UsesDynamicCandidateContractAcrossUnrelatedFixtures(
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
        var factory = new CapturingHttpClientFactory(Envelope(output));
        var service = new OpenAiVoiceScenarioResolutionService(
            new OpenAiOptionsProvider(new FakeAiModelSettingsService()), factory);

        var result = await service.ResolveAsync(Request(group, recognizedText, candidates), TestContext.Current.CancellationToken);

        Assert.Equal(decision, result.Decision);
        Assert.Equal(matchedId, result.MatchedContextId);
        Assert.NotNull(factory.ProviderRequestBody);
        var providerRequest = JsonSerializer.Deserialize<OpenAiResponsesRequest>(factory.ProviderRequestBody!, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        Assert.NotNull(providerRequest);
        var suppliedRequest = JsonSerializer.Deserialize<VoiceScenarioResolutionRequest>(providerRequest!.Input, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        Assert.NotNull(suppliedRequest);
        Assert.Equal(recognizedText, suppliedRequest!.RecognizedText);
        Assert.All(candidates, candidate => Assert.Contains(suppliedRequest.Candidates, supplied => supplied.Id == candidate.Id));
    }

    [Fact]
    public async Task RejectsContextIdOutsideSuppliedFiniteList()
    {
        var candidates = Candidates("safety");
        var output = new VoiceScenarioResolutionResponse
        {
            Decision = "published_context",
            MatchedContextId = "invented-id",
            Confidence = .99
        };
        var service = new OpenAiVoiceScenarioResolutionService(
            new OpenAiOptionsProvider(new FakeAiModelSettingsService()),
            new CapturingHttpClientFactory(Envelope(output)));

        await Assert.ThrowsAsync<InvalidDataException>(() =>
            service.ResolveAsync(Request("safety", "first choice", candidates), TestContext.Current.CancellationToken));
    }

    [Fact]
    public void ProductionResolverContainsNoConcreteFixturePhrases()
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "EnglishVoiceTutor.Api"));
        var source = File.ReadAllText(Path.Combine(root, "Services", "OpenAiVoiceScenarioResolutionService.cs"));
        foreach (var phrase in new[] { "language school", "meeting neighbor", "meeting a friend in a park", "hobby club", "doctor", "manager" })
            Assert.DoesNotContain(phrase, source, StringComparison.OrdinalIgnoreCase);
    }

    public void Dispose() => Environment.SetEnvironmentVariable(OpenAiConstants.ApiKeyEnvironmentVariableName, _originalApiKey);

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

    private static string Envelope(VoiceScenarioResolutionResponse response)
    {
        var output = JsonSerializer.Serialize(response, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        return "{\"id\":\"response\",\"output\":[{\"type\":\"message\",\"role\":\"assistant\",\"content\":[{\"type\":\"output_text\",\"text\":" + JsonSerializer.Serialize(output) + "}]}]}";
    }

    private sealed class CapturingHttpClientFactory(string envelope) : IHttpClientFactory
    {
        public string? ProviderRequestBody { get; private set; }
        public HttpClient CreateClient(string name) => new(new Handler(envelope, body => ProviderRequestBody = body));
    }

    private sealed class Handler(string envelope, Action<string> capture) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            capture(await request.Content!.ReadAsStringAsync(cancellationToken));
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(envelope, Encoding.UTF8, "application/json") };
        }
    }

    private sealed class FakeAiModelSettingsService : IAiModelSettingsService
    {
        public AiModelSettings GetActiveSettings() => AiModelSettings.Defaults;
        public Task<AiModelSettingsResponse> GetAsync(CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<AiModelSettingsResponse> SaveDraftAsync(AiModelSettings draft, string? updatedBy, CancellationToken cancellationToken) => throw new NotSupportedException();
        public AiModelSettingsValidationResponse Validate(AiModelSettings settings) => throw new NotSupportedException();
        public Task<AiModelSettingsResponse> PublishAsync(string? updatedBy, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<AiModelSettingsResponse> ResetDraftFromActiveAsync(string? updatedBy, CancellationToken cancellationToken) => throw new NotSupportedException();
    }
}
