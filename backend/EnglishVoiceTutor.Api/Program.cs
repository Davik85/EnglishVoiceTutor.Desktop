using System.Data.Common;
using System.Net;
using EnglishVoiceTutor.Api.Constants;
using EnglishVoiceTutor.Api.Contracts.Common;
using EnglishVoiceTutor.Api.Contracts.LessonHistory;
using EnglishVoiceTutor.Api.Contracts.LessonMessages;
using EnglishVoiceTutor.Api.Contracts.LessonSessions;
using EnglishVoiceTutor.Api.Contracts.LessonSummaries;
using EnglishVoiceTutor.Api.Contracts.UserSettings;
using EnglishVoiceTutor.Api.Data;
using EnglishVoiceTutor.Api.Models;
using EnglishVoiceTutor.Api.Services;
using EnglishVoiceTutor.Shared.StudyLanguages;
using Microsoft.EntityFrameworkCore;
using HttpBadHttpRequestException = Microsoft.AspNetCore.Http.BadHttpRequestException;

var builder = WebApplication.CreateBuilder(args);

var defaultConnectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(defaultConnectionString))
{
    throw new InvalidOperationException("Connection string 'DefaultConnection' is required. Configure it in appsettings.Development.json, user secrets, or environment variables before starting the backend.");
}

builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseNpgsql(defaultConnectionString);
});

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
builder.Services.AddScoped<TutorIdentityGuard>();
builder.Services.AddScoped<ILessonChatService, OpenAiLessonChatService>();
builder.Services.AddScoped<ILessonHintService, OpenAiLessonHintService>();
builder.Services.AddScoped<AudioTranscriptionService>();
builder.Services.AddScoped<TranslationService>();
builder.Services.AddScoped<AudioSpeechService>();
builder.Services.AddScoped<RealtimeVoiceSessionService>();
builder.Services.AddScoped<DevUserProvider>();
builder.Services.AddScoped<IUserSettingsService, UserSettingsService>();
builder.Services.AddScoped<ILessonSessionService, LessonSessionService>();
builder.Services.AddScoped<ILessonMessageService, LessonMessageService>();
builder.Services.AddScoped<ILessonSummaryService, LessonSummaryService>();
builder.Services.AddScoped<ILessonHistoryService, LessonHistoryService>();
builder.Services.AddScoped<IHealthService, HealthService>();

var app = builder.Build();

app.UseWebSockets();

app.MapGet(ApiConstants.HealthRoute, HandleHealthAsync);
app.MapGet(ApiConstants.ApiHealthRoute, HandleHealthAsync);
app.MapGet(ApiConstants.DatabaseHealthRoute, HandleDatabaseHealthAsync);

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
app.MapPost(ApiConstants.LessonChatFeedbackRoute, HandleLessonChatFeedbackAsync);
app.MapPost(ApiConstants.AudioTranscriptionRoute, HandleAudioTranscriptionAsync);
app.MapPost(ApiConstants.TranslationRoute, HandleTranslationAsync);
app.MapPost(ApiConstants.AudioSpeechRoute, HandleAudioSpeechAsync);
app.MapPost(ApiConstants.AudioSpeechStreamRoute, HandleAudioSpeechStreamAsync);
app.MapGet(ApiConstants.DevUserSettingsRoute, HandleGetDevUserSettingsAsync);
app.MapPut(ApiConstants.DevUserSettingsRoute, HandleUpdateDevUserSettingsAsync);
app.MapPost(ApiConstants.DevLessonSessionsRoute, HandleCreateDevLessonSessionAsync);
app.MapPut(ApiConstants.DevLessonSessionFinishRoute, HandleFinishDevLessonSessionAsync);
app.MapGet(ApiConstants.DevLessonSessionsRoute, HandleGetDevLessonSessionsAsync);
app.MapGet(ApiConstants.DevLessonSessionByIdRoute, HandleGetDevLessonSessionByIdAsync);
app.MapPost(ApiConstants.DevLessonSessionMessagesRoute, HandleCreateDevLessonMessageAsync);
app.MapGet(ApiConstants.DevLessonSessionMessagesRoute, HandleGetDevLessonMessagesAsync);
app.MapPut(ApiConstants.DevLessonSessionSummaryRoute, HandleUpsertDevLessonSummaryAsync);
app.MapGet(ApiConstants.DevLessonSessionSummaryRoute, HandleGetDevLessonSummaryAsync);
app.MapGet(ApiConstants.DevLessonSummariesRoute, HandleGetDevLessonSummariesAsync);
app.MapGet(ApiConstants.DevLessonHistoryRoute, HandleGetDevLessonHistoryAsync);
app.MapGet(ApiConstants.DevLessonHistoryBySessionIdRoute, HandleGetDevLessonHistoryDetailAsync);
app.Map(ApiConstants.RealtimeVoiceRoute, HandleRealtimeVoiceAsync);

