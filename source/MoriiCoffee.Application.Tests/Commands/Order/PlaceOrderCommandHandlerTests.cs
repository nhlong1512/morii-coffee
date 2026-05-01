using FluentAssertions;
using Moq;
using MoriiCoffee.Application.Commands.Order.PlaceOrder;
using MoriiCoffee.Application.SeedWork.Abstractions;
using MoriiCoffee.Application.SeedWork.DTOs.Cart;
using MoriiCoffee.Application.SeedWork.Exceptions;
using MoriiCoffee.Domain.Aggregates.UserAggregate.Entities;
using MoriiCoffee.Domain.Repositories;
using MoriiCoffee.Domain.SeedWork.Persistence;
using MoriiCoffee.Domain.Shared.Enums.Order;
using Xunit;
using OrderEntity = MoriiCoffee.Domain.Aggregates.OrderAggregate.Order;

namespace MoriiCoffee.Application.Tests.Commands.Order;

public class PlaceOrderCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ICartService> _cartService = new();
    private readonly Mock<IOrderIdGenerator> _orderIdGenerator = new();
    private readonly Mock<IOrderRepository> _ordersRepo = new();
    private readonly Mock<IUserDeliveryProfileRepository> _profilesRepo = new();
    private readonly PlaceOrderCommandHandler _handler;

    public PlaceOrderCommandHandlerTests()
    {
        _unitOfWork.Setup(u => u.Orders).Returns(_ordersRepo.Object);
        _unitOfWork.Setup(u => u.UserDeliveryProfiles).Returns(_profilesRepo.Object);
        _unitOfWork
            .Setup(u => u.ExecuteInTransactionAsync(It.IsAny<Func<Task>>()))
            .Returns<Func<Task>>(fn => fn());

        _handler = new PlaceOrderCommandHandler(
            _unitOfWork.Object,
            _cartService.Object,
            _orderIdGenerator.Object);
    }

    [Fact]
    public async Task Handle_CartIsEmpty_ThrowsBadRequestException()
    {
        var userId = Guid.NewGuid();
        _cartService.Setup(c => c.GetCartAsync(userId))
            .ReturnsAsync(new CartDto { Items = [] });

        var act = () => _handler.Handle(new PlaceOrderCommand { UserId = userId }, CancellationToken.None);

        await act.Should().ThrowAsync<BadRequestException>()
            .WithMessage("*empty*");
    }

    [Fact]
    public async Task Handle_ValidCart_CreatesOrderAndClearsCart()
    {
        var userId = Guid.NewGuid();
        _cartService.Setup(c => c.GetCartAsync(userId)).ReturnsAsync(BuildCart());
        _orderIdGenerator.Setup(g => g.GenerateAsync()).ReturnsAsync("MRC-20260502-001");
        _ordersRepo.Setup(r => r.CreateAsync(It.IsAny<OrderEntity>())).Returns(Task.CompletedTask);

        var result = await _handler.Handle(BuildCommand(userId), CancellationToken.None);

        result.Should().NotBeNull();
        result.OrderNumber.Should().Be("MRC-20260502-001");
        _cartService.Verify(c => c.ClearCartAsync(userId), Times.Once);
        _unitOfWork.Verify(u => u.ExecuteInTransactionAsync(It.IsAny<Func<Task>>()), Times.Once);
    }

    [Fact]
    public async Task Handle_SaveDeliveryProfileTrue_NoExistingProfile_CreatesNewProfile()
    {
        var userId = Guid.NewGuid();
        _cartService.Setup(c => c.GetCartAsync(userId)).ReturnsAsync(BuildCart());
        _orderIdGenerator.Setup(g => g.GenerateAsync()).ReturnsAsync("MRC-20260502-001");
        _ordersRepo.Setup(r => r.CreateAsync(It.IsAny<OrderEntity>())).Returns(Task.CompletedTask);
        _profilesRepo.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync((UserDeliveryProfile?)null);

        await _handler.Handle(BuildCommand(userId, saveDeliveryProfile: true), CancellationToken.None);

        _profilesRepo.Verify(
            r => r.UpsertAsync(It.Is<UserDeliveryProfile>(p => p.UserId == userId)),
            Times.Once);
    }

    [Fact]
    public async Task Handle_SaveDeliveryProfileTrue_ExistingProfile_UpdatesProfile()
    {
        var userId = Guid.NewGuid();
        var existing = UserDeliveryProfile.Create(userId, "Old Name", "0900000000", "Old Address");
        _cartService.Setup(c => c.GetCartAsync(userId)).ReturnsAsync(BuildCart());
        _orderIdGenerator.Setup(g => g.GenerateAsync()).ReturnsAsync("MRC-20260502-001");
        _ordersRepo.Setup(r => r.CreateAsync(It.IsAny<OrderEntity>())).Returns(Task.CompletedTask);
        _profilesRepo.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync(existing);

        var command = BuildCommand(userId, saveDeliveryProfile: true);
        await _handler.Handle(command, CancellationToken.None);

        _profilesRepo.Verify(
            r => r.UpsertAsync(It.Is<UserDeliveryProfile>(p => p.FullName == command.FullName)),
            Times.Once);
    }

    private static CartDto BuildCart() => new()
    {
        Items =
        [
            new CartItemDto
            {
                ProductId = Guid.NewGuid(),
                ProductName = "Cà phê sữa",
                UnitPrice = 45_000,
                Quantity = 2
            }
        ]
    };

    private static PlaceOrderCommand BuildCommand(Guid userId, bool saveDeliveryProfile = false) => new()
    {
        UserId = userId,
        FullName = "Nguyễn Văn A",
        PhoneNumber = "0901234567",
        Address = "123 Đường ABC, Quận 1",
        PaymentMethod = EPaymentMethod.COD,
        SaveDeliveryProfile = saveDeliveryProfile
    };
}
