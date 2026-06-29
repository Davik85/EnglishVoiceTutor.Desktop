from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]

def read(path: str) -> str:
    return (ROOT / path).read_text(encoding="utf-8")

def require(condition: bool, message: str) -> None:
    if not condition:
        raise AssertionError(message)

def main() -> None:
    settings_xaml = read("Views/SettingsView.xaml")
    settings_vm = read("ViewModels/SettingsViewModel.cs")
    settings_text = read("Localization/SettingsLocalizedText.cs")
    app_loc = read("Localization/AppLocalization.cs")
    subtopics_xaml = read("Views/SubtopicsView.xaml")
    lesson_vm = read("ViewModels/LessonChatViewModel.cs")

    for token in ["ContactsSectionNav", "ContactsSection", "SupportEmailAddress", "WebsiteUrl", "support@languagevoicetutor.com", "https://languagevoicetutor.com"]:
        require(token in settings_xaml or token in settings_vm or token in settings_text, f"missing Contacts UI token: {token}")

    for token in ["ContactsTabHeader", "ContactsTitle", "ContactsHelperText", "SupportEmailLabel", "SupportEmailAddress", "WebsiteLabel", "WebsiteUrl"]:
        require(token in settings_text, f"SettingsLocalizedText missing {token}")
        require(token in settings_vm, f"SettingsViewModel missing {token}")

    for english in ["Contacts", "Support email", "Website", "For product support, billing questions, legal requests, or privacy questions, contact us by email or visit the website."]:
        require(f'l("{english}")' in app_loc, f"localized/fallback Contacts text not wired for {english}")

    require("allowMailTo" in settings_vm, "external-link helper must explicitly gate mailto support")
    require("Uri.UriSchemeHttps" in settings_vm and "Uri.UriSchemeMailto" in settings_vm, "external-link helper must allow only https/http and opt-in mailto")

    title_line = next(line for line in subtopics_xaml.splitlines() if 'Text="{Binding Title}"' in line)
    require('TextWrapping="Wrap"' in title_line and 'TextTrimming="None"' in title_line, "Subtopics title must wrap and not trim")
    require('Text="{Binding DisplayTitle}"' in subtopics_xaml and 'TextWrapping="Wrap"' in subtopics_xaml, "Subtopic titles must wrap")

    back_start = lesson_vm.index("private async Task Back()")
    back_body = lesson_vm[back_start:lesson_vm.index("private bool CanGoBack()")]
    require("ShouldConfirmManualEarlyFinish() && !ShowFinishLessonConfirmation()" in back_body, "Back must use Finish lesson confirmation path")
    require("return;" in back_body.split("ShouldConfirmManualEarlyFinish()", 1)[1].split("isFinishLessonInProgress = true", 1)[0], "Cancel path must stay in the lesson")
    require("navigateBack();" in back_body, "Confirm path must continue with existing back navigation")

if __name__ == "__main__":
    main()
