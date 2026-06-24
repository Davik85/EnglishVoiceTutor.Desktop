# Lesson timing source of truth

Runtime lesson timing has one owner: the resolved level profile values on the runtime lesson content selected for the lesson.

The only runtime thresholds are:

- `SoftWrapUpAfterUserTurn`
- `FinalMessageAtUserTurn`

When the backend runtime content source is `CmsPublishedSnapshot`, the desktop must use that scenario content and the level profiles from the same runtime response. It must not combine CMS scenario content with packaged static JSON timing. Packaged static JSON is allowed only as initialization or emergency fallback content, and diagnostics should make fallback visible.

Prompt templates and scenario content may describe the conversation flow, the current runtime phase, wrap-up wording, and final-message wording. They must not contain independent numeric instructions such as “wrap around turn 10” or “final at turn 15.” Legacy scenario metadata fields such as `softWrapUpAfterUserTurn` and `finalMessageAtUserTurn` remain DTO/import compatible, but runtime ignores those fields and uses level profiles instead.

## Debugging effective runtime timing

Use these safe diagnostics when investigating timing:

- Backend CMS runtime status reports source, effective source, content pack slug, published version, snapshot hash, fallback state, and validation state.
- Desktop debug logs include runtime content source, effective source, pack slug, version/hash, fallback state, selected lesson phase, learner turn count, soft wrap threshold, final threshold, and whether wrap-up has already started.
- Backend lesson prompts derive runtime phase from the resolved thresholds supplied by the desktop request for the active lesson. Realtime session construction preserves those same resolved thresholds.

Expected behavior for A1 configured as Wrap=14 and Final=15:

- active roleplay remains active before turn 14;
- wrap-up begins at turn 14;
- final response occurs at turn 15;
- subsequent wrap-up turns do not repeat the first wrap-up transition.
