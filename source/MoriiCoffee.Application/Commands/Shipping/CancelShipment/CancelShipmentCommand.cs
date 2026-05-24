using MoriiCoffee.Application.SeedWork.DTOs.Shipping;
using MoriiCoffee.Domain.SeedWork.Command;

namespace MoriiCoffee.Application.Commands.Shipping.CancelShipment;

public class CancelShipmentCommand : ICommand<ShipmentSummaryDto>
{
    public Guid OrderId { get; set; }
}
