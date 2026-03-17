namespace MoriiCoffee.Domain.Shared.Settings;

/// <summary>
/// Top-level configuration for the transactional email system.
/// Bound from the <c>EmailSettings</c> section in appsettings.json.
/// The <see cref="Provider"/> value selects which concrete implementation is registered at startup.
/// </summary>
public class EmailSettings
{
    /// <summary>
    /// Email provider to use. Accepted values: <c>"SendGrid"</c> or <c>"AwsSes"</c>.
    /// Defaults to <c>"SendGrid"</c>.
    /// </summary>
    public string Provider { get; set; } = "SendGrid";

    /// <summary>Sender email address shown in the From field (e.g., no-reply@moriicoffee.com).</summary>
    public string FromEmail { get; set; } = null!;

    /// <summary>Sender display name shown in the From field (e.g., "Morii Coffee").</summary>
    public string FromName { get; set; } = null!;

    /// <summary>
    /// Base URL of the password-reset page on the frontend (e.g., https://moriicoffee.com/reset-password).
    /// The token is appended as a <c>token</c> query parameter.
    /// </summary>
    public string ResetPasswordBaseUrl { get; set; } = null!;

    /// <summary>SendGrid-specific configuration. Required when <see cref="Provider"/> is <c>"SendGrid"</c>.</summary>
    public SendGridOptions SendGrid { get; set; } = new();

    /// <summary>AWS SES-specific configuration. Required when <see cref="Provider"/> is <c>"AwsSes"</c>.</summary>
    public AwsSesOptions AwsSes { get; set; } = new();
}

/// <summary>Configuration for the SendGrid email provider.</summary>
public class SendGridOptions
{
    /// <summary>SendGrid API key. Obtain from the SendGrid dashboard under Settings → API Keys.</summary>
    public string ApiKey { get; set; } = null!;
}

/// <summary>Configuration for the AWS Simple Email Service provider.</summary>
public class AwsSesOptions
{
    /// <summary>AWS region where SES is configured (e.g., <c>ap-southeast-1</c>).</summary>
    public string Region { get; set; } = null!;

    /// <summary>AWS IAM access key ID with SES send permissions.</summary>
    public string AccessKey { get; set; } = null!;

    /// <summary>AWS IAM secret access key corresponding to <see cref="AccessKey"/>.</summary>
    public string SecretKey { get; set; } = null!;
}
