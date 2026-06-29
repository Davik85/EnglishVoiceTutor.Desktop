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

EXPECTED_CONTACT_TEXT = {
    "en": {
        "Contacts": "Contacts",
        "Support email": "Support email",
        "Website": "Website",
        "For product support, billing questions, legal requests, or privacy questions, contact us by email or visit the website.": "For product support, billing questions, legal requests, or privacy questions, contact us by email or visit the website.",
    },
    "es": {
        "Contacts": "Contactos",
        "Support email": "Correo de soporte",
        "Website": "Sitio web",
        "For product support, billing questions, legal requests, or privacy questions, contact us by email or visit the website.": "Para soporte del producto, preguntas de facturación, solicitudes legales o consultas de privacidad, contáctanos por correo electrónico o visita el sitio web.",
    },
    "fr": {
        "Contacts": "Contacts",
        "Support email": "E-mail d’assistance",
        "Website": "Site web",
        "For product support, billing questions, legal requests, or privacy questions, contact us by email or visit the website.": "Pour l’assistance produit, les questions de facturation, les demandes juridiques ou les questions de confidentialité, contactez-nous par e-mail ou consultez le site web.",
    },
    "de": {
        "Contacts": "Kontakt",
        "Support email": "Support-E-Mail",
        "Website": "Website",
        "For product support, billing questions, legal requests, or privacy questions, contact us by email or visit the website.": "Bei Produktsupport, Abrechnungsfragen, rechtlichen Anfragen oder Datenschutzfragen kontaktieren Sie uns per E-Mail oder besuchen Sie die Website.",
    },
    "it": {
        "Contacts": "Contatti",
        "Support email": "Email di supporto",
        "Website": "Sito web",
        "For product support, billing questions, legal requests, or privacy questions, contact us by email or visit the website.": "Per supporto sul prodotto, domande di fatturazione, richieste legali o domande sulla privacy, contattaci via email o visita il sito web.",
    },
    "pt": {
        "Contacts": "Contactos",
        "Support email": "Email de suporte",
        "Website": "Site",
        "For product support, billing questions, legal requests, or privacy questions, contact us by email or visit the website.": "Para suporte do produto, questões de faturação, pedidos legais ou questões de privacidade, contacte-nos por email ou visite o site.",
    },
    "ru": {
        "Contacts": "Контакты",
        "Support email": "Почта поддержки",
        "Website": "Сайт",
        "For product support, billing questions, legal requests, or privacy questions, contact us by email or visit the website.": "По вопросам поддержки, оплаты, юридических запросов или конфиденциальности свяжитесь с нами по почте или посетите сайт.",
    },
    "pl": {
        "Contacts": "Kontakt",
        "Support email": "E-mail pomocy technicznej",
        "Website": "Strona internetowa",
        "For product support, billing questions, legal requests, or privacy questions, contact us by email or visit the website.": "W sprawach pomocy technicznej, płatności, kwestii prawnych lub prywatności skontaktuj się z nami e-mailem albo odwiedź stronę internetową.",
    },
    "ar": {
        "Contacts": "جهات الاتصال",
        "Support email": "البريد الإلكتروني للدعم",
        "Website": "الموقع الإلكتروني",
        "For product support, billing questions, legal requests, or privacy questions, contact us by email or visit the website.": "للدعم الخاص بالمنتج أو أسئلة الفوترة أو الطلبات القانونية أو أسئلة الخصوصية، تواصل معنا عبر البريد الإلكتروني أو قم بزيارة الموقع.",
    },
    "ja": {
        "Contacts": "連絡先",
        "Support email": "サポートメール",
        "Website": "ウェブサイト",
        "For product support, billing questions, legal requests, or privacy questions, contact us by email or visit the website.": "製品サポート、請求、法的な依頼、プライバシーに関するお問い合わせは、メールでご連絡いただくか、ウェブサイトをご覧ください。",
    },
    "ko": {
        "Contacts": "연락처",
        "Support email": "지원 이메일",
        "Website": "웹사이트",
        "For product support, billing questions, legal requests, or privacy questions, contact us by email or visit the website.": "제품 지원, 결제 문의, 법적 요청 또는 개인정보 관련 문의는 이메일로 연락하거나 웹사이트를 방문해 주세요.",
    },
    "sr": {
        "Contacts": "Kontakt",
        "Support email": "E-pošta podrške",
        "Website": "Veb-sajt",
        "For product support, billing questions, legal requests, or privacy questions, contact us by email or visit the website.": "Za podršku za proizvod, pitanja o naplati, pravne zahteve ili pitanja privatnosti, kontaktirajte nas putem e-pošte ili posetite veb-sajt.",
    },
    "hr": {
        "Contacts": "Kontakt",
        "Support email": "E-pošta podrške",
        "Website": "Web-stranica",
        "For product support, billing questions, legal requests, or privacy questions, contact us by email or visit the website.": "Za podršku za proizvod, pitanja o naplati, pravne zahtjeve ili pitanja privatnosti, kontaktirajte nas e-poštom ili posjetite web-stranicu.",
    },
    "bg": {
        "Contacts": "Контакти",
        "Support email": "Имейл за поддръжка",
        "Website": "Уебсайт",
        "For product support, billing questions, legal requests, or privacy questions, contact us by email or visit the website.": "За поддръжка на продукта, въпроси за плащане, правни искания или въпроси за поверителност се свържете с нас по имейл или посетете уебсайта.",
    },
}

