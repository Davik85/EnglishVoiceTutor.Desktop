using EnglishVoiceTutor.Api.Models;

namespace EnglishVoiceTutor.Api.Services;

public sealed class LessonPromptBuilder
{
    public string BuildInput(LessonChatRequest request)
    {
        return $"Level: {request.SelectedLevel}\n" +
               $"Topic: {request.TopicTitle}\n" +
               $"Subtopic: {request.SubtopicTitle}\n" +
               $"Native language: {request.NativeLanguageName}\n" +
               $"User message: {request.UserMessage}";
    }
}
