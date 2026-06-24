#!/usr/bin/env python3
"""Policy checks for Admin CMS runtime-source visibility and fallback warnings."""
import pathlib

ROOT = pathlib.Path(__file__).resolve().parents[1]
ADMIN_JS = ROOT / "backend/EnglishVoiceTutor.Api/wwwroot/admin/admin.js"
MODELS = ROOT / "backend/EnglishVoiceTutor.Api/Services/Cms/CmsRuntimeContentModels.cs"
DOC = ROOT / "docs/cms-content-mvp-plan.md"


def read(path: pathlib.Path) -> str:
    return path.read_text(encoding="utf-8")


def require(text: str, needle: str, label: str) -> None:
    if needle not in text:
        raise AssertionError(f"Missing {label}: {needle}")


def forbid(text: str, needle: str, label: str) -> None:
    if needle in text:
        raise AssertionError(f"Forbidden {label}: {needle}")


admin_js = read(ADMIN_JS)
models = read(MODELS)
doc = read(DOC)

require(admin_js, 'const cmsSnapshotActive = flagsEnabled && effectiveSource === "CmsPublishedSnapshot" && validationSuccess && !fallbackUsed;', "strict CMS green-state rule")
require(admin_js, 'headline = "Static JSON emergency fallback is active"', "fallback headline")
require(admin_js, 'statusLabel = "Fallback active"', "fallback badge")
require(admin_js, 'positive = false', "default non-green state")
forbid(admin_js, 'if (fallbackUsed) { headline = "Fallback to static JSON is active"; positive = true; }', "old green fallback status")
require(admin_js, 'CMS edits affect learner lessons only when the CMS published snapshot is enabled, valid, and effectively active.', "operator warning copy")
require(admin_js, 'While static JSON fallback is active, CMS draft or published edits may not affect learner runtime.', "fallback impact copy")

require(models, 'Static JSON emergency fallback is active; learner runtime is using packaged static JSON', "backend fallback status message")
require(models, 'CMS draft or published edits may not affect learner lessons', "backend fallback warning")
require(models, 'Warnings = CreateWarnings(result)', "backend warning injection")

require(doc, 'CMS published snapshot is the intended primary learner runtime source.', "primary runtime source docs")
require(doc, 'Static JSON is an emergency fallback and initialization source.', "static JSON fallback docs")
require(doc, 'Static JSON fallback must be visible in Admin CMS', "fallback visibility docs")
print("CMS runtime source visibility policy passed.")
