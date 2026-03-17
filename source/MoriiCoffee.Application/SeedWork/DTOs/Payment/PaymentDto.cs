using MoriiCoffee.Domain.Shared.Enums.Payment;
using Swashbuckle.AspNetCore.Annotations;

namespace MoriiCoffee.Application.SeedWork.DTOs.Payment;

/// <summary>Response DTO returned by all payment endpoints.</summary>
public class PaymentDto
{
    [SwaggerSchema("Unique identifier of the payment record.")]
    public Guid Id { get; set; }

    [SwaggerSchema("ID of the user who made the payment.")]
    public Guid UserId { get; set; }

    [SwaggerSchema("Payment amount.")]
    public decimal Amount { get; set; }

    [SwaggerSchema("ISO 4217 currency code (e.g., 'vnd', 'usd').")]
    public string Currency { get; set; } = null!;

    [SwaggerSchema("Stripe PaymentIntent ID for cross-referencing with the Stripe dashboard.")]
    public string StripePaymentIntentId { get; set; } = null!;

    [SwaggerSchema("Current lifecycle status of the payment.")]
    public EPaymentStatus Status { get; set; }

    [SwaggerSchema("Payment method used (e.g., 'card'). Set after confirmation.")]
    public string? PaymentMethod { get; set; }

    [SwaggerSchema("Description of what the payment is for.")]
    public string? Description { get; set; }

    [SwaggerSchema("UTC timestamp when the payment was initiated.")]
    public DateTime CreatedAt { get; set; }

    [SwaggerSchema("UTC timestamp of the last status update.")]
    public DateTime? UpdatedAt { get; set; }
}
