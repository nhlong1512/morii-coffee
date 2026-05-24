using MoriiCoffee.Application.SeedWork.DTOs.Shipping;
using MoriiCoffee.Domain.SeedWork.Query;

namespace MoriiCoffee.Application.Queries.Shipping.GetShipmentByOrderId;

public record GetShipmentByOrderIdQuery(Guid OrderId, Guid RequestingUserId, bool IsAdmin) : IQuery<ShipmentSummaryDto?>;
