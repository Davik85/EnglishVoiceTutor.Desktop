# CMS/Admin Content MVP Plan

Review date: 2026-06-06.



## Step 5D-6d update — refresh resilience and unsaved-change protection

Step 5D-6d completed Admin CMS refresh resilience and unsaved-change protection for the bootstrap-admin `/admin/` shell. Admin refresh no longer logs out the admin because refresh authentication uses the existing admin-only HTTP-only cookie, while the admin JWT remains memory-only in JavaScript. Browser Web Storage is not used: no `sessionStorage`, no `localStorage`, and no IndexedDB.

The URL hash stores only safe workspace identifiers: `adminTab`, `cmsSubTab`, `selectedUserId`, `contentPackSlug`, `topicKey`, `scenarioKey`, `promptTemplateKey`, and `tutorId`. Selected user details are restored after refresh through an admin-only lookup by `selectedUserId`; selected CMS entities are restored by stable keys. Unsaved content, prompt bodies, full scenario JSON, tutor profile JSON, tokens, and passwords are not stored in the hash or browser storage.

Unsaved CMS changes are tracked in memory by comparing current form values against the last loaded/saved baseline. Dirty topic, scenario, prompt template, and tutor behavior profile forms show a visible indicator and warn before browser refresh, tab close, top-level admin tab switching, CMS sub-tab switching, selecting another CMS entity, publish/restore reload flows, or logout would discard edits. `Save draft` remains explicit and required: successful saves clear the dirty indicator, and failed saves keep it.

Current Admin CMS Content capabilities include content pack overview, topic editing, scenario editing through the recommended structured fields or visually separated Advanced JSON technical fallback, prompt template editing, tutor behavior profile editing, validation/preview summary, and versions/publish/restore flow. Step 5D-6e is complete: scenario structured fields now have compact local **Jump to** navigation, collapsible/visually separated Basic fields, Lesson setup, Context selection / choices, Conversation flow / response guidance, Wrap-up / summary guidance, and Advanced JSON sections, and concise helper text for normal content editors. `Format JSON` only pretty-prints/re-indents JSON in the editor. `Validate JSON` checks syntax and required scenario fields. Neither action saves or publishes; `Save draft` is required to persist changes and remains draft-only.

Runtime learner behavior is unchanged. The CMS read path remains controlled by configuration, disabled by default, and backed by static JSON fallback. Production RBAC, role-based approval, production billing operations, and full external tester handoff remain future work. CMS draft-save audit logging is implemented for successful Save draft operations; the later critical-change approval workflow should wait until production roles exist.

## Step 5D-6a update — Admin CMS internal sub-tabs

Step 5D-6a reorganized the existing static backend Admin `CMS Content` section into internal sub-tabs: Overview, Topics, Scenarios, Prompts, Tutors, Validation & Preview, Versions & Publish, and Audit. This is a UI organization step only: existing CMS Admin API calls, bounded draft save operations, validation, preview, version listing, publish, and restore behavior remain in place.

The Admin CMS UI remains development-only/admin-only. Runtime learner behavior is unchanged by default, the CMS read path remains disabled by default, and static JSON fallback remains available. External tester handoff remains paused, production billing remains deferred, and public release remains not ready. No EF migration, lesson JSON edit, prompt/tutor content edit, desktop UI change, billing/Paddle/subscription/entitlement/payment change, or password reset behavior change is part of this step.

## Step 5D-6c update — Full scenario JSON editing foundation

Step 5D-6c adds the foundation for editing complete lesson scenario definitions in the Admin CMS instead of only the simplified title, description, setup message, and active fields. CMS scenario drafts now persist a `DefinitionJson` text payload that stores the full scenario JSON definition imported from static lesson content. Scenario detail APIs return this payload, the Scenarios tab displays it in a large advanced textarea, and Save draft validates and persists it alongside the existing simple fields.

Validation now checks full scenario JSON cautiously: it must be valid JSON with an object root and include the required baseline blocks (`id`, `metadata`, `lessonSetup.setupMessage`, `learningGoal`, `targetLanguage`, `levelProfiles`, `conversationFlow`, `controlledVariation`, `offTopicHandling`, `feedbackRules`, `hintRules`, and `aiTutorPromptInstructions`). Preview summaries expose whether sampled scenarios have full JSON present and valid without dumping large scenario bodies. Published snapshots include the full JSON payload, and restore repopulates draft `DefinitionJson` from the selected version so full scenario JSON survives import, draft save, publish, and rollback.

Compatibility remains intentionally safe: existing CMS scenario rows without `DefinitionJson` can still be viewed through an internally marked fallback JSON built from the previously stored scenario fields, while the static import smoke path populates real `DefinitionJson` for the baseline. Runtime learner behavior is unchanged by default, the CMS read path remains disabled unless explicitly enabled by configuration, and static JSON fallback remains available. Lesson JSON files, prompt source files, tutor source files, desktop UI, billing, Paddle, subscriptions, entitlements, payments, password reset, and support-user workflows are outside this step.

Implemented draft-save audit logging tracks full scenario JSON saves through changed field names and stable content hashes so large JSON edits are reviewable without storing unsafe verbatim secrets in logs. Future governance can add finer field-level JSON path review if needed. Approval workflows and production content-manager roles remain future work until production roles and operational policies exist.

