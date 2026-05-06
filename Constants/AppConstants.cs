namespace EnglishVoiceTutor.Desktop.Constants;

public static class AppConstants
{
    public const string AppName = "English Voice Tutor Desktop";
    public const string ShortAppName = "English Voice Tutor";
    public const string WelcomeSubtitle = "Practice spoken English with short AI-powered lessons.";
    public const string MvpFooterNote = "MVP build — AI, voice, and avatar will be connected step by step.";

    public const string LevelSelectionTitle = "Choose your English level";
    public const string LevelSelectionSubtitle = "We will use this level later to adapt lessons and corrections.";
    public const string SelectedLevelPrefix = "Selected level:";

    public const string HomeTitle = "Choose a conversation topic";
    public const string HomeSubtitle = "Start with a practical situation and practice step by step.";
    public const string DailyLimitText = "Free MVP limit: 3 lessons today";
    public const string SettingsPlaceholderMessage = "Settings screen will be added in a future step.";
    public const string LessonHistoryTitle = "Lesson history";
    public const string LessonHistorySubtitle = "Recent completed lessons on this device.";
    public const string EmptyLessonHistoryText = "No completed lessons yet. Finish a lesson to see it here.";
    public const string LessonHistoryBackButtonText = "Back to topics";
    public const string LessonHistoryGoodLabel = "What went well";
    public const string LessonHistoryImproveLabel = "What to improve";

    public const string SettingsTitle = "Settings";
    public const string SettingsSubtitle = "Configure your learning preferences.";
    public const string NativeLanguageTitle = "Native language";
    public const string NativeLanguageSubtitle = "Translations will use this language later.";
    public const string SettingsSavedMessage = "Settings saved for this session.";
    public const string BackButtonText = "Back";

    public const string NativeLanguageRussian = "Russian";
    public const string NativeLanguageSpanish = "Spanish";
    public const string NativeLanguageGerman = "German";
    public const string NativeLanguageFrench = "French";
    public const string NativeLanguageItalian = "Italian";
    public const string NativeLanguagePortuguese = "Portuguese";
    public static readonly IReadOnlyList<string> SupportedNativeLanguages =
    [
        NativeLanguageRussian,
        NativeLanguageSpanish,
        NativeLanguageGerman,
        NativeLanguageFrench,
        NativeLanguageItalian,
        NativeLanguagePortuguese
    ];

    public const string SubtopicsTitlePrefix = "Choose a situation for";
    public const string SubtopicsSubtitle = "Pick a realistic scenario for your short speaking lesson.";
    public const string StartLessonPlaceholderMessage = "Lesson chat will be added in the next step.";

    public const string LessonChatTitle = "Lesson chat";
    public const string LessonSummaryTitle = "Lesson summary";
    public const string BotSenderName = "Bot";
    public const string UserSenderName = "You";
    public const string StartRecordingButtonText = "Start recording";
    public const string StopRecordingButtonText = "Stop recording";
    public const string RecordingStartedMessage = "Recording... Click Stop recording when you finish.";
    public const string RecordingStartErrorMessage = "Could not start voice recording. Please check your microphone.";
    public const string RecordingStopErrorMessage = "Could not stop voice recording. Please try again.";
    public const string TranscribingAudioMessage = "Transcribing your voice...";
    public const string TranscriptionCompletedMessage = "Voice transcription is ready. Review the text and press Send.";
    public const string TranscriptionFailedMessage = "Could not transcribe the recording. Please try again or type your answer.";
    public const string EmptyTranscriptionMessage = "No speech was recognized. Please try again.";
    public const string EmptyMessageWarning = "Please type your answer before sending.";
    public const string MockHintText = "Hint: Try answering with a short complete sentence.";
    public const string HintFallbackUserMessage = "I need a hint for what to say next.";
    public const string MockBotFirstMessage = "Hi! Let's practice this situation. Are you ready?";
    public const string MockBotReplyText = "Good! I understood your answer. In the next step, AI will give real corrections.";
    public const string TranslateButtonText = "Translate";
    public const string HideTranslationButtonText = "Hide translation";
    public const string TranslationLabel = "Translation";
    public const string DefaultNativeLanguageName = "Russian";
    public const string MockBotFirstMessageTranslation = "Привет! Давайте потренируем эту ситуацию. Вы готовы?";
    public const string MockBotReplyTextTranslation = "Хорошо! Я понял ваш ответ. На следующем шаге ИИ будет давать настоящие исправления.";
    public const string MockUserMessageTranslationText = "Mock translation to the user's native language will be connected later.";
    public const string FinishLessonButtonText = "Finish lesson";
    public const string ViewFeedbackButtonText = "View feedback";
    public const string FeedbackPanelTitle = "Feedback";
    public const string FeedbackCorrectedVersionTitle = "Corrected version";
    public const string FeedbackGrammarTipTitle = "Grammar tip";
    public const string FeedbackVocabularyTipTitle = "Vocabulary tip";
    public const string FeedbackCultureTipTitle = "Culture tip";
    public const string FeedbackNaturalVersionTitle = "More natural version";
    public const string FeedbackTranslateButtonText = "Translate feedback";
    public const string FeedbackHideTranslationButtonText = "Hide feedback translation";
    public const string FeedbackTranslationLabel = "Feedback translation";
    public const string MockFeedbackType = "correction";
    public const string MockFeedbackShortText = "Good start. Here is a more natural version.";
    public const string MockCorrectedVersion = "Yes, I am ready.";
    public const string MockGrammarTip = "Use a full sentence when you want to sound clearer and more confident.";
    public const string MockVocabularyTip = "Short answers are understandable, but complete phrases sound more natural in practice.";
    public const string MockCultureTip = "In many everyday conversations, a friendly full answer helps keep the dialogue going.";
    public const string MockNaturalVersion = "Yes, I am ready. Let's start.";
    public const string MockFeedbackShortTextTranslation = "Хорошее начало. Вот более естественный вариант.";
    public const string MockCorrectedVersionTranslation = "Да, я готов.";
    public const string MockGrammarTipTranslation = "Используйте полное предложение, если хотите звучать понятнее и увереннее.";
    public const string MockVocabularyTipTranslation = "Короткие ответы понятны, но полные фразы звучат естественнее во время практики.";
    public const string MockCultureTipTranslation = "Во многих повседневных разговорах дружелюбный полный ответ помогает поддержать диалог.";
    public const string MockNaturalVersionTranslation = "Да, я готов. Давайте начнём.";
    public const string SummaryFallbackGoodText = "You completed a short practice dialogue and received AI feedback on your response.";
    public const string SummaryFallbackImproveText = "Keep practicing full sentences and apply the feedback tips to improve grammar and vocabulary.";
    public const string UsefulPhrasesTitle = "Useful phrases to remember";
    public static readonly IReadOnlyList<string> SummaryFallbackUsefulPhrases =
    [
        "Could you help me, please?",
        "I would like to...",
        "Could you repeat that, please?",
        "That sounds good to me."
    ];
    public const string ChooseAnotherSituationText = "Choose another situation";
    public const string BackToTopicsText = "Back to topics";
}
