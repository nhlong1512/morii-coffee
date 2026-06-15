using FluentAssertions;
using Moq;
using MoriiCoffee.Application.SeedWork.Abstractions;
using MoriiCoffee.Domain.Shared.Enums.Order;
using MoriiCoffee.Infrastructure.Services.Payment;
using Xunit;

namespace MoriiCoffee.Application.Tests.Infrastructure.Payment;

public class PaymentGatewayResolverTests
{
    [Fact]
    public void Resolve_RoutesByProvider()
    {
        var stripe = new Mock<IPaymentGateway>();
        stripe.SetupGet(x => x.Provider).Returns(EPaymentProvider.Stripe);
        var resolver = new PaymentGatewayResolver([stripe.Object]);

        resolver.Resolve(EPaymentProvider.Stripe).Should().BeSameAs(stripe.Object);
        var act = () => resolver.Resolve(EPaymentProvider.Vnpay);
        act.Should().Throw<NotSupportedException>();
    }
}
