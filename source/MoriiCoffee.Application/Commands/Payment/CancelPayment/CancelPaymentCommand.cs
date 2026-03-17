using MoriiCoffee.Domain.SeedWork.Command;

namespace MoriiCoffee.Application.Commands.Payment.CancelPayment;

/// <summary>Cancels a pending payment. Verifies the payment belongs to the calling user.</summary>
public class CancelPaymentCommand : ICommand<bool>
{
    public CancelPaymentCommand(Guid paymentId, Guid userId)
    {
        PaymentId = paymentId;
        UserId = userId;
    }

    public Guid PaymentId { get; }
    public Guid UserId { get; }
}
