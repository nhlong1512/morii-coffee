using Microsoft.EntityFrameworkCore;
using MoriiCoffee.Domain.SeedWork.Persistence;
using MoriiCoffee.Domain.SeedWork.Query;
using OrderSummaryDto = MoriiCoffee.Application.SeedWork.DTOs.Order.OrderSummaryDto;

namespace MoriiCoffee.Application.Queries.Order.GetMyOrders;

/// <summary>
/// Handles <see cref="GetMyOrdersQuery"/> by returning all non-deleted orders for the requesting user,
/// optionally filtered by status and ordered by creation date descending.
/// </summary>
public class GetMyOrdersQueryHandler : IQueryHandler<GetMyOrdersQuery, List<OrderSummaryDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>Initialises the handler with its required dependency.</summary>
    public GetMyOrdersQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    /// <inheritdoc />
    public async Task<List<OrderSummaryDto>> Handle(GetMyOrdersQuery request, CancellationToken cancellationToken)
    {
        var query = _unitOfWork.Orders
            .FindByCondition(o => !o.IsDeleted && o.UserId == request.UserId, false);

        if (request.Status.HasValue)
        {
            query = query.Where(o => o.OrderStatus == request.Status.Value);
        }

        var orders = await query
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync(cancellationToken);

        var orderIds = orders.Select(o => o.Id).ToList();
        var shipmentMap = orderIds.Count == 0
            ? new Dictionary<Guid, MoriiCoffee.Domain.Aggregates.ShippingAggregate.Shipment>()
            : await _unitOfWork.Shipments
                .FindByCondition(s => !s.IsDeleted && orderIds.Contains(s.OrderId), false)
                .ToDictionaryAsync(s => s.OrderId, cancellationToken);

        return orders.Select(o => new OrderSummaryDto
        {
            Id = o.Id,
            OrderNumber = o.OrderNumber,
            Total = o.Total,
            OrderStatus = o.OrderStatus,
            PaymentMethod = o.PaymentMethod,
            DeliveryMethod = o.DeliveryMethod,
            ShippingProvider = o.ShippingProvider,
            ShipmentStatus = shipmentMap.GetValueOrDefault(o.Id)?.Status,
            ShipmentStatusLabel = shipmentMap.GetValueOrDefault(o.Id)?.StatusLabel,
            CreatedAt = o.CreatedAt,
            UpdatedAt = o.UpdatedAt
        }).ToList();
    }
}
