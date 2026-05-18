using MoriiCoffee.Domain.Shared.Enums.Order;

namespace MoriiCoffee.Application.SeedWork.DTOs.Payment;

/// <summary>
/// Payment snapshot embedded inside an order response so the UI can render both
/// fulfilment state and payment state without calling a second endpoint.
/// </summary>
public class OrderPaymentInfoDto
{
    /// <summary>Order-level payment lifecycle (Pending / Paid / Failed / Refunded...).</summary>
    public EPaymentStatus PaymentStatus { get; set; }

    /// <summary>Total number of payment attempts recorded for this order.</summary>
    public int AttemptCount { get; set; }

    /// <summary>Internal id of the latest payment attempt, if one exists.</summary>
    public Guid? LatestPaymentId { get; set; }

    /// <summary>Status of the latest payment attempt (Created / Succeeded / Failed / Expired).</summary>
    public EPaymentTransactionStatus? LatestAttemptStatus { get; set; }

    /// <summary>Stripe Checkout Session id of the latest attempt.</summary>
    public string? StripeSessionId { get; set; }

    /// <summary>Stripe PaymentIntent id, when the latest attempt has reached success.</summary>
    public string? StripePaymentIntentId { get; set; }

    /// <summary>Stripe Charge id of the successful payment, when available.</summary>
    public string? StripeChargeId { get; set; }

    /// <summary>Failure reason of the latest attempt, if Stripe marked it failed.</summary>
    public string? FailureReason { get; set; }

    /// <summary>UTC timestamp when the latest attempt row was created.</summary>
    public DateTime? LatestAttemptCreatedAt { get; set; }
}
