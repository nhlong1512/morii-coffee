using MoriiCoffee.Application.SeedWork.DTOs.Shipping;
using MoriiCoffee.Domain.SeedWork.Command;

namespace MoriiCoffee.Application.Commands.Shipping.SyncShipment;

public class SyncShipmentCommand : ICommand<ShipmentSummaryDto>
{
    public Guid OrderId { get; set; }
}
