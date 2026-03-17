using MoriiCoffee.Application.SeedWork.DTOs.Payment;
using MoriiCoffee.Domain.SeedWork.Command;

namespace MoriiCoffee.Application.Commands.Payment.RefundPayment;

/// <summary>Admin-only command to refund a succeeded payment via Stripe.</summary>
public class RefundPaymentCommand : ICommand<RefundResultDto>
{
    public RefundPaymentCommand(Guid paymentId) => PaymentId = paymentId;
    public Guid PaymentId { get; }
}
