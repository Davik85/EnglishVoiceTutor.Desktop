using EnglishVoiceTutor.Desktop.Models.LessonContent;
using EnglishVoiceTutor.Shared.StudyLanguages;

namespace EnglishVoiceTutor.Api.Services.Cms;

public sealed class LocalizedLessonSetupResolver
{
    public LocalizedLessonSetup Resolve(LessonScenario scenario, string? backendStudyLanguage)
    {
        ArgumentNullException.ThrowIfNull(scenario);

        var requestedLanguage = backendStudyLanguage?.Trim();
        var language = StudyLanguageCatalog.All.FirstOrDefault(candidate =>
                string.Equals(candidate.Id, backendStudyLanguage?.Trim(), StringComparison.OrdinalIgnoreCase)
                || string.Equals(candidate.EnglishName, backendStudyLanguage?.Trim(), StringComparison.OrdinalIgnoreCase));
        if (language is null && !string.IsNullOrWhiteSpace(requestedLanguage))
        {
            return new LocalizedLessonSetup
            {
                Source = "unsupported_study_language",
                Status = "incomplete",
                FallbackUsed = false
            };
        }

        language ??= StudyLanguageCatalog.English;
        if (string.Equals(language.Id, StudyLanguageCatalog.DefaultStudyLanguageId, StringComparison.OrdinalIgnoreCase))
        {
            return new LocalizedLessonSetup
            {
                ResolvedStudyLanguageId = language.Id,
                SetupMessageTemplate = scenario.LessonSetup.SetupMessage,
                ContextVariantDisplayTitles = scenario.ControlledVariation.ContextVariants
                    .Where(variant => !string.IsNullOrWhiteSpace(variant.Id))
                    .ToDictionary(variant => variant.Id, variant => variant.Title, StringComparer.Ordinal),
                Source = "canonical_english",
                Status = "complete",
                FallbackUsed = false
            };
        }

        var localizations = scenario.SetupLocalizations;
        if (localizations is not null && localizations.TryGetValue(language.Id, out var localization) && IsComplete(scenario, localization))
        {
            return new LocalizedLessonSetup
            {
                ResolvedStudyLanguageId = language.Id,
                SetupMessageTemplate = localization.SetupMessageTemplate,
                ContextVariantDisplayTitles = new Dictionary<string, string>(localization.ContextVariantTitles, StringComparer.Ordinal),
                Source = "published_snapshot",
                Status = "complete",
                FallbackUsed = false
            };
        }

        return new LocalizedLessonSetup
        {
            ResolvedStudyLanguageId = language.Id,
            Source = "missing_published_localization",
            Status = "incomplete",
            FallbackUsed = false
        };
    }

    private static bool IsComplete(LessonScenario scenario, LessonSetupLocalization localization)
    {
        if (string.IsNullOrWhiteSpace(localization.SetupMessageTemplate))
        {
            return false;
        }

        var ids = scenario.ControlledVariation.ContextVariants.Select(variant => variant.Id).ToHashSet(StringComparer.Ordinal);
        return localization.ContextVariantTitles.Count == ids.Count
            && localization.ContextVariantTitles.All(pair => ids.Contains(pair.Key) && !string.IsNullOrWhiteSpace(pair.Value));
    }
}
