from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]


def read(path: str) -> str:
    return (ROOT / path).read_text(encoding="utf-8")


def test_admin_cms_tutor_responses_canonicalize_legacy_elena_to_lana() -> None:
    source = read("backend/EnglishVoiceTutor.Api/Services/Cms/CmsContentAdminService.cs")
    assert "TutorId = TutorAvatarOptions.ToCanonicalId(profile.TutorId)" in source
    assert ".GroupBy(profile => TutorAvatarOptions.ToCanonicalId(profile.TutorId), StringComparer.Ordinal)" in source
    assert "var canonicalTutorId = TutorAvatarOptions.ToCanonicalId(value);" in source


def test_published_snapshot_canonicalizes_legacy_elena_to_lana() -> None:
    source = read("backend/EnglishVoiceTutor.Api/Services/Cms/CmsContentSnapshotBuilder.cs")
    assert "TutorId = TutorAvatarOptions.ToCanonicalId(tutor.TutorId)" in source
    assert "var canonicalTutorId = TutorAvatarOptions.ToCanonicalId(profile.TutorId);" in source
    assert "tutorProfile.Id = canonicalTutorId;" in source


def test_lana_is_canonical_and_elena_only_legacy_alias() -> None:
    source = read("Models/TutorAvatarOptions.cs")
    assert 'public const string DefaultAvatarId = "lana";' in source
    assert 'public const string LegacyElenaTutorAlias = "elena";' in source
    assert "? DefaultAvatarId" in source
