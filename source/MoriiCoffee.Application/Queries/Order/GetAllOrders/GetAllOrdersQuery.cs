using MoriiCoffee.Domain.SeedWork.Query;
using MoriiCoffee.Domain.Shared.Enums.Order;
using OrderSummaryDto = MoriiCoffee.Application.SeedWork.DTOs.Order.OrderSummaryDto;

namespace MoriiCoffee.Application.Queries.Order.GetAllOrders;

/// <summary>
/// Admin query to retrieve a summary list of all orders in the system.
/// Supports optional filtering by status and/or user.
/// </summary>
public record GetAllOrdersQuery(EOrderStatus? Status, Guid? UserId) : IQuery<List<OrderSummaryDto>>;