app.Logger.LogInformation("{ServiceName} started. Environment={EnvironmentName}; StartedAtUtc={StartedAtUtc:o}; Real lesson chat endpoint enabled at {LessonChatReplyRoute}.",
    ApiConstants.ServiceName,
    app.Environment.EnvironmentName,
    DateTimeOffset.UtcNow,
    ApiConstants.LessonChatReplyRoute);

app.Run();

static async Task HandleRealtimeVoiceAsync(
    HttpContext context,
    RealtimeVoiceSessionService realtimeVoiceSessionService,
    ILoggerFactory loggerFactory)
{
    var logger = loggerFactory.CreateLogger("RealtimeVoiceEndpoint");
    if (!context.WebSockets.IsWebSocketRequest)
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        await context.Response.WriteAsync("Realtime voice endpoint requires a WebSocket request.");
        return;
    }

    using var webSocket = await context.WebSockets.AcceptWebSocketAsync();
    logger.LogInformation("Realtime voice desktop WebSocket accepted. Route={Route}; Path={Path}.", ApiConstants.RealtimeVoiceRoute, context.Request.Path);
    await realtimeVoiceSessionService.RunGatewayAsync(webSocket, context.RequestAborted);
}

static IResult HandleHealthAsync(IHealthService healthService)
{
    var response = healthService.GetHealth();

    return Results.Ok(response);
}

static async Task<IResult> HandleDatabaseHealthAsync(
    IHealthService healthService,
    CancellationToken cancellationToken)
{
    var response = await healthService.GetDatabaseHealthAsync(cancellationToken);

    if (response.CanConnect)
    {
        return Results.Ok(response);
    }

    return Results.Json(response, statusCode: StatusCodes.Status503ServiceUnavailable);
}



static async Task<IResult> HandleCreateDevLessonSessionAsync(
    StartLessonSessionRequest request,
    ILessonSessionService lessonSessionService,
    ILoggerFactory loggerFactory,
    CancellationToken cancellationToken)
{
    var logger = loggerFactory.CreateLogger("DevLessonSessionsEndpoint");

    try
    {
        var createdSession = await lessonSessionService.StartDevLessonSessionAsync(request, cancellationToken);
        return Results.Created($"/api/dev/lesson-sessions/{createdSession.Id}", createdSession);
    }
    catch (LessonSessionValidationException exception)
    {
        return Results.BadRequest(new { error = exception.Message });
    }
    catch (Exception exception) when (IsLessonSessionStorageUnavailable(exception))
    {
        logger.LogWarning(exception, "Dev lesson session POST failed because storage is unavailable.");
        return Results.Json(CreateLessonSessionStorageUnavailableResponse(), statusCode: StatusCodes.Status503ServiceUnavailable);
    }
}

static async Task<IResult> HandleFinishDevLessonSessionAsync(
    Guid sessionId,
    FinishLessonSessionRequest request,
    ILessonSessionService lessonSessionService,
    ILoggerFactory loggerFactory,
    CancellationToken cancellationToken)
{
    var logger = loggerFactory.CreateLogger("DevLessonSessionsEndpoint");

    try
    {
        var updatedSession = await lessonSessionService.FinishDevLessonSessionAsync(sessionId, request, cancellationToken);
        return Results.Ok(updatedSession);
    }
    catch (LessonSessionValidationException exception)
    {
        return Results.BadRequest(new { error = exception.Message });
    }
    catch (KeyNotFoundException)
    {
        return Results.NotFound(new { error = "Lesson session was not found." });
    }
    catch (Exception exception) when (IsLessonSessionStorageUnavailable(exception))
    {
        logger.LogWarning(exception, "Dev lesson session finish PUT failed because storage is unavailable.");
        return Results.Json(CreateLessonSessionStorageUnavailableResponse(), statusCode: StatusCodes.Status503ServiceUnavailable);
    }
}

