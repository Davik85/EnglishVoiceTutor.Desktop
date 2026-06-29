# CMS prompt management admin guide

This guide is for CMS admins who manage lesson prompts and tutor behavior. You do not need to know the codebase or use a command line.

## Main rule

Use CMS for normal tutor behavior changes. Do not edit backend code or static JSON files for normal production tuning. The backend assembles the selected published content and protects required guardrails; CMS owns the normal wording and style that admins tune.


For field-level, paste-ready behavior fixes based on tester feedback, use the practical [CMS behavior tuning playbook](CMS_BEHAVIOR_TUNING_PLAYBOOK.md).

## What CMS controls

CMS controls editable lesson and tutor behavior:

- base tutor prompt wording;
- lesson response rules;
- roleplay behavior;
- correction frequency and correction style;
- scenario continuity wording;
- level-specific strictness guidance;
- tutor personality and style;
- scenario-specific behavior;
- wrap-up and final-message wording.

## What CMS does not control

CMS does not control backend guardrails or runtime safety rules:

- prompt assembly order;
- runtime phase contract, such as active roleplay, wrap-up, and final;
- source coherence between scenario, level profile, tutor, and prompt content;
- target study-language lock;
- tutor identity/source guardrails;
- structured response format;
- the rule that no active dialogue continues after the final message;
- non-secret diagnostics;
- static fallback and initialization mechanics.

CMS also does not own secrets, API keys, billing behavior, user permissions, deployments, database migrations, installer builds, or release uploads.

## Before editing: confirm CMS is active

1. Open the Admin CMS **Overview** area first.
2. Check the runtime content status.
3. Normal lesson testing should show the published CMS snapshot as the effective source, validation passing, and fallback not used.
4. If runtime status shows static JSON fallback, CMS edits may not appear in lessons until the published CMS snapshot is active again.


## How to read CMS Overview without guessing

Backend `0.1.35-backend.48` serves the clarified Admin CMS Overview for this state. The **Overview** page separates five different ideas that can otherwise look similar:

1. **CMS content pack / seed identity** shows the selected CMS pack slug and name, such as `static-json-v1` / `Static JSON Baseline`. This is the CMS content pack identity and seed lineage. The name may describe the original seed source; it does **not** mean learners are currently using static JSON.
2. **Draft workspace status** shows whether the selected CMS workspace is editable draft content. `Draft` means admin edits are saved in the CMS draft workspace. Draft changes do not affect learner runtime until a CMS admin publishes them.
3. **Published snapshot status** shows the active published version number, snapshot hash, and validation result. Learners can use this immutable published snapshot when runtime flags are enabled and validation succeeds.
4. **Emergency static JSON fallback enabled** shows whether the backend is allowed to use packaged static JSON if the CMS snapshot is missing or invalid. This is a safety setting, not proof that fallback is active.
5. **Actual learner runtime source** shows the source learners are using right now. Healthy runtime shows `Effective source = CmsPublishedSnapshot`. Fallback runtime shows `Effective source = StaticJsonFallback` or static JSON wording and must be treated as an attention state unless an emergency rollback is intentional.

A healthy Overview/runtime status is exactly:

- `Actual learner runtime source = CmsPublishedSnapshot`;
- `Currently using static JSON fallback = No`;
- `Validation success = Yes`;
- a published version exists;
- the Overview runtime card says **Learner runtime is using CMS published snapshot**.

The fallback labels have separate meanings:

- **Emergency static JSON fallback enabled: Yes** means the backend is allowed to protect learners by falling back to packaged static JSON if the CMS snapshot is missing or invalid. This is configuration, not proof that fallback is active.
- **Currently using static JSON fallback: No** means learners are not currently using static JSON fallback. This is the active runtime result.

If the selected content pack is `static-json-v1`, read it as CMS seed identity unless **Actual learner runtime source** or **Currently using static JSON fallback** says fallback is active.

