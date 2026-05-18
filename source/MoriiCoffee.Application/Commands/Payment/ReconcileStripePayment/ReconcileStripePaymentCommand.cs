using MoriiCoffee.Application.SeedWork.DTOs.Payment;
using MoriiCoffee.Domain.SeedWork.Command;

namespace MoriiCoffee.Application.Commands.Payment.ReconcileStripePayment;

/// <summary>
/// Re-checks a Stripe checkout session against the provider and synchronizes the local payment
/// and order state when the webhook has not yet done so.
/// </summary>
public class ReconcileStripePaymentCommand : ICommand<OrderPaymentSummaryDto>
{
    /// <summary>Order to reconcile.</summary>
    public Guid OrderId { get; set; }

    /// <summary>Optional Stripe Checkout Session id from the success redirect.</summary>
    public string? SessionId { get; set; }

    /// <summary>Id of the calling user from the JWT.</summary>
    public Guid RequestingUserId { get; set; }

    /// <summary>True when the caller has the ADMIN role.</summary>
    public bool IsAdmin { get; set; }
}
