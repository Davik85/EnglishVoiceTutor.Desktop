# Lesson timing source of truth

Runtime lesson timing must come from one coherent content bundle for the lesson.

## Coherent runtime bundles

When the effective runtime source is `CmsPublishedSnapshot`, the scenario content, level profile thresholds, prompt/runtime metadata, content pack slug, version number, snapshot hash, and source diagnostics must all describe the same published CMS snapshot. The desktop must not combine a CMS scenario with packaged static JSON level timing.

When the effective runtime source is `StaticJsonFallback`, the scenario content, level profile thresholds, prompt/runtime metadata, content pack identity, and diagnostics must all describe the packaged static fallback content. Static JSON is kept for CMS initialization, local development, and emergency fallback only; it is not the normal learner runtime source when a valid CMS published snapshot is active.

The runtime must never silently combine CMS and static sources, request thresholds, or hardcoded prompt timing from different bundles.

## Threshold ownership

The runtime thresholds are:

- `softWrapUpAfterUserTurn` / `SoftWrapUpAfterUserTurn`
- `finalMessageAtUserTurn` / `FinalMessageAtUserTurn`

The desktop resolves these values from the active `LevelProfile` on the loaded runtime `LessonScenario` and sends the resolved values to backend lesson chat and realtime session startup. The backend `LessonLimitHelper` honors resolved request thresholds before falling back to backend defaults, so an already-resolved CMS lesson is not recomputed from static defaults. Realtime prompt construction uses the same resolved threshold fields.

Legacy scenario `metadata.softWrapUpAfterUserTurn` and `metadata.finalMessageAtUserTurn` remain required by the static JSON audit for import/deserialization compatibility. They are deprecated for runtime timing and must not drive runtime lesson limits. Runtime timing uses the active level profile from the loaded coherent bundle.

## Bounded full-lesson prompt history

The existing level-profile-first policy also supplies the effective final learner turn for prompt-history capacity. Desktop and backend use the same bound: `min((effectiveFinalLearnerTurn * 2) + 3, 70)`. A missing or non-positive effective final turn falls back to 10 messages; the backend remains the absolute 70-message safety boundary.

This capacity preserves setup/context overhead and prior eligible conversation without changing lesson length, wrap-up timing, or final timing. The current learner input remains separate from the prior history and is not counted twice. No database, CMS timing, or level-profile value changed for this implementation.

## Prompt-template policy

Prompt templates may refer to the current runtime phase (`active_roleplay`, `wrap_up`, `final`, `completed`) and to the resolved lesson-length section supplied by backend code. Prompt templates must not define independent numeric timing such as “wrap at turn 10” or “final at turn 15.”

## Safe diagnostics

Use these non-secret diagnostics to debug timing/source behavior:

- `effectiveRuntimeSource`
- `contentPackSlug`
- `versionNumber` / `snapshotHash` when available
- `fallbackUsed`
- `scenarioKey`
- `resolvedLevelId`
- `softWrapUpAfterUserTurn`
- `finalMessageAtUserTurn`
- `lessonPhase`
- `hasWrapUpStarted`

Desktop debug logs include source, effective source, content pack slug, version/hash, fallback state, lesson phase, learner turn count, soft wrap threshold, final threshold, and whether wrap-up has started. Backend prompts include the safe source diagnostics and resolved limits for the active request.

Expected behavior for a CMS A1 profile configured as Wrap=14 and Final=15: newly loaded lessons should use CMS scenario content and CMS profile thresholds from the same published snapshot. Active roleplay remains active before turn 14, wrap-up begins at turn 14, and the final response occurs at turn 15 because the active lesson bundle is CMS, not because packaged static JSON was edited to match CMS.