static async Task<IResult> HandleGetDevLessonSessionsAsync(
    ILessonSessionService lessonSessionService,
    ILoggerFactory loggerFactory,
    CancellationToken cancellationToken)
{
    var logger = loggerFactory.CreateLogger("DevLessonSessionsEndpoint");

    try
    {
        var sessions = await lessonSessionService.GetRecentDevLessonSessionsAsync(cancellationToken);
        return Results.Ok(sessions);
    }
    catch (Exception exception) when (IsLessonSessionStorageUnavailable(exception))
    {
        logger.LogWarning(exception, "Dev lesson sessions GET failed because storage is unavailable.");
        return Results.Json(CreateLessonSessionStorageUnavailableResponse(), statusCode: StatusCodes.Status503ServiceUnavailable);
    }
}

static async Task<IResult> HandleGetDevLessonSessionByIdAsync(
    Guid sessionId,
    ILessonSessionService lessonSessionService,
    ILoggerFactory loggerFactory,
    CancellationToken cancellationToken)
{
    var logger = loggerFactory.CreateLogger("DevLessonSessionsEndpoint");

    try
    {
        var session = await lessonSessionService.GetDevLessonSessionByIdAsync(sessionId, cancellationToken);
        return session is null ? Results.NotFound(new { error = "Lesson session was not found." }) : Results.Ok(session);
    }
    catch (Exception exception) when (IsLessonSessionStorageUnavailable(exception))
    {
        logger.LogWarning(exception, "Dev lesson session by id GET failed because storage is unavailable.");
        return Results.Json(CreateLessonSessionStorageUnavailableResponse(), statusCode: StatusCodes.Status503ServiceUnavailable);
    }
}




static async Task<IResult> HandleGetDevLessonHistoryAsync(
    ILessonHistoryService lessonHistoryService,
    ILoggerFactory loggerFactory,
    CancellationToken cancellationToken)
{
    var logger = loggerFactory.CreateLogger("DevLessonHistoryEndpoint");

    try
    {
        var history = await lessonHistoryService.GetRecentDevLessonHistoryAsync(cancellationToken);
        return Results.Ok(history);
    }
    catch (Exception exception) when (IsLessonSessionStorageUnavailable(exception))
    {
        logger.LogWarning(exception, "Dev lesson history list GET failed because storage is unavailable.");
        return Results.Json(CreateLessonSessionStorageUnavailableResponse(), statusCode: StatusCodes.Status503ServiceUnavailable);
    }
}

static async Task<IResult> HandleGetDevLessonHistoryDetailAsync(
    Guid sessionId,
    ILessonHistoryService lessonHistoryService,
    ILoggerFactory loggerFactory,
    CancellationToken cancellationToken)
{
    var logger = loggerFactory.CreateLogger("DevLessonHistoryEndpoint");

    try
    {
        var detail = await lessonHistoryService.GetDevLessonHistoryDetailAsync(sessionId, cancellationToken);
        return detail is null
            ? Results.NotFound(new { error = "Lesson session was not found." })
            : Results.Ok(detail);
    }
    catch (Exception exception) when (IsLessonSessionStorageUnavailable(exception))
    {
        logger.LogWarning(exception, "Dev lesson history detail GET failed because storage is unavailable.");
        return Results.Json(CreateLessonSessionStorageUnavailableResponse(), statusCode: StatusCodes.Status503ServiceUnavailable);
    }
}

static async Task<IResult> HandleUpsertDevLessonSummaryAsync(
    Guid sessionId,
    UpsertLessonSummaryRequest request,
    ILessonSummaryService lessonSummaryService,
    ILoggerFactory loggerFactory,
    CancellationToken cancellationToken)
{
    var logger = loggerFactory.CreateLogger("DevLessonSummariesEndpoint");

    try
    {
        var summary = await lessonSummaryService.UpsertDevLessonSummaryAsync(sessionId, request, cancellationToken);
        return Results.Ok(summary);
    }
    catch (LessonSummaryValidationException exception)
    {
        return Results.BadRequest(new { error = exception.Message });
    }
    catch (KeyNotFoundException)
    {
        return Results.NotFound(new { error = "Lesson session was not found." });
    }
    catch (Exception exception) when (IsLessonSessionStorageUnavailable(exception))
    {
        logger.LogWarning(exception, "Dev lesson summary PUT failed because storage is unavailable.");
        return Results.Json(CreateLessonSessionStorageUnavailableResponse(), statusCode: StatusCodes.Status503ServiceUnavailable);
    }
}