## Goal

Build a safe CMS/Admin content editing foundation before external tester handoff, so tester feedback about lesson topics, situations, starter messages, prompts, and tutor behavior can be fixed through a controlled backend CMS workflow instead of code or lesson JSON edits.

This is an audit and implementation plan. Step 5D-1 added the backend CMS content schema foundation. Step 5D-2 added a bootstrap-admin static JSON import and seed foundation that imports current packaged lessons, prompt files, and tutor profiles into CMS tables and publishes an immutable baseline snapshot when validation passes. Step 5D-3 added a backend published-snapshot read/status path with hash verification, safe deserialization, required-content validation, and static JSON fallback status. Step 5D-4 added bootstrap-admin CMS content API endpoints for draft reads, bounded draft updates, draft validation, and safe preview summaries; CMS draft-save audit logging is implemented for successful Save draft operations. Step 5D-5 added bootstrap-admin CMS version list/detail, publish, and restore endpoints so valid drafts can be promoted to immutable snapshots and previous snapshots can be restored through audited backend operations. Step 5D-6 added a static backend Admin CMS Content UI editor shell over those existing endpoints for authenticated bootstrap admins. No lesson JSON migration, desktop UI change, prompt rewrite, billing change, or default runtime lesson loading change is part of these steps.

## Product decision

- CMS/Admin content MVP now starts **before** external tester handoff.
- External tester handoff is paused until the content editing foundation is ready enough for controlled content fixes.
- Desktop hardening is stable enough to pause tester delivery and start CMS content foundation work.
- Production billing remains deferred.
- Public release remains not ready.
- This MVP is content-focused only; it is not full production Admin or operational support tooling.

## Current content architecture audit

### Desktop static content

Current lesson and prompt content is loaded from files packaged with the desktop app:

- `Content/Lessons/` contains topic folders and lesson scenario JSON files.
- `Content/Prompts/` contains `lesson_tutor_base_prompt.txt`, `lesson_setup_rules.txt`, and `lesson_response_rules.txt`.
- `Content/Tutors/` contains tutor avatar/personality JSON files such as Elena and Nelli.
- `Content/StudyLanguages/study_languages.json` mirrors the shared study language catalog.

`Services/LessonContentService.cs` reads those files from `AppContext.BaseDirectory/Content`, deserializes lesson JSON into `Models/LessonContent/*`, and exposes loader methods for scenarios, tutor profiles, and prompt text. The current accepted desktop package therefore carries static content with the app.

### Current lesson JSON structure

A `LessonScenario` currently includes:

- `id`;
- `metadata` with topic, subtopic, lesson type, supported levels, wrap-up/final turn thresholds, and setup-turn behavior;
- `lessonSetup` with setup message, context choices, and setup instructions;
- `learningGoal`;
- `situation`;
- `roles`;
- `targetLanguage`;
- `levelProfiles`;
- `conversationFlow` with opening, default opening example, follow-up questions, correction style, wrap-up/final messages, and intents;
- `roleplayBeats`;
- reciprocal question handling;
- expected progression;
- controlled variation;
- off-topic handling;
- `feedbackRules`;
- `hintRules`;
- repetition logic;
- `aiTutorPromptInstructions`.

### Current hardcoded/static areas

The audit found the following content-related areas are currently hardcoded or file/static based:

| Area | Current source | CMS MVP disposition |
| --- | --- | --- |
| Study languages | `Shared/StudyLanguages/StudyLanguageCatalog.cs` and `Content/StudyLanguages/study_languages.json` | Keep static for MVP. Must remain English, French, German, Portuguese, Spanish, Italian. |
| Interface languages | Localization files/catalogs | Keep static. Release-ready list must remain `en`, `es`, `fr`, `de`, `it`, `pt`, `ru`, `pl`, `ar`, `ja`, `ko`, `sr`, `hr`, `bg`. |
| Topic list/cards | `HomeViewModel.CreateTopics` plus localized display text | Make content-backed later through published CMS read path, but do not change desktop runtime in this planning step. |
| Subtopic/situation list | `SubtopicsViewModel.CreateSubtopicsForTopic` plus localized display text | Make content-backed later through published CMS read path, with JSON/static fallback. |
| Lesson scenarios | `Content/Lessons/*/*.json` | Primary CMS MVP content import target. Do not rewrite JSON in this step. |
| Starter/setup messages | `lessonSetup.setupMessage`, `conversationFlow.opening`, `conversationFlow.defaultOpeningExample`, localized setup helpers | Editable in CMS MVP with preview and validation. |
| Tutor/avatar behavior | `Content/Tutors/*.json`, `TutorAvatarProfileProvider`, prompt builder identity rules | Editable only as bounded `TutorBehaviorProfile` text/rules for approved tutor IDs; avatar assets and broad identity catalog remain static for now. |
| Base lesson prompt rules | `Content/Prompts/*.txt` and `LessonPromptBuilder` canonical policies | Editable as controlled prompt templates where variables are validated; critical safety/runtime policies remain code-owned until explicitly approved. |
| Hint prompt/input | `OpenAiLessonHintService`, `OpenAiConstants.LessonHintSystemInstructions`, `LessonPromptBuilder.BuildHintInput`, JSON `hintRules` | Editable configuration for hint style/example/levels and optional CMS prompt template. |
| Feedback prompt/config | `LessonPromptBuilder`, lesson JSON `feedbackRules`, backend structured output schema | Editable feedback style/rules; schema and persistence remain code-owned. |
| Summary generation | Desktop `LessonChatViewModel` builds summary input and backend stores summaries; final summary behavior is runtime logic | Keep completion/storage logic static; allow summary prompt/config only if a backend generation path uses it in a later implementation. |
| Translation prompt | `TranslationService` uses `OpenAiConstants.TranslationSystemInstructions` and a code input template | Keep static for MVP unless a narrow prompt-template edit is explicitly approved; translation safety and language behavior should remain backend-owned. |
| Safety/boundary instructions | `LessonPromptBuilder`, `OpenAiConstants`, language/output guards, tutor identity guard | Mostly code-owned in MVP. CMS may add bounded content-level safety notes but cannot remove required backend safety rules. |
| Free conversation behavior | `Content/Lessons/FreeConversation/open_conversation.json`, hardcoded Free Conversation topic/subtopic, prompt builder free-conversation policies | Editable scenario/prompt content only; code-owned safety/boundary rules stay static. |
| Lesson completion/summary turn logic | JSON turn thresholds plus `LessonLimitHelper`/`LessonChatViewModel` phase logic | Turn thresholds can be content fields with validation; completion mechanics remain code-owned. |

