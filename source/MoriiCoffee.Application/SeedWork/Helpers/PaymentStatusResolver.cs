using MoriiCoffee.Domain.Aggregates.OrderAggregate;
using MoriiCoffee.Domain.Aggregates.PaymentAggregate;
using MoriiCoffee.Domain.Shared.Enums.Order;

namespace MoriiCoffee.Application.SeedWork.Helpers;

/// <summary>
/// Computes the customer-facing payment lifecycle from one authoritative source: payment attempts
/// plus their refund history. Stored summary fields on <see cref="Order"/> are treated only as a
/// fallback for legacy or incomplete data.
/// </summary>
public static class PaymentStatusResolver
{
    /// <summary>Resolves the payment status for an order from its payment attempts.</summary>
    public static EPaymentStatus Resolve(Order order, IReadOnlyCollection<Payment> payments)
        => Resolve(order.PaymentMethod, order.PaymentStatus, order.Total, payments);

    /// <summary>Resolves the payment status for an order from its payment attempts.</summary>
    public static EPaymentStatus Resolve(
        EPaymentMethod paymentMethod,
        EPaymentStatus fallbackStatus,
        decimal orderTotal,
        IReadOnlyCollection<Payment> payments)
    {
        if (paymentMethod == EPaymentMethod.COD)
            return EPaymentStatus.NotRequired;

        if (payments.Count == 0)
            return fallbackStatus;

        var latestSucceededPayment = payments
            .Where(p => p.Status == EPaymentTransactionStatus.Succeeded)
            .OrderByDescending(p => p.CreatedAt)
            .FirstOrDefault();

        if (latestSucceededPayment is not null)
        {
            var cumulativeRefunded = latestSucceededPayment.TotalSucceededRefundAmount;
            if (cumulativeRefunded >= orderTotal && orderTotal > 0)
                return EPaymentStatus.Refunded;

            if (cumulativeRefunded > 0)
                return EPaymentStatus.PartiallyRefunded;

            return EPaymentStatus.Paid;
        }

        var latestAttempt = payments.OrderByDescending(p => p.CreatedAt).First();
        return latestAttempt.Status switch
        {
            EPaymentTransactionStatus.Created => EPaymentStatus.Pending,
            EPaymentTransactionStatus.Failed => EPaymentStatus.Failed,
            EPaymentTransactionStatus.Expired => EPaymentStatus.Failed,
            _ => fallbackStatus
        };
    }
}
