using Swashbuckle.AspNetCore.Annotations;

namespace MoriiCoffee.Application.SeedWork.DTOs.Payment;

/// <summary>
/// Returned after creating a Stripe PaymentIntent.
/// The frontend uses <see cref="ClientSecret"/> to confirm the payment with the Stripe.js SDK.
/// </summary>
public class PaymentIntentResultDto
{
    [SwaggerSchema("Internal payment record ID.")]
    public Guid PaymentId { get; set; }

    [SwaggerSchema("Stripe PaymentIntent client secret. Pass this to Stripe.js to complete payment on the frontend.")]
    public string ClientSecret { get; set; } = null!;

    [SwaggerSchema("Stripe PaymentIntent ID for status polling or Stripe dashboard reference.")]
    public string PaymentIntentId { get; set; } = null!;
}
