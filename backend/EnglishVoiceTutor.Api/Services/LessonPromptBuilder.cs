using System.Text;
using EnglishVoiceTutor.Api.Constants;
using EnglishVoiceTutor.Api.Models;

namespace EnglishVoiceTutor.Api.Services;

public sealed class LessonPromptBuilder
{
    private const string LessonContextHeader = "Lesson context (already selected by learner):";
    private const string TutorAvatarProfileHeader = "Tutor avatar profile (stable identity):";
    private const string LearnerProfileHeader = "Learner profile:";
    private const string RecentConversationHeader = "Recent active lesson conversation context (oldest to newest):";
    private const string NoRecentConversationContext = "- No recent conversation messages were provided.";
    private const string UserMessageHeader = "Learner latest message:";
    private const string LearnerDraftHeader = "Learner draft / latest input:";
    private const string LastBotMessageHeader = "Latest bot message:";
    private const string CurrentTurnTaskHeader = "Current turn task:";
    private const string HintTaskHeader = "Hint task:";

    private readonly TutorAvatarProfileProvider _avatarProfileProvider;

    public LessonPromptBuilder(TutorAvatarProfileProvider avatarProfileProvider)
    {
        _avatarProfileProvider = avatarProfileProvider;
    }

    public string BuildInput(LessonChatRequest request)
    {
        var prompt = new StringBuilder();
        var avatarProfile = _avatarProfileProvider.GetById(request.TutorAvatarId);

        AppendLessonContext(prompt, request);
        AppendAvatarProfile(prompt, avatarProfile);
        AppendLearnerProfile(prompt, request);
        AppendRecentConversation(prompt, request.RecentMessages);

        prompt.AppendLine(UserMessageHeader);
        prompt.AppendLine(request.UserMessage);
        prompt.AppendLine();

        prompt.AppendLine(CurrentTurnTaskHeader);
        prompt.AppendLine($"Respond to the learner's latest message as {avatarProfile.DisplayName}, the selected tutor avatar, as part of the selected situation.");
        prompt.AppendLine("Use learner profile as stable context and recent conversation as active lesson context.");
        prompt.AppendLine("If the learner profile includes a display name, you may address the learner by name naturally, but do not repeat it in every message.");
        prompt.AppendLine("Do not ask for the learner's name if the learner profile already includes a display name.");
        prompt.AppendLine("If the learner profile includes a learning goal, use it as gentle context without overriding the selected topic or situation.");
        prompt.AppendLine("Do not restart the lesson.");
        prompt.AppendLine("Do not ask the learner to choose a topic.");
        prompt.AppendLine("Do not ask for native language.");
        prompt.AppendLine("Continue the dialogue naturally with one next question in the same scenario.");

        return prompt.ToString();
    }

    public string BuildHintInput(LessonChatRequest request)
    {
        var prompt = new StringBuilder();
        var avatarProfile = _avatarProfileProvider.GetById(request.TutorAvatarId);

        AppendLessonContext(prompt, request, includeNativeLanguage: false);
        AppendAvatarProfile(prompt, avatarProfile);
        AppendLearnerProfile(prompt, request);
        AppendRecentConversation(prompt, request.RecentMessages);

        prompt.AppendLine(LastBotMessageHeader);
        prompt.AppendLine(request.LastBotMessage);
        prompt.AppendLine();

        prompt.AppendLine(LearnerDraftHeader);
        prompt.AppendLine(request.UserMessage);
        prompt.AppendLine();

        prompt.AppendLine(HintTaskHeader);
        prompt.AppendLine("Give one short hint sentence the learner can say next in this exact situation.");
        prompt.AppendLine($"The hint must answer or continue from the learner's point of view, not {avatarProfile.DisplayName}'s point of view.");
        prompt.AppendLine("The hint should help the learner respond to the latest bot message and recent conversation.");
        prompt.AppendLine("If the learner profile includes a display name, hint examples may use that name when appropriate.");
        prompt.AppendLine("If the learner's real name or personal detail is unknown, use placeholders such as [your name].");
        prompt.AppendLine("Do not invent a learner name.");
        prompt.AppendLine($"Do not use {avatarProfile.DisplayName} or the tutor avatar name as the learner's name.");
        prompt.AppendLine("For introductions, prefer examples like: \"My name is [your name].\", \"I'm [your name].\", \"I'm from [your country].\"");

        return prompt.ToString();
    }

