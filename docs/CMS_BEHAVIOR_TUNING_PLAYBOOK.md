# CMS behavior tuning playbook

This playbook is for a non-developer CMS admin who receives lesson feedback from a tester and needs to make one practical behavior change in Admin CMS. It does not require command-line use.

## Current runtime state after Admin CMS Overview clarification

Backend `0.1.35-backend.48` serves the clarified Admin CMS Overview. Windows direct tester `0.1.36-tester.30` is the published tester build for this handoff state.

The Overview now separates:

- **CMS content pack / seed identity**: for example `static-json-v1` / `Static JSON Baseline`. This identifies the CMS pack and original seed lineage. It is not proof that learners are using static JSON fallback.
- **Draft workspace status**: whether the admin is editing draft CMS content. Draft edits are not visible to learners until published.
- **Actual learner runtime source**: the decisive learner source field. Healthy runtime is `CmsPublishedSnapshot`.
- **Emergency static JSON fallback enabled**: whether fallback is allowed as a safety mechanism.
- **Currently using static JSON fallback**: the decisive active fallback field. Healthy runtime is `No`.

Healthy behavior-tuning state is:

- **Actual learner runtime source** = `CmsPublishedSnapshot`;
- **Validation success** = `Yes`;
- **Currently using static JSON fallback** = `No`;
- a published version exists.

Static JSON is initialization/emergency fallback only. Do not change static JSON for normal production behavior tuning.

## Before changing anything

- [ ] Admin CMS **Overview** shows **Actual learner runtime source** = `CmsPublishedSnapshot`.
- [ ] Admin CMS **Overview** shows **Currently using static JSON fallback** = `No`.
- [ ] Admin CMS shows a published version exists.
- [ ] You are editing the draft workspace, not assuming Save draft is published.
- [ ] You can write a short change summary before publishing.
- [ ] You know the tester's scenario, level, tutor, and symptom.

If these checks are not true, pause. A CMS draft edit may not affect tester lessons until runtime is using the CMS published snapshot again.

## Operating model

1. **Tester reports a symptom.** Ask for scenario, level, tutor, and one example message if the report is vague.
2. **Admin identifies the behavior category.** Decide whether the issue is global prompt behavior, level behavior, tutor style, scenario-local wording/flow, or publish/rollback.
3. **Admin edits exactly one CMS area.** Use one tab and one field/type of field unless you intentionally plan a controlled multi-step change.
4. **Admin validates or previews if available.** Use **Validation & Preview** to check the draft before publish.
5. **Admin publishes.** Use **Versions & Publish**, write a clear change summary, and publish the current draft.
6. **Tester starts a new lesson.** Existing active lessons may keep old content. Test with a newly started lesson using the same scenario, level, and tutor.
7. **Admin rolls back if worse.** Restore the previous published version from **Versions & Publish**, or edit a new draft that removes the text and publish again.

## How to choose the correct CMS area

| CMS area | Use it for | Do not use it for |
| --- | --- | --- |
| **Prompts** | Global behavior across lessons: acknowledgements, correction frequency, model phrasing, roleplay continuity, one-question-at-a-time style. | Scenario-only wording, tutor identity, or numeric wrap/final turn thresholds. |
| **Levels** | A1/A2/B1/B2 strictness, language complexity, correction style, answer length, and timing thresholds owned by level profiles. | Scenario-local flow wording or tutor personality. |
| **Tutors** | Personality, tone, warmth, directness, speaking style, and tutor-specific style. | Changing avatar identity or overriding scenario facts. |
| **Scenarios** | Situation-specific flow, beats, awkward wording, local constraints, wrap-up intent, final-message intent, and local roleplay guidance. | Global policy that should apply to all lessons, or tutor identity changes. |
| **Validation & Preview** | Draft validation and preview before publish. | Publishing or restoring versions. |
| **Versions & Publish** | Publish current draft, write change summary, inspect versions, restore a previous published version. | Editing prompt/scenario/tutor/level content directly. |

## Rules that prevent drift

- Do not add numeric wrap/final turn numbers to prompt templates.
- Do not change static JSON for normal production behavior tuning.
- Do not ask a developer to change `LessonPromptBuilder.cs` for normal wording/style changes.
- Do not change tutor identity inside scenario text.
- Do not edit multiple behavior layers at once unless intentionally doing a controlled multi-step change.
- Prefer structured CMS fields over Advanced JSON. Use Advanced JSON only for rare technical edits.

## Field/action matrix

