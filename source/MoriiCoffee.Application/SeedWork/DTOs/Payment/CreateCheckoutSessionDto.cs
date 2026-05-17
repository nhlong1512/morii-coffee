namespace MoriiCoffee.Application.SeedWork.DTOs.Payment;

/// <summary>Request body for <c>POST /api/v1/payments/checkout-session</c>.</summary>
public class CreateCheckoutSessionDto
{
    /// <summary>Id of the Order to create a Stripe Checkout Session for.</summary>
    public Guid OrderId { get; set; }
}
