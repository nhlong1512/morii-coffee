using MoriiCoffee.Application.SeedWork.DTOs.Shipping;
using MoriiCoffee.Application.SeedWork.Exceptions;
using MoriiCoffee.Application.Services.Shipping;
using MoriiCoffee.Domain.SeedWork.Command;
using MoriiCoffee.Domain.SeedWork.Persistence;

namespace MoriiCoffee.Application.Commands.Shipping.CreateShipment;

public class CreateShipmentCommandHandler : ICommandHandler<CreateShipmentCommand, ShipmentSummaryDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ShipmentLifecycleService _shipmentLifecycleService;

    public CreateShipmentCommandHandler(IUnitOfWork unitOfWork, ShipmentLifecycleService shipmentLifecycleService)
    {
        _unitOfWork = unitOfWork;
        _shipmentLifecycleService = shipmentLifecycleService;
    }

    public async Task<ShipmentSummaryDto> Handle(CreateShipmentCommand request, CancellationToken cancellationToken)
    {
        var order = await _unitOfWork.Orders.GetByIdWithItemsAsync(request.OrderId)
            ?? throw new NotFoundException("Order", request.OrderId);

        var shipment = await _shipmentLifecycleService.TryCreateForOrderAsync(order, cancellationToken)
            ?? throw new BadRequestException("Only GHN delivery orders can create shipments.");

        return ShipmentLifecycleService.ToSummaryDto(shipment);
    }
}
