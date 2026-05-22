using FluentAssertions;
using Moq;
using MoriiCoffee.Application.Commands.Wishlist.AddItemToWishlist;
using MoriiCoffee.Application.SeedWork.Exceptions;
using MoriiCoffee.Domain.Repositories;
using MoriiCoffee.Domain.SeedWork.Persistence;
using Xunit;
using ProductEntity = MoriiCoffee.Domain.Aggregates.ProductAggregate.Product;
using WishlistItemEntity = MoriiCoffee.Domain.Aggregates.WishlistAggregate.WishlistItem;

namespace MoriiCoffee.Application.Tests.Commands.Wishlist;

public class AddItemToWishlistCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IProductsRepository> _productsRepo = new();
    private readonly Mock<IWishlistItemRepository> _wishlistRepo = new();
    private readonly AddItemToWishlistCommandHandler _handler;

    public AddItemToWishlistCommandHandlerTests()
    {
        _unitOfWork.Setup(u => u.Products).Returns(_productsRepo.Object);
        _unitOfWork.Setup(u => u.WishlistItems).Returns(_wishlistRepo.Object);
        _unitOfWork.Setup(u => u.CommitAsync()).ReturnsAsync(1);
        _handler = new AddItemToWishlistCommandHandler(_unitOfWork.Object);
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
    public async Task Handle_ProductAlreadyInWishlist_ReturnsTrueWithoutInserting()
    {
        var productId = Guid.NewGuid();
        _productsRepo.Setup(r => r.GetByIdAsync(productId))
            .ReturnsAsync(new ProductEntity { Id = productId });
        _wishlistRepo.Setup(r => r.ExistsAsync(It.IsAny<Guid>(), productId))
            .ReturnsAsync(true);

        var result = await _handler.Handle(BuildCommand(productId: productId), CancellationToken.None);

        result.Should().BeTrue();
        _wishlistRepo.Verify(r => r.AddAsync(It.IsAny<WishlistItemEntity>()), Times.Never);
        _unitOfWork.Verify(u => u.CommitAsync(), Times.Never);
    }

    [Fact]
    public async Task Handle_NewProduct_AddsItemWithCorrectUserAndProductId()
    {
        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        _productsRepo.Setup(r => r.GetByIdAsync(productId))
            .ReturnsAsync(new ProductEntity { Id = productId });
        _wishlistRepo.Setup(r => r.ExistsAsync(userId, productId))
            .ReturnsAsync(false);

        var result = await _handler.Handle(BuildCommand(productId, userId), CancellationToken.None);

        result.Should().BeTrue();
        _wishlistRepo.Verify(r => r.AddAsync(It.Is<WishlistItemEntity>(i =>
            i.UserId == userId &&
            i.ProductId == productId)), Times.Once);
        _unitOfWork.Verify(u => u.CommitAsync(), Times.Once);
    }

    [Fact]
    public async Task Handle_NewProduct_SetsAddedAtToUtcNow()
    {
        var productId = Guid.NewGuid();
        var before = DateTime.UtcNow.AddSeconds(-1);
        _productsRepo.Setup(r => r.GetByIdAsync(productId))
            .ReturnsAsync(new ProductEntity { Id = productId });
        _wishlistRepo.Setup(r => r.ExistsAsync(It.IsAny<Guid>(), productId))
            .ReturnsAsync(false);

        WishlistItemEntity? captured = null;
        _wishlistRepo.Setup(r => r.AddAsync(It.IsAny<WishlistItemEntity>()))
            .Callback<WishlistItemEntity>(item => captured = item);

        await _handler.Handle(BuildCommand(productId: productId), CancellationToken.None);

        captured.Should().NotBeNull();
        captured!.AddedAt.Should().BeAfter(before).And.BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    private static AddItemToWishlistCommand BuildCommand(
        Guid? productId = null,
        Guid? userId = null) => new()
    {
        UserId = userId ?? Guid.NewGuid(),
        ProductId = productId ?? Guid.NewGuid(),
    };
}