static async Task<IResult> HandleGetDevLessonSummaryAsync(
    Guid sessionId,
    ILessonSummaryService lessonSummaryService,
    ILoggerFactory loggerFactory,
    CancellationToken cancellationToken)
{
    var logger = loggerFactory.CreateLogger("DevLessonSummariesEndpoint");

    try
    {
        var summary = await lessonSummaryService.GetDevLessonSummaryAsync(sessionId, cancellationToken);
        return Results.Ok(summary);
    }
    catch (KeyNotFoundException)
    {
        return Results.NotFound(new { error = "Lesson session summary was not found." });
    }
    catch (Exception exception) when (IsLessonSessionStorageUnavailable(exception))
    {
        logger.LogWarning(exception, "Dev lesson summary GET failed because storage is unavailable.");
        return Results.Json(CreateLessonSessionStorageUnavailableResponse(), statusCode: StatusCodes.Status503ServiceUnavailable);
    }
}

static async Task<IResult> HandleGetDevLessonSummariesAsync(
    ILessonSummaryService lessonSummaryService,
    ILoggerFactory loggerFactory,
    CancellationToken cancellationToken)
{
    var logger = loggerFactory.CreateLogger("DevLessonSummariesEndpoint");

    try
    {
        var summaries = await lessonSummaryService.GetRecentDevLessonSummariesAsync(cancellationToken);
        return Results.Ok(summaries);
    }
    catch (Exception exception) when (IsLessonSessionStorageUnavailable(exception))
    {
        logger.LogWarning(exception, "Dev lesson summaries GET failed because storage is unavailable.");
        return Results.Json(CreateLessonSessionStorageUnavailableResponse(), statusCode: StatusCodes.Status503ServiceUnavailable);
    }
}
static async Task<IResult> HandleCreateDevLessonMessageAsync(
    Guid sessionId,
    CreateLessonMessageRequest request,
    ILessonMessageService lessonMessageService,
    ILoggerFactory loggerFactory,
    CancellationToken cancellationToken)
{
    var logger = loggerFactory.CreateLogger("DevLessonMessagesEndpoint");

    try
    {
        var createdMessage = await lessonMessageService.CreateDevLessonMessageAsync(sessionId, request, cancellationToken);
        return Results.Created($"/api/dev/lesson-sessions/{sessionId}/messages/{createdMessage.Id}", createdMessage);
    }
    catch (LessonMessageValidationException exception)
    {
        return Results.BadRequest(new { error = exception.Message });
    }
    catch (KeyNotFoundException)
    {
        return Results.NotFound(new { error = "Lesson session was not found." });
    }
    catch (Exception exception) when (IsLessonSessionStorageUnavailable(exception))
    {
        logger.LogWarning(exception, "Dev lesson message POST failed because storage is unavailable.");
        return Results.Json(CreateLessonSessionStorageUnavailableResponse(), statusCode: StatusCodes.Status503ServiceUnavailable);
    }
}

static async Task<IResult> HandleGetDevLessonMessagesAsync(
    Guid sessionId,
    ILessonMessageService lessonMessageService,
    ILoggerFactory loggerFactory,
    CancellationToken cancellationToken)
{
    var logger = loggerFactory.CreateLogger("DevLessonMessagesEndpoint");

    try
    {
        var messages = await lessonMessageService.GetDevLessonMessagesAsync(sessionId, cancellationToken);
        return Results.Ok(messages);
    }
    catch (KeyNotFoundException)
    {
        return Results.NotFound(new { error = "Lesson session was not found." });
    }
    catch (Exception exception) when (IsLessonSessionStorageUnavailable(exception))
    {
        logger.LogWarning(exception, "Dev lesson messages GET failed because storage is unavailable.");
        return Results.Json(CreateLessonSessionStorageUnavailableResponse(), statusCode: StatusCodes.Status503ServiceUnavailable);
    }
}

