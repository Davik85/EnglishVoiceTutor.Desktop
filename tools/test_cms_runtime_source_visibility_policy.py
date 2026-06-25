#!/usr/bin/env python3
"""Policy checks for Admin CMS runtime-source visibility and fallback warnings."""
import pathlib

ROOT = pathlib.Path(__file__).resolve().parents[1]
ADMIN_JS = ROOT / "backend/EnglishVoiceTutor.Api/wwwroot/admin/admin.js"
ADMIN_INDEX = ROOT / "backend/EnglishVoiceTutor.Api/wwwroot/admin/index.html"
APPSETTINGS = ROOT / "backend/EnglishVoiceTutor.Api/appsettings.json"
OPTIONS = ROOT / "backend/EnglishVoiceTutor.Api/Options/CmsContentOptions.cs"
MODELS = ROOT / "backend/EnglishVoiceTutor.Api/Services/Cms/CmsRuntimeContentModels.cs"
DOC = ROOT / "docs/cms-content-mvp-plan.md"
ADMIN_GUIDE = ROOT / "docs/CMS_PROMPT_MANAGEMENT_ADMIN_GUIDE.md"


def read(path: pathlib.Path) -> str:
    return path.read_text(encoding="utf-8")


def require(text: str, needle: str, label: str) -> None:
    if needle not in text:
        raise AssertionError(f"Missing {label}: {needle}")


def forbid(text: str, needle: str, label: str) -> None:
    if needle in text:
        raise AssertionError(f"Forbidden {label}: {needle}")


admin_js = read(ADMIN_JS)
admin_index = read(ADMIN_INDEX)
appsettings = read(APPSETTINGS)
options = read(OPTIONS)
models = read(MODELS)
doc = read(DOC)
admin_guide = read(ADMIN_GUIDE)

require(admin_js, 'const cmsSnapshotActive = flagsEnabled && effectiveSource === "CmsPublishedSnapshot" && validationSuccess && !fallbackUsed;', "strict CMS green-state rule")
require(admin_js, 'headline = "Learner runtime is using CMS published snapshot"', "healthy runtime headline")
require(admin_js, 'headline = "Learner runtime is using static JSON fallback"', "fallback headline")
require(admin_js, 'statusLabel = "Fallback active"', "fallback badge")
require(admin_js, 'positive = false', "default non-green state")
forbid(admin_js, 'if (fallbackUsed) { headline = "Fallback to static JSON is active"; positive = true; }', "old green fallback status")
for stale in [
    "Runtime still uses static JSON by default",
    "runtime still uses static JSON by default",
    "Runtime learner behavior still remains static JSON by default",
    "Runtime learner behavior still uses static JSON by default",
    "Learner runtime still uses static JSON by default",
    "static JSON remains the default",
    "do not change learner runtime defaults",
]:
    forbid(admin_js, stale, "stale Admin JS normal-state copy")
    forbid(admin_index, stale, "stale Admin overview normal-state copy")
require(admin_js, 'CMS edits affect learner lessons only when the CMS published snapshot is enabled, valid, and effectively active.', "operator warning copy")
require(admin_js, '{ label: "Emergency static JSON fallback enabled", value: getCmsResponseValue(status, "fallbackToStaticJson") }', "fallback enabled label")
require(admin_js, '{ label: "Currently using static JSON fallback", value: fallbackUsed }', "fallback used label")
require(admin_js, '{ label: "Actual learner runtime source", value: effectiveSource }', "actual runtime source label")
require(admin_index, 'Actual learner runtime source', "overview runtime source card")
require(admin_js, 'While static JSON fallback is active, CMS draft or published edits may not affect learner runtime.', "fallback impact copy")

require(models, 'Static JSON emergency fallback is active; learner runtime is using packaged static JSON', "backend fallback status message")
require(models, 'CMS draft or published edits may not affect learner lessons', "backend fallback warning")
require(models, 'Warnings = CreateWarnings(result)', "backend warning injection")

require(doc, 'CMS published snapshot is the intended primary learner runtime source.', "primary runtime source docs")
require(doc, 'Static JSON is an emergency fallback and initialization source.', "static JSON fallback docs")
require(doc, 'Static JSON fallback must be visible in Admin CMS', "fallback visibility docs")
require(admin_guide, "The name may describe the original seed source; it does **not** mean learners are currently using static JSON.", "content pack identity docs")
require(admin_guide, "**Emergency static JSON fallback enabled: Yes**", "fallback enabled docs")
require(admin_guide, "**Currently using static JSON fallback: No**", "fallback used docs")
require(admin_guide, "`Effective source = CmsPublishedSnapshot`", "healthy effective source docs")
require(admin_guide, "`Fallback used = No`", "healthy fallback docs")
print("CMS runtime source visibility policy passed.")

require(appsettings, '"ReadPublishedSnapshotEnabled": true', "appsettings CMS read default")
require(appsettings, '"UsePublishedSnapshotForRuntime": true', "appsettings CMS runtime default")
require(appsettings, '"FallbackToStaticJson": true', "appsettings fallback retained")
require(options, 'public bool ReadPublishedSnapshotEnabled { get; set; } = true;', "options CMS read default")
require(options, 'public bool UsePublishedSnapshotForRuntime { get; set; } = true;', "options CMS runtime default")
require(options, 'public bool FallbackToStaticJson { get; set; } = true;', "options fallback retained")
