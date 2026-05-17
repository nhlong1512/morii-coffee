using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using MoriiCoffee.Application.Commands.Payment.CreateCheckoutSession;
using MoriiCoffee.Application.SeedWork.Abstractions;
using MoriiCoffee.Application.SeedWork.Exceptions;
using MoriiCoffee.Domain.Aggregates.OrderAggregate.Entities;
using MoriiCoffee.Domain.Aggregates.OrderAggregate.ValueObjects;
using MoriiCoffee.Domain.Repositories;
using MoriiCoffee.Domain.SeedWork.Persistence;
using MoriiCoffee.Domain.Shared.Enums.Order;
using MoriiCoffee.Domain.Shared.Settings;
using Xunit;
using OrderEntity = MoriiCoffee.Domain.Aggregates.OrderAggregate.Order;
using PaymentEntity = MoriiCoffee.Domain.Aggregates.PaymentAggregate.Payment;

namespace MoriiCoffee.Application.Tests.Commands.Payment;

/// <summary>
/// Unit tests for <see cref="CreateCheckoutSessionCommandHandler"/>. Covers US1 acceptance:
/// happy path, COD rejection, non-pending rejection, ownership enforcement, and Stripe
/// failure handling.
/// </summary>
public class CreateCheckoutSessionCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IOrderRepository> _ordersRepo = new();
    private readonly Mock<IPaymentRepository> _paymentsRepo = new();
    private readonly Mock<IPaymentGateway> _gateway = new();

    private readonly StripeSettings _stripeSettings = new()
    {
        SecretKey = "sk_test_dummy",
        PublishableKey = "pk_test_dummy",
        WebhookSigningSecret = "whsec_dummy",
        Currency = "vnd",
        SuccessUrlTemplate = "/checkout/success?session_id={CHECKOUT_SESSION_ID}",
        CancelUrlPath = "/checkout/cancel"
    };

    private readonly EmailSettings _emailSettings = new()
    {
        StorefrontUrl = "http://localhost:3000"
    };

    private readonly CreateCheckoutSessionCommandHandler _handler;

    public CreateCheckoutSessionCommandHandlerTests()
    {
        _unitOfWork.Setup(u => u.Orders).Returns(_ordersRepo.Object);
        _unitOfWork.Setup(u => u.Payments).Returns(_paymentsRepo.Object);
        _unitOfWork
            .Setup(u => u.ExecuteInTransactionAsync(It.IsAny<Func<Task>>()))
            .Returns<Func<Task>>(fn => fn());

        _gateway.SetupGet(g => g.PublishableKey).Returns("pk_test_dummy");

        _handler = new CreateCheckoutSessionCommandHandler(
            _unitOfWork.Object,
            _gateway.Object,
            _stripeSettings,
            _emailSettings,
            NullLogger<CreateCheckoutSessionCommandHandler>.Instance);
    }

    [Fact]
    public async Task Handle_OrderNotFound_ThrowsNotFoundException()
    {
        _ordersRepo.Setup(r => r.GetByIdWithItemsAsync(It.IsAny<Guid>())).ReturnsAsync((OrderEntity?)null);

        var act = () => _handler.Handle(
            new CreateCheckoutSessionCommand { OrderId = Guid.NewGuid(), UserId = Guid.NewGuid() },
            CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_OrderOwnedByDifferentUser_ThrowsUnauthorized()
    {
        var ownerId = Guid.NewGuid();
        var order = BuildStripeOrder(ownerId, EPaymentStatus.Pending);
        _ordersRepo.Setup(r => r.GetByIdWithItemsAsync(order.Id)).ReturnsAsync(order);

        var act = () => _handler.Handle(
            new CreateCheckoutSessionCommand { OrderId = order.Id, UserId = Guid.NewGuid() }, // different user
            CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedException>();
    }

    [Fact]
    public async Task Handle_CodOrder_ThrowsBadRequest()
    {
        var userId = Guid.NewGuid();
        var order = BuildOrder(userId, EPaymentMethod.COD, EPaymentStatus.NotRequired);
        _ordersRepo.Setup(r => r.GetByIdWithItemsAsync(order.Id)).ReturnsAsync(order);

        var act = () => _handler.Handle(
            new CreateCheckoutSessionCommand { OrderId = order.Id, UserId = userId },
            CancellationToken.None);

        await act.Should().ThrowAsync<BadRequestException>()
            .WithMessage("*only supports Stripe payment*");
    }

    [Fact]
    public async Task Handle_PaymentStatusNotPending_ThrowsBadRequest()
    {
        var userId = Guid.NewGuid();
        var order = BuildStripeOrder(userId, EPaymentStatus.Pending);
        order.MarkPaymentPaid("pi_existing", "ch_existing"); // → Paid
        _ordersRepo.Setup(r => r.GetByIdWithItemsAsync(order.Id)).ReturnsAsync(order);

        var act = () => _handler.Handle(
            new CreateCheckoutSessionCommand { OrderId = order.Id, UserId = userId },
            CancellationToken.None);

        await act.Should().ThrowAsync<BadRequestException>()
            .WithMessage("*not awaiting payment*");
    }

    [Fact]
    public async Task Handle_HappyPath_CreatesSessionAndPersistsPayment()
    {
        var userId = Guid.NewGuid();
        var order = BuildStripeOrder(userId, EPaymentStatus.Pending);
        _ordersRepo.Setup(r => r.GetByIdWithItemsAsync(order.Id)).ReturnsAsync(order);

        _gateway
            .Setup(g => g.CreateCheckoutSessionAsync(
                It.IsAny<CreateCheckoutSessionRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CheckoutSessionResult
            {
                SessionId = "cs_test_happy",
                Url = "https://checkout.stripe.com/c/pay/cs_test_happy",
                ExpiresAtUtc = DateTime.UtcNow.AddHours(24)
            });

        PaymentEntity? persisted = null;
        _paymentsRepo
            .Setup(r => r.CreateAsync(It.IsAny<PaymentEntity>()))
            .Callback<PaymentEntity>(p => persisted = p)
            .Returns(Task.CompletedTask);

        var result = await _handler.Handle(
            new CreateCheckoutSessionCommand { OrderId = order.Id, UserId = userId },
            CancellationToken.None);

        result.SessionId.Should().Be("cs_test_happy");
        result.CheckoutUrl.Should().StartWith("https://checkout.stripe.com/");
        result.Amount.Should().Be((long)order.Total);
        result.Currency.Should().Be("vnd");
        result.PublishableKey.Should().Be("pk_test_dummy");

        persisted.Should().NotBeNull();
        result.PaymentId.Should().Be(persisted!.Id);
        persisted!.StripeSessionId.Should().Be("cs_test_happy");
        persisted.OrderId.Should().Be(order.Id);
        persisted.Status.Should().Be(EPaymentTransactionStatus.Created);

        _gateway.Verify(g => g.CreateCheckoutSessionAsync(
            It.Is<CreateCheckoutSessionRequest>(r =>
                r.OrderId == order.Id &&
                r.PaymentId == persisted.Id &&
                r.Currency == "vnd" &&
                r.TotalAmount == (long)order.Total &&
                r.Items.Count == 1 &&
                r.SuccessUrl == "http://localhost:3000/checkout/success?session_id={CHECKOUT_SESSION_ID}" &&
                r.CancelUrl == "http://localhost:3000/checkout/cancel"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_GatewayThrows_DoesNotPersistPayment()
    {
        var userId = Guid.NewGuid();
        var order = BuildStripeOrder(userId, EPaymentStatus.Pending);
        _ordersRepo.Setup(r => r.GetByIdWithItemsAsync(order.Id)).ReturnsAsync(order);

        _gateway
            .Setup(g => g.CreateCheckoutSessionAsync(
                It.IsAny<CreateCheckoutSessionRequest>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Stripe API down"));

        var act = () => _handler.Handle(
            new CreateCheckoutSessionCommand { OrderId = order.Id, UserId = userId },
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
        _paymentsRepo.Verify(r => r.CreateAsync(It.IsAny<PaymentEntity>()), Times.Never);
    }

    // ─── Helpers ──────────────────────────────────────────────────────────

    private static OrderEntity BuildOrder(
        Guid userId,
        EPaymentMethod paymentMethod,
        EPaymentStatus expectedInitialPaymentStatus)
    {
        var order = OrderEntity.Create(
            "MRC-20260514-001",
            userId,
            new DeliveryInfo("Test", "0900000000", "1 Test St"),
            new[] { OrderItem.Create(Guid.NewGuid(), "Cà phê sữa", 45_000m, 1) },
            paymentMethod);

        order.PaymentStatus.Should().Be(expectedInitialPaymentStatus);
        return order;
    }

    private static OrderEntity BuildStripeOrder(Guid userId, EPaymentStatus expected)
        => BuildOrder(userId, EPaymentMethod.STRIPE, expected);
}
