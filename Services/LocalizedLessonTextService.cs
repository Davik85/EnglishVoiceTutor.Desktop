using System.Globalization;
using System.Text;
using EnglishVoiceTutor.Desktop.Constants;
using EnglishVoiceTutor.Desktop.Models.LessonContent;
using EnglishVoiceTutor.Shared.StudyLanguages;

namespace EnglishVoiceTutor.Desktop.Services;

public static class LocalizedLessonTextService
{
    public const string OpeningMessageSource = "backend localized setup template or canonical lesson setup";
    // The lesson JSON scenario text is semantic metadata.

    public sealed record LocalizedScenarioOption(int Number, string CanonicalTitle, string LocalizedTitle, ContextVariant Variant);

    public sealed record LocalizedScenarioSelection(string CanonicalScenario, string LocalizedScenario, ContextVariant Variant);
    public sealed record BackendLocalizedSetupValidationResult(bool IsValid, string Reason, LocalizedLessonSetup? LocalizedSetup);

    public static bool TryGetCompleteBackendLocalizedSetup(LessonScenario lessonScenario, StudyLanguageDefinition studyLanguage, out LocalizedLessonSetup? localizedSetup)
    {
        var result = ValidateBackendLocalizedSetup(lessonScenario, studyLanguage);
        localizedSetup = result.LocalizedSetup;
        return result.IsValid;
    }

    public static BackendLocalizedSetupValidationResult ValidateBackendLocalizedSetup(LessonScenario lessonScenario, StudyLanguageDefinition studyLanguage)
    {
        var language = ResolveLanguage(studyLanguage);
        if (IsEnglish(language)) return new(false, "english_uses_canonical_setup", null);
        var candidate = lessonScenario.LocalizedSetup;
        if (candidate is null
            || !string.Equals(candidate.Status, "complete", StringComparison.Ordinal)
            || candidate.FallbackUsed
            || !string.Equals(candidate.ResolvedStudyLanguageId, language.Id, StringComparison.Ordinal)
            || string.IsNullOrWhiteSpace(candidate.SetupMessageTemplate)) return new(false, "missing_or_incomplete_localized_setup", null);

        var variants = lessonScenario.ControlledVariation.ContextVariants;
        var variantIds = variants.Select(variant => variant.Id).ToArray();
        if (variantIds.Any(string.IsNullOrWhiteSpace)
            || variantIds.Distinct(StringComparer.Ordinal).Count() != variantIds.Length
            || variantIds.Distinct(StringComparer.OrdinalIgnoreCase).Count() != variantIds.Length) return new(false, "invalid_context_variant_ids", null);

        var titles = candidate.ContextVariantDisplayTitles;
        if (titles is null || titles.Count != variantIds.Length
            || titles.Keys.Any(key => string.IsNullOrWhiteSpace(key))
            || titles.Keys.Distinct(StringComparer.OrdinalIgnoreCase).Count() != titles.Count
            || titles.Any(pair => string.IsNullOrWhiteSpace(pair.Value))) return new(false, "invalid_context_title_mapping", null);

        var expected = variantIds.ToHashSet(StringComparer.Ordinal);
        if (!titles.Keys.ToHashSet(StringComparer.Ordinal).SetEquals(expected)) return new(false, "context_title_ids_do_not_match", null);
        return new(true, "complete", candidate);
    }

    public static IReadOnlyList<LocalizedScenarioOption> GetLocalizedScenarioOptions(
        LessonScenario lessonScenario,
        StudyLanguageDefinition studyLanguage)
    {
        var language = ResolveLanguage(studyLanguage);
        if (TryGetCompleteBackendLocalizedSetup(lessonScenario, language, out var localizedSetup))
        {
            return lessonScenario.ControlledVariation.ContextVariants
                .Select((variant, index) => new LocalizedScenarioOption(index + 1, variant.Title.Trim(), localizedSetup!.ContextVariantDisplayTitles[variant.Id], variant))
                .ToArray();
        }
        return lessonScenario.ControlledVariation.ContextVariants
            .Select((variant, index) => new LocalizedScenarioOption(
                index + 1,
                variant.Title.Trim(),
                AdaptShortScenarioText(variant.Title, language),
                variant))
            .ToArray();
    }

