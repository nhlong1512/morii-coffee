using MoriiCoffee.Application.SeedWork.DTOs.Shipping;
using MoriiCoffee.Application.SeedWork.Exceptions;
using MoriiCoffee.Application.Services.Shipping;
using MoriiCoffee.Domain.SeedWork.Command;
using MoriiCoffee.Domain.SeedWork.Persistence;

namespace MoriiCoffee.Application.Commands.Shipping.CancelShipment;

public class CancelShipmentCommandHandler : ICommandHandler<CancelShipmentCommand, ShipmentSummaryDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ShipmentLifecycleService _shipmentLifecycleService;

    public CancelShipmentCommandHandler(IUnitOfWork unitOfWork, ShipmentLifecycleService shipmentLifecycleService)
    {
        _unitOfWork = unitOfWork;
        _shipmentLifecycleService = shipmentLifecycleService;
    }

    public async Task<ShipmentSummaryDto> Handle(CancelShipmentCommand request, CancellationToken cancellationToken)
    {
        var shipment = await _unitOfWork.Shipments.GetByOrderIdAsync(request.OrderId)
            ?? throw new NotFoundException("Shipment", request.OrderId);

        var cancelled = await _shipmentLifecycleService.CancelAsync(shipment, cancellationToken);
        return ShipmentLifecycleService.ToSummaryDto(cancelled);
    }
}
