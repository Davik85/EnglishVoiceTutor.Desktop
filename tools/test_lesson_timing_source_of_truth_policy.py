#!/usr/bin/env python3
from __future__ import annotations

import json
import pathlib
import re

ROOT = pathlib.Path(__file__).resolve().parents[1]
PROMPT_DIRS = [ROOT / "Content" / "Prompts"]
LESSON_DIR = ROOT / "Content" / "Lessons"
PROMPT_BUILDER = ROOT / "backend" / "EnglishVoiceTutor.Api" / "Services" / "LessonPromptBuilder.cs"
LIMIT_HELPER = ROOT / "backend" / "EnglishVoiceTutor.Api" / "Services" / "LessonLimitHelper.cs"
SNAPSHOT_BUILDER = ROOT / "backend" / "EnglishVoiceTutor.Api" / "Services" / "Cms" / "CmsContentSnapshotBuilder.cs"
SCENARIO_DEF = ROOT / "backend" / "EnglishVoiceTutor.Api" / "Services" / "Cms" / "CmsScenarioDefinitionJson.cs"
DOC = ROOT / "docs" / "LESSON_TIMING_SOURCE_OF_TRUTH.md"

FORBIDDEN_NUMERIC_TIMING = re.compile(
    r"(?i)(wrap(?:-?up)?|final|finish|end the lesson|complete the lesson|last question|one last time)"
    r"[^\n.]{0,80}\b(10|14|15|20|25|30)\b|"
    r"\b(10|14|15|20|25|30)\b[^\n.]{0,80}"
    r"(wrap(?:-?up)?|final|finish|end the lesson|complete the lesson|last question|one last time)"
)

ALLOWED_JSON_PATH_PARTS = {"levelProfiles"}


def assert_true(condition: bool, message: str) -> None:
    if not condition:
        raise AssertionError(message)


def scan_prompt_files() -> None:
    for directory in PROMPT_DIRS:
        if not directory.exists():
            continue
        for path in directory.rglob("*.txt"):
            text = path.read_text(encoding="utf-8")
            match = FORBIDDEN_NUMERIC_TIMING.search(text)
            if match is not None:
                raise AssertionError(f"Prompt template contains numeric lesson timing guidance: {path}: {match.group(0)!r}")


def walk_json(value, path=()):
    if isinstance(value, dict):
        for key, child in value.items():
            yield from walk_json(child, path + (str(key),))
    elif isinstance(value, list):
        for index, child in enumerate(value):
            yield from walk_json(child, path + (str(index),))
    elif isinstance(value, str):
        yield path, value


def scan_lesson_content() -> None:
    for path in LESSON_DIR.rglob("*.json"):
        data = json.loads(path.read_text(encoding="utf-8"))
        for json_path, _ in walk_json(data):
            pass
        def walk_objects(value, path_parts=()):
            if isinstance(value, dict):
                yield path_parts, value
                for key, child in value.items():
                    yield from walk_objects(child, path_parts + (str(key),))
            elif isinstance(value, list):
                for index, child in enumerate(value):
                    yield from walk_objects(child, path_parts + (str(index),))

        for object_path, obj in walk_objects(data):
            if any(part in ALLOWED_JSON_PATH_PARTS for part in object_path):
                continue
            for key in ("softWrapUpAfterUserTurn", "finalMessageAtUserTurn", "wrapUpAfterUserTurn"):
                assert_true(key not in obj, f"Scenario content must not define independent timing outside levelProfiles in {path}:{'.'.join(object_path)}: {key}")
        for json_path, text in walk_json(data):
            if any(part in ALLOWED_JSON_PATH_PARTS for part in json_path):
                continue
            match = FORBIDDEN_NUMERIC_TIMING.search(text)
            if match is not None:
                raise AssertionError(f"Scenario content contains numeric timing guidance outside levelProfiles: {path}:{'.'.join(json_path)}: {match.group(0)!r}")


def scan_runtime_code() -> None:
    prompt = PROMPT_BUILDER.read_text(encoding="utf-8")
    limits = LIMIT_HELPER.read_text(encoding="utf-8")
    snapshot = SNAPSHOT_BUILDER.read_text(encoding="utf-8")
    definition = SCENARIO_DEF.read_text(encoding="utf-8")
    doc = DOC.read_text(encoding="utf-8")

    assert_true("request.SoftLearnerTurnLimit > 0" in prompt, "Realtime prompt construction must preserve desktop resolved soft wrap threshold.")
    assert_true("request.HardLearnerTurnLimit > 0" in prompt, "Realtime prompt construction must preserve desktop resolved final threshold.")
    assert_true("request.SoftWrapUpAfterUserTurn > 0" in limits, "Backend normal chat must honor resolved level-profile soft wrap threshold from the request.")
    assert_true("request.FinalMessageAtUserTurn > 0" in limits, "Backend normal chat must honor resolved level-profile final threshold from the request.")
    assert_true("lesson.Metadata.SoftWrapUpAfterUserTurn = 0" in snapshot, "Published snapshot builder must clear legacy scenario soft wrap metadata.")
    assert_true("lesson.Metadata.FinalMessageAtUserTurn = 0" in snapshot, "Published snapshot builder must clear legacy scenario final metadata.")
    assert_true("softWrapUpAfterUserTurn = (int?)null" in definition, "Fallback definition JSON must not emit scenario soft wrap timing.")
    assert_true("Prompt templates" in doc and "must not contain independent numeric" in doc, "Timing source-of-truth documentation is missing prompt guidance.")


if __name__ == "__main__":
    scan_prompt_files()
    scan_lesson_content()
    scan_runtime_code()
    print("Lesson timing source-of-truth policy passed.")