| Symptom | Likely CMS area | Tab to open | Field/type of field to edit | Exact example text to add or replace | Expected effect | What not to change |
| --- | --- | --- | --- | --- | --- | --- |
| Tutor repeats the same acknowledgement too often, such as “Nice”. | Global response style. | **Prompts** | `lesson_response_rules` prompt template. Add near acknowledgement/response rules. | `Vary short acknowledgements. Do not reuse the same acknowledgement in consecutive turns. Use simple alternatives such as “Great”, “Good answer”, “That works”, “I see”, “Thanks”, or continue directly when no acknowledgement is needed.` | The tutor should stop saying the same acknowledgement repeatedly across lessons. | Do not edit a single scenario unless the issue appears only in that scenario. Do not change backend code. |
| Tutor corrects acceptable answers too often. | Global correction policy; level strictness if only one level is affected. | **Prompts** first; **Levels** only if level-specific. | `lesson_response_rules` correction section, or selected level profile correction guidance. | `If the learner's answer is understandable and acceptable for the level, do not correct it. Briefly acknowledge the meaning and continue the conversation. Correct only errors that block understanding, repeat often, or are the focus of the lesson.` | Acceptable answers should receive less correction and more natural continuation. | Do not make the tutor ignore serious errors. Do not weaken all levels if only A1 is too strict. |
| Tutor gives “You can say...” on almost every turn. | Global model-phrase behavior. | **Prompts** | `lesson_response_rules` prompt template. Add near alternative/model phrasing rules. | `Do not give “You can say...” or a model sentence on every turn. Offer a model phrase only when the learner asks for help, makes an important mistake, sounds stuck, or the phrase teaches the current lesson goal. Otherwise continue the roleplay naturally.` | Model phrases become occasional support instead of a repeated pattern. | Do not delete all correction guidance. Do not edit scenario flow unless only one scenario causes it. |
| Tutor repeats already answered questions. | Continuity and memory of recent answers. | **Prompts** for global issue; **Scenarios** for one scenario. | `lesson_response_rules` continuity rule, or scenario conversation-flow/AI tutor instructions field. | `Do not ask again for information the learner already gave in the current lesson unless clarification is needed. Use the learner's previous answer and ask the next relevant question instead.` | Tutor should move forward after answered questions. | Do not add hidden state requirements or backend changes. Do not rewrite unrelated scenario text. |
| Tutor restarts the scenario after roleplay has begun. | Roleplay continuity. | **Prompts** for global issue; **Scenarios** for one scenario. | `lesson_response_rules` roleplay continuity rule, or scenario opening/follow-up guidance. | `After roleplay has begun, do not restart the greeting, setup, context choice, or first bot message. Continue from the latest learner answer and keep the scenario moving forward.` | Tutor should stop restarting introductions or setup mid-lesson. | Do not change setup text unless the setup itself is wrong. Do not add numeric turn rules. |
| Tutor leaves the selected scenario. | Scenario adherence. | **Scenarios** | Selected scenario structured field for AI tutor instructions, conversation flow, or scenario-specific guidance. | `Stay inside this scenario. If the learner goes off track, briefly acknowledge them and redirect back to the current situation with one simple question connected to this scenario.` | Tutor should keep the selected situation and redirect gently. | Do not change tutor identity. Do not add global restrictions if only this scenario drifts. |
| Tutor asks more than one question at a time. | Global response shape. | **Prompts** | `lesson_response_rules` prompt template. | `Ask only one question per tutor turn. If several questions are possible, choose the most useful next question and save the others for later turns.` | Tutor turns should become easier to answer, especially for lower levels. | Do not change lesson timing or backend phase rules. |
| A1 language is too complex. | Level complexity. | **Levels** | A1 level profile language complexity / response-length / examples guidance. | `For A1, use very short sentences and common words. Prefer one clear idea per sentence. Avoid idioms, complex grammar, and long explanations. Ask simple questions the learner can answer with a short phrase or one sentence.` | A1 tutor language should become simpler without affecting higher levels. | Do not edit global prompts if A2/B1/B2 are acceptable. Do not add timing numbers to prompt templates. |
| A1 corrections are too strict. | A1 level correction strictness. | **Levels** | A1 level profile correction guidance. | `For A1, be gentle and selective. Accept understandable short answers even when grammar is imperfect. Correct only one important error at a time, and give a very short model phrase when it helps.` | A1 should feel more supportive and less punitive. | Do not lower strictness for all levels unless all levels are too strict. |
| Tutor personality/style feels wrong. | Tutor profile. | **Tutors** | Selected tutor behavior profile style/personality/speaking-rules field. | `Sound warm, patient, and encouraging. Keep responses concise. Avoid sounding overly formal, sarcastic, or like an examiner. Maintain the same tutor identity and do not introduce a different persona.` | The selected tutor should feel closer to the desired tone across lessons. | Do not change scenario text to rename or redefine the tutor. Do not change avatar IDs. |
| Scenario-specific wording is awkward. | Local scenario wording. | **Scenarios** | Selected scenario structured text field: setup, first bot message guidance, context option title, goal, can-do statement, hint example, or conversation-flow guidance. | `Use natural, simple wording for this situation. Replace awkward phrasing with: “Tell me what you need, and I’ll help you choose the best option.”` | Only that scenario's wording should improve. | Do not edit global prompts for a local wording problem. Do not use Advanced JSON unless structured fields cannot represent the change. |
| Wrap-up is too long. | Scenario wrap-up wording, or level timing only if wrap begins too early/late. | **Scenarios** | Selected scenario wrap-up/summary guidance field. | `Keep the wrap-up brief. Summarize the learner's main success in one short sentence, give at most one quick improvement tip, and move to the final closing without asking a new roleplay question.` | Wrap-up should be shorter while preserving a useful closing. | Do not add “wrap at turn X” to prompts or scenario text. |
| Final message continues active dialogue. | Final-message intent. | **Scenarios** for wording; backend already owns final no-dialogue guardrail. | Selected scenario final-message guidance field. | `The final message must close the lesson. Do not ask a new question, do not introduce a new task, and do not continue roleplay. Give a brief friendly closing and stop.` | Final messages should sound final rather than continuing the conversation. | Do not ask developers to change normal wording in `LessonPromptBuilder.cs`. Do not add numeric final turn rules. |
| Free conversation behaves like guided roleplay, or guided roleplay behaves like free conversation. | Scenario mode and prompt behavior alignment. | **Scenarios** first; **Prompts** only if global mode confusion happens everywhere. | Selected scenario AI tutor instructions / conversation-flow guidance, or global `lesson_response_rules` roleplay section. | For free conversation: `Treat this as open conversation practice. Follow the learner's topic while keeping responses level-appropriate, and ask one natural follow-up question.` For guided roleplay: `Treat this as guided roleplay. Stay in the selected situation, move through the scenario beats, and redirect gently if the learner leaves the roleplay.` | Lesson mode should match the selected scenario type. | Do not change unrelated tutor style. Do not edit multiple scenarios unless the issue is global. |

