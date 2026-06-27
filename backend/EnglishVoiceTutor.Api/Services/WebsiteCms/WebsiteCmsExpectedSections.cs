namespace EnglishVoiceTutor.Api.Services.WebsiteCms;

public static class WebsiteCmsExpectedSections
{
    public static readonly IReadOnlyList<ExpectedWebsiteCmsSection> All =
    [
        new("home", "Home page", "Hero, product description, tester/download, and footer/support website copy."),
        new("desktop", "Desktop page", "Windows desktop app title, description, tester/download wording, and requirements or availability copy."),
        new("mobile", "Mobile page / Coming soon", "Mobile status, coming soon wording, and future availability copy."),
        new("legal_terms", "Terms", "Terms website text."),
        new("legal_privacy", "Privacy Policy", "Privacy policy website text."),
        new("legal_refunds", "Refund Policy", "Refund policy website text."),
        new("legal_cancellation", "Cancellation Policy", "Cancellation policy website text."),
        new("legal_support", "Support", "Support website text."),
        new("legal_pricing", "Pricing", "Pricing website text."),
        new("legal_seller_company", "Seller / Company details", "Seller identity, company details, and public business-contact context."),
        new("legal_ai_data_disclosures", "AI / data disclosure", "AI usage and learner data disclosure website text."),
        new("legal_platform_status", "Platform availability", "Platform availability, service status, and operational notice website text.")
    ];
}

public sealed record ExpectedWebsiteCmsSection(string SectionKey, string DisplayName, string Description);
