using EnglishVoiceTutor.Api.Constants;
using EnglishVoiceTutor.Api.Models;
using EnglishVoiceTutor.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpClient();
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

app.MapGet(ApiConstants.HealthRoute, () =>
{
    return Results.Ok(new
    {
        status = ApiConstants.HealthOkStatus,
        service = ApiConstants.ServiceName
    });
});

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
    CancellationToken cancellationToken)
{
    if (!request.HasFormContentType)
    {
        return Results.BadRequest(new
        {
            error = ApiConstants.EmptyAudioFileError
        });
    }

    var form = await request.ReadFormAsync(cancellationToken);
    var audioFile = form.Files.GetFile(OpenAiConstants.MultipartFileFieldName);

    if (audioFile is null || audioFile.Length <= 0)
    {
        return Results.BadRequest(new
        {
            error = ApiConstants.EmptyAudioFileError
        });
    }

    try
    {
        var response = await audioTranscriptionService.TranscribeAsync(audioFile, cancellationToken);

        return Results.Ok(response);
    }
    catch (Exception)
    {
        return Results.Problem(ApiConstants.AudioTranscriptionError);
    }
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
