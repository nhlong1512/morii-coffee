using MoriiCoffee.Domain.SeedWork.Query;
using MoriiCoffee.Domain.Shared.Enums.Order;
using OrderSummaryDto = MoriiCoffee.Application.SeedWork.DTOs.Order.OrderSummaryDto;

namespace MoriiCoffee.Application.Queries.Order.GetMyOrders;

/// <summary>
/// Query to retrieve a summary list of orders belonging to the authenticated user.
/// An optional status filter narrows results to a specific lifecycle phase.
/// </summary>
public record GetMyOrdersQuery(Guid UserId, EOrderStatus? Status) : IQuery<List<OrderSummaryDto>>;
