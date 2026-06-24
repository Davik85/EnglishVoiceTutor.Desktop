# CMS / Admin Planning

Review date: 2026-06-13.

## Current product decision

CMS/Admin content foundation is now the next focus **before external tester handoff**. The desktop hardening block is stable enough to pause tester delivery and build a content editing foundation first, so future tester feedback about lessons, scenarios, prompts, and tutor behavior can be fixed through CMS instead of code or JSON changes.

This changes the previous "not built now" decision only for the **content-focused CMS foundation**. Full production Admin and production billing operations remain deferred.

## Current foundation

A local Development admin support foundation exists for controlled diagnostics/support work, with existing smoke/audit coverage. It is not a full CMS, is not production Admin, and does not make public release ready.

Relevant existing foundation:

- `backend/EnglishVoiceTutor.Api/wwwroot/admin/index.html`
- `backend/EnglishVoiceTutor.Api/wwwroot/admin/admin.js`
- `backend/EnglishVoiceTutor.Api/wwwroot/admin/admin.css`
- `backend/EnglishVoiceTutor.Api/Endpoints/AdminEndpoints.cs`
- `backend/EnglishVoiceTutor.Api/Services/Admin/*`
- `tools/smoke_admin_foundation.ps1`
- `tools/audit_admin_shell.ps1`

Current Admin foundation supports development bootstrap admin status/capabilities, user lookup, manual Premium grant/revoke, free lesson allowance reset, audit-action visibility, and the bootstrap-admin `CMS Content` workspace. The Admin CMS workspace has sub-tabs for Overview, Topics, Scenarios, Prompts, Tutors, Validation & Preview, Versions & Publish, and Audit. It supports content pack overview, topic editing, scenario editing, structured scenario editing, advanced full scenario JSON editing, prompt template editing, tutor behavior profile editing, validation/preview summary, versions/publish/restore flows, and read-only recent audit review with smoke/test entries hidden by default. It is still not production Admin/CMS, not production RBAC, and not public-release readiness.

Admin CMS refresh resilience is in place: admin auth survives refresh via the existing admin-only HTTP-only cookie, the JWT remains memory-only in JavaScript, browser Web Storage is not used, the URL hash stores only safe workspace identifiers, and selected user/CMS entities are restored after session validation. Unsaved CMS changes are tracked in memory, show a visible dirty indicator, warn before refresh/navigation/entity switching/logout discards edits, and are not persisted in browser storage or the URL hash. `Save draft` remains explicit and required to persist CMS edits. It persists drafts only, never runtime-visible content; after save the UI tells the admin to publish the current draft and offers **Go to Publish**. Browser publishing requires a change summary for changed-content publishes and shows a clear local validation error when that summary is missing.


## Latest completed Admin CMS Content step

Step 5D-6e is complete for the development/admin Admin CMS Scenarios editor usability refinement. The Scenarios editor now has compact local **Jump to** navigation, collapsible and visually separated sections for Basic fields, Lesson setup, Context selection / choices, Conversation flow / response guidance, Wrap-up / summary guidance, and Advanced JSON, plus helper text for normal content editors. Structured scenario fields load from and update canonical `DefinitionJson` while preserving unknown JSON fields, and they remain the recommended normal editing path. Advanced JSON remains available as a visually separated technical fallback for rare full-JSON edits. `Format JSON` only pretty-prints, and `Validate JSON` only checks syntax and required scenario fields; neither action saves, publishes, or persists edits.

Publishing remains isolated in **Versions & Publish**. Draft changes are not visible to learner runtime until published. Published versions are immutable, and restore copies a previous published version into a new version instead of mutating old versions.

Local runtime CMS read was confirmed only under explicit development configuration: `CmsContent__ReadPublishedSnapshotEnabled=true`, `CmsContent__UsePublishedSnapshotForRuntime=true`, `CmsContent__ContentPackSlug=static-json-v1`, and `CmsContent__FallbackToStaticJson=true`; backend logs showed `Source=CmsPublishedSnapshot`, `VersionNumber=34`, validation passed, no fallback, and the expected 6 topics, 26 scenarios, 3 prompt templates, and 3 tutor behavior profiles. Static JSON fallback remains available.