## How to answer tester feedback

Use this response template when converting feedback into an admin action:

- **Symptom:** `<tester's words and one example>`
- **Behavior category:** `<global prompt / level / tutor / scenario / publish issue>`
- **CMS location:** `<tab and selected content pack/scenario/level/tutor/prompt>`
- **Exact field:** `<field or prompt template name>`
- **Exact text to add/replace:** `<paste-ready English text>`
- **Publish/test steps:** `Save draft → Validation & Preview → Versions & Publish → write summary → Publish current draft → tester starts a new lesson with the same scenario, level, and tutor.`
- **Expected result:** `<specific behavior change>`
- **Rollback instruction:** `If worse, open Versions & Publish and restore the previous published version, or remove the added text from the draft and publish again with a rollback summary.`

## Example response to “the tutor repeats ‘Nice’ too often”

- **Symptom:** Tutor says “Nice” several turns in a row.
- **Behavior category:** Global acknowledgement variation.
- **CMS location:** **Prompts** tab, selected content pack `static-json-v1` if that is the active CMS pack.
- **Exact field:** `lesson_response_rules` prompt template, acknowledgement/normal response rules.
- **Exact text to add:** `Vary short acknowledgements. Do not reuse the same acknowledgement in consecutive turns. Use simple alternatives such as “Great”, “Good answer”, “That works”, “I see”, “Thanks”, or continue directly when no acknowledgement is needed.`
- **Publish/test steps:** Save draft, validate/preview, publish with summary `Reduce repeated acknowledgements`, then ask the tester to start a new lesson and check several tutor replies.
- **Expected result:** The tutor varies acknowledgement wording or continues directly.
- **Rollback instruction:** Restore the previous published version from **Versions & Publish**, or remove this sentence from the draft and publish again.

## Rollback and restore workflow

1. Open **Versions & Publish**.
2. Find the last known good published version from before the behavior change.
3. Use restore to copy that old version into a new published version.
4. Write a rollback summary such as `Rollback acknowledgement variation change after tester feedback`.
5. Ask the tester to start a new lesson and verify the old behavior is restored.

Published versions are immutable history. Restore creates a new published version; it does not edit old history.
