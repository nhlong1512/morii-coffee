using MoriiCoffee.Application.SeedWork.Abstractions;
using MoriiCoffee.Domain.Aggregates.OrderAggregate;
using MoriiCoffee.Domain.Aggregates.PaymentAggregate;
using MoriiCoffee.Domain.Aggregates.PaymentAggregate.Entities;
using MoriiCoffee.Domain.SeedWork.Persistence;
using MoriiCoffee.Domain.Shared.Enums.Order;

namespace MoriiCoffee.Application.SeedWork.Helpers;

/// <summary>
/// Synchronizes local refund rows and order payment state from the provider-side payment intent.
/// Used by both the admin refund flow and the dedicated refund reconcile endpoint.
/// </summary>
public static class RefundStateReconciler
{
    public static async Task<RefundStateReconciliationResult> ReconcileAsync(
        IUnitOfWork unitOfWork,
        IPaymentGateway gateway,
        Order order,
        Payment payment,
        Guid adminUserId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(unitOfWork);
        ArgumentNullException.ThrowIfNull(gateway);
        ArgumentNullException.ThrowIfNull(order);
        ArgumentNullException.ThrowIfNull(payment);

        var providerStatus = await gateway.GetPaymentStatusAsync(
            payment.StripePaymentIntentId!,
            cancellationToken);

        var changed = false;
        var reconciledRefundCount = 0;

        foreach (var providerRefund in providerStatus.Refunds)
        {
            var localRefund = payment.Refunds.FirstOrDefault(r => r.StripeRefundId == providerRefund.RefundId);
            if (localRefund is null)
            {
                // Import successful and pending provider refunds that do not exist locally.
                // Failed refunds are only reconciled when we already have a local row to update.
                if (string.Equals(providerRefund.Status, "failed", StringComparison.OrdinalIgnoreCase))
                    continue;

                localRefund = RefundRecord.Create(
                    payment.Id,
                    providerRefund.RefundId,
                    providerRefund.Amount,
                    adminUserId,
                    BuildImportedRefundReason(providerRefund.Status),
                    payment.Provider);

                payment.AddRefund(localRefund);
                await unitOfWork.Payments.CreateRefundAsync(localRefund);
                changed = true;
                reconciledRefundCount++;
            }

            changed |= ApplyProviderRefundStatus(localRefund, providerRefund.Status);
        }

        var resolvedPaymentStatus = PaymentStatusResolver.Resolve(order, [payment]);
        if (resolvedPaymentStatus != order.PaymentStatus &&
            resolvedPaymentStatus is EPaymentStatus.Refunded or EPaymentStatus.PartiallyRefunded)
        {
            order.ApplyRefund(payment.TotalSucceededRefundAmount);
            await unitOfWork.Orders.Update(order);
            changed = true;
        }

        if (changed)
            await unitOfWork.Payments.Update(payment);

        return new RefundStateReconciliationResult
        {
            Changed = changed,
            PaymentStatus = PaymentStatusResolver.Resolve(order, [payment]),
            LatestRefund = payment.Refunds
                .OrderByDescending(r => r.CreatedAt)
                .FirstOrDefault(),
            ReconciledRefundCount = reconciledRefundCount
        };
    }

    private static bool ApplyProviderRefundStatus(RefundRecord refund, string providerStatus)
    {
        if (string.Equals(providerStatus, "succeeded", StringComparison.OrdinalIgnoreCase))
        {
            if (refund.Status == ERefundStatus.Succeeded)
                return false;

            refund.MarkSucceeded();
            return true;
        }

        if (string.Equals(providerStatus, "failed", StringComparison.OrdinalIgnoreCase))
        {
            if (refund.Status == ERefundStatus.Failed)
                return false;

            refund.MarkFailed("Reconciled from Stripe provider state.");
            return true;
        }

        return false;
    }

    private static string BuildImportedRefundReason(string providerStatus)
        => $"Imported from Stripe refund reconciliation (provider status: {providerStatus}).";
}

public class RefundStateReconciliationResult
{
    public bool Changed { get; set; }

    public EPaymentStatus PaymentStatus { get; set; }

    public RefundRecord? LatestRefund { get; set; }

    public int ReconciledRefundCount { get; set; }
}
