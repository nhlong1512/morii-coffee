using MoriiCoffee.Domain.Aggregates.ShippingAggregate;
using MoriiCoffee.Domain.SeedWork.Persistence;

namespace MoriiCoffee.Domain.Repositories;

public interface IShipmentRepository : IRepositoryBase<Shipment>
{
    Task<Shipment?> GetByOrderIdAsync(Guid orderId);

    Task<Shipment?> GetByClientOrderCodeAsync(string clientOrderCode);

    Task<Shipment?> GetByProviderOrderCodeAsync(string providerOrderCode);
}
