using MoriiCoffee.Domain.Aggregates.PaymentAggregate;
using MoriiCoffee.Domain.SeedWork.Persistence;

namespace MoriiCoffee.Domain.Repositories;

/// <summary>
/// Repository contract for managing <see cref="Payment"/> entities.
/// Provides Stripe-specific lookup beyond the standard <see cref="IRepositoryBase{T}"/> contract.
/// </summary>
public interface IPaymentsRepository : IRepositoryBase<Payment>
{
    /// <summary>
    /// Retrieves a payment by its Stripe PaymentIntent ID.
    /// Used by the webhook handler to locate the local record when Stripe events arrive.
    /// </summary>
    Task<Payment?> GetByStripePaymentIntentIdAsync(string paymentIntentId);
}
