using MoriiCoffee.Domain.SeedWork.Query;
using OrderDto = MoriiCoffee.Application.SeedWork.DTOs.Order.OrderDto;

namespace MoriiCoffee.Application.Queries.Order.GetOrderById;

/// <summary>
/// Query to retrieve a single order by its ID with full item details.
/// Access is restricted to the order owner unless <paramref name="IsAdmin"/> is <c>true</c>.
/// </summary>
public record GetOrderByIdQuery(Guid OrderId, Guid RequestingUserId, bool IsAdmin) : IQuery<OrderDto>;
