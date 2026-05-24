using MoriiCoffee.Application.SeedWork.DTOs.Shipping;
using MoriiCoffee.Domain.SeedWork.Command;
using MoriiCoffee.Domain.Shared.Enums.Order;
using MoriiCoffee.Domain.Shared.Enums.Shipping;

namespace MoriiCoffee.Application.Commands.Shipping.CreateShippingQuote;

public class CreateShippingQuoteCommand : ICommand<ShippingQuoteDto?>
{
    public Guid UserId { get; set; }

    public EDeliveryMethod DeliveryMethod { get; set; } = EDeliveryMethod.GHN_DELIVERY;

    public EPaymentMethod PaymentMethod { get; set; } = EPaymentMethod.COD;

    public ShippingAddressDto Address { get; set; } = new();

    public int? SelectedServiceId { get; set; }
}
