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
    for const in ("A1FinalMessageAtUserTurn = 15", "B2FinalMessageAtUserTurn = 32", "RequiredLevelCount = 4"):
        require(text, const, f"named level constant {const}")
    require(text, "finalMessageAtUserTurn must be greater than wrapUpAfterUserTurn", "turn validation")
    require(text, "unknown active level ids", "unknown active id validation")


def test_backend_level_settings_drive_lesson_behavior() -> None:
    limits = LIMITS.read_text(encoding="utf-8")
    runtime = RUNTIME.read_text(encoding="utf-8")
    require(limits, "CmsLevelProfiles.Resolve", "level fallback in lesson limit helper")
    require(runtime, "ApplyCmsLevelProfiles", "runtime applies CMS levels into lesson scenarios")
    require(runtime, "SoftWrapUpAfterUserTurn = profile.WrapUpAfterUserTurn", "level wrap-up propagated")
    require(runtime, "FinalMessageAtUserTurn = profile.FinalMessageAtUserTurn", "level final turn propagated")


def test_admin_has_levels_tab_and_validation() -> None:
    html = ADMIN_HTML.read_text(encoding="utf-8")
    js = ADMIN_JS.read_text(encoding="utf-8")
    validation = VALIDATION.read_text(encoding="utf-8")
    require(html, 'data-cms-sub-tab-id="levels"', "levels CMS sub-tab")
    require(html, "cms-level-final-turn", "level final turn editor")
    require(js, "level_profiles", "level profiles template binding")
    require(js, "Admin CMS UI level profile draft edit", "level save draft reason")
    require(validation, "ValidateLevelProfiles", "draft level validation")

if __name__ == "__main__":
    test_cms_level_profiles_are_named_and_required()
    test_backend_level_settings_drive_lesson_behavior()
    test_admin_has_levels_tab_and_validation()