    private static void AppendLessonContext(StringBuilder prompt, LessonChatRequest request, bool includeNativeLanguage = true)
    {
        prompt.AppendLine(LessonContextHeader);
        prompt.AppendLine($"- Level: {request.SelectedLevel}");
        prompt.AppendLine($"- Topic: {request.TopicTitle}");
        prompt.AppendLine($"- Situation/Subtopic: {request.SubtopicTitle}");

        if (includeNativeLanguage)
        {
            prompt.AppendLine($"- Native language: {request.NativeLanguageName}");
        }

        prompt.AppendLine();
    }

    private static void AppendAvatarProfile(StringBuilder prompt, TutorAvatarProfile avatarProfile)
    {
        prompt.AppendLine(TutorAvatarProfileHeader);
        prompt.AppendLine($"- Id: {avatarProfile.Id}");
        prompt.AppendLine($"- Display name: {avatarProfile.DisplayName}");
        prompt.AppendLine($"- Age: {avatarProfile.Age}");
        prompt.AppendLine($"- Location: {avatarProfile.Location}");
        prompt.AppendLine($"- Role: {avatarProfile.Role}");
        prompt.AppendLine($"- Interests: {string.Join(", ", avatarProfile.Interests)}");
        prompt.AppendLine($"- Personality: {avatarProfile.PersonalitySummary}");
        prompt.AppendLine($"- Speaking style: {avatarProfile.SpeakingStyle}");
        prompt.AppendLine($"- Boundaries and lesson behavior: {avatarProfile.Boundaries}");
        prompt.AppendLine();
    }

    private static void AppendLearnerProfile(StringBuilder prompt, LessonChatRequest request)
    {
        var userDisplayName = NormalizeOptionalText(request.UserDisplayName);
        var learningGoal = NormalizeOptionalText(request.LearningGoal);

        if (string.IsNullOrWhiteSpace(userDisplayName) && string.IsNullOrWhiteSpace(learningGoal))
        {
            return;
        }

        prompt.AppendLine(LearnerProfileHeader);

        if (!string.IsNullOrWhiteSpace(userDisplayName))
        {
            prompt.AppendLine($"- Display name: {userDisplayName}");
        }

        if (!string.IsNullOrWhiteSpace(learningGoal))
        {
            prompt.AppendLine($"- Learning goal: {learningGoal}");
        }

        prompt.AppendLine();
    }

    private static void AppendRecentConversation(StringBuilder prompt, IReadOnlyList<RecentConversationMessage> recentMessages)
    {
        prompt.AppendLine(RecentConversationHeader);

        var relevantMessages = recentMessages
            .Where(message => !string.IsNullOrWhiteSpace(message.Text))
            .TakeLast(OpenAiConstants.RecentConversationMessagesLimit)
            .ToArray();

        if (relevantMessages.Length == 0)
        {
            prompt.AppendLine(NoRecentConversationContext);
            prompt.AppendLine();
            return;
        }

        foreach (var message in relevantMessages)
        {
            prompt.AppendLine($"- {NormalizeSender(message.Sender)}: {message.Text.Trim()}");
        }

        prompt.AppendLine();
    }

    private static string NormalizeOptionalText(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : value.Trim();
    }

    private static string NormalizeSender(string sender)
    {
        if (string.IsNullOrWhiteSpace(sender))
        {
            return "Unknown";
        }

        return sender.Trim();
    }
}
