using System.Text;
using EnglishVoiceTutor.Api.Constants;
using EnglishVoiceTutor.Api.Models;

namespace EnglishVoiceTutor.Api.Services;

public sealed class LessonPromptBuilder
{
    private const string LessonContextHeader = "Lesson context (already selected by learner):";
    private const string TutorAvatarProfileHeader = "Tutor avatar profile (stable identity):";
    private const string LearnerProfileHeader = "Learner profile:";
    private const string LessonLengthHeader = "Lesson length metadata:";
    private const string ActiveLevelProfileHeader = "Active level profile:";
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
        AppendLessonLength(prompt, request);
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
        prompt.AppendLine("Do not ask the learner to choose a topic or context.");
        prompt.AppendLine("Stay inside the selected roleplay context.");
        prompt.AppendLine("Keep the base scenario goal and topic fixed; do not turn the lesson into unrelated free conversation.");
        prompt.AppendLine("Adapt difficulty to the selected level and active level profile.");
        prompt.AppendLine("For A1, keep output very simple and avoid difficult follow-ups.");
        prompt.AppendLine("For A1/A2, ask only one question at a time.");
        prompt.AppendLine("For B1/B2, you may use slightly richer natural responses, but every turn must stay inside the selected scenario.");
        prompt.AppendLine("The setup and context choice are already complete and did not count as lesson turns.");
        prompt.AppendLine("Do not ask for native language.");

        if (LessonLimitHelper.ShouldEndLessonNow(request))
        {
            prompt.AppendLine("This is the hard-limit final turn. Give a short friendly closing message and do not ask a new question.");
        }
        else if (LessonLimitHelper.ShouldStartWrappingUp(request))
        {
            prompt.AppendLine("The lesson is in wrap-up. Continue the selected scenario, but gently guide the learner toward finishing within the remaining turns.");
        }
        else
        {
            prompt.AppendLine("Continue the dialogue naturally with one next question in the same scenario.");
        }

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
        prompt.AppendLine("Give one short hint the learner can use next in this exact situation, following the active level profile hint strategy.");
        prompt.AppendLine("A1: a fuller sentence starter is okay. A2: use a less complete sentence starter. B1: suggest structure or a useful phrase. B2: suggest natural direction or tone.");
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
        prompt.AppendLine($"- Level: {ChooseFirstNonEmpty(request.Level, request.SelectedLevel)}");
        prompt.AppendLine($"- Topic: {ChooseFirstNonEmpty(request.Topic, request.TopicTitle)}");
        prompt.AppendLine($"- Situation/Subtopic: {ChooseFirstNonEmpty(request.Subtopic, request.SubtopicTitle)}");

        if (!string.IsNullOrWhiteSpace(request.LessonScenarioId))
        {
            prompt.AppendLine($"- Lesson scenario id: {request.LessonScenarioId}");
        }

        if (!string.IsNullOrWhiteSpace(request.LessonGoal))
        {
            prompt.AppendLine($"- Lesson goal: {request.LessonGoal}");
        }

        if (!string.IsNullOrWhiteSpace(request.SelectedContextTitle))
        {
            prompt.AppendLine($"- Selected roleplay context: {request.SelectedContextTitle}");
        }

        if (!string.IsNullOrWhiteSpace(request.SelectedContextVariantId))
        {
            prompt.AppendLine($"- Selected context variant id: {request.SelectedContextVariantId}");
        }

        if (!string.IsNullOrWhiteSpace(request.SelectedContextOpeningLine))
        {
            prompt.AppendLine($"- Context opening line already shown by tutor: {request.SelectedContextOpeningLine}");
        }

        if (request.TargetLanguageKeyPhrases.Count > 0)
        {
            prompt.AppendLine($"- Target key phrases: {string.Join(", ", request.TargetLanguageKeyPhrases)}");
        }

        if (request.GrammarFocus.Count > 0)
        {
            prompt.AppendLine($"- Grammar focus: {string.Join(", ", request.GrammarFocus)}");
        }

        AppendActiveLevelProfile(prompt, request);

        if (!string.IsNullOrWhiteSpace(request.FeedbackRulesSummary))
        {
            prompt.AppendLine($"- Feedback rules: {request.FeedbackRulesSummary}");
        }

        if (includeNativeLanguage)
        {
            prompt.AppendLine($"- Native language: {request.NativeLanguageName}");
        }

