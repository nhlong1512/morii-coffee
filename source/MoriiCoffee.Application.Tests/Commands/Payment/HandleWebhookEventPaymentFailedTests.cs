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

/// <summary>US2 acceptance scenario 2.2: async payment failure marks Payment + Order Failed.</summary>
public class HandleWebhookEventPaymentFailedTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IOrderRepository> _ordersRepo = new();
    private readonly Mock<IPaymentRepository> _paymentsRepo = new();
    private readonly Mock<IPaymentWebhookEventRepository> _webhookRepo = new();
    private readonly HandleWebhookEventCommandHandler _handler;

    public HandleWebhookEventPaymentFailedTests()
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
    public async Task Handle_PaymentIntentFailed_MarksPaymentFailedAndOrderFailed()
    {
        var order = BuildStripeOrder();
        var payment = PaymentEntity.Create(order.Id, "cs_test_pi_failed", order.Total, "vnd");
        // Pretend the PaymentIntent id was populated on session completion attempt.
        // The handler looks up by PI id, so we expose it via the repository mock directly.
        _paymentsRepo.Setup(r => r.GetByPaymentIntentIdAsync("pi_failed")).ReturnsAsync(payment);
        _ordersRepo.Setup(r => r.GetByIdAsync(order.Id)).ReturnsAsync(order);

        var command = new HandleWebhookEventCommand
        {
            Envelope = new WebhookEventEnvelope
            {
                EventId = "evt_pi_failed",
                EventType = "payment_intent.payment_failed",
                RawBody = "{}",
                ProviderPaymentId = "pi_failed",
                FailureReason = "Your card was declined."
            }
        };

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Result.Should().Be(EPaymentWebhookProcessingResult.Processed);
        payment.Status.Should().Be(EPaymentTransactionStatus.Failed);
        payment.FailureReason.Should().Be("Your card was declined.");
        order.PaymentStatus.Should().Be(EPaymentStatus.Failed);
    }

    private static OrderEntity BuildStripeOrder()
    {
        return OrderEntity.Create(
            "MRC-20260514-102",
            Guid.NewGuid(),
            new DeliveryInfo("Test", "0900000000", "1 Test St"),
            new[] { OrderItem.Create(Guid.NewGuid(), "Cà phê sữa", 45_000m, 1) },
            EPaymentMethod.STRIPE);
    }
}
