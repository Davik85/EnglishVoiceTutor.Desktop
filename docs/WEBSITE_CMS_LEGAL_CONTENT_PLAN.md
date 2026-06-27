# Website CMS Legal/Support/Pricing Content Plan

Review date: 2026-06-27.

## Purpose

Define a small, safe Admin Website CMS feature foundation and future workflow for managing public website legal, seller, support, policy, and pricing display content without code changes after implementation. The backend foundation slice, production database rollout, Admin shell skeleton, admin-only section detail/save-draft, validation/preview/review-status, and admin-only publish rollout are complete. This still does not change public website rendering and does not provide final legal advice.

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

Current tab behavior is admin-only management for initialized Website CMS sections. It can load metadata and section detail, initialize missing sections, save drafts, validate stored drafts, show admin-only simple-text previews, move safe internal review statuses, and explicitly publish an approved draft into internal Website CMS `PublishedBody` storage. It does not change `site/public/`, change public rendering, enable live Paddle, or add checkout links/buttons.

## Validation/preview/review slice

The added validation/preview/review slice is intentionally admin-only. Validation checks the current stored `DraftBody` with `WebsiteCmsContentGuard` and reports errors/warnings without database writes. Preview returns simple escaped/admin-only draft text with metadata and empty-draft warnings; it is not public rendering. Review status updates are limited to `not_started`, `draft`, `owner_review_needed`, `legal_review_needed`, `owner_approved`, and `legal_approved`, require `ChangeReason`, and never update `PublishedBody` or `PublishedAtUtc`. The admin-only publish workflow is now implemented for internal Website CMS storage only; public rendering remains deferred.

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

## Admin-only publish workflow

The admin-only Website CMS publish workflow without public rendering is implemented. Publishing creates or updates a Website CMS published snapshot only; it does not edit `site/public/`, deploy the public site, add checkout links/buttons, enable live Paddle, or connect Website CMS data to unauthenticated public rendering. The next functional Website CMS step should be publish rollback/unpublish design, or owner/legal copy preparation outside code.

Required publish gates:

1. **Allowed source state**: publish may use only the current stored `DraftBody` for one initialized Website CMS section. Empty drafts must not be publishable for required public pages. Internal notes, validation diagnostics, raw provider data, and audit payloads are never publish source content.
2. **Validation result**: `WebsiteCmsContentGuard` must run immediately before publish against the exact draft being published. Publish must be blocked if validation fails or if secret-like/provider/private identifiers are detected.
3. **Review status**: publish is allowed only from a configured approved review state. For legal/policy pages, the required state should be `legal_approved`; for non-legal operational copy, the required state may be `owner_approved` when legal review is not required by policy. `owner_approved` and `legal_approved` are internal review markers only and never publish by themselves.
4. **Change reason**: publish requires a non-empty admin change reason/publish summary distinct enough to explain what changed and why.
5. **Owner/legal approval marker**: publish must record which approval marker authorized the publish, the approving actor or internal reviewer reference where available, and the UTC approval/publish timestamp. Final legal, seller, support, refund, cancellation, privacy, terms, and pricing copy still requires owner/legal approval outside code before publish.

Implementation note (2026-06-27): `POST /api/admin/website-cms/sections/{sectionKey}/publish` is admin-only, requires a non-empty `DraftBody`, non-empty `ChangeReason`, `WebsiteCmsContentGuard`, and `legal_approved`, and stores the snapshot only in internal Website CMS `PublishedBody`. It does not update public rendering, modify `site/public/`, or enable live Paddle.

Publish effects:

- Publish copies the validated `DraftBody` into `PublishedBody` for that section and sets `PublishedAtUtc` to the publish time in UTC.
- Publish must not transform the draft in a way that changes legal meaning. Any sanitization or formatting used for public display should be deterministic and separately tested.
- Publish may update publish metadata such as publish actor, publish change summary, approval marker, published hash/version, and review status if a future `published` state is added. It must not erase `DraftBody` automatically and must not expose `InternalNotes`.
- Publish must be audited as attempted, succeeded, or failed with actor, timestamp, section key, prior/new published hashes or bounded summaries, validation result, approval marker, change reason, source, request/correlation id, and published version/snapshot id. Audit rows should avoid full sensitive payloads when hashes or bounded summaries are enough.
- All publish actions, rollback actions, approval-marker changes, and review-status changes must remain admin-only.

## Rollback and unpublish design