static bool IsLessonSessionStorageUnavailable(Exception exception)
{
    return exception is DbException
        or TimeoutException
        or InvalidOperationException
        or DbUpdateException
        || exception.InnerException is not null && IsLessonSessionStorageUnavailable(exception.InnerException);
}

static ErrorResponse CreateLessonSessionStorageUnavailableResponse()
{
    return new ErrorResponse
    {
        Status = "ServiceUnavailable",
        Message = "Lesson session storage is unavailable.",
        CheckedAtUtc = DateTimeOffset.UtcNow
    };
}

static async Task<IResult> HandleGetDevUserSettingsAsync(
    IUserSettingsService userSettingsService,
    ILoggerFactory loggerFactory,
    CancellationToken cancellationToken)
{
    var logger = loggerFactory.CreateLogger("DevUserSettingsEndpoint");

    try
    {
        var settings = await userSettingsService.GetDevUserSettingsAsync(cancellationToken);

        return Results.Ok(settings);
    }
    catch (Exception exception) when (IsUserSettingsStorageUnavailable(exception))
    {
        logger.LogWarning(exception, "Dev user settings GET failed because storage is unavailable.");
        return Results.Json(CreateUserSettingsStorageUnavailableResponse(), statusCode: StatusCodes.Status503ServiceUnavailable);
    }
}

static async Task<IResult> HandleUpdateDevUserSettingsAsync(
    UpdateUserSettingsRequest request,
    IUserSettingsService userSettingsService,
    ILoggerFactory loggerFactory,
    CancellationToken cancellationToken)
{
    var logger = loggerFactory.CreateLogger("DevUserSettingsEndpoint");

    try
    {
        var settings = await userSettingsService.UpdateDevUserSettingsAsync(request, cancellationToken);

        return Results.Ok(settings);
    }
    catch (UserSettingsValidationException exception)
    {
        return Results.BadRequest(new
        {
            error = exception.Message
        });
    }
    catch (Exception exception) when (IsUserSettingsStorageUnavailable(exception))
    {
        logger.LogWarning(exception, "Dev user settings PUT failed because storage is unavailable.");
        return Results.Json(CreateUserSettingsStorageUnavailableResponse(), statusCode: StatusCodes.Status503ServiceUnavailable);
    }
}


static bool IsUserSettingsStorageUnavailable(Exception exception)
{
    return exception is DbException
        or TimeoutException
        or InvalidOperationException
        or DbUpdateException
        || exception.InnerException is not null && IsUserSettingsStorageUnavailable(exception.InnerException);
}

static ErrorResponse CreateUserSettingsStorageUnavailableResponse()
{
    return new ErrorResponse
    {
        Status = "ServiceUnavailable",
        Message = "User settings storage is unavailable.",
        CheckedAtUtc = DateTimeOffset.UtcNow
    };
}

static async Task<IResult> HandleLessonChatReplyAsync(
    LessonChatRequest request,
    ILessonChatService lessonChatService,
    ILoggerFactory loggerFactory,
    CancellationToken cancellationToken)
{
    var logger = loggerFactory.CreateLogger("LessonChatReplyEndpoint");
    logger.LogInformation("LessonChatReplyEndpoint lessonType={LessonType}; topic={Topic}; subtopic={Subtopic}; userTurnNumber={UserTurnNumber}; TargetLanguageId={TargetLanguageId}; TargetLanguageName={TargetLanguageName}; TargetLanguageCode={TargetLanguageCode}.",
        request.LessonType,
        string.IsNullOrWhiteSpace(request.Topic) ? request.TopicTitle : request.Topic,
        string.IsNullOrWhiteSpace(request.Subtopic) ? request.SubtopicTitle : request.Subtopic,
        request.UserTurnNumber,
        string.IsNullOrWhiteSpace(request.TargetLanguageId) ? StudyLanguageCatalog.DefaultStudyLanguageId : request.TargetLanguageId,
        string.IsNullOrWhiteSpace(request.TargetLanguageName) ? StudyLanguageCatalog.English.EnglishName : request.TargetLanguageName,
        string.IsNullOrWhiteSpace(request.TargetLanguageCode) ? StudyLanguageCatalog.English.Bcp47Code : request.TargetLanguageCode);

    if (string.IsNullOrWhiteSpace(request.UserMessage))
    {
        return Results.BadRequest(new
        {
            error = ApiConstants.EmptyUserMessageError
        });
    }

    try
    {
        request.RequestPurpose = "typed_lesson_chat";
        var response = await lessonChatService.CreateReplyAsync(request, cancellationToken);

        return Results.Ok(response);
    }
    catch (Exception exception)
    {
        logger.LogError(exception, "LessonChatReplyEndpoint failed to create a real lesson chat reply.");

        return Results.Problem(
            title: "Lesson chat reply failed.",
            detail: "The real lesson chat service could not create a reply. Please check backend AI configuration and try again.",
            statusCode: StatusCodes.Status502BadGateway);
    }
}

