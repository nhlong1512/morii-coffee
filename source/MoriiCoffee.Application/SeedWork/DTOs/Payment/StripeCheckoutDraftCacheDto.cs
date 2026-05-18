using MoriiCoffee.Application.SeedWork.DTOs.Cart;
using MoriiCoffee.Domain.Shared.Enums.Order;

namespace MoriiCoffee.Application.SeedWork.DTOs.Payment;

/// <summary>
/// Cached snapshot of a Stripe checkout attempt before a local order exists.
/// </summary>
public class StripeCheckoutDraftCacheDto
{
    public Guid DraftId { get; set; }

    public Guid UserId { get; set; }

    public string FullName { get; set; } = null!;

    public string PhoneNumber { get; set; } = null!;

    public string Address { get; set; } = null!;

    public string? Notes { get; set; }

    public bool SaveDeliveryProfile { get; set; }

    public List<CartItemDto> Items { get; set; } = [];

    public decimal Amount { get; set; }

    public string Currency { get; set; } = "vnd";

    public string SessionId { get; set; } = null!;

    public DateTime ExpiresAtUtc { get; set; }

    public EPaymentStatus PaymentStatus { get; set; } = EPaymentStatus.Pending;

    public string? FailureReason { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
