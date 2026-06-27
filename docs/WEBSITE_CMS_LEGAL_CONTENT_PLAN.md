# Website CMS Legal/Support/Pricing Content Plan

Review date: 2026-06-27.

## Purpose

Define a small, safe Admin Website CMS feature foundation and future workflow for managing public website legal, seller, support, policy, and pricing display content without code changes after implementation. The first backend foundation slice, production database rollout, Admin shell skeleton, admin-only section detail/save-draft plus validation/preview/review-status rollout, deployment, scripts, and tests are now complete. This still does not change public website rendering and does not provide final legal advice.

All legal/policy copy managed through this future CMS must be treated as owner/legal review draft content until approved by the product owner and qualified legal reviewer.

## Non-goals

This plan must not be used to:

- change `site/public/` HTML, CSS, JavaScript, assets, or routes in this task;
- deploy the public site;
- enable production/live Paddle;
- change billing, subscriptions, entitlements, Desktop behavior, public website runtime behavior, deployment scripts, or production configuration in this task;
- store or operate Paddle configuration, checkout wiring, webhooks, payment actions, refunds, chargebacks, tax settings, or customer billing records;
- replace legal review or publish final legal advice;
- let public website draft content affect users before an explicit future rendering implementation is approved.

## Current static-site status

The public website is currently maintained as static files under `site/public/`. The confirmed production public website root is `/var/www/languagevoicetutor/site`; do not upload public website files to the parent `/var/www/languagevoicetutor/`. The prior accidental parent-directory upload was moved into `/var/www/languagevoicetutor/_mistaken_static_upload_20260625`.

Temporary Paddle review-readiness pages already exist and were deployed as static pages:

- `pricing.html`
- `terms.html`
- `privacy.html`
- `refunds.html`
- `cancellation.html`
- `support.html`

Those files are suitable as temporary static review-readiness shells, but future updates to legal/support/pricing copy should be planned for a controlled Admin CMS workflow after separate legal and owner review.

The existing Admin shell has a `CMS Content` workspace for learner/runtime lesson, prompt, scenario, topic, tutor, validation, version, publish, and audit workflows. Website policy content must stay separate from learner/runtime content packs. The Website area is now a **top-level Admin Shell tab** in the left navigation, not a `CMS Content` sub-tab.

## Target Admin CMS tab

Preferred tab name: **Website**.

Acceptable alternate tab name: **Public Site**.

The tab should be clearly labeled as public website content management and should show a warning that legal and policy content remains draft/review content until explicitly approved and published.

Current tab behavior is admin-only draft management for initialized Website CMS sections. It can load metadata and section detail, save drafts, validate stored drafts, show admin-only simple-text previews, and move safe internal review statuses, but it does not publish content, change `site/public/`, change public rendering, or enable Paddle.

## Validation/preview/review slice

The added validation/preview/review slice is intentionally admin-only. Validation checks the current stored `DraftBody` with `WebsiteCmsContentGuard` and reports errors/warnings without database writes. Preview returns simple escaped/admin-only draft text with metadata and empty-draft warnings; it is not public rendering. Review status updates are limited to `not_started`, `draft`, `owner_review_needed`, `legal_review_needed`, `owner_approved`, and `legal_approved`, require `ChangeReason`, and never update `PublishedBody` or `PublishedAtUtc`. Publish and public rendering remain deferred design topics only.

## Editable sections

Keep the first version small and structured. Suggested editable sections:

1. **Seller / Company information**
   - Legal seller name placeholder or approved value.
   - Public business address or owner-approved contact address, if approved.
   - Business registration/tax text only if owner/legal approved.
   - Public footer/company display wording.
2. **Support contact**
   - Support email.
   - Optional phone/support channel wording only if owner-approved.
   - Support categories: billing, account, download/install, technical, privacy/data deletion.
   - Response-time wording only if operationally approved.
