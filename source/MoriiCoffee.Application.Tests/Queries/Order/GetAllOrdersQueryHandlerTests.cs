using FluentAssertions;
using MockQueryable;
using Moq;
using MoriiCoffee.Application.Queries.Order.GetAllOrders;
using MoriiCoffee.Domain.Repositories;
using MoriiCoffee.Domain.SeedWork.Persistence;
using MoriiCoffee.Domain.Shared.Enums.Order;
using Xunit;
using OrderEntity = MoriiCoffee.Domain.Aggregates.OrderAggregate.Order;

namespace MoriiCoffee.Application.Tests.Queries.Order;

public class GetAllOrdersQueryHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IOrderRepository> _ordersRepo = new();
    private readonly GetAllOrdersQueryHandler _handler;

    public GetAllOrdersQueryHandlerTests()
    {
        _unitOfWork.Setup(u => u.Orders).Returns(_ordersRepo.Object);
        _handler = new GetAllOrdersQueryHandler(_unitOfWork.Object);
    }

    [Fact]
    public async Task Handle_NoFilters_ReturnsAllOrders()
    {
        var userId1 = Guid.NewGuid();
        var userId2 = Guid.NewGuid();
        var orders = new List<OrderEntity>
        {
            BuildOrder(userId1, EOrderStatus.PENDING),
            BuildOrder(userId2, EOrderStatus.CONFIRMED)
        }.BuildMock();
        _ordersRepo.Setup(r => r.FindAll(false)).Returns(orders);

        var result = await _handler.Handle(new GetAllOrdersQuery(null, null), CancellationToken.None);

        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task Handle_StatusFilter_ReturnsOnlyMatchingStatus()
    {
        var orders = new List<OrderEntity>
        {
            BuildOrder(Guid.NewGuid(), EOrderStatus.PENDING),
            BuildOrder(Guid.NewGuid(), EOrderStatus.CONFIRMED),
            BuildOrder(Guid.NewGuid(), EOrderStatus.CONFIRMED)
        }.BuildMock();
        _ordersRepo.Setup(r => r.FindAll(false)).Returns(orders);

        var result = await _handler.Handle(new GetAllOrdersQuery(EOrderStatus.CONFIRMED, null), CancellationToken.None);

        result.Should().HaveCount(2);
        result.Should().AllSatisfy(o => o.OrderStatus.Should().Be(EOrderStatus.CONFIRMED));
    }

    [Fact]
    public async Task Handle_UserIdFilter_ReturnsOnlyOrdersForUser()
    {
        var targetUserId = Guid.NewGuid();
        var orders = new List<OrderEntity>
        {
            BuildOrder(targetUserId, EOrderStatus.PENDING),
            BuildOrder(targetUserId, EOrderStatus.CONFIRMED),
            BuildOrder(Guid.NewGuid(), EOrderStatus.PENDING)
        }.BuildMock();
        _ordersRepo.Setup(r => r.FindAll(false)).Returns(orders);

        var result = await _handler.Handle(new GetAllOrdersQuery(null, targetUserId), CancellationToken.None);

        result.Should().HaveCount(2);
        result.Should().AllSatisfy(o => o.Id.Should().NotBeEmpty());
    }

    [Fact]
    public async Task Handle_NoOrders_ReturnsEmptyList()
    {
        _ordersRepo.Setup(r => r.FindAll(false))
            .Returns(new List<OrderEntity>().BuildMock());

        var result = await _handler.Handle(new GetAllOrdersQuery(null, null), CancellationToken.None);

        result.Should().BeEmpty();
    }

    private static OrderEntity BuildOrder(Guid userId, EOrderStatus status)
    {
        var delivery = new Domain.Aggregates.OrderAggregate.ValueObjects.DeliveryInfo("Test", "0901234567", "123 Test St");
        var items = new List<Domain.Aggregates.OrderAggregate.Entities.OrderItem>
        {
            Domain.Aggregates.OrderAggregate.Entities.OrderItem.Create(Guid.NewGuid(), "Item", 10_000, 1, null, null)
        };
        var order = OrderEntity.Create("MRC-TEST-001", userId, delivery, items, EPaymentMethod.COD);
        if (status == EOrderStatus.CONFIRMED) order.Confirm();
        return order;
    }
}
