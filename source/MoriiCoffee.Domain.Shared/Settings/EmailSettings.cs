namespace MoriiCoffee.Domain.Shared.Settings;

public class EmailSettings
{
    /// <summary>
    /// Sender email address (must be verified in Brevo dashboard)
    /// </summary>
    public string FromEmail { get; set; } = null!;

    /// <summary>
    /// Display name for sender
    /// </summary>
    public string FromName { get; set; } = null!;

    /// <summary>
    /// Frontend base URL for storefront links
    /// </summary>
    public string StorefrontUrl { get; set; } = null!;

    /// <summary>
    /// Frontend password reset page URL
    /// </summary>
    public string ResetPasswordBaseUrl { get; set; } = null!;

    /// <summary>
    /// Brevo-specific configuration
    /// </summary>
    public BrevoSettings Brevo { get; set; } = null!;
}

public class BrevoSettings
{
    /// <summary>
    /// Brevo API key (xkeysib-... format)
    /// </summary>
    public string ApiKey { get; set; } = null!;
}
