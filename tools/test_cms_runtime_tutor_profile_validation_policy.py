#!/usr/bin/env python3
"""Deterministic checks for CMS runtime tutor behavior profile id validation."""
from __future__ import annotations

import json
import pathlib
import re

ROOT = pathlib.Path(__file__).resolve().parents[1]
RUNTIME_SERVICE = ROOT / "backend" / "EnglishVoiceTutor.Api" / "Services" / "Cms" / "CmsRuntimeLessonContentService.cs"
AVATAR_OPTIONS = ROOT / "Models" / "TutorAvatarOptions.cs"
BACKEND_CSPROJ = ROOT / "backend" / "EnglishVoiceTutor.Api" / "EnglishVoiceTutor.Api.csproj"
TUTOR_DIR = ROOT / "Content" / "Tutors"


def read(path: pathlib.Path) -> str:
    return path.read_text(encoding="utf-8")


def main() -> int:
    runtime_service = read(RUNTIME_SERVICE)
    avatar_options = read(AVATAR_OPTIONS)
    backend_csproj = read(BACKEND_CSPROJ)
    tutor_ids = sorted(json.loads(path.read_text(encoding="utf-8"))["id"] for path in TUTOR_DIR.glob("*.json"))
    option_ids = sorted(re.findall(r'public const string \w+AvatarId = "([^"]+)";', avatar_options))

    if tutor_ids != option_ids:
        raise AssertionError(f"Tutor JSON ids {tutor_ids} do not match desktop avatar option ids {option_ids}.")

    for linked_file in ["../../Models/TutorAvatarOption.cs", "../../Models/TutorAvatarOptions.cs"]:
        if linked_file not in backend_csproj:
            raise AssertionError(f"Backend project must link approved avatar definitions: {linked_file}")

    if "RequiredTutorBehaviorProfileCount = 2" in runtime_service:
        raise AssertionError("CMS runtime tutor validation must not use the obsolete exact count of 2.")

    for needle in [
        "RequiredTutorBehaviorProfileIds = TutorAvatarOptions.All",
        "Expected required tutor ids:",
        "actual tutor ids:",
        "missing tutor ids:",
        "unknown/extra tutor ids:",
        "duplicate tutor ids:",
    ]:
        if needle not in runtime_service:
            raise AssertionError(f"Missing actionable runtime tutor validation text: {needle}")

    print("CMS runtime tutor profile validation policy passed.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
