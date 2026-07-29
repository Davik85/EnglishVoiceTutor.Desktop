#!/usr/bin/env python3
"""Deterministic checks that lesson openings are target-language aware."""
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]


def read(relative: str) -> str:
    return (ROOT / relative).read_text(encoding="utf-8")


def require_text(text: str, needle: str, source: str) -> None:
    if needle not in text:
        raise AssertionError(f"Missing {needle!r} in {source}")


def main() -> None:
    vm = read("ViewModels/LessonChatViewModel.cs")
    main_vm = read("ViewModels/MainViewModel.cs")
    localizer = read("Services/LocalizedLessonTextService.cs")
    backend = read("Services/LessonChatBackendService.cs")

    for needle in [
        "LocalizedLessonTextService.BuildSetupMessage",
        "Opening message created: Source=",
        "SelectedLanguageId={this.studyLanguage.Id}",
        "BackendLocalizedSetup",
        "ProjectionStatus=",
        "RenderLocalizedLessonTemplate",
        "GetSelectedContextConfirmationLine(matchedVariant, localizedScenario)",
        "private string GetSelectedContextConfirmationLine(ContextVariant variant, string resolvedLocalizedTitle)",
        "BuildContextConfirmationLine(variant, resolvedLocalizedTitle, studyLanguage, englishConfirmationLine)",
        "Starting lesson with StudyLanguageId=",
    ]:
        require_text(vm, needle, "ViewModels/LessonChatViewModel.cs")

    for needle in [
        "OpeningMessageSource",
        "TryGetCompleteBackendLocalizedSetup",
        "ValidateBackendLocalizedSetup",
        "ContextVariantDisplayTitles",
        "renderLocalizedTemplate(localizedSetup!.SetupMessageTemplate!)",
        "Aujourd’hui, nous allons pratiquer",
        "The lesson JSON scenario text is semantic metadata.",
        "BuildContextOpeningLine",
        "BuildContextConfirmationLine",
        "string resolvedLocalizedTitle",
        "BuildInvalidContextRedirect",
        "BuildFinalLessonMessage",
    ]:
        require_text(localizer, needle, "Services/LocalizedLessonTextService.cs")

    if "Today we'll practice" in localizer:
        raise AssertionError("Localized opening builder must not use the English opening as the non-English fallback.")

    for needle in [
        "IsRuntimeLessonScenarioValid(runtimeScenario, selectedStudyLanguage)",
        "LogRuntimeScenarioLocalizationRejection",
        "ApplyPackagedStaticRuntimeDiagnostics(localScenario)",
        "LocalizedLessonTextService.TryGetCompleteBackendLocalizedSetup(runtimeScenario, studyLanguage, out _)",
    ]:
        require_text(main_vm, needle, "ViewModels/MainViewModel.cs")

    for needle in [
        "localizedSetup!.ContextVariantDisplayTitles[variant.Id]",
        "int.TryParse(normalizedInput",
        "option.LocalizedTitle, option.CanonicalTitle",
        ".Concat(option.Variant.Aliases)",
        "if (IsEnglish(language))",
    ]:
        require_text(localizer, needle, "Services/LocalizedLessonTextService.cs")

    for needle in [
        'rendered = template.Replace(", {{userDisplayName}}", string.Empty, StringComparison.Ordinal)',
        'rendered.StartsWith("¡", StringComparison.Ordinal)',
        'template.Replace("{{userDisplayName}}", UserDisplayName, StringComparison.Ordinal)',
    ]:
        require_text(vm, needle, "LessonChatViewModel.cs")

    confirmation = localizer[localizer.index("public static string BuildContextConfirmationLine"):localizer.index("public static string BuildContextOpeningLine")]
    require_text(confirmation, "? AdaptShortScenarioText(variant.Title, language)", "Services/LocalizedLessonTextService.cs")
    require_text(confirmation, ": resolvedLocalizedTitle", "Services/LocalizedLessonTextService.cs")
    require_text(confirmation, "return englishFallback;", "Services/LocalizedLessonTextService.cs")

    for source, name in [(localizer, "LocalizedLessonTextService.cs"), (main_vm, "MainViewModel.cs"), (vm, "LessonChatViewModel.cs")]:
        if "SetupLocalizations" in source:
            raise AssertionError(f"{name} must not read authored SetupLocalizations for runtime UI behavior.")

    rejection_log = main_vm[main_vm.index("private static void LogRuntimeScenarioLocalizationRejection"):]
    for forbidden in ["SetupMessageTemplate}", "UserDisplayName", "DefinitionJson"]:
        if forbidden in rejection_log:
            raise AssertionError(f"Rejection diagnostics must not include {forbidden}.")

    require_text(backend, "InputLength={inputLength}; TargetLanguageId={resolvedTargetLanguage.Id}", "Services/LessonChatBackendService.cs")

    print("Multilingual opening message policy checks passed.")


if __name__ == "__main__":
    main()
