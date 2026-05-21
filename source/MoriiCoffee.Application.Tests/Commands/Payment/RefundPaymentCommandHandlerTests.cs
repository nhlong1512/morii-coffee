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
        _paymentsRepo
            .Setup(r => r.CreateRefundAsync(It.IsAny<MoriiCoffee.Domain.Aggregates.PaymentAggregate.Entities.RefundRecord>()))
            .Returns(Task.CompletedTask);

        _handler = new RefundPaymentCommandHandler(
            _unitOfWork.Object,
            _gateway.Object,
            NullLogger<RefundPaymentCommandHandler>.Instance);

        _gateway
            .Setup(g => g.GetPaymentStatusAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string paymentIntentId, CancellationToken _) => new PaymentProviderStatusResult
            {
                PaymentIntentId = paymentIntentId,
                ChargeId = "ch_paid",
                AmountRefunded = 0,
                Refunds = []
            });
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
        _paymentsRepo.Verify(r => r.CreateRefundAsync(It.IsAny<MoriiCoffee.Domain.Aggregates.PaymentAggregate.Entities.RefundRecord>()), Times.Once);
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

    [Fact]
    public async Task Handle_StripeAlreadyFullyRefunded_SynchronizesLocalState_AndReturnsSuccess()
    {
        var order = BuildPaidOrder();
        var payment = BuildSucceededPayment(order.Id, amount: 100_000m);

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
                        RefundId = "re_synced_full",
                        Amount = 100_000,
                        Status = "succeeded"
                    }
                ]
            });

        var result = await _handler.Handle(
            new RefundPaymentCommand
            {
                OrderId = order.Id,
                AdminUserId = Guid.NewGuid()
            },
            CancellationToken.None);

        result.StripeRefundId.Should().Be("re_synced_full");
        result.Amount.Should().Be(100_000m);
        result.Status.Should().Be(ERefundStatus.Succeeded);
        result.PaymentStatus.Should().Be(EPaymentStatus.Refunded);
        payment.Refunds.Should().ContainSingle(r => r.StripeRefundId == "re_synced_full");
        _gateway.Verify(
            g => g.CreateRefundAsync(It.IsAny<RefundRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_StripeAlreadyPartiallyRefunded_SynchronizesThenRefundsRemainingBalance()
    {
        var order = BuildPaidOrder();
        var payment = BuildSucceededPayment(order.Id, amount: 100_000m);

        _ordersRepo.Setup(r => r.GetByIdAsync(order.Id)).ReturnsAsync(order);
        _paymentsRepo.Setup(r => r.GetLatestSucceededByOrderIdAsync(order.Id)).ReturnsAsync(payment);
        _gateway
            .Setup(g => g.GetPaymentStatusAsync(payment.StripePaymentIntentId!, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PaymentProviderStatusResult
            {
                PaymentIntentId = payment.StripePaymentIntentId!,
                ChargeId = "ch_paid",
                AmountRefunded = 30_000,
                Refunds =
                [
                    new ProviderRefundStatusResult
                    {
                        RefundId = "re_synced_partial",
                        Amount = 30_000,
                        Status = "succeeded"
                    }
                ]
            });
        _gateway
            .Setup(g => g.CreateRefundAsync(It.IsAny<RefundRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RefundResult { RefundId = "re_new_remaining", Status = "pending" });

        var result = await _handler.Handle(
            new RefundPaymentCommand
            {
                OrderId = order.Id,
                AdminUserId = Guid.NewGuid(),
                Amount = null
            },
            CancellationToken.None);

        result.StripeRefundId.Should().Be("re_new_remaining");
        result.Amount.Should().Be(70_000m);
        result.Status.Should().Be(ERefundStatus.Pending);
        result.PaymentStatus.Should().Be(EPaymentStatus.Refunded);
        payment.Refunds.Should().HaveCount(2);
        payment.Refunds.Should().Contain(r => r.StripeRefundId == "re_synced_partial" && r.Status == ERefundStatus.Succeeded);
        payment.Refunds.Should().Contain(r => r.StripeRefundId == "re_new_remaining" && r.Status == ERefundStatus.Pending);
        _paymentsRepo.Verify(
            r => r.CreateRefundAsync(It.IsAny<MoriiCoffee.Domain.Aggregates.PaymentAggregate.Entities.RefundRecord>()),
            Times.Exactly(2));
    }

    [Fact]
    public async Task Handle_CreateRefundHitsAlreadyRefundedRace_SynchronizesLocalState_AndReturnsSuccess()
    {
        var order = BuildPaidOrder();
        var payment = BuildSucceededPayment(order.Id, amount: 100_000m);

        _ordersRepo.Setup(r => r.GetByIdAsync(order.Id)).ReturnsAsync(order);
        _paymentsRepo.Setup(r => r.GetLatestSucceededByOrderIdAsync(order.Id)).ReturnsAsync(payment);
        _gateway
            .SetupSequence(g => g.GetPaymentStatusAsync(payment.StripePaymentIntentId!, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PaymentProviderStatusResult
            {
                PaymentIntentId = payment.StripePaymentIntentId!,
                ChargeId = "ch_paid",
                AmountRefunded = 0,
                Refunds = []
            })
            .ReturnsAsync(new PaymentProviderStatusResult
            {
                PaymentIntentId = payment.StripePaymentIntentId!,
                ChargeId = "ch_paid",
                AmountRefunded = 100_000,
                Refunds =
                [
                    new ProviderRefundStatusResult
                    {
                        RefundId = "re_race_full",
                        Amount = 100_000,
                        Status = "succeeded"
                    }
                ]
            });
        _gateway
            .Setup(g => g.CreateRefundAsync(It.IsAny<RefundRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new PaymentGatewayAlreadyRefundedException("already refunded"));

        var result = await _handler.Handle(
            new RefundPaymentCommand
            {
                OrderId = order.Id,
                AdminUserId = Guid.NewGuid()
            },
            CancellationToken.None);

        result.StripeRefundId.Should().Be("re_race_full");
        result.Status.Should().Be(ERefundStatus.Succeeded);
        result.PaymentStatus.Should().Be(EPaymentStatus.Refunded);
        payment.Refunds.Should().ContainSingle(r => r.StripeRefundId == "re_race_full");
        _paymentsRepo.Verify(r => r.CreateRefundAsync(It.IsAny<MoriiCoffee.Domain.Aggregates.PaymentAggregate.Entities.RefundRecord>()), Times.Once);
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
