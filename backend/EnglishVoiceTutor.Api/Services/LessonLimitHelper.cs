using EnglishVoiceTutor.Api.Constants;
using EnglishVoiceTutor.Api.Models;
using EnglishVoiceTutor.Api.Services.Cms;

namespace EnglishVoiceTutor.Api.Services;

public static class LessonLimitHelper
{
    public static int GetSoftLearnerTurnLimit(LessonChatRequest request)
    {
        return ResolveLevelTurnLimits(request).WrapUpAfterUserTurn;
    }

    public static int GetHardLearnerTurnLimit(LessonChatRequest request)
    {
        return ResolveLevelTurnLimits(request).FinalMessageAtUserTurn;
    }

    private static CmsLevelProfile ResolveLevelTurnLimits(LessonChatRequest request)
    {
        return CmsLevelProfiles.Resolve(string.IsNullOrWhiteSpace(request.SelectedLevel) ? request.Level : request.SelectedLevel);
    }

    public static int GetRemainingLearnerTurns(LessonChatRequest request)
    {
        if (request.RemainingLearnerTurns > 0)
        {
            return request.RemainingLearnerTurns;
        }

        return Math.Max(GetHardLearnerTurnLimit(request) - Math.Max(request.UserTurnNumber, request.LearnerTurnCount), 0);
    }

    public static bool ShouldStartWrappingUp(LessonChatRequest request)
    {
        return request.ShouldStartWrappingUp || Math.Max(request.UserTurnNumber, request.LearnerTurnCount) >= GetSoftLearnerTurnLimit(request);
    }

    public static bool ShouldEndLessonNow(LessonChatRequest request)
    {
        return request.ShouldEndLessonNow || Math.Max(request.UserTurnNumber, request.LearnerTurnCount) >= GetHardLearnerTurnLimit(request);
    }
}
