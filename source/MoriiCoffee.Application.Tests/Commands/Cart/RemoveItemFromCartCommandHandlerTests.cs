using FluentAssertions;
using Moq;
using MoriiCoffee.Application.Commands.Cart.RemoveItemFromCart;
using MoriiCoffee.Application.SeedWork.Abstractions;
using Xunit;

namespace MoriiCoffee.Application.Tests.Commands.Cart;

public class RemoveItemFromCartCommandHandlerTests
{
    private readonly Mock<ICartService> _cartService = new();
    private readonly RemoveItemFromCartCommandHandler _handler;

    public RemoveItemFromCartCommandHandlerTests()
    {
        _handler = new RemoveItemFromCartCommandHandler(_cartService.Object);
    }

    [Fact]
    public async Task Handle_CallsRemoveItemWithCorrectArgs()
    {
        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var variantId = Guid.NewGuid();

        await _handler.Handle(
            new RemoveItemFromCartCommand { UserId = userId, ProductId = productId, VariantId = variantId },
            CancellationToken.None);

        _cartService.Verify(c => c.RemoveItemAsync(userId, productId, variantId), Times.Once);
    }

    [Fact]
    public async Task Handle_NoVariant_PassesNullVariantId()
    {
        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();

        await _handler.Handle(
            new RemoveItemFromCartCommand { UserId = userId, ProductId = productId, VariantId = null },
            CancellationToken.None);

        _cartService.Verify(c => c.RemoveItemAsync(userId, productId, null), Times.Once);
    }

    [Fact]
    public async Task Handle_ReturnsTrue()
    {
        var result = await _handler.Handle(
            new RemoveItemFromCartCommand { UserId = Guid.NewGuid(), ProductId = Guid.NewGuid() },
            CancellationToken.None);

        result.Should().BeTrue();
    }
}
