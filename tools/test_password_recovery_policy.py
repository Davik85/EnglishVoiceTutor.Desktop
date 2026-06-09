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

password_reset_service = (ROOT / "backend/EnglishVoiceTutor.Api/Services/Auth/PasswordResetService.cs").read_text(encoding="utf-8")
smtp_sender = (ROOT / "backend/EnglishVoiceTutor.Api/Services/Email/SmtpPasswordResetEmailSender.cs").read_text(encoding="utf-8")
auth_endpoints = (ROOT / "backend/EnglishVoiceTutor.Api/Endpoints/AuthEndpoints.cs").read_text(encoding="utf-8")
settings_vm = (ROOT / "ViewModels/SettingsViewModel.cs").read_text(encoding="utf-8")

for needle in [
    "RandomNumberGenerator.GetInt32(0, 1_000_000)",
    'ToString("D6", CultureInfo.InvariantCulture)',
    "HashToken(rawCode)",
]:
    if needle not in password_reset_service:
        raise SystemExit(f"Password reset code policy missing: {needle}")

for forbidden in [
    "ResetTokenByteLength",
    "Base64UrlEncode",
    "HashToken(rawToken)",
    "SendPasswordResetAsync(user, rawToken",
]:
    if forbidden in password_reset_service:
        raise SystemExit(f"Password reset still contains old long-token implementation detail: {forbidden}")

if "Reset code: {resetCode}" not in smtp_sender:
    raise SystemExit("Password reset email must show the six-digit code on a 'Reset code: 123456' style line.")

for forbidden in ["Reset code:\n", "reset token", "Reset token", "{resetToken}"]:
    if forbidden in smtp_sender:
        raise SystemExit(f"Password reset email still contains old token wording/format: {forbidden}")

if "PasswordChangeInvalidLengthMessage" not in auth_endpoints.split("ConfirmPasswordResetAsync", 1)[1].split("ChangePasswordAsync", 1)[0]:
    raise SystemExit("Too-short reset passwords must return the configured minimum-length validation message.")

if "PasswordOperationResultStatus.BackendUnavailable => BackendUxText.CouldNotConnect" not in settings_vm:
    raise SystemExit("Desktop password operations must reserve backend-unavailable messaging for connectivity/server failures.")

print("Password recovery/change static policy checks passed.")
