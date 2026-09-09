using EnglishVoiceTutor.Api.Constants;
using EnglishVoiceTutor.Api.Models;
using EnglishVoiceTutor.Shared.StudyLanguages;

namespace EnglishVoiceTutor.Api.Services;

public sealed class MockLessonHintService : ILessonHintService
{
    public Task<LessonHintResponse> CreateHintAsync(LessonChatRequest request, CancellationToken cancellationToken = default)
    {
        var hintText = StudyLanguageCatalog.GetById(request.TargetLanguageId).Id switch
        {
            "fr" => "Pouvez-vous répéter, s'il vous plaît ?",
            "de" => "Könnten Sie das bitte wiederholen?",
            "pt" => "Pode repetir, por favor?",
            "es" => "¿Puede repetirlo, por favor?",
            "it" => "Può ripetere, per favore?",
            _ => ApiConstants.MockHintText
        };

        return Task.FromResult(new LessonHintResponse
        {
            HintText = hintText
        });
    }
}
