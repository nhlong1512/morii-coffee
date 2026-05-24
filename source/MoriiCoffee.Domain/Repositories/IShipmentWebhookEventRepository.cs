using MoriiCoffee.Domain.Aggregates.ShippingAggregate;
using MoriiCoffee.Domain.SeedWork.Persistence;

namespace MoriiCoffee.Domain.Repositories;

public interface IShipmentWebhookEventRepository : IRepositoryBase<ShipmentWebhookEvent>
{
    Task<bool> ExistsAsync(string? providerEventId, string? providerOrderCode, string? clientOrderCode, string eventType);
}
