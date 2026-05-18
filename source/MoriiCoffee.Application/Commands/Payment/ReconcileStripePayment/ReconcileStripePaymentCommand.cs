using MoriiCoffee.Application.SeedWork.DTOs.Payment;
using MoriiCoffee.Domain.SeedWork.Command;

namespace MoriiCoffee.Application.Commands.Payment.ReconcileStripePayment;

/// <summary>
/// Re-checks a Stripe checkout session against the provider and finalizes the local order when
/// the success redirect reaches the frontend before the webhook does.
/// </summary>
public class ReconcileStripePaymentCommand : ICommand<ReconcileStripePaymentResponseDto>
{
    /// <summary>Optional Stripe Checkout Session id from the success redirect.</summary>
    public string? SessionId { get; set; }

    /// <summary>Optional local checkout draft id if the frontend persisted it before redirecting.</summary>
    public Guid? CheckoutDraftId { get; set; }

    /// <summary>Id of the calling user from the JWT.</summary>
    public Guid RequestingUserId { get; set; }

    /// <summary>True when the caller has the ADMIN role.</summary>
    public bool IsAdmin { get; set; }
}
