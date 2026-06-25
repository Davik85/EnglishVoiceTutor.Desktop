using System.Text;
using EnglishVoiceTutor.Api.Constants;
using EnglishVoiceTutor.Api.Models;
using EnglishVoiceTutor.Api.Models.RealtimeVoice;
using EnglishVoiceTutor.Api.Services.Cms;
using EnglishVoiceTutor.Shared.StudyLanguages;

namespace EnglishVoiceTutor.Api.Services;

public sealed class LessonPromptBuilder
{
    private const string LessonContextHeader = "Lesson context (already selected by learner):";
    private const string TutorAvatarProfileHeader = "Tutor avatar profile (stable identity):";
    private const string LearnerProfileHeader = "Learner profile:";
    private const string TargetStudyLanguageHeader = "TARGET STUDY LANGUAGE:";
    private const string LessonLengthHeader = "Lesson length metadata:";
    private const string ActiveLevelProfileHeader = "Active level profile:";
    private const string RecentConversationHeader = "Recent active lesson conversation context (oldest to newest):";
    private const string NoRecentConversationContext = "- No recent conversation messages were provided.";
    private const string UserMessageHeader = "Learner latest message:";
    private const string LearnerDraftHeader = "Learner draft / latest input:";
    private const string LastBotMessageHeader = "Latest bot message:";
    private const string CurrentTurnTaskHeader = "Current turn task:";
    private const string HintTaskHeader = "Hint task:";
    private const string FeedbackTaskHeader = "Feedback task:";
    private const string NormalChatMode = "normal lesson chat";
    private const string RealtimeVoiceMode = "realtime voice conversation";

    private readonly TutorAvatarProfileProvider _avatarProfileProvider;

    public LessonPromptBuilder(TutorAvatarProfileProvider avatarProfileProvider)
    {
        _avatarProfileProvider = avatarProfileProvider;
    }

    public string BuildInput(LessonChatRequest request)
    {
        var prompt = new StringBuilder();
        var avatarProfile = ResolveRequestTutorProfile(request, _avatarProfileProvider.GetById(request.TutorAvatarId));

        AppendLessonContext(prompt, request, avatarProfile);
        AppendTargetStudyLanguage(prompt, request);
        AppendCanonicalTeachingPolicy(prompt, request, avatarProfile, NormalChatMode);
        AppendAvatarProfile(prompt, avatarProfile);
        AppendLearnerProfile(prompt, request);
        AppendLessonLength(prompt, request);
        AppendRecentConversation(prompt, request.RecentMessages);

        prompt.AppendLine(UserMessageHeader);
        prompt.AppendLine(request.UserMessage);
        prompt.AppendLine();

        if (string.Equals(request.RequestPurpose, "feedback", StringComparison.OrdinalIgnoreCase))
        {
            prompt.AppendLine(FeedbackTaskHeader);
            AppendFeedbackOnlyTask(prompt, request, avatarProfile);
        }
        else
        {
            prompt.AppendLine(CurrentTurnTaskHeader);
            if (IsFreeConversation(request))
            {
                AppendFreeConversationTask(prompt, request, avatarProfile);
            }
            else
            {
                AppendGuidedRoleplayTask(prompt, request, avatarProfile);
            }
        }

        return prompt.ToString();
    }


    public string BuildRealtimeInstructions(RealtimeVoiceSessionStartRequest request)
    {
        var prompt = new StringBuilder();
        var chatRequest = CreateLessonChatRequest(request);
        var avatarProfile = CreateRealtimeTutorProfile(request, _avatarProfileProvider.GetById(chatRequest.TutorAvatarId));

        prompt.AppendLine("You are the realtime voice engine for English Voice Tutor Desktop.");
        prompt.AppendLine("Voice-first rule: every assistant response must produce audio and a matching transcript from the same Realtime response id and same turn. Do not rely on separate TTS or separate text generation.");
        prompt.AppendLine("Use the canonical lesson teaching policy below. Realtime changes only audio transport and spoken-friendly formatting; it must not change teaching behavior.");
        prompt.AppendLine();

        AppendLessonContext(prompt, chatRequest, avatarProfile);
        AppendTargetStudyLanguage(prompt, chatRequest);
        AppendCanonicalTeachingPolicy(prompt, chatRequest, avatarProfile, RealtimeVoiceMode);
        AppendAvatarProfile(prompt, avatarProfile);
        AppendLearnerProfile(prompt, chatRequest);
        AppendLessonLength(prompt, chatRequest);
        AppendRecentConversation(prompt, chatRequest.RecentMessages);

        prompt.AppendLine("Realtime output format:");
        prompt.AppendLine("- Speak naturally for voice, but follow the same lesson rules as normal Lesson Chat.");
        prompt.AppendLine("- Do not output JSON for realtime turns.");
        prompt.AppendLine("- Assistant audio and assistant transcript must come from this same realtime response.");
        prompt.AppendLine("- Ask at most one question in a turn.");
        prompt.AppendLine($"- Target-language lesson lock applies to every spoken realtime response: use {ResolveTargetLanguage(chatRequest).LanguageLockName}.");
        prompt.AppendLine();

        return prompt.ToString();
    }

    public string BuildRealtimeResponseInstructions(RealtimeVoiceSessionStartRequest request)
    {
        var chatRequest = CreateLessonChatRequest(request);
        var avatarProfile = CreateRealtimeTutorProfile(request, _avatarProfileProvider.GetById(chatRequest.TutorAvatarId));
        var prompt = new StringBuilder();

        prompt.AppendLine($"Respond now as {avatarProfile.DisplayName}, the selected tutor profile.");
        prompt.AppendLine("Follow the canonical lesson teaching policy from the session instructions.");
        prompt.AppendLine($"Target-language lesson lock: speak {ResolveTargetLanguage(chatRequest).LanguageLockName} only, even if the learner asks for Finnish, Russian, Spanish, English, or another language.");
        prompt.AppendLine("Produce assistant audio and a matching assistant transcript from this same Realtime response.");
        prompt.AppendLine($"Current counted learner turn: {chatRequest.LearnerTurnCount} of {LessonLimitHelper.GetHardLearnerTurnLimit(chatRequest)}.");
        if (LessonLimitHelper.GetRuntimePhase(chatRequest) == LessonRuntimePhase.Final)
        {
            prompt.AppendLine("This is the final turn. Say the final lesson message first. Do not ask another question.");
            if (!string.IsNullOrWhiteSpace(chatRequest.ConversationFinalMessage))
            {
                prompt.AppendLine($"Exact final message from lesson JSON: {chatRequest.ConversationFinalMessage.Trim()}");
            }
        }
        else if (LessonLimitHelper.GetRuntimePhase(chatRequest) == LessonRuntimePhase.WrapUp)
        {
            prompt.AppendLine(LessonLimitHelper.IsFirstWrapUpInstruction(chatRequest)
                ? "Start closing the current scenario now. Do not restart the greeting or scenario setup."
                : "Continue closing the current scenario. Do not repeat the first wrap-up transition, restart the greeting, or reset the scenario.");
            if (!string.IsNullOrWhiteSpace(chatRequest.ConversationWrapUpMessage))
            {
                prompt.AppendLine($"Wrap-up direction from lesson JSON: {chatRequest.ConversationWrapUpMessage.Trim()}");
            }
        }

        if (IsFreeConversation(chatRequest))
        {
            prompt.AppendLine("This is Free Conversation: safe open topic selection is allowed.");
        }
        else
        {
            prompt.AppendLine($"This is guided roleplay. Continue the selected canonical scenario: {chatRequest.SelectedContextTitle}.");
            if (!string.IsNullOrWhiteSpace(chatRequest.SelectedContextLocalizedTitle))
            {
                prompt.AppendLine($"Localized selected scenario shown to learner: {chatRequest.SelectedContextLocalizedTitle}.");
            }
            prompt.AppendLine("The canonical scenario metadata may be in English; it is stable semantic identity, not an instruction to answer in English.");
            prompt.AppendLine($"Continue from the last visible tutor message: {chatRequest.LastBotMessage}.");
            prompt.AppendLine("Do not ask the learner to choose a topic, choose a situation, or request unrelated help/tips.");
            prompt.AppendLine("If the learner goes off-topic, briefly acknowledge and redirect back to the selected lesson goal and context.");
        }

        AppendLevelSpecificRealtimeRules(prompt, chatRequest, avatarProfile);

        return prompt.ToString();
    }

