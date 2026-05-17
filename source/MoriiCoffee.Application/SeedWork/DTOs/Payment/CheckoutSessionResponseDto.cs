namespace MoriiCoffee.Application.SeedWork.DTOs.Payment;

/// <summary>
/// Response body for <c>POST /api/v1/payments/checkout-session</c>. The frontend redirects the
/// browser to <see cref="CheckoutUrl"/>; everything else is included for diagnostics / future
/// embedded-element flows.
/// </summary>
public class CheckoutSessionResponseDto
{
    /// <summary>Stripe Checkout Session id (e.g. <c>cs_test_...</c>).</summary>
    public string SessionId { get; set; } = null!;

    /// <summary>Browser redirect target — <c>https://checkout.stripe.com/...</c></summary>
    public string CheckoutUrl { get; set; } = null!;

    /// <summary>UTC time at which the session expires at Stripe (default 24 h from creation).</summary>
    public DateTime ExpiresAtUtc { get; set; }

    /// <summary>Internal Payment row id created server-side for this attempt.</summary>
    public Guid PaymentId { get; set; }

    /// <summary>The Order id this session pays for.</summary>
    public Guid OrderId { get; set; }

    /// <summary>Amount in VND (zero-decimal: integer == đồng).</summary>
    public long Amount { get; set; }

    /// <summary>ISO 4217 lowercase (e.g. <c>vnd</c>).</summary>
    public string Currency { get; set; } = null!;

    /// <summary>Stripe publishable key — safe to expose. Useful for clients that want to integrate Stripe Elements.</summary>
    public string PublishableKey { get; set; } = null!;
}
