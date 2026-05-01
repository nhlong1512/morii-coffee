using MoriiCoffee.Application.SeedWork.Exceptions;
using MoriiCoffee.Domain.SeedWork.Persistence;
using MoriiCoffee.Domain.SeedWork.Query;
using OrderDto = MoriiCoffee.Application.SeedWork.DTOs.Order.OrderDto;
using OrderEntity = MoriiCoffee.Domain.Aggregates.OrderAggregate.Order;
using OrderItemDto = MoriiCoffee.Application.SeedWork.DTOs.Order.OrderItemDto;

namespace MoriiCoffee.Application.Queries.Order.GetOrderById;

/// <summary>
/// Handles <see cref="GetOrderByIdQuery"/> by loading the order with its items and enforcing
/// ownership rules for non-admin callers.
/// </summary>
public class GetOrderByIdQueryHandler : IQueryHandler<GetOrderByIdQuery, OrderDto>
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>Initialises the handler with its required dependency.</summary>
    public GetOrderByIdQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    /// <inheritdoc />
    public async Task<OrderDto> Handle(GetOrderByIdQuery request, CancellationToken cancellationToken)
    {
        var order = await _unitOfWork.Orders.GetByIdWithItemsAsync(request.OrderId);

        if (order is null)
            throw new NotFoundException("Order", request.OrderId);

        if (!request.IsAdmin && order.UserId != request.RequestingUserId)
            throw new UnauthorizedException("You are not authorized to view this order.");

        return MapToDto(order);
    }

    /// <summary>Maps an <see cref="OrderEntity"/> aggregate to its full <see cref="OrderDto"/> representation.</summary>
    private static OrderDto MapToDto(OrderEntity order) => new()
    {
        Id = order.Id,
        OrderNumber = order.OrderNumber,
        UserId = order.UserId,
        DeliveryFullName = order.DeliveryInfo.FullName,
        DeliveryPhoneNumber = order.DeliveryInfo.PhoneNumber,
        DeliveryAddress = order.DeliveryInfo.Address,
        Notes = order.Notes,
        PaymentMethod = order.PaymentMethod,
        Subtotal = order.Subtotal,
        Tax = order.Tax,
        Shipping = order.Shipping,
        Discount = order.Discount,
        Total = order.Total,
        OrderStatus = order.OrderStatus,
        CreatedAt = order.CreatedAt,
        UpdatedAt = order.UpdatedAt,
        Items = order.Items.Select(i => new OrderItemDto
        {
            Id = i.Id,
            ProductId = i.ProductId,
            ProductName = i.ProductName,
            VariantId = i.VariantId,
            VariantLabel = i.VariantLabel,
            UnitPrice = i.UnitPrice,
            Quantity = i.Quantity,
            LineTotal = i.LineTotal
        }).ToList()
    };
}
