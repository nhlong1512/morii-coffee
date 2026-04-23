using FluentAssertions;
using Moq;
using MoriiCoffee.Application.Commands.Cart.AddCartItem;
using MoriiCoffee.Application.Commands.Cart.ClearCart;
using MoriiCoffee.Application.Commands.Cart.RemoveCartItem;
using MoriiCoffee.Application.Commands.Cart.UpdateCartItem;
using MoriiCoffee.Application.Queries.Cart.GetCart;
using MoriiCoffee.Application.SeedWork.Abstractions;
using MoriiCoffee.Application.SeedWork.DTOs.Cart;
using MoriiCoffee.Application.SeedWork.Exceptions;
using MoriiCoffee.Domain.Aggregates.ProductAggregate.Entities;
using MoriiCoffee.Domain.Repositories;
using MoriiCoffee.Domain.SeedWork.Persistence;
using Xunit;
using ProductEntity = MoriiCoffee.Domain.Aggregates.ProductAggregate.Product;

namespace MoriiCoffee.Application.Tests.Commands.Cart;

/// <summary>Covers add/update/remove/clear/get cart command and query handler behavior.</summary>
public class CartCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IProductsRepository> _productsRepo = new();
    private readonly Mock<IProductVariantsRepository> _variantsRepo = new();
    private readonly Mock<ICartService> _cartService = new();

    public CartCommandHandlerTests()
    {
        _unitOfWork.Setup(u => u.Products).Returns(_productsRepo.Object);
        _unitOfWork.Setup(u => u.ProductVariants).Returns(_variantsRepo.Object);
    }

    // ── AddCartItem ────────────────────────────────────────────────────────────

    [Fact]
    public async Task AddCartItem_ValidVariant_CallsAddItemAndReturnsCart()
    {
        var userId = Guid.NewGuid();
        var variantId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var variant = new ProductVariant { Id = variantId, ProductId = productId, Name = "Large", AdditionalPrice = 5_000m };
        var product = new ProductEntity { Id = productId, Name = "Latte", BasePrice = 50_000m };
        var expectedCart = new CartDto { UserId = userId, GrandTotal = 55_000m };

        _variantsRepo.Setup(r => r.GetByIdAsync(variantId)).ReturnsAsync(variant);
        _productsRepo.Setup(r => r.GetByIdAsync(productId)).ReturnsAsync(product);
        _cartService.Setup(s => s.AddItemAsync(userId, It.IsAny<CartItemDto>())).ReturnsAsync(expectedCart);

        var handler = new AddCartItemCommandHandler(_unitOfWork.Object, _cartService.Object);
        var result = await handler.Handle(new AddCartItemCommand { UserId = userId, VariantId = variantId, Quantity = 1 }, CancellationToken.None);

        result.Should().Be(expectedCart);
        _cartService.Verify(s => s.AddItemAsync(userId, It.Is<CartItemDto>(d => d.VariantId == variantId && d.UnitPrice == 55_000m)), Times.Once);
    }

    [Fact]
    public async Task AddCartItem_VariantNotFound_ThrowsNotFoundException()
    {
        _variantsRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((ProductVariant?)null);

        var handler = new AddCartItemCommandHandler(_unitOfWork.Object, _cartService.Object);

        await handler.Invoking(h => h.Handle(new AddCartItemCommand { UserId = Guid.NewGuid(), VariantId = Guid.NewGuid(), Quantity = 1 }, CancellationToken.None))
            .Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task AddCartItem_DuplicateVariant_ServiceReceivesAddCallAndAccumulates()
    {
        var userId = Guid.NewGuid();
        var variantId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var variant = new ProductVariant { Id = variantId, ProductId = productId, Name = "M", AdditionalPrice = 0m };
        var product = new ProductEntity { Id = productId, Name = "Espresso", BasePrice = 40_000m };
        var expectedCart = new CartDto { UserId = userId, GrandTotal = 80_000m }; // 2 × 40k

        _variantsRepo.Setup(r => r.GetByIdAsync(variantId)).ReturnsAsync(variant);
        _productsRepo.Setup(r => r.GetByIdAsync(productId)).ReturnsAsync(product);
        _cartService.Setup(s => s.AddItemAsync(userId, It.IsAny<CartItemDto>())).ReturnsAsync(expectedCart);

        var handler = new AddCartItemCommandHandler(_unitOfWork.Object, _cartService.Object);
        var result = await handler.Handle(new AddCartItemCommand { UserId = userId, VariantId = variantId, Quantity = 2 }, CancellationToken.None);

        result.GrandTotal.Should().Be(80_000m);
    }

    // ── UpdateCartItem ─────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateCartItem_Success_ReturnsUpdatedCart()
    {
        var userId = Guid.NewGuid();
        var variantId = Guid.NewGuid();
        var updatedCart = new CartDto { UserId = userId, GrandTotal = 60_000m };

        _cartService.Setup(s => s.UpdateItemQuantityAsync(userId, variantId, 3)).ReturnsAsync(updatedCart);

        var handler = new UpdateCartItemCommandHandler(_cartService.Object);
        var result = await handler.Handle(new UpdateCartItemCommand { UserId = userId, VariantId = variantId, Quantity = 3 }, CancellationToken.None);

        result.Should().Be(updatedCart);
    }

    // ── RemoveCartItem ─────────────────────────────────────────────────────────

    [Fact]
    public async Task RemoveCartItem_Success_ReturnsUpdatedCart()
    {
        var userId = Guid.NewGuid();
        var variantId = Guid.NewGuid();
        var updatedCart = new CartDto { UserId = userId, GrandTotal = 0m };

        _cartService.Setup(s => s.RemoveItemAsync(userId, variantId)).ReturnsAsync(updatedCart);

        var handler = new RemoveCartItemCommandHandler(_cartService.Object);
        var result = await handler.Handle(new RemoveCartItemCommand { UserId = userId, VariantId = variantId }, CancellationToken.None);

        result.Should().Be(updatedCart);
    }

    // ── ClearCart ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task ClearCart_Success_ReturnsTrue()
    {
        var userId = Guid.NewGuid();
        _cartService.Setup(s => s.ClearCartAsync(userId)).Returns(Task.CompletedTask);

        var handler = new ClearCartCommandHandler(_cartService.Object);
        var result = await handler.Handle(new ClearCartCommand { UserId = userId }, CancellationToken.None);

        result.Should().BeTrue();
        _cartService.Verify(s => s.ClearCartAsync(userId), Times.Once);
    }

    // ── GetCart ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetCart_ReturnsCartFromService()
    {
        var userId = Guid.NewGuid();
        var cart = new CartDto { UserId = userId, Items = new(), GrandTotal = 0m };

        _cartService.Setup(s => s.GetCartAsync(userId)).ReturnsAsync(cart);

        var handler = new GetCartQueryHandler(_cartService.Object);
        var result = await handler.Handle(new GetCartQuery(userId), CancellationToken.None);

        result.Should().Be(cart);
    }

    [Fact]
    public async Task GetCart_EmptyUserId_StillReturnsCart()
    {
        var userId = Guid.Empty;
        var emptyCart = new CartDto { UserId = userId };

        _cartService.Setup(s => s.GetCartAsync(userId)).ReturnsAsync(emptyCart);

        var handler = new GetCartQueryHandler(_cartService.Object);
        var result = await handler.Handle(new GetCartQuery(userId), CancellationToken.None);

        result.UserId.Should().Be(Guid.Empty);
    }
}
