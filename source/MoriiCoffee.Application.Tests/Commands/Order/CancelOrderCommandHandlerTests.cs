using FluentAssertions;
using MediatR;
using Moq;
using MoriiCoffee.Application.Commands.Order.CancelOrder;
using MoriiCoffee.Application.SeedWork.Exceptions;
using MoriiCoffee.Domain.Aggregates.OrderAggregate.Entities;
using MoriiCoffee.Domain.Aggregates.OrderAggregate.ValueObjects;
using MoriiCoffee.Domain.Repositories;
using MoriiCoffee.Domain.SeedWork.Persistence;
using MoriiCoffee.Domain.Shared.Enums.Order;
using Xunit;
using OrderEntity = MoriiCoffee.Domain.Aggregates.OrderAggregate.Order;

namespace MoriiCoffee.Application.Tests.Commands.Order;

public class CancelOrderCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IOrderRepository> _ordersRepo = new();
    private readonly CancelOrderCommandHandler _handler;

    public CancelOrderCommandHandlerTests()
    {
        _unitOfWork.Setup(u => u.Orders).Returns(_ordersRepo.Object);
        _unitOfWork.Setup(u => u.CommitAsync()).ReturnsAsync(1);
        _handler = new CancelOrderCommandHandler(_unitOfWork.Object);
    }

    [Fact]
    public async Task Handle_OrderNotFound_ThrowsNotFoundException()
    {
        _ordersRepo.Setup(r => r.GetByIdWithItemsAsync(It.IsAny<Guid>()))
            .ReturnsAsync((OrderEntity?)null);

        var act = () => _handler.Handle(
            new CancelOrderCommand { OrderId = Guid.NewGuid(), UserId = Guid.NewGuid() },
            CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_NotOwner_ThrowsUnauthorizedException()
    {
        var ownerId = Guid.NewGuid();
        var order = BuildPendingOrder(ownerId);
        _ordersRepo.Setup(r => r.GetByIdWithItemsAsync(order.Id)).ReturnsAsync(order);

        var act = () => _handler.Handle(
            new CancelOrderCommand { OrderId = order.Id, UserId = Guid.NewGuid() },
            CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedException>();
    }

    [Fact]
    public async Task Handle_OrderAlreadyCancelled_ThrowsInvalidOperationException()
    {
        var userId = Guid.NewGuid();
        var order = BuildPendingOrder(userId);
        order.Cancel(); // PENDING → CANCELLED; second Cancel() will throw

        _ordersRepo.Setup(r => r.GetByIdWithItemsAsync(order.Id)).ReturnsAsync(order);

        var act = () => _handler.Handle(
            new CancelOrderCommand { OrderId = order.Id, UserId = userId },
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Handle_ValidCancellation_CommitsAndReturnsUnit()
    {
        var userId = Guid.NewGuid();
        var order = BuildPendingOrder(userId);
        _ordersRepo.Setup(r => r.GetByIdWithItemsAsync(order.Id)).ReturnsAsync(order);

        var result = await _handler.Handle(
            new CancelOrderCommand { OrderId = order.Id, UserId = userId },
            CancellationToken.None);

        result.Should().Be(Unit.Value);
        _unitOfWork.Verify(u => u.CommitAsync(), Times.Once);
    }

    private static OrderEntity BuildPendingOrder(Guid userId)
    {
        var deliveryInfo = new DeliveryInfo("Test User", "0901234567", "123 Test St");
        var items = new List<OrderItem>
        {
            OrderItem.Create(Guid.NewGuid(), "Cà phê sữa", 45_000, 1, null, null)
        };
        return OrderEntity.Create("MRC-20260502-001", userId, deliveryInfo, items, EPaymentMethod.COD, null);
    }
}
