# Lesson Flow Review

Review date: 2026-05-17.

This document records the current lesson flow contract for guided roleplay, Conversation Mode, feedback, and summary behavior.

## Methodology boundaries

Keep these layers separate:

1. Lesson scenario: topic, subtopic, learning goal, roles, target language, scenario flow, roleplay beats, wrap-up/final behavior.
2. Context selection: the learner chooses or supplies safe small details for the scenario.
3. Level rules: A1/A2/B1/B2 complexity, grammar depth, sentence length, conversation depth, and feedback strictness.
4. Tutor profile: tutor identity, personality, tone, and safe background details.

Active roleplay remains guided by the selected level, topic, subtopic, and scenario.

## Guided lesson flow

1. Setup/context selection.
   - The learner selects a predefined context or types a safe simple custom context.
   - This is setup, not active roleplay practice.
   - Context selection does not count as a learner turn.
2. Context confirmation.
   - Desktop confirms the chosen context and displays the opening roleplay line.
   - Scenario placeholders such as `{tutorName}` are resolved from the active tutor profile at runtime.
3. Active roleplay.
   - `CurrentLessonPhase` becomes `ActiveRoleplay`.
   - Valid learner turns count only after active roleplay starts.
   - Typed messages and valid voice transcripts can count.
   - Empty, placeholder, invalid, or non-English transcripts do not count.
4. Soft wrap-up.
   - When `LearnerTurnCount >= softWrapUpAfterUserTurn`, backend/tutor should start wrapping up naturally while staying in the scenario.
5. Final message.
   - At `finalMessageAtUserTurn`, the app shows the final tutor message once.
   - The final message should not ask a new question or invite continuation.
6. Awaiting Finish.
   - New lesson input is disabled.
   - Message review remains enabled for existing messages.
   - Finish lesson remains enabled.
7. Summary.
   - Finish lesson navigates to the summary/history flow.
   - Summary should use the whole valid lesson conversation and exclude invalid transcript retry/technical messages.

## Context-selection feedback

ContextSelection messages can receive phrase-level feedback. This feedback is tied to the clicked setup/context message through `sourceMessageId` and `sourceMessageKind`.

ContextSelection messages should not be treated as active roleplay answers. They should not increment active-roleplay learner turns and should not be evaluated as if the learner had already entered the roleplay scenario.

## Free Conversation flow

- No context selection is required.
- Practice starts as open conversation / active practice.
- Safety boundaries and gentle redirection still apply.
- Final turn limit remains higher than guided roleplay.

## Conversation Mode flow

Current MVP voice decision:

Conversation Mode uses the stable TTS provider by default:

`microphone recording -> audio transcription -> lesson chat reply -> gpt-4o-mini-tts playback`

Realtime remains in the codebase for future testing, but it is not the default MVP path. The learner must hear exactly the same text that is displayed, so Conversation Mode does not shorten, summarize, rewrite, or chunk spoken text.

Conversation Mode uses the same lesson methodology and the same chat reply flow as normal Lesson Chat:

- the same selected level/topic/subtopic/scenario guides the reply;
- valid transcribed learner speech becomes a learner turn when phase policy allows it;
- the lesson chat reply endpoint generates the visible bot reply;
- Conversation Mode TTS speaks that visible bot reply exactly with `gpt-4o-mini-tts`;
- transcript messages remain available for feedback after returning to Lesson Chat.

## Button state expectations

### Setup/context selection

- Send is enabled only when lesson input is enabled and user text is valid for setup handling.
- Record is allowed only if lesson input is enabled and not busy.
- Hint can provide setup/context help.
- Finish lesson should not accidentally create a completed lesson before meaningful practice.
- Setup bot message should not auto-play unless explicitly allowed by current behavior.

### Context confirmation/opening

- The selected context is confirmed.
- The opening roleplay line appears.
- Active roleplay starts only after this point.
- Learner turn count remains unchanged until the learner sends a valid active-roleplay turn.

### Active roleplay

- Send, Record, Hint, and Conversation Mode are available when not busy and before the final limit.
- Learner turns count only valid user roleplay messages.
- Invalid/empty/non-English transcripts show a retry path and do not count.
- Feedback should attach to valid learner messages when available.

### Final and Awaiting Finish

- Final tutor message is shown once at the final learner turn.
- New lesson input should stop immediately after the final message.
- Conversation Mode should stop/disable for the completed lesson.
- `IsLessonCompleteAwaitingFinish` should be true.
- Send, recording, Hint, Back, and Conversation Mode are disabled.
- Finish lesson remains enabled.
- View feedback remains enabled on valid user messages.
- Translate remains enabled on existing messages.
- Play voice remains enabled on existing bot messages.

### Summary/history

- Finish lesson navigates to summary/history.
- Lesson history should be saved.
- Summary should reflect the whole valid lesson conversation.
- Chat commands should no longer mutate the completed lesson.

## Regression risks

- Counting setup/context-selection text as an active learner turn.
- Treating phrase-level ContextSelection feedback as active roleplay feedback.
- Counting invalid transcript retry messages.
- Disabling review actions in Awaiting Finish.
- Letting Conversation Mode continue after the final tutor message.
- Summarizing only the last exchange instead of the whole valid conversation.
- Speaking text in Conversation Mode that differs from the visible bot text.

## Study-language lesson flow note

The lesson flow uses one shared set of lesson JSON scenarios for all study languages. The selected Settings study language is passed as runtime context so tutor replies, roleplay, hints, feedback, generated summary content, transcription, and Conversation Mode speech adapt to English, French, German, Portuguese, Spanish, or Italian without duplicating scenario trees. English remains the default.
