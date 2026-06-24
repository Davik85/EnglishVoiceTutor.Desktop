from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
LEVELS = ROOT / "backend" / "EnglishVoiceTutor.Api" / "Services" / "Cms" / "CmsLevelProfiles.cs"
LIMITS = ROOT / "backend" / "EnglishVoiceTutor.Api" / "Services" / "LessonLimitHelper.cs"
RUNTIME = ROOT / "backend" / "EnglishVoiceTutor.Api" / "Services" / "Cms" / "CmsRuntimeLessonContentService.cs"
ADMIN_HTML = ROOT / "backend" / "EnglishVoiceTutor.Api" / "wwwroot" / "admin" / "index.html"
ADMIN_JS = ROOT / "backend" / "EnglishVoiceTutor.Api" / "wwwroot" / "admin" / "admin.js"
VALIDATION = ROOT / "backend" / "EnglishVoiceTutor.Api" / "Services" / "Cms" / "CmsContentValidationService.cs"


def require(text: str, needle: str, label: str) -> None:
    if needle not in text:
        raise AssertionError(f"Missing {label}: {needle}")


def test_cms_level_profiles_are_named_and_required() -> None:
    text = LEVELS.read_text(encoding="utf-8")
    for key in ('"a1"', '"a2"', '"b1"', '"b2"'):
        require(text, key, f"required level key {key}")
    for const in ("A1WrapUpAfterUserTurn = 14", "A1FinalMessageAtUserTurn = 15", "B2FinalMessageAtUserTurn = 32", "RequiredLevelCount = 4", "SortOrder = 1", "SortOrder = 4"):
        require(text, const, f"named level constant {const}")
    require(text, "AddMissingRequiredDefaults", "safe repair helper for existing packs")
    require(text, "finalMessageAtUserTurn must be greater than wrapUpAfterUserTurn", "turn validation")
    require(text, "unknown active level ids", "unknown active id validation")


def test_backend_level_settings_drive_lesson_behavior() -> None:
    limits = LIMITS.read_text(encoding="utf-8")
    runtime = RUNTIME.read_text(encoding="utf-8")
    require(limits, "CmsLevelProfiles.Resolve", "static level fallback in lesson limit helper")
    require(limits, "request.SoftWrapUpAfterUserTurn > 0", "resolved desktop level-profile wrap threshold honored")
    require(limits, "request.FinalMessageAtUserTurn > 0", "resolved desktop level-profile final threshold honored")
    require(runtime, "ApplyCmsLevelProfiles", "runtime applies CMS levels into lesson scenarios")
    require(runtime, "SoftWrapUpAfterUserTurn = profile.WrapUpAfterUserTurn", "level wrap-up propagated")
    require(runtime, "FinalMessageAtUserTurn = profile.FinalMessageAtUserTurn", "level final turn propagated")


def test_admin_has_levels_tab_and_validation() -> None:
    html = ADMIN_HTML.read_text(encoding="utf-8")
    js = ADMIN_JS.read_text(encoding="utf-8")
    validation = VALIDATION.read_text(encoding="utf-8")
    require(html, 'data-cms-sub-tab-id="levels"', "levels CMS sub-tab")
    require(html, "cms-level-final-turn", "level final turn editor")
    if "cms-scenario-soft-wrap-turn" in html or "cms-scenario-final-message-turn" in html:
        raise AssertionError("Scenario editor must not expose scenario metadata turn-limit fields as normal controls.")
    require(html, "runtime ignores them", "legacy scenario turn-limit warning")
    require(html, "cms-level-initialize-button", "default level initialization button")
    require(html, "Save draft only persists CMS draft data", "publish explanation for levels")
    require(js, "level_profiles", "level profiles template binding")
    require(js, "CmsDefaultLevelProfiles", "draft-ready default level profiles")
    require(js, "mergeMissingDefaultCmsLevels", "missing level merge without overwriting existing levels")
    require(js, 'template?.id || "level_profiles"', "save path for missing level_profiles template")
    require(js, "Admin CMS UI level profile draft edit", "level save draft reason")
    require(validation, "ValidateLevelProfiles", "draft level validation")
    require(validation, "Legacy scenario metadata turn-limit fields are tolerated", "legacy scenario metadata import compatibility")

if __name__ == "__main__":
    test_cms_level_profiles_are_named_and_required()
    test_backend_level_settings_drive_lesson_behavior()
    test_admin_has_levels_tab_and_validation()
