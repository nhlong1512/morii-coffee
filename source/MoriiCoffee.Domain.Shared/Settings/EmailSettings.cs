namespace MoriiCoffee.Domain.Shared.Settings;

/// <summary>
/// Top-level configuration for the transactional email system.
/// Bound from the <c>EmailSettings</c> section in appsettings.json.
/// Email delivery is handled exclusively by SendGrid.
/// </summary>
public class EmailSettings
{
    /// <summary>Sender email address shown in the From field (e.g., no-reply@moriicoffee.com).</summary>
    public string FromEmail { get; set; } = null!;

    /// <summary>Sender display name shown in the From field (e.g., "Morii Coffee").</summary>
    public string FromName { get; set; } = null!;

    /// <summary>
    /// Base URL of the storefront homepage (e.g., http://localhost:3000 or https://moriicoffee.com).
    /// Used in welcome email CTA button.
    /// </summary>
    public string StorefrontUrl { get; set; } = null!;

    /// <summary>
    /// Base URL of the password-reset page on the frontend (e.g., https://moriicoffee.com/reset-password).
    /// The token is appended as a <c>token</c> query parameter.
    /// </summary>
    public string ResetPasswordBaseUrl { get; set; } = null!;

    /// <summary>SendGrid-specific configuration (API key and delivery settings).</summary>
    public SendGridOptions SendGrid { get; set; } = new();
}

/// <summary>Configuration for the SendGrid email provider.</summary>
public class SendGridOptions
{
    /// <summary>SendGrid API key. Obtain from the SendGrid dashboard under Settings → API Keys.</summary>
    public string ApiKey { get; set; } = null!;
}