RUSSIAN_HELPER_TEXT = EXPECTED_CONTACT_TEXT["ru"]["For product support, billing questions, legal requests, or privacy questions, contact us by email or visit the website."]


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



def parse_settings_constructor_arguments(app_loc: str) -> list[str]:
    marker = "var settings = new SettingsLocalizedText("
    start = app_loc.find(marker)
    require(start >= 0, "SettingsLocalizedText runtime constructor call missing")
    index = start + len(marker)
    depth = 1
    current: list[str] = []
    args: list[str] = []
    in_string = False
    escape = False
    while index < len(app_loc):
        char = app_loc[index]
        if in_string:
            current.append(char)
            if escape:
                escape = False
            elif char == "\\":
                escape = True
            elif char == '"':
                in_string = False
        else:
            if char == '"':
                in_string = True
                current.append(char)
            elif char == '(':
                depth += 1
                current.append(char)
            elif char == ')':
                depth -= 1
                if depth == 0:
                    args.append("".join(current).strip())
                    return args
                current.append(char)
            elif char == ',' and depth == 1:
                args.append("".join(current).strip())
                current = []
            else:
                current.append(char)
        index += 1
    raise AssertionError("could not parse SettingsLocalizedText runtime constructor arguments")


def parse_settings_record_members(settings_text: str) -> list[str]:
    start = settings_text.find("public sealed record SettingsLocalizedText(")
    require(start >= 0, "SettingsLocalizedText record missing")
    body = settings_text[settings_text.find("(", start) + 1:settings_text.rfind(");")]
    return re.findall(r"\bstring\s+(\w+)\b", body)