## Where to edit common behavior

| Desired change | CMS area to use | Notes |
| --- | --- | --- |
| Base tutor prompt | **Prompts** tab, `lesson_tutor_base` | Global baseline tutor behavior. |
| Lesson response rules | **Prompts** tab, `lesson_response_rules` | Global rules for normal replies. |
| Correction behavior | **Prompts** tab for global correction style; **Levels** tab for level strictness | Make frequency and tone clear without contradicting level guidance. |
| Roleplay behavior | **Prompts** tab for global roleplay rules; **Scenarios** tab for scenario-specific roleplay | Keep the tutor acting inside the selected situation. |
| Scenario continuity | **Prompts** tab for global continuity rules; **Scenarios** tab for situation-specific flow | Use this to stop restarts and repeated questions. |
| Level strictness | **Levels** tab | Level profiles control strictness guidance and lesson timing thresholds. |
| Tutor personality/style | **Tutors** tab | Tune style without changing the selected tutor identity. |
| Scenario-specific behavior | **Scenarios** tab | Use structured scenario fields when available; use Advanced JSON only for rare technical edits. |
| Wrap-up/final wording | **Scenarios** tab | Edit wording and intent only, not numeric timing. |

## Why wrap/final timing belongs to level profiles

Wrap-up and final timing are runtime phase decisions. Level profiles own the numbers that decide when wrap-up starts and when the final message happens. Prompt text and scenario wording may describe how the tutor should sound during wrap-up or final, but they must not add separate turn numbers such as "wrap at turn 10" or "finish at turn 15." Keeping timing in level profiles prevents contradictory instructions and keeps desktop, backend chat, realtime startup, diagnostics, and CMS preview aligned.

## Safe editing workflow

1. **Check CMS Overview first.** Confirm you are editing the intended content pack and that runtime status looks healthy.
2. **Confirm runtime source.** Make sure lessons are using the CMS published snapshot, not fallback/static JSON.
3. **Edit a draft.** Draft edits are not visible to learners until published.
4. **Make one small behavior change at a time.** Small changes are easier to test and revert.
5. **Validate or preview where available.** Use CMS validation/preview before publishing.
6. **Publish.** Use **Versions & Publish** and include a clear publish summary.
7. **Start a new lesson to test.** Existing lessons may keep older loaded content; use a newly started lesson for verification.
8. **Rollback or revert if behavior becomes worse.** Restore a previous published version or edit a new draft that reverses the change, then publish again.

## Examples of safe changes

### Make the tutor correct less often

Edit `lesson_response_rules` in **Prompts**. Change correction wording to say the tutor should correct only important errors or errors that block understanding. Keep the tutor conversational when the learner answer is acceptable.

### Make the tutor more conversational

Edit `lesson_tutor_base` or `lesson_response_rules` in **Prompts**. Ask the tutor to acknowledge briefly, answer naturally, and continue with one relevant question.

### Stop repeated questions

Edit continuity wording in **Prompts** or the selected scenario in **Scenarios**. Tell the tutor not to ask again for information the learner already gave unless clarification is needed.

### Keep the tutor inside the scenario

Edit the selected scenario in **Scenarios**. Strengthen scenario-specific guidance so the tutor stays in role and redirects gently if the learner goes off track.

### Make A1 simpler

Edit the A1 level profile in **Levels**. Make language complexity, correction guidance, and answer-length guidance simpler. Do not add A1 timing numbers to prompt templates.

### Make wrap-up shorter

Edit wrap-up/final wording in the selected scenario in **Scenarios**. Ask for a shorter summary or briefer closing. Do not change prompt text to start wrap-up earlier; timing belongs to the level profile.

## Examples of unsafe changes

Do not make these changes in CMS:

