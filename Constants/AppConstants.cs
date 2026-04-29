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
    public const string HistoryPlaceholderMessage = "Lesson history will be added in a future step.";
    public const string SettingsPlaceholderMessage = "Settings screen will be added in a future step.";

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
    public const string EmptyMessageWarning = "Please type your answer before sending.";
    public const string MockHintText = "Hint: Try answering with a short complete sentence.";
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
    public const string MockSummaryGoodText = "You completed a short practice dialogue and answered in English.";
    public const string MockSummaryImproveText = "Next, we will connect AI feedback to correct grammar, vocabulary, and natural phrasing.";
    public const string MockUsefulPhrasesTitle = "Useful phrases to remember";
    public const string ChooseAnotherSituationText = "Choose another situation";
    public const string BackToTopicsText = "Back to topics";
}