static async Task<IResult> HandleMockLessonChatReplyAsync(
    LessonChatRequest request,
    MockLessonChatService mockLessonChatService,
    ILoggerFactory loggerFactory,
    CancellationToken cancellationToken)
{
    var logger = loggerFactory.CreateLogger("MockLessonChatEndpoint");
    logger.LogWarning("Mock lesson chat endpoint was called.");

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
    ILoggerFactory loggerFactory,
    CancellationToken cancellationToken)
{
    var logger = loggerFactory.CreateLogger("LessonChatHintEndpoint");
    logger.LogInformation("LessonChatHintEndpoint lessonType={LessonType}; topic={Topic}; subtopic={Subtopic}; lessonPhase={LessonPhase}; TargetLanguageId={TargetLanguageId}; TargetLanguageName={TargetLanguageName}; TargetLanguageCode={TargetLanguageCode}.",
        request.LessonType,
        string.IsNullOrWhiteSpace(request.Topic) ? request.TopicTitle : request.Topic,
        string.IsNullOrWhiteSpace(request.Subtopic) ? request.SubtopicTitle : request.Subtopic,
        request.LessonPhase,
        string.IsNullOrWhiteSpace(request.TargetLanguageId) ? StudyLanguageCatalog.DefaultStudyLanguageId : request.TargetLanguageId,
        string.IsNullOrWhiteSpace(request.TargetLanguageName) ? StudyLanguageCatalog.English.EnglishName : request.TargetLanguageName,
        string.IsNullOrWhiteSpace(request.TargetLanguageCode) ? StudyLanguageCatalog.English.Bcp47Code : request.TargetLanguageCode);

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

static async Task<IResult> HandleLessonChatFeedbackAsync(
    LessonChatRequest request,
    ILessonChatService lessonChatService,
    ILoggerFactory loggerFactory,
    CancellationToken cancellationToken)
{
    var logger = loggerFactory.CreateLogger("LessonChatFeedbackEndpoint");
    logger.LogInformation("LessonChatFeedbackEndpoint sourceMessageId={SourceMessageId}; sourceMessageKind={SourceMessageKind}; sourceMessageLength={UserMessageLength}; lessonType={LessonType}; topic={Topic}; subtopic={Subtopic}; userTurnNumber={UserTurnNumber}; TargetLanguageId={TargetLanguageId}; TargetLanguageName={TargetLanguageName}; TargetLanguageCode={TargetLanguageCode}.",
        request.SourceMessageId,
        request.SourceMessageKind,
        request.UserMessage?.Trim().Length ?? 0,
        request.LessonType,
        string.IsNullOrWhiteSpace(request.Topic) ? request.TopicTitle : request.Topic,
        string.IsNullOrWhiteSpace(request.Subtopic) ? request.SubtopicTitle : request.Subtopic,
        request.UserTurnNumber,
        string.IsNullOrWhiteSpace(request.TargetLanguageId) ? StudyLanguageCatalog.DefaultStudyLanguageId : request.TargetLanguageId,
        string.IsNullOrWhiteSpace(request.TargetLanguageName) ? StudyLanguageCatalog.English.EnglishName : request.TargetLanguageName,
        string.IsNullOrWhiteSpace(request.TargetLanguageCode) ? StudyLanguageCatalog.English.Bcp47Code : request.TargetLanguageCode);

    if (string.IsNullOrWhiteSpace(request.UserMessage))
    {
        return Results.BadRequest(new
        {
            error = ApiConstants.EmptyUserMessageError
        });
    }

    try
    {
        request.RequestPurpose = "feedback";
        var feedback = await lessonChatService.CreateFeedbackAsync(request, cancellationToken);
        return Results.Ok(feedback);
    }
    catch (Exception exception)
    {
        logger.LogError(exception, "LessonChatFeedbackEndpoint failed to create feedback for the selected message.");
        return Results.Problem(
            title: "Lesson feedback failed.",
            detail: "The real lesson chat service could not create feedback. Please check backend AI configuration and try again.",
            statusCode: StatusCodes.Status502BadGateway);
    }
}

// Stable TTS pipeline: used by normal Lesson Chat voice playback and default TTS Conversation Mode.
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
        var targetLanguageId = form["targetLanguageId"].ToString();
        var targetLanguageCode = form["targetLanguageCode"].ToString();
        var targetLanguage = StudyLanguageCatalog.GetById(targetLanguageId);

        if (!string.IsNullOrWhiteSpace(targetLanguageCode))
        {
            targetLanguage = StudyLanguageCatalog.All.FirstOrDefault(language => string.Equals(language.TranscriptionLanguageCode, targetLanguageCode.Trim(), StringComparison.OrdinalIgnoreCase)) ?? targetLanguage;
        }

        logger.LogInformation(
            "Audio transcription form read completed. FileName={FileName}; FileLength={FileLength}; TargetLanguageId={TargetLanguageId}; TranscriptionLanguageCode={TranscriptionLanguageCode}.",
            audioFile?.FileName ?? "<missing>",
            audioFile?.Length ?? 0,
            targetLanguage.Id,
            targetLanguage.TranscriptionLanguageCode);

        if (audioFile is null || audioFile.Length <= 0)
        {
            return Results.BadRequest(new
            {
                error = ApiConstants.EmptyAudioFileError
            });
        }

        var response = await audioTranscriptionService.TranscribeAsync(audioFile, targetLanguage, cancellationToken);
        var transcriptionLength = response.Text?.Length ?? 0;

        logger.LogInformation("Audio transcription completed successfully. TranscriptionLength={TranscriptionLength}; TargetLanguageId={TargetLanguageId}; TranscriptionLanguageCode={TranscriptionLanguageCode}.", transcriptionLength, targetLanguage.Id, targetLanguage.TranscriptionLanguageCode);

        return Results.Ok(response);
    }
    catch (HttpBadHttpRequestException exception)
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

