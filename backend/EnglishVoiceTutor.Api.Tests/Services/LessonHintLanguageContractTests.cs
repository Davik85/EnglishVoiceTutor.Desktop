using EnglishVoiceTutor.Api.Constants;
using EnglishVoiceTutor.Api.Models;
using EnglishVoiceTutor.Api.Services;
using EnglishVoiceTutor.Shared.StudyLanguages;
using Microsoft.Extensions.Logging.Abstractions;

namespace EnglishVoiceTutor.Api.Tests.Services;

public sealed class LessonHintLanguageContractTests
{
    private const string LegacyHintText = "You can say: Hi, my name is David.";

    [Fact]
    public void HintSystemInstructionsDelegateOutputLanguageToSelectedTargetStudyLanguage()
    {
        var instructions = OpenAiConstants.LessonHintSystemInstructions;

        Assert.DoesNotContain("English only", instructions, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("English lesson hint writer", instructions, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("selected target study language is supplied in the request input", instructions, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("hint only in that target study language", instructions, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Do not switch to another language", instructions, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [MemberData(nameof(SupportedPromptLanguageCases))]
    public void BuildHintInputCarriesExplicitTargetStudyLanguageForEverySupportedLanguage(
        string languageId,
        string instructionName)
    {
        var prompt = CreateBuilder().BuildHintInput(CreateRequest(languageId));

        Assert.Contains("TARGET STUDY LANGUAGE:", prompt, StringComparison.Ordinal);
        Assert.Contains($"The learner is practicing {instructionName}.", prompt, StringComparison.Ordinal);
        Assert.Contains($"All tutor-facing lesson content must be in {instructionName}.", prompt, StringComparison.Ordinal);
        Assert.Contains($"Use {instructionName} for tutor replies, roleplay, hints, feedback, corrections, examples, and summary.", prompt, StringComparison.Ordinal);
        Assert.Contains("Do not switch to another language even if the learner asks.", prompt, StringComparison.Ordinal);

        if (!string.Equals(languageId, StudyLanguageCatalog.DefaultStudyLanguageId, StringComparison.Ordinal))
        {
            Assert.DoesNotContain("All tutor-facing lesson content must be in English.", prompt, StringComparison.Ordinal);
            Assert.DoesNotContain("Always speak English in tutor messages.", prompt, StringComparison.Ordinal);
        }
    }

    [Theory]
    [InlineData("")]
    [InlineData("unknown")]
    public void BuildHintInputDefaultsMissingOrUnknownLanguageIdToEnglish(string languageId)
    {
        var prompt = CreateBuilder().BuildHintInput(CreateRequest(languageId));

        Assert.Contains("The learner is practicing English.", prompt, StringComparison.Ordinal);
        Assert.Contains("All tutor-facing lesson content must be in English.", prompt, StringComparison.Ordinal);
        Assert.Contains("Always speak English in tutor messages.", prompt, StringComparison.Ordinal);
    }

    [Theory]
    [MemberData(nameof(FallbackLanguageCases))]
    public async Task DeterministicFallbackUsesSelectedSupportedStudyLanguage(
        string languageId,
        string expectedHint)
    {
        var response = await new MockLessonHintService().CreateHintAsync(CreateRequest(languageId), CancellationToken.None);

        Assert.False(string.IsNullOrWhiteSpace(response.HintText));
        Assert.Equal(expectedHint, response.HintText);
        Assert.DoesNotContain("David", response.HintText, StringComparison.OrdinalIgnoreCase);

        if (!string.Equals(languageId, StudyLanguageCatalog.DefaultStudyLanguageId, StringComparison.Ordinal))
        {
            Assert.NotEqual(LegacyHintText, response.HintText);
        }
    }

    public static IEnumerable<object[]> SupportedPromptLanguageCases() =>
        StudyLanguageCatalog.All.Select(language => new object[] { language.Id, language.TutorInstructionName });

    public static IEnumerable<object[]> FallbackLanguageCases()
    {
        yield return ["en", "Could you please say that again?"];
        yield return ["fr", "Pouvez-vous répéter, s'il vous plaît ?"];
        yield return ["de", "Könnten Sie das bitte wiederholen?"];
        yield return ["pt", "Pode repetir, por favor?"];
        yield return ["es", "¿Puede repetirlo, por favor?"];
        yield return ["it", "Può ripetere, per favore?"];
        yield return ["", "Could you please say that again?"];
        yield return ["unknown", "Could you please say that again?"];
    }

    private static LessonPromptBuilder CreateBuilder() => new(
        new TutorAvatarProfileProvider(NullLogger<TutorAvatarProfileProvider>.Instance));

    private static LessonChatRequest CreateRequest(string targetLanguageId) => new()
    {
        SelectedLevel = "A1",
        TopicTitle = "Everyday conversation",
        SubtopicTitle = "Asking for clarification",
        LastBotMessage = "Please tell me more.",
        UserMessage = string.Empty,
        TutorAvatarId = "lana",
        TargetLanguageId = targetLanguageId
    };
}
