using EnglishVoiceTutor.Api.Constants;
using EnglishVoiceTutor.Api.Models;

namespace EnglishVoiceTutor.Api.Services;

public static class LessonLimitHelper
{
    public static int GetSoftLearnerTurnLimit(LessonChatRequest request)
    {
        return request.SoftLearnerTurnLimit > 0
            ? request.SoftLearnerTurnLimit
            : ApiConstants.DefaultLessonSoftLearnerTurnLimit;
    }

    public static int GetHardLearnerTurnLimit(LessonChatRequest request)
    {
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

        return Math.Max(GetHardLearnerTurnLimit(request) - request.LearnerTurnCount, 0);
    }

    public static bool ShouldStartWrappingUp(LessonChatRequest request)
    {
        return request.ShouldStartWrappingUp || request.LearnerTurnCount >= GetSoftLearnerTurnLimit(request);
    }

    public static bool ShouldEndLessonNow(LessonChatRequest request)
    {
        return request.ShouldEndLessonNow || request.LearnerTurnCount >= GetHardLearnerTurnLimit(request);
    }
}
