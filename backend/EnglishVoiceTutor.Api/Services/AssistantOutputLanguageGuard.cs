using System.Text.RegularExpressions;
using EnglishVoiceTutor.Api.Models;
using EnglishVoiceTutor.Shared.StudyLanguages;

namespace EnglishVoiceTutor.Api.Services;

public static partial class AssistantOutputLanguageGuard
{
    private static readonly string[] LanguageSwitchRequestMarkers =
    [
        "speak finnish",
        "speak russian",
        "speak spanish",
        "can you speak russian",
        "can you speak finnish",
        "can you speak spanish",
        "puhu suomea",
        "говори по-русски",
        "говорите по-русски"
    ];

    private static readonly string[] NonEnglishOutputMarkers =
    [
        "puhun",
        "suomea",
        "kiitos",
        "hei ",
        "hola",
        "gracias",
        "por favor",
        "привет",
        "спасибо",
        "хорошо",
        "говорю"
    ];

    public static bool IsLanguageSwitchRequest(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        var normalized = text.Trim().ToLowerInvariant();
        return LanguageSwitchRequestMarkers.Any(marker => normalized.Contains(marker, StringComparison.OrdinalIgnoreCase));
    }

    public static bool IsClearlyNonEnglishTutorOutput(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        var normalized = text.Trim().ToLowerInvariant();
        var letterCount = LetterRegex().Matches(normalized).Count;
        if (letterCount < 12)
        {
            return false;
        }

        var cyrillicCount = CyrillicRegex().Matches(normalized).Count;
        if (cyrillicCount >= 4 && cyrillicCount * 2 >= letterCount)
        {
            return true;
        }

        var markerHits = NonEnglishOutputMarkers.Count(marker => normalized.Contains(marker, StringComparison.OrdinalIgnoreCase));
        return markerHits >= 2;
    }

    public static LessonChatResponse CreateSafeTargetLanguageFallback(LessonChatRequest request, LessonChatResponse originalReply)
    {
        return new LessonChatResponse
        {
            BotReply = BuildSafeTargetLanguageReply(request),
            Feedback = originalReply.Feedback,
            IsLessonComplete = originalReply.IsLessonComplete
        };
    }

    public static string BuildSafeTargetLanguageReply(LessonChatRequest request)
    {
        var targetLanguage = StudyLanguageCatalog.GetById(request.TargetLanguageId);
        var languageName = targetLanguage.LanguageLockName;
        var level = string.IsNullOrWhiteSpace(request.SelectedLevel) ? request.Level : request.SelectedLevel;

        if (string.Equals(targetLanguage.Id, "es", StringComparison.OrdinalIgnoreCase))
        {
            return level.StartsWith("a1", StringComparison.OrdinalIgnoreCase)
                ? "Practiquemos en español. ¿Qué quieres decir en esta situación?"
                : "Mantengamos esta lección en español. Puedo ayudarte con esta situación en español.";
        }

        if (string.Equals(targetLanguage.Id, "fr", StringComparison.OrdinalIgnoreCase))
        {
            return level.StartsWith("a1", StringComparison.OrdinalIgnoreCase)
                ? "Pratiquons en français. Que veux-tu dire dans cette situation ?"
                : "Gardons cette leçon en français. Je peux t'aider avec cette situation en français.";
        }

        if (string.Equals(targetLanguage.Id, "de", StringComparison.OrdinalIgnoreCase))
        {
            return level.StartsWith("a1", StringComparison.OrdinalIgnoreCase)
                ? "Üben wir auf Deutsch. Was möchtest du in dieser Situation sagen?"
                : "Lass uns diese Lektion auf Deutsch halten. Ich kann dir mit dieser Situation auf Deutsch helfen.";
        }

        if (string.Equals(targetLanguage.Id, "pt", StringComparison.OrdinalIgnoreCase))
        {
            return level.StartsWith("a1", StringComparison.OrdinalIgnoreCase)
                ? "Vamos praticar em português. O que você quer dizer nesta situação?"
                : "Vamos manter esta lição em português. Posso ajudar você com esta situação em português.";
        }

        if (string.Equals(targetLanguage.Id, "it", StringComparison.OrdinalIgnoreCase))
        {
            return level.StartsWith("a1", StringComparison.OrdinalIgnoreCase)
                ? "Esercitiamoci in italiano. Che cosa vuoi dire in questa situazione?"
                : "Manteniamo questa lezione in italiano. Posso aiutarti con questa situazione in italiano.";
        }

        if (level.StartsWith("a1", StringComparison.OrdinalIgnoreCase))
        {
            if (request.Subtopic.Contains("introdu", StringComparison.OrdinalIgnoreCase) || request.TopicTitle.Contains("Everyday", StringComparison.OrdinalIgnoreCase))
            {
                return "Let's practice in English. What's your name?";
            }

            return "Let's practice in English. Please say it in English.";
        }

        if (level.StartsWith("a2", StringComparison.OrdinalIgnoreCase))
        {
            return "Let's use English. Please say it in English.";
        }

        return $"Let's keep this lesson in {languageName}. I can help you with this situation in {languageName}.";
    }

