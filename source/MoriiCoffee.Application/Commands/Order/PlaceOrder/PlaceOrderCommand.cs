using MoriiCoffee.Domain.SeedWork.Command;
using MoriiCoffee.Domain.Shared.Enums.Order;
using MoriiCoffee.Domain.Shared.Enums.Shipping;
using OrderDto = MoriiCoffee.Application.SeedWork.DTOs.Order.OrderDto;

namespace MoriiCoffee.Application.Commands.Order.PlaceOrder;

/// <summary>
/// Command to place a new order from the authenticated user's current cart.
/// Delivery info is provided inline; the cart is cleared after a successful placement.
/// </summary>
public class PlaceOrderCommand : ICommand<OrderDto>
{
    /// <summary>Identifier of the authenticated user placing the order (set from JWT claims).</summary>
    public Guid UserId { get; set; }

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

    /// <summary>Payment method chosen by the customer.</summary>
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