Simple admin-only unpublish is implemented for current `website_cms_sections` rows. It clears only internal `PublishedBody` / `PublishedAtUtc`, requires `ChangeReason`, runs `WebsiteCmsContentGuard` on the reason, leaves `DraftBody` unchanged, and does not change public rendering or `site/public/`. Revision-history rollback is still deferred; the production smoke test for the admin-only publish rollout previously required manual SQL cleanup before this unpublish foundation existed.

### Definitions

- **Rollback** means restoring a previous `PublishedBody` / `PublishedAtUtc` from a prior published revision or history entry. A safe rollback should either copy the prior published body into a new draft for review or create a new published snapshot from that prior revision through the same validation, approval, change-reason, and audit gates. Rollback must not mutate historical published/audit records in place.
- **Unpublish** means clearing the current `PublishedBody` / `PublishedAtUtc` or marking the section unpublished so no current published website content is available for that section. Unpublish is not the same as restoring earlier content.

### Current storage limitation

`website_cms_sections` currently has only one `PublishedBody` value per section and does not store published revision history. True rollback to previous published content may require a future revision/history table or an audit snapshot that preserves prior published content safely. Without that history, the system can only unpublish current content or manually restore content from an external approved copy; it cannot reliably reconstruct an earlier published revision.

### Future implementation options

1. **Option A: simple unpublish only.** Add an admin-only operation that clears `PublishedBody` / `PublishedAtUtc` or otherwise removes the current published state, with no previous revision restoration. This is the smallest safety foundation but does not solve accidental overwrite recovery.
2. **Option B: published revision history.** Add Website CMS published revision/history storage with immutable published snapshots, actor, timestamp, hash/version, approval marker, and change reason. Rollback can then restore a selected prior revision through a new audited publish action.
3. **Option C: soft-unpublish status.** Add a published/unpublished flag or status while preserving `PublishedBody`. This supports hiding content without losing the last published body, but public renderers must honor the status and never serve unpublished sections.

Recommended path: for legal/policy content, implement published revision history before connecting Website CMS to public rendering. This gives operators a safe recovery path for approved terms, privacy, refund, cancellation, seller, support, and pricing copy before unauthenticated users can see CMS-managed content.

### Safety requirements

- Rollback and unpublish must be admin-only and protected by a high-permission Admin policy.
- Every rollback or unpublish request must require a non-empty `ChangeReason`.
- Rollback must re-run validation before publishing restored content and must audit the source revision/snapshot id where history exists.
- Unpublish must audit the previous published hash or bounded summary before clearing or hiding content.
- Neither workflow may expose `DraftBody` publicly.
- Neither workflow may expose `InternalNotes` publicly.
- Neither workflow may modify `site/public/`.
- Neither workflow may enable live Paddle.
- Neither workflow may add checkout links or checkout buttons.

### Public rendering boundary

Public rendering integration must not happen until rollback/unpublish rules are implemented or the missing rollback/unpublish risk is explicitly accepted in a separate reviewed decision. If a public renderer is later added, it must read only current published content or a safe published snapshot, must honor any unpublished status, and must never read draft content or internal notes.

## Review status rules

Website CMS status values should have narrow meanings:

| Status | Meaning | Publish effect |
| --- | --- | --- |
| `not_started` | Section exists but no usable draft has been prepared. | Not publishable. |
| `draft` | Admin draft is being edited internally. | Not publishable. |
| `owner_review_needed` | Draft is ready for product-owner review. | Not publishable. |
| `legal_review_needed` | Draft is ready for qualified legal review. | Not publishable. |
| `owner_approved` | Product owner has approved the current draft as an internal marker. | Does not publish by itself; may be an allowed publish gate only for sections that do not require legal approval. |
| `legal_approved` | Legal reviewer has approved the current draft as an internal marker. | Does not publish by itself; should be the normal allowed publish gate for legal/policy/pricing/seller copy. |
| `published` | Optional future state meaning the current draft/published snapshot has completed the publish action. | May be set only by the publish workflow; not by review-status-only endpoints. |

Review-status-only changes must never update `PublishedBody` or `PublishedAtUtc`. Approval statuses are not public-facing legal advice and do not imply live Paddle readiness, billing readiness, or public rendering integration.

## Public rendering integration boundaries

Public rendering must remain separate from publish:

- The public site must never read or serve `DraftBody`.
- Public rendering may only read a published snapshot/body after a separately approved implementation is designed, reviewed, tested, and deployed.
- Public rendering integration is a later separate task after the admin-only publish workflow exists.
- Static `site/public/` files remain the production public rendering source until that later published-only integration is approved and completed.
- Admin preview remains admin-only and must not be treated as public rendering.

## Paddle/legal readiness boundaries

