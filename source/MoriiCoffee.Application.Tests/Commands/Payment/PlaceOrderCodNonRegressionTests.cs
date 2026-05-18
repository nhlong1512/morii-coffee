using FluentAssertions;
using Moq;
using MockQueryable;
using MoriiCoffee.Application.Commands.Order.PlaceOrder;
using MoriiCoffee.Application.SeedWork.Abstractions;
using MoriiCoffee.Application.SeedWork.DTOs.Cart;
using MoriiCoffee.Domain.Repositories;
using MoriiCoffee.Domain.SeedWork.Persistence;
using MoriiCoffee.Domain.Shared.Enums.Order;
using MoriiCoffee.Domain.Shared.Settings;
using Xunit;
using OrderEntity = MoriiCoffee.Domain.Aggregates.OrderAggregate.Order;

namespace MoriiCoffee.Application.Tests.Commands.Payment;

/// <summary>
/// US4 non-regression tests for the existing COD checkout flow. Guarantees:
/// <list type="bullet">
/// <item>COD orders are created with <see cref="EPaymentStatus.NotRequired"/>.</item>
/// <item>The PlaceOrder handler never touches <see cref="IPaymentGateway"/>.</item>
/// <item>An admin can confirm a COD order without the new FR-013 guard blocking it.</item>
/// </list>
/// </summary>
public class PlaceOrderCodNonRegressionTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ICartService> _cartService = new();
    private readonly Mock<IOrderIdGenerator> _orderIdGenerator = new();
    private readonly Mock<IOrderRepository> _ordersRepo = new();
    private readonly Mock<IUserDeliveryProfileRepository> _profilesRepo = new();
    private readonly Mock<IProductsRepository> _productsRepo = new();
    private readonly PlaceOrderCommandHandler _handler;
    private static readonly AwsS3Settings S3Settings = new() { CdnBaseUrl = "https://cdn.test" };

    public PlaceOrderCodNonRegressionTests()
    {
        _unitOfWork.Setup(u => u.Orders).Returns(_ordersRepo.Object);
        _unitOfWork.Setup(u => u.UserDeliveryProfiles).Returns(_profilesRepo.Object);
        _unitOfWork.Setup(u => u.Products).Returns(_productsRepo.Object);
        _unitOfWork
            .Setup(u => u.ExecuteInTransactionAsync(It.IsAny<Func<Task>>()))
            .Returns<Func<Task>>(fn => fn());
        _productsRepo
            .Setup(r => r.FindByCondition(It.IsAny<System.Linq.Expressions.Expression<Func<MoriiCoffee.Domain.Aggregates.ProductAggregate.Product, bool>>>(), false))
            .Returns(new List<MoriiCoffee.Domain.Aggregates.ProductAggregate.Product>().BuildMock());

        _handler = new PlaceOrderCommandHandler(
            _unitOfWork.Object,
            _cartService.Object,
            _orderIdGenerator.Object,
            S3Settings);
    }

    [Fact]
    public async Task Handle_CodOrder_PaymentStatusIsNotRequired_AndNoGatewayCall()
    {
        var userId = Guid.NewGuid();
        _cartService.Setup(c => c.GetCartAsync(userId)).ReturnsAsync(BuildCart());
        _orderIdGenerator.Setup(g => g.GenerateAsync()).ReturnsAsync("MRC-20260514-COD1");

        OrderEntity? captured = null;
        _ordersRepo
            .Setup(r => r.CreateAsync(It.IsAny<OrderEntity>()))
            .Callback<OrderEntity>(o => captured = o)
            .Returns(Task.CompletedTask);

        await _handler.Handle(BuildCommand(userId), CancellationToken.None);

        captured.Should().NotBeNull();
        captured!.PaymentMethod.Should().Be(EPaymentMethod.COD);
        captured.PaymentStatus.Should().Be(EPaymentStatus.NotRequired);
        captured.OrderStatus.Should().Be(EOrderStatus.PENDING);

        // Critical: the COD path must not create a Payment row or even touch the Payments repo.
        _unitOfWork.VerifyGet(u => u.Payments, Times.Never);
    }

    [Fact]
    public void Confirm_CodOrder_NotBlockedByFr013Guard()
    {
        // Build a fresh COD order through the same path PlaceOrder uses.
        var delivery = new MoriiCoffee.Domain.Aggregates.OrderAggregate.ValueObjects.DeliveryInfo(
            "Test", "0900000000", "1 Test St");
        var items = new[]
        {
            MoriiCoffee.Domain.Aggregates.OrderAggregate.Entities.OrderItem
                .Create(Guid.NewGuid(), "Cà phê sữa", 45_000m, 1)
        };
        var order = OrderEntity.Create(
            "MRC-20260514-COD2",
            Guid.NewGuid(),
            delivery,
            items,
            EPaymentMethod.COD);

        // FR-013 guard must not block COD confirmation.
        var act = () => order.Confirm();
        act.Should().NotThrow();
        order.OrderStatus.Should().Be(EOrderStatus.CONFIRMED);
    }

    // ── Helpers ──────────────────────────────────────────────────────────

    private static CartDto BuildCart() => new()
    {
        Items =
        [
            new CartItemDto
            {
                ProductId = Guid.NewGuid(),
                ProductName = "Cà phê sữa",
                UnitPrice = 45_000m,
                Quantity = 1
            }
        ]
    };

    private static PlaceOrderCommand BuildCommand(Guid userId) => new()
    {
        UserId = userId,
        FullName = "Test User",
        PhoneNumber = "0900000000",
        Address = "1 Test St",
        PaymentMethod = EPaymentMethod.COD,
        SaveDeliveryProfile = false
    };
}
