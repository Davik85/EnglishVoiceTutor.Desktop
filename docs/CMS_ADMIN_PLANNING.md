# CMS / Admin Planning

Review date: 2026-06-06.

## Current product decision

CMS/Admin content MVP is now the next focus **before external tester handoff**. The desktop hardening block is stable enough to pause tester delivery and build a content editing foundation first, so future tester feedback about lessons, scenarios, prompts, and tutor behavior can be fixed through CMS instead of code or JSON changes.

This changes the previous "not built now" decision only for the **content-focused CMS MVP**. Full production Admin and production billing operations remain deferred.

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

Current Admin foundation supports development bootstrap admin status/capabilities, user lookup, manual Premium grant/revoke, free lesson allowance reset, audit-action visibility, and the development/admin-only `CMS Content` workspace. The Admin CMS workspace has sub-tabs for Overview, Topics, Scenarios, Prompts, Tutors, Validation & Preview, Versions & Publish, and Audit. It supports content pack overview, topic editing, scenario editing, structured scenario editing, advanced full scenario JSON editing, prompt template editing, tutor behavior profile editing, validation/preview summary, versions/publish/restore flows, and read-only recent audit review with smoke/test entries hidden by default. It is still not production Admin/CMS, not production RBAC, and not public-release readiness.

Admin CMS refresh resilience is in place: admin auth survives refresh via the existing admin-only HTTP-only cookie, the JWT remains memory-only in JavaScript, browser Web Storage is not used, the URL hash stores only safe workspace identifiers, and selected user/CMS entities are restored after session validation. Unsaved CMS changes are tracked in memory, show a visible dirty indicator, warn before refresh/navigation/entity switching/logout discards edits, and are not persisted in browser storage or the URL hash. `Save draft` remains explicit and required to persist CMS edits. It persists drafts only, never runtime-visible content; after save the UI tells the admin to publish the current draft and offers **Go to Publish**. Browser publishing requires a change summary for changed-content publishes and shows a clear local validation error when that summary is missing.


## Latest completed Admin CMS Content step

Step 5D-6e is complete for the development/admin Admin CMS Scenarios editor usability refinement. The Scenarios editor now has compact local **Jump to** navigation, collapsible and visually separated sections for Basic fields, Lesson setup, Context selection / choices, Conversation flow / response guidance, Wrap-up / summary guidance, and Advanced JSON, plus helper text for normal content editors. Structured scenario fields load from and update canonical `DefinitionJson` while preserving unknown JSON fields, and they remain the recommended normal editing path. Advanced JSON remains available as a visually separated technical fallback for rare full-JSON edits. `Format JSON` only pretty-prints, and `Validate JSON` only checks syntax and required scenario fields; neither action saves, publishes, or persists edits.

Publishing remains isolated in **Versions & Publish**. Draft changes are not visible to learner runtime until published. Published versions are immutable, and restore copies a previous published version into a new version instead of mutating old versions.

Local runtime CMS read was confirmed only under explicit development configuration: `CmsContent__ReadPublishedSnapshotEnabled=true`, `CmsContent__UsePublishedSnapshotForRuntime=true`, `CmsContent__ContentPackSlug=static-json-v1`, and `CmsContent__FallbackToStaticJson=true`; backend logs showed `Source=CmsPublishedSnapshot`, `VersionNumber=34`, validation passed, no fallback, and the expected 6 topics, 26 scenarios, 3 prompt templates, and 2 tutor behavior profiles. Static JSON fallback remains available.

This is still a development/admin CMS MVP, not production CMS/RBAC readiness, not critical-change approval readiness, not external tester handoff, and not public release readiness. The next recommended implementation step is another CMS/admin improvement, not billing: refine validation/preview and content QA workflow ergonomics for practical admin content review. Production RBAC and critical-change approval come later.

## CMS/Admin content MVP goal

Create a safe backend-owned CMS/Admin foundation for editing lesson and prompt content before external testers begin content QA.

The MVP must allow content edits without changing desktop runtime code or packaged lesson JSON after the migration is implemented and approved. It must protect users by serving only published content and falling back to current static JSON if CMS content is unavailable.

Detailed plan: `docs/cms-content-mvp-plan.md`.

## MVP scope

CMS content MVP should focus on:

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

## Explicit non-goals for the MVP

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

Native/Explanation languages remain broad. CMS MVP must not expand or narrow these catalogs.

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

1. Document CMS MVP scope and data model. This planning task.
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
12. Then send controlled tester package to external testers after CMS/Admin content MVP is ready enough for practical content changes without code edits.

## Risks

- Broken draft content could disrupt lessons if runtime reads drafts. Mitigation: runtime reads immutable published snapshots only.
- Prompt edits could weaken safety. Mitigation: keep backend safety rules code-owned and validate prompt templates.
- CMS migration could block testers. Mitigation: keep current static JSON fallback until CMS is proven.
- Scope creep could pull billing/Admin operations into CMS MVP. Mitigation: keep billing, Paddle, subscription, entitlement, and broad user management deferred.
- Secrets could be entered into prompt fields. Mitigation: validate and audit content fields; never store provider secrets in CMS.
- Admin refresh/session behavior could accidentally persist sensitive drafts. Mitigation: keep JWT memory-only, use only the existing admin-only HTTP-only cookie for refresh auth, store only safe URL hash identifiers, avoid Web Storage, and never persist unsaved CMS content in browser storage.

