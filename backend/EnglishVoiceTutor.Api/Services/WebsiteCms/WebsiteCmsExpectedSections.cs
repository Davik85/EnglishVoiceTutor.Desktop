namespace EnglishVoiceTutor.Api.Services.WebsiteCms;

public static class WebsiteCmsExpectedSections
{
    public static readonly IReadOnlyList<ExpectedWebsiteCmsSection> All =
    [
        new("seller_company", "Seller / Company", "Seller identity, company profile, and public business-contact context."),
        new("support", "Support", "Customer support contact, response expectations, and help-channel guidance."),
        new("pricing", "Pricing", "Public pricing-plan description and review-safe billing explanation."),
        new("terms", "Terms", "Terms of service overview and legal policy copy."),
        new("privacy", "Privacy", "Privacy policy overview and data-handling policy copy."),
        new("refunds", "Refunds", "Refund policy and customer support expectations for refund requests."),
        new("cancellation", "Cancellation", "Cancellation policy and subscription-renewal explanation copy."),
        new("ai_data_disclosures", "AI / Data Disclosures", "AI usage, learner data handling, and safety disclosure copy."),
        new("platform_status", "Platform Status", "Platform availability, service status, and operational notice copy.")
    ];
}

public sealed record ExpectedWebsiteCmsSection(string SectionKey, string DisplayName, string Description);
