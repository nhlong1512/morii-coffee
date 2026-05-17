namespace MoriiCoffee.Domain.Shared.Settings;

/// <summary>
/// Strongly-typed configuration for the Stripe payment-provider integration.
/// Bound from the "Stripe" section in appsettings*.json + environment variables.
/// </summary>
/// <remarks>
/// <para>
/// Real secret values (<see cref="SecretKey"/>, <see cref="WebhookSigningSecret"/>) MUST be supplied
/// via environment variables in the form <c>Stripe__SecretKey</c>, <c>Stripe__WebhookSigningSecret</c>,
/// and <c>Stripe__PublishableKey</c>. They must NEVER be committed to source control.
/// </para>
/// <para>
/// The placeholders left in <c>appsettings.json</c> intentionally show the section shape so the file
/// is self-documenting, but they are blank by design.
/// </para>
/// </remarks>
public class StripeSettings
{
    /// <summary>
    /// Stripe API secret key — e.g. <c>sk_test_...</c> (test mode) or <c>sk_live_...</c> (live mode).
    /// Used by the backend to call the Stripe API. Never exposed to clients.
    /// </summary>
    public string SecretKey { get; set; } = string.Empty;

    /// <summary>
    /// Stripe API publishable key — e.g. <c>pk_test_...</c> or <c>pk_live_...</c>.
    /// Safe to expose to the frontend; included in the checkout-session response for clients that
    /// might want to render Stripe Elements in a future iteration.
    /// </summary>
    public string PublishableKey { get; set; } = string.Empty;

    /// <summary>
    /// Shared signing secret for webhook payloads — e.g. <c>whsec_...</c>.
    /// Used to verify the HMAC-SHA256 signature on every event delivered to the webhook endpoint.
    /// </summary>
    public string WebhookSigningSecret { get; set; } = string.Empty;

    /// <summary>
    /// ISO 4217 currency code for charges. VND (Vietnamese đồng) is a zero-decimal currency:
    /// the integer amount sent to Stripe equals the đồng amount (no multiplication by 100).
    /// </summary>
    public string Currency { get; set; } = "vnd";

    /// <summary>
    /// Path component appended to the storefront URL when redirecting the customer back after a
    /// successful payment. <c>{CHECKOUT_SESSION_ID}</c> is a Stripe placeholder substituted at runtime.
    /// </summary>
    public string SuccessUrlTemplate { get; set; } = "/checkout/success?session_id={CHECKOUT_SESSION_ID}";

    /// <summary>
    /// Path component appended to the storefront URL when the customer cancels the Stripe-hosted
    /// payment page.
    /// </summary>
    public string CancelUrlPath { get; set; } = "/checkout/cancel";

    /// <summary>
    /// <c>true</c> when <see cref="SecretKey"/> looks like a live key (<c>sk_live_</c> prefix).
    /// Used at startup to log which mode the deployment is operating in (FR-020), so operators
    /// can immediately spot a misconfiguration that mixes live and test environments.
    /// </summary>
    public bool IsLiveMode =>
        !string.IsNullOrWhiteSpace(SecretKey) &&
        SecretKey.StartsWith("sk_live_", StringComparison.Ordinal);
}