- Live Paddle enablement remains separate from Website CMS publish.
- The Website CMS publish workflow must not introduce checkout links, checkout buttons, Paddle client-side tokens, live price IDs, webhook secrets, billing behavior changes, entitlement behavior changes, or payment operations.
- Final legal, seller, support, pricing, refund, cancellation, privacy, and terms copy requires owner/legal approval outside code before it is published.

## Audit requirements

Audit every meaningful website CMS action:

- draft created/updated;
- review status changed;
- validation run;
- publish attempted/succeeded/failed;
- previous version restored into a new draft;
- rollback attempted/succeeded/failed;
- section retired/reactivated.

Audit records should include actor, timestamp, section key, changed fields, old/new hashes or bounded summaries, source, request/correlation id, validation result, approval marker, change reason, and publish version. Do not store full sensitive payloads in audit rows when hashes or bounded summaries are enough.

## Safety and validation requirements

Validate content before save and publish:

- `WebsiteCmsContentGuard` must run before publish against the exact `DraftBody` that would be copied to `PublishedBody`;
- required fields are present for each section;
- public support email is syntactically valid;
- required legal pages have non-empty body copy before publish;
- effective dates are valid dates and not accidentally missing;
- pricing copy does not contain Paddle IDs or checkout secrets;
- platform wording does not claim mobile availability before release;
- secret-like, provider, or private identifiers block publish;
- publish must not expose internal notes;
- publish must not expose raw provider payloads, customer IDs, transaction IDs, subscription IDs, API keys, JWT keys, Paddle secrets, webhook secrets, connection strings, bearer tokens, signatures, private support-case identifiers, or diagnostic identifiers;
- content length is bounded;
- public HTML/Markdown output is sanitized and link targets are allowlisted where practical;
- draft content must not be returned by public unauthenticated endpoints.

If any blocked value is detected, validation should block save/publish and require manual cleanup.

## Future implementation testing requirements

Publish tests should prove that:

- publish requires a passing validation result from `WebsiteCmsContentGuard`;
- publish requires an allowed review state;
- publish requires a non-empty change reason/publish summary;
- publish copies `DraftBody` to `PublishedBody` and sets `PublishedAtUtc` without modifying `site/public/`;
- review-status-only approval changes do not update `PublishedBody` or `PublishedAtUtc`;
- a public endpoint, if later added, returns published content only.

Rollback/unpublish tests should prove that:

- unpublish clears or marks published content safely;
- rollback restores a previous published revision if revision/history storage exists;
- rollback and unpublish require `ChangeReason`;
- rollback and unpublish reject unknown sections;
- rollback and unpublish do not change `DraftBody`;
- rollback and unpublish do not alter `site/public/`;
- there are no public unauthenticated rollback or unpublish routes;
- public rendering never reads draft content;
- draft content never appears in public rendering, public endpoints, static exports, logs, audit summaries, or unauthenticated responses.

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

## Current status after 2026-06-27 Website CMS admin-only publish rollout

- Backend release `0.1.35-backend.57` is deployed. The production backend symlink points to `/opt/languagevoicetutor/backend/releases/0.1.35-backend.57`.
- Post-deploy health checks returned `200 Healthy` for `https://api.languagevoicetutor.com/health` and `https://api.languagevoicetutor.com/api/health/database`.
- Website remains a top-level Admin Shell tab, separate from the `CMS Content` sub-tabs. `CMS Content` remains focused on learner/runtime content packs.
- The Admin Website tab supports admin-only metadata overview, missing-section initialization, section detail, draft-body save, stored-draft validation, admin-only simple-text draft preview, review-status changes with required `ChangeReason`, and explicit admin-only publish to Website CMS `PublishedBody`.
- Publish requires a non-empty `DraftBody`, non-empty `ChangeReason`, `legal_approved` status, and `WebsiteCmsContentGuard`; it copies `DraftBody` to `PublishedBody`, sets `PublishedAtUtc`, and updates metadata.
- Publish is internal-only. It does not update public website rendering, does not modify `site/public/`, does not enable live Paddle, and does not add checkout links/buttons.
- First backend foundation exists: the `website_cms_sections` table stores section key, draft body, optional published body, review status, effective date, internal notes, change reason, and updated/published timestamps.
- Production DB migration `20260625090000_AddWebsiteCmsLegalContentFoundation` has been applied manually from reviewed SQL. Production `__EFMigrationsHistory` contains that migration, and the `website_cms_sections` table plus `IX_website_cms_sections_ReviewStatus`, `IX_website_cms_sections_SectionKey`, and `PK_website_cms_sections` exist.
- Rollout note: the production table grant for `website_cms_sections` was manually corrected for runtime role `lvt_app`; future manual SQL rollouts must verify runtime DB grants after creating tables.
- Production contains the 9 expected Website CMS rows: `seller_company`, `support`, `pricing`, `terms`, `privacy`, `refunds`, `cancellation`, `ai_data_disclosures`, and `platform_status`.
- Production smoke test on `platform_status` passed: a temporary draft was saved, review status was set to `legal_approved`, validation and admin-only preview worked, publish copied `DraftBody` into `PublishedBody`, and `PublishedAtUtc` was set. Public rendering did not change.
- Smoke data was manually cleaned up before the simple admin-only unpublish foundation existed.
- Production DB verification after cleanup confirmed 9 rows in `website_cms_sections`, `ReviewStatus=not_started` for all rows, `DraftBody` length `0` for all rows, and `PublishedBody` null / `has_published=false` for all rows.
- Fresh service logs after smoke test showed no new `website-cms` errors, permission-denied errors, exceptions, failures, or `500` responses.
- Public rendering is still not connected. The deployed static public website remains the actual public rendering source, and `site/public/` remains the source for static public pages.
- Simple admin-only unpublish is implemented for internal `PublishedBody` only; revision-history rollback is not implemented.
- Live Paddle is still not enabled. No checkout buttons, checkout links, Paddle client tokens, live price IDs, webhook secrets, or public payment behavior were added.
- Legal, seller, support, refund, cancellation, privacy, terms, and pricing final values still require owner/legal approval before paid public launch claims.

