using FluentAssertions;
using Moq;
using MoriiCoffee.Application.Commands.Payment.ReconcileStripePayment;
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

public class ReconcileStripePaymentCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IOrderRepository> _ordersRepo = new();
    private readonly Mock<IPaymentRepository> _paymentsRepo = new();
    private readonly Mock<IPaymentGateway> _paymentGateway = new();
    private readonly ReconcileStripePaymentCommandHandler _handler;

    public ReconcileStripePaymentCommandHandlerTests()
    {
        _unitOfWork.Setup(u => u.Orders).Returns(_ordersRepo.Object);
        _unitOfWork.Setup(u => u.Payments).Returns(_paymentsRepo.Object);
        _unitOfWork.Setup(u => u.CommitAsync()).ReturnsAsync(1);

        _handler = new ReconcileStripePaymentCommandHandler(
            _unitOfWork.Object,
            _paymentGateway.Object);
    }

    [Fact]
    public async Task Handle_PaidSession_FlipsLocalPaymentAndOrderToPaid()
    {
        var userId = Guid.NewGuid();
        var order = BuildStripeOrder(userId);
        var payment = PaymentEntity.Create(order.Id, "cs_paid", 45_000m, "vnd");

        _ordersRepo.Setup(r => r.GetByIdAsync(order.Id)).ReturnsAsync(order);
        _paymentsRepo.Setup(r => r.GetLatestPendingByOrderIdAsync(order.Id)).ReturnsAsync(payment);
        _paymentsRepo.Setup(r => r.ListByOrderIdAsync(order.Id)).ReturnsAsync([payment]);
        _paymentGateway
            .Setup(g => g.GetCheckoutSessionStatusAsync("cs_paid", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CheckoutSessionStatusResult
            {
                SessionId = "cs_paid",
                State = CheckoutSessionState.Paid,
                PaymentIntentId = "pi_paid",
                ChargeId = "ch_paid"
            });

        var result = await _handler.Handle(new ReconcileStripePaymentCommand
        {
            OrderId = order.Id,
            RequestingUserId = userId,
            IsAdmin = false
        }, CancellationToken.None);

        result.PaymentStatus.Should().Be(EPaymentStatus.Paid);
        result.Payments.Single().Status.Should().Be(EPaymentTransactionStatus.Succeeded);
        result.Payments.Single().StripePaymentIntentId.Should().Be("pi_paid");
        payment.Status.Should().Be(EPaymentTransactionStatus.Succeeded);
        order.PaymentStatus.Should().Be(EPaymentStatus.Paid);

        _paymentsRepo.Verify(r => r.Update(payment), Times.Once);
        _ordersRepo.Verify(r => r.Update(order), Times.Once);
        _unitOfWork.Verify(u => u.CommitAsync(), Times.Once);
    }

    [Fact]
    public async Task Handle_ExpiredSession_MarksLocalStateAsFailed()
    {
        var userId = Guid.NewGuid();
        var order = BuildStripeOrder(userId);
        var payment = PaymentEntity.Create(order.Id, "cs_expired", 45_000m, "vnd");

        _ordersRepo.Setup(r => r.GetByIdAsync(order.Id)).ReturnsAsync(order);
        _paymentsRepo.Setup(r => r.GetLatestPendingByOrderIdAsync(order.Id)).ReturnsAsync(payment);
        _paymentsRepo.Setup(r => r.ListByOrderIdAsync(order.Id)).ReturnsAsync([payment]);
        _paymentGateway
            .Setup(g => g.GetCheckoutSessionStatusAsync("cs_expired", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CheckoutSessionStatusResult
            {
                SessionId = "cs_expired",
                State = CheckoutSessionState.Expired
            });

        var result = await _handler.Handle(new ReconcileStripePaymentCommand
        {
            OrderId = order.Id,
            RequestingUserId = userId,
            IsAdmin = false
        }, CancellationToken.None);

        result.PaymentStatus.Should().Be(EPaymentStatus.Failed);
        result.Payments.Single().Status.Should().Be(EPaymentTransactionStatus.Expired);
        payment.Status.Should().Be(EPaymentTransactionStatus.Expired);
        order.PaymentStatus.Should().Be(EPaymentStatus.Failed);
    }

    [Fact]
    public async Task Handle_ForeignOrder_ThrowsUnauthorized()
    {
        var order = BuildStripeOrder(Guid.NewGuid());
        _ordersRepo.Setup(r => r.GetByIdAsync(order.Id)).ReturnsAsync(order);

        var act = () => _handler.Handle(new ReconcileStripePaymentCommand
        {
            OrderId = order.Id,
            RequestingUserId = Guid.NewGuid(),
            IsAdmin = false
        }, CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedException>();
    }

    private static OrderEntity BuildStripeOrder(Guid userId)
    {
        return OrderEntity.Create(
            "MRC-20260518-009",
            userId,
            new DeliveryInfo("Test", "0900000000", "1 Test St"),
            [OrderItem.Create(Guid.NewGuid(), "A-Mê", 45_000m, 1)],
            EPaymentMethod.STRIPE);
    }
}
