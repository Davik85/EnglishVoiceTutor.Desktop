#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]

checks = {
    "backend reset request route": (ROOT / "backend/EnglishVoiceTutor.Api/Constants/ApiConstants.cs", "AuthPasswordResetRequestRoute"),
    "backend reset confirm route": (ROOT / "backend/EnglishVoiceTutor.Api/Constants/ApiConstants.cs", "AuthPasswordResetConfirmRoute"),
    "backend change password route": (ROOT / "backend/EnglishVoiceTutor.Api/Constants/ApiConstants.cs", "AuthChangePasswordRoute"),
    "backend change password requires auth": (ROOT / "backend/EnglishVoiceTutor.Api/Endpoints/AuthEndpoints.cs", "MapPost(ApiConstants.AuthChangePasswordRoute, ChangePasswordAsync).RequireAuthorization()"),
    "backend smtp options": (ROOT / "backend/EnglishVoiceTutor.Api/Options/SmtpEmailOptions.cs", "support@languagevoicetutor.com"),
    "desktop reset endpoint": (ROOT / "Constants/BackendConstants.cs", "AuthPasswordResetRequestEndpoint"),
    "desktop reset command": (ROOT / "ViewModels/SettingsViewModel.cs", "RequestPasswordResetAsync"),
    "desktop reset binding": (ROOT / "Views/SettingsView.xaml", "ConfirmPasswordResetCommand"),
    "desktop change binding": (ROOT / "Views/SettingsView.xaml", "ChangePasswordCommand"),
}

for label, (path, needle) in checks.items():
    text = path.read_text(encoding="utf-8")
    if needle not in text:
        raise SystemExit(f"Missing {label}: {needle} in {path}")

changed_files = [path for path in [item[0] for item in checks.values()] if path.exists()]
secret_needles = [
    "sk-",
    "OPENAI_API_KEY=",
    "SMTP_PASSWORD=",
    "PADDLE_API_KEY=",
    "BEGIN PRIVATE KEY",
]
for path in set(changed_files):
    text = path.read_text(encoding="utf-8")
    for needle in secret_needles:
        if needle in text:
            raise SystemExit(f"Possible secret marker {needle!r} found in {path}")

print("Password recovery/change static policy checks passed.")
