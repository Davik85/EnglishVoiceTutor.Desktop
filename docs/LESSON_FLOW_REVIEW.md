# Lesson Flow Review

Review date: 2026-05-13.

## Required guided lesson flow

1. Setup/context selection.
   - Tutor asks learner to choose one of the controlled context variants or a safe simple custom context.
   - User turn counting must not start here.
2. Context confirmation.
   - Desktop confirms the chosen context and displays the opening roleplay line.
3. Active roleplay.
   - `CurrentLessonPhase` becomes `ActiveRoleplay`.
   - Learner turn counting starts only after roleplay begins.
4. Soft wrap-up.
   - When `LearnerTurnCount >= softWrapUpAfterUserTurn`, backend/tutor should start wrapping up naturally.
5. Final message.
   - At `finalMessageAtUserTurn`, app should show final lesson message and stop normal input.
6. Awaiting Finish lesson.
   - Only finishing/navigation appropriate to the completed lesson should remain active.
7. Summary.
   - Finish lesson navigates to summary and stores history.

## Required Free Conversation flow

- No context selection.
- Starts as open conversation / active practice.
- Must keep safety boundaries and redirect unsafe content.
- Final turn limit is 30 learner turns.
- Realtime may start immediately when Conversation Mode is enabled, subject to backend availability.

## Button state expectations

### Setup screen

- Send enabled only when lesson input is enabled and user text is valid.
- Record allowed only if lesson input is enabled and not busy.
- Hint can provide setup/context help.
- Finish lesson should not complete the lesson before a meaningful roleplay has started unless deliberately allowed by current UI rules.
- Conversation Mode may be enabled before context, but guided realtime should defer actual session start until context selection.
- Setup bot message should not auto-play.

### Active roleplay

- Send/record/hint available when not busy and final limit not reached.
- Finish lesson available according to current lesson options rules.
- Conversation Mode can start/stop, using selected context metadata.
- Learner turns count only user roleplay messages.

### Recording

- Stop recording should be available.
- Send/hint/finish/conversation toggles should avoid conflicting actions.
- Avatar should show listening.

### Sending/transcribing

- Duplicate sends should be disabled.
- Recording should be disabled during send/transcribe.
- Avatar/status should show thinking/transcribing.

### Conversation Mode enabled before context

- For guided lessons, the UI can enter Conversation Mode layout before context, but realtime should not start until context is selected.
- Buttons must clearly avoid simultaneous text/realtime actions that corrupt phase state.

### Conversation Mode active after context

- Realtime session should be started.
- Assistant visible transcript and audio must come from the same realtime response.
- Back/finish/toggle-off should stop microphone, playback, and sockets cleanly.

### Final limit reached

- Input should be disabled.
- Normal send/record/hint/conversation should not continue the lesson.
- Final message should be shown once.
- Finish lesson should remain the primary available action.

### Awaiting Finish lesson

- `IsLessonCompleteAwaitingFinish` should be true.
- Conversation Mode should be disabled/stopped.
- Finish lesson should navigate to summary.

### Lesson finished

- Lesson history should be saved.
- Summary screen should show feedback/fallback summary.
- Chat commands should no longer mutate the completed lesson.
