# Account-deletion requests

`POST /api/me/account-deletion-requests` lets an authenticated user submit a request for account deletion. The request body contains the current password and an optional reason. The server derives the user from the authenticated claim and never stores, logs, or returns the password.

The request is stored as a `user_feedback_reports` item with category `account_deletion`. A user can have only one active request (`new`, `reviewed`, `needs_information`, or `processing`); a repeat submission returns that request's identifier and status. `resolved` and `rejected` are terminal statuses.

Support users manage these requests in the existing Admin CMS feedback-report queue, including filtering, status updates, and email replies. Submitting or resolving a request does not delete, deactivate, anonymize, cancel billing, or revoke authentication. Those actions require a separate approved operational process.
