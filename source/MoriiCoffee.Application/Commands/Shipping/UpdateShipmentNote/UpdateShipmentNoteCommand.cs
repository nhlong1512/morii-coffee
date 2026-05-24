using MoriiCoffee.Application.SeedWork.DTOs.Shipping;
using MoriiCoffee.Domain.SeedWork.Command;

namespace MoriiCoffee.Application.Commands.Shipping.UpdateShipmentNote;

public class UpdateShipmentNoteCommand : ICommand<ShipmentSummaryDto>
{
    public Guid OrderId { get; set; }

    public string Note { get; set; } = null!;
}