### Existing Admin foundation audit

A local development Admin shell already exists under `backend/EnglishVoiceTutor.Api/wwwroot/admin/`, backed by Admin endpoints and services for development bootstrap admin status, user lookup, manual Premium grant/revoke, free lesson allowance reset, capabilities, and audit actions. Scripts exist for static audit and smoke coverage:

- `tools/audit_admin_shell.ps1`;
- `tools/smoke_admin_foundation.ps1`.

This foundation now includes a bootstrap-admin Admin CMS Content workspace for content editing, draft save, validation/preview summary, version listing, publish, restore, and read-only recent audit inspection. It is still **not** production Admin/CMS, not production RBAC, and not public-release readiness. Draft-save audit logging is implemented for successful Save draft operations; critical-change approval is still future governance work.


## Server verification status

As of 2026-06-09, the active CMS task is production/server verification, not new content modeling. Use `docs/CMS_ADMIN_SERVER_VERIFICATION.md` to verify server environment variables, `/admin/`, authenticated Admin CMS APIs, draft save, publish, restore/versioning, and runtime snapshot source behavior. Static JSON must stay the default runtime source unless the CMS published snapshot runtime flag is explicitly enabled after validation. Public release and external tester handoff remain blocked until CMS/Admin verification and the update/version-check system are complete.

## MVP scope: editable content

The CMS content MVP should allow authenticated admins/content managers to edit only content needed to respond to tester feedback:

1. Lesson topic metadata:
   - stable topic ID/slug;
   - display title/description for learner-facing topic cards where supported by current UI/read path;
   - order and active/published flags.
2. Lesson subtopic/situation metadata:
   - stable subtopic/scenario ID/slug;
   - parent topic;
   - learner-facing title/description;
   - situation description;
   - lesson type;
   - active/published flags.
3. Lesson starter/setup content:
   - setup message;
   - context choices;
   - opening instruction;
   - default opening example;
   - first user task.
4. Scenario guidance:
   - learning goal;
   - roles;
   - target-language notes;
   - level profiles;
   - guided follow-up questions;
   - roleplay beats;
   - expected progression;
   - controlled variation;
   - off-topic handling;
   - wrap-up/final message copy and turn thresholds.
5. Prompt templates/configuration:
   - base lesson tutor prompt template;
   - setup rules template;
   - response rules template;
   - hint prompt template/configuration;
   - feedback prompt template/configuration;
   - summary prompt/configuration if/when backend generation uses a CMS template.
6. Tutor behavior profile:
   - approved tutor behavior/personality rules;
   - approved tutor speaking rules per level;
   - identity rules for existing tutor IDs only.
7. Content operations:
   - validation;
   - preview without publishing;
   - draft/published workflow;
   - versioning;
   - rollback/restore;
   - audit trail.

## Non-goals for CMS MVP

Do **not** include the following in CMS MVP:

- production billing controls;
- Paddle management;
- payment editing;
- refunds or chargebacks;
- subscription support tools beyond existing deferred Admin planning;
- entitlement editing;
- broad user management;
- mobile-specific CMS;
- public marketplace/store release work;
- public production Admin operations;
- full multi-role enterprise Admin;
- secrets, provider keys, OpenAI API keys, webhook secrets, SMTP passwords, or email API keys;
- direct OpenAI key handling in desktop;
- direct production database editing as a CMS workflow;
- study language list editing unless explicitly approved later;
- Interface language catalog editing;
- Native/Explanation language catalog changes;
- rewriting lesson content in this planning step;
- migrating JSON into the database in this planning step.

## Proposed backend data model

Step 5D-1 adds backend-owned CMS entities with EF migration `20260603120000_AddCmsContentFoundation`. These tables are schema foundation only and are not used by runtime lesson loading yet. Suggested concepts:

### `ContentPack`

Represents a coherent editable content set.

Recommended fields:

