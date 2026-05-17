using MoriiCoffee.Application.SeedWork.DTOs.Payment;
using MoriiCoffee.Domain.SeedWork.Command;

namespace MoriiCoffee.Application.Commands.Payment.CreateCheckoutSession;

/// <summary>
/// Command issued by a customer who has just placed an order with <c>PaymentMethod = STRIPE</c>
/// and now needs the redirect URL to the Stripe-hosted Checkout page.
/// </summary>
public class CreateCheckoutSessionCommand : ICommand<CheckoutSessionResponseDto>
{
    /// <summary>The Order to create the session for.</summary>
    public Guid OrderId { get; set; }

    /// <summary>Id of the calling user (set by the controller from the JWT NameIdentifier claim).</summary>
    public Guid UserId { get; set; }
}
