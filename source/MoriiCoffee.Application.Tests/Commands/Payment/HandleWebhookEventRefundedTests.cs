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
/// US3 acceptance scenario 3.1 + 3.2: <c>charge.refunded</c> webhook flips local RefundRecord
/// to Succeeded and Order to Refunded / PartiallyRefunded based on cumulative amount.
/// </summary>
public class HandleWebhookEventRefundedTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IOrderRepository> _ordersRepo = new();
    private readonly Mock<IPaymentRepository> _paymentsRepo = new();
    private readonly Mock<IPaymentWebhookEventRepository> _webhookRepo = new();
    private readonly Mock<IStripeCheckoutDraftService> _draftService = new();
    private readonly HandleWebhookEventCommandHandler _handler;

    public HandleWebhookEventRefundedTests()
    {
        _unitOfWork.Setup(u => u.Orders).Returns(_ordersRepo.Object);
        _unitOfWork.Setup(u => u.Payments).Returns(_paymentsRepo.Object);
        _unitOfWork.Setup(u => u.PaymentWebhookEvents).Returns(_webhookRepo.Object);
        _unitOfWork.Setup(u => u.CommitAsync()).ReturnsAsync(1);
        _webhookRepo.Setup(r => r.TryInsertAsync(It.IsAny<PaymentWebhookEvent>())).ReturnsAsync(true);

        _handler = new HandleWebhookEventCommandHandler(
            _unitOfWork.Object,
            _draftService.Object,
            NullLogger<HandleWebhookEventCommandHandler>.Instance);
    }

    [Fact]
    public async Task Handle_ChargeRefunded_FullAmount_FlipsOrderToRefunded()
    {
        var order = BuildPaidOrder(amount: 100_000m);
        var payment = PaymentEntity.Create(order.Id, "cs_test_ref", 100_000m, "vnd");
        payment.MarkSucceeded("pi_paid", "ch_paid");
        var refund = RefundRecord.Create(payment.Id, "re_test_a", 100_000m, Guid.NewGuid(), "test");
        payment.AddRefund(refund);

        _paymentsRepo.Setup(r => r.GetByPaymentIntentIdAsync("pi_paid")).ReturnsAsync(payment);
        _ordersRepo.Setup(r => r.GetByIdAsync(order.Id)).ReturnsAsync(order);

        var command = new HandleWebhookEventCommand
        {
            Envelope = new WebhookEventEnvelope
            {
                EventId = "evt_ref_full",
                EventType = "charge.refunded",
                RawBody = "{}",
                ProviderPaymentId = "pi_paid",
                ProviderChargeId = "ch_paid",
                AmountRefunded = 100_000,
                ProviderRefundIds = new[] { "re_test_a" }
            }
        };

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Result.Should().Be(EPaymentWebhookProcessingResult.Processed);
        refund.Status.Should().Be(ERefundStatus.Succeeded);
        order.PaymentStatus.Should().Be(EPaymentStatus.Refunded);
    }

    [Fact]
    public async Task Handle_ChargeRefunded_PartialAmount_FlipsOrderToPartiallyRefunded()
    {
        var order = BuildPaidOrder(amount: 100_000m);
        var payment = PaymentEntity.Create(order.Id, "cs_test_ref2", 100_000m, "vnd");
        payment.MarkSucceeded("pi_paid", "ch_paid");
        var refund = RefundRecord.Create(payment.Id, "re_partial", 30_000m, Guid.NewGuid(), "test");
        payment.AddRefund(refund);

        _paymentsRepo.Setup(r => r.GetByPaymentIntentIdAsync("pi_paid")).ReturnsAsync(payment);
        _ordersRepo.Setup(r => r.GetByIdAsync(order.Id)).ReturnsAsync(order);

        var command = new HandleWebhookEventCommand
        {
            Envelope = new WebhookEventEnvelope
            {
                EventId = "evt_ref_partial",
                EventType = "charge.refunded",
                RawBody = "{}",
                ProviderPaymentId = "pi_paid",
                AmountRefunded = 30_000,
                ProviderRefundIds = new[] { "re_partial" }
            }
        };

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Result.Should().Be(EPaymentWebhookProcessingResult.Processed);
        refund.Status.Should().Be(ERefundStatus.Succeeded);
        order.PaymentStatus.Should().Be(EPaymentStatus.PartiallyRefunded);
    }

    private static OrderEntity BuildPaidOrder(decimal amount)
    {
        var order = OrderEntity.Create(
            "MRC-20260514-301",
            Guid.NewGuid(),
            new DeliveryInfo("Test", "0900000000", "1 Test St"),
            new[] { OrderItem.Create(Guid.NewGuid(), "Cà phê sữa", amount, 1) },
            EPaymentMethod.STRIPE);
        order.MarkPaymentPaid("pi_paid", "ch_paid");
        return order;
    }
}