- adding secrets, API keys, tokens, passwords, provider keys, Paddle keys, or webhook secrets;
- adding turn numbers to prompt templates, such as "wrap at turn 10";
- telling the tutor to ignore the runtime phase;
- changing tutor identity inside scenario text;
- making prompts very long, repetitive, or contradictory;
- editing fallback/static JSON for normal production tuning.

## Troubleshooting

### Changes do not appear in lessons

Check CMS Overview and runtime status. Draft changes must be published before new lessons can use them. Start a new lesson after publishing.

### Tutor still uses old behavior

Confirm you published the draft and started a new lesson. Check that the selected scenario, level, prompt template, and tutor profile are the ones you edited.

### Lesson appears to use fallback

If runtime status shows fallback/static JSON, CMS changes may not affect learner lessons. Treat fallback as an operator attention state unless it is an intentional emergency rollback.

### Wrap starts at the wrong time

Check the selected level profile. Wrap/final timing belongs to level profiles, not prompt templates or scenario text.

### Tutor repeats questions

Edit scenario continuity or global response rules. Add concise wording that the tutor should remember recently answered information and ask a new relevant question instead.

### Tutor corrects too much

Edit correction behavior in `lesson_response_rules` and level strictness guidance. Reduce correction frequency and ask for brief corrections only when needed.

### Tutor leaves the scenario

Edit scenario-specific behavior in **Scenarios** and, if needed, global roleplay behavior in **Prompts**. Ask the tutor to stay in the selected role and redirect gently.

## Glossary

- **CMS draft:** Saved CMS edits that are not visible to learner runtime until published.
- **CMS published snapshot:** Immutable published CMS content that learner runtime can use when CMS runtime is active and valid.
- **Prompt template:** Editable CMS text used by the backend when assembling tutor instructions.
- **Level profile:** CMS level settings for complexity, strictness, answer length, and wrap/final timing thresholds.
- **Tutor profile:** CMS tutor behavior/style settings for an approved tutor avatar.
- **Scenario:** A lesson situation with setup, roles, context, flow, and scenario-specific guidance.
- **Fallback/static JSON:** Packaged content used for initialization, local development, rollback, or emergency fallback; not the normal production tuning surface.
- **Runtime phase:** The current lesson stage, such as active roleplay, wrap-up, or final.
- **Wrap-up:** The lesson stage where the tutor starts closing the scenario and summarizing briefly.
- **Final message:** The last tutor message for the lesson; active dialogue must not continue after it.

## Admin → System → AI Models

Super Admins can review and edit backend AI model identifiers in **Admin → System → AI Models**. Use this section for model IDs only: lesson tutor chat, feedback/correction, lesson hint, translation, speech-to-text, lesson chat text-to-speech, Conversation Mode text-to-speech, and Realtime voice. Do not enter API keys, bearer tokens, organization IDs, or other secrets. OpenAI keys remain environment/server secrets.

Recommended workflow: Save draft → Validate format → Test provider access → Publish → run a small real lesson. AI Models CMS has two checks: **Validate format** checks only that model IDs are non-empty, reasonably short, and limited to safe model-ID characters; **Test provider access** runs safe minimal provider checks for draft text model roles without publishing. Format validation does not prove provider access, so a syntactically valid but unavailable provider model can still break AI calls until corrected. If a new model breaks lessons, restore the previous known-good model such as `gpt-5.2` and inspect safe backend logs. API keys remain server environment secrets and are never stored in CMS.

#### GPT-5.5 lesson tutor chat compatibility workflow

`gpt-5.5` has failed lesson tutor chat with `invalid_request` / HTTP 400 under the current lesson chat Responses API request shape. Keep `gpt-5.5` unpublished for lesson tutor chat until compatibility diagnostics isolate whether the minimal request, structured output mode, or lesson runtime request shape is rejected, and then verify with a small real lesson.

Recommended AI Models workflow: Save draft → Validate format → Test provider access → Run compatibility diagnostics for new model family if needed → Publish → small real lesson. API keys remain server environment secrets and must not be saved or exposed in CMS output.