3. **Pricing and subscription display copy**
   - Human-readable price/billing-period copy.
   - Free/trial/Premium explanation.
   - Renewal and cancellation summary copy.
   - Included Premium feature display copy.
   - No Paddle price IDs, checkout links, client-side tokens, or live-payment enablement.
4. **Terms of Use**
   - Owner/legal review draft body.
   - Effective date.
   - Review status and reviewer notes.
5. **Privacy Policy**
   - Owner/legal review draft body.
   - Account, lesson, voice/audio, AI provider, payment processor, support, retention/deletion, and minors/children wording.
   - Effective date and review status.
6. **Refund Policy**
   - Refund request process.
   - Eligibility/window wording only after owner/legal approval.
   - Support escalation path.
7. **Cancellation Policy**
   - How customers cancel.
   - Renewal cancellation and access-through-current-period wording if this remains the product decision.
   - Support escalation path.
8. **AI/data/privacy disclosures**
   - High-level explanation that lessons may be processed by backend and AI providers.
   - Voice/audio processing summary where applicable.
   - Data retention/deletion contact path.
   - Avoid unverifiable claims such as “no data is stored” or “100% private.”
9. **Platform availability wording**
   - Windows desktop availability wording.
   - Android/iOS wording must remain “planned” or “in development” until actually released.
   - Avoid paid-production availability claims until live billing and operations are approved.

## Draft/published workflow

The website CMS should follow a conservative content lifecycle:

1. Admin edits a draft section.
2. Admin saves draft with a required change reason.
3. Draft content is not public and does not affect `site/public/` rendering.
4. Validation runs before publish.
5. Publish requires an explicit change summary.
6. Publishing creates an immutable published website-content version.
7. Public rendering changes only after a separately approved rendering integration reads the published version.
8. Previous published versions can be viewed and restored by copying them into a new draft/version, not by mutating old versions.

## Owner/legal review status fields

Each legal/support/pricing section should carry review metadata:

- `draft`, `owner_review_needed`, `owner_approved`, `legal_review_needed`, `legal_approved`, `published`, or `retired` status;
- reviewer name or internal user id where appropriate;
- reviewed timestamp in UTC;
- effective date shown publicly, when applicable;
- next review date, optional;
- owner/legal notes that are internal-only and never rendered publicly;
- publish change summary.

Legal sections should not be publishable as “final” unless the configured owner/legal approval fields are satisfied.

## Audit requirements

Audit every meaningful website CMS action:

- draft created/updated;
- review status changed;
- validation run;
- publish attempted/succeeded/failed;
- previous version restored into a new draft;
- section retired/reactivated.

Audit records should include actor, timestamp, section key, changed fields, old/new hashes or bounded summaries, source, request/correlation id, validation result, and publish version. Do not store full sensitive payloads in audit rows when hashes or bounded summaries are enough.

## Validation rules

Validate content before save and publish:

- required fields are present for each section;
- public support email is syntactically valid;
- required legal pages have non-empty body copy before publish;
- effective dates are valid dates and not accidentally missing;
- pricing copy does not contain Paddle IDs or checkout secrets;
- platform wording does not claim mobile availability before release;
- no raw provider payloads, signatures, connection strings, API keys, JWT keys, or webhook secrets appear in any public copy;
- no customer IDs, transaction IDs, subscription IDs, or private support-case identifiers appear inside public legal copy;
- content length is bounded;
- public HTML/Markdown output is sanitized and link targets are allowlisted where practical;
- draft content must not be returned by public unauthenticated endpoints.

## Secret and identifier guardrails

The Website/Public Site CMS must not store or display the following in public legal/support/pricing copy:

- Paddle secrets;
- Paddle webhook secrets;
- Paddle API keys;
- Paddle client-side tokens;
- Paddle price IDs unless a future owner-approved display requirement explicitly allows non-secret public price references;
- OpenAI keys or other provider API keys;
- JWT keys or bearer tokens;
- database connection strings;
- raw provider webhook/API payloads;
- signatures;
- customer IDs;
- transaction IDs;
- subscription IDs;
- private account, billing, support-case, or diagnostic identifiers.

