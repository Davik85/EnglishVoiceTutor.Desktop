using System.Net;
using EnglishVoiceTutor.Api.Constants;
using EnglishVoiceTutor.Api.Models;
using EnglishVoiceTutor.Api.Services;
using Microsoft.AspNetCore.Server.Kestrel.Core;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpClient();
builder.Services.AddHttpClient(OpenAiConstants.AudioSpeechHttpClientName, httpClient =>
{
    httpClient.Timeout = Timeout.InfiniteTimeSpan;
});
builder.Services.AddScoped<MockLessonChatService>();
builder.Services.AddScoped<MockLessonHintService>();
builder.Services.AddScoped<OpenAiOptionsProvider>();
builder.Services.AddScoped<TutorAvatarProfileProvider>();
builder.Services.AddScoped<LessonPromptBuilder>();
builder.Services.AddScoped<ILessonChatService, OpenAiLessonChatService>();
builder.Services.AddScoped<ILessonHintService, OpenAiLessonHintService>();
builder.Services.AddScoped<AudioTranscriptionService>();
builder.Services.AddScoped<TranslationService>();
builder.Services.AddScoped<AudioSpeechService>();

var app = builder.Build();

app.MapGet(ApiConstants.HealthRoute, CreateHealthResponse);
app.MapGet(ApiConstants.ApiHealthRoute, CreateHealthResponse);

app.MapGet(ApiConstants.BackendConfigStatusRoute, (OpenAiOptionsProvider optionsProvider) =>
{
    var options = optionsProvider.GetOptions();

    var openAiStatus = string.IsNullOrWhiteSpace(options.ApiKey)
        ? OpenAiConstants.NotConfiguredStatus
        : OpenAiConstants.ConfiguredStatus;

    return Results.Ok(new BackendConfigStatusResponse
    {
        OpenAiStatus = openAiStatus,
        OpenAiModel = options.Model
    });
});

app.MapPost(ApiConstants.LessonChatReplyRoute, HandleLessonChatReplyAsync);
app.MapPost(ApiConstants.LessonChatMockReplyRoute, HandleMockLessonChatReplyAsync);
app.MapPost(ApiConstants.LessonChatHintRoute, HandleLessonChatHintAsync);
app.MapPost(ApiConstants.AudioTranscriptionRoute, HandleAudioTranscriptionAsync);
app.MapPost(ApiConstants.TranslationRoute, HandleTranslationAsync);
app.MapPost(ApiConstants.AudioSpeechRoute, HandleAudioSpeechAsync);

app.Run();

static IResult CreateHealthResponse()
{
    return Results.Ok(new
    {
        status = ApiConstants.HealthOkStatus,
        service = ApiConstants.ServiceName
    });
}

static async Task<IResult> HandleLessonChatReplyAsync(
    LessonChatRequest request,
    ILessonChatService lessonChatService,
    CancellationToken cancellationToken)
{
    if (string.IsNullOrWhiteSpace(request.UserMessage))
    {
        return Results.BadRequest(new
        {
            error = ApiConstants.EmptyUserMessageError
        });
    }

    var response = await lessonChatService.CreateReplyAsync(request, cancellationToken);

    return Results.Ok(response);
}

static async Task<IResult> HandleMockLessonChatReplyAsync(
    LessonChatRequest request,
    MockLessonChatService mockLessonChatService,
    CancellationToken cancellationToken)
{
    if (string.IsNullOrWhiteSpace(request.UserMessage))
    {
        return Results.BadRequest(new
        {
            error = ApiConstants.EmptyUserMessageError
        });
    }

    var response = await mockLessonChatService.CreateReplyAsync(request, cancellationToken);

    return Results.Ok(response);
}

static async Task<IResult> HandleLessonChatHintAsync(
    LessonChatRequest request,
    ILessonHintService lessonHintService,
    CancellationToken cancellationToken)
{
    if (string.IsNullOrWhiteSpace(request.UserMessage))
    {
        return Results.BadRequest(new
        {
            error = ApiConstants.EmptyUserMessageError
        });
    }

    var response = await lessonHintService.CreateHintAsync(request, cancellationToken);

    return Results.Ok(response);
}