    private static void AppendGuidedRoleplayTask(StringBuilder prompt, LessonChatRequest request, TutorAvatarProfile avatarProfile)
    {
        prompt.AppendLine($"Respond to the learner's latest message as {avatarProfile.DisplayName}, the selected tutor avatar, as part of the selected situation.");
        prompt.AppendLine("Use learner profile as stable context and recent conversation as active lesson context.");
        prompt.AppendLine("Learner profile display name is personalization metadata, not an automatically spoken roleplay fact.");
        prompt.AppendLine("Treat the learner's name as known only when current lesson/session conversation context includes it.");
        prompt.AppendLine("For introduction scenarios, ask the learner's name when it has not appeared in the current lesson/session conversation yet.");
        prompt.AppendLine("If the learner profile includes a learning goal, use it as gentle context without overriding the selected topic or situation.");
        prompt.AppendLine("Interpret the learner message by the provided lesson phase first, not by phrase matching.");
        prompt.AppendLine("The lesson is already in active roleplay unless the request explicitly says setup phase.");
        prompt.AppendLine("Never restart the lesson setup during active roleplay.");
        prompt.AppendLine("Never ask the learner to choose a situation during active roleplay.");
        prompt.AppendLine("If a learner message matches or resembles a scenario title during active roleplay, treat it as a normal learner reply and continue the current stage.");
        prompt.AppendLine("Do not skip introduction or progression steps because a scenario title phrase appears again.");
        prompt.AppendLine("If the learner asks a meta question such as what to say or asks for an explanation, answer briefly as a tutor, then immediately continue the same roleplay scenario.");
        prompt.AppendLine("Use RecentMessages and LastBotMessage to preserve continuity from the latest exchange.");
        prompt.AppendLine("Do not repeat the opening line unless this is the first active roleplay turn.");
        prompt.AppendLine("Do not restart the lesson.");
        prompt.AppendLine("Do not ask the learner to choose a topic or context.");
        prompt.AppendLine("Stay inside the selected roleplay context.");
        prompt.AppendLine("Keep the base scenario goal and topic fixed; do not turn the lesson into unrelated free conversation.");
        prompt.AppendLine("Adapt difficulty to the selected level and active level profile.");
        if (IsA1(request))
        {
            AppendA1StrictRules(prompt, request);
        }
        prompt.AppendLine("For A1/A2, ask only one question at a time.");
        prompt.AppendLine("For B1/B2, you may use slightly richer natural responses, but every turn must stay inside the selected scenario.");
        prompt.AppendLine("The setup and context choice are already complete and did not count as lesson turns.");
        prompt.AppendLine("Do not ask for native language.");
        AppendTutorIdentityRules(prompt, avatarProfile);
        AppendGuidedRoleplayRetentionRules(prompt);
        AppendNaturalRoleplayCorrectionPolicy(prompt);
        AppendScenarioContinuityPolicy(prompt);

        if (LessonLimitHelper.ShouldEndLessonNow(request))
        {
            prompt.AppendLine("This is the hard-limit final turn. Give the lesson final message first and do not ask a new question.");
            if (!string.IsNullOrWhiteSpace(request.ConversationFinalMessage))
            {
                prompt.AppendLine($"Use this final message from lesson JSON: {request.ConversationFinalMessage.Trim()}");
            }
        }
        else if (LessonLimitHelper.ShouldStartWrappingUp(request))
        {
            prompt.AppendLine(LessonLimitHelper.IsFirstWrapUpInstruction(request)
                ? "The lesson has just reached wrap-up. Start closing the current selected scenario; do not restart it."
                : "The lesson is already in wrap-up. Continue closing the current selected scenario; do not repeat the first wrap-up transition or restart it.");
            if (!string.IsNullOrWhiteSpace(request.ConversationWrapUpMessage))
            {
                prompt.AppendLine($"Prefer this wrap-up direction from lesson JSON: {request.ConversationWrapUpMessage.Trim()}");
            }
        }
        else
        {
            prompt.AppendLine("Continue the dialogue naturally with one next question in the same scenario.");
        }
    }

    private static void AppendFreeConversationTask(StringBuilder prompt, LessonChatRequest request, TutorAvatarProfile avatarProfile)
    {
        prompt.AppendLine($"Respond to the learner's latest message as {avatarProfile.DisplayName}, the selected tutor avatar.");
        prompt.AppendLine("This is Free Conversation study-language practice, not a guided roleplay.");
        prompt.AppendLine("The learner may choose any safe topic.");
        prompt.AppendLine("Follow the safety boundaries from lesson instructions.");
        prompt.AppendLine("Keep the conversation in the selected target study language.");
        prompt.AppendLine("Adapt difficulty to the selected level and active level profile.");
        prompt.AppendLine("Keep responses concise and suitable for voice.");
        prompt.AppendLine("Correct lightly and naturally; do not overcorrect.");
        prompt.AppendLine("If unsafe or provocative content appears, refuse briefly and redirect to a safe topic for practice in the selected target study language.");
        prompt.AppendLine("Use learner profile as stable context and recent conversation as active conversation context.");
        prompt.AppendLine("Do not ask for native language.");
        AppendTutorIdentityRules(prompt, avatarProfile);

        if (LessonLimitHelper.ShouldEndLessonNow(request))
        {
            prompt.AppendLine("This is the hard-limit final turn. Give a short friendly closing message and do not ask a new question.");
            if (!string.IsNullOrWhiteSpace(request.ConversationFinalMessage))
            {
                prompt.AppendLine($"Use this final message from lesson JSON: {request.ConversationFinalMessage.Trim()}");
            }
        }
        else if (LessonLimitHelper.ShouldStartWrappingUp(request))
        {
            prompt.AppendLine(LessonLimitHelper.IsFirstWrapUpInstruction(request)
                ? "The conversation has just reached wrap-up. Start closing the current conversation."
                : "The conversation is already in wrap-up. Continue closing; do not repeat the first wrap-up transition.");
            prompt.AppendLine("Ask at most one natural follow-up question if it helps the wrap-up.");
            if (!string.IsNullOrWhiteSpace(request.ConversationWrapUpMessage))
            {
                prompt.AppendLine($"Prefer this wrap-up direction from lesson JSON: {request.ConversationWrapUpMessage.Trim()}");
            }
        }
        else
        {
            prompt.AppendLine("Ask one natural follow-up question unless the learner needs a brief correction or safe redirection first.");
        }
    }

    private static void AppendFeedbackOnlyTask(StringBuilder prompt, LessonChatRequest request, TutorAvatarProfile avatarProfile)
    {
        prompt.AppendLine("Create feedback only for the exact learner source message above. Do not use the latest or any other user message as the feedback source.");
        prompt.AppendLine($"- SourceMessageId: {request.SourceMessageId}");
        prompt.AppendLine($"- SourceMessageKind: {request.SourceMessageKind}");
        prompt.AppendLine("- Return a short placeholder botReply such as: Feedback ready.");
        prompt.AppendLine("- The feedback object is the only content shown in the feedback panel.");
        prompt.AppendLine("- Keep feedback kind, brief, level-appropriate, and in the selected target study language.");
        prompt.AppendLine("- Fill every structured feedback field with learner-friendly target-language text: shortText, correctedVersion, grammarTip, vocabularyTip, cultureTip, naturalVersion.");
        prompt.AppendLine("- Do not mention JSON field names or technical labels in the feedback text.");
        prompt.AppendLine($"- Do not claim to be anyone except {avatarProfile.DisplayName}, the selected tutor profile.");

        if (string.Equals(request.SourceMessageKind, "ContextSelection", StringComparison.OrdinalIgnoreCase))
        {
            prompt.AppendLine("Context-selection feedback mode:");
            prompt.AppendLine("- The learner was choosing or suggesting the scenario/context, not answering the active roleplay yet.");
            prompt.AppendLine("- Treat the text as a short setup phrase or scenario choice.");
            prompt.AppendLine("- Explicitly say this phrase is clear for choosing the situation when it is understandable.");
            prompt.AppendLine("- If needed, correct only capitalization and punctuation in correctedVersion.");
            prompt.AppendLine("- Suggest a more natural full sentence in naturalVersion, for example: Let's talk about next steps.");
            prompt.AppendLine("- Explicitly say this phrase starts the scenario and is not the learner's roleplay answer yet.");
            prompt.AppendLine("- Good grammarTip pattern: This is a phrase, not a full sentence. That is okay for choosing a situation.");
            prompt.AppendLine("- Good cultureTip pattern: In the real scenario, after choosing the topic, you would give your update or roleplay answer next.");
            prompt.AppendLine("- Do not say the learner did not give an update.");
            prompt.AppendLine("- Do not say this does not fit a daily standup update.");
            prompt.AppendLine("- Do not say the learner repeated the situation name.");
            prompt.AppendLine("- Do not treat the context phrase as an active roleplay reply.");
            return;
        }

        prompt.AppendLine("Active roleplay feedback mode:");
        prompt.AppendLine("- Evaluate the exact source message as the learner's active roleplay or voice transcript turn.");
        prompt.AppendLine("- Give normal roleplay feedback connected to the selected scenario, recent conversation, and level profile.");
        prompt.AppendLine("- Correct grammar and naturalness without overcorrecting.");
    }


