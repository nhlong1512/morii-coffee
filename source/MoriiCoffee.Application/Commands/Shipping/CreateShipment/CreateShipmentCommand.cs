using MoriiCoffee.Application.SeedWork.DTOs.Shipping;
using MoriiCoffee.Domain.SeedWork.Command;

namespace MoriiCoffee.Application.Commands.Shipping.CreateShipment;

public class CreateShipmentCommand : ICommand<ShipmentSummaryDto>
{
    public Guid OrderId { get; set; }
}
