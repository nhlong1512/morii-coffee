using FluentAssertions;
using Moq;
using MoriiCoffee.Application.Commands.Cart.AddItemToCart;
using MoriiCoffee.Application.SeedWork.Abstractions;
using MoriiCoffee.Application.SeedWork.DTOs.Cart;
using MoriiCoffee.Application.SeedWork.Exceptions;
using MoriiCoffee.Domain.Repositories;
using MoriiCoffee.Domain.SeedWork.Persistence;
using Xunit;
using ProductEntity = MoriiCoffee.Domain.Aggregates.ProductAggregate.Product;
using ProductVariantEntity = MoriiCoffee.Domain.Aggregates.ProductAggregate.Entities.ProductVariant;

namespace MoriiCoffee.Application.Tests.Commands.Cart;

public class AddItemToCartCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ICartService> _cartService = new();
    private readonly Mock<IProductsRepository> _productsRepo = new();
    private readonly Mock<IProductVariantsRepository> _variantsRepo = new();
    private readonly AddItemToCartCommandHandler _handler;

    public AddItemToCartCommandHandlerTests()
    {
        _unitOfWork.Setup(u => u.Products).Returns(_productsRepo.Object);
        _unitOfWork.Setup(u => u.ProductVariants).Returns(_variantsRepo.Object);
        _handler = new AddItemToCartCommandHandler(_unitOfWork.Object, _cartService.Object);
    }

    [Fact]
    public async Task Handle_ProductNotFound_ThrowsNotFoundException()
    {
        _productsRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync((ProductEntity?)null);

        var act = () => _handler.Handle(BuildCommand(), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage("*Product*");
    }

    [Fact]
    public async Task Handle_ProductSoftDeleted_ThrowsNotFoundException()
    {
        var product = new ProductEntity { Id = Guid.NewGuid(), IsDeleted = true };
        _productsRepo.Setup(r => r.GetByIdAsync(product.Id))
            .ReturnsAsync(product);

        var act = () => _handler.Handle(BuildCommand(productId: product.Id), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_VariantNotFound_ThrowsNotFoundException()
    {
        var productId = Guid.NewGuid();
        var variantId = Guid.NewGuid();
        _productsRepo.Setup(r => r.GetByIdAsync(productId))
            .ReturnsAsync(new ProductEntity { Id = productId, Name = "Cà phê sữa", BasePrice = 45_000 });
        _variantsRepo.Setup(r => r.GetByIdAsync(variantId))
            .ReturnsAsync((ProductVariantEntity?)null);

        var act = () => _handler.Handle(BuildCommand(productId, variantId), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage("*ProductVariant*");
    }

    [Fact]
    public async Task Handle_ValidProduct_NoVariant_AddsItemWithBasePrice()
    {
        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        _productsRepo.Setup(r => r.GetByIdAsync(productId))
            .ReturnsAsync(new ProductEntity { Id = productId, Name = "Cà phê sữa", BasePrice = 45_000 });

        var result = await _handler.Handle(BuildCommand(productId, userId: userId), CancellationToken.None);

        result.Should().BeTrue();
        _cartService.Verify(c => c.AddItemAsync(userId, It.Is<CartItemDto>(i =>
            i.ProductId == productId &&
            i.UnitPrice == 45_000 &&
            i.VariantId == null)), Times.Once);
    }

    [Fact]
    public async Task Handle_ValidProduct_WithVariant_AddsPriceWithVariantSurcharge()
    {
        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var variantId = Guid.NewGuid();
        _productsRepo.Setup(r => r.GetByIdAsync(productId))
            .ReturnsAsync(new ProductEntity { Id = productId, Name = "Cà phê sữa", BasePrice = 45_000 });
        _variantsRepo.Setup(r => r.GetByIdAsync(variantId))
            .ReturnsAsync(new ProductVariantEntity { Id = variantId, Name = "Size L", AdditionalPrice = 5_000 });

        await _handler.Handle(BuildCommand(productId, variantId, userId: userId), CancellationToken.None);

        _cartService.Verify(c => c.AddItemAsync(userId, It.Is<CartItemDto>(i =>
            i.UnitPrice == 50_000 &&
            i.VariantId == variantId &&
            i.VariantLabel == "Size L")), Times.Once);
    }

    private static AddItemToCartCommand BuildCommand(
        Guid? productId = null,
        Guid? variantId = null,
        Guid? userId = null,
        int quantity = 1) => new()
    {
        UserId = userId ?? Guid.NewGuid(),
        ProductId = productId ?? Guid.NewGuid(),
        VariantId = variantId,
        Quantity = quantity
    };
}