- `Id`;
- `Slug`;
- `Name`;
- `Description`;
- `Status` (`Draft`, `Published`, `Archived`);
- `BaseStaticContentVersion` or import marker;
- `CreatedByUserId`, `CreatedAtUtc`;
- `UpdatedByUserId`, `UpdatedAtUtc`.

### `LessonTopic`

Represents a learner-facing topic.

Recommended fields:

- `Id`;
- `ContentPackId`;
- `StableTopicKey`/slug;
- `SortOrder`;
- `Title`;
- `Description`;
- `IsActive`;
- `CreatedAtUtc`, `UpdatedAtUtc`.

### `LessonScenario` or `LessonSituation`

Represents a subtopic/situation/scenario currently stored as lesson JSON.

Recommended fields:

- `Id`;
- `ContentPackId`;
- `TopicId`;
- `StableScenarioKey`/slug;
- `Title`/subtopic;
- `Description`;
- `LessonType`;
- `SupportedLevelIds`;
- `SetupMessage`;
- `ContextSelectionJson`;
- `LearningGoalJson`;
- `SituationJson`;
- `RolesJson`;
- `TargetLanguageJson`;
- `LevelProfilesJson`;
- `ConversationFlowJson`;
- `RoleplayBeatsJson`;
- `ReciprocalQuestionHandlingJson`;
- `ExpectedScenarioProgressionJson`;
- `ControlledVariationJson`;
- `OffTopicHandlingJson`;
- `FeedbackRulesJson`;
- `HintRulesJson`;
- `RepetitionLogicJson`;
- `AiTutorPromptInstructionsJson`;
- `SoftWrapUpAfterUserTurn`;
- `FinalMessageAtUserTurn`;
- `IsActive`.

Use typed owned entities where practical, but JSON columns are acceptable for an MVP if validation is strict and the published snapshot remains deterministic.

### `PromptTemplate`

Represents editable prompt/config templates.

Recommended fields:

- `Id`;
- `ContentPackId`;
- `TemplateKey` (`lesson_tutor_base`, `lesson_setup_rules`, `lesson_response_rules`, `hint`, `feedback`, `summary`);
- `TargetStudyLanguageId` nullable only when intentionally global;
- `Body`;
- `AllowedPlaceholdersJson`;
- `RequiredPlaceholdersJson`;
- `MaxLength`;
- `IsActive`;
- `UpdatedByUserId`, `UpdatedAtUtc`.

### `TutorBehaviorProfile`

Represents editable behavior rules for existing tutor IDs.

Recommended fields:

- `Id`;
- `ContentPackId`;
- `TutorId` (`elena`, `nelli`, etc. only if already supported);
- `DisplayName` read-only or tightly controlled;
- `CommunicationStyleJson`;
- `SpeakingRulesJson`;
- `IdentityRulesJson`;
- `SafetyNotesJson`;
- `IsActive`.

### `ContentVersion`

Represents each publish event.

Recommended fields:

- `Id`;
- `ContentPackId`;
- `VersionNumber`;
- `SnapshotHash`;
- `PublishStatus`;
- `PublishedByUserId`;
- `PublishedAtUtc`;
- `ValidationSummaryJson`;
- `ChangeSummary`;
- `RestoredFromVersionId` nullable.

### `PublishedContentSnapshot`

Represents immutable content used by runtime read paths.

Recommended fields:

- `Id`;
- `ContentVersionId`;
- `SnapshotJson`;
- `SnapshotHash`;
- `CreatedAtUtc`.

The runtime read path should read this immutable snapshot, not mutable draft rows.

### `ContentAuditLog`

Records content changes and publish/rollback actions.

Recommended fields:

- `Id`;
- `ActorUserId`;
- `Action` (`DraftCreated`, `DraftUpdated`, `ValidationRun`, `Published`, `RollbackPublished`, `DraftDiscarded`);
- `EntityType`;
- `EntityId`;
- `ContentPackId`;
- `BeforeHash`;
- `AfterHash`;
- `ChangedFieldsJson`;
- `Reason`;
- `CreatedAtUtc`;
- request metadata such as IP/user agent where safe and available.

## Draft/published workflow

1. Admin/content manager opens a draft content pack.
2. Edits are saved to draft rows only.
3. Draft validation can run at any time.
4. Preview uses draft content and displays the exact generated learner-facing content/prompt variables without publishing.
5. Publish is disabled until validation passes.
6. Publishing creates a new immutable `ContentVersion` and `PublishedContentSnapshot`.
7. Backend/desktop runtime uses only the latest valid published snapshot.
8. Broken drafts never affect users.
9. A previous published version can be restored by publishing a new version copied from an older snapshot.
10. Every edit, validation, publish, and restore is recorded in `ContentAuditLog`.

## Validation rules

Validation should run server-side before publish and be available on demand in Admin UI.

Required validation:

