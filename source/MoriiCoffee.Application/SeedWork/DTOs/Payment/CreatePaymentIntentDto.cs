using Swashbuckle.AspNetCore.Annotations;

namespace MoriiCoffee.Application.SeedWork.DTOs.Payment;

/// <summary>Request body for creating a Stripe PaymentIntent.</summary>
public class CreatePaymentIntentDto
{
    [SwaggerSchema("Payment amount. For VND this is the full amount (no minor units); for USD this is cents.")]
    public decimal Amount { get; set; }

    [SwaggerSchema("ISO 4217 currency code in lowercase (e.g., 'vnd', 'usd'). Defaults to 'vnd'.")]
    public string Currency { get; set; } = "vnd";

    [SwaggerSchema("Human-readable description of the payment (e.g., 'Order #12345').")]
    public string? Description { get; set; }
}
