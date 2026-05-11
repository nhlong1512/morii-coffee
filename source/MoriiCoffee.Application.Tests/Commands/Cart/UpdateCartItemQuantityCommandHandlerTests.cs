using FluentAssertions;
using Moq;
using MoriiCoffee.Application.Commands.Cart.UpdateCartItemQuantity;
using MoriiCoffee.Application.SeedWork.Abstractions;
using Xunit;

namespace MoriiCoffee.Application.Tests.Commands.Cart;

public class UpdateCartItemQuantityCommandHandlerTests
{
    private readonly Mock<ICartService> _cartService = new();
    private readonly UpdateCartItemQuantityCommandHandler _handler;

    public UpdateCartItemQuantityCommandHandlerTests()
    {
        _handler = new UpdateCartItemQuantityCommandHandler(_cartService.Object);
    }

    [Fact]
    public async Task Handle_CallsUpdateQuantityWithCorrectArgs()
    {
        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var variantId = Guid.NewGuid();

        await _handler.Handle(
            new UpdateCartItemQuantityCommand
            {
                UserId = userId,
                ProductId = productId,
                VariantId = variantId,
                Quantity = 3
            },
            CancellationToken.None);

        _cartService.Verify(c => c.UpdateQuantityAsync(userId, productId, variantId, 3), Times.Once);
    }

    [Fact]
    public async Task Handle_ZeroQuantity_PassesZeroToService()
    {
        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();

        await _handler.Handle(
            new UpdateCartItemQuantityCommand { UserId = userId, ProductId = productId, Quantity = 0 },
            CancellationToken.None);

        _cartService.Verify(c => c.UpdateQuantityAsync(userId, productId, null, 0), Times.Once);
    }

    [Fact]
    public async Task Handle_ReturnsTrue()
    {
        var result = await _handler.Handle(
            new UpdateCartItemQuantityCommand
            {
                UserId = Guid.NewGuid(),
                ProductId = Guid.NewGuid(),
                Quantity = 1
            },
            CancellationToken.None);

        result.Should().BeTrue();
    }
}
