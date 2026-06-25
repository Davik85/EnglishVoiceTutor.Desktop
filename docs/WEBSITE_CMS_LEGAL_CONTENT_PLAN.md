# Website CMS Legal/Support/Pricing Content Plan

Review date: 2026-06-25.

## Purpose

Define a small, safe Admin CMS feature foundation and future workflow for managing public website legal, seller, support, policy, and pricing display content without code changes after implementation. This plan now has a first backend foundation slice implemented. It still does not change public website rendering and does not provide final legal advice.

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

The public website is currently maintained as static files under `site/public/`. Temporary Paddle review-readiness pages already exist:

- `pricing.html`
- `terms.html`
- `privacy.html`
- `refunds.html`
- `cancellation.html`
- `support.html`

Those files are suitable as temporary static review-readiness shells, but future updates to legal/support/pricing copy should be planned for a controlled Admin CMS workflow after separate legal and owner review.

The existing Admin shell has a `CMS Content` workspace for lesson/prompt/tutor content with draft saves, validation/preview, versions/publish, and audit concepts. The website CMS should be a separate future area so public website policy content is not mixed with learner lesson content.

## Target Admin CMS tab

Preferred tab name: **Website**.

Acceptable alternate tab name: **Public Site**.

The tab should be clearly labeled as public website content management and should show a warning that legal and policy content remains draft/review content until explicitly approved and published.

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

## Implementation status

- Read-only Website UI skeleton exists as a top-level Admin Shell tab, separate from the CMS Content sub-tabs.
- First backend foundation added: a dedicated `website_cms_sections` persistence model for section key, draft body, optional published body, review status, effective date, internal notes, change reason, and updated/published timestamps.
- Secret-like Website CMS content guard added to block obvious Paddle secrets, webhook secrets/signatures, API keys, JWT keys, connection strings, raw provider payload markers, customer IDs, transaction IDs, and subscription IDs before future save/publish flows persist content.
- Public rendering is still not connected. `site/public/` remains static and unchanged by this foundation.
- Live Paddle is still not enabled. No checkout buttons, checkout links, Paddle client tokens, live price IDs, webhook secrets, or public payment behavior were added.
- No full editing UI, publish-to-public-site behavior, unauthenticated Website CMS endpoint, billing behavior, entitlement behavior, Desktop behavior, deployment script, backend environment variable, or production configuration change is included in this slice.
- Safe SQL generation script added for the pending `20260625090000_AddWebsiteCmsLegalContentFoundation` migration; it writes reviewable SQL under `artifacts/sql/backend` and does not apply the migration.
- The Website CMS migration still must be reviewed and applied separately by an operator after backups/environment checks.
- Public rendering and live Paddle remain disconnected: no public Website CMS rendering path, checkout button/link, or live-payment behavior is enabled by this status update.

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

This plan now records a backend foundation slice. It does not change public rendering, public static pages, billing behavior, entitlement behavior, Desktop behavior, deployment scripts, production configuration, backend environment variables, or Paddle configuration. The added migration is scoped to Website CMS section storage only and does not alter existing lesson/content/runtime behavior.
