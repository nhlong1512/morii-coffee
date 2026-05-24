using MoriiCoffee.Application.SeedWork.DTOs.Cart;
using MoriiCoffee.Domain.Shared.Enums.Order;
using MoriiCoffee.Domain.Shared.Enums.Shipping;

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

    public int? ProvinceId { get; set; }

    public string? ProvinceName { get; set; }

    public int? DistrictId { get; set; }

    public string? DistrictName { get; set; }

    public string? WardCode { get; set; }

    public string? WardName { get; set; }

    public string? Notes { get; set; }

    public bool SaveDeliveryProfile { get; set; }

    public EDeliveryMethod DeliveryMethod { get; set; } = EDeliveryMethod.GHN_DELIVERY;

    public string? ShippingQuoteFingerprint { get; set; }

    public int? ShippingServiceId { get; set; }

    public int? ShippingServiceTypeId { get; set; }

    public string? ShippingServiceLabel { get; set; }

    public decimal? ShippingFee { get; set; }

    public DateTime? ShippingQuoteExpiresAt { get; set; }

    public string? ShippingProviderEnvironment { get; set; }

    public List<CartItemDto> Items { get; set; } = [];

    public decimal Amount { get; set; }

    public string Currency { get; set; } = "vnd";

    public string SessionId { get; set; } = null!;

    public DateTime ExpiresAtUtc { get; set; }

    public EPaymentStatus PaymentStatus { get; set; } = EPaymentStatus.Pending;

    public string? FailureReason { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