- Required fields are present and non-empty for topic title, scenario title, setup message, opening/default example, prompt template body, supported levels, and at least one scenario per active topic.
- Study language IDs are only the supported static IDs: `en`, `fr`, `de`, `pt`, `es`, `it`.
- Level IDs/names match the current supported levels used by lessons, such as `A1 Beginner`, `A2 Elementary`, `B1 Intermediate`, and `B2 Upper-Intermediate`.
- Active topic must have at least one active scenario.
- Active scenario must include level profiles for required supported levels.
- Turn thresholds are positive and final turn is greater than or equal to soft wrap-up turn.
- Prompt template body is not empty.
- Required placeholders are present; unsupported placeholders are rejected.
- Placeholder names must be from an allow-list, for example `{tutorName}`, `{learnerName}`, `{studyLanguageName}`, `{level}`, `{topicTitle}`, `{subtopicTitle}`, `{scenarioContext}`, `{nativeLanguageName}`, `{recentConversation}`.
- No invalid tutor/avatar references; tutor IDs must refer to existing approved tutor profiles.
- No unsupported Interface language assumptions; content must not expand or require new Interface language IDs.
- Content length limits are enforced for titles, descriptions, setup messages, prompt templates, roleplay beats, and free-form instructions.
- Prompt safety checks reject attempts to remove backend safety constraints, ask for secrets, expose hidden system messages, bypass lesson boundaries, or override backend authorization/payment logic.
- Content fields must not contain secrets, API keys, bearer tokens, webhook secrets, SMTP passwords, private keys, connection strings, or provider credentials.
- JSON substructures must deserialize into the current lesson-content DTO shape used by the runtime mapping layer.
- Published snapshot generation must be deterministic and hashable.

## Preview workflow

Admin UI preview should work without publishing:

1. Select content pack draft, topic, scenario/subtopic, level, study language, tutor profile, and optional learner/native language context.
2. Preview learner-facing topic/subtopic card text as the desktop would show it.
3. Preview setup/starter message and context choices.
4. Preview generated lesson opening prompt with placeholder substitution.
5. Preview prompt template variables and show unresolved, missing, or unsupported placeholders.
6. Preview hint/feedback/summary prompt configuration where applicable.
7. Show validation warnings alongside preview.
8. Make it clear that preview uses draft content and has no effect on learners until publish.

Preview should not require sending content to OpenAI in the first MVP. A later optional preview can run a real model call through the backend only, never from desktop and never with keys in the browser.

## Versioning and rollback workflow

- Every publish creates a monotonically increasing `ContentVersion`.
- Each `ContentVersion` has an immutable `PublishedContentSnapshot` and hash.
- Admin UI can list previous versions with publish time, publisher, change summary, validation status, and restore status.
- Admin UI can view a previous version snapshot in read-only mode.
- Restore copies a previous snapshot into a new draft or publishes a new version from that snapshot after validation.
- Runtime never points to a mutable draft.
- Audit trail records who changed what, when, and why.

## Fallback strategy

The accepted lesson flow must keep working during migration:

- Backend read path can try the latest valid published CMS snapshot only when `CmsContent:ReadPublishedSnapshotEnabled=true`.
- If CMS content is unavailable, unpublished, invalid, disabled by configuration, corrupt, missing required sections, or any CMS read error occurs, backend should use the current static JSON/content behavior when `CmsContent:FallbackToStaticJson=true` (the default).
- Existing desktop package flow must not be blocked by half-migrated CMS content.
- Current lesson JSON remains the baseline fallback until the CMS read path is proven and explicitly made primary.
- No tester should be blocked by a broken draft.
- If a published snapshot is corrupt, the system should either roll back to the previous valid snapshot or fall back to static JSON while alerting admins.

## Migration/import strategy from existing lesson JSON

1. Keep current JSON unchanged while building the CMS schema and importer.
2. Add an import tool/service that reads existing `Content/Lessons`, `Content/Prompts`, and `Content/Tutors` into a draft `ContentPack`.
3. Run validation against imported content.
4. Publish the imported content as version 1 only after validation passes.
5. Compare generated published snapshot against current static content mapping for deterministic equivalence.
6. Enable backend read path with feature flag/config and JSON fallback.
7. Run desktop regression and content audit before tester handoff.

## Security model

- CMS/Admin APIs require authentication.
- Content editing requires an admin/content-manager role or policy; anonymous edits are forbidden.
- Existing development bootstrap Admin policy is not enough for production CMS. Production roles/RBAC must be explicit before production use.
- Read/write operations must be audited.
- Publishing and rollback should require a reason/change summary.
- Production users/admins must not edit PostgreSQL directly as a CMS workflow.
- CMS fields must not store secrets.
- Backend remains the source of truth.
- Desktop continues to call backend APIs only.
- Desktop must not receive OpenAI API keys or provider secrets.
- Prompt preview/model calls, if added later, must go through backend services with normal auth, rate limits, and audit logging.

## Proposed Admin UI sections

Minimum UI:

1. Content dashboard:
   - current published version;
   - latest draft status;
   - validation status;
   - publish/rollback shortcuts.
2. Topics:
   - list, edit title/description/order/active state;
   - show scenario counts and validation state.
3. Scenarios/Situations:
   - edit setup message, situation, context choices, openings, level profiles, roleplay beats, wrap-up/final behavior, hint/feedback rules.
4. Prompt templates:
   - edit controlled templates;
   - show placeholders and validation.
5. Tutor behavior:
   - edit bounded rules for existing tutor profiles.
6. Preview:
   - learner-facing preview and prompt-variable preview.
7. Versions:
   - view previous versions;
   - restore previous version.
8. Audit log:
   - who changed/published/restored content and when.

## Implementation phases

