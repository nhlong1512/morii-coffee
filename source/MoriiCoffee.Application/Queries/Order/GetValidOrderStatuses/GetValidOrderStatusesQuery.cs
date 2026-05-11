using MoriiCoffee.Domain.SeedWork.Query;
using MoriiCoffee.Domain.Shared.Enums.Order;

namespace MoriiCoffee.Application.Queries.Order.GetValidOrderStatuses;

/// <summary>
/// Query to retrieve the list of valid next statuses for a given order.
/// Only accessible to admins.
/// </summary>
public record GetValidOrderStatusesQuery(Guid OrderId) : IQuery<List<EOrderStatus>>;