## Acceptance criteria for this planning step

- Documentation reflects CMS/Admin content MVP as the next focus before external tester handoff.
- Documentation clearly separates CMS content MVP from full production Admin.
- Production billing remains deferred.
- Public release remains not ready.
- Editable content and non-goals are defined.
- Draft/published, validation, preview, versioning, rollback, fallback, and security requirements are documented.
- No runtime code, backend code, desktop UI code, lesson JSON, EF migration, billing logic, Paddle logic, subscription logic, entitlement logic, or payment logic is changed by this planning step.


Implemented CMS draft-save audit logging details: successful Topic, Scenario (bounded fields, structured scenario fields, and full scenario JSON), Prompt Template, and Tutor Behavior Profile Save draft operations write `DraftSaved` rows to `cms_content_audit_logs`. Rows capture audit id, `createdAtUtc`, actor user id, actor email when available, content pack id and slug, entity type, entity id, stable key (`stableTopicKey`, `stableScenarioKey`, `templateKey`, or `tutorId`), changed field names, before/after SHA-256 hashes, source `AdminCms`, status, and request id when available. Audit rows intentionally do not store or display full before/after JSON snapshots, prompt/tutor source text snapshots, passwords, tokens, provider secrets, OpenAI API keys, Paddle API keys/webhook secrets, or admin bearer tokens. Large edited values are represented by hashes. No-op Save draft requests avoid noisy draft-save audit rows. Admins can read recent CMS audit entries through development/admin-only audit endpoints and the CMS Content Audit subtab, which is aligned to the selected content pack (`static-json-v1` by default) and supports entity type, stable key text, limit, and Refresh audit controls. Runtime learner behavior is unchanged: CMS read path remains disabled by default and static JSON fallback remains available. Production RBAC and critical-change approval remain future work.

Structured scenario editor update: the Admin CMS Scenarios subtab now includes a safer structured editor for common scenario content (title/subtopic, description, setup message, first bot message guidance, context option titles, valid context keywords, custom context rules, invalid context redirect, goal text, can-do statements, opening/first-user-task/follow-up guidance, AI tutor instructions, wrap-up/final message guidance, hint example, and wrap/final turn counters). `DefinitionJson` remains the canonical stored scenario definition; no per-field scenario database columns or EF migration were added. Structured edits parse the current `DefinitionJson`, update only known JSON paths, and write the merged valid JSON back to `DefinitionJson`, preserving unknown fields and advanced configuration in place. Advanced JSON remains available with `Format JSON` and `Validate JSON` for rare technical changes. Save draft remains explicit; invalid Advanced JSON or invalid structured numeric/required data is rejected before saving, and backend scenario validation still rejects invalid JSON, missing required fields, or accidental stable id/title/setup mismatches. CMS draft-save audit logging still records successful scenario saves with changed field names and before/after hashes without storing full scenario JSON bodies. Runtime learner behavior remains unchanged by default: the CMS read path is still disabled unless explicitly enabled, and static JSON fallback remains available.



## Save draft to publish discoverability

`Save draft` is intentionally draft-only: it persists CMS draft changes and audit entries, but it never publishes and never changes runtime learner behavior. The Admin CMS editor now displays “Draft saved. To apply this content to runtime, publish the current draft.” after successful draft saves and provides a **Go to Publish** action that switches to **Versions & Publish** while preserving the selected content pack, selected CMS entity, selected user, and URL hash state. **Versions & Publish** remains the source of truth for publication and keeps the explicit **Publish current draft** confirmation flow. Publishing changed content requires a short publish change summary; no-change publish checks may skip the summary. Publish failures now surface backend errors/warnings and validation details near the publish controls. Runtime CMS mode reads immutable published snapshots only; static JSON remains default and CMS runtime reads remain disabled by default.

## Runtime published snapshot integration guardrails

The CMS runtime read path is intentionally controlled and reversible. Static JSON remains default, and CMS runtime content is not enabled unless `CmsContent:UsePublishedSnapshotForRuntime=true` is set alongside `CmsContent:ReadPublishedSnapshotEnabled=true`. Runtime reads only immutable published snapshots for `CmsContent:ContentPackSlug` (`static-json-v1` by default), never draft content. Scenario `DefinitionJson` is carried through the published snapshot so structured scenario fields and unknown advanced JSON fields remain available to runtime logic. If the selected snapshot is missing or invalid, fallback to static JSON occurs only when `CmsContent:FallbackToStaticJson=true`; otherwise diagnostics report a server-side content error rather than silently serving broken CMS content. Logs and diagnostics expose source, slug, version, snapshot hash, counts, fallback state, validation status, and bounded errors/warnings only.
