using System.Text.Json;
using System.Text.RegularExpressions;
using EnglishVoiceTutor.Api.Constants;
using EnglishVoiceTutor.Api.Models;

namespace EnglishVoiceTutor.Api.Services;

public sealed partial class AiModelSettingsService(IWebHostEnvironment environment, ILogger<AiModelSettingsService> logger) : IAiModelSettingsService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };
    private static readonly Regex SafeModelIdRegex = SafeModelId();
    private const int MaxModelIdLength = 120;
    private readonly object _sync = new();

    public AiModelSettings GetActiveSettings()
    {
        try
        {
            var document = ReadDocument();
            var validation = Validate(document.Active);
            if (validation.IsValid)
            {
                return Normalize(document.Active);
            }

            logger.LogWarning("AI model CMS active settings are invalid; fallback default model settings are being used. ErrorCount={ErrorCount}.", validation.Errors.Count);
        }
        catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException)
        {
            logger.LogWarning(ex, "AI model CMS settings could not be read; fallback default model settings are being used.");
        }

        return AiModelSettings.Defaults;
    }

    public Task<AiModelSettingsResponse> GetAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var document = ReadDocument();
        return Task.FromResult(ToResponse(document));
    }

    public async Task<AiModelSettingsResponse> SaveDraftAsync(AiModelSettings draft, string? updatedBy, CancellationToken cancellationToken)
    {
        var validation = Validate(draft);
        if (!validation.IsValid)
        {
            throw new InvalidOperationException(string.Join(" ", validation.Errors));
        }

        var current = ReadDocument();
        var next = current with
        {
            Draft = Normalize(draft),
            UpdatedAtUtc = DateTimeOffset.UtcNow,
            UpdatedBy = NormalizeUpdatedBy(updatedBy),
            Revision = current.Revision + 1
        };
        await WriteDocumentAsync(next, cancellationToken);
        return ToResponse(next);
    }

    public AiModelSettingsValidationResponse Validate(AiModelSettings settings)
    {
        List<string> errors = [];
        ValidateRequiredModel(settings.LessonTutorChatModel, "Lesson tutor chat model", errors);
        ValidateRequiredModel(settings.FeedbackCorrectionModel, "Feedback / correction model", errors);
        ValidateRequiredModel(settings.LessonHintModel, "Lesson hint model", errors);
        ValidateRequiredModel(settings.TranslationModel, "Translation model", errors);
        ValidateRequiredModel(settings.SpeechToTextModel, "Speech-to-text model", errors);
        ValidateRequiredModel(settings.LessonChatTextToSpeechModel, "Lesson chat text-to-speech model", errors);
        ValidateRequiredModel(settings.ConversationModeTextToSpeechModel, "Conversation mode text-to-speech model", errors);
        ValidateRequiredModel(settings.RealtimeVoiceModel, "Realtime voice model", errors);

        var warnings = errors.Count == 0
            ? new[] { "Model IDs are syntactically valid only; publish after a small test lesson because nonexistent provider model names can break AI calls." }
            : Array.Empty<string>();
        return new AiModelSettingsValidationResponse(errors.Count == 0, errors, warnings);
    }

    public async Task<AiModelSettingsResponse> PublishAsync(string? updatedBy, CancellationToken cancellationToken)
    {
        var current = ReadDocument();
        var validation = Validate(current.Draft);
        if (!validation.IsValid)
        {
            throw new InvalidOperationException(string.Join(" ", validation.Errors));
        }

        var next = current with
        {
            Active = Normalize(current.Draft),
            UpdatedAtUtc = DateTimeOffset.UtcNow,
            UpdatedBy = NormalizeUpdatedBy(updatedBy),
            Revision = current.Revision + 1
        };
        await WriteDocumentAsync(next, cancellationToken);
        return ToResponse(next);
    }

    public async Task<AiModelSettingsResponse> ResetDraftFromActiveAsync(string? updatedBy, CancellationToken cancellationToken)
    {
        var current = ReadDocument();
        var next = current with
        {
            Draft = Normalize(current.Active),
            UpdatedAtUtc = DateTimeOffset.UtcNow,
            UpdatedBy = NormalizeUpdatedBy(updatedBy),
            Revision = current.Revision + 1
        };
        await WriteDocumentAsync(next, cancellationToken);
        return ToResponse(next);
    }

    private AiModelSettingsDocument ReadDocument()
    {
        lock (_sync)
        {
            var path = GetPath();
            if (!File.Exists(path))
            {
                var defaults = AiModelSettings.Defaults;
                return new AiModelSettingsDocument(defaults, defaults, DateTimeOffset.UtcNow, null, 0);
            }

            using var stream = File.OpenRead(path);
            var document = JsonSerializer.Deserialize<AiModelSettingsDocument>(stream, JsonOptions);
            if (document is null)
            {
                var defaults = AiModelSettings.Defaults;
                return new AiModelSettingsDocument(defaults, defaults, DateTimeOffset.UtcNow, null, 0);
            }

            return document with
            {
                Active = Normalize(document.Active),
                Draft = Normalize(document.Draft)
            };
        }
    }

    private async Task WriteDocumentAsync(AiModelSettingsDocument document, CancellationToken cancellationToken)
    {
        var path = GetPath();
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await using var stream = File.Create(path);
        await JsonSerializer.SerializeAsync(stream, document, JsonOptions, cancellationToken);
    }

    private string GetPath() => Path.Combine(environment.ContentRootPath, "site", "content", "ai-model-settings.json");

    private static AiModelSettingsResponse ToResponse(AiModelSettingsDocument document) =>
        new(document.Active, document.Draft, document.UpdatedAtUtc, document.UpdatedBy, document.Revision, []);

    private static AiModelSettings Normalize(AiModelSettings? settings)
    {
        settings ??= AiModelSettings.Defaults;
        return new AiModelSettings(
            NormalizeModel(settings.LessonTutorChatModel, AiModelSettings.Defaults.LessonTutorChatModel),
            NormalizeModel(settings.FeedbackCorrectionModel, AiModelSettings.Defaults.FeedbackCorrectionModel),
            NormalizeModel(settings.LessonHintModel, AiModelSettings.Defaults.LessonHintModel),
            NormalizeModel(settings.TranslationModel, AiModelSettings.Defaults.TranslationModel),
            NormalizeModel(settings.SpeechToTextModel, AiModelSettings.Defaults.SpeechToTextModel),
            NormalizeModel(settings.LessonChatTextToSpeechModel, AiModelSettings.Defaults.LessonChatTextToSpeechModel),
            NormalizeModel(settings.ConversationModeTextToSpeechModel, AiModelSettings.Defaults.ConversationModeTextToSpeechModel),
            NormalizeModel(settings.RealtimeVoiceModel, AiModelSettings.Defaults.RealtimeVoiceModel));
    }

    private static string NormalizeModel(string? value, string fallback) => string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
    private static string? NormalizeUpdatedBy(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim()[..Math.Min(value.Trim().Length, 120)];

    private static void ValidateRequiredModel(string? value, string label, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            errors.Add($"{label} is required.");
            return;
        }

        var trimmed = value.Trim();
        if (trimmed.Length > MaxModelIdLength)
        {
            errors.Add($"{label} is too long.");
        }

        if (!SafeModelIdRegex.IsMatch(trimmed))
        {
            errors.Add($"{label} may contain only letters, numbers, dot, dash, underscore, and colon.");
        }
    }

    [GeneratedRegex("^[A-Za-z0-9._:-]+$", RegexOptions.CultureInvariant)]
    private static partial Regex SafeModelId();
}