    public string BuildHintInput(LessonChatRequest request)
    {
        var prompt = new StringBuilder();
        var avatarProfile = ResolveRequestTutorProfile(request, _avatarProfileProvider.GetById(request.TutorAvatarId));

        AppendLessonContext(prompt, request, avatarProfile, includeNativeLanguage: false);
        AppendCanonicalTeachingPolicy(prompt, request, avatarProfile, NormalChatMode);
        AppendAvatarProfile(prompt, avatarProfile);
        AppendLearnerProfile(prompt, request);
        AppendRecentConversation(prompt, request.RecentMessages);

        prompt.AppendLine(LastBotMessageHeader);
        prompt.AppendLine(request.LastBotMessage);
        prompt.AppendLine();

        prompt.AppendLine(LearnerDraftHeader);
        prompt.AppendLine(request.UserMessage);
        prompt.AppendLine();

        prompt.AppendLine(HintTaskHeader);
        if (IsFreeConversation(request))
        {
            AppendFreeConversationHintTask(prompt, request, avatarProfile);
        }
        else
        {
            AppendGuidedRoleplayHintTask(prompt, request, avatarProfile);
        }

        return prompt.ToString();
    }

    private static void AppendTargetStudyLanguage(StringBuilder prompt, LessonChatRequest request)
    {
        var targetLanguage = ResolveTargetLanguage(request);
        prompt.AppendLine(TargetStudyLanguageHeader);
        prompt.AppendLine($"The learner is practicing {targetLanguage.TutorInstructionName}.");
        prompt.AppendLine($"All tutor-facing lesson content must be in {targetLanguage.TutorInstructionName}.");
        prompt.AppendLine($"Use {targetLanguage.TutorInstructionName} for tutor replies, roleplay, hints, feedback, corrections, examples, and summary.");
        prompt.AppendLine("Do not switch to another language even if the learner asks.");
        prompt.AppendLine($"If the learner uses another language, gently answer in {targetLanguage.TutorInstructionName} and guide them back to the lesson.");
        prompt.AppendLine($"The lesson JSON scenario text is a semantic plan. Adapt it naturally into {targetLanguage.TutorInstructionName}; do not treat English wording in JSON as the output language.");
        prompt.AppendLine($"The lesson JSON scenario text is semantic metadata. Use it for meaning, scenario, and teaching intent, then express tutor-facing output in {targetLanguage.TutorInstructionName}.");
        prompt.AppendLine();
    }

    private static void AppendGuidedRoleplayHintTask(StringBuilder prompt, LessonChatRequest request, TutorAvatarProfile avatarProfile)
    {
        prompt.AppendLine("Give one short target-language hint the learner can use next in this exact situation, following the active level profile hint strategy.");
        prompt.AppendLine("A1: a fuller sentence starter is okay. A2: use a less complete sentence starter. B1: suggest structure or a useful phrase. B2: suggest natural direction or tone.");
        prompt.AppendLine($"The hint must answer or continue from the learner's point of view, not {avatarProfile.DisplayName}'s point of view.");
        prompt.AppendLine("The hint should help the learner respond to the latest bot message and recent conversation.");
        prompt.AppendLine("If the learner profile includes a display name, hint examples may use that name when appropriate.");
        prompt.AppendLine("If the learner's real name or personal detail is unknown, use placeholders such as [your name].");
        prompt.AppendLine("Do not invent a learner name.");
        prompt.AppendLine($"Do not use {avatarProfile.DisplayName} or the tutor avatar name as the learner's name.");

        if (IsIntroductionsSubtopic(request))
        {
            prompt.AppendLine("For introductions, prefer examples like: \"My name is [your name].\", \"I'm [your name].\", \"I'm from [your country].\"");
        }
    }

    private static void AppendFreeConversationHintTask(StringBuilder prompt, LessonChatRequest request, TutorAvatarProfile avatarProfile)
    {
        prompt.AppendLine("Give one short target-language hint that helps the learner continue open target-language conversation safely, following the active level profile hint strategy.");
        prompt.AppendLine("A1: a fuller sentence starter is okay. A2: use a less complete sentence starter. B1: suggest structure or a useful phrase. B2: suggest natural direction or tone.");
        prompt.AppendLine($"The hint must answer or continue from the learner's point of view, not {avatarProfile.DisplayName}'s point of view.");
        prompt.AppendLine("The hint should help the learner respond to the latest bot message or continue the recent conversation in the selected target study language.");
        prompt.AppendLine("If the learner's topic is unsafe, harmful, illegal, hateful, sexually explicit, or asks for professional medical/legal/financial advice, redirect to a safe everyday topic for target-language practice.");
        prompt.AppendLine("Do not include roleplay-only instructions or introductions-specific examples.");
        prompt.AppendLine("If the learner profile includes a display name, hint examples may use that name when appropriate.");
        prompt.AppendLine("Do not invent a learner name.");
        prompt.AppendLine($"Do not use {avatarProfile.DisplayName} or the tutor avatar name as the learner's name.");
    }



    private static void AppendTargetLanguageLock(StringBuilder prompt, LessonChatRequest request)
    {
        var targetLanguage = ResolveTargetLanguage(request);
        prompt.AppendLine("Target-language lesson language lock:");
        prompt.AppendLine($"- Always speak {targetLanguage.LanguageLockName} in tutor messages.");
        prompt.AppendLine("- Do not switch to another language even if the learner asks.");
        prompt.AppendLine($"- If the learner asks to use another language, politely refuse in {targetLanguage.LanguageLockName} and continue the current lesson.");
        prompt.AppendLine("- Keep the reply level-appropriate.");
        prompt.AppendLine($"- For A1, use a scenario-compatible equivalent of: Let's practice in {targetLanguage.LanguageLockName}.");
        prompt.AppendLine("- Translation is handled only by the app's Translate button, not by changing tutor language.");
        prompt.AppendLine($"- Do not produce Finnish, Russian, English, Spanish, or any other non-target-language tutor output unless {targetLanguage.LanguageLockName} is that language.");
        prompt.AppendLine("- Deterministic language-switch refusal examples must be adapted into the active target language:");
        prompt.AppendLine($"  User: \"Speak Finnish.\" Tutor A1: answer in {targetLanguage.LanguageLockName} and return to the lesson.");
        prompt.AppendLine($"  User: \"Can you speak Russian?\" Tutor A1: answer in {targetLanguage.LanguageLockName} and ask one scenario-compatible question.");
        prompt.AppendLine($"  User: \"Puhu suomea.\" Tutor A1: answer in {targetLanguage.LanguageLockName} and guide the learner back.");
        prompt.AppendLine($"  Tutor B1/B2: keep the lesson in {targetLanguage.LanguageLockName} and continue the scenario.");
        prompt.AppendLine();
    }

