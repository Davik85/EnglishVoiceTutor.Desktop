using System.Text.RegularExpressions;
using EnglishVoiceTutor.Api.Models;

namespace EnglishVoiceTutor.Api.Services;

public sealed class TutorIdentityGuard
{
    private static readonly Regex SelfIntroductionRegex = new(
        @"\b(?i:(?:I\s*(?:am|'m)|my\s+name\s+is)\s+)(?<name>[A-Z][a-z]+)\b",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

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

    public LessonChatResponse PreventWrongTutorSelfIntroduction(LessonChatResponse response, TutorAvatarProfile activeTutorProfile)
    {
        var botReply = response.BotReply?.Trim() ?? string.Empty;
        var match = SelfIntroductionRegex.Match(botReply);
        if (!match.Success)
        {
            return response;
        }

        var generatedName = match.Groups["name"].Value;
        if (string.Equals(generatedName, activeTutorProfile.DisplayName, StringComparison.OrdinalIgnoreCase)
            || CommonWordsThatAreNotTutorNames.Contains(generatedName))
        {
            return response;
        }

        _logger.LogWarning(
            "Generated lesson-chat tutor self-introduction used a name that does not match the active tutor profile. ActiveTutor={ActiveTutor}; GeneratedName={GeneratedName}.",
            activeTutorProfile.DisplayName,
            generatedName);

        var correctedReply = SelfIntroductionRegex.Replace(
            botReply,
            matchResult => matchResult.Value[..^generatedName.Length] + activeTutorProfile.DisplayName,
            count: 1);

        return new LessonChatResponse
        {
            BotReply = correctedReply,
            Feedback = response.Feedback,
            IsLessonComplete = response.IsLessonComplete
        };
    }
}
