namespace EnglishVoiceTutor.Api.Tests.AdminUi;

public sealed class AdminLocalizedSetupMessagesUiStaticTests
{
    private static readonly string AdminJs = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "../../../../EnglishVoiceTutor.Api/wwwroot/admin/admin.js"));
    private static readonly string AdminIndex = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "../../../../EnglishVoiceTutor.Api/wwwroot/admin/index.html"));

    [Fact]
    public void LessonSetupKeepsTheEnglishFieldAndAddsExactlyFiveFullLocalizedMessageTextareas()
    {
        Assert.Contains("Visible first lesson message", AdminIndex);
        foreach (var language in new[] { "fr", "de", "pt", "es", "it" })
        {
            Assert.Contains($"id=\"cms-scenario-localized-setup-message-{language}\"", AdminIndex);
            Assert.Contains($"cms-scenario-localized-setup-message-{language}\" name=\"cms-scenario-localized-setup-message-{language}\" rows=\"6\"", AdminIndex);
        }

        Assert.Equal(5, CountOccurrences(AdminIndex, "id=\"cms-scenario-localized-setup-message-"));
        Assert.DoesNotContain("cms-scenario-localized-setup-message-en", AdminIndex);
        Assert.Contains("Edit the full message as one text block.", AdminIndex);
        Assert.DoesNotContain("contextVariantTitles", AdminIndex, StringComparison.Ordinal);
    }

    [Fact]
    public void LocalizedMessagesLoadAndMergeOnlyTheirCompleteTemplatesIntoDefinitionJson()
    {
        Assert.Contains("getCmsStringField(root, [\"setupLocalizations\", languageId, \"setupMessageTemplate\"])", AdminJs);
        Assert.Contains("localization.setupMessageTemplate = value;", AdminJs);
        Assert.Contains("{ contextVariantTitles: {} }", AdminJs);
        Assert.Contains("mergeCmsLocalizedSetupMessagesToDefinition(root);", AdminJs);
        Assert.Contains("localizedSetupMessages: getCmsLocalizedSetupMessageSnapshot()", AdminJs);
        Assert.DoesNotContain("localizedSetup", AdminJs.Substring(AdminJs.IndexOf("function mergeCmsLocalizedSetupMessagesToDefinition"), AdminJs.IndexOf("function getCmsStructuredScenarioSnapshot") - AdminJs.IndexOf("function mergeCmsLocalizedSetupMessagesToDefinition")));
    }

    [Fact]
    public void StructuredSaveMergesVisibleLocalizedMessagesButAdvancedJsonSavePreservesItsOwnDocument()
    {
        var structuredMerge = AdminJs.Substring(
            AdminJs.IndexOf("function mergeCmsStructuredScenarioFieldsToDefinition", StringComparison.Ordinal),
            AdminJs.IndexOf("function validateCmsStructuredScenarioInput", StringComparison.Ordinal) - AdminJs.IndexOf("function mergeCmsStructuredScenarioFieldsToDefinition", StringComparison.Ordinal));
        var saveScenario = AdminJs.Substring(
            AdminJs.IndexOf("async function saveCmsScenarioDraft", StringComparison.Ordinal),
            AdminJs.IndexOf("async function saveCmsLevelDraft", StringComparison.Ordinal) - AdminJs.IndexOf("async function saveCmsScenarioDraft", StringComparison.Ordinal));
        var advancedBranch = saveScenario.Substring(0, saveScenario.IndexOf("} else {", StringComparison.Ordinal));

        Assert.Contains("mergeCmsLocalizedSetupMessagesToDefinition(root);", structuredMerge);
        Assert.Contains("if (!validateCmsScenarioJsonInput())", advancedBranch);
        Assert.DoesNotContain("mergeCmsLocalizedSetupMessagesToDefinition", advancedBranch);
        Assert.Contains("definitionJson: cmsScenarioDefinitionJsonInput.value", saveScenario);
        Assert.Contains("() => selectCmsScenario(cmsSelectedScenario)", saveScenario);
        Assert.Contains("fillCmsScenarioForm();", AdminJs);
        Assert.Contains("fillCmsStructuredScenarioFieldsFromDefinition();", AdminJs);
    }

    [Fact]
    public void LocalizedMessageControlsDoNotCreateComponentOrContextMappingEditors()
    {
        Assert.DoesNotContain("localized-goal", AdminIndex, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("localized-context-variant", AdminIndex, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("localized-situation", AdminIndex, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("translation-provider", AdminIndex, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("automatic translation", AdminIndex, StringComparison.OrdinalIgnoreCase);
    }

    private static int CountOccurrences(string value, string needle)
    {
        var count = 0;
        var index = 0;
        while ((index = value.IndexOf(needle, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += needle.Length;
        }

        return count;
    }
}
