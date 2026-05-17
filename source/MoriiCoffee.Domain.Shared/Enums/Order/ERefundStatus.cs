namespace MoriiCoffee.Domain.Shared.Enums.Order;

/// <summary>
/// Lifecycle status for a single refund record against a paid Payment.
/// Refunds at Stripe are asynchronous; the local row stays <see cref="Pending"/> until the
/// <c>charge.refunded</c> webhook arrives and flips it to <see cref="Succeeded"/>.
/// </summary>
public enum ERefundStatus
{
    /// <summary>The refund has been accepted by Stripe but not yet confirmed via webhook.</summary>
    Pending = 1,

    /// <summary>The refund has settled at Stripe (charge.refunded webhook received).</summary>
    Succeeded = 2,

    /// <summary>The refund failed (Stripe reversed or denied it).</summary>
    Failed = 3
}
