using EnglishVoiceTutor.Api.Constants;
using EnglishVoiceTutor.Api.Models;

namespace EnglishVoiceTutor.Api.Services;

public static class LessonLimitHelper
{
    public static int GetSoftLearnerTurnLimit(LessonChatRequest request)
    {
        if (request.SoftWrapUpAfterUserTurn > 0)
        {
            return request.SoftWrapUpAfterUserTurn;
        }

        return request.SoftLearnerTurnLimit > 0
            ? request.SoftLearnerTurnLimit
            : ApiConstants.DefaultLessonSoftLearnerTurnLimit;
    }

    public static int GetHardLearnerTurnLimit(LessonChatRequest request)
    {
        if (request.FinalMessageAtUserTurn > 0)
        {
            return request.FinalMessageAtUserTurn;
        }

        return request.HardLearnerTurnLimit > 0
            ? request.HardLearnerTurnLimit
            : ApiConstants.DefaultLessonHardLearnerTurnLimit;
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