static bool IsRequestBodyReadTimeout(HttpBadHttpRequestException exception)
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
// Stable TTS pipeline: used by normal Lesson Chat voice playback and default TTS Conversation Mode.
static async Task<IResult> HandleAudioSpeechStreamAsync(
    AudioSpeechRequest request,
    AudioSpeechService audioSpeechService,
    HttpContext httpContext,
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

    var logger = loggerFactory.CreateLogger("AudioSpeechStreamEndpoint");
    var response = httpContext.Response;
    response.ContentType = OpenAiConstants.DefaultBotVoiceStreamResponseFormat == OpenAiConstants.PcmSpeechResponseFormat
        ? OpenAiConstants.PcmContentType
        : OpenAiConstants.WavContentType;

    try
    {
        var metrics = await audioSpeechService.StreamSpeechAsync(request.Text, response.Body, request.Purpose, cancellationToken);
        logger.LogInformation(
            "Audio speech stream endpoint completed. Endpoint={Endpoint}; FirstHeaderMs={FirstHeaderMs}; FirstChunkMs={FirstChunkMs}; FirstChunkWrittenMs={FirstChunkWrittenMs}; TotalMs={TotalMs}; TotalBytes={TotalBytes}.",
            "audio/speech-stream",
            metrics.FirstHeaderMs,
            metrics.FirstChunkMs,
            metrics.FirstChunkWrittenMs,
            metrics.TotalMs,
            metrics.TotalBytes);
        return Results.Empty;
    }
    catch (AudioSpeechRequestCanceledException exception) when (exception.InternalTimeoutReached && !response.HasStarted)
    {
        logger.LogWarning(
            exception,
            "Audio speech stream timed out before any audio was written. TimeoutSeconds={TimeoutSeconds}; ClientCancellationRequested={ClientCancellationRequested}.",
            OpenAiConstants.BotVoiceFirstAudioTimeoutSeconds,
            exception.ClientCancellationRequested);
        return Results.Problem(
            title: "Audio speech stream timed out.",
            detail: $"OpenAI speech streaming did not produce audio within {OpenAiConstants.BotVoiceFirstAudioTimeoutSeconds} seconds.",
            statusCode: StatusCodes.Status504GatewayTimeout);
    }
    catch (AudioSpeechRequestCanceledException exception) when (exception.InternalTimeoutReached)
    {
        logger.LogWarning(exception, "Audio speech stream timed out after streaming started; closing response gracefully.");
        return Results.Empty;
    }
    catch (AudioSpeechRequestCanceledException exception) when (exception.ClientCancellationRequested || cancellationToken.IsCancellationRequested)
    {
        logger.LogInformation(exception, "Audio speech stream was canceled because the client aborted the request.");
        return response.HasStarted
            ? Results.Empty
            : Results.Problem(
                title: "Client closed request.",
                detail: "The client canceled the audio speech stream before the backend could finish writing the response.",
                statusCode: 499);
    }
    catch (HttpRequestException exception) when (!response.HasStarted)
    {
        logger.LogWarning(
            exception,
            "OpenAI audio speech stream HTTP request failed. StatusCode={StatusCode}.",
            exception.StatusCode?.ToString() ?? HttpStatusCode.ServiceUnavailable.ToString());
        return Results.Problem(ApiConstants.AudioSpeechError);
    }
    catch (InvalidOperationException exception) when (!response.HasStarted)
    {
        logger.LogWarning(exception, "Audio speech stream failed during validation or OpenAI processing.");
        return Results.Problem(ApiConstants.AudioSpeechError);
    }
    catch (Exception exception) when (!response.HasStarted)
    {
        logger.LogError(exception, "Unexpected audio speech stream failure before response started.");
        return Results.Problem(ApiConstants.AudioSpeechError);
    }
    catch (Exception exception)
    {
        logger.LogWarning(exception, "Audio speech stream failed after streaming started; closing response gracefully.");
        return Results.Empty;
    }
}

