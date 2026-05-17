using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using MoriiCoffee.Application.Commands.Payment.HandleWebhookEvent;
using MoriiCoffee.Application.SeedWork.Abstractions;
using MoriiCoffee.Domain.Aggregates.OrderAggregate.Entities;
using MoriiCoffee.Domain.Aggregates.OrderAggregate.ValueObjects;
using MoriiCoffee.Domain.Aggregates.PaymentAggregate.Entities;
using MoriiCoffee.Domain.Repositories;
using MoriiCoffee.Domain.SeedWork.Persistence;
using MoriiCoffee.Domain.Shared.Enums.Order;
using Xunit;
using OrderEntity = MoriiCoffee.Domain.Aggregates.OrderAggregate.Order;
using PaymentEntity = MoriiCoffee.Domain.Aggregates.PaymentAggregate.Payment;

namespace MoriiCoffee.Application.Tests.Commands.Payment;

/// <summary>
/// US2 acceptance scenario 2.1: <c>checkout.session.expired</c> → Order PaymentStatus becomes Failed.
/// </summary>
public class HandleWebhookEventExpiredTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IOrderRepository> _ordersRepo = new();
    private readonly Mock<IPaymentRepository> _paymentsRepo = new();
    private readonly Mock<IPaymentWebhookEventRepository> _webhookRepo = new();
    private readonly HandleWebhookEventCommandHandler _handler;

    public HandleWebhookEventExpiredTests()
    {
        _unitOfWork.Setup(u => u.Orders).Returns(_ordersRepo.Object);
        _unitOfWork.Setup(u => u.Payments).Returns(_paymentsRepo.Object);
        _unitOfWork.Setup(u => u.PaymentWebhookEvents).Returns(_webhookRepo.Object);
        _unitOfWork.Setup(u => u.CommitAsync()).ReturnsAsync(1);
        _webhookRepo.Setup(r => r.TryInsertAsync(It.IsAny<PaymentWebhookEvent>())).ReturnsAsync(true);

        _handler = new HandleWebhookEventCommandHandler(
            _unitOfWork.Object,
            NullLogger<HandleWebhookEventCommandHandler>.Instance);
    }

    [Fact]
    public async Task Handle_CheckoutSessionExpired_FlipsPaymentExpiredAndOrderFailed()
    {
        var order = BuildStripeOrder();
        var payment = PaymentEntity.Create(order.Id, "cs_test_exp", order.Total, "vnd");

        _paymentsRepo.Setup(r => r.GetBySessionIdAsync("cs_test_exp")).ReturnsAsync(payment);
        _ordersRepo.Setup(r => r.GetByIdAsync(order.Id)).ReturnsAsync(order);

        var command = new HandleWebhookEventCommand
        {
            Envelope = new WebhookEventEnvelope
            {
                EventId = "evt_expired",
                EventType = "checkout.session.expired",
                RawBody = "{}",
                ProviderSessionId = "cs_test_exp"
            }
        };

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Result.Should().Be(EPaymentWebhookProcessingResult.Processed);
        payment.Status.Should().Be(EPaymentTransactionStatus.Expired);
        order.PaymentStatus.Should().Be(EPaymentStatus.Failed);
    }

    [Fact]
    public async Task Handle_ExpiredAfterSucceeded_DoesNotRegressState()
    {
        var order = BuildStripeOrder();
        order.MarkPaymentPaid("pi_existing", "ch_existing"); // order is already Paid

        var payment = PaymentEntity.Create(order.Id, "cs_test_exp2", order.Total, "vnd");
        payment.MarkSucceeded("pi_existing", "ch_existing");

        _paymentsRepo.Setup(r => r.GetBySessionIdAsync("cs_test_exp2")).ReturnsAsync(payment);
        // We do NOT set up Orders.GetByIdAsync because if the payment is already Succeeded the
        // handler must short-circuit and not touch the Order at all.

        var command = new HandleWebhookEventCommand
        {
            Envelope = new WebhookEventEnvelope
            {
                EventId = "evt_expired_late",
                EventType = "checkout.session.expired",
                RawBody = "{}",
                ProviderSessionId = "cs_test_exp2"
            }
        };

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Result.Should().Be(EPaymentWebhookProcessingResult.Processed);
        payment.Status.Should().Be(EPaymentTransactionStatus.Succeeded); // unchanged
        order.PaymentStatus.Should().Be(EPaymentStatus.Paid); // unchanged
    }

    private static OrderEntity BuildStripeOrder()
    {
        return OrderEntity.Create(
            "MRC-20260514-101",
            Guid.NewGuid(),
            new DeliveryInfo("Test", "0900000000", "1 Test St"),
            new[] { OrderItem.Create(Guid.NewGuid(), "Cà phê sữa", 45_000m, 1) },
            EPaymentMethod.STRIPE);
    }
}