static async Task<IResult> HandleAudioTranscriptionAsync(
    HttpRequest request,
    AudioTranscriptionService audioTranscriptionService,
    ILoggerFactory loggerFactory,
    CancellationToken cancellationToken)
{
    if (!request.HasFormContentType)
    {
        return Results.BadRequest(new
        {
            error = ApiConstants.EmptyAudioFileError
        });
    }

    var logger = loggerFactory.CreateLogger("AudioTranscriptionEndpoint");

    try
    {
        logger.LogInformation("Starting audio transcription form read.");

        var form = await request.ReadFormAsync(cancellationToken);
        var audioFile = form.Files.GetFile(OpenAiConstants.MultipartFileFieldName);

        logger.LogInformation(
            "Audio transcription form read completed. FileName={FileName}; FileLength={FileLength}.",
            audioFile?.FileName ?? "<missing>",
            audioFile?.Length ?? 0);

        if (audioFile is null || audioFile.Length <= 0)
        {
            return Results.BadRequest(new
            {
                error = ApiConstants.EmptyAudioFileError
            });
        }

        var response = await audioTranscriptionService.TranscribeAsync(audioFile, cancellationToken);
        var transcriptionLength = response.Text?.Length ?? 0;

        logger.LogInformation("Audio transcription completed successfully. TranscriptionLength={TranscriptionLength}.", transcriptionLength);

        return Results.Ok(response);
    }
    catch (BadHttpRequestException exception)
    {
        var isBodyReadTimeout = IsRequestBodyReadTimeout(exception);
        var statusCode = isBodyReadTimeout
            ? StatusCodes.Status408RequestTimeout
            : StatusCodes.Status400BadRequest;

        logger.LogWarning(
            exception,
            "Audio transcription upload failed while reading the request body. StatusCode={StatusCode}; IsBodyReadTimeout={IsBodyReadTimeout}.",
            statusCode,
            isBodyReadTimeout);

        return Results.Problem(
            title: isBodyReadTimeout ? ApiConstants.AudioUploadTimedOutTitle : ApiConstants.AudioUploadFailedTitle,
            detail: isBodyReadTimeout ? ApiConstants.AudioUploadTimedOutDetail : ApiConstants.AudioUploadFailedDetail,
            statusCode: statusCode);
    }
    catch (OperationCanceledException exception) when (cancellationToken.IsCancellationRequested)
    {
        logger.LogInformation(exception, "Audio transcription upload was canceled by the client while reading or processing audio.");

        return Results.Problem(
            title: ApiConstants.AudioUploadCanceledTitle,
            detail: ApiConstants.AudioUploadCanceledDetail,
            statusCode: 499);
    }
    catch (IOException exception)
    {
        logger.LogWarning(exception, "Audio transcription upload failed because the request body could not be read.");

        return Results.Problem(
            title: ApiConstants.AudioUploadFailedTitle,
            detail: ApiConstants.AudioUploadFailedDetail,
            statusCode: StatusCodes.Status400BadRequest);
    }
    catch (InvalidOperationException exception)
    {
        logger.LogWarning(exception, "Audio transcription request failed during form validation or transcription processing.");

        return Results.Problem(
            title: ApiConstants.AudioUploadFailedTitle,
            detail: ApiConstants.AudioTranscriptionError,
            statusCode: StatusCodes.Status400BadRequest);
    }
    catch (Exception exception)
    {
        logger.LogError(exception, "Unexpected audio transcription request failure.");
        return Results.Problem(ApiConstants.AudioTranscriptionError);
    }
}

static bool IsRequestBodyReadTimeout(BadHttpRequestException exception)
{
    return exception.Message.Contains("MinRequestBodyDataRate", StringComparison.OrdinalIgnoreCase)
        || (exception.Message.Contains("request body", StringComparison.OrdinalIgnoreCase)
            && exception.Message.Contains("timed out", StringComparison.OrdinalIgnoreCase));
}

