using MoriiCoffee.Domain.Shared.Enums.Order;

namespace MoriiCoffee.Application.SeedWork.DTOs.Payment;

/// <summary>
/// Customer-facing payment status after reconciling a Stripe checkout session.
/// </summary>
public class ReconcileStripePaymentResponseDto
{
    public Guid? CheckoutDraftId { get; set; }

    public string SessionId { get; set; } = null!;

    public Guid? OrderId { get; set; }

    public string? OrderNumber { get; set; }

    public EPaymentStatus PaymentStatus { get; set; }

    public string? FailureReason { get; set; }

    public DateTime? ExpiresAtUtc { get; set; }
}
