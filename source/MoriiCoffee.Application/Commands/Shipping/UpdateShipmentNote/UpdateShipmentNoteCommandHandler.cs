using MoriiCoffee.Application.SeedWork.DTOs.Shipping;
using MoriiCoffee.Application.SeedWork.Exceptions;
using MoriiCoffee.Application.Services.Shipping;
using MoriiCoffee.Domain.SeedWork.Command;
using MoriiCoffee.Domain.SeedWork.Persistence;

namespace MoriiCoffee.Application.Commands.Shipping.UpdateShipmentNote;

public class UpdateShipmentNoteCommandHandler : ICommandHandler<UpdateShipmentNoteCommand, ShipmentSummaryDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ShipmentLifecycleService _shipmentLifecycleService;

    public UpdateShipmentNoteCommandHandler(IUnitOfWork unitOfWork, ShipmentLifecycleService shipmentLifecycleService)
    {
        _unitOfWork = unitOfWork;
        _shipmentLifecycleService = shipmentLifecycleService;
    }

    public async Task<ShipmentSummaryDto> Handle(UpdateShipmentNoteCommand request, CancellationToken cancellationToken)
    {
        var shipment = await _unitOfWork.Shipments.GetByOrderIdAsync(request.OrderId)
            ?? throw new NotFoundException("Shipment", request.OrderId);

        var updated = await _shipmentLifecycleService.UpdateNoteAsync(shipment, request.Note, cancellationToken);
        return ShipmentLifecycleService.ToSummaryDto(updated);
    }
}
