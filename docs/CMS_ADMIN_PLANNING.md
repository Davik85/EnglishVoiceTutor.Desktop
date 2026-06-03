# CMS / Admin Planning

Review date: 2026-06-03.

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

Current Admin foundation supports development bootstrap admin status/capabilities, user lookup, manual Premium grant/revoke, free lesson allowance reset, and audit-action visibility. It does **not** support content editing, draft/published workflow, preview, content validation, versioning, or rollback.

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
9. Audit trail records who changed what and when.

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
7. Add validation and preview endpoints/UI.
8. Add publish/version/rollback and audit trail.
9. Run desktop regression and release gate.
10. Then send controlled tester package to external testers.

## Risks

- Broken draft content could disrupt lessons if runtime reads drafts. Mitigation: runtime reads immutable published snapshots only.
- Prompt edits could weaken safety. Mitigation: keep backend safety rules code-owned and validate prompt templates.
- CMS migration could block testers. Mitigation: keep current static JSON fallback until CMS is proven.
- Scope creep could pull billing/Admin operations into CMS MVP. Mitigation: keep billing, Paddle, subscription, entitlement, and broad user management deferred.
- Secrets could be entered into prompt fields. Mitigation: validate and audit content fields; never store provider secrets in CMS.

## Acceptance criteria for this planning step

- Documentation reflects CMS/Admin content MVP as the next focus before external tester handoff.
- Documentation clearly separates CMS content MVP from full production Admin.
- Production billing remains deferred.
- Public release remains not ready.
- Editable content and non-goals are defined.
- Draft/published, validation, preview, versioning, rollback, fallback, and security requirements are documented.
- No runtime code, backend code, desktop UI code, lesson JSON, EF migration, billing logic, Paddle logic, subscription logic, entitlement logic, or payment logic is changed by this planning step.
