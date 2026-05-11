using FluentAssertions;
using Moq;
using MoriiCoffee.Application.Queries.Order.GetValidOrderStatuses;
using MoriiCoffee.Application.SeedWork.Exceptions;
using MoriiCoffee.Domain.Aggregates.OrderAggregate.Entities;
using MoriiCoffee.Domain.Aggregates.OrderAggregate.ValueObjects;
using MoriiCoffee.Domain.Repositories;
using MoriiCoffee.Domain.SeedWork.Persistence;
using MoriiCoffee.Domain.Shared.Enums.Order;
using Xunit;
using OrderEntity = MoriiCoffee.Domain.Aggregates.OrderAggregate.Order;

namespace MoriiCoffee.Application.Tests.Queries.Order;

public class GetValidOrderStatusesQueryHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IOrderRepository> _ordersRepo = new();
    private readonly GetValidOrderStatusesQueryHandler _handler;

    public GetValidOrderStatusesQueryHandlerTests()
    {
        _unitOfWork.Setup(u => u.Orders).Returns(_ordersRepo.Object);
        _handler = new GetValidOrderStatusesQueryHandler(_unitOfWork.Object);
    }

    [Fact]
    public async Task Handle_OrderNotFound_ThrowsNotFoundException()
    {
        _ordersRepo.Setup(r => r.GetByIdWithItemsAsync(It.IsAny<Guid>()))
            .ReturnsAsync((OrderEntity?)null);

        var act = () => _handler.Handle(new GetValidOrderStatusesQuery(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_PendingOrder_ReturnsAllForwardStatusesPlusCancelled()
    {
        var order = BuildOrder();
        _ordersRepo.Setup(r => r.GetByIdWithItemsAsync(order.Id)).ReturnsAsync(order);

        var result = await _handler.Handle(new GetValidOrderStatusesQuery(order.Id), CancellationToken.None);

        result.Should().Contain([
            EOrderStatus.CONFIRMED,
            EOrderStatus.READY_TO_PICKUP,
            EOrderStatus.IN_DELIVERY,
            EOrderStatus.DELIVERED,
            EOrderStatus.REVIEWED,
            EOrderStatus.CANCELLED
        ]);
        result.Should().NotContain(EOrderStatus.PENDING);
    }

    [Fact]
    public async Task Handle_CancelledOrder_ReturnsEmptyList()
    {
        var order = BuildOrder();
        order.Cancel();
        _ordersRepo.Setup(r => r.GetByIdWithItemsAsync(order.Id)).ReturnsAsync(order);

        var result = await _handler.Handle(new GetValidOrderStatusesQuery(order.Id), CancellationToken.None);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_DeliveredOrder_ReturnsOnlyReviewed()
    {
        var order = BuildOrder();
        order.Confirm();
        order.MarkReadyToPickup();
        order.MarkInDelivery();
        order.MarkDelivered();
        _ordersRepo.Setup(r => r.GetByIdWithItemsAsync(order.Id)).ReturnsAsync(order);

        var result = await _handler.Handle(new GetValidOrderStatusesQuery(order.Id), CancellationToken.None);

        result.Should().ContainSingle().Which.Should().Be(EOrderStatus.REVIEWED);
    }

    private static OrderEntity BuildOrder()
    {
        var delivery = new DeliveryInfo("Test User", "0901234567", "123 Test St");
        var items = new List<OrderItem>
        {
            OrderItem.Create(Guid.NewGuid(), "Cà phê sữa", 45_000, 1, null, null)
        };
        return OrderEntity.Create("MRC-TEST-001", Guid.NewGuid(), delivery, items, EPaymentMethod.COD);
    }
}