CMS/Admin is connected, and `static-json-v1` is initialized as Draft/admin content for the last verified tester release snapshot. This is still not full production RBAC readiness, not critical-change approval readiness, and not broad public release readiness. Learner runtime now uses the CMS published snapshot; static JSON fallback remains available but should not be active in normal runtime status. Production billing remains deferred. Production RBAC and critical-change approval come later.

## CMS/Admin content foundation goal

Create a safe backend-owned CMS/Admin foundation for editing lesson and prompt content before external testers begin content QA.

The product must allow content edits without changing desktop runtime code or packaged lesson JSON after the migration is implemented and approved. It must protect users by serving only published content and falling back to current static JSON if CMS content is unavailable.

Detailed plan: `docs/cms-content-mvp-plan.md`.

## Product scope

CMS content foundation should focus on:

- lesson topics;
- subtopics/situations/scenarios;
- lesson setup/starter messages;
- context choices and roleplay beats;
- lesson opening/default starter examples;
- prompt templates for lesson tutor/setup/response behavior;
- tutor behavior instructions for existing approved tutor profiles;
- hint/feedback/summary prompt configuration where applicable;
- content validation;
- preview without publishing;
- draft/published workflow;
- published versions and immutable snapshots;
- rollback/restore previous version;
- audit trail for content changes.

## Explicit non-goals for the product

Do not include:

- production billing controls;
- Paddle management or Paddle logic changes;
- payment editing, refunds, or chargebacks;
- subscription support tooling beyond existing deferred planning;
- entitlement editing;
- broad user management;
- public production Admin operations;
- mobile-specific CMS;
- full multi-role enterprise Admin;
- public marketplace/store release work;
- secrets or provider keys in CMS fields;
- direct OpenAI key handling in desktop;
- direct production database editing as a CMS workflow;
- study language list editing unless explicitly approved later;
- Interface language catalog editing;
- Native/Explanation language catalog changes.

## Language boundaries

Study languages must remain exactly:

- English
- French
- German
- Portuguese
- Spanish
- Italian

Release-ready Interface languages must remain exactly:

- `en`
- `es`
- `fr`
- `de`
- `it`
- `pt`
- `ru`
- `pl`
- `ar`
- `ja`
- `ko`
- `sr`
- `hr`
- `bg`

Native/Explanation languages remain broad. CMS foundation must not expand or narrow these catalogs.

## Content workflow requirements

CMS content lifecycle must include:

1. Admin/content manager edits a draft.
2. Validation runs before publish.
3. Preview works against draft content without publishing.
4. Backend/desktop runtime uses only published content.
5. Every publish creates a new version and immutable published snapshot.
6. Previous published versions can be viewed and restored.
7. Broken drafts never affect users.
8. Static JSON remains available as fallback during rollout.
9. Audit trail records who changed what and when; the current Admin CMS Audit subtab exposes recent draft-save changes as read-only metadata rows with entity type, stable key, and limit filters, hides smoke/test entries by default, and offers a **Show smoke/test entries** checkbox for debugging.

## Validation requirements

Server-side validation must check at minimum:

- required fields;
- supported study language IDs only;
- valid level IDs/names;
- non-empty prompt templates;
- required/allowed placeholders;
- no missing active scenarios under active topics;
- valid existing tutor/avatar references;
- no unsupported Interface language assumptions;
- content length limits;
- prompt safety/boundary checks;
- no secrets, keys, tokens, connection strings, or provider credentials in content/prompt fields.

## Proposed rollout sequence

1. Document CMS foundation scope and data model. This planning task.
2. Add backend CMS content models and EF migration after plan approval.
3. Import current JSON content into CMS draft/published seed or migration path without changing lesson JSON.
4. Add backend read path for latest published CMS content with fallback to current static JSON.
5. Add Admin content API for draft read/update.
6. Add simple Admin UI for content editing.
7. Add validation and preview endpoints/UI. **Backend/API and development Admin UI summary are in place.**
8. Add publish/version/rollback. **Implemented for development/admin use.**
9. Add CMS draft-save audit logging. **Next recommended CMS implementation step.** Audit each draft edit with actor, timestamp, content pack, entity type, stable key/id, changed fields, old/new values or hashes, source, and request/correlation id.
10. Add critical-change approval workflow after production roles/RBAC exist. **Later governance step.**
11. Run desktop regression and release gate.
12. Then send controlled tester package to external testers after CMS/Admin content foundation is ready enough for practical content changes without code edits.