    public static bool TryResolveLocalizedScenarioSelection(
        string inputText,
        LessonScenario lessonScenario,
        StudyLanguageDefinition studyLanguage,
        out string canonicalScenario,
        out string localizedScenario,
        out ContextVariant? matchedVariant)
    {
        canonicalScenario = string.Empty;
        localizedScenario = string.Empty;
        matchedVariant = null;

        var normalizedInput = NormalizeScenarioSelection(inputText);
        if (string.IsNullOrWhiteSpace(normalizedInput))
        {
            return false;
        }

        var options = GetLocalizedScenarioOptions(lessonScenario, studyLanguage);
        if (int.TryParse(normalizedInput, NumberStyles.Integer, CultureInfo.InvariantCulture, out var optionNumber))
        {
            var numberedOption = options.FirstOrDefault(option => option.Number == optionNumber);
            if (numberedOption is not null)
            {
                canonicalScenario = numberedOption.CanonicalTitle;
                localizedScenario = numberedOption.LocalizedTitle;
                matchedVariant = numberedOption.Variant;
                return true;
            }
        }

        foreach (var option in options)
        {
            var localizedCandidates = new[] { option.LocalizedTitle, option.CanonicalTitle }
                .Concat(option.Variant.Aliases)
                .Where(candidate => !string.IsNullOrWhiteSpace(candidate));

            foreach (var candidate in localizedCandidates)
            {
                var normalizedCandidate = NormalizeScenarioSelection(candidate);
                if (ScenarioSelectionMatches(normalizedInput, normalizedCandidate))
                {
                    canonicalScenario = option.CanonicalTitle;
                    localizedScenario = option.LocalizedTitle;
                    matchedVariant = option.Variant;
                    return true;
                }
            }
        }

        return false;
    }