    private static void AppendCanonicalTeachingPolicy(StringBuilder prompt, LessonChatRequest request, TutorAvatarProfile avatarProfile, string mode)
    {
        prompt.AppendLine("Canonical tutor teaching policy shared by normal Lesson Chat and Realtime Conversation Mode:");
        prompt.AppendLine($"- Current mode: {mode}.");
        prompt.AppendLine($"- Tutor identity comes only from the selected TutorProfile: {avatarProfile.DisplayName} ({avatarProfile.Id}).");
        prompt.AppendLine("- Lesson content describes roles and scenarios; it must not override the tutor profile identity.");
        prompt.AppendLine("- UI label, avatar name, prompt identity, and any tutor self-reference must stay aligned with the selected TutorProfile.");
        prompt.AppendLine("- The tutor may react warmly to jokes, compliments, and small talk, then return to the lesson goal.");
        prompt.AppendLine("- Target study language, level profile, feedback rules, hint rules, off-topic rules, turn limits, and guided scenario retention all apply in this mode.");
        AppendTargetLanguageLock(prompt, request);

        if (IsFreeConversation(request))
        {
            prompt.AppendLine("- Free Conversation allows safe open topic selection and natural open-topic questions.");
        }
        else
        {
            prompt.AppendLine("- Guided roleplay must continue the selected context, selected context variant id, role, situation, and learning goal.");
            prompt.AppendLine("- Guided roleplay must not become generic AI chat or free conversation.");
            prompt.AppendLine("- Guided roleplay must not ask broad assistant-offer questions or open-topic selection questions.");
            prompt.AppendLine("- If the learner goes off-topic: briefly acknowledge, redirect to the selected lesson goal, and do not switch topic.");
            AppendGuidedScenarioFlexibilityPolicy(prompt);
            AppendNaturalRoleplayCorrectionPolicy(prompt);
            AppendScenarioContinuityPolicy(prompt);
        }

        AppendTutorIdentityRules(prompt, avatarProfile);

        if (string.Equals(mode, RealtimeVoiceMode, StringComparison.OrdinalIgnoreCase))
        {
            AppendLevelSpecificRealtimeRules(prompt, request, avatarProfile);
        }
        else if (IsA1(request))
        {
            AppendA1StrictRules(prompt, request);
        }

        prompt.AppendLine();
    }


    private static void AppendLevelSpecificRealtimeRules(StringBuilder prompt, LessonChatRequest request, TutorAvatarProfile avatarProfile)
    {
        var level = ChooseFirstNonEmpty(request.SelectedLevel, request.Level).ToLowerInvariant();
        prompt.AppendLine("Level-specific realtime behavior:");

        var speakingRule = avatarProfile.SpeakingRules.FirstOrDefault(rule => level.StartsWith(rule.Key, StringComparison.OrdinalIgnoreCase)).Value;
        if (!string.IsNullOrWhiteSpace(speakingRule))
        {
            prompt.AppendLine($"- Tutor profile speaking rule: {speakingRule.Trim()}");
        }

        if (level.StartsWith("a1", StringComparison.OrdinalIgnoreCase))
        {
            AppendA1StrictRules(prompt, request);
            prompt.AppendLine("- Use 1-2 short sentences.");
            prompt.AppendLine("- Use simple words.");
            prompt.AppendLine("- Ask one simple question.");
        }
        else if (level.StartsWith("a2", StringComparison.OrdinalIgnoreCase))
        {
            prompt.AppendLine("- Use 1-3 short sentences.");
            prompt.AppendLine("- Ask one clear follow-up question.");
            prompt.AppendLine("- Use simple natural target-language sentences.");
        }
        else if (level.StartsWith("b1", StringComparison.OrdinalIgnoreCase))
        {
            prompt.AppendLine("- Use natural but concise conversation.");
            prompt.AppendLine("- You may ask a follow-up and add one detail.");
        }
        else if (level.StartsWith("b2", StringComparison.OrdinalIgnoreCase))
        {
            prompt.AppendLine("- Use more natural and nuanced conversation.");
            prompt.AppendLine("- Avoid long monologues.");
        }

        prompt.AppendLine();
    }


    private static void AppendNaturalRoleplayCorrectionPolicy(StringBuilder prompt)
    {
        prompt.AppendLine("Natural roleplay correction policy:");
        prompt.AppendLine("- During active_roleplay, behave like a conversation partner first and a tutor second.");
        prompt.AppendLine("- Do not give alternative phrasing, advice, or model sentences on every turn.");
        prompt.AppendLine("- Use phrases like \"You can say...\" or \"You can also say...\" only when the learner made an error, the wording is unnatural for this level/scenario, the learner asks for help, or the current teaching mode explicitly requires a model phrase.");
        prompt.AppendLine("- If the learner answer is acceptable or natural enough, briefly acknowledge it and continue with one natural scenario question.");
        prompt.AppendLine("- Keep any needed correction short: correct one important issue, then continue the same scenario.");
    }

    private static void AppendScenarioContinuityPolicy(StringBuilder prompt)
    {
        prompt.AppendLine("Scenario continuity policy:");
        prompt.AppendLine("- Track the recent conversation context included in this prompt and continue from the learner's latest answer.");
        prompt.AppendLine("- Do not ask again for basic information already answered in recent turns, such as name, country, city, home, work, or business, unless clarification is needed.");
        prompt.AppendLine("- Do not restart greetings, introductions, setup, context choice, or the opening line after roleplay has begun.");
        prompt.AppendLine("- Stay in the selected scenario until the runtime phase says wrap_up or final.");
        prompt.AppendLine("- Ask exactly one scenario-compatible question when continuing active roleplay.");
    }

    private static void AppendGuidedScenarioFlexibilityPolicy(StringBuilder prompt)
    {
        prompt.AppendLine("Guided scenario flexibility:");
        prompt.AppendLine("- Stay inside the selected guided scenario.");
        prompt.AppendLine("- Answer natural learner questions that fit the scenario, including normal introduction and small-talk reciprocal questions.");
        prompt.AppendLine("- Use the active tutor profile for simple personal answers such as name, home city, study/work, hobbies, and how you are.");
        prompt.AppendLine("- After answering, ask one short scenario-compatible question back.");
        prompt.AppendLine("- Do not refuse normal introduction/small-talk questions just because the tutor is in a role.");
        prompt.AppendLine("- Do not say \"No, I'm your neighbor\" when asked whether you study or work.");
        prompt.AppendLine("- For A1, answer with one short sentence plus one simple question.");
    }

    private static void AppendTutorIdentityRules(StringBuilder prompt, TutorAvatarProfile avatarProfile)
    {
        prompt.AppendLine($"- Your current tutor display name is: {avatarProfile.DisplayName}. Use this name when introducing yourself.");
        prompt.AppendLine($"- You are {avatarProfile.DisplayName}.");
        prompt.AppendLine($"- If the learner asks your name, answer with \"I'm {avatarProfile.DisplayName}.\"");
        prompt.AppendLine($"- If you introduce yourself by name, use only this exact tutor name: {avatarProfile.DisplayName}.");
        prompt.AppendLine($"- Do not say you are from any place except {avatarProfile.HomeCity}, {avatarProfile.CountryOrRegion}.");
        prompt.AppendLine("- Do not invent or use any other tutor name, city, country, job, study field, age, hobby, or background.");
        prompt.AppendLine("- Stay consistent with the selected tutor profile.");
    }

    private static void AppendGuidedRoleplayRetentionRules(StringBuilder prompt)
    {
        prompt.AppendLine("Guided roleplay retention rules:");
        prompt.AppendLine("- This is active guided roleplay, not free conversation.");
        prompt.AppendLine("- You are still the selected tutor avatar while playing the role required by the scenario.");
        prompt.AppendLine("- If the learner asks a personal small-talk question, answer using your tutor profile, then ask one short scenario-compatible question back.");
        prompt.AppendLine("- Stay inside the selected roleplay context and continue from the last visible tutor message.");
        prompt.AppendLine("- Do not ask the learner to choose a new topic, context, or situation during active roleplay.");
        prompt.AppendLine("- Do not offer unrelated help or tips.");
        prompt.AppendLine("- Do not say: \"How can I " + "assist you today?\"");
        prompt.AppendLine("- Do not say: \"What would you like to " + "discuss?\"");
        prompt.AppendLine("- Keep the selected lesson goal, target language, and grammar focus fixed.");
    }

