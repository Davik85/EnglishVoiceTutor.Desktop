using System.Net;
using System.Text;
using EnglishVoiceTutor.Api.Constants;
using EnglishVoiceTutor.Api.Models;
using EnglishVoiceTutor.Api.Services;
using EnglishVoiceTutor.Api.Services.Auth;
using EnglishVoiceTutor.Api.Services.Usage;
using EnglishVoiceTutor.Shared.StudyLanguages;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;

namespace EnglishVoiceTutor.Api.Tests.Services;

public sealed class AudioTranscriptionServiceRequestTests
{
    [Theory]
    [InlineData("gpt-transcribe", "languages[]", "language")]
    [InlineData("gpt-4o-mini-transcribe", "language", "languages[]")]
    public async Task SelectedModelUsesItsMultipartLanguageFieldAndPreservesResponseAndUsage(
        string model, string expectedLanguageField, string absentLanguageField)
    {
        var settings = AiModelSettings.Defaults with { SpeechToTextModel = model };
        var modelSettings = new FakeAiModelSettingsService(settings);
        var capture = new CapturingHttpClientFactory();
        var usage = new RecordingUsageEventService();
        var service = new AudioTranscriptionService(
            new OpenAiOptionsProvider(modelSettings, () => "test-api-key"),
            capture,
            new FakeRequestUserResolver(),
            usage,
            modelSettings,
            NullLogger<AudioTranscriptionService>.Instance);
        await using var wavStream = new MemoryStream(new byte[48]);
        var audioFile = new FormFile(wavStream, 0, wavStream.Length, "audioFile", "sample.wav")
        {
            Headers = new HeaderDictionary(),
            ContentType = OpenAiConstants.WavContentType
        };

        var result = await service.TranscribeAsync(audioFile, StudyLanguageCatalog.GetById("fr"),
            "Prefer the phrase rendez-vous.", TestContext.Current.CancellationToken);

        Assert.Equal("Bonjour.", result.Text);
        Assert.Equal(HttpMethod.Post, capture.Method);
        Assert.Equal(OpenAiConstants.AudioTranscriptionsEndpoint, capture.RequestUri?.ToString());
        Assert.Equal(["file", "model", expectedLanguageField, "prompt"],
            capture.Parts.Select(part => part.Name));
        var filePart = Assert.Single(capture.Parts, part => part.Name == "file");
        Assert.Equal("sample.wav", filePart.FileName);
        Assert.Equal(OpenAiConstants.WavContentType, filePart.ContentType);
        Assert.Equal(48, filePart.FileBytes?.Length);
        Assert.Equal(model, Assert.Single(capture.Parts, part => part.Name == "model").Text);
        Assert.Equal("fr", Assert.Single(capture.Parts, part => part.Name == expectedLanguageField).Text);
        Assert.DoesNotContain(capture.Parts, part => part.Name == absentLanguageField);
        Assert.Equal("The learner is practicing French. Prefer the phrase rendez-vous.",
            Assert.Single(capture.Parts, part => part.Name == "prompt").Text);

        var recorded = Assert.Single(usage.Records);
        Assert.Equal(UsageConstants.Operations.AudioTranscription, recorded.Operation);
        Assert.Equal(UsageConstants.Statuses.Success, recorded.Status);
        Assert.Equal(model, recorded.Model);
        Assert.Equal("fr", recorded.StudyLanguage);
        Assert.Equal("Bonjour.".Length, recorded.InputCharacters);
    }

    private sealed record CapturedPart(
        string Name, string? Text, string? FileName, string? ContentType, byte[]? FileBytes);

    private sealed class CapturingHttpClientFactory : IHttpClientFactory
    {
        private readonly CapturingHandler _handler = new();
        public HttpMethod? Method => _handler.Method;
        public Uri? RequestUri => _handler.RequestUri;
        public IReadOnlyList<CapturedPart> Parts => _handler.Parts;
        public HttpClient CreateClient(string name) => new(_handler, disposeHandler: false);
    }

    private sealed class CapturingHandler : HttpMessageHandler
    {
        public HttpMethod? Method { get; private set; }
        public Uri? RequestUri { get; private set; }
        public IReadOnlyList<CapturedPart> Parts { get; private set; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Method = request.Method;
            RequestUri = request.RequestUri;
            var multipart = Assert.IsType<MultipartFormDataContent>(request.Content);
            var parts = new List<CapturedPart>();
            foreach (var content in multipart)
            {
                var disposition = content.Headers.ContentDisposition!;
                var name = disposition.Name!.Trim('"');
                var fileName = disposition.FileName?.Trim('"');
                parts.Add(new CapturedPart(
                    name,
                    fileName is null ? await content.ReadAsStringAsync(cancellationToken) : null,
                    fileName,
                    content.Headers.ContentType?.MediaType,
                    fileName is null ? null : await content.ReadAsByteArrayAsync(cancellationToken)));
            }

            Parts = parts;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"text\":\"  Bonjour.  \"}", Encoding.UTF8, "application/json")
            };
        }
    }

    private sealed class FakeAiModelSettingsService(AiModelSettings settings) : IAiModelSettingsService
    {
        public AiModelSettings GetActiveSettings() => settings;
        public Task<AiModelSettingsResponse> GetAsync(CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<AiModelSettingsResponse> SaveDraftAsync(AiModelSettings draft, string? updatedBy, CancellationToken cancellationToken) => throw new NotSupportedException();
        public AiModelSettingsValidationResponse Validate(AiModelSettings candidate) => throw new NotSupportedException();
        public Task<AiModelSettingsResponse> PublishAsync(string? updatedBy, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<AiModelSettingsResponse> ResetDraftFromActiveAsync(string? updatedBy, CancellationToken cancellationToken) => throw new NotSupportedException();
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

    private sealed class FakeRequestUserResolver : IRequestUserResolver
    {
        public ResolvedRequestUser ResolveCurrentUser() =>
            new(Guid.Parse("55b1c643-1cce-499a-9e83-556bc18d1b37"), RequestUserResolver.AuthenticatedSource);
    }
}
