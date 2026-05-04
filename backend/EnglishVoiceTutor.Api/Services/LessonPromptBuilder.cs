using System.Text;
using EnglishVoiceTutor.Api.Models;

namespace EnglishVoiceTutor.Api.Services;

public sealed class LessonPromptBuilder
{
    private const string LessonContextHeader = "Lesson context (already selected by learner):";
    private const string UserMessageHeader = "Learner latest message:";
    private const string LearnerDraftHeader = "Learner draft / latest input:";
    private const string LastBotMessageHeader = "Latest bot message:";
    private const string CurrentTurnTaskHeader = "Current turn task:";
    private const string HintTaskHeader = "Hint task:";

    public string BuildInput(LessonChatRequest request)
    {
        var prompt = new StringBuilder();

        prompt.AppendLine(LessonContextHeader);
        prompt.AppendLine($"- Level: {request.SelectedLevel}");
        prompt.AppendLine($"- Topic: {request.TopicTitle}");
        prompt.AppendLine($"- Situation/Subtopic: {request.SubtopicTitle}");
        prompt.AppendLine($"- Native language: {request.NativeLanguageName}");
        prompt.AppendLine();

        prompt.AppendLine(UserMessageHeader);
        prompt.AppendLine(request.UserMessage);
        prompt.AppendLine();

        prompt.AppendLine(CurrentTurnTaskHeader);
        prompt.AppendLine("Respond to the learner's latest message as part of the selected situation.");
        prompt.AppendLine("Do not restart the lesson.");
        prompt.AppendLine("Do not ask the learner to choose a topic.");
        prompt.AppendLine("Do not ask for native language.");
        prompt.AppendLine("Continue the dialogue naturally with one next question in the same scenario.");

        return prompt.ToString();
    }

    public string BuildHintInput(LessonChatRequest request)
    {
        var prompt = new StringBuilder();

        prompt.AppendLine(LessonContextHeader);
        prompt.AppendLine($"- Level: {request.SelectedLevel}");
        prompt.AppendLine($"- Topic: {request.TopicTitle}");
        prompt.AppendLine($"- Situation/Subtopic: {request.SubtopicTitle}");
        prompt.AppendLine();

        prompt.AppendLine(LastBotMessageHeader);
        prompt.AppendLine(request.LastBotMessage);
        prompt.AppendLine();

        prompt.AppendLine(LearnerDraftHeader);
        prompt.AppendLine(request.UserMessage);
        prompt.AppendLine();

        prompt.AppendLine(HintTaskHeader);
        prompt.AppendLine("Give one short hint sentence the learner can say next in this exact situation.");
        prompt.AppendLine("The hint must answer or continue from the learner's point of view.");
        prompt.AppendLine("The hint should help the learner respond to the latest bot message.");
        prompt.AppendLine("If the learner's real name or personal detail is unknown, use placeholders.");
        prompt.AppendLine("Do not invent a learner name.");
        prompt.AppendLine("Do not use the tutor or bot name as the learner's name.");
        prompt.AppendLine("For introductions, prefer examples like: \"My name is [your name].\", \"I'm [your name].\", \"I'm from [your country].\"");

        return prompt.ToString();
    }
}
