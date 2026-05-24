using Microsoft.EntityFrameworkCore;
using MoriiCoffee.Domain.Aggregates.ShippingAggregate;
using MoriiCoffee.Domain.Repositories;
using MoriiCoffee.Infrastructure.Persistence.Data;
using MoriiCoffee.Infrastructure.Persistence.SeedWork.Repository;

namespace MoriiCoffee.Infrastructure.Persistence.Repositories;

public class ShipmentRepository : RepositoryBase<Shipment>, IShipmentRepository
{
    private readonly ApplicationDbContext _context;

    public ShipmentRepository(ApplicationDbContext context) : base(context)
    {
        _context = context;
    }

    public Task<Shipment?> GetByOrderIdAsync(Guid orderId)
    {
        return _context.Shipments
            .Where(x => !x.IsDeleted && x.OrderId == orderId)
            .FirstOrDefaultAsync();
    }

    public Task<Shipment?> GetByClientOrderCodeAsync(string clientOrderCode)
    {
        return _context.Shipments
            .Where(x => !x.IsDeleted && x.ClientOrderCode == clientOrderCode)
            .FirstOrDefaultAsync();
    }

    public Task<Shipment?> GetByProviderOrderCodeAsync(string providerOrderCode)
    {
        return _context.Shipments
            .Where(x => !x.IsDeleted && x.ProviderOrderCode == providerOrderCode)
            .FirstOrDefaultAsync();
    }
}
