using System.Net;
using System.Text;
using System.Text.Json;
using EnglishVoiceTutor.Api.Data;
using EnglishVoiceTutor.Api.Models;
using EnglishVoiceTutor.Api.Services;
using EnglishVoiceTutor.Api.Services.Auth;
using EnglishVoiceTutor.Api.Services.Cms;
using EnglishVoiceTutor.Api.Services.Usage;
using EnglishVoiceTutor.Desktop.Models.LessonContent;
using Microsoft.Extensions.Logging.Abstractions;

namespace EnglishVoiceTutor.Api.Tests.Services;

public sealed class AiTextModelRequestShapeTests
{
    [Theory]
    [InlineData("", "gpt-5.2", false, false, 0.3)]
    [InlineData("", "gpt-5.5", false, false, null)]
    [InlineData("", "gpt-5.6-terra", true, false, null)]
    [InlineData("", "gpt-5.2", true, false, null)]
    [InlineData("", "lesson-chat-distinct-model", true, false, null)]
    [InlineData("feedback", "gpt-5.2", false, false, 0.3)]
    [InlineData("feedback", "gpt-5.2", false, true, null)]
    [InlineData("feedback", "feedback-distinct-model", false, true, null)]
    public async Task LessonAndFeedbackRuntimeRequestsPreserveLegacyPolicyUnlessTheirOverrideIsEnabled(
        string requestPurpose,
        string modelId,
        bool lessonTutorOmitTemperature,
        bool feedbackOmitTemperature,
        double? expectedTemperature)
    {
        var settings = AiModelSettings.Defaults with
        {
            LessonTutorChatModel = string.Equals(requestPurpose, "feedback", StringComparison.OrdinalIgnoreCase)
                ? "lesson-chat-distinct-model"
                : modelId,
            FeedbackCorrectionModel = string.Equals(requestPurpose, "feedback", StringComparison.OrdinalIgnoreCase)
                ? modelId
                : "feedback-distinct-model",
            LessonTutorChatOmitTemperature = lessonTutorOmitTemperature,
            FeedbackCorrectionOmitTemperature = feedbackOmitTemperature
        };
        var capture = new CapturingHttpClientFactory(CreateOutputEnvelope(CreateLessonReplyJson()));
        var usage = new RecordingUsageEventService();
        var avatarProvider = new TutorAvatarProfileProvider(NullLogger<TutorAvatarProfileProvider>.Instance);
        var service = new OpenAiLessonChatService(
            CreateOptionsProvider(settings),
            new LessonPromptBuilder(avatarProvider),
            new TutorBehaviorProfileResolver(new StaticRuntimeService(), avatarProvider),
            new TutorIdentityGuard(NullLogger<TutorIdentityGuard>.Instance),
            capture,
            new FakeRequestUserResolver(),
            usage,
            NullLogger<OpenAiLessonChatService>.Instance);

        await service.CreateReplyAsync(new LessonChatRequest
        {
            SelectedLevel = "A1",
            TopicTitle = "Introductions",
            SubtopicTitle = "Greetings",
            UserMessage = "Hello.",
            TutorAvatarId = "lana",
            TutorDisplayName = "Lana",
            RequestPurpose = requestPurpose
        }, CancellationToken.None);

        AssertRequest(capture.RequestBodies.Single(), modelId, expectedTemperature);
        Assert.Equal(modelId, Assert.Single(usage.Records).Model);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task LessonHintRuntimeRequestKeepsItsExistingOmittedTemperatureShape(bool omitTemperature)
    {
        var settings = AiModelSettings.Defaults with
        {
            LessonTutorChatModel = "lesson-chat-distinct-model",
            LessonHintModel = "hint-distinct-model",
            LessonHintOmitTemperature = omitTemperature
        };
        var capture = new CapturingHttpClientFactory(CreateOutputEnvelope("{\"hintText\":\"Try a greeting.\"}"));
        var usage = new RecordingUsageEventService();
        var avatarProvider = new TutorAvatarProfileProvider(NullLogger<TutorAvatarProfileProvider>.Instance);
        var service = new OpenAiLessonHintService(
            CreateOptionsProvider(settings),
            new MockLessonHintService(),
            new LessonPromptBuilder(avatarProvider),
            new TutorBehaviorProfileResolver(new StaticRuntimeService(), avatarProvider),
            capture,
            new FakeRequestUserResolver(),
            usage);

        await service.CreateHintAsync(new LessonChatRequest
        {
            SelectedLevel = "A1",
            TopicTitle = "Introductions",
            SubtopicTitle = "Greetings",
            UserMessage = "Help me.",
            TutorAvatarId = "lana"
        }, CancellationToken.None);

        var body = capture.RequestBodies.Single();
        AssertTemperature(body, null);
        Assert.Equal("hint-distinct-model", ReadModel(body));
        Assert.Equal("hint-distinct-model", Assert.Single(usage.Records).Model);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task TranslationRuntimeRequestKeepsItsExistingOmittedTemperatureShape(bool omitTemperature)
    {
        var settings = AiModelSettings.Defaults with
        {
            LessonTutorChatModel = "lesson-chat-distinct-model",
            TranslationModel = "translation-distinct-model",
            TranslationOmitTemperature = omitTemperature
        };
        var capture = new CapturingHttpClientFactory(CreateOutputEnvelope("{\"translatedText\":\"Szia.\"}"));
        var usage = new RecordingUsageEventService();
        var service = new TranslationService(
            CreateOptionsProvider(settings),
            capture,
            new DevUserProvider(),
            usage);

        await service.TranslateAsync(new TranslationRequest
        {
            Text = "Hello.",
            SourceLanguageName = "English",
            TargetLanguage = "Hungarian"
        }, CancellationToken.None);

        var body = capture.RequestBodies.Single();
        AssertTemperature(body, null);
        Assert.Equal("translation-distinct-model", ReadModel(body));
        Assert.Equal("translation-distinct-model", Assert.Single(usage.Records).Model);
    }

    [Theory]
    [InlineData(false, false, 0.3, 0.3)]
    [InlineData(true, true, null, null)]
    public async Task ProviderAccessRequestsUseEachDraftRolesEffectiveTemperature(
        bool lessonTutorOmitTemperature,
        bool feedbackOmitTemperature,
        double? expectedLessonTutorTemperature,
        double? expectedFeedbackTemperature)
    {
        var draft = AiModelSettings.Defaults with
        {
            LessonTutorChatModel = "gpt-5.2-chat",
            FeedbackCorrectionModel = "gpt-5.2-feedback",
            LessonHintModel = "gpt-5.2-hint",
            TranslationModel = "gpt-5.2-translation",
            LessonTutorChatOmitTemperature = lessonTutorOmitTemperature,
            FeedbackCorrectionOmitTemperature = feedbackOmitTemperature,
            LessonHintOmitTemperature = lessonTutorOmitTemperature,
            TranslationOmitTemperature = feedbackOmitTemperature
        };
        var capture = new CapturingHttpClientFactory("{\"id\":\"provider-test\",\"output\":[]}");
        var service = new AiModelProviderAccessTestService(new FakeAiModelSettingsService(draft), capture);

        var result = await service.TestDraftAsync(draft, "test-api-key", CancellationToken.None);

        Assert.Equal("partial", result.OverallStatus);
        Assert.Equal(8, capture.RequestBodies.Count);
        AssertRequest(capture.RequestBodies[0], "gpt-5.2-chat", expectedLessonTutorTemperature);
        AssertRequest(capture.RequestBodies[1], "gpt-5.2-feedback", expectedFeedbackTemperature);
        AssertRequest(capture.RequestBodies[2], "gpt-5.2-hint", null);
        AssertRequest(capture.RequestBodies[3], "gpt-5.2-translation", null);
        AssertTemperature(capture.RequestBodies[4], null);
        AssertTemperature(capture.RequestBodies[5], expectedLessonTutorTemperature);
        AssertTemperature(capture.RequestBodies[6], null);
        AssertTemperature(capture.RequestBodies[7], expectedLessonTutorTemperature);
        Assert.DoesNotContain("Hello.", string.Join("\n", capture.RequestBodies), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("")]
    [InlineData("feedback")]
    public async Task LessonAndFeedbackProviderInputUsesPublishedTutorRatherThanClientName(string requestPurpose)
    {
        var capture = new CapturingHttpClientFactory(CreateOutputEnvelope(CreateLessonReplyJson()));
        var avatarProvider = new TutorAvatarProfileProvider(NullLogger<TutorAvatarProfileProvider>.Instance);
        var service = new OpenAiLessonChatService(
            CreateOptionsProvider(AiModelSettings.Defaults),
            new LessonPromptBuilder(avatarProvider),
            new TutorBehaviorProfileResolver(new PublishedRuntimeService(), avatarProvider),
            new TutorIdentityGuard(NullLogger<TutorIdentityGuard>.Instance),
            capture, new FakeRequestUserResolver(), new RecordingUsageEventService(),
            NullLogger<OpenAiLessonChatService>.Instance);

        await service.CreateReplyAsync(new LessonChatRequest
        {
            TutorAvatarId = "lana", TutorDisplayName = "Client Spoof",
            UserMessage = "Hello", SelectedLevel = "A1", RequestPurpose = requestPurpose
        }, TestContext.Current.CancellationToken);

        using var document = JsonDocument.Parse(Assert.Single(capture.RequestBodies));
        var input = document.RootElement.GetProperty("input").GetString()!;
        Assert.Contains("published-style-only", input);
        Assert.Contains("You are CmsLana.", input);
        Assert.DoesNotContain("Client Spoof", input);
    }

    [Fact]
    public async Task HintProviderInputUsesTheSamePublishedTutor()
    {
        var capture = new CapturingHttpClientFactory(CreateOutputEnvelope("{\"hintText\":\"Try a greeting.\"}"));
        var avatarProvider = new TutorAvatarProfileProvider(NullLogger<TutorAvatarProfileProvider>.Instance);
        var service = new OpenAiLessonHintService(
            CreateOptionsProvider(AiModelSettings.Defaults), new MockLessonHintService(),
            new LessonPromptBuilder(avatarProvider),
            new TutorBehaviorProfileResolver(new PublishedRuntimeService(), avatarProvider),
            capture, new FakeRequestUserResolver(), new RecordingUsageEventService());

        await service.CreateHintAsync(new LessonChatRequest
        {
            TutorAvatarId = "lana", TutorDisplayName = "Client Spoof",
            UserMessage = "Help", SelectedLevel = "A1"
        }, TestContext.Current.CancellationToken);

        using var document = JsonDocument.Parse(Assert.Single(capture.RequestBodies));
        var input = document.RootElement.GetProperty("input").GetString()!;
        Assert.Contains("published-style-only", input);
        Assert.Contains("You are CmsLana.", input);
        Assert.DoesNotContain("Client Spoof", input);
    }

    private static OpenAiOptionsProvider CreateOptionsProvider(AiModelSettings settings) =>
        new(new FakeAiModelSettingsService(settings), () => "test-api-key");

    private static string CreateLessonReplyJson() => JsonSerializer.Serialize(new LessonChatResponse
    {
        BotReply = "Hello!",
        Feedback = new FeedbackDto
        {
            ShortText = "Good.",
            CorrectedVersion = "Hello.",
            GrammarTip = "Use a greeting.",
            VocabularyTip = "Hello is common.",
            CultureTip = "A greeting is polite.",
            NaturalVersion = "Hello!"
        },
        IsLessonComplete = false
    }, new JsonSerializerOptions(JsonSerializerDefaults.Web));

    private static string CreateOutputEnvelope(string outputText) => JsonSerializer.Serialize(new
    {
        id = "response-id",
        output = new[]
        {
            new
            {
                content = new[] { new { text = outputText } }
            }
        }
    });

    private static void AssertRequest(string body, string expectedModel, double? expectedTemperature)
    {
        Assert.Equal(expectedModel, ReadModel(body));
        AssertTemperature(body, expectedTemperature);
    }

    private static string ReadModel(string body)
    {
        using var document = JsonDocument.Parse(body);
        return document.RootElement.GetProperty("model").GetString()!;
    }

    private static void AssertTemperature(string body, double? expectedTemperature)
    {
        using var document = JsonDocument.Parse(body);
        if (expectedTemperature.HasValue)
        {
            Assert.True(document.RootElement.TryGetProperty("temperature", out var temperature));
            Assert.Equal(expectedTemperature.Value, temperature.GetDouble());
            return;
        }

        Assert.False(document.RootElement.TryGetProperty("temperature", out _));
    }

    private sealed class CapturingHttpClientFactory(string responseBody) : IHttpClientFactory
    {
        private readonly CapturingHandler _handler = new(responseBody);
        public IReadOnlyList<string> RequestBodies => _handler.RequestBodies;
        public HttpClient CreateClient(string name) => new(_handler, disposeHandler: false);
    }

    private sealed class CapturingHandler(string responseBody) : HttpMessageHandler
    {
        public List<string> RequestBodies { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestBodies.Add(await request.Content!.ReadAsStringAsync(cancellationToken));
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(responseBody, Encoding.UTF8, "application/json")
            };
        }
    }

    private sealed class FakeAiModelSettingsService(AiModelSettings settings) : IAiModelSettingsService
    {
        public AiModelSettings GetActiveSettings() => settings;
        public Task<AiModelSettingsResponse> GetAsync(CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<AiModelSettingsResponse> SaveDraftAsync(AiModelSettings draft, string? updatedBy, CancellationToken cancellationToken) => throw new NotSupportedException();
        public AiModelSettingsValidationResponse Validate(AiModelSettings candidate) => new(true, [], []);
        public Task<AiModelSettingsResponse> PublishAsync(string? updatedBy, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<AiModelSettingsResponse> ResetDraftFromActiveAsync(string? updatedBy, CancellationToken cancellationToken) => throw new NotSupportedException();
    }

    private sealed class PublishedRuntimeService : ICmsRuntimeLessonContentService
    {
        public Task<CmsRuntimeLessonContentReadResult> ReadRuntimeLessonContentAsync(CancellationToken cancellationToken) =>
            Task.FromResult(new CmsRuntimeLessonContentReadResult
            {
                Success = true, Source = CmsContentConstants.Sources.CmsPublishedSnapshot,
                Content = new CmsRuntimeLessonContent
                {
                    TutorBehaviorProfiles = [new CmsPublishedTutorBehaviorProfile
                    {
                        TutorId = "lana", DisplayName = "CmsLana", IsActive = true,
                        TutorProfile = new TutorProfile
                        {
                            Id = "lana", DisplayName = "CmsLana", Age = 22,
                            HomeCity = "London", CountryOrRegion = "United Kingdom",
                            Studies = "fashion design", Hobbies = ["art"],
                            CommunicationStyle = ["published-style-only"],
                            SpeakingRules = new Dictionary<string, string>
                            {
                                ["a1"] = "short", ["a2"] = "short",
                                ["b1"] = "natural", ["b2"] = "nuanced"
                            },
                            IdentityRules = ["Always use this identity."]
                        }
                    }]
                }
            });
    }

    private sealed class StaticRuntimeService : ICmsRuntimeLessonContentService
    {
        public Task<CmsRuntimeLessonContentReadResult> ReadRuntimeLessonContentAsync(CancellationToken cancellationToken) =>
            Task.FromResult(new CmsRuntimeLessonContentReadResult { Success = false });
    }

    private sealed class FakeRequestUserResolver : IRequestUserResolver
    {
        public ResolvedRequestUser ResolveCurrentUser() => new(Guid.NewGuid(), RequestUserResolver.AuthenticatedSource);
    }

    private sealed class RecordingUsageEventService : IUsageEventService
    {
        public List<UsageEventRecord> Records { get; } = [];
        public Task TryRecordAsync(UsageEventRecord record, CancellationToken cancellationToken = default)
        {
            Records.Add(record);
            return Task.CompletedTask;
        }
    }
}