def runtime_settings_value(app_loc: str, language_id: str, member_name: str) -> str:
    members = parse_settings_record_members(read("Localization/SettingsLocalizedText.cs"))
    args = parse_settings_constructor_arguments(app_loc)
    require(len(args) == len(members), f"SettingsLocalizedText constructor/member mismatch: {len(args)} args for {len(members)} members")
    member_args = dict(zip(members, args))
    expression = member_args[member_name]
    localized_call = re.fullmatch(r'l\("(.*)"\)', expression)
    literal = re.fullmatch(r'"(.*)"', expression)
    if localized_call:
        english = localized_call.group(1)
        if language_id == "en":
            return english
        block = extract_language_block(app_loc, language_id)
        match = re.search(rf'\["{re.escape(english)}"\]\s*=\s*"([^"]+)"', block)
        return match.group(1) if match else english
    if literal:
        return literal.group(1)
    raise AssertionError(f"unsupported runtime SettingsLocalizedText expression for {member_name}: {expression}")

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

    interface_change_start = settings_vm.index("partial void OnSelectedInterfaceLanguageOptionChanged")
    interface_change_body = settings_vm[interface_change_start:settings_vm.index("partial void OnBackendBaseUrlChanged", interface_change_start)]
    require("SettingsLocalization.GetSettingsText(value.Id)" in interface_change_body, "Settings screen must refresh Contacts from selected interface language")
    require("SelectedStudyLanguage" not in interface_change_body.split("localizedText =", 1)[1].split("RefreshLocalizedText", 1)[0], "Settings localization must not use study language")

    refresh_start = settings_vm.index("private void RefreshLocalizedText()")
    refresh_body = settings_vm[refresh_start:settings_vm.index("private void RefreshAudioInputDevices", refresh_start)]
    for token in ["ContactsTabHeader", "ContactsTitle", "ContactsHelperText", "SupportEmailLabel", "SupportEmailAddress", "WebsiteLabel", "WebsiteUrl"]:
        require(f"OnPropertyChanged(nameof({token}))" in refresh_body, f"runtime interface-language refresh must notify Contacts binding: {token}")

    require('ContactsTabHeader = "Contacts"' in settings_text, "English Contacts tab label must stay English")
    require('ContactsTitle = "Contacts"' in settings_text, "English Contacts section title must stay English")
    require('ContactsHelperText = "For product support, billing questions, legal requests, or privacy questions, contact us by email or visit the website."' in settings_text, "English Contacts helper text must stay English")
    require('SupportEmailLabel = "Support email"' in settings_text, "English support email label must stay English")
    require('WebsiteLabel = "Website"' in settings_text, "English website label must stay English")

    for language_id in SUPPORTED_LANGUAGE_IDS:
        if language_id == "en":
            values = EXPECTED_CONTACT_TEXT[language_id]
            for english, expected in values.items():
                require(expected in settings_text, f"English Contacts text missing: {expected}")
            continue

        block = extract_language_block(app_loc, language_id)
        for english, expected in EXPECTED_CONTACT_TEXT[language_id].items():
            match = re.search(rf'\["{re.escape(english)}"\]\s*=\s*"([^"]+)"', block)
            require(match is not None, f"{language_id} missing Contacts localization key: {english}")
            require(match.group(1) == expected, f"{language_id} Contacts localization mismatch for {english}: {match.group(1)!r}")
            if language_id != "ru":
                require(match.group(1) != RUSSIAN_HELPER_TEXT, f"{language_id} Contacts helper text must not use Russian fallback")

    for language_id in ["en", "es", "pt", "pl", "hr", "ru"]:
        runtime_values = {
            "ContactsTabHeader": runtime_settings_value(app_loc, language_id, "ContactsTabHeader"),
            "ContactsTitle": runtime_settings_value(app_loc, language_id, "ContactsTitle"),
            "ContactsHelperText": runtime_settings_value(app_loc, language_id, "ContactsHelperText"),
            "SupportEmailLabel": runtime_settings_value(app_loc, language_id, "SupportEmailLabel"),
            "WebsiteLabel": runtime_settings_value(app_loc, language_id, "WebsiteLabel"),
        }
        expected = EXPECTED_CONTACT_TEXT[language_id]
        require(runtime_values["ContactsTabHeader"] == expected["Contacts"], f"{language_id} runtime Contacts tab mismatch")
        require(runtime_values["ContactsTitle"] == expected["Contacts"], f"{language_id} runtime Contacts title mismatch")
        require(runtime_values["ContactsHelperText"] == expected["For product support, billing questions, legal requests, or privacy questions, contact us by email or visit the website."], f"{language_id} runtime Contacts helper mismatch")
        require(runtime_values["SupportEmailLabel"] == expected["Support email"], f"{language_id} runtime support label mismatch")
        require(runtime_values["WebsiteLabel"] == expected["Website"], f"{language_id} runtime website label mismatch")
        if language_id != "ru":
            require(runtime_values["ContactsHelperText"] != RUSSIAN_HELPER_TEXT, f"{language_id} runtime Contacts helper must not use Russian text")

    require(runtime_settings_value(app_loc, "hr", "ContactsHelperText") != runtime_settings_value(app_loc, "ru", "ContactsHelperText"), "runtime Contacts lookup must distinguish interface language from Russian")

    ru_block = extract_language_block(app_loc, "ru")
    for russian in EXPECTED_CONTACT_TEXT["ru"].values():
        require(russian in ru_block, f"Russian Contacts text missing: {russian}")

    require('SupportEmailAddress = "support@languagevoicetutor.com"' in settings_text, "support email value changed")
    require('WebsiteUrl = "https://languagevoicetutor.com"' in settings_text, "website URL value changed")

    require("allowMailTo" in settings_vm, "external-link helper must explicitly gate mailto support")
    require("Uri.UriSchemeHttps" in settings_vm and "Uri.UriSchemeMailto" in settings_vm, "external-link helper must allow only safe https/mailto links")
    external_link_start = settings_vm.index("private static bool TryOpenExternalUrl")
    external_link_body = settings_vm[external_link_start:settings_vm.index("private void ApplySignedOutSubscriptionStatus", external_link_start)]
    require(re.search(r"uri\.Scheme\s*!=\s*Uri\.UriSchemeHttp(?!s)", external_link_body) is None, "contact links must not allow plain http")

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
