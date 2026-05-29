namespace EnglishVoiceTutor.Desktop.Models.Access;

public sealed record AccessDisplayModel(
    AccessDisplayState State,
    string Message,
    bool? CanStartNewLesson,
    bool IsBackendDriven);
