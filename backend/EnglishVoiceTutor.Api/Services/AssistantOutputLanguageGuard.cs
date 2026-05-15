using System.Text.RegularExpressions;
using EnglishVoiceTutor.Api.Models;

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

    public static LessonChatResponse CreateSafeEnglishFallback(LessonChatRequest request, LessonChatResponse originalReply)
    {
        return new LessonChatResponse
        {
            BotReply = BuildSafeEnglishReply(request),
            Feedback = originalReply.Feedback,
            IsLessonComplete = originalReply.IsLessonComplete
        };
    }

    public static string BuildSafeEnglishReply(LessonChatRequest request)
    {
        var level = string.IsNullOrWhiteSpace(request.SelectedLevel) ? request.Level : request.SelectedLevel;
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

        return "Let's keep this lesson in English. I can help you with this situation in English.";
    }

    [GeneratedRegex(@"\p{L}")]
    private static partial Regex LetterRegex();

    [GeneratedRegex(@"\p{IsCyrillic}")]
    private static partial Regex CyrillicRegex();
}
