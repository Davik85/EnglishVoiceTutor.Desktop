using System.Text.Json;
using EnglishVoiceTutor.Api.Models;
using EnglishVoiceTutor.Api.Services;

namespace EnglishVoiceTutor.Api.Tests.Services;

public sealed class OpenAiLessonChatServiceModelPolicyTests
{
    [Theory]
    [InlineData("gpt-5.2", false, 0.3)]
    [InlineData("gpt-5.5", false, null)]
    [InlineData("gpt-5.5-2026-06-01", false, null)]
    [InlineData("gpt-5.6-terra", true, null)]
    [InlineData("gpt-5.2", true, null)]
    public void LessonTutorChatPolicyPreservesLegacyBehaviorUnlessOverrideIsEnabled(string modelId, bool omitTemperature, double? expected)
    {
        Assert.Equal(expected, AiTextModelTemperaturePolicy.Resolve(AiTextModelRole.LessonTutorChat, modelId, omitTemperature));
    }

    [Theory]
    [InlineData("gpt-5.2", false, 0.3)]
    [InlineData("gpt-5.5", false, null)]
    [InlineData("gpt-5.6-terra", true, null)]
    public void FeedbackPolicyMatchesItsExistingSharedLessonRequestBehavior(string modelId, bool omitTemperature, double? expected)
    {
        Assert.Equal(expected, AiTextModelTemperaturePolicy.Resolve(AiTextModelRole.FeedbackCorrection, modelId, omitTemperature));
    }

    [Theory]
    [InlineData("hint", false)]
    [InlineData("hint", true)]
    [InlineData("translation", false)]
    [InlineData("translation", true)]
    public void RolesThatAlreadyOmitTemperatureContinueToOmitIt(string roleName, bool omitTemperature)
    {
        var role = roleName == "hint" ? AiTextModelRole.LessonHint : AiTextModelRole.Translation;
        Assert.Null(AiTextModelTemperaturePolicy.Resolve(role, "gpt-5.2", omitTemperature));
    }

    [Fact]
    public void NullEffectiveTemperatureIsAbsentFromSerializedRequest()
    {
        var request = new OpenAiResponsesRequest
        {
            Model = "gpt-5.6-terra",
            Instructions = "test",
            Input = "test",
            Temperature = null
        };

        var json = JsonSerializer.Serialize(request, new JsonSerializerOptions(JsonSerializerDefaults.Web));

        Assert.DoesNotContain("temperature", json, StringComparison.OrdinalIgnoreCase);
    }
}
