# Next Steps

## Current stable baseline

The current baseline is acceptable for stabilization: Windows audit/build commands were reported passing by the developer; normal Lesson Chat uses `tts-1`; Realtime uses `gpt-realtime`; Realtime response creation is transcript-gated; valid Realtime transcripts appear as feedback-eligible learner messages; summaries should use the whole valid lesson conversation; Awaiting Finish disables new lesson input while preserving message review actions.

## Do not touch yet

- Do not rewrite `LessonChatViewModel`.
- Do not redesign Realtime.
- Do not migrate all lesson JSON yet.
- Do not add payments/subscriptions.
- Do not add new avatars.
- Do not start UI polish before lesson behavior is stable.

## Recommended next tasks

1. Full manual smoke test across 5 topics.
2. Scenario QA pass for all 26 lesson JSON files.
3. Methodology polish for A1/A2/B1/B2 prompts.
4. Feedback quality pass.
5. Summary quality pass.
6. Realtime latency measurement.
7. Small architecture extraction only after smoke tests.

## Candidate Codex tasks

- Review all lesson scenarios for methodology consistency — check scenario, context variation, level rules, and tutor profile separation across all lesson JSON files.
- Add regression tests for final-state message review — protect Awaiting Finish review actions from future command-state regressions.
- Improve lesson summary quality from full conversation — review summary inputs and output expectations without changing runtime behavior first.
- Measure Realtime latency and produce a tuning report — capture first-audio and playback metrics before optimizing.
- Extract BotVoicePlaybackCoordinator from LessonChatViewModel — move manual/auto voice playback after exact-text behavior is pinned.
- Extract RealtimeConversationCoordinator from LessonChatViewModel — move realtime lifecycle and transcript handling after smoke tests pass.
- Create a scenario QA report for all Content/Lessons JSON files — identify methodology polish needs without mass migration.
- Add broader prompt policy regression tests — cover reciprocal questions, tutor identity, level complexity, feedback, and final-turn behavior.

## Recommended immediate next task

Run a full manual smoke test across all five MVP topics and record pass/fail in `docs/MANUAL_TEST_CHECKLIST.md`.
