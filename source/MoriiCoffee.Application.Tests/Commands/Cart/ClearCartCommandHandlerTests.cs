using FluentAssertions;
using Moq;
using MoriiCoffee.Application.Commands.Cart.ClearCart;
using MoriiCoffee.Application.SeedWork.Abstractions;
using Xunit;

namespace MoriiCoffee.Application.Tests.Commands.Cart;

public class ClearCartCommandHandlerTests
{
    private readonly Mock<ICartService> _cartService = new();
    private readonly ClearCartCommandHandler _handler;

    public ClearCartCommandHandlerTests()
    {
        _handler = new ClearCartCommandHandler(_cartService.Object);
    }

    [Fact]
    public async Task Handle_CallsClearCartWithCorrectUserId()
    {
        var userId = Guid.NewGuid();

        await _handler.Handle(new ClearCartCommand { UserId = userId }, CancellationToken.None);

        _cartService.Verify(c => c.ClearCartAsync(userId), Times.Once);
    }

    [Fact]
    public async Task Handle_ReturnsTrue()
    {
        var result = await _handler.Handle(
            new ClearCartCommand { UserId = Guid.NewGuid() }, CancellationToken.None);

        result.Should().BeTrue();
    }
}
