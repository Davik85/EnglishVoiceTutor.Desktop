# Account-deletion requests

> **Deployment status (2026-07-23).** Production is `0.1.35-backend.132`, with `0.1.35-backend.131` as the previous rollback release. Migration `20260723045852_AddAccountAnonymizationExecution` and the complete account-deletion backend and Admin UI workflow are deployed. The `.130` manual-resolution correction and `.131` report-ID confirmation correction are deployed through `.132`. Only the reviewed correction for retained `AdminAction` target-history rows and the failed-execution dialog remains locally implemented and pending as code-only `0.1.35-backend.133`; it requires no migration, Paddle/provider action, or financial-record change.

## Current deployed contract

Backend `0.1.35-backend.127` introduced `POST /api/me/account-deletion-requests`; it remains available in current production backend `0.1.35-backend.132`. The endpoint requires an authenticated learner, derives the account identity only from that authenticated user, requires confirmation with the current password, and accepts an optional deletion reason. The password is used only for confirmation: it is never stored, returned, emailed, or displayed in Admin CMS.

Only one unresolved request is allowed per user. The partial unique index `IX_user_feedback_reports_ActiveAccountDeletionRequest_UserId`, applied by migration `20260721120000_AddActiveAccountDeletionRequestConstraint`, enforces that boundary. A duplicate submission safely returns the existing active request identifier and status instead of creating another unresolved request. Once a prior request reaches a terminal status, a later request may create a new support ticket by design.

## Support workflow

Account-deletion requests reuse the existing feedback/support queue with category `account_deletion`. Authorized administrators can filter and open them, change their support status, and reply through the existing email reply mechanism. Replies may acknowledge receipt, request more information, give a processing update, reject the request with an explanation, or confirm completion after the real data operation.

Submitting, replying to, or changing the status of a request does **not** automatically:

- delete or deactivate the account;
- anonymize or otherwise alter user data;
- cancel a subscription;
- revoke all authentication token families; or
- perform the actual deletion/anonymization procedure.

Actual deletion/anonymization is performed through the deployed Super-Admin workflow after a fresh preflight, rather than by ticket submission or an arbitrary support-status change. A request must not be marked `resolved` or otherwise completed merely to close the ticket: that status must reflect completed anonymization. The password must never be copied into support correspondence or Admin CMS.

The repository-grounded design and complete data inventory are in [Account anonymization procedure and data inventory](ACCOUNT_ANONYMIZATION_PROCEDURE.md). That document is a design/runbook draft only: it does not implement deletion/anonymization, authorize a production operation, or change the request-intake behavior described here.

## Client status

Mobile Settings integration is implemented and manually verified. **Settings → Request account deletion** requires the current password and displays the returned request ID and status for both a newly created request and an existing active request. An incorrect password neither creates a request nor logs the learner out. Mobile wording describes a **request**, not immediate deletion; submitting or resolving the support ticket still does not perform real deletion or anonymization.

Future client design only: the entry is intended to move to **Settings → App → Contact & Support → Request account deletion**, subject to the actual Mobile Settings structure. Before password entry and submission it must show a neutral warning that saved learner information will no longer be available after irreversible deletion/anonymization, the reason remains optional, and submitting creates a support-managed request rather than immediate deletion. This future placement and warning are not implemented by the current Mobile flow.