If any such value is detected, validation should block save/publish and require manual cleanup.

## Public rendering options

Future implementation can choose one of these safe rendering paths:

1. **Static export after publish**: Admin publishes approved content, then a controlled build/export step writes static pages. This keeps the public site static and easy to review.
2. **Backend-rendered public pages**: Public routes read only the latest published website-content snapshot. Drafts are never served.
3. **Static site reads published JSON**: Public static pages fetch a published, cacheable, unauthenticated JSON snapshot. This is simple but requires careful cache, sanitization, and availability design.

The safest early path is static export or read-only admin skeleton first, because it avoids changing public runtime behavior while data structures and approval workflow are reviewed.

## Safest first implementation slice

Recommended first implementation slice after this documentation:

**Add backend data model and read-only Admin CMS Website tab skeleton without changing the public site rendering.**

That slice should only introduce safe storage/read views and admin navigation placeholders. It should not edit `site/public/`, should not expose unauthenticated public rendering, should not enable Paddle live mode, and should not alter billing, entitlement, Desktop, deployment, or production environment behavior.

## Current status after 2026-06-27 Website CMS draft-save rollout

- Backend release `0.1.35-backend.55` is deployed. The production backend symlink points to `/opt/languagevoicetutor/backend/releases/0.1.35-backend.55`.
- Post-deploy health checks returned `200 Healthy` for `https://api.languagevoicetutor.com/health` and `https://api.languagevoicetutor.com/api/health/database`.
- Website remains a top-level Admin Shell tab, separate from the `CMS Content` sub-tabs. `CMS Content` remains focused on learner/runtime content packs.
- The Admin Website tab supports admin-only metadata overview, section selection/detail loading, `DraftBody`, `InternalNotes`, `EffectiveDate`, `ReviewStatus`, required `ChangeReason`, and **Save draft**.
- Save draft is admin-only, runs `WebsiteCmsContentGuard` before persistence to block obvious secret-like/private/provider identifiers, and stores draft metadata only.
- Saving a draft does not publish content, does not update public website rendering, and does not modify `site/public/`.
- First backend foundation exists: the `website_cms_sections` table stores section key, draft body, optional published body, review status, effective date, internal notes, change reason, and updated/published timestamps.
- Production DB migration `20260625090000_AddWebsiteCmsLegalContentFoundation` has been applied manually from reviewed SQL. Production `__EFMigrationsHistory` contains that migration, and the `website_cms_sections` table plus `IX_website_cms_sections_ReviewStatus`, `IX_website_cms_sections_SectionKey`, and `PK_website_cms_sections` exist.
- Rollout note: the production table grant for `website_cms_sections` was manually corrected for runtime role `lvt_app`; future manual SQL rollouts must verify runtime DB grants after creating tables.
- Production contains the 9 expected Website CMS rows: `seller_company`, `support`, `pricing`, `terms`, `privacy`, `refunds`, `cancellation`, `ai_data_disclosures`, and `platform_status`.
- Production smoke test on `platform_status` passed: a temporary draft was saved, `ReviewStatus` became `draft`, metadata showed `Draft exists = Yes`, the draft was cleared, and `ReviewStatus` was restored to `not_started`.
- Production DB verification after cleanup confirmed 9 rows in `website_cms_sections`, `ReviewStatus=not_started` for all rows, `DraftBody` length `0` for all rows, and `PublishedBody` null / `has_published=false` for all rows.
- Fresh service logs after smoke test showed no new `website-cms` errors, permission-denied errors, exceptions, failures, or `500` responses.
- Public rendering is still not connected. The deployed static public website remains the actual public rendering source, and `site/public/` remains the source for static public pages.
- Publish workflow is not implemented.
- Live Paddle is still not enabled. No checkout buttons, checkout links, Paddle client tokens, live price IDs, webhook secrets, or public payment behavior were added.
- Legal, seller, support, refund, cancellation, privacy, terms, and pricing final values still require owner/legal approval before paid public launch claims.