## Risks

- Broken draft content could disrupt lessons if runtime reads drafts. Mitigation: runtime reads immutable published snapshots only.
- Prompt edits could weaken safety. Mitigation: keep backend safety rules code-owned and validate prompt templates.
- CMS migration could block testers. Mitigation: keep current static JSON fallback until CMS is proven.
- Scope creep could pull billing/Admin operations into CMS foundation. Mitigation: keep billing, Paddle, subscription, entitlement, and broad user management deferred.
- Secrets could be entered into prompt fields. Mitigation: validate and audit content fields; never store provider secrets in CMS.
- Admin refresh/session behavior could accidentally persist sensitive drafts. Mitigation: keep JWT memory-only, use only the existing admin-only HTTP-only cookie for refresh auth, store only safe URL hash identifiers, avoid Web Storage, and never persist unsaved CMS content in browser storage.

## Acceptance criteria for this planning step

- Documentation reflects CMS/Admin content foundation as the next focus before external tester handoff.
- Documentation clearly separates CMS content foundation from full production Admin.
- Production billing remains deferred.
- Public release remains not ready.
- Editable content and non-goals are defined.
- Draft/published, validation, preview, versioning, rollback, fallback, and security requirements are documented.
- No runtime code, backend code, desktop UI code, lesson JSON, EF migration, billing logic, Paddle logic, subscription logic, entitlement logic, or payment logic is changed by this planning step.


Implemented CMS draft-save audit logging details: successful Topic, Scenario (bounded fields, structured scenario fields, and full scenario JSON), Prompt Template, and Tutor Behavior Profile Save draft operations write `DraftSaved` rows to `cms_content_audit_logs`. Rows capture audit id, `createdAtUtc`, actor user id, actor email when available, content pack id and slug, entity type, entity id, stable key (`stableTopicKey`, `stableScenarioKey`, `templateKey`, or `tutorId`), changed field names, before/after SHA-256 hashes, source `AdminCms`, status, and request id when available. Audit rows intentionally do not store or display full before/after JSON snapshots, prompt/tutor source text snapshots, passwords, tokens, provider secrets, OpenAI API keys, Paddle API keys/webhook secrets, or admin bearer tokens. Large edited values are represented by hashes. No-op Save draft requests avoid noisy draft-save audit rows. Admins can read recent CMS audit entries through bootstrap-admin-protected audit endpoints and the CMS Content Audit subtab, which is aligned to the selected content pack (`static-json-v1` by default) and supports entity type, stable key text, limit, and Refresh audit controls. Runtime learner behavior now uses CMS published snapshot; static JSON fallback remains available for rollback/safety. Production RBAC and critical-change approval remain future work.

Structured scenario editor update: the Admin CMS Scenarios subtab now includes a safer structured editor for common scenario content (title/subtopic, description, setup message, first bot message guidance, context option titles, valid context keywords, custom context rules, invalid context redirect, goal text, can-do statements, opening/first-user-task/follow-up guidance, AI tutor instructions, wrap-up/final message guidance, hint example, and wrap/final turn counters). `DefinitionJson` remains the canonical stored scenario definition; no per-field scenario database columns or EF migration were added. Structured edits parse the current `DefinitionJson`, update only known JSON paths, and write the merged valid JSON back to `DefinitionJson`, preserving unknown fields and advanced configuration in place. Advanced JSON remains available with `Format JSON` and `Validate JSON` for rare technical changes. Save draft remains explicit; invalid Advanced JSON or invalid structured numeric/required data is rejected before saving, and backend scenario validation still rejects invalid JSON, missing required fields, or accidental stable id/title/setup mismatches. CMS draft-save audit logging still records successful scenario saves with changed field names and before/after hashes without storing full scenario JSON bodies. Runtime learner behavior now targets the CMS published snapshot when enabled, valid, and effectively active; static JSON fallback remains available for emergency safety.



## Save draft to publish discoverability

