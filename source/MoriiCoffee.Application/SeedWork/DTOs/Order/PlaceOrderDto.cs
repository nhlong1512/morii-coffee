using MoriiCoffee.Domain.Shared.Enums.Order;
using MoriiCoffee.Domain.Shared.Enums.Shipping;

namespace MoriiCoffee.Application.SeedWork.DTOs.Order;

/// <summary>
/// Request payload sent by the client to place a new order from the current cart.
/// </summary>
public class PlaceOrderDto
{
    /// <summary>Recipient's full name for delivery.</summary>
    public string FullName { get; set; } = null!;

    /// <summary>Recipient's contact phone number.</summary>
    public string PhoneNumber { get; set; } = null!;

    /// <summary>Full street address for delivery.</summary>
    public string Address { get; set; } = null!;

    public int? ProvinceId { get; set; }

    public string? ProvinceName { get; set; }

    public int? DistrictId { get; set; }

    public string? DistrictName { get; set; }

    public string? WardCode { get; set; }

    public string? WardName { get; set; }

    /// <summary>Optional order notes (e.g., "leave at front door").</summary>
    public string? Notes { get; set; }

    /// <summary>Payment method chosen by the customer (e.g., COD, MOMO, PAYPAL).</summary>
    public EPaymentMethod PaymentMethod { get; set; }

    public EDeliveryMethod DeliveryMethod { get; set; } = EDeliveryMethod.GHN_DELIVERY;

    public string? ShippingQuoteFingerprint { get; set; }

    public int? ShippingServiceId { get; set; }

    public int? ShippingServiceTypeId { get; set; }

    public string? ShippingServiceLabel { get; set; }

    public decimal? ShippingFee { get; set; }

    public DateTime? ShippingQuoteExpiresAt { get; set; }

    public string? ShippingProviderEnvironment { get; set; }

    /// <summary>
     /// When <c>true</c>, the provided delivery info is saved as the user's default delivery profile.
    /// </summary>
    public bool SaveDeliveryProfile { get; set; }
}
