from pathlib import Path
import re

ROOT = Path(__file__).resolve().parents[1]
SUPPORTED_LANGUAGE_IDS = ["en", "es", "fr", "de", "it", "pt", "ru", "pl", "ar", "ja", "ko", "sr", "hr", "bg"]
CONTACT_KEYS = [
    "Contacts",
    "Support email",
    "Website",
    "For product support, billing questions, legal requests, or privacy questions, contact us by email or visit the website.",
]


def read(path: str) -> str:
    return (ROOT / path).read_text(encoding="utf-8")


def require(condition: bool, message: str) -> None:
    if not condition:
        raise AssertionError(message)


def extract_language_block(text: str, language_id: str) -> str:
    marker = f'["{language_id}"] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)'
    start = text.find(marker)
    require(start >= 0, f"missing localization dictionary for {language_id}")
    brace = text.find("{", start)
    depth = 0
    for index in range(brace, len(text)):
        char = text[index]
        if char == "{":
            depth += 1
        elif char == "}":
            depth -= 1
            if depth == 0:
                return text[brace:index + 1]
    raise AssertionError(f"could not parse localization dictionary for {language_id}")


def main() -> None:
    settings_xaml = read("Views/SettingsView.xaml")
    settings_vm = read("ViewModels/SettingsViewModel.cs")
    settings_text = read("Localization/SettingsLocalizedText.cs")
    app_loc = read("Localization/AppLocalization.cs")
    interface_languages = read("Models/InterfaceLanguageOptions.cs")
    subtopics_xaml = read("Views/SubtopicsView.xaml")
    lesson_vm = read("ViewModels/LessonChatViewModel.cs")

    for language_id in SUPPORTED_LANGUAGE_IDS:
        require(f'"{language_id}"' in interface_languages or f'{language_id.capitalize()}Id' in interface_languages, f"supported UI language not declared: {language_id}")

    for token in ["ContactsSectionNav", "ContactsSection", "SupportEmailAddress", "WebsiteUrl", "support@languagevoicetutor.com", "https://languagevoicetutor.com"]:
        require(token in settings_xaml or token in settings_vm or token in settings_text, f"missing Contacts UI token: {token}")

    for token in ["ContactsTabHeader", "ContactsTitle", "ContactsHelperText", "SupportEmailLabel", "SupportEmailAddress", "WebsiteLabel", "WebsiteUrl"]:
        require(token in settings_text, f"SettingsLocalizedText missing {token}")
        require(token in settings_vm, f"SettingsViewModel missing {token}")

    for english in CONTACT_KEYS:
        require(f'l("{english}")' in app_loc, f"localized/fallback Contacts text not wired for {english}")

    for language_id in SUPPORTED_LANGUAGE_IDS:
        if language_id == "en":
            continue
        block = extract_language_block(app_loc, language_id)
        for english in CONTACT_KEYS:
            match = re.search(rf'\["{re.escape(english)}"\]\s*=\s*"([^"]+)"', block)
            require(match is not None, f"{language_id} missing Contacts localization key: {english}")
            require(match.group(1) != english, f"{language_id} Contacts localization still falls back to English for: {english}")

    ru_block = extract_language_block(app_loc, "ru")
    for russian in ["Контакты", "Почта поддержки", "Сайт", "По вопросам поддержки, оплаты, юридических запросов или конфиденциальности свяжитесь с нами по почте или посетите сайт."]:
        require(russian in ru_block, f"Russian Contacts text missing: {russian}")

    require("allowMailTo" in settings_vm, "external-link helper must explicitly gate mailto support")
    require("Uri.UriSchemeHttps" in settings_vm and "Uri.UriSchemeMailto" in settings_vm, "external-link helper must allow only safe https/mailto links")
    require("uri.Scheme != Uri.UriSchemeHttp" in settings_vm, "contact links must not allow arbitrary schemes")

    title_line = next(line for line in subtopics_xaml.splitlines() if 'Text="{Binding Title}"' in line)
    require('TextWrapping="Wrap"' in title_line and 'TextTrimming="None"' in title_line, "Subtopics title must wrap and not trim")
    require('HorizontalScrollBarVisibility="Disabled"' in subtopics_xaml, "Subtopics view must wrap instead of horizontal scrolling")
    require('Text="{Binding DisplayTitle}"' in subtopics_xaml and 'TextWrapping="Wrap"' in subtopics_xaml and 'TextTrimming="None"' in subtopics_xaml, "Subtopic titles must wrap and not trim")
    require('Text="{Binding DisplayDescription}"' in subtopics_xaml and 'TextWrapping="Wrap"' in subtopics_xaml, "Subtopic descriptions must wrap")

    back_start = lesson_vm.index("private async Task Back()")
    back_body = lesson_vm[back_start:lesson_vm.index("private bool CanGoBack()")]
    require("ShouldConfirmManualEarlyFinish() && !ShowFinishLessonConfirmation()" in back_body, "Back must use Finish lesson confirmation path")
    require("return;" in back_body.split("ShouldConfirmManualEarlyFinish()", 1)[1].split("isFinishLessonInProgress = true", 1)[0], "Cancel path must stay in the lesson")
    require("navigateBack();" in back_body, "Confirm path must continue with existing back navigation")


if __name__ == "__main__":
    main()
