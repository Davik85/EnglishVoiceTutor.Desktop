using System.Text.RegularExpressions;
using EnglishVoiceTutor.Api.Models;

namespace EnglishVoiceTutor.Api.Services;

public sealed class TutorIdentityGuard
{
    private const string GeneratedNameGroupName = "name";

    private static readonly Regex SelfIntroductionRegex = new(
        @"\b(?:(?:hi|hello)\s*,?\s*)?(?:(?:I\s*(?:am|'m))|(?:my\s+name\s+is))\s+(?<name>[A-Z][a-z]+)\b",
        RegexOptions.CultureInvariant | RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly HashSet<string> CommonWordsThatAreNotTutorNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "able",
        "about",
        "available",
        "clear",
        "fine",
        "going",
        "good",
        "great",
        "happy",
        "here",
        "learning",
        "ready",
        "sorry",
        "sure",
        "working"
    };

    private readonly ILogger<TutorIdentityGuard> _logger;

    public TutorIdentityGuard(ILogger<TutorIdentityGuard> logger)
    {
        _logger = logger;
    }

    public LessonChatResponse PreventWrongTutorSelfIntroduction(
        LessonChatResponse response,
        TutorAvatarProfile activeTutorProfile,
        string operationSource = "lesson_chat")
    {
        var botReply = response.BotReply?.Trim() ?? string.Empty;
        var match = SelfIntroductionRegex.Match(botReply);
        if (!TryResolveMismatch(match, activeTutorProfile.DisplayName, out var generatedName))
        {
            return response;
        }

        var correctedReply = ReplaceGeneratedName(botReply, match, generatedName, activeTutorProfile.DisplayName);
        if (string.Equals(correctedReply, botReply, StringComparison.Ordinal))
        {
            _logger.LogWarning(
                "TutorIdentityGuard detected a potential wrong tutor self-introduction but skipped correction. ActiveTutor={ActiveTutor}; GeneratedName={GeneratedName}; Source={Source}.",
                activeTutorProfile.DisplayName,
                generatedName,
                operationSource);
            return response;
        }

        _logger.LogWarning(
            "TutorIdentityGuard corrected wrong tutor self-introduction. ActiveTutor={ActiveTutor}; GeneratedName={GeneratedName}; Source={Source}.",
            activeTutorProfile.DisplayName,
            generatedName,
            operationSource);

        return new LessonChatResponse
        {
            BotReply = correctedReply,
            Feedback = response.Feedback,
            IsLessonComplete = response.IsLessonComplete
        };
    }

    internal static bool TryResolveMismatch(Match match, string activeTutorDisplayName, out string generatedName)
    {
        generatedName = string.Empty;
        if (!match.Success)
        {
            return false;
        }

        generatedName = match.Groups[GeneratedNameGroupName].Value;
        if (string.IsNullOrWhiteSpace(generatedName)
            || string.IsNullOrWhiteSpace(activeTutorDisplayName)
            || CommonWordsThatAreNotTutorNames.Contains(generatedName)
            || string.Equals(generatedName, activeTutorDisplayName, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return true;
    }

    internal static string ReplaceGeneratedName(string botReply, Match match, string generatedName, string activeTutorDisplayName)
    {
        var nameGroup = match.Groups[GeneratedNameGroupName];
        if (!nameGroup.Success || nameGroup.Length != generatedName.Length)
        {
            return botReply;
        }

        return botReply[..nameGroup.Index] + activeTutorDisplayName + botReply[(nameGroup.Index + nameGroup.Length)..];
    }
}