    public static string NormalizeScenarioSelection(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var normalizedForm = value.Trim().Replace('_', ' ').Normalize(NormalizationForm.FormD);
        var builder = new System.Text.StringBuilder(normalizedForm.Length);
        foreach (var character in normalizedForm)
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(character);
            if (category == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            builder.Append(char.IsLetterOrDigit(character) ? char.ToLowerInvariant(character) : ' ');
        }

        return string.Join(' ', builder.ToString().Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }

    private static bool ScenarioSelectionMatches(string normalizedInput, string normalizedCandidate)
    {
        return !string.IsNullOrWhiteSpace(normalizedCandidate)
            && (normalizedInput.Equals(normalizedCandidate, StringComparison.OrdinalIgnoreCase)
                || normalizedInput.Contains(normalizedCandidate, StringComparison.OrdinalIgnoreCase)
                || normalizedCandidate.Contains(normalizedInput, StringComparison.OrdinalIgnoreCase));
    }

    public static string BuildSetupMessage(
        LessonScenario lessonScenario,
        string selectedSubtopicTitle,
        string userDisplayName,
        StudyLanguageDefinition studyLanguage,
        Func<string, string> renderEnglishTemplate,
        Func<string, string> renderLocalizedTemplate)
    {
        var language = ResolveLanguage(studyLanguage);
        var englishTemplate = string.IsNullOrWhiteSpace(lessonScenario.LessonSetup.SetupMessage)
            ? AppConstants.MockBotFirstMessage
            : renderEnglishTemplate(lessonScenario.LessonSetup.SetupMessage.Trim());

        if (IsEnglish(language))
        {
            return englishTemplate;
        }

        if (TryGetCompleteBackendLocalizedSetup(lessonScenario, language, out var localizedSetup))
        {
            return renderLocalizedTemplate(localizedSetup!.SetupMessageTemplate!);
        }

        var subtopic = AdaptShortScenarioText(selectedSubtopicTitle, language);
        var goal = AdaptGoal(lessonScenario, language);
        var choices = lessonScenario.ControlledVariation.ContextVariants
            .Take(3)
            .Select((variant, index) => $"{index + 1}. {AdaptShortScenarioText(variant.Title, language)}")
            .ToArray();
        var choiceBlock = choices.Length == 0
            ? LocalizeChooseSimpleSituation(language, subtopic)
            : string.Join(Environment.NewLine, choices);

        return language.Id switch
        {
            "fr" => $"Aujourd’hui, nous allons pratiquer : {subtopic}.\n\nObjectif : {goal}\n\nChoisis une situation :\n{choiceBlock}\n\nOu propose ta propre situation sur ce thème.",
            "de" => $"Heute üben wir: {subtopic}.\n\nZiel: {goal}\n\nWähle eine Situation:\n{choiceBlock}\n\nOder schlage eine eigene passende Situation vor.",
            "pt" => $"Hoje vamos praticar: {subtopic}.\n\nObjetivo: {goal}\n\nEscolha uma situação:\n{choiceBlock}\n\nOu sugira sua própria situação sobre este tema.",
            "es" => $"Hoy vamos a practicar: {subtopic}.\n\nObjetivo: {goal}\n\nElige una situación:\n{choiceBlock}\n\nO propone tu propia situación sobre este tema.",
            "it" => $"Oggi pratichiamo: {subtopic}.\n\nObiettivo: {goal}\n\nScegli una situazione:\n{choiceBlock}\n\nOppure proponi una tua situazione su questo tema.",
            _ => englishTemplate
        };
    }

    public static string BuildContextConfirmationLine(ContextVariant variant, string resolvedLocalizedTitle, StudyLanguageDefinition studyLanguage, string englishFallback)
    {
        var language = ResolveLanguage(studyLanguage);
        if (IsEnglish(language))
        {
            return englishFallback;
        }

        var title = string.IsNullOrWhiteSpace(resolvedLocalizedTitle)
            ? AdaptShortScenarioText(variant.Title, language)
            : resolvedLocalizedTitle;
        return language.Id switch
        {
            "fr" => $"Très bien ! Imaginons cette situation : {title}.",
            "de" => $"Sehr gut! Stellen wir uns diese Situation vor: {title}.",
            "pt" => $"Muito bem! Vamos imaginar esta situação: {title}.",
            "es" => $"Muy bien. Imaginemos esta situación: {title}.",
            "it" => $"Molto bene! Immaginiamo questa situazione: {title}.",
            _ => englishFallback
        };
    }

    public static string BuildContextOpeningLine(string englishOpeningLine, LessonScenario lessonScenario, StudyLanguageDefinition studyLanguage)
    {
        var language = ResolveLanguage(studyLanguage);
        if (IsEnglish(language))
        {
            return englishOpeningLine;
        }

        if (IsIntroductionsLesson(lessonScenario))
        {
            return language.Id switch
            {
                "fr" => "Bonjour ! Ravi de te rencontrer. Comment tu t’appelles ?",
                "de" => "Hallo! Schön, dich kennenzulernen. Wie heißt du?",
                "pt" => "Olá! Prazer em conhecer você. Como você se chama?",
                "es" => "¡Hola! Encantado de conocerte. ¿Cómo te llamas?",
                "it" => "Ciao! Piacere di conoscerti. Come ti chiami?",
                _ => englishOpeningLine
            };
        }

        return language.Id switch
        {
            "fr" => "Commençons simplement. Que veux-tu dire en premier ?",
            "de" => "Fangen wir einfach an. Was möchtest du zuerst sagen?",
            "pt" => "Vamos começar de forma simples. O que você quer dizer primeiro?",
            "es" => "Empecemos de forma sencilla. ¿Qué quieres decir primero?",
            "it" => "Cominciamo in modo semplice. Che cosa vuoi dire per prima cosa?",
            _ => englishOpeningLine
        };
    }

    public static string BuildCustomContextStartMessage(string userMessage, string openingLine, StudyLanguageDefinition studyLanguage)
    {
        var language = ResolveLanguage(studyLanguage);
        var trimmed = userMessage.Trim();
        return language.Id switch
        {
            "fr" => $"Bonne idée. Restons simples : {trimmed}.\n\n{openingLine}",
            "de" => $"Gute Idee. Halten wir es einfach: {trimmed}.\n\n{openingLine}",
            "pt" => $"Boa ideia. Vamos manter simples: {trimmed}.\n\n{openingLine}",
            "es" => $"Buena idea. Mantengámoslo sencillo: {trimmed}.\n\n{openingLine}",
            "it" => $"Buona idea. Manteniamola semplice: {trimmed}.\n\n{openingLine}",
            _ => $"Good idea. Let's keep it simple: {trimmed}.\n\n{openingLine}"
        };
    }

    public static string BuildInvalidContextRedirect(string englishRedirect, string selectedSubtopicTitle, StudyLanguageDefinition studyLanguage)
    {
        var language = ResolveLanguage(studyLanguage);
        if (IsEnglish(language))
        {
            return englishRedirect;
        }

        var subtopic = AdaptShortScenarioText(selectedSubtopicTitle, language).ToLower(CultureInfo.CurrentCulture);
        return language.Id switch
        {
            "fr" => $"C’est intéressant, mais cette leçon porte sur {subtopic}. Choisis une situation qui correspond à cette leçon.",
            "de" => $"Das klingt interessant, aber in dieser Lektion geht es um {subtopic}. Bitte wähle eine passende Situation.",
            "pt" => $"Isso parece interessante, mas esta lição é sobre {subtopic}. Escolha uma situação que combine com a lição.",
            "es" => $"Suena interesante, pero esta lección trata de {subtopic}. Elige una situación que encaje con la lección.",
            "it" => $"Sembra interessante, ma questa lezione riguarda {subtopic}. Scegli una situazione adatta alla lezione.",
            _ => englishRedirect
        };
    }

    public static string BuildSetupContextHint(IEnumerable<ContextVariant> variants, string selectedSubtopicTitle, StudyLanguageDefinition studyLanguage)
    {
        var language = ResolveLanguage(studyLanguage);
        var titles = variants
            .Take(3)
            .Select(variant => $"\"{AdaptShortScenarioText(variant.Title, language)}\"")
            .ToArray();

        if (IsEnglish(language))
        {
            return titles.Length == 0
                ? $"Choose a simple situation about {selectedSubtopicTitle.ToLowerInvariant()}."
                : $"You can choose: {string.Join(", ", titles)}.";
        }

        var subtopic = AdaptShortScenarioText(selectedSubtopicTitle, language).ToLower(CultureInfo.CurrentCulture);
        return language.Id switch
        {
            "fr" => titles.Length == 0 ? $"Choisis une situation simple sur {subtopic}." : $"Tu peux choisir : {string.Join(", ", titles)}.",
            "de" => titles.Length == 0 ? $"Wähle eine einfache Situation zu {subtopic}." : $"Du kannst wählen: {string.Join(", ", titles)}.",
            "pt" => titles.Length == 0 ? $"Escolha uma situação simples sobre {subtopic}." : $"Você pode escolher: {string.Join(", ", titles)}.",
            "es" => titles.Length == 0 ? $"Elige una situación sencilla sobre {subtopic}." : $"Puedes elegir: {string.Join(", ", titles)}.",
            "it" => titles.Length == 0 ? $"Scegli una situazione semplice su {subtopic}." : $"Puoi scegliere: {string.Join(", ", titles)}.",
            _ => titles.Length == 0 ? $"Choose a simple situation about {selectedSubtopicTitle.ToLowerInvariant()}." : $"You can choose: {string.Join(", ", titles)}."
        };
    }

    public static string BuildExampleHint(string englishHint, StudyLanguageDefinition studyLanguage)
    {
        var language = ResolveLanguage(studyLanguage);
        if (IsEnglish(language))
        {
            return englishHint.Trim();
        }

        return language.Id switch
        {
            "fr" => "Essaie une phrase simple en français, par exemple : Je m’appelle [ton nom].",
            "de" => "Versuche einen einfachen Satz auf Deutsch, zum Beispiel: Ich heiße [dein Name].",
            "pt" => "Tente uma frase simples em português, por exemplo: Eu me chamo [seu nome].",
            "es" => "Prueba una frase sencilla en español, por ejemplo: Me llamo [tu nombre].",
            "it" => "Prova una frase semplice in italiano, per esempio: Mi chiamo [il tuo nome].",
            _ => englishHint.Trim()
        };
    }

    public static string BuildFinalLessonMessage(string englishFinalMessage, StudyLanguageDefinition studyLanguage)
    {
        var language = ResolveLanguage(studyLanguage);
        if (IsEnglish(language))
        {
            return englishFinalMessage;
        }

        return language.Id switch
        {
            "fr" => "Très bien travaillé. La leçon est terminée : ouvre le résumé pour revoir tes points forts et les prochaines étapes.",
            "de" => "Sehr gut gemacht. Die Lektion ist beendet: Öffne die Zusammenfassung, um deine Stärken und nächsten Schritte zu sehen.",
            "pt" => "Muito bom trabalho. A lição terminou: abra o resumo para revisar seus pontos fortes e os próximos passos.",
            "es" => "Muy buen trabajo. La lección ha terminado: abre el resumen para revisar tus puntos fuertes y los próximos pasos.",
            "it" => "Ottimo lavoro. La lezione è finita: apri il riepilogo per rivedere i tuoi punti forti e i prossimi passi.",
            _ => englishFinalMessage
        };
    }

    public static string AdaptShortScenarioText(string value, StudyLanguageDefinition studyLanguage)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var language = ResolveLanguage(studyLanguage);
        if (IsEnglish(language))
        {
            return value.Trim();
        }

        var normalized = value.Trim();
        var key = normalized.ToLowerInvariant();
        return language.Id switch
        {
            "fr" => key switch
            {
                "introductions" => "les présentations",
                "meeting a new neighbor" => "Rencontrer un nouveau voisin",
                "first day at a language school" => "Premier jour dans une école de langue",
                "meeting someone at a hobby club" => "Rencontrer quelqu’un dans un club de loisirs",
                "meeting a colleague in the break room" => "Rencontrer un collègue dans la salle de pause",
                "joining an online english meeting" => "Rejoindre une réunion en ligne",
                _ => normalized
            },
            "de" => key switch
            {
                "introductions" => "Vorstellungen",
                "meeting a new neighbor" => "Einen neuen Nachbarn treffen",
                "first day at a language school" => "Erster Tag in einer Sprachschule",
                "meeting someone at a hobby club" => "Jemanden in einem Hobbyclub treffen",
                _ => normalized
            },
            "pt" => key switch
            {
                "introductions" => "apresentações",
                "meeting a new neighbor" => "Conhecer um novo vizinho",
                "first day at a language school" => "Primeiro dia em uma escola de idiomas",
                "meeting someone at a hobby club" => "Conhecer alguém em um clube de hobby",
                _ => normalized
            },
            "es" => key switch
            {
                "introductions" => "presentaciones",
                "meeting a new neighbor" => "Conocer a un nuevo vecino",
                "first day at a language school" => "Primer día en una escuela de idiomas",
                "meeting someone at a hobby club" => "Conocer a alguien en un club de aficiones",
                _ => normalized
            },
            "it" => key switch
            {
                "introductions" => "presentazioni",
                "meeting a new neighbor" => "Conoscere un nuovo vicino",
                "first day at a language school" => "Primo giorno in una scuola di lingue",
                "meeting someone at a hobby club" => "Conoscere qualcuno in un club di hobby",
                _ => normalized
            },
            _ => normalized
        };
    }

