using MoriiCoffee.Domain.Aggregates.PaymentAggregate;
using MoriiCoffee.Domain.Aggregates.PaymentAggregate.Entities;
using MoriiCoffee.Domain.SeedWork.Persistence;
using MoriiCoffee.Domain.Shared.Enums.Order;

namespace MoriiCoffee.Domain.Repositories;

/// <summary>
/// Repository contract for the <see cref="Payment"/> aggregate. Inherits generic CRUD/pagination
/// from <see cref="IRepositoryBase{T}"/> and adds the lookups that the application layer needs
/// during checkout, webhook dispatch, and refund flows.
/// </summary>
public interface IPaymentRepository : IRepositoryBase<Payment>
{
    /// <summary>
    /// Loads a Payment by its Stripe Checkout Session id, eagerly including its refund children.
    /// Used by the webhook handler to find the local Payment from a <c>checkout.session.*</c> event.
    /// Returns <c>null</c> when no matching, non-deleted Payment exists.
    /// </summary>
    Task<Payment?> GetBySessionIdAsync(string stripeSessionId, EPaymentProvider provider = EPaymentProvider.Stripe);

    /// <summary>
    /// Loads a Payment by its Stripe PaymentIntent id, eagerly including its refund children.
    /// Used by the webhook handler for <c>payment_intent.*</c> and <c>charge.refunded</c> events.
    /// Returns <c>null</c> when no matching, non-deleted Payment exists.
    /// </summary>
    Task<Payment?> GetByPaymentIntentIdAsync(string stripePaymentIntentId, EPaymentProvider provider = EPaymentProvider.Stripe);

    /// <summary>
    /// Loads the most recent Payment for the given <c>OrderId</c> that is still in
    /// <see cref="MoriiCoffee.Domain.Shared.Enums.Order.EPaymentTransactionStatus.Created"/>.
    /// Used by the refund flow to verify a previous successful payment exists.
    /// </summary>
    Task<Payment?> GetLatestPendingByOrderIdAsync(Guid orderId);

    /// <summary>
    /// Loads the most recent successfully captured Payment for the given <c>OrderId</c>, eagerly
    /// including its refunds. Used by the refund flow.
    /// </summary>
    Task<Payment?> GetLatestSucceededByOrderIdAsync(Guid orderId);

    /// <summary>
    /// Lists all Payments (with their refunds) for an order, newest first. Used by the
    /// <c>GET /payments/by-order/{orderId}</c> endpoint.
    /// </summary>
    Task<IReadOnlyList<Payment>> ListByOrderIdAsync(Guid orderId);

    /// <summary>
    /// Persists a newly created refund child row for a payment. This is kept explicit because
    /// refund rows are created asynchronously after a successful provider API call, and relying
    /// on aggregate graph state alone proved fragile in the real EF tracking flow.
    /// </summary>
    Task CreateRefundAsync(RefundRecord refund);
}
