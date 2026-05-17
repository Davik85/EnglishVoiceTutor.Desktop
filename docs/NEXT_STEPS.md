# Next Steps

Review date: 2026-05-17.

This roadmap starts from the current MVP state: lesson content audit passes with 26 lesson JSON files, desktop Debug/Release builds pass on Windows, backend build passes on Windows, normal Lesson Chat works, and Conversation Mode uses the stable TTS provider by default.

## Immediate next step

Run a short regression smoke-test after this documentation update.

Use `docs/MANUAL_TEST_CHECKLIST.md` and confirm at minimum:

- app start;
- level, topic, and subtopic selection;
- normal Lesson Chat typed input;
- Enter-to-send;
- normal voice recording and transcription;
- Hint;
- feedback on context-selection and active roleplay messages;
- Translate;
- Play voice;
- Conversation Mode entry;
- Conversation Mode recording, transcription, bot reply, and playback;
- Conversation Mode Hint overlay;
- exit/back from Conversation Mode;
- feedback on Conversation Mode transcript after returning to chat;
- lesson summary;
- backend logs show normal Lesson Chat `tts-1`, Conversation Mode `gpt-4o-mini-tts`, `HasInstructions=True`, and no Realtime WebSocket by default.

## MVP infrastructure roadmap

After the smoke-test, continue toward MVP infrastructure:

1. Decide the data storage approach.
2. Add user profile / local settings persistence needed for MVP.
3. Add lesson history persistence that survives app restarts and supports user review.
4. Decide account/auth strategy.
5. Define and enforce usage limits.
6. Create payment/subscription plan.
7. Prepare packaging/installer flow.
8. Add error reporting / log export for support.
9. Create the release checklist.

## Ongoing methodology polishing

Keep methodology and learning-quality improvements ongoing but separate from infrastructure work:

- tutor prompt refinements;
- level rules polish;
- lesson scenario improvements;
- feedback wording improvements;
- summary quality improvements.

## Voice roadmap

Current MVP voice decision:

Conversation Mode uses the stable TTS provider by default:

`microphone recording -> audio transcription -> lesson chat reply -> gpt-4o-mini-tts playback`

Realtime remains in the codebase for future testing, but it is not the default MVP path. The learner must hear exactly the same text that is displayed, so Conversation Mode does not shorten, summarize, rewrite, or chunk spoken text.

Future voice work should be done only after the MVP smoke-test is stable and should compare real logs for:

- normal Lesson Chat `tts-1` playback;
- Conversation Mode `gpt-4o-mini-tts` playback;
- transcription usage;
- lesson chat reply usage;
- any future Realtime provider-switch experiment.

## Architecture caution

Do not start large refactors before smoke-test results are recorded. `LessonChatViewModel` remains a future extraction candidate, but the next phase should prioritize MVP infrastructure and release readiness over architecture churn.

## Study-language follow-ups

- Validate multilingual study-language behavior manually for English, Spanish, French, and German before release.
- Keep the single shared lesson JSON scenario tree; do not create per-language lesson folders or translated JSON copies.
- Consider a future data-storage/product task for richer native-language/translation-target preferences. Current study-language persistence uses the existing settings file only.
- UI/interface localization is separate from study-language support and remains future work.
- Realtime remains future/non-default; do not make it the default Conversation Mode provider without a separate provider-switch plan.
