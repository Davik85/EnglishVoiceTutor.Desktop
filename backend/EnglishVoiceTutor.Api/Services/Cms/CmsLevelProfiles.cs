using System.Text.Json;
using EnglishVoiceTutor.Api.Constants;

namespace EnglishVoiceTutor.Api.Services.Cms;

public static class CmsLevelProfiles
{
    public const int RequiredLevelCount = 4;
    public const int A1WrapUpAfterUserTurn = 10;
    public const int A1FinalMessageAtUserTurn = 15;
    public const int A2WrapUpAfterUserTurn = 14;
    public const int A2FinalMessageAtUserTurn = 20;
    public const int B1WrapUpAfterUserTurn = 18;
    public const int B1FinalMessageAtUserTurn = 25;
    public const int B2WrapUpAfterUserTurn = 24;
    public const int B2FinalMessageAtUserTurn = 32;
    public const int MinimumTurnLimit = 1;
    public const int MaximumFinalMessageAtUserTurn = 80;

    public static readonly IReadOnlyList<string> RequiredLevelKeys = ["a1", "a2", "b1", "b2"];

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true, WriteIndented = true };

    public static IReadOnlyList<CmsLevelProfile> Defaults =>
    [
        new() { StableLevelKey = "a1", DisplayName = "A1 Beginner", IsActive = true, SortOrder = 1, WrapUpAfterUserTurn = A1WrapUpAfterUserTurn, FinalMessageAtUserTurn = A1FinalMessageAtUserTurn, BotLanguageComplexityGuidance = "Use simple short sentences, simple words, and one question at a time. Give more support.", CorrectionGuidance = "Correct one important mistake gently and give a short model answer.", AnswerLengthGuidance = "Use 1-2 short sentences.", AdminNotes = "Shortest default lesson length for new learners." },
        new() { StableLevelKey = "a2", DisplayName = "A2 Elementary", IsActive = true, SortOrder = 2, WrapUpAfterUserTurn = A2WrapUpAfterUserTurn, FinalMessageAtUserTurn = A2FinalMessageAtUserTurn, BotLanguageComplexityGuidance = "Use simple but slightly more varied language. Ask one clear question at a time.", CorrectionGuidance = "Correct lightly with short examples.", AnswerLengthGuidance = "Use 1-3 short sentences.", AdminNotes = "Short-to-medium lesson length." },
        new() { StableLevelKey = "b1", DisplayName = "B1 Intermediate", IsActive = true, SortOrder = 3, WrapUpAfterUserTurn = B1WrapUpAfterUserTurn, FinalMessageAtUserTurn = B1FinalMessageAtUserTurn, BotLanguageComplexityGuidance = "Use more natural dialogue with moderate detail.", CorrectionGuidance = "Give moderate corrections for clarity, grammar, and natural phrasing.", AnswerLengthGuidance = "Use concise natural turns with one useful detail.", AdminNotes = "Medium lesson length." },
        new() { StableLevelKey = "b2", DisplayName = "B2 Upper-Intermediate", IsActive = true, SortOrder = 4, WrapUpAfterUserTurn = B2WrapUpAfterUserTurn, FinalMessageAtUserTurn = B2FinalMessageAtUserTurn, BotLanguageComplexityGuidance = "Support longer discussion, natural expressions, and nuanced dialogue.", CorrectionGuidance = "Give deeper corrections for precision, register, and naturalness.", AnswerLengthGuidance = "Use natural but not monologue-length responses.", AdminNotes = "Longest default lesson length." }
    ];

    public static string DefaultJson() => JsonSerializer.Serialize(Defaults, JsonOptions);

    public static List<CmsLevelProfile> DeserializeOrDefaults(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return Defaults.Select(Clone).ToList();
        return JsonSerializer.Deserialize<List<CmsLevelProfile>>(json, JsonOptions) ?? Defaults.Select(Clone).ToList();
    }


    public static List<CmsLevelProfile> AddMissingRequiredDefaults(IEnumerable<CmsLevelProfile>? profiles)
    {
        var result = profiles?.Select(Clone).ToList() ?? [];
        foreach (var requiredDefault in Defaults)
        {
            if (!result.Any(profile => string.Equals(profile.StableLevelKey?.Trim(), requiredDefault.StableLevelKey, StringComparison.OrdinalIgnoreCase)))
            {
                result.Add(Clone(requiredDefault));
            }
        }

        return result
            .OrderBy(profile => profile.SortOrder)
            .ThenBy(profile => profile.StableLevelKey, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public static CmsLevelProfile Resolve(string? selectedLevel, IEnumerable<CmsLevelProfile>? profiles = null)
    {
        var key = NormalizeLevelKey(selectedLevel);
        return (profiles ?? Defaults).FirstOrDefault(profile => profile.IsActive && string.Equals(profile.StableLevelKey, key, StringComparison.OrdinalIgnoreCase))
            ?? Defaults.First(profile => profile.StableLevelKey == key)
            ?? Defaults.First();
    }

    public static void Validate(IEnumerable<CmsLevelProfile> profiles, ICollection<string> errors, string label = "Level profiles")
    {
        var list = profiles.ToList();
        var activeKeys = list.Where(p => p.IsActive).Select(p => (p.StableLevelKey ?? string.Empty).Trim().ToLowerInvariant()).ToList();
        var duplicates = activeKeys.GroupBy(k => k, StringComparer.Ordinal).Where(g => g.Count() > 1).Select(g => g.Key).ToArray();
        if (duplicates.Length > 0) errors.Add($"{label} contain duplicate active level ids: {string.Join(", ", duplicates)}.");
        foreach (var required in RequiredLevelKeys)
        {
            if (!activeKeys.Contains(required, StringComparer.Ordinal)) errors.Add($"{label} must include active level id '{required}'.");
        }
        var unknown = activeKeys.Where(k => !RequiredLevelKeys.Contains(k, StringComparer.Ordinal)).ToArray();
        if (unknown.Length > 0) errors.Add($"{label} contain unknown active level ids: {string.Join(", ", unknown)}.");
        foreach (var p in list.Where(p => p.IsActive))
        {
            if (string.IsNullOrWhiteSpace(p.DisplayName)) errors.Add($"{label} '{p.StableLevelKey}' is missing displayName.");
            if (p.WrapUpAfterUserTurn < MinimumTurnLimit) errors.Add($"{label} '{p.StableLevelKey}' wrapUpAfterUserTurn must be positive.");
            if (p.FinalMessageAtUserTurn < MinimumTurnLimit || p.FinalMessageAtUserTurn > MaximumFinalMessageAtUserTurn) errors.Add($"{label} '{p.StableLevelKey}' finalMessageAtUserTurn must be between {MinimumTurnLimit} and {MaximumFinalMessageAtUserTurn}.");
            if (p.FinalMessageAtUserTurn <= p.WrapUpAfterUserTurn) errors.Add($"{label} '{p.StableLevelKey}' finalMessageAtUserTurn must be greater than wrapUpAfterUserTurn.");
            if (string.IsNullOrWhiteSpace(p.BotLanguageComplexityGuidance)) errors.Add($"{label} '{p.StableLevelKey}' is missing botLanguageComplexityGuidance.");
            if (string.IsNullOrWhiteSpace(p.CorrectionGuidance)) errors.Add($"{label} '{p.StableLevelKey}' is missing correctionGuidance.");
            if (string.IsNullOrWhiteSpace(p.AnswerLengthGuidance)) errors.Add($"{label} '{p.StableLevelKey}' is missing answerLengthGuidance.");
        }
    }

    public static string NormalizeLevelKey(string? value)
    {
        var trimmed = (value ?? string.Empty).Trim().ToLowerInvariant();
        return RequiredLevelKeys.FirstOrDefault(key => trimmed == key || trimmed.StartsWith(key, StringComparison.Ordinal)) ?? "a1";
    }

    private static CmsLevelProfile Clone(CmsLevelProfile p) => new() { StableLevelKey = p.StableLevelKey, DisplayName = p.DisplayName, IsActive = p.IsActive, SortOrder = p.SortOrder, WrapUpAfterUserTurn = p.WrapUpAfterUserTurn, FinalMessageAtUserTurn = p.FinalMessageAtUserTurn, BotLanguageComplexityGuidance = p.BotLanguageComplexityGuidance, CorrectionGuidance = p.CorrectionGuidance, AnswerLengthGuidance = p.AnswerLengthGuidance, AdminNotes = p.AdminNotes };
}

public sealed class CmsLevelProfile
{
    public string StableLevelKey { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public int SortOrder { get; set; }
    public int WrapUpAfterUserTurn { get; set; } = ApiConstants.DefaultLessonSoftLearnerTurnLimit;
    public int FinalMessageAtUserTurn { get; set; } = ApiConstants.DefaultLessonHardLearnerTurnLimit;
    public string BotLanguageComplexityGuidance { get; set; } = string.Empty;
    public string CorrectionGuidance { get; set; } = string.Empty;
    public string AnswerLengthGuidance { get; set; } = string.Empty;
    public string AdminNotes { get; set; } = string.Empty;
}
