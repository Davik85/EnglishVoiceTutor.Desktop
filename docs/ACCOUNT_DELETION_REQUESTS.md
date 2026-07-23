# Account-deletion requests

> **Production workflow correction (2026-07-23).** Production `0.1.35-backend.130` exposed that support could manually mark an account-deletion request `resolved` before anonymization completed. The repository now rejects that transition unless the related anonymization operation is completed; successful anonymization continues to resolve the request itself. This bounded fix is not yet deployed and requires no migration or Paddle/provider change.

> **Local implementation update (2026-07-23).** The complete Super-Admin Admin Shell confirmation UI and backend execution flow are implemented locally for a fresh `processing` request. Execution removes learner access/data, redacts support content, retains local financial/provider history unchanged, and resolves the request only after verification. It does not call Paddle or another provider, send email, cancel renewal, refund, or otherwise alter financial records. Active effective Premium/current paid access blocks deletion until expiry; customers should be reminded to cancel renewal, while disputes/refunds/chargebacks remain manual. The combined migration and backend deployment remain pending; production is `0.1.35-backend.129`.

## Current deployed contract

Backend `0.1.35-backend.127` introduced `POST /api/me/account-deletion-requests`; it remains available in current production backend `0.1.35-backend.128`. The endpoint requires an authenticated learner, derives the account identity only from that authenticated user, requires confirmation with the current password, and accepts an optional deletion reason. The password is used only for confirmation: it is never stored, returned, emailed, or displayed in Admin CMS.

Only one unresolved request is allowed per user. The partial unique index `IX_user_feedback_reports_ActiveAccountDeletionRequest_UserId`, applied by migration `20260721120000_AddActiveAccountDeletionRequestConstraint`, enforces that boundary. A duplicate submission safely returns the existing active request identifier and status instead of creating another unresolved request. Once a prior request reaches a terminal status, a later request may create a new support ticket by design.

## Support workflow

Account-deletion requests reuse the existing feedback/support queue with category `account_deletion`. Authorized administrators can filter and open them, change their support status, and reply through the existing email reply mechanism. Replies may acknowledge receipt, request more information, give a processing update, reject the request with an explanation, or confirm completion after the real data operation.

This release provides request intake and support-managed tracking only. Submitting, replying to, or changing the status of a request does **not** automatically:

- delete or deactivate the account;
- anonymize or otherwise alter user data;
- cancel a subscription;
- revoke all authentication token families; or
- perform the actual deletion/anonymization procedure.

Actual deletion or anonymization remains a manual support process. A request must not be marked `resolved` or otherwise completed merely to close the ticket: that status must reflect that the real approved deletion or anonymization work has been performed. The password must never be copied into support correspondence or Admin CMS.

The repository-grounded design and complete data inventory are in [Account anonymization procedure and data inventory](ACCOUNT_ANONYMIZATION_PROCEDURE.md). That document is a design/runbook draft only: it does not implement deletion/anonymization, authorize a production operation, or change the request-intake behavior described here.

## Client status

Mobile Settings integration is implemented and manually verified. **Settings → Request account deletion** requires the current password and displays the returned request ID and status for both a newly created request and an existing active request. An incorrect password neither creates a request nor logs the learner out. Mobile wording describes a **request**, not immediate deletion; submitting or resolving the support ticket still does not perform real deletion or anonymization.

Future client design only: the entry is intended to move to **Settings → App → Contact & Support → Request account deletion**, subject to the actual Mobile Settings structure. Before password entry and submission it must show a neutral warning that saved learner information will no longer be available after irreversible deletion/anonymization, the reason remains optional, and submitting creates a support-managed request rather than immediate deletion. This future placement and warning are not implemented by the current Mobile flow.