## Scripts and tests added for this rollout

- `scripts/generate-backend-website-cms-migration-sql.ps1` generates reviewable SQL from `20260620165657_AddAdminRoleAssignmentPersistence` to `20260625090000_AddWebsiteCmsLegalContentFoundation` at `artifacts/sql/backend/20260625090000_AddWebsiteCmsLegalContentFoundation.from-20260620165657.sql`. It uses `dotnet ef migrations script` only; it does not apply SQL and does not read or print database secrets.
- `scripts/package-backend-linux-release.ps1` and `scripts/upload-backend-linux-release.ps1` are the accepted backend package/upload scripts used for release `0.1.35-backend.57`. They do not run EF migrations or apply SQL; schema rollout remains a separate reviewed operator step.
- Coverage includes `tools/test_admin_website_cms_skeleton_policy.py`, `tools/test_backend_linux_deployment_policy.py`, `tools/test_static_site_paddle_review_pages.py`, `tools/test_documentation_source_of_truth_policy.py`, and backend API tests for `WebsiteCmsContentGuard` plus `WebsiteCmsSafetyTests`.

## Next safe steps

1. Prepare owner/legal-approved public legal, seller, support, refund, cancellation, privacy, terms, and pricing copy outside code; final legal/seller/support/pricing copy still requires owner/legal approval.
2. Next functional Website CMS step should be publish rollback/unpublish design, or owner/legal copy preparation outside code. Do not connect Website CMS to public routes or static site output.
3. Keep `WebsiteCmsContentGuard` and draft validation conservative; secret-like values and private identifiers must remain blocked before persistence or review.
4. Keep requiring a change reason for every save or internal review-status change.
5. Defer public rendering. The public website must continue rendering from static `site/public/` until publish/public snapshot rules are approved and a separately approved published-only rendering integration is designed, reviewed, and tested.
6. Keep live Paddle as a separate readiness step; do not add checkout links/buttons or live payment behavior as part of Website CMS work.

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

This plan now records a backend foundation slice, admin-only missing-section initialization, section detail/save-draft support, stored-draft validation, admin-only preview, internal review-status updates, and explicit admin-only publish into internal PublishedBody storage. It does not change public rendering, public static pages, billing behavior, entitlement behavior, Desktop behavior, deployment scripts, production configuration, backend environment variables, or Paddle configuration. The added migration is scoped to Website CMS section storage only and does not alter existing lesson/content/runtime behavior.

## Draft validation/preview/review slice added 2026-06-27

The completed admin-only slice adds initialized-section detail reads, draft-body saves, stored-draft validation, admin-only safe-text preview, and internal review-status changes for Admin Website CMS rows. Admins can load section detail; save `DraftBody`, `ReviewStatus`, `EffectiveDate`, `InternalNotes`, and required `ChangeReason`; validate the stored draft without database writes; preview the draft without publishing; and move internal review statuses with a required reason. This remains internal workflow only: admin-only internal publish to `PublishedBody`, no public website rendering, no static `site/public` changes, no live Paddle enablement, and no legal approval is implied.

Publish rollback/unpublish design, public preview/rendering, final owner/legal copy approval, and live Paddle readiness remain deferred.
