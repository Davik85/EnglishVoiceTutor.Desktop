# Lesson Behavior Tuning Guide

This guide maps the prompt and behavior sources that control lesson tutor behavior after the technical source-of-truth cleanup.

## Source map

| Behavior area | Primary source | Notes |
| --- | --- | --- |
| Roleplay behavior | `backend/EnglishVoiceTutor.Api/Services/LessonPromptBuilder.cs` canonical teaching policy and guided-roleplay task | Centralized runtime instructions for normal chat and realtime. Prefer changing this before editing many lesson JSON files. |
| Correction behavior | `LessonPromptBuilder.cs` natural roleplay correction policy; `Content/Prompts/lesson_response_rules.txt`; `Content/Prompts/lesson_tutor_base_prompt.txt`; scenario `feedbackRules` | Code/template policy prevents correction advice on every acceptable answer. Scenario/CMS feedback rules should only add local nuance. |
| Level-specific strictness | CMS level profiles in `backend/EnglishVoiceTutor.Api/Services/Cms/CmsLevelProfiles.cs` plus scenario `levelProfiles` | Level profiles own lesson timing and broad strictness. Scenario level profiles should not redefine timing. |
| Scenario continuity | `LessonPromptBuilder.cs` scenario continuity policy; recent conversation block; scenario `conversationFlow`, `roleplayBeats`, and `expectedScenarioProgression` | The prompt tells the tutor to track recent turns, avoid repeated basic questions, and never restart setup after roleplay begins. |
| Wrap/final behavior | Runtime phase from `LessonLimitHelper` and level profile timing; `LessonPromptBuilder.cs` wrap/final branches; scenario wrap/final messages | Prompt templates must not define turn numbers. Runtime phase decides active roleplay vs wrap-up vs final. |
| Tutor identity/personality | Tutor avatar profiles and `LessonPromptBuilder.cs` tutor identity rules | Do not change avatar identities in behavior tuning. Tune style/personality through tutor profiles. |
| CMS prompt/template models | CMS prompt templates and scenario JSON fields imported by CMS services | CMS should own content wording and local scenario intent; code/templates should own global safety and behavioral guardrails. |

## Conflicts found

- The previous A1 default correction guidance said to correct one important mistake and give a short model answer, which could be read as requiring a model answer even when the learner answer was acceptable.
- The global response rules did not explicitly say that acceptable answers may be acknowledged without correction.
- Scenario continuity rules existed in pieces, but did not explicitly prohibit repeating already answered basic questions from recent chat history.
- The prompt already warned not to restart setup, but the rule was spread across sections rather than stated as a central continuity policy.

## What to change where

### More or less correction

- Change global correction policy in `LessonPromptBuilder.cs` when the behavior should apply to all guided roleplay turns.
- Change `Content/Prompts/lesson_response_rules.txt` or `lesson_tutor_base_prompt.txt` for short global template wording.
- Change CMS level profile `CorrectionGuidance` for broad A1/A2/B1/B2 strictness.
- Change scenario `feedbackRules` only for a scenario-specific correction need.

### More natural roleplay

- Prefer `LessonPromptBuilder.cs` guided-roleplay task and natural roleplay correction policy.
- Use scenario `conversationFlow`, `roleplayBeats`, and `expectedScenarioProgression` to describe what should happen next in a specific scenario.
- Avoid adding advice-heavy instructions to individual scenarios unless the lesson is explicitly a teaching/model-phrase mode.

### Stricter A1 behavior

- Prefer CMS level profile fields for A1 language complexity, correction guidance, and answer length.
- Use `AppendA1StrictRules` in `LessonPromptBuilder.cs` only for global A1 rules that must apply in normal chat and realtime.
- Keep A1 corrections short and conditional: correct only when needed, then continue with one simple question.

### Tutor personality

- Tune tutor avatar profile fields, communication style, and speaking rules.
- Do not override avatar identity from scenario content.
- Keep self-introductions aligned with the selected tutor profile.

### Wrap/final behavior

- Timing belongs to CMS level profiles and runtime phase logic.
- Scenario wrap/final messages should describe the closing intent, not turn numbers.
- Prompt templates must continue to defer to runtime phase for wrap-up and final-message behavior.

## CMS later vs code/templates now

Change in code/templates now when the rule is a global product behavior guardrail, such as no correction advice on every acceptable turn, one question at a time, no scenario restart, and no repeated answered basic questions.

Change in CMS later when the wording is lesson-specific: scenario setup, roleplay beats, expected progression, local feedback rules, hint rules, and tutor profile style.
