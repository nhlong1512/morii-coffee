using MoriiCoffee.Application.SeedWork.DTOs.Shipping;
using MoriiCoffee.Application.SeedWork.Exceptions;
using MoriiCoffee.Application.Services.Shipping;
using MoriiCoffee.Domain.SeedWork.Command;
using MoriiCoffee.Domain.SeedWork.Persistence;

namespace MoriiCoffee.Application.Commands.Shipping.RequoteShipment;

public class RequoteShipmentCommandHandler : ICommandHandler<RequoteShipmentCommand, ShippingQuoteDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ShipmentLifecycleService _shipmentLifecycleService;

    public RequoteShipmentCommandHandler(IUnitOfWork unitOfWork, ShipmentLifecycleService shipmentLifecycleService)
    {
        _unitOfWork = unitOfWork;
        _shipmentLifecycleService = shipmentLifecycleService;
    }

    public async Task<ShippingQuoteDto> Handle(RequoteShipmentCommand request, CancellationToken cancellationToken)
    {
        var order = await _unitOfWork.Orders.GetByIdWithItemsAsync(request.OrderId)
            ?? throw new NotFoundException("Order", request.OrderId);

        return await _shipmentLifecycleService.RequoteAsync(order, cancellationToken);
    }
}
