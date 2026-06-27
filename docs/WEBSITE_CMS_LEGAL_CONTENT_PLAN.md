# Website CMS Legal/Support/Pricing Content Plan

Review date: 2026-06-27.

## Purpose

Website CMS is now simplified into practical website text management for release readiness. The visible Admin product UI is **not** a legal workflow engine: it lets the main admin manually manage website text drafts for Legal pages, Home page, Desktop page, and Mobile page / Coming soon without editing code.

Saved website text is stored in the existing Website CMS storage. Public website rendering remains a separate future implementation step, and saved CMS text is not served publicly by this task.

## Non-goals

This plan must not be used to:

- change `site/public/` HTML, CSS, JavaScript, assets, or routes in this task;
- deploy the public site;
- enable production/live Paddle;
- add checkout buttons or checkout links;
- change billing, subscriptions, entitlements, Desktop behavior, public website runtime behavior, deployment scripts, production configuration, backend environment variables, or secrets;
- apply database migrations;
- create or expose public unauthenticated Website CMS endpoints;
- recreate the complex visible validation, preview, review-status, publish, unpublish, rollback, or revision-history workflow in the main UI.

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

The Website tab is a top-level Admin Shell tab. The visible product UI is intentionally simple:

- section groups for **Legal pages**, **Home page**, **Desktop page**, and **Mobile page / Coming soon**;
- editable website text for the selected section;
- one visible **Save** button;
- a simple **Change note** field with placeholder “What changed?”;
- clear wording: “Website CMS now manages active public website text; static site text remains the fallback.”

The visible Admin Website tab must not show a complex legal workflow engine. Validation, preview, review-status controls, internal legal/owner approval controls, publish/unpublish controls, rollback, and revision-history workflows are backend/internal or future concerns and should not be visible in the simplified main UI.

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

## Storage and public rendering boundary

The simplified Website tab should continue reusing the existing `website_cms_sections` table and draft-save behavior. Expected section keys may be maintained in code without a new migration, including `home`, `desktop`, `mobile`, and legal-prefixed keys such as `legal_terms`, `legal_privacy`, `legal_refunds`, `legal_cancellation`, `legal_support`, `legal_pricing`, `legal_seller_company`, `legal_ai_data_disclosures`, and `legal_platform_status`.

Admin save stores website text in CMS only. It must not publish public pages, change static `site/public/` files, expose draft/internal notes publicly, enable live Paddle, change billing, change entitlement behavior, or change Desktop behavior. Public rendering integration must be designed and reviewed as a separate controlled future task after real copy is prepared.

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

## Current status after 2026-06-27 Website CMS simplification

- Website remains a top-level Admin Shell tab, separate from the `CMS Content` sub-tabs. `CMS Content` remains focused on learner/runtime content packs.
- The visible Admin Website tab is simplified into website text management for Legal pages, Home page, Desktop page, and Mobile page / Coming soon.
- The main admin can select a section, edit website text, enter a Change note, and Save.
- The visible product UI is not a legal workflow engine and does not show validation, preview, review-status, publish, unpublish, owner/legal approval, rollback, or revision-history workflow controls.
- Website CMS now manages active public website text; static site text remains the fallback.
- Public rendering is still not connected. The deployed static public website remains the actual public rendering source, and `site/public/` remains the source for static public pages.
- Live Paddle is still not enabled. No checkout buttons, checkout links, Paddle client tokens, live price IDs, webhook secrets, or public payment behavior were added.
- Legal, seller, support, refund, cancellation, privacy, terms, pricing, home, desktop, and mobile/coming-soon final copy still needs preparation and approval outside code before any public rendering task.

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


## Simplified public website text manager update (2026-06-27)
- The Website CMS is connected to public website text through `GET /api/website/texts`, a no-auth read-only endpoint that returns only safe public section text.
- Admin → Website includes a simple “Load current website texts” action that creates missing rows and fills empty CMS text from the current static site defaults without overwriting non-empty admin text.
- Public pages under `site/public` mark replaceable text with `data-website-cms-section` and load CMS text with safe plain-text rendering and line breaks. If the fetch fails or a key is empty, the original static HTML remains visible.
- Save in the Website text manager updates the CMS text field used by public rendering. The visible product path does not require publish, unpublish, review, rollback, legal_approved, or owner_approved.
- This plan still excludes live Paddle enablement, checkout links, billing behavior changes, entitlement behavior changes, secrets, environment changes, production config changes, migrations, and deployment.
