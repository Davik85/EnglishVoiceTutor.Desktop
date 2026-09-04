using EnglishVoiceTutor.Api.Models;

namespace EnglishVoiceTutor.Api.Tests.AdminUi;

public sealed class AdminAiModelTemperatureUiStaticTests
{
    private static readonly string AdminJs = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "../../../../EnglishVoiceTutor.Api/wwwroot/admin/admin.js"));

    [Fact]
    public void FourTextRolesRenderIndependentOmitTemperatureCheckboxes()
    {
        var fieldDefinitions = Slice("const aiModelFields = [", "let aiModelDraft = {};");

        Assert.Contains("[\"lessonTutorChatModel\", \"Lesson tutor chat model\", \"lessonTutorChatOmitTemperature\"]", fieldDefinitions);
        Assert.Contains("[\"feedbackCorrectionModel\", \"Feedback / correction model\", \"feedbackCorrectionOmitTemperature\"]", fieldDefinitions);
        Assert.Contains("[\"lessonHintModel\", \"Lesson hint model\", \"lessonHintOmitTemperature\"]", fieldDefinitions);
        Assert.Contains("[\"translationModel\", \"Translation model\", \"translationOmitTemperature\"]", fieldDefinitions);
        Assert.Contains("omitInput.type = \"checkbox\"", AdminJs);
        Assert.Contains("Omit temperature parameter", AdminJs);
        Assert.Contains("Off preserves this role's existing behavior.", AdminJs);
    }

    [Fact]
    public void AudioAndRealtimeRolesDoNotReceiveTemperatureFlags()
    {
        var omitProperties = typeof(AiModelSettings).GetProperties()
            .Where(property => property.Name.EndsWith("OmitTemperature", StringComparison.Ordinal))
            .Select(property => property.Name)
            .OrderBy(name => name)
            .ToArray();
        var fieldDefinitions = Slice("const aiModelFields = [", "let aiModelDraft = {};");

        Assert.Equal(
        [
            "FeedbackCorrectionOmitTemperature",
            "LessonHintOmitTemperature",
            "LessonTutorChatOmitTemperature",
            "TranslationOmitTemperature"
        ],
            omitProperties);
        Assert.Contains("[\"speechToTextModel\", \"Speech-to-text model\"]", fieldDefinitions);
        Assert.Contains("[\"lessonChatTextToSpeechModel\", \"Lesson chat text-to-speech model\"]", fieldDefinitions);
        Assert.Contains("[\"conversationModeTextToSpeechModel\", \"Conversation Mode text-to-speech model\"]", fieldDefinitions);
        Assert.Contains("[\"realtimeVoiceModel\", \"Realtime voice model\"]", fieldDefinitions);
    }

    [Fact]
    public void DraftActionsCollectOrRestoreTheBooleanMappings()
    {
        Assert.Contains("aiModelDraft[input.dataset.aiModelOmitKey] = input.checked", AdminJs);
        Assert.Contains("omitInput.checked = aiModelDraft[omitTemperatureKey] === true", AdminJs);
        Assert.Contains("async function saveAiModelDraft() { collectAiModelDraft();", AdminJs);
        Assert.Contains("async function validateAiModelDraft() { collectAiModelDraft();", AdminJs);
        Assert.Contains("async function testAiModelProviderAccess() { collectAiModelDraft();", AdminJs);
        Assert.Contains("aiModelDraft = payload.draft || payload.active || aiModelDraft; renderAiModelFields();", AdminJs);
        Assert.Contains("aiModelDraft = payload.draft || payload.active || {}; renderAiModelFields();", AdminJs);
    }

    private static string Slice(string startMarker, string endMarker)
    {
        var start = AdminJs.IndexOf(startMarker, StringComparison.Ordinal);
        var end = AdminJs.IndexOf(endMarker, start, StringComparison.Ordinal);
        Assert.True(start >= 0 && end > start);
        return AdminJs[start..end];
    }
}
