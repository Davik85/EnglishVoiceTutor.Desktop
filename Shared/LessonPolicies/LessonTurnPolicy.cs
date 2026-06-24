namespace EnglishVoiceTutor.Shared.LessonPolicies;

public enum LessonTurnPhase
{
    SetupContextSelection,
    ActiveRoleplay,
    WrapUp,
    Final,
    Completed
}

public sealed record LessonTurnPolicyContext(
    string LessonType,
    string SelectedLevel,
    LessonTurnPhase CurrentPhase,
    int CurrentLearnerTurnCount,
    int? ContentSoftWrapUpTurn = null,
    int? ContentFinalTurn = null,
    bool HasSelectedContext = false);

public sealed record LessonTurnResult(
    bool ShouldCountUserTurn,
    int LearnerTurnCountBefore,
    int LearnerTurnCountAfter,
    int SoftWrapUpTurn,
    int FinalTurn,
    bool ShouldStartWrappingUp,
    bool ShouldUseFinalMessage,
    bool ShouldCompleteAfterAssistantMessage,
    bool IsFirstWrapUpTurn,
    LessonTurnPhase PhaseBefore,
    LessonTurnPhase PhaseAfter);

public static class LessonTurnPolicy
{
    public const string GuidedRoleplayLessonType = "guided_roleplay";
    public const string FreeConversationLessonType = "free_conversation";
    public const int BeginnerGuidedSoftWrapUpTurn = 10;
    public const int BeginnerGuidedFinalTurn = 15;
    public const int AdvancedGuidedSoftWrapUpTurn = 20;
    public const int AdvancedGuidedFinalTurn = 25;
    public const int FreeConversationSoftWrapUpTurn = 25;
    public const int FreeConversationFinalTurn = 30;

    public static LessonTurnResult EvaluateUserInput(LessonTurnPolicyContext context, bool isValidEnglishTranscript)
    {
        var softWrapUpTurn = ResolveSoftWrapUpTurn(context);
        var finalTurn = ResolveFinalTurn(context);
        var canCount = isValidEnglishTranscript && context.CurrentPhase is LessonTurnPhase.ActiveRoleplay or LessonTurnPhase.WrapUp;
        var after = canCount ? Math.Min(context.CurrentLearnerTurnCount + 1, finalTurn) : context.CurrentLearnerTurnCount;
        var phaseAfter = DerivePhase(after, softWrapUpTurn, finalTurn, context.CurrentPhase);
        var shouldUseFinalMessage = canCount && phaseAfter == LessonTurnPhase.Final;
        return new LessonTurnResult(
            canCount,
            context.CurrentLearnerTurnCount,
            after,
            softWrapUpTurn,
            finalTurn,
            canCount && phaseAfter == LessonTurnPhase.WrapUp,
            shouldUseFinalMessage,
            shouldUseFinalMessage,
            canCount && context.CurrentPhase != LessonTurnPhase.WrapUp && phaseAfter == LessonTurnPhase.WrapUp,
            context.CurrentPhase,
            phaseAfter);
    }

    public static LessonTurnPhase DerivePhase(int activeRoleplayUserTurnCount, int softWrapUpTurn, int finalTurn, LessonTurnPhase currentPhase = LessonTurnPhase.ActiveRoleplay)
    {
        if (currentPhase == LessonTurnPhase.Completed)
        {
            return LessonTurnPhase.Completed;
        }

        if (currentPhase == LessonTurnPhase.SetupContextSelection)
        {
            return LessonTurnPhase.SetupContextSelection;
        }

        if (activeRoleplayUserTurnCount >= finalTurn)
        {
            return LessonTurnPhase.Final;
        }

        if (activeRoleplayUserTurnCount >= softWrapUpTurn)
        {
            return LessonTurnPhase.WrapUp;
        }

        return LessonTurnPhase.ActiveRoleplay;
    }

    public static int ResolveSoftWrapUpTurn(LessonTurnPolicyContext context)
    {
        if (context.ContentSoftWrapUpTurn is > 0)
        {
            return context.ContentSoftWrapUpTurn.Value;
        }

        if (IsFreeConversation(context.LessonType))
        {
            return FreeConversationSoftWrapUpTurn;
        }

        return IsBeginnerLevel(context.SelectedLevel)
            ? BeginnerGuidedSoftWrapUpTurn
            : AdvancedGuidedSoftWrapUpTurn;
    }

    public static int ResolveFinalTurn(LessonTurnPolicyContext context)
    {
        if (context.ContentFinalTurn is > 0)
        {
            return context.ContentFinalTurn.Value;
        }

        if (IsFreeConversation(context.LessonType))
        {
            return FreeConversationFinalTurn;
        }

        return IsBeginnerLevel(context.SelectedLevel)
            ? BeginnerGuidedFinalTurn
            : AdvancedGuidedFinalTurn;
    }

    private static bool IsBeginnerLevel(string selectedLevel)
    {
        var level = selectedLevel.TrimStart();
        return level.StartsWith("A1", StringComparison.OrdinalIgnoreCase)
            || level.StartsWith("A2", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsFreeConversation(string lessonType)
    {
        return string.Equals(lessonType, FreeConversationLessonType, StringComparison.OrdinalIgnoreCase);
    }
}
