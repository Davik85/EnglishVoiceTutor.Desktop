# Lesson Flow Review

Review date: 2026-05-14.

This document records the current lesson flow contract for guided roleplay and summary behavior.

## Guided lesson flow

1. Setup/context selection.
   - Tutor asks the learner to choose one controlled context variant or a safe simple custom context.
   - This is setup, not roleplay practice.
   - Setup/context selection does not count as a learner turn.
2. Context confirmation.
   - Desktop confirms the chosen context and displays the opening roleplay line.
   - Scenario placeholders such as `{tutorName}` are resolved from the active tutor profile at runtime.
3. Active roleplay.
   - `CurrentLessonPhase` becomes `ActiveRoleplay`.
   - Valid learner turns count only after active roleplay starts.
   - Typed messages, valid chained voice transcripts, and valid Realtime transcripts can count.
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

## Free Conversation flow

- No context selection is required.
- Practice starts as open conversation / active practice.
- Safety boundaries and gentle redirection still apply.
- Final turn limit is 30 learner turns.
- Realtime may start immediately when Conversation Mode is enabled, subject to backend availability.

## Button state expectations

### Setup/context selection

- Send is enabled only when lesson input is enabled and user text is valid for setup handling.
- Record is allowed only if lesson input is enabled and not busy.
- Hint can provide setup/context help.
- Finish lesson should not accidentally create a completed lesson before meaningful practice.
- Conversation Mode may be enabled before context, but guided realtime should defer actual session start until context selection/opening.
- Setup bot message should not auto-play.

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

### Soft wrap-up

- The tutor should keep the current scenario and naturally move toward closure.
- The app should not jump to summary until Finish lesson is clicked after completion.

### Final message

- Final tutor message is shown once at the final learner turn.
- New lesson input should stop immediately after the final message.
- Conversation Mode should stop/disable for the completed lesson.

### Awaiting Finish

- `IsLessonCompleteAwaitingFinish` should be true.
- Send disabled.
- Start recording disabled.
- Hint disabled.
- Back disabled.
- Conversation Mode disabled.
- Finish lesson enabled.
- View feedback enabled on valid user messages.
- Translate enabled on existing messages.
- Play voice enabled on existing bot messages.

### Summary/history

- Finish lesson navigates to summary/history.
- Lesson history should be saved.
- Summary should reflect the whole valid lesson conversation.
- Chat commands should no longer mutate the completed lesson.

## Regression risks

- Counting setup text as a learner turn.
- Counting invalid transcript retry messages.
- Generating a Realtime assistant response before a valid transcript.
- Disabling review actions in Awaiting Finish.
- Letting Realtime continue after the final tutor message.
- Summarizing only the last exchange instead of the whole valid conversation.
