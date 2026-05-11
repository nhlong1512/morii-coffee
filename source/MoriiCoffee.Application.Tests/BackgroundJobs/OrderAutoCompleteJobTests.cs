using FluentAssertions;
using Microsoft.Extensions.Logging;
using MockQueryable;
using Moq;
using MoriiCoffee.Domain.Aggregates.OrderAggregate.Entities;
using MoriiCoffee.Domain.Aggregates.OrderAggregate.ValueObjects;
using MoriiCoffee.Domain.Repositories;
using MoriiCoffee.Domain.SeedWork.Persistence;
using MoriiCoffee.Domain.Shared.Enums.Order;
using MoriiCoffee.Domain.Shared.Settings;
using MoriiCoffee.Infrastructure.BackgroundJobs;
using Xunit;
using OrderEntity = MoriiCoffee.Domain.Aggregates.OrderAggregate.Order;

namespace MoriiCoffee.Application.Tests.BackgroundJobs;

public class OrderAutoCompleteJobTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IOrderRepository> _ordersRepo = new();
    private readonly Mock<ILogger<OrderAutoCompleteJob>> _logger = new();
    private readonly OrderSettings _settings = new() { AutoCompleteAfterDays = 3 };
    private readonly OrderAutoCompleteJob _job;

    public OrderAutoCompleteJobTests()
    {
        _unitOfWork.Setup(u => u.Orders).Returns(_ordersRepo.Object);
        _unitOfWork.Setup(u => u.CommitAsync()).ReturnsAsync(1);
        _job = new OrderAutoCompleteJob(_unitOfWork.Object, _settings, _logger.Object);
    }

    [Fact]
    public async Task Execute_NoStaleOrders_DoesNotCommit()
    {
        _ordersRepo.Setup(r => r.FindByCondition(
                It.IsAny<System.Linq.Expressions.Expression<Func<OrderEntity, bool>>>(), true))
            .Returns(new List<OrderEntity>().BuildMock());

        await _job.ExecuteAsync();

        _unitOfWork.Verify(u => u.CommitAsync(), Times.Never);
    }

    [Fact]
    public async Task Execute_StaleInDeliveryOrders_MarksThemDeliveredAndCommits()
    {
        var order1 = BuildInDeliveryOrder(daysAgo: 4);
        var order2 = BuildInDeliveryOrder(daysAgo: 5);
        var staleOrders = new List<OrderEntity> { order1, order2 }.BuildMock();

        _ordersRepo.Setup(r => r.FindByCondition(
                It.IsAny<System.Linq.Expressions.Expression<Func<OrderEntity, bool>>>(), true))
            .Returns(staleOrders);

        await _job.ExecuteAsync();

        order1.OrderStatus.Should().Be(EOrderStatus.DELIVERED);
        order2.OrderStatus.Should().Be(EOrderStatus.DELIVERED);
        _unitOfWork.Verify(u => u.CommitAsync(), Times.Once);
    }

    [Fact]
    public async Task Execute_SingleStaleOrder_CommitsExactlyOnce()
    {
        var order = BuildInDeliveryOrder(daysAgo: 10);
        _ordersRepo.Setup(r => r.FindByCondition(
                It.IsAny<System.Linq.Expressions.Expression<Func<OrderEntity, bool>>>(), true))
            .Returns(new List<OrderEntity> { order }.BuildMock());

        await _job.ExecuteAsync();

        _unitOfWork.Verify(u => u.CommitAsync(), Times.Once);
        order.OrderStatus.Should().Be(EOrderStatus.DELIVERED);
    }

    private static OrderEntity BuildInDeliveryOrder(int daysAgo = 4)
    {
        var delivery = new DeliveryInfo("Test User", "0901234567", "123 Test St");
        var items = new List<OrderItem>
        {
            OrderItem.Create(Guid.NewGuid(), "Cà phê sữa", 45_000, 1, null, null)
        };
        var order = OrderEntity.Create("MRC-TEST-001", Guid.NewGuid(), delivery, items, EPaymentMethod.COD);
        order.Confirm();
        order.MarkReadyToPickup();
        order.MarkInDelivery();
        return order;
    }
}
