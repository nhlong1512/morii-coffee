using MoriiCoffee.Application.SeedWork.DTOs.Shipping;
using MoriiCoffee.Application.SeedWork.Exceptions;
using MoriiCoffee.Application.Services.Shipping;
using MoriiCoffee.Domain.SeedWork.Persistence;
using MoriiCoffee.Domain.SeedWork.Query;

namespace MoriiCoffee.Application.Queries.Shipping.GetShipmentByOrderId;

public class GetShipmentByOrderIdQueryHandler : IQueryHandler<GetShipmentByOrderIdQuery, ShipmentSummaryDto?>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetShipmentByOrderIdQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ShipmentSummaryDto?> Handle(GetShipmentByOrderIdQuery request, CancellationToken cancellationToken)
    {
        var order = await _unitOfWork.Orders.GetByIdAsync(request.OrderId)
            ?? throw new NotFoundException("Order", request.OrderId);

        if (!request.IsAdmin && order.UserId != request.RequestingUserId)
            throw new UnauthorizedException("You are not authorized to view this shipment.");

        var shipment = await _unitOfWork.Shipments.GetByOrderIdAsync(request.OrderId);
        return shipment is null ? null : ShipmentLifecycleService.ToSummaryDto(shipment);
    }
}
