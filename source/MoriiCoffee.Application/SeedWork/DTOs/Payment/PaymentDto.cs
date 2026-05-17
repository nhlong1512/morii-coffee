using MoriiCoffee.Domain.Shared.Enums.Order;

namespace MoriiCoffee.Application.SeedWork.DTOs.Payment;

/// <summary>One Payment attempt + its refund children, used by <c>GET /payments/by-order/{orderId}</c>.</summary>
public class PaymentDto
{
    /// <summary>Internal Payment id.</summary>
    public Guid Id { get; set; }

    /// <summary>Stripe Checkout Session id.</summary>
    public string StripeSessionId { get; set; } = null!;

    /// <summary>Stripe PaymentIntent id, when populated.</summary>
    public string? StripePaymentIntentId { get; set; }

    /// <summary>Amount in VND.</summary>
    public decimal Amount { get; set; }

    /// <summary>Lowercase ISO 4217 currency code.</summary>
    public string Currency { get; set; } = null!;

    /// <summary>Per-attempt status (Created / Succeeded / Failed / Expired).</summary>
    public EPaymentTransactionStatus Status { get; set; }

    /// <summary>Free-text failure reason populated on <see cref="EPaymentTransactionStatus.Failed"/>.</summary>
    public string? FailureReason { get; set; }

    /// <summary>UTC creation timestamp.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Refunds attached to this attempt.</summary>
    public List<RefundDto> Refunds { get; set; } = new();
}
