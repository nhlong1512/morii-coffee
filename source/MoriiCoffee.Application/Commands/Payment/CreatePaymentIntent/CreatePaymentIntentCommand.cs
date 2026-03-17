using MoriiCoffee.Application.SeedWork.DTOs.Payment;
using MoriiCoffee.Domain.SeedWork.Command;

namespace MoriiCoffee.Application.Commands.Payment.CreatePaymentIntent;

/// <summary>Creates a Stripe PaymentIntent and persists a pending Payment record.</summary>
public class CreatePaymentIntentCommand : ICommand<PaymentIntentResultDto>
{
    public CreatePaymentIntentCommand(Guid userId, CreatePaymentIntentDto dto)
    {
        UserId = userId;
        Amount = dto.Amount;
        Currency = dto.Currency.ToLowerInvariant();
        Description = dto.Description;
    }

    public Guid UserId { get; }
    public decimal Amount { get; }
    public string Currency { get; }
    public string? Description { get; }
}
