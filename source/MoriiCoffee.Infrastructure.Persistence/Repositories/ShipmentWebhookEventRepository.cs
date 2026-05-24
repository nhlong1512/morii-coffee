using Microsoft.EntityFrameworkCore;
using MoriiCoffee.Domain.Aggregates.ShippingAggregate;
using MoriiCoffee.Domain.Repositories;
using MoriiCoffee.Infrastructure.Persistence.Data;
using MoriiCoffee.Infrastructure.Persistence.SeedWork.Repository;

namespace MoriiCoffee.Infrastructure.Persistence.Repositories;

public class ShipmentWebhookEventRepository : RepositoryBase<ShipmentWebhookEvent>, IShipmentWebhookEventRepository
{
    private readonly ApplicationDbContext _context;

    public ShipmentWebhookEventRepository(ApplicationDbContext context) : base(context)
    {
        _context = context;
    }

    public Task<bool> ExistsAsync(string? providerEventId, string? providerOrderCode, string? clientOrderCode, string eventType)
    {
        if (!string.IsNullOrWhiteSpace(providerEventId))
        {
            return _context.ShipmentWebhookEvents.AnyAsync(x =>
                !x.IsDeleted &&
                x.EventType == eventType &&
                x.ProviderEventId == providerEventId);
        }

        return _context.ShipmentWebhookEvents.AnyAsync(x =>
            !x.IsDeleted &&
            x.EventType == eventType &&
            (
                (!string.IsNullOrWhiteSpace(providerOrderCode) && x.ProviderOrderCode == providerOrderCode) ||
                (!string.IsNullOrWhiteSpace(clientOrderCode) && x.ClientOrderCode == clientOrderCode)
            ));
    }
}