    private static string AdaptGoal(LessonScenario lessonScenario, StudyLanguageDefinition language)
    {
        if (IsIntroductionsLesson(lessonScenario))
        {
            return language.Id switch
            {
                "fr" => "tu vas apprendre à dire ton nom, d’où tu viens et à poser des questions simples.",
                "de" => "du lernst, deinen Namen zu sagen, woher du kommst, und einfache Fragen zu stellen.",
                "pt" => "você vai aprender a dizer seu nome, de onde você é, e fazer perguntas simples.",
                "es" => "aprenderás a decir tu nombre, de dónde eres y a hacer preguntas sencillas.",
                "it" => "imparerai a dire il tuo nome, da dove vieni e a fare domande semplici.",
                _ => lessonScenario.LearningGoal.Goal
            };
        }

        var goal = string.IsNullOrWhiteSpace(lessonScenario.LearningGoal.Goal)
            ? lessonScenario.Metadata.Subtopic
            : lessonScenario.LearningGoal.Goal;
        return language.Id switch
        {
            "fr" => $"pratiquer cette situation en {language.TutorInstructionName} : {goal}.",
            "de" => $"diese Situation auf {language.TutorInstructionName} üben: {goal}.",
            "pt" => $"praticar esta situação em {language.TutorInstructionName}: {goal}.",
            "es" => $"practicar esta situación en {language.TutorInstructionName}: {goal}.",
            "it" => $"praticare questa situazione in {language.TutorInstructionName}: {goal}.",
            _ => goal
        };
    }

