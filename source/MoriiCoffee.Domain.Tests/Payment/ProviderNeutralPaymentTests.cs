using FluentAssertions;
using MoriiCoffee.Domain.Shared.Enums.Order;
using Xunit;

namespace MoriiCoffee.Domain.Tests.Payment;

public class ProviderNeutralPaymentTests
{
    [Fact]
    public void Create_PersistsProviderOwnership()
    {
        var payment = Domain.Aggregates.PaymentAggregate.Payment.Create(
            Guid.NewGuid(), "txn-ref", 100000, "vnd", provider: EPaymentProvider.Vnpay);

        payment.Provider.Should().Be(EPaymentProvider.Vnpay);
    }
}
