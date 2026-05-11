using FluentAssertions;
using MediatR;
using Moq;
using MoriiCoffee.Application.Commands.Order.UpdateOrderStatus;
using MoriiCoffee.Application.SeedWork.Exceptions;
using MoriiCoffee.Domain.Aggregates.OrderAggregate.Entities;
using MoriiCoffee.Domain.Aggregates.OrderAggregate.ValueObjects;
using MoriiCoffee.Domain.Repositories;
using MoriiCoffee.Domain.SeedWork.Persistence;
using MoriiCoffee.Domain.Shared.Enums.Order;
using Xunit;
using OrderEntity = MoriiCoffee.Domain.Aggregates.OrderAggregate.Order;

namespace MoriiCoffee.Application.Tests.Commands.Order;

public class UpdateOrderStatusCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IOrderRepository> _ordersRepo = new();
    private readonly UpdateOrderStatusCommandHandler _handler;

    public UpdateOrderStatusCommandHandlerTests()
    {
        _unitOfWork.Setup(u => u.Orders).Returns(_ordersRepo.Object);
        _unitOfWork.Setup(u => u.CommitAsync()).ReturnsAsync(1);
        _handler = new UpdateOrderStatusCommandHandler(_unitOfWork.Object);
    }

    [Fact]
    public async Task Handle_OrderNotFound_ThrowsNotFoundException()
    {
        _ordersRepo.Setup(r => r.GetByIdWithItemsAsync(It.IsAny<Guid>()))
            .ReturnsAsync((OrderEntity?)null);

        var act = () => _handler.Handle(
            new UpdateOrderStatusCommand { OrderId = Guid.NewGuid(), NewStatus = EOrderStatus.CONFIRMED },
            CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_ValidTransition_CommitsAndReturnsUnit()
    {
        var order = BuildPendingOrder();
        _ordersRepo.Setup(r => r.GetByIdWithItemsAsync(order.Id)).ReturnsAsync(order);

        var result = await _handler.Handle(
            new UpdateOrderStatusCommand { OrderId = order.Id, NewStatus = EOrderStatus.CONFIRMED },
            CancellationToken.None);

        result.Should().Be(Unit.Value);
        order.OrderStatus.Should().Be(EOrderStatus.CONFIRMED);
        _unitOfWork.Verify(u => u.CommitAsync(), Times.Once);
    }

    [Fact]
    public async Task Handle_SkipStepTransition_Succeeds()
    {
        var order = BuildPendingOrder();
        _ordersRepo.Setup(r => r.GetByIdWithItemsAsync(order.Id)).ReturnsAsync(order);

        await _handler.Handle(
            new UpdateOrderStatusCommand { OrderId = order.Id, NewStatus = EOrderStatus.IN_DELIVERY },
            CancellationToken.None);

        order.OrderStatus.Should().Be(EOrderStatus.IN_DELIVERY);
    }

    [Fact]
    public async Task Handle_BackwardTransition_ThrowsInvalidOperationException()
    {
        var order = BuildPendingOrder();
        order.Confirm();
        _ordersRepo.Setup(r => r.GetByIdWithItemsAsync(order.Id)).ReturnsAsync(order);

        var act = () => _handler.Handle(
            new UpdateOrderStatusCommand { OrderId = order.Id, NewStatus = EOrderStatus.PENDING },
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Handle_SameStatus_IsNoOp_CommitsOnce()
    {
        var order = BuildPendingOrder();
        _ordersRepo.Setup(r => r.GetByIdWithItemsAsync(order.Id)).ReturnsAsync(order);

        await _handler.Handle(
            new UpdateOrderStatusCommand { OrderId = order.Id, NewStatus = EOrderStatus.PENDING },
            CancellationToken.None);

        order.OrderStatus.Should().Be(EOrderStatus.PENDING);
        _unitOfWork.Verify(u => u.CommitAsync(), Times.Once);
    }

    private static OrderEntity BuildPendingOrder()
    {
        var delivery = new DeliveryInfo("Test User", "0901234567", "123 Test St");
        var items = new List<OrderItem>
        {
            OrderItem.Create(Guid.NewGuid(), "Cà phê sữa", 45_000, 1, null, null)
        };
        return OrderEntity.Create("MRC-TEST-001", Guid.NewGuid(), delivery, items, EPaymentMethod.COD);
    }
}
