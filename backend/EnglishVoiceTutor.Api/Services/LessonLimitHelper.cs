using EnglishVoiceTutor.Api.Models;
using EnglishVoiceTutor.Api.Services.Cms;

namespace EnglishVoiceTutor.Api.Services;

public enum LessonRuntimePhase
{
    SetupContextSelection,
    ActiveRoleplay,
    WrapUp,
    Final,
    Completed
}

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

    public static int GetActiveRoleplayUserTurnCount(LessonChatRequest request)
    {
        return Math.Max(request.UserTurnNumber, request.LearnerTurnCount);
    }

    public static int GetRemainingLearnerTurns(LessonChatRequest request)
    {
        if (request.RemainingLearnerTurns > 0)
        {
            return request.RemainingLearnerTurns;
        }

        return Math.Max(GetHardLearnerTurnLimit(request) - GetActiveRoleplayUserTurnCount(request), 0);
    }

    public static LessonRuntimePhase GetRuntimePhase(LessonChatRequest request)
    {
        var requestedPhase = ParseRuntimePhase(request.LessonPhase);
        if (requestedPhase is LessonRuntimePhase.Completed or LessonRuntimePhase.SetupContextSelection)
        {
            return requestedPhase.Value;
        }

        var activeTurnCount = GetActiveRoleplayUserTurnCount(request);
        if (request.ShouldEndLessonNow || activeTurnCount >= GetHardLearnerTurnLimit(request) || requestedPhase == LessonRuntimePhase.Final)
        {
            return LessonRuntimePhase.Final;
        }

        if (activeTurnCount >= GetSoftLearnerTurnLimit(request) || requestedPhase == LessonRuntimePhase.WrapUp || request.HasWrapUpStarted)
        {
            return LessonRuntimePhase.WrapUp;
        }

        return LessonRuntimePhase.ActiveRoleplay;
    }

    public static bool IsFirstWrapUpInstruction(LessonChatRequest request)
    {
        return GetRuntimePhase(request) == LessonRuntimePhase.WrapUp && request.ShouldStartWrappingUp && !request.HasWrapUpStarted;
    }

    public static bool ShouldStartWrappingUp(LessonChatRequest request)
    {
        return GetRuntimePhase(request) == LessonRuntimePhase.WrapUp;
    }

    public static bool ShouldEndLessonNow(LessonChatRequest request)
    {
        return GetRuntimePhase(request) == LessonRuntimePhase.Final;
    }

    public static string ToContractValue(LessonRuntimePhase phase)
    {
        return phase switch
        {
            LessonRuntimePhase.SetupContextSelection => "setup_context_selection",
            LessonRuntimePhase.ActiveRoleplay => "active_roleplay",
            LessonRuntimePhase.WrapUp => "wrap_up",
            LessonRuntimePhase.Final => "final",
            LessonRuntimePhase.Completed => "completed",
            _ => "active_roleplay"
        };
    }

    private static LessonRuntimePhase? ParseRuntimePhase(string phase)
    {
        return phase?.Trim().ToLowerInvariant() switch
        {
            "setup_context_selection" or "setupcontextselection" => LessonRuntimePhase.SetupContextSelection,
            "active_roleplay" or "activeroleplay" => LessonRuntimePhase.ActiveRoleplay,
            "wrap_up" or "wrapup" => LessonRuntimePhase.WrapUp,
            "final" => LessonRuntimePhase.Final,
            "completed" => LessonRuntimePhase.Completed,
            _ => null
        };
    }
}
