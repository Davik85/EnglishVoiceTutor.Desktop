namespace EnglishVoiceTutor.Shared.LessonPolicies;

public enum LessonTurnPhase
{
    SetupContextSelection,
    ActiveRoleplay,
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
        var canCount = isValidEnglishTranscript && context.CurrentPhase == LessonTurnPhase.ActiveRoleplay;
        var after = canCount ? Math.Min(context.CurrentLearnerTurnCount + 1, finalTurn) : context.CurrentLearnerTurnCount;
        var shouldUseFinalMessage = canCount && after >= finalTurn;
        return new LessonTurnResult(
            canCount,
            context.CurrentLearnerTurnCount,
            after,
            softWrapUpTurn,
            finalTurn,
            canCount && after >= softWrapUpTurn && after < finalTurn,
            shouldUseFinalMessage,
            shouldUseFinalMessage,
            context.CurrentPhase,
            shouldUseFinalMessage ? LessonTurnPhase.Completed : context.CurrentPhase);
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