    private static void AppendA1StrictRules(StringBuilder prompt, LessonChatRequest request)
    {
        prompt.AppendLine("A1 strict output rules:");
        prompt.AppendLine("- Use very simple target-language sentences. Use very simple English when English is the target study language.");
        prompt.AppendLine("- Use short sentences.");
        prompt.AppendLine("- Ask one question at a time.");
        prompt.AppendLine("- Avoid phrasal verbs when a simpler verb exists.");
        prompt.AppendLine("- Avoid complex tenses unless they are the lesson target.");
        prompt.AppendLine("- Avoid long suggestions.");
        prompt.AppendLine("- Avoid long explanations unless the learner asks.");
        prompt.AppendLine("- No long advice and no explanations inside roleplay unless correcting one important mistake.");
        prompt.AppendLine("- No generic assistant phrases.");
        prompt.AppendLine("- One short response plus one question is usually enough.");

        if (IsIntroductionsSubtopic(request))
        {
            prompt.AppendLine("A1 introductions/new-neighbor rules:");
            prompt.AppendLine("- After 'My name is David.', a good reply is: 'Nice to meet you, David. Where are you from?'");
            prompt.AppendLine("- If the learner asks your name after sharing their country, answer simply using the active tutor profile name, then continue the introduction scenario.");
            prompt.AppendLine("- After 'I am from Russia.', a good reply is: 'Nice. Do you live here now?' or 'Good. Where do you live?'");
            prompt.AppendLine("- Do not ask: 'Where did you move from?'");
            prompt.AppendLine("- Do not ask: 'How long have you been living here?'");
        }
    }

    private static void AppendLessonContext(StringBuilder prompt, LessonChatRequest request, TutorAvatarProfile avatarProfile, bool includeNativeLanguage = true)
    {
        prompt.AppendLine(LessonContextHeader);
        prompt.AppendLine($"- Level: {ChooseFirstNonEmpty(request.Level, request.SelectedLevel)}");
        prompt.AppendLine($"- Topic: {ChooseFirstNonEmpty(request.TopicTitle, request.Topic)}");
        prompt.AppendLine($"- Situation/Subtopic: {ChooseFirstNonEmpty(request.SubtopicTitle, request.Subtopic)}");

        if (!string.IsNullOrWhiteSpace(request.LessonPhase))
        {
            prompt.AppendLine($"- Lesson phase: {LessonLimitHelper.ToContractValue(LessonLimitHelper.GetRuntimePhase(request))}");
        }

        if (!string.IsNullOrWhiteSpace(request.LessonType))
        {
            prompt.AppendLine($"- Lesson type: {request.LessonType}");
        }

        if (!string.IsNullOrWhiteSpace(request.LessonScenarioId))
        {
            prompt.AppendLine($"- Lesson scenario id: {request.LessonScenarioId}");
        }

        if (!string.IsNullOrWhiteSpace(request.LessonGoal))
        {
            prompt.AppendLine($"- Lesson goal: {request.LessonGoal}");
        }

        if (!string.IsNullOrWhiteSpace(request.SelectedContextTitle))
        {
            prompt.AppendLine($"- Selected roleplay canonical context: {request.SelectedContextTitle}");
        }

        if (!string.IsNullOrWhiteSpace(request.SelectedContextLocalizedTitle))
        {
            prompt.AppendLine($"- Selected roleplay localized display context: {request.SelectedContextLocalizedTitle}");
        }

        if (!string.IsNullOrWhiteSpace(request.SelectedContextTitle) || !string.IsNullOrWhiteSpace(request.SelectedContextLocalizedTitle))
        {
            prompt.AppendLine("- The canonical context may be English lesson metadata; the localized context is the learner-visible target-language selection when available.");
        }

        if (!string.IsNullOrWhiteSpace(request.SelectedContextVariantId))
        {
            prompt.AppendLine($"- Selected context variant id: {request.SelectedContextVariantId}");
        }

        if (!string.IsNullOrWhiteSpace(request.SelectedContextConfirmationLine))
        {
            prompt.AppendLine($"- Context confirmation line already shown by tutor: {ResolveScenarioPlaceholders(request.SelectedContextConfirmationLine, avatarProfile)}");
        }

        if (!string.IsNullOrWhiteSpace(request.SelectedContextOpeningLine))
        {
            prompt.AppendLine($"- Context opening line already shown by tutor: {ResolveScenarioPlaceholders(request.SelectedContextOpeningLine, avatarProfile)}");
        }

        if (!string.IsNullOrWhiteSpace(request.SelectedContextOpeningIntent))
        {
            prompt.AppendLine($"- Selected context opening intent: {ResolveScenarioPlaceholders(request.SelectedContextOpeningIntent, avatarProfile)}");
        }

        if (request.TargetLanguageKeyPhrases.Count > 0)
        {
            prompt.AppendLine($"- Target key phrases: {string.Join(", ", request.TargetLanguageKeyPhrases)}");
        }

        if (request.GrammarFocus.Count > 0)
        {
            prompt.AppendLine($"- Grammar focus: {string.Join(", ", request.GrammarFocus)}");
        }

        AppendScenarioFlow(prompt, request);

        AppendActiveLevelProfile(prompt, request);

        if (!string.IsNullOrWhiteSpace(request.FeedbackRulesSummary))
        {
            prompt.AppendLine($"- Feedback rules: {request.FeedbackRulesSummary}");
        }

        if (request.AiTutorPromptInstructions.Count > 0)
        {
            prompt.AppendLine("- Lesson-specific tutor instructions:");
            foreach (var instruction in request.AiTutorPromptInstructions.Where(instruction => !string.IsNullOrWhiteSpace(instruction)))
            {
                prompt.AppendLine($"  - {instruction.Trim()}");
            }
        }

        if (includeNativeLanguage)
        {
            prompt.AppendLine($"- Native language: {request.NativeLanguageName}");
        }

        prompt.AppendLine();
    }


    private static void AppendScenarioFlow(StringBuilder prompt, LessonChatRequest request)
    {
        var hasScenarioFlow = !string.IsNullOrWhiteSpace(request.ConversationOpening)
            || !string.IsNullOrWhiteSpace(request.ConversationFirstUserTask)
            || request.ConversationGuidedPracticeFollowUpQuestions.Count > 0
            || !string.IsNullOrWhiteSpace(request.ConversationVariationOrComplication)
            || !string.IsNullOrWhiteSpace(request.ConversationCorrectionMoment)
            || !string.IsNullOrWhiteSpace(request.ConversationWrapUpMessage)
            || !string.IsNullOrWhiteSpace(request.ConversationFinalMessage)
            || !string.IsNullOrWhiteSpace(request.ConversationWrapUpIntent)
            || !string.IsNullOrWhiteSpace(request.ConversationFinalMessageIntent)
            || request.RoleplayBeats.Count > 0
            || !string.IsNullOrWhiteSpace(request.ReciprocalQuestionIfUserAsksTutorName)
            || !string.IsNullOrWhiteSpace(request.ReciprocalQuestionIfUserAsksSimplePersonalQuestion)
            || request.ReciprocalQuestionMustNotRefuseScenarioCompatibleQuestions
            || request.ExpectedScenarioProgression.Count > 0;

        if (!hasScenarioFlow)
        {
            return;
        }

        prompt.AppendLine("Scenario conversation flow from lesson JSON:");
        AppendOptionalLine(prompt, "- Opening policy", request.ConversationOpening);
        AppendOptionalLine(prompt, "- First user task", request.ConversationFirstUserTask);
        if (request.ConversationGuidedPracticeFollowUpQuestions.Count > 0)
        {
            prompt.AppendLine($"- Guided practice follow-up questions: {string.Join(" | ", request.ConversationGuidedPracticeFollowUpQuestions.Where(question => !string.IsNullOrWhiteSpace(question)).Select(question => question.Trim()))}");
        }
        AppendOptionalLine(prompt, "- Variation or complication", request.ConversationVariationOrComplication);
        AppendOptionalLine(prompt, "- Correction moment", request.ConversationCorrectionMoment);
        if (LessonLimitHelper.GetRuntimePhase(request) != LessonRuntimePhase.ActiveRoleplay)
        {
            AppendOptionalLine(prompt, "- Wrap-up message from lesson JSON", request.ConversationWrapUpMessage);
            AppendOptionalLine(prompt, "- Final message from lesson JSON", request.ConversationFinalMessage);
            AppendOptionalLine(prompt, "- Wrap-up intent", request.ConversationWrapUpIntent);
            AppendOptionalLine(prompt, "- Final message intent", request.ConversationFinalMessageIntent);
        }
        if (request.ExpectedScenarioProgression.Count > 0)
        {
            prompt.AppendLine("- Expected scenario progression:");
            foreach (var step in request.ExpectedScenarioProgression.Where(step => !string.IsNullOrWhiteSpace(step)))
            {
                prompt.AppendLine($"  - {step.Trim()}");
            }
        }

        if (request.RoleplayBeats.Count > 0)
        {
            prompt.AppendLine("- Roleplay beats:");
            foreach (var beat in request.RoleplayBeats.Where(beat => !string.IsNullOrWhiteSpace(beat.Intent)))
            {
                var beatId = string.IsNullOrWhiteSpace(beat.Id) ? "beat" : beat.Id.Trim();
                prompt.AppendLine($"  - {beatId}: {beat.Intent.Trim()}");
            }
        }

        if (!string.IsNullOrWhiteSpace(request.ReciprocalQuestionIfUserAsksTutorName)
            || !string.IsNullOrWhiteSpace(request.ReciprocalQuestionIfUserAsksSimplePersonalQuestion)
            || request.ReciprocalQuestionMustNotIgnoreUserQuestion
            || request.ReciprocalQuestionMustNotRefuseScenarioCompatibleQuestions)
        {
            prompt.AppendLine("- Reciprocal question handling:");
            AppendOptionalLine(prompt, "  - If learner asks tutor name", request.ReciprocalQuestionIfUserAsksTutorName);
            AppendOptionalLine(prompt, "  - If learner asks a simple personal question", request.ReciprocalQuestionIfUserAsksSimplePersonalQuestion);
            prompt.AppendLine($"  - Must not ignore learner's reciprocal question: {request.ReciprocalQuestionMustNotIgnoreUserQuestion}");
            prompt.AppendLine($"  - Must not refuse scenario-compatible questions: {request.ReciprocalQuestionMustNotRefuseScenarioCompatibleQuestions}");
        }
    }

