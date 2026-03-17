namespace MoriiCoffee.Domain.Shared.Settings;

/// <summary>
/// Configuration for the Stripe payment provider.
/// Bound from the <c>StripeSettings</c> section in appsettings.json.
/// All keys must be kept out of source control — use environment variables or secrets in production.
/// </summary>
public class StripeSettings
{
    /// <summary>Stripe secret key (sk_live_... or sk_test_...). Used server-side to call the Stripe API.</summary>
    public string SecretKey { get; set; } = null!;

    /// <summary>Stripe publishable key (pk_live_... or pk_test_...). Safe to expose to the frontend.</summary>
    public string PublishableKey { get; set; } = null!;

    /// <summary>
    /// Stripe webhook signing secret (whsec_...).
    /// Used by the webhook handler to verify that events originate from Stripe.
    /// </summary>
    public string WebhookSecret { get; set; } = null!;
}