// Stable TTS pipeline: used by normal Lesson Chat voice playback and default TTS Conversation Mode.
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
        logger.LogInformation(
            "Audio speech endpoint request accepted. Endpoint={Endpoint}; Model={Model}; Purpose={Purpose}; SpeechSpeed={SpeechSpeed}; HasInstructions={HasInstructions}; InstructionsLength={InstructionsLength}; InputLength={InputLength}; TargetLanguageId={TargetLanguageId}; TargetLanguageCode={TargetLanguageCode}.",
            "audio/speech",
            request.Model ?? OpenAiConstants.DefaultBotVoiceSpeechModel,
            request.Purpose,
            request.SpeechSpeed,
            !string.IsNullOrWhiteSpace(request.Instructions),
            request.Instructions?.Length ?? 0,
            request.Text.Length,
            string.IsNullOrWhiteSpace(request.TargetLanguageId) ? StudyLanguageCatalog.DefaultStudyLanguageId : request.TargetLanguageId,
            string.IsNullOrWhiteSpace(request.TargetLanguageCode) ? StudyLanguageCatalog.English.Bcp47Code : request.TargetLanguageCode);

        var audioBytes = await audioSpeechService.CreateSpeechAsync(request.Text, request.Purpose, request.SpeechSpeed, request.Model, request.Instructions, cancellationToken);

        return Results.File(audioBytes, OpenAiConstants.SpeechResponseContentType);
    }
    catch (AudioSpeechRequestCanceledException exception) when (exception.ClientCancellationRequested || cancellationToken.IsCancellationRequested)
    {
        logger.LogInformation(
            "Audio speech request was canceled because the client aborted the request. Endpoint={Endpoint}; ClientCancellationRequested={ClientCancellationRequested}; InternalTimeoutReached={InternalTimeoutReached}.",
            "audio/speech",
            exception.ClientCancellationRequested,
            exception.InternalTimeoutReached);
        return Results.Problem(
            title: "Client closed request.",
            detail: "The client canceled the audio speech request before the backend could finish writing the response.",
            statusCode: 499);
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
    catch (TaskCanceledException) when (cancellationToken.IsCancellationRequested)
    {
        logger.LogInformation("Audio speech request was canceled because the client aborted the request. Endpoint={Endpoint}; Purpose={Purpose}; ClientCancellationRequested={ClientCancellationRequested}.", "audio/speech", request.Purpose, true);
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
