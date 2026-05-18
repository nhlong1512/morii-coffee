namespace MoriiCoffee.Application.SeedWork.DTOs.Payment;

/// <summary>Request body for manually reconciling a Stripe payment after the success redirect.</summary>
public class ReconcileStripePaymentDto
{
    /// <summary>Optional Stripe Checkout Session id from the success redirect query string.</summary>
    public string? SessionId { get; set; }

    /// <summary>Optional local checkout draft id if the frontend persisted it before redirecting.</summary>
    public Guid? CheckoutDraftId { get; set; }
}
