using MoriiCoffee.Application.SeedWork.DTOs.Payment;
using MoriiCoffee.Domain.SeedWork.Command;

namespace MoriiCoffee.Application.Commands.Payment.ReconcileRefundPayment;

/// <summary>
/// Re-checks Stripe refund state for an order's latest successful payment and synchronizes local
/// refund rows plus order payment status.
/// </summary>
public class ReconcileRefundPaymentCommand : ICommand<RefundReconcileResponseDto>
{
    public Guid OrderId { get; set; }

    public Guid AdminUserId { get; set; }
}