    private static string LocalizeChooseSimpleSituation(StudyLanguageDefinition language, string subtopic)
    {
        return language.Id switch
        {
            "fr" => $"Choisis une situation simple sur {subtopic}.",
            "de" => $"Wähle eine einfache Situation zu {subtopic}.",
            "pt" => $"Escolha uma situação simples sobre {subtopic}.",
            "es" => $"Elige una situación sencilla sobre {subtopic}.",
            "it" => $"Scegli una situazione semplice su {subtopic}.",
            _ => $"Choose a simple situation about {subtopic}."
        };
    }

    private static bool IsIntroductionsLesson(LessonScenario lessonScenario)
    {
        return string.Equals(lessonScenario.Metadata.Subtopic, "Introductions", StringComparison.OrdinalIgnoreCase)
            || string.Equals(lessonScenario.Id, "everyday_english_introductions", StringComparison.OrdinalIgnoreCase);
    }

    private static StudyLanguageDefinition ResolveLanguage(StudyLanguageDefinition? studyLanguage)
    {
        return StudyLanguageCatalog.GetById(studyLanguage?.Id);
    }

    private static bool IsEnglish(StudyLanguageDefinition language)
    {
        return string.Equals(language.Id, StudyLanguageCatalog.DefaultStudyLanguageId, StringComparison.OrdinalIgnoreCase);
    }
}
