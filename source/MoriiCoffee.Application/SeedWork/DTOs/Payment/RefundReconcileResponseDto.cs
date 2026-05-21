using MoriiCoffee.Domain.Shared.Enums.Order;

namespace MoriiCoffee.Application.SeedWork.DTOs.Payment;

/// <summary>
/// Response for <c>POST /api/v1/payments/{orderId}/refund/reconcile</c>.
/// Indicates whether local refund state changed after querying Stripe directly.
/// </summary>
public class RefundReconcileResponseDto
{
    public Guid OrderId { get; set; }

    public EPaymentStatus PaymentStatus { get; set; }

    public ERefundStatus? LatestRefundStatus { get; set; }

    public bool Reconciled { get; set; }

    public int ReconciledRefundCount { get; set; }
}
