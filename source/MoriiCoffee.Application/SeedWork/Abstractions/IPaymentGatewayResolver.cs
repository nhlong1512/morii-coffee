using MoriiCoffee.Domain.Shared.Enums.Order;

namespace MoriiCoffee.Application.SeedWork.Abstractions;

public interface IPaymentGatewayResolver
{
    IPaymentGateway Resolve(EPaymentProvider provider);
}
