using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using EnglishVoiceTutor.Api.Constants;
using EnglishVoiceTutor.Api.Models;

namespace EnglishVoiceTutor.Api.Services;

public sealed class OpenAiLessonChatService : ILessonChatService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly OpenAiOptionsProvider _optionsProvider;
    private readonly MockLessonChatService _mockLessonChatService;
    private readonly LessonPromptBuilder _lessonPromptBuilder;
    private readonly IHttpClientFactory _httpClientFactory;

    public OpenAiLessonChatService(
        OpenAiOptionsProvider optionsProvider,
        MockLessonChatService mockLessonChatService,
        LessonPromptBuilder lessonPromptBuilder,
        IHttpClientFactory httpClientFactory)
    {
        _optionsProvider = optionsProvider;
        _mockLessonChatService = mockLessonChatService;
        _lessonPromptBuilder = lessonPromptBuilder;
        _httpClientFactory = httpClientFactory;
    }

    public async Task<LessonChatResponse> CreateReplyAsync(
        LessonChatRequest request,
        CancellationToken cancellationToken = default)
    {
        var options = _optionsProvider.GetOptions();

        if (string.IsNullOrWhiteSpace(options.ApiKey))
        {
            return await _mockLessonChatService.CreateReplyAsync(request, cancellationToken);
        }

        try
        {
            var openAiResponse = await SendResponsesApiRequestAsync(request, options, cancellationToken);

            var lessonReply = JsonSerializer.Deserialize<LessonChatResponse>(openAiResponse.OutputText, JsonOptions);

            if (!IsValidLessonReply(lessonReply))
            {
                return await _mockLessonChatService.CreateReplyAsync(request, cancellationToken);
            }

            return lessonReply!;
        }
        catch
        {
            return await _mockLessonChatService.CreateReplyAsync(request, cancellationToken);
        }
    }

    private async Task<OpenAiResponsesResponse> SendResponsesApiRequestAsync(
        LessonChatRequest request,
        OpenAiOptions options,
        CancellationToken cancellationToken)
    {
        var apiRequest = new OpenAiResponsesRequest
        {
            Model = options.Model,
            Instructions = OpenAiConstants.LessonReplySystemInstructions,
            Input = _lessonPromptBuilder.BuildInput(request)
        };

        var httpClient = _httpClientFactory.CreateClient();

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, OpenAiConstants.ResponsesEndpoint);
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue(OpenAiConstants.AuthorizationScheme, options.ApiKey);

        var requestJson = JsonSerializer.Serialize(apiRequest, JsonOptions);
        httpRequest.Content = new StringContent(requestJson, Encoding.UTF8, OpenAiConstants.ContentTypeJson);

        using var response = await httpClient.SendAsync(httpRequest, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException("OpenAI request failed.");
        }

        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
        var parsedResponse = JsonSerializer.Deserialize<OpenAiResponsesResponse>(responseJson, JsonOptions);

        if (parsedResponse is null || string.IsNullOrWhiteSpace(parsedResponse.OutputText))
        {
            throw new InvalidOperationException("OpenAI response does not contain output text.");
        }

        return parsedResponse;
    }

    private static bool IsValidLessonReply(LessonChatResponse? reply)
    {
        if (reply is null)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(reply.BotReply))
        {
            return false;
        }

        if (reply.Feedback is null)
        {
            return false;
        }

        return true;
    }
}
