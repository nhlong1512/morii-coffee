using FluentAssertions;
using MockQueryable;
using Moq;
using MoriiCoffee.Application.Queries.Order.GetOrderById;
using MoriiCoffee.Application.SeedWork.Exceptions;
using MoriiCoffee.Domain.Aggregates.OrderAggregate.Entities;
using MoriiCoffee.Domain.Aggregates.OrderAggregate.ValueObjects;
using MoriiCoffee.Domain.Repositories;
using MoriiCoffee.Domain.SeedWork.Persistence;
using MoriiCoffee.Domain.Shared.Enums.Order;
using Xunit;
using OrderEntity = MoriiCoffee.Domain.Aggregates.OrderAggregate.Order;

namespace MoriiCoffee.Application.Tests.Queries.Order;

public class GetOrderByIdQueryHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IOrderRepository> _ordersRepo = new();
    private readonly Mock<IProductsRepository> _productsRepo = new();
    private readonly GetOrderByIdQueryHandler _handler;

    public GetOrderByIdQueryHandlerTests()
    {
        _unitOfWork.Setup(u => u.Orders).Returns(_ordersRepo.Object);
        _unitOfWork.Setup(u => u.Products).Returns(_productsRepo.Object);
        _productsRepo
            .Setup(r => r.FindByCondition(It.IsAny<System.Linq.Expressions.Expression<Func<Domain.Aggregates.ProductAggregate.Product, bool>>>(), false))
            .Returns(new List<Domain.Aggregates.ProductAggregate.Product>().BuildMock());
        _handler = new GetOrderByIdQueryHandler(_unitOfWork.Object);
    }

    [Fact]
    public async Task Handle_OrderNotFound_ThrowsNotFoundException()
    {
        _ordersRepo.Setup(r => r.GetByIdWithItemsAsync(It.IsAny<Guid>()))
            .ReturnsAsync((OrderEntity?)null);

        var act = () => _handler.Handle(
            new GetOrderByIdQuery(Guid.NewGuid(), Guid.NewGuid(), false),
            CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_NonOwner_NonAdmin_ThrowsUnauthorizedException()
    {
        var ownerId = Guid.NewGuid();
        var order = BuildOrder(ownerId);
        _ordersRepo.Setup(r => r.GetByIdWithItemsAsync(order.Id)).ReturnsAsync(order);

        var act = () => _handler.Handle(
            new GetOrderByIdQuery(order.Id, Guid.NewGuid(), false),
            CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedException>();
    }

    [Fact]
    public async Task Handle_Owner_ReturnsOrderDto()
    {
        var ownerId = Guid.NewGuid();
        var order = BuildOrder(ownerId);
        _ordersRepo.Setup(r => r.GetByIdWithItemsAsync(order.Id)).ReturnsAsync(order);

        var result = await _handler.Handle(
            new GetOrderByIdQuery(order.Id, ownerId, false),
            CancellationToken.None);

        result.Should().NotBeNull();
        result.Id.Should().Be(order.Id);
        result.UserId.Should().Be(ownerId);
    }

    [Fact]
    public async Task Handle_AdminCanViewAnyOrder()
    {
        var ownerId = Guid.NewGuid();
        var order = BuildOrder(ownerId);
        _ordersRepo.Setup(r => r.GetByIdWithItemsAsync(order.Id)).ReturnsAsync(order);

        var result = await _handler.Handle(
            new GetOrderByIdQuery(order.Id, Guid.NewGuid(), IsAdmin: true),
            CancellationToken.None);

        result.Should().NotBeNull();
        result.Id.Should().Be(order.Id);
    }

    private static OrderEntity BuildOrder(Guid userId)
    {
        var delivery = new DeliveryInfo("Test User", "0901234567", "123 Test St");
        var items = new List<OrderItem>
        {
            OrderItem.Create(Guid.NewGuid(), "Cà phê sữa", 45_000, 1, null, null)
        };
        return OrderEntity.Create("MRC-TEST-001", userId, delivery, items, EPaymentMethod.COD);
    }
}
