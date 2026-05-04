using System.Text;
using EnglishVoiceTutor.Api.Models;

namespace EnglishVoiceTutor.Api.Services;

public sealed class LessonPromptBuilder
{
    private const string LessonContextHeader = "Lesson context (already selected by learner):";
    private const string UserMessageHeader = "Learner latest message:";
    private const string CurrentTurnTaskHeader = "Current turn task:";

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
}