    private static void AppendOptionalLine(StringBuilder prompt, string label, string value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            prompt.AppendLine($"{label}: {value.Trim()}");
        }
    }

    private static void AppendActiveLevelProfile(StringBuilder prompt, LessonChatRequest request)
    {
        var hasProfile = !string.IsNullOrWhiteSpace(request.ActiveLevelProfileDifficultyNotes)
            || !string.IsNullOrWhiteSpace(request.ActiveLevelProfileTutorLanguageStyle)
            || !string.IsNullOrWhiteSpace(request.ActiveLevelProfileExpectedUserResponse)
            || !string.IsNullOrWhiteSpace(request.ActiveLevelProfileFeedbackStrictness)
            || !string.IsNullOrWhiteSpace(request.ActiveLevelProfileHintStrategy)
            || !string.IsNullOrWhiteSpace(request.ActiveLevelProfileCorrectionPriority)
            || !string.IsNullOrWhiteSpace(request.ActiveLevelProfileConversationDepth)
            || !string.IsNullOrWhiteSpace(request.ActiveLevelProfileExampleGoodAnswer)
            || !string.IsNullOrWhiteSpace(request.ActiveLevelProfileExampleStretchAnswer)
            || request.ActiveLevelProfileAddedKeyPhrases.Count > 0
            || request.ActiveLevelProfileAddedUsefulConstructions.Count > 0
            || request.ActiveLevelProfileAddedGrammarFocus.Count > 0;

        if (!hasProfile)
        {
            return;
        }

        prompt.AppendLine(ActiveLevelProfileHeader);
        prompt.AppendLine($"- Selected level: {ChooseFirstNonEmpty(request.SelectedLevel, request.Level)}");

        if (!string.IsNullOrWhiteSpace(request.LevelBotLanguageComplexityGuidance))
        {
            prompt.AppendLine($"- CMS language complexity guidance: {request.LevelBotLanguageComplexityGuidance}");
        }

        if (!string.IsNullOrWhiteSpace(request.LevelCorrectionGuidance))
        {
            prompt.AppendLine($"- CMS correction guidance: {request.LevelCorrectionGuidance}");
        }

        if (!string.IsNullOrWhiteSpace(request.LevelAnswerLengthGuidance))
        {
            prompt.AppendLine($"- CMS answer length guidance: {request.LevelAnswerLengthGuidance}");
        }

        if (!string.IsNullOrWhiteSpace(request.ActiveLevelProfileDifficultyNotes))
        {
            prompt.AppendLine($"- Difficulty notes: {request.ActiveLevelProfileDifficultyNotes}");
        }

        if (!string.IsNullOrWhiteSpace(request.ActiveLevelProfileTutorLanguageStyle))
        {
            prompt.AppendLine($"- Tutor language style: {request.ActiveLevelProfileTutorLanguageStyle}");
        }

        if (!string.IsNullOrWhiteSpace(request.ActiveLevelProfileExpectedUserResponse))
        {
            prompt.AppendLine($"- Expected user response: {request.ActiveLevelProfileExpectedUserResponse}");
        }

        if (!string.IsNullOrWhiteSpace(request.ActiveLevelProfileFeedbackStrictness))
        {
            prompt.AppendLine($"- Feedback strictness: {request.ActiveLevelProfileFeedbackStrictness}");
        }

        if (!string.IsNullOrWhiteSpace(request.ActiveLevelProfileHintStrategy))
        {
            prompt.AppendLine($"- Hint strategy: {request.ActiveLevelProfileHintStrategy}");
        }

        if (!string.IsNullOrWhiteSpace(request.ActiveLevelProfileCorrectionPriority))
        {
            prompt.AppendLine($"- Correction priority: {request.ActiveLevelProfileCorrectionPriority}");
        }

        if (!string.IsNullOrWhiteSpace(request.ActiveLevelProfileConversationDepth))
        {
            prompt.AppendLine($"- Conversation depth: {request.ActiveLevelProfileConversationDepth}");
        }

        if (!string.IsNullOrWhiteSpace(request.ActiveLevelProfileExampleGoodAnswer))
        {
            prompt.AppendLine($"- Example good answer: {request.ActiveLevelProfileExampleGoodAnswer}");
        }

        if (!string.IsNullOrWhiteSpace(request.ActiveLevelProfileExampleStretchAnswer))
        {
            prompt.AppendLine($"- Example stretch answer: {request.ActiveLevelProfileExampleStretchAnswer}");
        }

        if (request.ActiveLevelProfileAddedKeyPhrases.Count > 0)
        {
            prompt.AppendLine($"- Level-added key phrases: {string.Join(", ", request.ActiveLevelProfileAddedKeyPhrases)}");
        }

        if (request.ActiveLevelProfileAddedUsefulConstructions.Count > 0)
        {
            prompt.AppendLine($"- Level-added useful constructions: {string.Join(", ", request.ActiveLevelProfileAddedUsefulConstructions)}");
        }

        if (request.ActiveLevelProfileAddedGrammarFocus.Count > 0)
        {
            prompt.AppendLine($"- Level-added grammar focus: {string.Join(", ", request.ActiveLevelProfileAddedGrammarFocus)}");
        }

        prompt.AppendLine();
    }

    private static void AppendLessonLength(StringBuilder prompt, LessonChatRequest request)
    {
        prompt.AppendLine(LessonLengthHeader);
        prompt.AppendLine($"- Learner turn count including latest message: {Math.Max(request.UserTurnNumber, request.LearnerTurnCount)}");
        prompt.AppendLine($"- Soft learner turn limit: {LessonLimitHelper.GetSoftLearnerTurnLimit(request)}");
        prompt.AppendLine($"- Hard learner turn limit: {LessonLimitHelper.GetHardLearnerTurnLimit(request)}");
        prompt.AppendLine("- Setup/context selection is already complete and did not count as a lesson turn.");
        prompt.AppendLine($"- Remaining learner turns after latest message: {LessonLimitHelper.GetRemainingLearnerTurns(request)}");
        prompt.AppendLine($"- Runtime phase: {LessonLimitHelper.ToContractValue(LessonLimitHelper.GetRuntimePhase(request))}");
        prompt.AppendLine($"- shouldStartWrappingUp: {LessonLimitHelper.ShouldStartWrappingUp(request)}");
        prompt.AppendLine($"- isFirstWrapUpInstruction: {LessonLimitHelper.IsFirstWrapUpInstruction(request)}");
        prompt.AppendLine($"- shouldEndLessonNow: {LessonLimitHelper.ShouldEndLessonNow(request)}");
        prompt.AppendLine($"- effectiveRuntimeSource: {request.EffectiveRuntimeSource}");
        prompt.AppendLine($"- contentPackSlug: {request.ContentPackSlug}");
        prompt.AppendLine($"- versionNumber: {request.RuntimeContentVersionNumber?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty}");
        prompt.AppendLine($"- snapshotHash: {request.RuntimeContentSnapshotHash}");
        prompt.AppendLine($"- fallbackUsed: {request.RuntimeContentFallbackUsed}");
        prompt.AppendLine($"- scenarioKey: {request.RuntimeContentScenarioKey}");
        prompt.AppendLine($"- resolvedLevelId: {request.ResolvedLevelId}");
        prompt.AppendLine();
    }

    private static void AppendAvatarProfile(StringBuilder prompt, TutorAvatarProfile avatarProfile)
    {
        prompt.AppendLine(TutorAvatarProfileHeader);
        prompt.AppendLine($"You are {avatarProfile.DisplayName}.");
        prompt.AppendLine("Profile:");
        prompt.AppendLine($"- Id: {avatarProfile.Id}");
        prompt.AppendLine($"- Display name: {avatarProfile.DisplayName}");
        prompt.AppendLine($"- Age: {avatarProfile.Age}.");
        prompt.AppendLine($"- Lives in {avatarProfile.HomeCity}, {avatarProfile.CountryOrRegion}.");
        prompt.AppendLine($"- Studies {avatarProfile.Studies}.");
        prompt.AppendLine($"- Enjoys {FormatNaturalList(avatarProfile.Hobbies)}.");
        prompt.AppendLine("Style:");
        prompt.AppendLine($"- {FormatNaturalList(avatarProfile.CommunicationStyle)}.");
        prompt.AppendLine("- Speak clearly and briefly.");
        prompt.AppendLine("Identity rules:");
        AppendTutorIdentityRules(prompt, avatarProfile);
        foreach (var rule in avatarProfile.IdentityRules.Where(rule => !string.IsNullOrWhiteSpace(rule)))
        {
            prompt.AppendLine($"- {rule.Trim()}");
        }
        prompt.AppendLine();
    }

    private static void AppendLearnerProfile(StringBuilder prompt, LessonChatRequest request)
    {
        var userDisplayName = NormalizeOptionalText(request.UserDisplayName);
        var learningGoal = NormalizeOptionalText(request.LearningGoal);

        if (string.IsNullOrWhiteSpace(userDisplayName) && string.IsNullOrWhiteSpace(learningGoal))
        {
            return;
        }

        prompt.AppendLine(LearnerProfileHeader);

        if (!string.IsNullOrWhiteSpace(userDisplayName))
        {
            prompt.AppendLine($"- Display name: {userDisplayName}");
        }

        if (!string.IsNullOrWhiteSpace(learningGoal))
        {
            prompt.AppendLine($"- Learning goal: {learningGoal}");
        }

        prompt.AppendLine();
    }

    private static void AppendRecentConversation(StringBuilder prompt, IReadOnlyList<RecentConversationMessage> recentMessages)
    {
        prompt.AppendLine(RecentConversationHeader);

        var relevantMessages = recentMessages
            .Where(message => !string.IsNullOrWhiteSpace(message.Text))
            .TakeLast(OpenAiConstants.RecentConversationMessagesLimit)
            .ToArray();

        if (relevantMessages.Length == 0)
        {
            prompt.AppendLine(NoRecentConversationContext);
            prompt.AppendLine();
            return;
        }

        foreach (var message in relevantMessages)
        {
            prompt.AppendLine($"- {NormalizeSender(message.Sender)}: {message.Text.Trim()}");
        }

        prompt.AppendLine();
    }



    private static TutorAvatarProfile ResolveRequestTutorProfile(LessonChatRequest request, TutorAvatarProfile fallbackProfile)
    {
        if (string.IsNullOrWhiteSpace(request.TutorDisplayName))
        {
            return fallbackProfile;
        }

        return new TutorAvatarProfile
        {
            Id = fallbackProfile.Id,
            DisplayName = request.TutorDisplayName.Trim(),
            Age = fallbackProfile.Age,
            HomeCity = fallbackProfile.HomeCity,
            CountryOrRegion = fallbackProfile.CountryOrRegion,
            Studies = fallbackProfile.Studies,
            Hobbies = fallbackProfile.Hobbies,
            CommunicationStyle = fallbackProfile.CommunicationStyle,
            SpeakingRules = fallbackProfile.SpeakingRules,
            IdentityRules = fallbackProfile.IdentityRules
        };
    }

    private static TutorAvatarProfile CreateRealtimeTutorProfile(RealtimeVoiceSessionStartRequest request, TutorAvatarProfile fallbackProfile)
    {
        if (string.IsNullOrWhiteSpace(request.TutorDisplayName)
            || request.TutorProfileAge <= 0
            || string.IsNullOrWhiteSpace(request.TutorProfileHomeCity)
            || string.IsNullOrWhiteSpace(request.TutorProfileStudies))
        {
            return fallbackProfile;
        }

        return new TutorAvatarProfile
        {
            Id = string.IsNullOrWhiteSpace(request.TutorProfileId) ? fallbackProfile.Id : request.TutorProfileId.Trim(),
            DisplayName = request.TutorDisplayName.Trim(),
            Age = request.TutorProfileAge,
            HomeCity = request.TutorProfileHomeCity.Trim(),
            CountryOrRegion = string.IsNullOrWhiteSpace(request.TutorProfileCountryOrRegion) ? fallbackProfile.CountryOrRegion : request.TutorProfileCountryOrRegion.Trim(),
            Studies = request.TutorProfileStudies.Trim(),
            Hobbies = request.TutorProfileHobbies.Count > 0 ? request.TutorProfileHobbies.ToList() : fallbackProfile.Hobbies,
            CommunicationStyle = request.TutorProfileCommunicationStyle.Count > 0 ? request.TutorProfileCommunicationStyle.ToList() : fallbackProfile.CommunicationStyle,
            SpeakingRules = request.TutorProfileSpeakingRules.Count > 0 ? new Dictionary<string, string>(request.TutorProfileSpeakingRules, StringComparer.OrdinalIgnoreCase) : fallbackProfile.SpeakingRules,
            IdentityRules = request.TutorProfileIdentityRules.Count > 0 ? request.TutorProfileIdentityRules.ToList() : fallbackProfile.IdentityRules
        };
    }

    private static string FormatNaturalList(IEnumerable<string> values)
    {
        var items = values.Where(value => !string.IsNullOrWhiteSpace(value)).Select(value => value.Trim()).ToArray();
        return items.Length switch
        {
            0 => string.Empty,
            1 => items[0],
            2 => $"{items[0]} and {items[1]}",
            _ => $"{string.Join(", ", items[..^1])}, and {items[^1]}"
        };
    }

    private static LessonChatRequest CreateLessonChatRequest(RealtimeVoiceSessionStartRequest request)
    {
        var fallbackLevelTurnLimits = CmsLevelProfiles.Resolve(request.SelectedLevel);
        var resolvedSoftWrapTurn = request.SoftLearnerTurnLimit > 0
            ? request.SoftLearnerTurnLimit
            : fallbackLevelTurnLimits.WrapUpAfterUserTurn;
        var resolvedFinalTurn = request.HardLearnerTurnLimit > 0
            ? request.HardLearnerTurnLimit
            : fallbackLevelTurnLimits.FinalMessageAtUserTurn;

        return new LessonChatRequest
        {
            SelectedLevel = request.SelectedLevel,
            TopicTitle = request.TopicTitle,
            SubtopicTitle = request.SubtopicTitle,
            UserMessage = string.Empty,
            LastBotMessage = request.LastBotMessage,
            NativeLanguageName = request.NativeLanguageName,
            TargetLanguageId = request.TargetLanguageId,
            TargetLanguageName = request.TargetLanguageName,
            TargetLanguageNativeName = request.TargetLanguageNativeName,
            TargetLanguageCode = request.TargetLanguageCode,
            TutorAvatarId = request.TutorProfileId,
            UserDisplayName = request.UserDisplayName,
            LearningGoal = request.LearningGoal,
            LearnerTurnCount = request.LearnerTurnCount,
            SoftLearnerTurnLimit = resolvedSoftWrapTurn,
            HardLearnerTurnLimit = resolvedFinalTurn,
            RecentMessages = request.RecentMessages.Select(message => new RecentConversationMessage { Sender = message.Sender, Text = message.Text }).ToArray(),
            LessonPhase = ChooseFirstNonEmpty(request.CurrentPhase, request.LessonPhase),
            HasWrapUpStarted = request.HasWrapUpStarted,
            LessonScenarioId = request.LessonScenarioId,
            Level = request.SelectedLevel,
            Topic = request.Topic,
            Subtopic = request.Subtopic,
            LessonGoal = request.LessonGoal,
            LessonType = request.LessonType,
            AiTutorPromptInstructions = request.AiTutorPromptInstructions,
            SelectedContextVariantId = request.SelectedContextVariantId,
            SelectedContextTitle = request.SelectedContextTitle,
            SelectedContextLocalizedTitle = request.SelectedContextLocalizedTitle,
            SelectedContextOpeningLine = request.SelectedContextOpeningLine,
            SelectedContextConfirmationLine = request.SelectedContextConfirmationLine,
            SelectedContextOpeningIntent = request.SelectedContextOpeningIntent,
            UserTurnNumber = request.LearnerTurnCount,
            SoftWrapUpAfterUserTurn = resolvedSoftWrapTurn,
            FinalMessageAtUserTurn = resolvedFinalTurn,
            EffectiveRuntimeSource = request.EffectiveRuntimeSource,
            ContentPackSlug = request.ContentPackSlug,
            RuntimeContentVersionNumber = request.RuntimeContentVersionNumber,
            RuntimeContentSnapshotHash = request.RuntimeContentSnapshotHash,
            RuntimeContentFallbackUsed = request.RuntimeContentFallbackUsed,
            RuntimeContentScenarioKey = request.RuntimeContentScenarioKey,
            ResolvedLevelId = request.ResolvedLevelId,
            LevelBotLanguageComplexityGuidance = request.LevelBotLanguageComplexityGuidance,
            LevelCorrectionGuidance = request.LevelCorrectionGuidance,
            LevelAnswerLengthGuidance = request.LevelAnswerLengthGuidance,
            TargetLanguageKeyPhrases = request.TargetLanguageKeyPhrases,
            GrammarFocus = request.GrammarFocus,
            ConversationOpening = request.ConversationOpening,
            ConversationFirstUserTask = request.ConversationFirstUserTask,
            ConversationGuidedPracticeFollowUpQuestions = request.ConversationGuidedPracticeFollowUpQuestions,
            ConversationVariationOrComplication = request.ConversationVariationOrComplication,
            ConversationCorrectionMoment = request.ConversationCorrectionMoment,
            ConversationWrapUpMessage = request.ConversationWrapUpMessage,
            ConversationFinalMessage = request.ConversationFinalMessage,
            ConversationWrapUpIntent = request.ConversationWrapUpIntent,
            ConversationFinalMessageIntent = request.ConversationFinalMessageIntent,
            RoleplayBeats = request.RoleplayBeats.Select(beat => new ScenarioRoleplayBeat { Id = beat.Id, Intent = beat.Intent }).ToArray(),
            ReciprocalQuestionIfUserAsksTutorName = request.ReciprocalQuestionIfUserAsksTutorName,
            ReciprocalQuestionIfUserAsksSimplePersonalQuestion = request.ReciprocalQuestionIfUserAsksSimplePersonalQuestion,
            ReciprocalQuestionMustNotIgnoreUserQuestion = request.ReciprocalQuestionMustNotIgnoreUserQuestion,
            ReciprocalQuestionMustNotRefuseScenarioCompatibleQuestions = request.ReciprocalQuestionMustNotRefuseScenarioCompatibleQuestions,
            ExpectedScenarioProgression = request.ExpectedScenarioProgression,
            FeedbackRulesSummary = request.FeedbackRulesSummary,
            TutorProfileId = request.TutorProfileId,
            ActiveLevelProfileDifficultyNotes = request.ActiveLevelProfile.DifficultyNotes,
            ActiveLevelProfileTutorLanguageStyle = request.ActiveLevelProfile.TutorLanguageStyle,
            ActiveLevelProfileExpectedUserResponse = request.ActiveLevelProfile.ExpectedUserResponse,
            ActiveLevelProfileFeedbackStrictness = request.ActiveLevelProfile.FeedbackStrictness,
            ActiveLevelProfileHintStrategy = request.ActiveLevelProfile.HintStrategy,
            ActiveLevelProfileCorrectionPriority = request.ActiveLevelProfile.CorrectionPriority,
            ActiveLevelProfileConversationDepth = request.ActiveLevelProfile.ConversationDepth,
            ActiveLevelProfileExampleGoodAnswer = request.ActiveLevelProfile.ExampleGoodAnswer,
            ActiveLevelProfileExampleStretchAnswer = request.ActiveLevelProfile.ExampleStretchAnswer,
            ActiveLevelProfileAddedKeyPhrases = request.ActiveLevelProfile.AddedKeyPhrases,
            ActiveLevelProfileAddedUsefulConstructions = request.ActiveLevelProfile.AddedUsefulConstructions,
            ActiveLevelProfileAddedGrammarFocus = request.ActiveLevelProfile.AddedGrammarFocus
        };
    }

    private static bool IsA1(LessonChatRequest request)
    {
        var level = ChooseFirstNonEmpty(request.SelectedLevel, request.Level);
        return level.StartsWith("A1", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsFreeConversation(LessonChatRequest request)
    {
        return string.Equals(request.LessonType, "free_conversation", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsIntroductionsSubtopic(LessonChatRequest request)
    {
        return string.Equals(ChooseFirstNonEmpty(request.Subtopic, request.SubtopicTitle), "Introductions", StringComparison.OrdinalIgnoreCase);
    }

    private static string ResolveScenarioPlaceholders(string value, TutorAvatarProfile avatarProfile)
    {
        return string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : value.Replace("{tutorName}", avatarProfile.DisplayName, StringComparison.OrdinalIgnoreCase).Trim();
    }

    private static StudyLanguageDefinition ResolveTargetLanguage(LessonChatRequest request)
    {
        var byId = StudyLanguageCatalog.GetById(request.TargetLanguageId);
        if (!string.Equals(byId.Id, StudyLanguageCatalog.DefaultStudyLanguageId, StringComparison.OrdinalIgnoreCase)
            || string.IsNullOrWhiteSpace(request.TargetLanguageId))
        {
            return byId;
        }

        if (!string.IsNullOrWhiteSpace(request.TargetLanguageCode))
        {
            var byCode = StudyLanguageCatalog.All.FirstOrDefault(language =>
                string.Equals(language.Bcp47Code, request.TargetLanguageCode.Trim(), StringComparison.OrdinalIgnoreCase)
                || string.Equals(language.TranscriptionLanguageCode, request.TargetLanguageCode.Trim(), StringComparison.OrdinalIgnoreCase));
            if (byCode is not null)
            {
                return byCode;
            }
        }

        if (!string.IsNullOrWhiteSpace(request.TargetLanguageName))
        {
            var byName = StudyLanguageCatalog.All.FirstOrDefault(language =>
                string.Equals(language.EnglishName, request.TargetLanguageName.Trim(), StringComparison.OrdinalIgnoreCase)
                || string.Equals(language.NativeName, request.TargetLanguageName.Trim(), StringComparison.OrdinalIgnoreCase));
            if (byName is not null)
            {
                return byName;
            }
        }

        return StudyLanguageCatalog.English;
    }

    private static string ChooseFirstNonEmpty(string? primary, string fallback)
    {
        return string.IsNullOrWhiteSpace(primary)
            ? NormalizeOptionalText(fallback)
            : primary.Trim();
    }

    private static string NormalizeOptionalText(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : value.Trim();
    }

    private static string NormalizeSender(string sender)
    {
        if (string.IsNullOrWhiteSpace(sender))
        {
            return "Unknown";
        }

        return sender.Trim();
    }
}
