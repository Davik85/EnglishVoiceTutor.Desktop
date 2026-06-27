namespace EnglishVoiceTutor.Api.Services.WebsiteCms;

public static class WebsiteCmsDefaultTexts
{
    public static readonly IReadOnlyDictionary<string, string> BySectionKey = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["home"] = """
Available for testers
Application for Windows
Practice real-life language lessons by text or voice on your desktop.
Download desktop version

In development
Application for mobile devices
Android and iOS versions are planned.
Mobile version coming soon

© Language Voice Tutor. All rights reserved.
""",
        ["desktop"] = """
Language Voice Tutor tester download
Private tester download
Language Voice Tutor
A Windows desktop app for practicing spoken languages with an AI tutor.
Private tester build. Please use only if you were invited to test.
Current version: Loading...
Download for Windows
Loading release details...
Windows may show a SmartScreen warning because code signing is deferred.
Need help? Email support@languagevoicetutor.com.
""",
        ["mobile"] = """
In development
Application for mobile devices
Android and iOS versions are planned.
Mobile version coming soon
""",
        ["legal_terms"] = """
Owner/legal review draft
Terms of Use
These draft terms describe use of Language Voice Tutor and require owner/legal review before they are treated as final legal terms.
Seller/legal entity placeholder: <LEGAL_SELLER_NAME>.

Product description
Language Voice Tutor is a language-learning application for practicing lessons by text or voice with an AI tutor. The current supported app is the Windows desktop version. Android and iOS versions are planned or in development.

Accounts and acceptable use
You are responsible for keeping your account credentials secure and for using the service lawfully. Do not misuse the service, attempt to disrupt it, reverse engineer restricted systems, submit unlawful content, or use the tutor to harm others.

AI tutor disclaimer
The AI tutor may produce inaccurate, incomplete, or unexpected responses. Language Voice Tutor is intended for practice and educational support, not professional advice, emergency use, or guaranteed language assessment.

Subscriptions and payments
Premium subscription billing is planned or available only when enabled by the owner. The draft Premium price is <PREMIUM_PRICE_AND_BILLING_PERIOD>. Final pricing, taxes, renewal terms, payment processor details, and billing availability require owner/legal approval.

Cancellation, refunds, and support
See the Cancellation Policy and Refund Policy for draft customer support paths. For help, contact support@languagevoicetutor.com.
""",
        ["legal_privacy"] = """
Owner/legal review draft
Privacy Policy
This draft explains high-level data handling for Language Voice Tutor and requires owner/legal review before publication as a final policy.

Account and lesson data
We may process account information such as your email address, authentication details, settings, support messages, and product usage records. We may also process lesson text, practice prompts, answers, and tutor conversation content that you provide while using the app.

Voice, audio, and transcription processing
If you use voice features, audio may be captured and processed to create transcriptions, evaluate practice interactions, and provide tutor responses. Voice/audio and transcription handling should be reviewed by the owner and legal counsel for final retention and provider details.

AI processing
Lesson text, prompts, responses, transcriptions, and related context may be sent to AI service providers to generate tutor feedback and practice responses. Do not submit sensitive personal information that is not needed for language practice.

Payments
If paid billing is enabled, Paddle may act as payment processor and merchant of record at a high level for checkout, payment, tax, invoicing, subscription, and related customer support workflows. This draft does not include Paddle IDs, keys, secrets, signatures, customer IDs, transaction IDs, or raw payment payloads.

Retention, deletion, and minors
Data retention and deletion periods require owner/legal approval. To request access, correction, deletion, or privacy help, contact support@languagevoicetutor.com. Children/minors stance placeholder: owner decision required before final publication.
""",
        ["legal_refunds"] = """
Owner/legal review draft
Refund Policy
This draft refund page is provided for review readiness and is not final legal advice.

How to request a refund
If paid billing is enabled and you need billing help or want to request a refund, contact support@languagevoicetutor.com with the email address associated with your account and a short explanation of the issue.

Refund window and review
Refund window/policy placeholder: owner/legal approval required. No guaranteed refund promise is made by this draft unless the owner later approves specific written refund terms.

Payment processor coordination
When paid billing is enabled, refund handling may require coordination with Paddle as payment processor and merchant of record. Do not send payment card numbers or sensitive payment credentials by email.
""",
        ["legal_cancellation"] = """
Owner/legal review draft
Cancellation Policy
This draft explains cancellation support paths for a future or owner-enabled Premium subscription.

How to cancel
If paid billing is enabled, customers may be able to cancel through the account or billing flow provided at checkout or by contacting support at support@languagevoicetutor.com. Final self-service cancellation instructions require owner and payment processor configuration review.

Cancellation timing
Draft wording: cancellation may stop future renewals and allow access to continue until the end of the current paid billing period, unless the owner/legal-approved policy or payment processor rules say otherwise.

Support escalation
If you cannot cancel or believe a renewal occurred in error, contact support@languagevoicetutor.com with your account email and a short description of the issue. Do not include card numbers or sensitive payment credentials.
""",
        ["legal_support"] = """
Support
Contact support
For Language Voice Tutor help, contact support@languagevoicetutor.com.
Phone support placeholder: <SUPPORT_PHONE_OR_OWNER_DECISION>.

What we can help with
Account access and account questions.
Download, installation, and Windows desktop app setup.
Billing, subscription, cancellation, and refund questions if paid billing is enabled.
Privacy and data requests.
Bug reports, tutor behavior issues, and product feedback.

What to include
Please include your account email, the app version if available, your Windows version for install issues, and a short description of the problem. Do not send passwords, payment card numbers, API keys, or other secrets.
""",
        ["legal_pricing"] = """
Draft pricing
Pricing
Language Voice Tutor is currently offered for Windows desktop tester access. Premium subscription billing is planned or available only when enabled by the owner.
This page is a review-readiness draft. It does not include a live checkout button and does not enable production Paddle billing.

Free access and tester access
Invited testers may be able to use the Windows desktop app to evaluate lessons, voice practice, and AI tutor interactions. Free or tester access may be limited, changed, suspended, or ended as the product is prepared for wider release.

Premium subscription draft
A Premium subscription may be offered only after the owner enables paid billing and approves final pricing, product limits, renewal terms, taxes, cancellation rules, and refund rules.

Planned price
<PREMIUM_PRICE_AND_BILLING_PERIOD>

Checkout status
No live checkout is provided on this page yet.

Billing status
Paid production billing should be considered unavailable unless and until the owner enables it.

Supported platforms
The current supported platform is Windows desktop. Android and iOS versions are planned or in development and are not currently claimed as available.
""",
        ["legal_seller_company"] = "Seller/legal entity placeholder: <LEGAL_SELLER_NAME>. Public business-contact context requires owner/legal approval before final publication. For help, contact support@languagevoicetutor.com.",
        ["legal_ai_data_disclosures"] = "The AI tutor may produce inaccurate, incomplete, or unexpected responses. Lesson text, prompts, responses, transcriptions, and related context may be sent to AI service providers to generate tutor feedback and practice responses. Do not submit sensitive personal information that is not needed for language practice.",
        ["legal_platform_status"] = "The current supported platform is Windows desktop. Android and iOS versions are planned or in development and are not currently claimed as available. Paid production billing should be considered unavailable unless and until the owner enables it."
    };
}
