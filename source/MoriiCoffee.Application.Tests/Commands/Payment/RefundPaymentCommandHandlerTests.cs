using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using MoriiCoffee.Application.Commands.Payment.RefundPayment;
using MoriiCoffee.Application.SeedWork.Abstractions;
using MoriiCoffee.Application.SeedWork.Exceptions;
using MoriiCoffee.Domain.Aggregates.OrderAggregate.Entities;
using MoriiCoffee.Domain.Aggregates.OrderAggregate.ValueObjects;
using MoriiCoffee.Domain.Repositories;
using MoriiCoffee.Domain.SeedWork.Persistence;
using MoriiCoffee.Domain.Shared.Enums.Order;
using Xunit;
using OrderEntity = MoriiCoffee.Domain.Aggregates.OrderAggregate.Order;
using PaymentEntity = MoriiCoffee.Domain.Aggregates.PaymentAggregate.Payment;

namespace MoriiCoffee.Application.Tests.Commands.Payment;

/// <summary>Unit tests for <see cref="RefundPaymentCommandHandler"/>.</summary>
public class RefundPaymentCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IOrderRepository> _ordersRepo = new();
    private readonly Mock<IPaymentRepository> _paymentsRepo = new();
    private readonly Mock<IPaymentGateway> _gateway = new();
    private readonly RefundPaymentCommandHandler _handler;

    public RefundPaymentCommandHandlerTests()
    {
        _unitOfWork.Setup(u => u.Orders).Returns(_ordersRepo.Object);
        _unitOfWork.Setup(u => u.Payments).Returns(_paymentsRepo.Object);
        _unitOfWork
            .Setup(u => u.ExecuteInTransactionAsync(It.IsAny<Func<Task>>()))
            .Returns<Func<Task>>(fn => fn());

        _handler = new RefundPaymentCommandHandler(
            _unitOfWork.Object,
            _gateway.Object,
            NullLogger<RefundPaymentCommandHandler>.Instance);
    }

    [Fact]
    public async Task Handle_OrderNotFound_Throws()
    {
        _ordersRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((OrderEntity?)null);

        var act = () => _handler.Handle(
            new RefundPaymentCommand { OrderId = Guid.NewGuid(), AdminUserId = Guid.NewGuid() },
            CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_OrderHasNoSucceededPayment_ThrowsBadRequest()
    {
        var order = BuildPaidOrder();
        _ordersRepo.Setup(r => r.GetByIdAsync(order.Id)).ReturnsAsync(order);
        _paymentsRepo.Setup(r => r.GetLatestSucceededByOrderIdAsync(order.Id))
            .ReturnsAsync((PaymentEntity?)null);

        var act = () => _handler.Handle(
            new RefundPaymentCommand { OrderId = order.Id, AdminUserId = Guid.NewGuid() },
            CancellationToken.None);

        await act.Should().ThrowAsync<BadRequestException>()
            .WithMessage("*no successful Stripe payment*");
    }

    [Fact]
    public async Task Handle_FullRefund_HappyPath()
    {
        var order = BuildPaidOrder();
        var payment = BuildSucceededPayment(order.Id, amount: 100_000m);

        _ordersRepo.Setup(r => r.GetByIdAsync(order.Id)).ReturnsAsync(order);
        _paymentsRepo.Setup(r => r.GetLatestSucceededByOrderIdAsync(order.Id)).ReturnsAsync(payment);

        _gateway
            .Setup(g => g.CreateRefundAsync(It.IsAny<RefundRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RefundResult { RefundId = "re_test_full", Status = "pending" });

        var result = await _handler.Handle(
            new RefundPaymentCommand
            {
                OrderId = order.Id,
                AdminUserId = Guid.NewGuid(),
                Amount = null // null → full refund of remaining balance
            },
            CancellationToken.None);

        result.StripeRefundId.Should().Be("re_test_full");
        result.Amount.Should().Be(100_000m);
        result.Status.Should().Be(ERefundStatus.Pending);
        result.PaymentStatus.Should().Be(EPaymentStatus.Refunded); // optimistic full
        payment.Refunds.Should().ContainSingle();
    }

    [Fact]
    public async Task Handle_PartialRefund_OptimisticPartiallyRefunded()
    {
        var order = BuildPaidOrder();
        var payment = BuildSucceededPayment(order.Id, amount: 100_000m);

        _ordersRepo.Setup(r => r.GetByIdAsync(order.Id)).ReturnsAsync(order);
        _paymentsRepo.Setup(r => r.GetLatestSucceededByOrderIdAsync(order.Id)).ReturnsAsync(payment);

        _gateway
            .Setup(g => g.CreateRefundAsync(It.IsAny<RefundRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RefundResult { RefundId = "re_test_partial", Status = "pending" });

        var result = await _handler.Handle(
            new RefundPaymentCommand
            {
                OrderId = order.Id,
                AdminUserId = Guid.NewGuid(),
                Amount = 40_000m
            },
            CancellationToken.None);

        result.Amount.Should().Be(40_000m);
        result.PaymentStatus.Should().Be(EPaymentStatus.PartiallyRefunded);
    }

    [Fact]
    public async Task Handle_TwoPartialRefundsThatExhaustBalance_ReportsRefunded()
    {
        var order = BuildPaidOrder();
        var payment = BuildSucceededPayment(order.Id, amount: 100_000m);
        payment.AddRefund(
            MoriiCoffee.Domain.Aggregates.PaymentAggregate.Entities.RefundRecord.Create(
                payment.Id,
                "re_existing",
                40_000m,
                Guid.NewGuid(),
                "first partial"));

        _ordersRepo.Setup(r => r.GetByIdAsync(order.Id)).ReturnsAsync(order);
        _paymentsRepo.Setup(r => r.GetLatestSucceededByOrderIdAsync(order.Id)).ReturnsAsync(payment);

        _gateway
            .Setup(g => g.CreateRefundAsync(It.IsAny<RefundRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RefundResult { RefundId = "re_test_final", Status = "pending" });

        var result = await _handler.Handle(
            new RefundPaymentCommand
            {
                OrderId = order.Id,
                AdminUserId = Guid.NewGuid(),
                Amount = 60_000m
            },
            CancellationToken.None);

        result.Amount.Should().Be(60_000m);
        result.PaymentStatus.Should().Be(EPaymentStatus.Refunded);
        payment.Refunds.Should().HaveCount(2);
    }

    [Fact]
    public async Task Handle_AmountExceedsBalance_ThrowsBadRequest()
    {
        var order = BuildPaidOrder();
        var payment = BuildSucceededPayment(order.Id, amount: 100_000m);

        _ordersRepo.Setup(r => r.GetByIdAsync(order.Id)).ReturnsAsync(order);
        _paymentsRepo.Setup(r => r.GetLatestSucceededByOrderIdAsync(order.Id)).ReturnsAsync(payment);

        var act = () => _handler.Handle(
            new RefundPaymentCommand
            {
                OrderId = order.Id,
                AdminUserId = Guid.NewGuid(),
                Amount = 200_000m // way more than balance
            },
            CancellationToken.None);

        await act.Should().ThrowAsync<BadRequestException>()
            .WithMessage("*exceeds the remaining*");

        _gateway.Verify(g => g.CreateRefundAsync(It.IsAny<RefundRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_GatewayThrows_NoLocalRefundPersisted()
    {
        var order = BuildPaidOrder();
        var payment = BuildSucceededPayment(order.Id, amount: 100_000m);

        _ordersRepo.Setup(r => r.GetByIdAsync(order.Id)).ReturnsAsync(order);
        _paymentsRepo.Setup(r => r.GetLatestSucceededByOrderIdAsync(order.Id)).ReturnsAsync(payment);

        _gateway
            .Setup(g => g.CreateRefundAsync(It.IsAny<RefundRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Stripe down"));

        var act = () => _handler.Handle(
            new RefundPaymentCommand { OrderId = order.Id, AdminUserId = Guid.NewGuid() },
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
        payment.Refunds.Should().BeEmpty();
    }

    // ─── Helpers ──────────────────────────────────────────────────────────

    private static OrderEntity BuildPaidOrder()
    {
        var order = OrderEntity.Create(
            "MRC-20260514-201",
            Guid.NewGuid(),
            new DeliveryInfo("Test", "0900000000", "1 Test St"),
            new[] { OrderItem.Create(Guid.NewGuid(), "Cà phê sữa", 100_000m, 1) },
            EPaymentMethod.STRIPE);
        order.MarkPaymentPaid("pi_paid", "ch_paid");
        return order;
    }

    private static PaymentEntity BuildSucceededPayment(Guid orderId, decimal amount)
    {
        var payment = PaymentEntity.Create(orderId, "cs_test_refund", amount, "vnd");
        payment.MarkSucceeded("pi_paid", "ch_paid");
        return payment;
    }
}
