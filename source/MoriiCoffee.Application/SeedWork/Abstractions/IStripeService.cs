using MoriiCoffee.Application.SeedWork.DTOs.Payment;

namespace MoriiCoffee.Application.SeedWork.Abstractions;

/// <summary>
/// Abstracts all Stripe API interactions so the Application layer stays infrastructure-agnostic.
/// The concrete implementation in the Infrastructure layer uses the Stripe.net SDK.
/// </summary>
public interface IStripeService
{
    /// <summary>
    /// Creates a Stripe PaymentIntent and returns the client secret needed by the frontend
    /// to confirm the payment with Stripe.js.
    /// </summary>
    Task<PaymentIntentResultDto> CreatePaymentIntentAsync(decimal amount, string currency, string? description);

    /// <summary>Cancels a Stripe PaymentIntent that is still in a cancellable state.</summary>
    Task CancelPaymentIntentAsync(string paymentIntentId);

    /// <summary>Initiates a full refund for the charge associated with the given PaymentIntent.</summary>
    Task<RefundResultDto> RefundPaymentAsync(string paymentIntentId);
}
