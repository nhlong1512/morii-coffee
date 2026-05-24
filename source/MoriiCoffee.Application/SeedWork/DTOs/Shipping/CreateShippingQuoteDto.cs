using MoriiCoffee.Domain.Shared.Enums.Order;
using MoriiCoffee.Domain.Shared.Enums.Shipping;

namespace MoriiCoffee.Application.SeedWork.DTOs.Shipping;

public class CreateShippingQuoteDto
{
    public EDeliveryMethod DeliveryMethod { get; set; } = EDeliveryMethod.GHN_DELIVERY;

    public EPaymentMethod PaymentMethod { get; set; } = EPaymentMethod.COD;

    public ShippingAddressDto Address { get; set; } = new();

    public int? SelectedServiceId { get; set; }
}
