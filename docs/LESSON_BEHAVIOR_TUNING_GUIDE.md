# CMS-first lesson behavior tuning guide

Normal behavior tuning should be done in CMS, not by editing backend code. In short: normal behavior tuning should be done in CMS, not by editing backend code. Do not tune normal tutor behavior by editing LessonPromptBuilder.cs first; the backend should assemble the active runtime content source and enforce runtime guardrails.

## Ownership map

### Code-owned guardrails

Backend code owns only non-editable runtime protections:

- prompt assembly order;
- target study-language lock;
- tutor identity/source-coherence enforcement;
- structured lesson-chat response format;
- runtime phase contract (`active_roleplay`, `wrap_up`, `final`);
- final-turn completion enforcement and no active dialogue after final;
- setup/context selection not counting as lesson turns;
- diagnostics for runtime content source, content pack, version, snapshot hash, and fallback state;
- static JSON fallback/init mechanics.

### CMS-owned editable behavior

CMS owns normal lesson behavior wording and tuning:

- base tutor prompt wording;
- lesson response rules;
- roleplay behavior;
- correction frequency and correction style;
- scenario continuity wording;
- level-specific strictness;
- tutor personality/style;
- scenario-specific behavior;
- wrap/final wording, while runtime phase still owns timing.

### Static fallback/init content

`Content/Prompts/*`, `Content/Lessons/*`, and `Content/Tutors/*` are baseline seed content for CMS initialization and emergency static fallback. They are not the preferred long-term behavior tuning surface after CMS published runtime content is active.

## Where to change behavior

### Correction frequency

Use the Admin CMS **Prompts** tab and edit `lesson_response_rules` for global correction frequency, such as when to give model phrasing or when to continue without correction. Use the **Levels** tab for level-specific correction strictness through each level profile's correction guidance and related level fields.

### Natural roleplay behavior

Use the Admin CMS **Prompts** tab and edit `lesson_tutor_base` or `lesson_response_rules` for global roleplay tone. Use the **Scenarios** tab for scenario-specific roleplay instructions, roleplay beats, expected scenario progression, and AI tutor prompt instructions.

### Scenario continuity

Use the Admin CMS **Prompts** tab and edit `lesson_response_rules` for global continuity rules. Use the **Scenarios** tab for selected-context wording, conversation flow, reciprocal question handling, expected scenario progression, wrap-up intent, and final-message intent.

### Level strictness

Use the Admin CMS **Levels** tab. Level profiles control wrap/final turn timing, language complexity guidance, correction guidance, and answer length guidance. Prompt templates must not define numeric wrap-up or final-message turn thresholds.

### Tutor personality

Use the Admin CMS **Tutors** tab for tutor behavior profiles. Tutor profiles own editable communication style, speaking rules, and profile-specific behavior while the backend keeps identity coherence with the selected tutor avatar.

### Wrap/final wording

Use the Admin CMS **Scenarios** tab for scenario wrap-up and final message wording and intent. Runtime phase still decides when wrap-up and final are used; CMS edits should not add separate numeric turn thresholds outside level profiles.

## Runtime source coherence

The prompt builder composes prompts from the same active runtime content source as the scenario and level data:

- when the effective source is `CmsPublishedSnapshot`, scenario, level, tutor, and prompt sections are served from that published snapshot;
- when the effective source is `StaticJsonFallback`, scenario, level, tutor, and prompt sections are served from packaged static fallback content;
- CMS prompt sections must not be mixed with static scenario or level data, and static prompt sections must not be mixed with CMS scenario or level data.

## Backend change checklist

Change backend code only when the desired behavior is a non-negotiable runtime guardrail, source-coherence rule, assembly rule, structured response requirement, fallback/init mechanic, or diagnostic. If a change is about tutor tone, correction style, roleplay wording, continuity wording, level strictness, scenario behavior, or personality, make it in CMS content instead.
