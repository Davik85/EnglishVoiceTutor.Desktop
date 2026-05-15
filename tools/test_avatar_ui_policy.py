from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
view = ROOT / "Views" / "LessonChatView.xaml"
text = view.read_text(encoding="utf-8")
errors: list[str] = []

if re.search(r'ToolTip\s*=\s*"\{Binding\s+AvatarImagePath\b', text):
    errors.append("LessonChatView.xaml must not bind a tooltip to AvatarImagePath.")

if re.search(r'ToolTip\s*=\s*"\{Binding\s+AvatarAnimationAssetPath\b', text):
    errors.append("LessonChatView.xaml must not bind a tooltip to AvatarAnimationAssetPath.")

for match in re.finditer(r'ToolTip\s*=\s*"([^"]*)"', text):
    tooltip = match.group(1)
    if any(token.lower() in tooltip.lower() for token in (".png", ".jpg", ".jpeg", ".webp", "assets/", "avatars/")):
        errors.append(f"User-visible tooltip exposes technical asset data: {tooltip}")

if "gif:AnimationBehavior.SourceUri=\"{Binding AvatarAnimationAssetUri" not in text:
    errors.append("Avatar animation source binding must remain intact.")

if errors:
    for error in errors:
        print(f"ERROR: {error}")
    sys.exit(1)

print("Avatar UI policy passed: avatar source binding remains and no technical avatar tooltip is exposed.")
