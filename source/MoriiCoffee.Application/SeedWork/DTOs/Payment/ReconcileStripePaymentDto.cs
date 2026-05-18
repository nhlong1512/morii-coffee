namespace MoriiCoffee.Application.SeedWork.DTOs.Payment;

/// <summary>Request body for manually reconciling a Stripe payment after the success redirect.</summary>
public class ReconcileStripePaymentDto
{
    /// <summary>The order that should be reconciled.</summary>
    public Guid OrderId { get; set; }

    /// <summary>Optional Stripe Checkout Session id from the success redirect query string.</summary>
    public string? SessionId { get; set; }
}