1. Document CMS MVP scope and data model. **Implemented in Step 5D-0.**
2. Add backend CMS content models and EF migration after approval. **Implemented in Step 5D-1 as schema foundation only.**
3. Import current JSON content into CMS draft/published seed or migration path without changing current JSON. **Implemented in Step 5D-2 as a bootstrap-admin static import foundation.**
   - The importer reads `Content/Lessons`, `Content/Prompts`, `Content/Tutors`, and the static study-language reference file.
   - It creates or updates the stable content pack `static-json-v1` / `Static JSON Baseline`.
   - It imports topics inferred from lesson JSON metadata, scenarios, the three file-backed lesson prompt templates, and current tutor behavior profiles.
   - It validates required fields, supported static study-language IDs, scenario turn bounds, prompt/tutor emptiness, deterministic serialization, and obvious secret-like content before publishing.
   - If validation succeeds, it creates a published content version and `PublishedContentSnapshot` for the current static baseline; repeat imports skip version creation when the snapshot hash is unchanged.
   - Topic descriptions are currently empty because the existing lesson JSON does not carry dedicated topic descriptions; desktop topic display remains unchanged.
   - Hint, feedback, summary, translation, and immutable safety/runtime behavior remain code-owned unless they are already backed by imported prompt files.
   - Runtime lesson loading still uses static JSON/content and does not read CMS content yet.
4. Add backend published-content read path with static JSON fallback. **Implemented in Step 5D-3 as a bootstrap-admin status and service read path.**
   - `ICmsPublishedContentService` / `CmsPublishedContentService` read the latest published `ContentVersion` and `PublishedContentSnapshot` for the configured content pack slug.
   - The read path verifies the snapshot hash, deserializes into a mapped published content model containing topics, scenarios, prompt templates, and tutor behavior profiles, and validates required runtime-facing fields before reporting the CMS snapshot as usable.
   - Safe config defaults are `CmsContent:ReadPublishedSnapshotEnabled=false`, `CmsContent:ContentPackSlug=static-json-v1`, and `CmsContent:FallbackToStaticJson=true`.
   - `GET /api/admin/dev/cms/published-content/status` is Development-only and requires the existing bootstrap admin policy; it returns counts, hash/version status, validation status, and fallback status without returning prompt bodies.
   - Runtime lesson loading remains static JSON by default; this step prepared the read/status path and did not add an Admin UI editor or public learner-facing CMS endpoints.
5. Add Admin content API for draft read/update operations plus server-side validation and preview summaries. **Implemented in Step 5D-4 as bootstrap-admin backend endpoints.**
   - Content packs, topics, scenarios, prompt templates, and tutor behavior profiles can be listed/read.
   - Bounded draft updates are audited and validation/preview summary endpoints are available.
6. Add publish/version/rollback workflow and audit log. **Implemented in Step 5D-5 as bootstrap-admin backend endpoints.**
   - `GET /api/admin/dev/cms/content-packs/{slug}/versions` lists immutable published versions.
   - `GET /api/admin/dev/cms/content-packs/{slug}/versions/{versionNumber}` returns safe version detail, validation summary, counts, and hash validity.
   - `POST /api/admin/dev/cms/content-packs/{slug}/publish` validates draft rows, generates a deterministic snapshot, skips unchanged publishes, and creates a new `ContentVersion` plus `PublishedContentSnapshot` when changed.
   - `POST /api/admin/dev/cms/content-packs/{slug}/versions/{versionNumber}/restore` verifies the selected snapshot hash, restores it into draft rows, validates, and can publish a new rollback version without mutating history.
   - Runtime lesson loading remains static JSON by default; CMS read remains disabled by default and static JSON fallback remains available.
7. Add simple Admin UI/editor shell for content editing. **Implemented in Step 5D-6 through Step 5D-6d as a development-only static Admin shell tab with refresh resilience and unsaved-change protection.**
   - The existing backend Admin shell now includes a CMS Content tab after admin login, with internal sub-tabs for Overview, Topics, Scenarios, Prompts, Tutors, Validation & Preview, Versions & Publish, and Audit.
   - Authenticated bootstrap admins can load `static-json-v1`, view pack summary/counts, list topics/scenarios/prompt templates/tutor behavior profiles, select an item by row click or Select button, and make draft edits through the Step 5D-4 backend APIs.
   - Scenario editing supports bounded fields, structured form editing for common scenario content, and Full scenario JSON editing. `Format JSON` only pretty-prints/re-indents JSON, and `Validate JSON` checks syntax plus required scenario fields; neither action saves or publishes, so `Save draft` is required to persist edits.
   - The shell can run validation, load preview summary, list versions, publish the current draft with confirmation, and restore a previous version with confirmation.
   - Admin refresh uses the existing admin-only HTTP-only cookie, the JWT remains memory-only, Web Storage is not used, the URL hash stores only safe identifiers, selected user/CMS entities restore after refresh, and unsaved dirty state is tracked in memory with discard warnings.
   - The shell is intentionally basic and development-only; it is not production CMS/RBAC, does not call OpenAI, does not expose secrets, does not edit study/interface languages, does not mutate old version history directly, and does not persist unsaved content in browser storage.
   - Runtime lesson loading remains static JSON by default; CMS read remains disabled by default and static JSON fallback remains available.