`Save draft` is intentionally draft-only: it persists CMS draft changes and audit entries, but it never publishes and never changes runtime learner behavior. The Admin CMS editor now displays “Draft saved. To apply this content to runtime, publish the current draft.” after successful draft saves and provides a **Go to Publish** action that switches to **Versions & Publish** while preserving the selected content pack, selected CMS entity, selected user, and URL hash state. **Versions & Publish** remains the source of truth for publication and keeps the explicit **Publish current draft** confirmation flow. Publishing changed content requires a short publish change summary; no-change publish checks may skip the summary. Publish failures now surface backend errors/warnings and validation details near the publish controls. Runtime CMS mode reads immutable published snapshots only; CMS published snapshot is now active at runtime, while static JSON fallback remains available for rollback/safety.

## Runtime published snapshot integration guardrails

The CMS runtime read path is intentionally controlled and reversible. CMS published snapshot is the intended primary learner source when `CmsContent:UsePublishedSnapshotForRuntime=true` is set alongside `CmsContent:ReadPublishedSnapshotEnabled=true`. Runtime reads only immutable published snapshots for `CmsContent:ContentPackSlug` (`static-json-v1` by default), never draft content. Scenario `DefinitionJson` is carried through the published snapshot so structured scenario fields and unknown advanced JSON fields remain available to runtime logic. If the selected snapshot is missing or invalid, fallback to static JSON occurs only when `CmsContent:FallbackToStaticJson=true`; otherwise diagnostics report a server-side content error rather than silently serving broken CMS content. Logs and diagnostics expose source, slug, version, snapshot hash, counts, fallback state, validation status, and bounded errors/warnings only.

## 2026-06-13 update — CMS connection readiness

### Completed

- Readable Validation & Preview UI is complete for the deployed Admin CMS. Validation now shows Passed/Failed status, counts, errors, warnings, and collapsed raw validation JSON instead of dumping raw JSON in the main result area. Preview now shows readable metadata, counts, sample topics, sample scenarios, and collapsed raw preview JSON.
- Admin static asset cache busting and no-cache behavior are complete for `/admin` assets. `admin.js` and `admin.css` use the `admin-cms-20260613-raw-json-fix` version token, and no-cache headers apply to `/admin` static files only.
- Backend `0.1.35-backend.11` is the deployed backend containing the latest Admin CMS UI/cache fixes. The current backend symlink points to `/opt/languagevoicetutor/backend/releases/0.1.35-backend.11`; rollback reference is `/opt/languagevoicetutor/backend/releases/0.1.35-backend.8`.
- Health and database health are green after deploy. Build is green. Admin shell audit is green. EF model check reports no pending model changes. No EF migration was required.

### Current state

The Admin CMS practical workflow has reached the runtime milestone. CMS published snapshot is now active for controlled tester lessons, and static JSON fallback remains available for rollback/safety.

### Next safe step

Move from Admin CMS foundation/UI cleanup to CMS connection readiness:

1. Verify publish and restore safety with `static-json-v1`.
2. Confirm audit traceability for safe draft edits, publish, and restore actions.
3. Add or confirm runtime-read diagnostics for source, slug, version, snapshot hash, fallback state, validation status, and bounded errors/warnings.
4. Validate the published-snapshot runtime path only in a controlled development environment or explicitly approved environment.
5. Keep rollback to static JSON documented and tested.

### Not ready yet

- Production RBAC remains future work.
- Critical-change approval remains future work.
- Full Admin CMS production readiness remains future work.
- Billing and Paddle are outside CMS scope and remain deferred.
- Broad public production release is not ready.

### Do not enable by default

CMS published-snapshot runtime is now the intended learner default when enabled, valid, and effectively active. Keep static JSON available for initialization and emergency fallback, and document rollback/disable instructions separately.

## Runtime content status diagnostic

A bootstrap-admin protected, read-only runtime status diagnostic is available at `GET /api/admin/dev/cms/runtime-status`. The Admin CMS **Validation & Preview** area displays this as **Runtime content status**. It intentionally shows metadata only: flags, effective source, slug, version/hash, counts, validation status, fallback status, and bounded errors/warnings.

Controlled server validation comes first; localhost is only an explicit developer override for local backend work. This diagnostic must not be treated as runtime enablement. Learner runtime now reads CMS published snapshots in the controlled tester environment. The diagnostic confirms source, validation, and fallback state; rollback remains returning runtime to static JSON if needed.

### Runtime tutor profile validation note

