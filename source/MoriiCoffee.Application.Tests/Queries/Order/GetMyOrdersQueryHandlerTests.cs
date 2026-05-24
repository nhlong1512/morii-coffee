using FluentAssertions;
using MockQueryable;
using Moq;
using MoriiCoffee.Application.Queries.Order.GetMyOrders;
using MoriiCoffee.Domain.Repositories;
using MoriiCoffee.Domain.SeedWork.Persistence;
using MoriiCoffee.Domain.Shared.Enums.Order;
using Xunit;
using OrderEntity = MoriiCoffee.Domain.Aggregates.OrderAggregate.Order;

namespace MoriiCoffee.Application.Tests.Queries.Order;

public class GetMyOrdersQueryHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IOrderRepository> _ordersRepo = new();
    private readonly Mock<IShipmentRepository> _shipmentsRepo = new();
    private readonly GetMyOrdersQueryHandler _handler;

    public GetMyOrdersQueryHandlerTests()
    {
        _unitOfWork.Setup(u => u.Orders).Returns(_ordersRepo.Object);
        _unitOfWork.Setup(u => u.Shipments).Returns(_shipmentsRepo.Object);
        _shipmentsRepo.Setup(r => r.FindByCondition(
                It.IsAny<System.Linq.Expressions.Expression<Func<MoriiCoffee.Domain.Aggregates.ShippingAggregate.Shipment, bool>>>(),
                false))
            .Returns((System.Linq.Expressions.Expression<Func<MoriiCoffee.Domain.Aggregates.ShippingAggregate.Shipment, bool>> predicate, bool _) =>
                new List<MoriiCoffee.Domain.Aggregates.ShippingAggregate.Shipment>().BuildMock().Where(predicate));
        _handler = new GetMyOrdersQueryHandler(_unitOfWork.Object);
    }

    [Fact]
    public async Task Handle_ReturnsOnlyOrdersBelongingToUser()
    {
        var userId = Guid.NewGuid();
        var otherId = Guid.NewGuid();
        var orders = new List<OrderEntity>
        {
            BuildOrder(userId, EOrderStatus.PENDING),
            BuildOrder(otherId, EOrderStatus.CONFIRMED)
        }.BuildMock();

        _ordersRepo.Setup(r => r.FindByCondition(It.IsAny<System.Linq.Expressions.Expression<Func<OrderEntity, bool>>>(), false))
            .Returns((System.Linq.Expressions.Expression<Func<OrderEntity, bool>> predicate, bool _) =>
                orders.Where(predicate));

        var result = await _handler.Handle(new GetMyOrdersQuery(userId, null), CancellationToken.None);

        result.Should().HaveCount(1);
        result[0].OrderStatus.Should().Be(EOrderStatus.PENDING);
    }

    [Fact]
    public async Task Handle_WithStatusFilter_ReturnsOnlyMatchingOrders()
    {
        var userId = Guid.NewGuid();
        var orders = new List<OrderEntity>
        {
            BuildOrder(userId, EOrderStatus.PENDING),
            BuildOrder(userId, EOrderStatus.CONFIRMED),
            BuildOrder(userId, EOrderStatus.DELIVERED)
        }.BuildMock();

        _ordersRepo.Setup(r => r.FindByCondition(It.IsAny<System.Linq.Expressions.Expression<Func<OrderEntity, bool>>>(), false))
            .Returns((System.Linq.Expressions.Expression<Func<OrderEntity, bool>> predicate, bool _) =>
                orders.Where(predicate));

        var result = await _handler.Handle(new GetMyOrdersQuery(userId, EOrderStatus.CONFIRMED), CancellationToken.None);

        result.Should().HaveCount(1);
        result[0].OrderStatus.Should().Be(EOrderStatus.CONFIRMED);
    }

    [Fact]
    public async Task Handle_NoOrders_ReturnsEmptyList()
    {
        var orders = new List<OrderEntity>().BuildMock();
        _ordersRepo.Setup(r => r.FindByCondition(It.IsAny<System.Linq.Expressions.Expression<Func<OrderEntity, bool>>>(), false))
            .Returns(orders);

        var result = await _handler.Handle(new GetMyOrdersQuery(Guid.NewGuid(), null), CancellationToken.None);

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
        else if (status == EOrderStatus.DELIVERED)
        {
            order.Confirm();
            order.MarkReadyToPickup();
            order.MarkInDelivery();
            order.MarkDelivered();
        }

        return order;
    }
}
