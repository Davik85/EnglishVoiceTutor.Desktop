"""Policy checks for CMS-managed tutor display names in learner runtime."""
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]

def read(path: str) -> str:
    return (ROOT / path).read_text(encoding="utf-8")

def require(path: str, needle: str) -> None:
    text = read(path)
    if needle not in text:
        raise AssertionError(f"Missing {needle!r} in {path}")

require("Models/LessonContent/LessonScenario.cs", "List<TutorRuntimeMetadata> TutorProfiles")
require("Models/LessonContent/TutorRuntimeMetadata.cs", "public string TutorId")
require("Models/LessonContent/TutorRuntimeMetadata.cs", "public string DisplayName")
require("backend/EnglishVoiceTutor.Api/Program.cs", "scenario.Lesson.TutorProfiles = result.Content.TutorBehaviorProfiles")
require("backend/EnglishVoiceTutor.Api/Program.cs", "TutorId = profile.TutorId.Trim()")
require("backend/EnglishVoiceTutor.Api/Program.cs", "DisplayName = profile.DisplayName.Trim()")
require("ViewModels/MainViewModel.cs", "FirstOrDefault(tutor => string.Equals(tutor.TutorId, avatar.Id")
require("ViewModels/MainViewModel.cs", "profile.Id = avatar.Id")
require("ViewModels/LessonChatViewModel.cs", "TutorAvatarDisplayName = string.IsNullOrWhiteSpace(this.tutorProfile.DisplayName)")
require("backend/EnglishVoiceTutor.Api/Services/Cms/CmsContentValidationService.cs", "RequiredTutorBehaviorProfileIds = TutorAvatarOptions.All")
require("backend/EnglishVoiceTutor.Api/wwwroot/admin/index.html", "This is the learner-facing tutor name. It is shown in new lessons and Settings after Save draft + Publish. tutorId remains the stable internal ID.")
require("backend/EnglishVoiceTutor.Api/wwwroot/admin/index.html", "Do not duplicate the tutor name here unless needed. The Display name field is the source of truth")
require("backend/EnglishVoiceTutor.Api/Constants/ApiConstants.cs", "RuntimeTutorOptionsRoute")
require("backend/EnglishVoiceTutor.Api/Program.cs", "HandleGetRuntimeTutorOptionsAsync")
require("Services/BackendLessonContentClient.cs", "GetRuntimeTutorOptionsAsync")
require("ViewModels/SettingsViewModel.cs", "new TutorAvatarOption(localOption.Id, displayName)")
require("Models/LessonChatBackendRequest.cs", "public string TutorDisplayName")
require("backend/EnglishVoiceTutor.Api/Models/LessonChatRequest.cs", "public string TutorDisplayName")
require("backend/EnglishVoiceTutor.Api/Services/LessonPromptBuilder.cs", "Your current tutor display name is")
print("Tutor display name runtime policy passed.")