    [GeneratedRegex(@"\p{L}")]
    private static partial Regex LetterRegex();

    public static string BuildSafeTargetLanguageReply(LessonChatRequest request)
    {
        var targetLanguage = StudyLanguageCatalog.GetById(request.TargetLanguageId);
        var languageName = targetLanguage.LanguageLockName;
        var level = string.IsNullOrWhiteSpace(request.SelectedLevel) ? request.Level : request.SelectedLevel;

        if (string.Equals(targetLanguage.Id, "es", StringComparison.OrdinalIgnoreCase))
        {
            return level.StartsWith("a1", StringComparison.OrdinalIgnoreCase)
                ? "Practiquemos en español. ¿Qué quieres decir en esta situación?"
                : "Mantengamos esta lección en español. Puedo ayudarte con esta situación en español.";
        }

        if (string.Equals(targetLanguage.Id, "fr", StringComparison.OrdinalIgnoreCase))
        {
            return level.StartsWith("a1", StringComparison.OrdinalIgnoreCase)
                ? "Pratiquons en français. Que veux-tu dire dans cette situation ?"
                : "Gardons cette leçon en français. Je peux t'aider avec cette situation en français.";
        }

        if (string.Equals(targetLanguage.Id, "de", StringComparison.OrdinalIgnoreCase))
        {
            return level.StartsWith("a1", StringComparison.OrdinalIgnoreCase)
                ? "Üben wir auf Deutsch. Was möchtest du in dieser Situation sagen?"
                : "Lass uns diese Lektion auf Deutsch halten. Ich kann dir mit dieser Situation auf Deutsch helfen.";
        }

        if (string.Equals(targetLanguage.Id, "pt", StringComparison.OrdinalIgnoreCase))
        {
            return level.StartsWith("a1", StringComparison.OrdinalIgnoreCase)
                ? "Vamos praticar em português. O que você quer dizer nesta situação?"
                : "Vamos manter esta lição em português. Posso ajudar você com esta situação em português.";
        }

        if (string.Equals(targetLanguage.Id, "it", StringComparison.OrdinalIgnoreCase))
        {
            return level.StartsWith("a1", StringComparison.OrdinalIgnoreCase)
                ? "Esercitiamoci in italiano. Che cosa vuoi dire in questa situazione?"
                : "Manteniamo questa lezione in italiano. Posso aiutarti con questa situazione in italiano.";
        }

        if (level.StartsWith("a1", StringComparison.OrdinalIgnoreCase))
        {
            if (request.Subtopic.Contains("introdu", StringComparison.OrdinalIgnoreCase) || request.TopicTitle.Contains("Everyday", StringComparison.OrdinalIgnoreCase))
            {
                return "Let's practice in English. What's your name?";
            }

            return "Let's practice in English. Please say it in English.";
        }

        if (level.StartsWith("a2", StringComparison.OrdinalIgnoreCase))
        {
            return "Let's use English. Please say it in English.";
        }

        return $"Let's keep this lesson in {languageName}. I can help you with this situation in {languageName}.";
    }

    [GeneratedRegex(@"\p{IsCyrillic}")]
    private static partial Regex CyrillicRegex();
}