## Scripts and tests added for this rollout

- `scripts/generate-backend-website-cms-migration-sql.ps1` generates reviewable SQL from `20260620165657_AddAdminRoleAssignmentPersistence` to `20260625090000_AddWebsiteCmsLegalContentFoundation` at `artifacts/sql/backend/20260625090000_AddWebsiteCmsLegalContentFoundation.from-20260620165657.sql`. It uses `dotnet ef migrations script` only; it does not apply SQL and does not read or print database secrets.
- `scripts/package-backend-linux-release.ps1` and `scripts/upload-backend-linux-release.ps1` are the accepted backend package/upload scripts used for release `0.1.35-backend.55`. They do not run EF migrations or apply SQL; schema rollout remains a separate reviewed operator step.
- Coverage includes `tools/test_admin_website_cms_skeleton_policy.py`, `tools/test_backend_linux_deployment_policy.py`, `tools/test_static_site_paddle_review_pages.py`, `tools/test_documentation_source_of_truth_policy.py`, and backend API tests for `WebsiteCmsContentGuard` plus `WebsiteCmsSafetyTests`.

## Next safe steps

1. Prepare owner/legal-approved public legal, seller, support, refund, cancellation, privacy, terms, and pricing copy outside code; final legal/seller/support/pricing copy still requires owner/legal approval.
2. Next functional slice: Website CMS validation/preview/review workflow for admin-only drafts, including clear non-public preview semantics and owner/legal review states.
3. Keep `WebsiteCmsContentGuard` validation on save and extend validation where needed for preview/review; secret-like values and private identifiers must remain blocked before persistence or review.
4. Keep requiring a change reason for every save.
5. Defer publish and public rendering. The public website must continue rendering from static `site/public/` until a separately approved published-only rendering integration is designed, reviewed, and tested.
6. Keep live Paddle as a separate readiness step; do not add checkout links/buttons or live payment behavior as part of Website CMS validation/preview/review.

## Risks and guardrails

| Risk | Guardrail |
| --- | --- |
| Draft legal copy accidentally becomes public. | Public rendering must read published versions only; draft endpoints remain admin-only. |
| CMS is mistaken for legal approval. | UI must label content as owner/legal review draft until approved. |
| Secrets or identifiers are pasted into public copy. | Save/publish validation blocks secret-like values and private identifiers. |
| Pricing text diverges from actual billing configuration. | Pricing display copy requires owner review and should be reconciled against billing configuration before public launch. |
| Paddle review pages imply live payments are ready too early. | Avoid live-payment claims until live Paddle, operations, support, and legal review are approved. |
| Mobile availability is overstated. | Validate platform wording and keep mobile marked planned/in development until released. |
| Audit logs capture too much sensitive content. | Store changed field names and hashes/bounded summaries rather than full sensitive bodies when practical. |
| CMS scope expands into billing operations. | Keep payment operations, refunds, chargebacks, secrets, webhook handling, and entitlements outside Website CMS. |

## Current-slice confirmation

This plan now records a backend foundation slice, admin-only missing-section initialization, and admin-only section detail/save-draft support. It does not change public rendering, public static pages, billing behavior, entitlement behavior, Desktop behavior, deployment scripts, production configuration, backend environment variables, or Paddle configuration. The added migration is scoped to Website CMS section storage only and does not alter existing lesson/content/runtime behavior.

## Draft detail/save slice added 2026-06-26

The completed admin-only slice adds initialized-section detail reads and draft-body saves for Admin Website CMS rows. Admins can load a section detail and save `DraftBody`, `ReviewStatus`, `EffectiveDate`, `InternalNotes`, and required `ChangeReason`; saves run the Website CMS content guard before persistence. This remains draft storage only: no publish workflow, no public website rendering, no static `site/public` changes, no live Paddle enablement, and no legal approval is implied.

Publish, public preview/rendering, owner/legal review workflow, and final legal/seller approval remain deferred.
