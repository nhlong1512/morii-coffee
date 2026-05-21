using FluentAssertions;
using Moq;
using MoriiCoffee.Application.Queries.Payment.GetPaymentByOrderId;
using MoriiCoffee.Application.SeedWork.Exceptions;
using MoriiCoffee.Domain.Aggregates.OrderAggregate.Entities;
using MoriiCoffee.Domain.Aggregates.OrderAggregate.ValueObjects;
using MoriiCoffee.Domain.Aggregates.PaymentAggregate.Entities;
using MoriiCoffee.Domain.Repositories;
using MoriiCoffee.Domain.SeedWork.Persistence;
using MoriiCoffee.Domain.Shared.Enums.Order;
using Xunit;
using OrderEntity = MoriiCoffee.Domain.Aggregates.OrderAggregate.Order;
using PaymentEntity = MoriiCoffee.Domain.Aggregates.PaymentAggregate.Payment;

namespace MoriiCoffee.Application.Tests.Queries.Payment;

/// <summary>
/// Unit tests for <see cref="GetPaymentByOrderIdQueryHandler"/>. Verifies authorisation rules
/// (owner / admin / forbidden), shape of the response, and ordering.
/// </summary>
public class GetPaymentByOrderIdQueryHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IOrderRepository> _ordersRepo = new();
    private readonly Mock<IPaymentRepository> _paymentsRepo = new();
    private readonly GetPaymentByOrderIdQueryHandler _handler;

    public GetPaymentByOrderIdQueryHandlerTests()
    {
        _unitOfWork.Setup(u => u.Orders).Returns(_ordersRepo.Object);
        _unitOfWork.Setup(u => u.Payments).Returns(_paymentsRepo.Object);
        _handler = new GetPaymentByOrderIdQueryHandler(_unitOfWork.Object);
    }

    [Fact]
    public async Task Handle_OrderNotFound_ThrowsNotFoundException()
    {
        _ordersRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((OrderEntity?)null);

        var act = () => _handler.Handle(
            new GetPaymentByOrderIdQuery(Guid.NewGuid(), Guid.NewGuid(), isAdmin: false),
            CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_DifferentUserNonAdmin_ThrowsUnauthorized()
    {
        var ownerId = Guid.NewGuid();
        var order = BuildOrder(ownerId);
        _ordersRepo.Setup(r => r.GetByIdAsync(order.Id)).ReturnsAsync(order);

        var act = () => _handler.Handle(
            new GetPaymentByOrderIdQuery(order.Id, Guid.NewGuid(), isAdmin: false),
            CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedException>();
    }

    [Fact]
    public async Task Handle_OwnerCanView_ReturnsPayments()
    {
        var ownerId = Guid.NewGuid();
        var order = BuildOrder(ownerId);
        _ordersRepo.Setup(r => r.GetByIdAsync(order.Id)).ReturnsAsync(order);

        var payment = PaymentEntity.Create(order.Id, "cs_test_owner", 45_000m, "vnd");
        _paymentsRepo.Setup(r => r.ListByOrderIdAsync(order.Id))
            .ReturnsAsync(new[] { payment });

        var result = await _handler.Handle(
            new GetPaymentByOrderIdQuery(order.Id, ownerId, isAdmin: false),
            CancellationToken.None);

        result.OrderId.Should().Be(order.Id);
        result.PaymentStatus.Should().Be(EPaymentStatus.Pending);
        result.Payments.Should().ContainSingle();
        result.Payments[0].StripeSessionId.Should().Be("cs_test_owner");
    }

    [Fact]
    public async Task Handle_AdminCanView_OtherUsersOrder()
    {
        var ownerId = Guid.NewGuid();
        var order = BuildOrder(ownerId);
        _ordersRepo.Setup(r => r.GetByIdAsync(order.Id)).ReturnsAsync(order);
        _paymentsRepo.Setup(r => r.ListByOrderIdAsync(order.Id)).ReturnsAsync(Array.Empty<PaymentEntity>());

        var result = await _handler.Handle(
            new GetPaymentByOrderIdQuery(order.Id, Guid.NewGuid(), isAdmin: true),
            CancellationToken.None);

        result.OrderId.Should().Be(order.Id);
        result.Payments.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_OrderSummaryIsStale_ResolvesRefundedStatusFromPaymentHistory()
    {
        var ownerId = Guid.NewGuid();
        var order = BuildOrder(ownerId);
        order.MarkPaymentPaid("pi_paid", "ch_paid");

        var payment = PaymentEntity.Create(order.Id, "cs_refunded", 45_000m, "vnd");
        payment.MarkSucceeded("pi_paid", "ch_paid");
        payment.AddRefund(RefundRecord.Create(payment.Id, "re_refunded", 45_000m, Guid.NewGuid(), "sync"));
        payment.Refunds.Single().MarkSucceeded();

        _ordersRepo.Setup(r => r.GetByIdAsync(order.Id)).ReturnsAsync(order);
        _paymentsRepo.Setup(r => r.ListByOrderIdAsync(order.Id)).ReturnsAsync([payment]);

        var result = await _handler.Handle(
            new GetPaymentByOrderIdQuery(order.Id, ownerId, isAdmin: false),
            CancellationToken.None);

        result.PaymentStatus.Should().Be(EPaymentStatus.Refunded);
        result.Payments.Single().Refunds.Should().ContainSingle(r => r.StripeRefundId == "re_refunded");
    }

    // ─── Helpers ───────────────────────────────────────────────────────────

    private static OrderEntity BuildOrder(Guid userId)
    {
        return OrderEntity.Create(
            "MRC-20260514-002",
            userId,
            new DeliveryInfo("Test", "0900000000", "1 Test St"),
            new[] { OrderItem.Create(Guid.NewGuid(), "Cà phê sữa", 45_000m, 1) },
            EPaymentMethod.STRIPE);
    }
}
