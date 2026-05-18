using MoriiCoffee.Application.SeedWork.DTOs.Payment;
using MoriiCoffee.Domain.SeedWork.Command;

namespace MoriiCoffee.Application.Commands.Payment.CreateCheckoutSession;

/// <summary>
/// Command issued by a customer who wants to start a Stripe checkout directly from the current cart.
/// No order is created yet; the backend stores a checkout draft and finalizes it only after Stripe
/// confirms payment.
/// </summary>
public class CreateCheckoutSessionCommand : ICommand<CheckoutSessionResponseDto>
{
    /// <summary>Id of the calling user (set by the controller from the JWT NameIdentifier claim).</summary>
    public Guid UserId { get; set; }

    public string FullName { get; set; } = null!;

    public string PhoneNumber { get; set; } = null!;

    public string Address { get; set; } = null!;

    public string? Notes { get; set; }

    public bool SaveDeliveryProfile { get; set; }
}
