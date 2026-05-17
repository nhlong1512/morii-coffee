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
/// Unit tests for <see cref="HandleWebhookEventCommandHandler"/> focused on the
/// <c>checkout.session.completed</c> event (US1 acceptance scenario 1 + 2.1).
/// </summary>
public class HandleWebhookEventCompletedTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IOrderRepository> _ordersRepo = new();
    private readonly Mock<IPaymentRepository> _paymentsRepo = new();
    private readonly Mock<IPaymentWebhookEventRepository> _webhookRepo = new();
    private readonly HandleWebhookEventCommandHandler _handler;

    public HandleWebhookEventCompletedTests()
    {
        _unitOfWork.Setup(u => u.Orders).Returns(_ordersRepo.Object);
        _unitOfWork.Setup(u => u.Payments).Returns(_paymentsRepo.Object);
        _unitOfWork.Setup(u => u.PaymentWebhookEvents).Returns(_webhookRepo.Object);
        _unitOfWork.Setup(u => u.CommitAsync()).ReturnsAsync(1);

        // First-time insert succeeds by default; tests override for duplicate.
        _webhookRepo.Setup(r => r.TryInsertAsync(It.IsAny<PaymentWebhookEvent>())).ReturnsAsync(true);

        _handler = new HandleWebhookEventCommandHandler(
            _unitOfWork.Object,
            NullLogger<HandleWebhookEventCommandHandler>.Instance);
    }

    [Fact]
    public async Task Handle_CheckoutSessionCompleted_FlipsPaymentAndOrderToPaid()
    {
        var orderUserId = Guid.NewGuid();
        var order = BuildStripeOrder(orderUserId);
        var payment = PaymentEntity.Create(order.Id, "cs_test_a", 45_000m, "vnd");

        _paymentsRepo.Setup(r => r.GetBySessionIdAsync("cs_test_a")).ReturnsAsync(payment);
        _ordersRepo.Setup(r => r.GetByIdAsync(order.Id)).ReturnsAsync(order);

        var command = new HandleWebhookEventCommand
        {
            Envelope = new WebhookEventEnvelope
            {
                EventId = "evt_1",
                EventType = "checkout.session.completed",
                RawBody = "{}",
                ProviderSessionId = "cs_test_a",
                ProviderPaymentId = "pi_test_a",
                ProviderChargeId = "ch_test_a",
                MetadataOrderId = order.Id,
                MetadataPaymentId = payment.Id
            }
        };

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Result.Should().Be(EPaymentWebhookProcessingResult.Processed);

        payment.Status.Should().Be(EPaymentTransactionStatus.Succeeded);
        payment.StripePaymentIntentId.Should().Be("pi_test_a");
        payment.StripeChargeId.Should().Be("ch_test_a");

        order.PaymentStatus.Should().Be(EPaymentStatus.Paid);
        order.StripePaymentIntentId.Should().Be("pi_test_a");

        _ordersRepo.Verify(r => r.Update(order), Times.Once);
        _paymentsRepo.Verify(r => r.Update(payment), Times.Once);
    }

    [Fact]
    public async Task Handle_UnknownSession_ReturnsOrderNotFound()
    {
        _paymentsRepo.Setup(r => r.GetBySessionIdAsync(It.IsAny<string>()))
            .ReturnsAsync((PaymentEntity?)null);

        var command = new HandleWebhookEventCommand
        {
            Envelope = new WebhookEventEnvelope
            {
                EventId = "evt_2",
                EventType = "checkout.session.completed",
                RawBody = "{}",
                ProviderSessionId = "cs_unknown",
                ProviderPaymentId = "pi_x"
            }
        };

        var result = await _handler.Handle(command, CancellationToken.None);
        result.Result.Should().Be(EPaymentWebhookProcessingResult.OrderNotFound);
    }

    private static OrderEntity BuildStripeOrder(Guid userId)
    {
        return OrderEntity.Create(
            "MRC-20260514-003",
            userId,
            new DeliveryInfo("Test", "0900000000", "1 Test St"),
            new[] { OrderItem.Create(Guid.NewGuid(), "Cà phê sữa", 45_000m, 1) },
            EPaymentMethod.STRIPE);
    }
}
