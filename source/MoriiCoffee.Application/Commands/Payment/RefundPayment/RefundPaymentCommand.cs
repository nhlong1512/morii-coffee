using MoriiCoffee.Application.SeedWork.DTOs.Payment;
using MoriiCoffee.Domain.SeedWork.Command;

namespace MoriiCoffee.Application.Commands.Payment.RefundPayment;

/// <summary>
/// Admin-issued refund against a paid order. The handler validates remaining balance, calls
/// Stripe, and persists a <c>RefundRecord</c> in <c>Pending</c>; the <c>charge.refunded</c>
/// webhook later flips it to <c>Succeeded</c> and updates the Order's payment status.
/// </summary>
public class RefundPaymentCommand : ICommand<RefundResponseDto>
{
    /// <summary>Order being refunded.</summary>
    public Guid OrderId { get; set; }

    /// <summary>Calling admin's user id (recorded for audit).</summary>
    public Guid AdminUserId { get; set; }

    /// <summary>
    /// Refund amount in VND. Null / zero / not provided means "full refund of remaining unrefunded
    /// balance".
    /// </summary>
    public decimal? Amount { get; set; }

    /// <summary>Optional reason recorded on the refund row + forwarded to Stripe metadata.</summary>
    public string? Reason { get; set; }
}
