using System.IO;
using System.Text.Json;
using EnglishVoiceTutor.Desktop.Constants;
using EnglishVoiceTutor.Desktop.Models;
using EnglishVoiceTutor.Desktop.Models.LessonContent;

namespace EnglishVoiceTutor.Desktop.Services;

public sealed class LessonContentService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly string contentRootPath;

    public LessonContentService()
        : this(AppContext.BaseDirectory)
    {
    }

    public LessonContentService(string baseDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(baseDirectory);
        contentRootPath = Path.Combine(baseDirectory, ContentConstants.ContentRootFolder);
    }

    public string ContentRootPath => contentRootPath;

    public LessonScenario LoadIntroductionsLessonScenario()
    {
        return LoadSharedLessonScenario(
            ContentConstants.EverydayEnglishFolderName,
            ContentConstants.IntroductionsFileName);
    }


    public LessonScenario LoadSharedLessonScenario(string topicFolderName, string lessonFileName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(topicFolderName);
        ArgumentException.ThrowIfNullOrWhiteSpace(lessonFileName);

        var lessonPath = Path.Combine(
            contentRootPath,
            ContentConstants.SharedLessonsFolderName,
            topicFolderName,
            lessonFileName);

        return LoadJsonFile<LessonScenario>(lessonPath, "shared lesson scenario");
    }

    public LessonScenario LoadLessonScenario(string levelFolderName, string topicFolderName, string lessonFileName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(levelFolderName);
        ArgumentException.ThrowIfNullOrWhiteSpace(topicFolderName);
        ArgumentException.ThrowIfNullOrWhiteSpace(lessonFileName);

        var lessonPath = Path.Combine(
            contentRootPath,
            ContentConstants.LessonsFolder,
            levelFolderName,
            topicFolderName,
            lessonFileName);

        return LoadJsonFile<LessonScenario>(lessonPath, "lesson scenario");
    }

    public LessonScenario LoadLessonScenarioByLevelTopicSubtopic(string levelFolderName, string topicFolderName, string subtopicFileName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(subtopicFileName);

        var lessonFileName = subtopicFileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase)
            ? subtopicFileName
            : $"{subtopicFileName}.json";

        return LoadLessonScenario(levelFolderName, topicFolderName, lessonFileName);
    }

    public LessonScenario LoadLessonScenarioFromPath(string lessonScenarioPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(lessonScenarioPath);
        return LoadJsonFile<LessonScenario>(lessonScenarioPath, "lesson scenario");
    }

    public TutorProfile LoadLanaTutorProfile()
    {
        return LoadTutorProfile(ContentConstants.LanaTutorId);
    }

    public TutorProfile LoadTutorProfile(string tutorId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tutorId);
        var canonicalTutorId = TutorAvatarOptions.ToCanonicalId(tutorId);

        var tutorPath = Path.Combine(
            contentRootPath,
            ContentConstants.TutorsFolder,
            $"{canonicalTutorId}.json");

        return LoadJsonFile<TutorProfile>(tutorPath, "tutor profile");
    }

    public string LoadLessonTutorBasePrompt()
    {
        return LoadPromptText(ContentConstants.LessonTutorBasePromptFileName);
    }

    public string LoadLessonSetupRulesPrompt()
    {
        return LoadPromptText(ContentConstants.LessonSetupRulesPromptFileName);
    }

    public string LoadLessonResponseRulesPrompt()
    {
        return LoadPromptText(ContentConstants.LessonResponseRulesPromptFileName);
    }

    public string LoadPromptText(string promptFileName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(promptFileName);

        var promptPath = Path.Combine(
            contentRootPath,
            ContentConstants.PromptsFolder,
            promptFileName);

        if (!File.Exists(promptPath))
        {
            throw new FileNotFoundException($"The prompt file was not found: {promptPath}", promptPath);
        }

        return File.ReadAllText(promptPath);
    }

    private static T LoadJsonFile<T>(string filePath, string contentDescription)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"The {contentDescription} file was not found: {filePath}", filePath);
        }

        var json = File.ReadAllText(filePath);
        var content = JsonSerializer.Deserialize<T>(json, JsonOptions);

        if (content is null)
        {
            throw new InvalidDataException($"The {contentDescription} file could not be read: {filePath}");
        }

        return content;
    }
}