8. Add CMS draft-save audit logging and Admin CMS audit visibility. **Implemented: successful Topic, Scenario including full scenario JSON, Prompt Template, and Tutor Behavior Profile Save draft operations write bounded audit rows, and the Admin CMS Audit subtab exposes read-only recent changes with entity type, stable key text, and limit filters.**
   - Each content edit should record actor identity, timestamp UTC, content pack, entity type, stable key/id, changed fields, old/new values or hashes for large values, source, and request/correlation id.
9. Add critical-change approval workflow after production roles/RBAC exist. **Later governance step.**
10. Run desktop regression, release gate, content audits, and active lesson guard smoke where relevant. **Future phase.**
11. Then prepare controlled external tester handoff after CMS/Admin content MVP is ready enough for controlled content fixes. **Future phase.**

## Risks and mitigations

- **Risk:** Broken draft affects lessons. **Mitigation:** runtime reads only published snapshots, never drafts.
- **Risk:** CMS prompt edits weaken safety. **Mitigation:** immutable backend safety rules stay code-owned; prompt safety validation blocks unsafe templates.
- **Risk:** Half migration blocks testers. **Mitigation:** keep static JSON fallback until CMS is proven.
- **Risk:** Content stores secrets. **Mitigation:** secret scanning validation and audit trail.
- **Risk:** Admin foundation is mistaken for production CMS. **Mitigation:** document that current Admin shell is development/support foundation only.
- **Risk:** Billing/admin scope creep. **Mitigation:** keep billing/Paddle/subscription/entitlement operations deferred and outside this MVP.

## Future CMS governance requirements

Step 5D-6d adds refresh resilience and unsaved-change protection after the earlier Admin CMS table selection UX/governance documentation step. It does not add production roles, does not add an approval workflow, does not add database schema, and does not change runtime learner behavior. The bootstrap-admin Admin CMS draft editing flow remains in place, and publish/restore actions continue to rely on explicit confirmation dialogs for this phase.

### CMS draft-save audit logging

Successful Admin CMS Save draft operations are audited before production CMS operations. The audit record includes:

- actor user id;
- actor email;
- timestamp UTC;
- content pack slug;
- entity type: topic, scenario, prompt template, tutor behavior profile;
- entity id / stable key;
- changed fields;
- before value, or before hash when storing full values is too large;
- after value, or after hash when storing full values is too large;
- source: Admin UI / API;
- request/correlation id if available.

This requirement exists so future operators can understand who changed prompts, scenarios, tutor behavior, or lesson content.

### Future approval workflow

Critical CMS changes should later require approval after production roles are implemented. Examples of critical changes include:

- prompt template changes;
- tutor behavior/safety changes;
- large scenario changes;
- disabling important content;
- publish actions.

Future roles may include:

- Content Editor;
- Content Reviewer;
- Admin / Owner.

Draft editing and approval should be separated when roles exist. For now, keep the existing development-only admin flow and confirmation dialogs for publish/restore.

## Acceptance criteria for CMS content MVP implementation

- Authenticated admin/content-manager can edit draft content for topics, scenarios, starter messages, prompt templates, tutor behavior rules, and hint/feedback/summary configuration where applicable.
- Draft changes do not affect learners.
- Server validation catches required fields, unsupported study language IDs, invalid levels, missing scenarios, bad tutor references, invalid placeholders, length violations, prompt safety issues, and secrets.
- Admin can preview learner-facing content and prompt placeholders without publishing.
- Admin can publish only valid content.
- Every publish creates a version and immutable snapshot.
- Admin can view previous versions and restore one safely.
- CMS draft-save audit logging records edits with actor, timestamp, entity type, stable key/id, changed fields, old/new values or hashes, and request/correlation id; publish and rollback audit coverage remains required before production operations.
- Backend runtime uses published content only and falls back to static JSON if CMS content is unavailable.
- Existing accepted desktop Lesson Chat, Conversation Mode, TTS, STT, translation, hints, feedback, summary, active lesson guard, and tester package flow remain working.
- No billing, Paddle, subscription, entitlement, payment, study-language, Interface-language, Native/Explanation-language, desktop key handling, or public release scope is changed by CMS content MVP.


Implemented CMS draft-save audit logging details: successful Topic, Scenario (bounded fields, structured scenario fields, and full scenario JSON), Prompt Template, and Tutor Behavior Profile Save draft operations write `DraftSaved` rows to `cms_content_audit_logs`. Rows capture audit id, `createdAtUtc`, actor user id, actor email when available, content pack id and slug, entity type, entity id, stable key (`stableTopicKey`, `stableScenarioKey`, `templateKey`, or `tutorId`), changed field names, before/after SHA-256 hashes, source `AdminCms`, status, and request id when available. Audit rows intentionally do not store or display full before/after JSON snapshots, prompt/tutor source text snapshots, passwords, tokens, provider secrets, OpenAI API keys, Paddle API keys/webhook secrets, or admin bearer tokens. Large edited values are represented by hashes. No-op Save draft requests avoid noisy draft-save audit rows. Admins can read recent CMS audit entries through bootstrap-admin-protected audit endpoints and the CMS Content Audit subtab, which is aligned to the selected content pack (`static-json-v1` by default) and supports entity type, stable key text, limit, Refresh audit controls, and a **Show smoke/test entries** debugging checkbox. Smoke/test entries are hidden by default while normal manual Admin CMS UI changes remain visible. Runtime learner behavior is unchanged: CMS read path remains disabled by default and static JSON fallback remains available. Production RBAC and critical-change approval remain future work.

