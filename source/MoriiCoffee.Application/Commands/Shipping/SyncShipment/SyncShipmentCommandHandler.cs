using MoriiCoffee.Application.SeedWork.DTOs.Shipping;
using MoriiCoffee.Application.SeedWork.Exceptions;
using MoriiCoffee.Application.Services.Shipping;
using MoriiCoffee.Domain.SeedWork.Command;
using MoriiCoffee.Domain.SeedWork.Persistence;

namespace MoriiCoffee.Application.Commands.Shipping.SyncShipment;

public class SyncShipmentCommandHandler : ICommandHandler<SyncShipmentCommand, ShipmentSummaryDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ShipmentLifecycleService _shipmentLifecycleService;

    public SyncShipmentCommandHandler(IUnitOfWork unitOfWork, ShipmentLifecycleService shipmentLifecycleService)
    {
        _unitOfWork = unitOfWork;
        _shipmentLifecycleService = shipmentLifecycleService;
    }

    public async Task<ShipmentSummaryDto> Handle(SyncShipmentCommand request, CancellationToken cancellationToken)
    {
        var shipment = await _unitOfWork.Shipments.GetByOrderIdAsync(request.OrderId)
            ?? throw new NotFoundException("Shipment", request.OrderId);

        var synced = await _shipmentLifecycleService.SyncAsync(shipment, cancellationToken);
        return ShipmentLifecycleService.ToSummaryDto(synced);
    }
}
