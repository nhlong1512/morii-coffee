using FluentAssertions;
using Moq;
using MoriiCoffee.Application.Commands.Payment.ReconcileStripePayment;
using MoriiCoffee.Application.SeedWork.Abstractions;
using MoriiCoffee.Application.SeedWork.DTOs.Payment;
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
    private readonly Mock<IStripeCheckoutDraftService> _draftService = new();
    private readonly ReconcileStripePaymentCommandHandler _handler;

    public ReconcileStripePaymentCommandHandlerTests()
    {
        _unitOfWork.Setup(u => u.Orders).Returns(_ordersRepo.Object);
        _unitOfWork.Setup(u => u.Payments).Returns(_paymentsRepo.Object);

        _handler = new ReconcileStripePaymentCommandHandler(
            _unitOfWork.Object,
            _paymentGateway.Object,
            _draftService.Object);
    }

    [Fact]
    public async Task Handle_FinalizedSession_ReturnsExistingOrderState()
    {
        var userId = Guid.NewGuid();
        var order = BuildStripeOrder(userId);
        order.MarkPaymentPaid("pi_paid", "ch_paid");
        var payment = PaymentEntity.Create(order.Id, "cs_paid", 45_000m, "vnd");
        payment.MarkSucceeded("pi_paid", "ch_paid");

        _paymentsRepo.Setup(r => r.GetBySessionIdAsync("cs_paid")).ReturnsAsync(payment);
        _ordersRepo.Setup(r => r.GetByIdAsync(order.Id)).ReturnsAsync(order);

        var result = await _handler.Handle(new ReconcileStripePaymentCommand
        {
            SessionId = "cs_paid",
            RequestingUserId = userId,
            IsAdmin = false
        }, CancellationToken.None);

        result.OrderId.Should().Be(order.Id);
        result.OrderNumber.Should().Be(order.OrderNumber);
        result.PaymentStatus.Should().Be(EPaymentStatus.Paid);
        result.SessionId.Should().Be("cs_paid");
    }

    [Fact]
    public async Task Handle_PaidDraft_FinalizesAndReturnsCreatedOrder()
    {
        var draft = new StripeCheckoutDraftCacheDto
        {
            DraftId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            SessionId = "cs_draft_paid",
            ExpiresAtUtc = DateTime.UtcNow.AddHours(1)
        };

        _paymentsRepo.Setup(r => r.GetBySessionIdAsync("cs_draft_paid"))
            .ReturnsAsync((PaymentEntity?)null);
        _draftService.Setup(s => s.GetBySessionIdAsync("cs_draft_paid"))
            .ReturnsAsync(draft);
        _paymentGateway.Setup(g => g.GetCheckoutSessionStatusAsync("cs_draft_paid", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CheckoutSessionStatusResult
            {
                SessionId = "cs_draft_paid",
                State = ECheckoutSessionState.Paid,
                PaymentIntentId = "pi_draft_paid",
                ChargeId = "ch_draft_paid"
            });
        _draftService.Setup(s => s.FinalizeSucceededAsync(
                draft,
                "pi_draft_paid",
                "ch_draft_paid",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FinalizeStripeCheckoutResultDto
            {
                OrderId = Guid.NewGuid(),
                OrderNumber = "MRC-20260518-001",
                PaymentId = Guid.NewGuid(),
                PaymentStatus = EPaymentStatus.Paid,
                SessionId = "cs_draft_paid"
            });

        var result = await _handler.Handle(new ReconcileStripePaymentCommand
        {
            SessionId = "cs_draft_paid",
            RequestingUserId = draft.UserId,
            IsAdmin = false
        }, CancellationToken.None);

        result.OrderId.Should().NotBeNull();
        result.OrderNumber.Should().Be("MRC-20260518-001");
        result.PaymentStatus.Should().Be(EPaymentStatus.Paid);
        result.CheckoutDraftId.Should().Be(draft.DraftId);
    }

    [Fact]
    public async Task Handle_ExpiredDraft_ReturnsFailedWithoutOrder()
    {
        var draft = new StripeCheckoutDraftCacheDto
        {
            DraftId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            SessionId = "cs_draft_expired",
            ExpiresAtUtc = DateTime.UtcNow.AddMinutes(30),
            PaymentStatus = EPaymentStatus.Pending
        };

        _paymentsRepo.Setup(r => r.GetBySessionIdAsync("cs_draft_expired"))
            .ReturnsAsync((PaymentEntity?)null);
        _draftService.Setup(s => s.GetBySessionIdAsync("cs_draft_expired"))
            .ReturnsAsync(draft);
        _paymentGateway.Setup(g => g.GetCheckoutSessionStatusAsync("cs_draft_expired", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CheckoutSessionStatusResult
            {
                SessionId = "cs_draft_expired",
                State = ECheckoutSessionState.Expired
            });

        var result = await _handler.Handle(new ReconcileStripePaymentCommand
        {
            SessionId = "cs_draft_expired",
            RequestingUserId = draft.UserId,
            IsAdmin = false
        }, CancellationToken.None);

        result.OrderId.Should().BeNull();
        result.PaymentStatus.Should().Be(EPaymentStatus.Failed);
        _draftService.Verify(s => s.MarkExpiredAsync(draft), Times.Once);
    }

    [Fact]
    public async Task Handle_ForeignDraft_ThrowsUnauthorized()
    {
        var draft = new StripeCheckoutDraftCacheDto
        {
            DraftId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            SessionId = "cs_foreign"
        };

        _paymentsRepo.Setup(r => r.GetBySessionIdAsync("cs_foreign"))
            .ReturnsAsync((PaymentEntity?)null);
        _draftService.Setup(s => s.GetBySessionIdAsync("cs_foreign"))
            .ReturnsAsync(draft);

        var act = () => _handler.Handle(new ReconcileStripePaymentCommand
        {
            SessionId = "cs_foreign",
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