static async Task<IResult> HandleTranslationAsync(
    TranslationRequest request,
    TranslationService translationService,
    CancellationToken cancellationToken)
{
    if (string.IsNullOrWhiteSpace(request.Text))
    {
        return Results.BadRequest(new
        {
            error = ApiConstants.EmptyTranslationTextError
        });
    }

    if (string.IsNullOrWhiteSpace(request.TargetLanguage))
    {
        return Results.BadRequest(new
        {
            error = ApiConstants.EmptyTargetLanguageError
        });
    }

    try
    {
        var response = await translationService.TranslateAsync(request, cancellationToken);

        return Results.Ok(response);
    }
    catch (Exception)
    {
        return Results.Problem(ApiConstants.TranslationError);
    }
}

static async Task<IResult> HandleAudioSpeechAsync(
    AudioSpeechRequest request,
    AudioSpeechService audioSpeechService,
    ILoggerFactory loggerFactory,
    CancellationToken cancellationToken)
{
    if (string.IsNullOrWhiteSpace(request.Text))
    {
        return Results.BadRequest(new
        {
            error = ApiConstants.EmptySpeechTextError
        });
    }

    var logger = loggerFactory.CreateLogger("AudioSpeechEndpoint");

    try
    {
        var audioBytes = await audioSpeechService.CreateSpeechAsync(request.Text, cancellationToken);

        return Results.File(audioBytes, OpenAiConstants.SpeechResponseContentType);
    }
    catch (AudioSpeechRequestCanceledException exception) when (exception.InternalTimeoutReached)
    {
        logger.LogWarning(
            exception,
            "Audio speech request timed out after {TimeoutSeconds} seconds. ClientCancellationRequested={ClientCancellationRequested}.",
            OpenAiConstants.OpenAiSpeechTimeoutSeconds,
            exception.ClientCancellationRequested);

        return Results.Problem(
            title: "Audio speech request timed out.",
            detail: $"OpenAI speech generation exceeded the {OpenAiConstants.OpenAiSpeechTimeoutSeconds} second timeout.",
            statusCode: StatusCodes.Status504GatewayTimeout);
    }
    catch (AudioSpeechRequestCanceledException exception) when (exception.ClientCancellationRequested || cancellationToken.IsCancellationRequested)
    {
        logger.LogInformation(exception, "Audio speech request was canceled because the client aborted the request.");
        return Results.Problem(
            title: "Client closed request.",
            detail: "The client canceled the audio speech request before the backend could finish writing the response.",
            statusCode: 499);
    }
    catch (TaskCanceledException exception) when (cancellationToken.IsCancellationRequested)
    {
        logger.LogInformation(exception, "Audio speech request was canceled because the client aborted the request.");
        return Results.Problem(
            title: "Client closed request.",
            detail: "The client canceled the audio speech request before the backend could finish writing the response.",
            statusCode: 499);
    }
    catch (TaskCanceledException exception)
    {
        logger.LogWarning(exception, "Audio speech request was canceled before a response was produced.");
        return Results.Problem(
            title: "Audio speech request timed out.",
            detail: ApiConstants.AudioSpeechError,
            statusCode: StatusCodes.Status504GatewayTimeout);
    }
    catch (HttpRequestException exception)
    {
        logger.LogWarning(
            exception,
            "OpenAI audio speech HTTP request failed. StatusCode={StatusCode}.",
            exception.StatusCode?.ToString() ?? HttpStatusCode.ServiceUnavailable.ToString());
        return Results.Problem(ApiConstants.AudioSpeechError);
    }
    catch (InvalidOperationException exception)
    {
        logger.LogWarning(exception, "Audio speech request failed during validation or OpenAI processing.");
        return Results.Problem(ApiConstants.AudioSpeechError);
    }
    catch (Exception exception)
    {
        logger.LogError(exception, "Unexpected audio speech request failure.");
        return Results.Problem(ApiConstants.AudioSpeechError);
    }
}
