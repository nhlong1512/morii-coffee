using MoriiCoffee.Application.SeedWork.DTOs.Shipping;
using MoriiCoffee.Domain.SeedWork.Command;

namespace MoriiCoffee.Application.Commands.Shipping.RequoteShipment;

public class RequoteShipmentCommand : ICommand<ShippingQuoteDto>
{
    public Guid OrderId { get; set; }
}