        prompt.AppendLine();
    }


    private static void AppendActiveLevelProfile(StringBuilder prompt, LessonChatRequest request)
    {
        var hasProfile = !string.IsNullOrWhiteSpace(request.ActiveLevelProfileDifficultyNotes)
            || !string.IsNullOrWhiteSpace(request.ActiveLevelProfileTutorLanguageStyle)
            || !string.IsNullOrWhiteSpace(request.ActiveLevelProfileExpectedUserResponse)
            || !string.IsNullOrWhiteSpace(request.ActiveLevelProfileFeedbackStrictness)
            || !string.IsNullOrWhiteSpace(request.ActiveLevelProfileHintStrategy)
            || !string.IsNullOrWhiteSpace(request.ActiveLevelProfileCorrectionPriority)
            || !string.IsNullOrWhiteSpace(request.ActiveLevelProfileConversationDepth)
            || !string.IsNullOrWhiteSpace(request.ActiveLevelProfileExampleGoodAnswer)
            || !string.IsNullOrWhiteSpace(request.ActiveLevelProfileExampleStretchAnswer)
            || request.ActiveLevelProfileAddedKeyPhrases.Count > 0
            || request.ActiveLevelProfileAddedUsefulConstructions.Count > 0
            || request.ActiveLevelProfileAddedGrammarFocus.Count > 0;

        if (!hasProfile)
        {
            return;
        }

        prompt.AppendLine(ActiveLevelProfileHeader);
        prompt.AppendLine($"- Selected level: {ChooseFirstNonEmpty(request.SelectedLevel, request.Level)}");

        if (!string.IsNullOrWhiteSpace(request.ActiveLevelProfileDifficultyNotes))
        {
            prompt.AppendLine($"- Difficulty notes: {request.ActiveLevelProfileDifficultyNotes}");
        }

        if (!string.IsNullOrWhiteSpace(request.ActiveLevelProfileTutorLanguageStyle))
        {
            prompt.AppendLine($"- Tutor language style: {request.ActiveLevelProfileTutorLanguageStyle}");
        }

        if (!string.IsNullOrWhiteSpace(request.ActiveLevelProfileExpectedUserResponse))
        {
            prompt.AppendLine($"- Expected user response: {request.ActiveLevelProfileExpectedUserResponse}");
        }

        if (!string.IsNullOrWhiteSpace(request.ActiveLevelProfileFeedbackStrictness))
        {
            prompt.AppendLine($"- Feedback strictness: {request.ActiveLevelProfileFeedbackStrictness}");
        }

        if (!string.IsNullOrWhiteSpace(request.ActiveLevelProfileHintStrategy))
        {
            prompt.AppendLine($"- Hint strategy: {request.ActiveLevelProfileHintStrategy}");
        }

        if (!string.IsNullOrWhiteSpace(request.ActiveLevelProfileCorrectionPriority))
        {
            prompt.AppendLine($"- Correction priority: {request.ActiveLevelProfileCorrectionPriority}");
        }

        if (!string.IsNullOrWhiteSpace(request.ActiveLevelProfileConversationDepth))
        {
            prompt.AppendLine($"- Conversation depth: {request.ActiveLevelProfileConversationDepth}");
        }

        if (!string.IsNullOrWhiteSpace(request.ActiveLevelProfileExampleGoodAnswer))
        {
            prompt.AppendLine($"- Example good answer: {request.ActiveLevelProfileExampleGoodAnswer}");
        }

        if (!string.IsNullOrWhiteSpace(request.ActiveLevelProfileExampleStretchAnswer))
        {
            prompt.AppendLine($"- Example stretch answer: {request.ActiveLevelProfileExampleStretchAnswer}");
        }

        if (request.ActiveLevelProfileAddedKeyPhrases.Count > 0)
        {
            prompt.AppendLine($"- Level-added key phrases: {string.Join(", ", request.ActiveLevelProfileAddedKeyPhrases)}");
        }

        if (request.ActiveLevelProfileAddedUsefulConstructions.Count > 0)
        {
            prompt.AppendLine($"- Level-added useful constructions: {string.Join(", ", request.ActiveLevelProfileAddedUsefulConstructions)}");
        }

        if (request.ActiveLevelProfileAddedGrammarFocus.Count > 0)
        {
            prompt.AppendLine($"- Level-added grammar focus: {string.Join(", ", request.ActiveLevelProfileAddedGrammarFocus)}");
        }

        prompt.AppendLine();
    }

    private static void AppendLessonLength(StringBuilder prompt, LessonChatRequest request)
    {
        prompt.AppendLine(LessonLengthHeader);
        prompt.AppendLine($"- Learner turn count including latest message: {Math.Max(request.UserTurnNumber, request.LearnerTurnCount)}");
        prompt.AppendLine($"- Soft learner turn limit: {LessonLimitHelper.GetSoftLearnerTurnLimit(request)}");
        prompt.AppendLine($"- Hard learner turn limit: {LessonLimitHelper.GetHardLearnerTurnLimit(request)}");
        prompt.AppendLine("- Setup/context selection is already complete and did not count as a lesson turn.");
        prompt.AppendLine($"- Remaining learner turns after latest message: {LessonLimitHelper.GetRemainingLearnerTurns(request)}");
        prompt.AppendLine($"- shouldStartWrappingUp: {LessonLimitHelper.ShouldStartWrappingUp(request)}");
        prompt.AppendLine($"- shouldEndLessonNow: {LessonLimitHelper.ShouldEndLessonNow(request)}");
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

    private static string ChooseFirstNonEmpty(string? primary, string fallback)
    {
        return string.IsNullOrWhiteSpace(primary)
            ? NormalizeOptionalText(fallback)
            : primary.Trim();
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
