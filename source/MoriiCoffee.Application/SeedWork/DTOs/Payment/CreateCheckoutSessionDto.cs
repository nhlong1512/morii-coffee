using MoriiCoffee.Domain.Shared.Enums.Shipping;

namespace MoriiCoffee.Application.SeedWork.DTOs.Payment;

/// <summary>Request body for <c>POST /api/v1/payments/checkout-session</c>.</summary>
public class CreateCheckoutSessionDto
{
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
}
