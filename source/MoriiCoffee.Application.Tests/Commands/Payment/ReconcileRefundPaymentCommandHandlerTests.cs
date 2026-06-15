using FluentAssertions;
using Moq;
using MoriiCoffee.Application.Commands.Payment.ReconcileRefundPayment;
using MoriiCoffee.Application.SeedWork.Abstractions;
using MoriiCoffee.Domain.Aggregates.OrderAggregate.Entities;
using MoriiCoffee.Domain.Aggregates.OrderAggregate.ValueObjects;
using MoriiCoffee.Domain.Repositories;
using MoriiCoffee.Domain.SeedWork.Persistence;
using MoriiCoffee.Domain.Shared.Enums.Order;
using Xunit;
using OrderEntity = MoriiCoffee.Domain.Aggregates.OrderAggregate.Order;
using PaymentEntity = MoriiCoffee.Domain.Aggregates.PaymentAggregate.Payment;
using RefundRecordEntity = MoriiCoffee.Domain.Aggregates.PaymentAggregate.Entities.RefundRecord;

namespace MoriiCoffee.Application.Tests.Commands.Payment;

public class ReconcileRefundPaymentCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IOrderRepository> _ordersRepo = new();
    private readonly Mock<IPaymentRepository> _paymentsRepo = new();
    private readonly Mock<IPaymentGateway> _gateway = new();
    private readonly ReconcileRefundPaymentCommandHandler _handler;

    public ReconcileRefundPaymentCommandHandlerTests()
    {
        _unitOfWork.Setup(u => u.Orders).Returns(_ordersRepo.Object);
        _unitOfWork.Setup(u => u.Payments).Returns(_paymentsRepo.Object);
        _paymentsRepo
            .Setup(r => r.CreateRefundAsync(It.IsAny<RefundRecordEntity>()))
            .Returns(Task.CompletedTask);

        _handler = new ReconcileRefundPaymentCommandHandler(
            _unitOfWork.Object,
            BuildResolver());
    }

    private IPaymentGatewayResolver BuildResolver()
    {
        var resolver = new Mock<IPaymentGatewayResolver>();
        resolver.Setup(r => r.Resolve(It.IsAny<EPaymentProvider>())).Returns(_gateway.Object);
        return resolver.Object;
    }

    [Fact]
    public async Task Handle_WhenStripeAlreadyRefunded_ImportsRefund_AndMarksOrderRefunded()
    {
        var order = BuildPaidOrder();
        var payment = BuildSucceededPayment(order.Id, 100_000m);

        _ordersRepo.Setup(r => r.GetByIdAsync(order.Id)).ReturnsAsync(order);
        _paymentsRepo.Setup(r => r.GetLatestSucceededByOrderIdAsync(order.Id)).ReturnsAsync(payment);
        _gateway
            .Setup(g => g.GetPaymentStatusAsync(payment.StripePaymentIntentId!, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PaymentProviderStatusResult
            {
                PaymentIntentId = payment.StripePaymentIntentId!,
                ChargeId = "ch_paid",
                AmountRefunded = 100_000,
                Refunds =
                [
                    new ProviderRefundStatusResult
                    {
                        RefundId = "re_existing_full",
                        Amount = 100_000,
                        Status = "succeeded"
                    }
                ]
            });

        var result = await _handler.Handle(
            new ReconcileRefundPaymentCommand
            {
                OrderId = order.Id,
                AdminUserId = Guid.NewGuid()
            },
            CancellationToken.None);

        result.PaymentStatus.Should().Be(EPaymentStatus.Refunded);
        result.LatestRefundStatus.Should().Be(ERefundStatus.Succeeded);
        result.Reconciled.Should().BeTrue();
        result.ReconciledRefundCount.Should().Be(1);
        payment.Refunds.Should().ContainSingle(r => r.StripeRefundId == "re_existing_full");
        _unitOfWork.Verify(u => u.CommitAsync(), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenNoProviderChanges_ReturnsCurrentStateWithoutCommit()
    {
        var order = BuildPaidOrder();
        var payment = BuildSucceededPayment(order.Id, 100_000m);

        _ordersRepo.Setup(r => r.GetByIdAsync(order.Id)).ReturnsAsync(order);
        _paymentsRepo.Setup(r => r.GetLatestSucceededByOrderIdAsync(order.Id)).ReturnsAsync(payment);
        _gateway
            .Setup(g => g.GetPaymentStatusAsync(payment.StripePaymentIntentId!, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PaymentProviderStatusResult
            {
                PaymentIntentId = payment.StripePaymentIntentId!,
                ChargeId = "ch_paid",
                AmountRefunded = 0,
                Refunds = []
            });

        var result = await _handler.Handle(
            new ReconcileRefundPaymentCommand
            {
                OrderId = order.Id,
                AdminUserId = Guid.NewGuid()
            },
            CancellationToken.None);

        result.PaymentStatus.Should().Be(EPaymentStatus.Paid);
        result.LatestRefundStatus.Should().BeNull();
        result.Reconciled.Should().BeFalse();
        result.ReconciledRefundCount.Should().Be(0);
        _unitOfWork.Verify(u => u.CommitAsync(), Times.Never);
    }

    private static OrderEntity BuildPaidOrder()
    {
        var order = OrderEntity.Create(
            "MRC-20260521-301",
            Guid.NewGuid(),
            new DeliveryInfo("Test", "0900000000", "1 Test St"),
            new[] { OrderItem.Create(Guid.NewGuid(), "Ca phe sua", 100_000m, 1) },
            EPaymentMethod.STRIPE);
        order.MarkPaymentPaid("pi_paid", "ch_paid");
        return order;
    }

    private static PaymentEntity BuildSucceededPayment(Guid orderId, decimal amount)
    {
        var payment = PaymentEntity.Create(orderId, "cs_test_refund_reconcile", amount, "vnd");
        payment.MarkSucceeded("pi_paid", "ch_paid");
        return payment;
    }
}