Step 5D-6e scenario editor usability refinement is complete. The Admin CMS Scenarios subtab now includes a safer structured editor for common scenario content (title/subtopic, description, setup message, first bot message guidance, context option titles, valid context keywords, custom context rules, invalid context redirect, goal text, can-do statements, opening/first-user-task/follow-up guidance, AI tutor instructions, wrap-up/final message guidance, hint example, and wrap/final turn counters), with compact local **Jump to** navigation, collapsible/visually separated Basic fields, Lesson setup, Context selection / choices, Conversation flow / response guidance, Wrap-up / summary guidance, and Advanced JSON sections, and concise helper text for normal content editors. `DefinitionJson` remains the canonical stored scenario definition; no per-field scenario database columns or EF migration were added. Structured edits parse the current `DefinitionJson`, update only known JSON paths, and write the merged valid JSON back to `DefinitionJson`, preserving unknown fields and advanced configuration in place. Structured fields remain the recommended normal editing path. Advanced JSON remains available as a visually separated technical fallback with `Format JSON` and `Validate JSON` for rare full-JSON changes. Save draft remains explicit and draft-only; invalid Advanced JSON or invalid structured numeric/required data is rejected before saving, and backend scenario validation still rejects invalid JSON, missing required fields, or accidental stable id/title/setup mismatches. CMS draft-save audit logging still records successful scenario saves with changed field names and before/after hashes without storing full scenario JSON bodies. Runtime learner behavior remains unchanged by default: the CMS read path is still disabled unless explicitly enabled, and static JSON fallback remains available.


## Save draft publish guidance

Admin CMS draft saves now include explicit publish guidance in the editing context. A successful `Save draft` shows “Draft saved. To apply this content to runtime, publish the current draft.” and a **Go to Publish** action. The action only switches the UI to **Versions & Publish** and preserves content pack/entity/user URL hash state; it does not publish. The confirmed **Publish current draft** button in **Versions & Publish** remains the only publish mechanism. Changed-content publish requires a short publish change summary, while no-change publish checks can skip one; backend publish errors/warnings and validation details are displayed near the publish controls. Runtime CMS mode continues to read only published snapshots, static JSON remains default, and the CMS runtime path remains disabled by default. A local verification with `CmsContent__UsePublishedSnapshotForRuntime=true`, `CmsContent__ReadPublishedSnapshotEnabled=true`, `CmsContent__ContentPackSlug=static-json-v1`, and `CmsContent__FallbackToStaticJson=true` confirmed `Source=CmsPublishedSnapshot`, `ContentPackSlug=static-json-v1`, `VersionNumber=34`, `FallbackUsed=False`, `ValidationPassed=True`, `TopicCount=6`, `ScenarioCount=26`, `PromptTemplateCount=3`, and `TutorBehaviorProfileCount=2`.

## Controlled runtime CMS read path implementation note

A backend runtime lesson-content read service now provides the safe handoff point from static JSON to CMS published snapshots. The default remains static JSON. CMS runtime mode requires `CmsContent:UsePublishedSnapshotForRuntime=true`; snapshot reads still require `CmsContent:ReadPublishedSnapshotEnabled=true`; `CmsContent:ContentPackSlug` defaults to `static-json-v1`; `CmsContent:FallbackToStaticJson` controls whether invalid/missing CMS content falls back to packaged static JSON. The runtime service maps published topics, scenarios, prompt templates, and tutor behavior profiles into the runtime content shape and validates the current baseline counts (6 topics, 26 scenarios, 3 prompt templates, 2 tutor behavior profiles) plus required fields. It reads only published snapshot JSON and never draft rows, preserving immutable publish semantics. Scenario `DefinitionJson` remains present on published scenarios to preserve structured editor output and unknown advanced JSON fields. `/api/admin/dev/cms/runtime-content/status` is the bootstrap-admin diagnostic for source, version, hash, counts, fallback, validation, and bounded errors/warnings. The local runtime path and existing Admin CMS smoke coverage are verified, but external tester handoff remains paused until the production/server CMS/Admin verification and update/version-check work are complete. Production RBAC and critical-change approval remain future work.


## Next CMS refinement after latest completed step

Step 5D-6e is complete for Admin CMS scenario editor usability refinement, including compact local **Jump to** navigation, collapsible/visually separated structured scenario sections, helper text for normal content editors, and a visually separated Advanced JSON technical fallback. Structured fields remain the recommended normal editing path. This is still not full production RBAC readiness, not external tester handoff readiness, and not public release readiness until server CMS/Admin verification and update/version-check work are complete.

The next recommended implementation step is another CMS/admin improvement, not billing:

1. Refine Validation & Preview and content QA workflow ergonomics so admins can review draft content more efficiently before publishing.
2. Keep `Save draft` draft-only and keep publishing isolated in **Versions & Publish** with required publish summaries for changed content.
3. Continue preserving immutable published version history; restore should keep creating a new published version rather than mutating old versions.
4. Later, after production roles exist, add proper production RBAC and critical-change approval workflow.
