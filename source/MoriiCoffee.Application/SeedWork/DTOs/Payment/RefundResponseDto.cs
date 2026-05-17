using MoriiCoffee.Domain.Shared.Enums.Order;

namespace MoriiCoffee.Application.SeedWork.DTOs.Payment;

/// <summary>
/// Response for <c>POST /api/v1/payments/{orderId}/refund</c>. The order's payment status here
/// reflects the optimistic state set after Stripe accepts the refund; the authoritative status
/// arrives via the <c>charge.refunded</c> webhook within seconds.
/// </summary>
public class RefundResponseDto
{
    /// <summary>Internal RefundRecord id.</summary>
    public Guid RefundId { get; set; }

    /// <summary>Stripe Refund id.</summary>
    public string StripeRefundId { get; set; } = null!;

    /// <summary>Refund amount in VND.</summary>
    public decimal Amount { get; set; }

    /// <summary>Refund row status — typically <see cref="ERefundStatus.Pending"/> at creation time.</summary>
    public ERefundStatus Status { get; set; }

    /// <summary>Order's payment status after the optimistic local update.</summary>
    public EPaymentStatus PaymentStatus { get; set; }
}