The runtime validator must treat desktop tutor avatar definitions as the approved tutor-id source of truth. The currently approved tutor ids are `david`, `lana`, and `nelli`, matching packaged `Content/Tutors/*.json` and CMS static import/draft construction. The previous exact count of 2 was outdated; future diagnostics should report expected, actual, missing, unknown/extra, and duplicate ids while never exposing prompt or tutor instruction bodies.

## 2026-06-13 update — Controlled published-snapshot runtime validation tooling

Backend `0.1.35-backend.11` is active and runtime status diagnostics are clean: `effectiveSource=CmsPublishedSnapshot`, `validationSuccess=Yes`, `fallbackUsed=No`, no errors, no warnings, and `tutorBehaviorProfiles=3`. CMS published snapshot runtime is active for controlled tester lessons; static JSON fallback remains available for rollback/safety.

The next step is controlled CMS published-snapshot runtime validation using `tools/validate_cms_published_snapshot_runtime.ps1`. The script is safe by default: read-only mode calls the admin runtime-status diagnostic and verifies that CMS snapshot runtime is not the learner default. `-GenerateServerValidationPlan` only prints the temporary operator plan and does not edit production config, restart services, or run destructive commands.

Controlled validation must be explicit, reversible, and operator-approved. The runtime flags are `CmsContent__UsePublishedSnapshotForRuntime=true`, `CmsContent__ReadPublishedSnapshotEnabled=true`, `CmsContent__ContentPackSlug=static-json-v1`, and `CmsContent__FallbackToStaticJson=true`. During validation, runtime status must confirm `effectiveSource=CmsPublishedSnapshot`, `validationSuccess=true`, 6 topics, 26 scenarios, 3 prompt templates, and 3 tutor behavior profiles. Rollback is to disable or remove the temporary CMS runtime flags, restart backend, and confirm `effectiveSource=StaticJson` again.

CMS runtime is active for the controlled tester phase; do not expand this into broad public release without a separate decision. Billing, Paddle, subscriptions, entitlements, payments, installer behavior, desktop runtime behavior, public `latest.json`, deployment scripts, lesson JSON, and EF migrations are outside this validation step.

## CMS-managed level profiles (A1-B2)

- CMS now manages A1, A2, B1, and B2 level behavior profiles through the CMS Content **Levels** tab.
- Level profiles include stable level keys, display names, active flags, sort order, wrap-up turn, final-message turn, language complexity guidance, correction guidance, answer-length guidance, and admin notes.
- Lesson length defaults come from the selected level profile: A1 is configured for a shorter lesson around 15 learner turns, while B2 supports a longer dialogue.
- Scenario-specific lesson length values remain optional overrides when explicitly set and valid. Priority is: scenario override, then CMS level profile, then safe backend constants.
- Backend runtime content remains the source of truth for lesson behavior. Desktop may keep its current level labels for display, but desktop and future mobile should use backend runtime behavior from the CMS published snapshot.
- Static JSON fallback remains available; fallback runtime also receives safe default level profiles.

## 2026-06-13 milestone — CMS runtime active for controlled tester handoff

Backend `0.1.35-backend.11` is the active backend release. Windows tester `0.1.36-tester.8` is the current uploaded tester build in the public direct Windows release folder, with `latest.json` expected to point to `LanguageVoiceTutorSetup-0.1.36-tester.8.exe`, `backendBaseUrl=https://api.languagevoicetutor.com`, and `updateMode=manual-confirmation`.

CMS published snapshot runtime is now active for controlled tester lessons. Runtime status should show `effectiveSource=CmsPublishedSnapshot`, `validationSuccess=Yes`, `fallbackUsed=No`, no errors, and no warnings. CMS scenario edits are visible in the desktop app after **Save draft** plus **Publish current draft**. **Save draft** alone does not affect the app; publishing is required, and existing active lessons may keep old content until a new lesson starts.

CMS-managed A1, A2, B1, and B2 level behavior profiles are active and affect lesson behavior. A1 and B2 behavior differs as expected; level polishing continues later based on tester feedback. Static JSON fallback remains available for rollback/safety, but fallback should not be active during normal runtime status.

Next phase is controlled tester handoff and feedback collection. Verify the installed tester build from the public site, run a short smoke test, prepare tester handoff, and collect feedback on lesson quality, level behavior, voice, UI, and CMS-controlled content. Do not touch billing/Paddle in this phase and do not start broad public release yet.
