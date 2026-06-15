using MoriiCoffee.Application.SeedWork.Abstractions;
using MoriiCoffee.Domain.Shared.Enums.Order;

namespace MoriiCoffee.Infrastructure.Services.Payment;

public sealed class PaymentGatewayResolver : IPaymentGatewayResolver
{
    private readonly IReadOnlyDictionary<EPaymentProvider, IPaymentGateway> _gateways;

    public PaymentGatewayResolver(IEnumerable<IPaymentGateway> gateways)
    {
        _gateways = gateways.ToDictionary(gateway => gateway.Provider);
    }

    public IPaymentGateway Resolve(EPaymentProvider provider) =>
        _gateways.TryGetValue(provider, out var gateway)
            ? gateway
            : throw new NotSupportedException($"Payment provider '{provider}' is not registered.");
}
