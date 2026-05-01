using FluentAssertions;
using Moq;
using MoriiCoffee.Application.Commands.Cart.MergeGuestCart;
using MoriiCoffee.Application.SeedWork.Abstractions;
using MoriiCoffee.Application.SeedWork.DTOs.Cart;
using Xunit;

namespace MoriiCoffee.Application.Tests.Commands.Cart;

public class MergeGuestCartCommandHandlerTests
{
    private readonly Mock<ICartService> _cartService = new();
    private readonly MergeGuestCartCommandHandler _handler;

    public MergeGuestCartCommandHandlerTests()
    {
        _handler = new MergeGuestCartCommandHandler(_cartService.Object);
    }

    [Fact]
    public async Task Handle_EmptyGuestItems_CallsMergeWithEmptyList()
    {
        var userId = Guid.NewGuid();
        var command = new MergeGuestCartCommand { UserId = userId, GuestItems = [] };

        await _handler.Handle(command, CancellationToken.None);

        _cartService.Verify(
            c => c.MergeAsync(userId, It.Is<List<CartItemDto>>(l => l.Count == 0)),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithGuestItems_PassesAllItemsToService()
    {
        var userId = Guid.NewGuid();
        var guestItems = new List<CartItemDto>
        {
            new() { ProductId = Guid.NewGuid(), ProductName = "Cà phê sữa", UnitPrice = 45_000, Quantity = 2 },
            new() { ProductId = Guid.NewGuid(), ProductName = "Trà đào",    UnitPrice = 35_000, Quantity = 1 }
        };
        var command = new MergeGuestCartCommand { UserId = userId, GuestItems = guestItems };

        await _handler.Handle(command, CancellationToken.None);

        _cartService.Verify(c => c.MergeAsync(userId, guestItems), Times.Once);
    }

    [Fact]
    public async Task Handle_AlwaysReturnsTrue()
    {
        var command = new MergeGuestCartCommand { UserId = Guid.NewGuid(), GuestItems = [] };

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Should().BeTrue();
    }
}
