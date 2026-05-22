using FluentAssertions;
using MockQueryable;
using Moq;
using MoriiCoffee.Application.Queries.Wishlist.GetWishlist;
using MoriiCoffee.Domain.Aggregates.WishlistAggregate;
using MoriiCoffee.Domain.Repositories;
using MoriiCoffee.Domain.SeedWork.Persistence;
using MoriiCoffee.Domain.Shared.Enums.Product;
using MoriiCoffee.Domain.Shared.Settings;
using Xunit;
using ProductEntity = MoriiCoffee.Domain.Aggregates.ProductAggregate.Product;

namespace MoriiCoffee.Application.Tests.Queries.Wishlist;

public class GetWishlistQueryHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IWishlistItemRepository> _wishlistRepo = new();
    private readonly Mock<IProductsRepository> _productsRepo = new();
    private readonly AwsS3Settings _s3Settings = new() { CdnBaseUrl = "https://cdn.example.com" };
    private readonly GetWishlistQueryHandler _handler;

    public GetWishlistQueryHandlerTests()
    {
        _unitOfWork.Setup(u => u.WishlistItems).Returns(_wishlistRepo.Object);
        _unitOfWork.Setup(u => u.Products).Returns(_productsRepo.Object);
        _handler = new GetWishlistQueryHandler(_unitOfWork.Object, _s3Settings);
    }

    [Fact]
    public async Task Handle_EmptyWishlist_ReturnsEmptyItemsWithoutQueryingProducts()
    {
        var userId = Guid.NewGuid();
        _wishlistRepo.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync([]);

        var result = await _handler.Handle(new GetWishlistQuery { UserId = userId }, CancellationToken.None);

        result.Items.Should().BeEmpty();
        result.UpdatedAt.Should().BeNull();
        _productsRepo.Verify(
            r => r.FindByCondition(It.IsAny<System.Linq.Expressions.Expression<Func<ProductEntity, bool>>>(), false),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WithItems_ReturnsCorrectProductSnapshot()
    {
        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var addedAt = DateTime.UtcNow.AddHours(-1);

        var wishlistItems = new List<WishlistItem>
        {
            new() { Id = Guid.NewGuid(), UserId = userId, ProductId = productId, AddedAt = addedAt }
        };
        var products = new List<ProductEntity>
        {
            new()
            {
                Id = productId,
                Name = "Cà Phê Sữa",
                Slug = "ca-phe-sua",
                BasePrice = 45_000,
                ThumbnailUrl = "products/espresso.jpg",
                Status = EProductStatus.Active,
            }
        };

        _wishlistRepo.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync(wishlistItems);
        _productsRepo.Setup(r => r.FindByCondition(
                It.IsAny<System.Linq.Expressions.Expression<Func<ProductEntity, bool>>>(), false))
            .Returns(products.BuildMock());

        var result = await _handler.Handle(new GetWishlistQuery { UserId = userId }, CancellationToken.None);

        result.Items.Should().HaveCount(1);
        var item = result.Items[0];
        item.ProductId.Should().Be(productId.ToString());
        item.ProductName.Should().Be("Cà Phê Sữa");
        item.ProductSlug.Should().Be("ca-phe-sua");
        item.BasePrice.Should().Be(45_000);
        item.AddedAt.Should().Be(addedAt);
    }

    [Fact]
    public async Task Handle_ProductStatusActive_InStockIsTrue()
    {
        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();

        SetupSingleItem(userId, productId, EProductStatus.Active);

        var result = await _handler.Handle(new GetWishlistQuery { UserId = userId }, CancellationToken.None);

        result.Items[0].InStock.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ProductStatusInactive_InStockIsFalse()
    {
        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();

        SetupSingleItem(userId, productId, EProductStatus.Inactive);

        var result = await _handler.Handle(new GetWishlistQuery { UserId = userId }, CancellationToken.None);

        result.Items[0].InStock.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_ProductStatusOutOfStock_InStockIsFalse()
    {
        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();

        SetupSingleItem(userId, productId, EProductStatus.OutOfStock);

        var result = await _handler.Handle(new GetWishlistQuery { UserId = userId }, CancellationToken.None);

        result.Items[0].InStock.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_ThumbnailWithStorageKey_ResolvedToCdnUrl()
    {
        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();

        SetupSingleItem(userId, productId, thumbnailUrl: "products/espresso.jpg");

        var result = await _handler.Handle(new GetWishlistQuery { UserId = userId }, CancellationToken.None);

        result.Items[0].ThumbnailUrl.Should().Be("https://cdn.example.com/products/espresso.jpg");
    }

    [Fact]
    public async Task Handle_ThumbnailNull_ThumbnailUrlIsNull()
    {
        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();

        SetupSingleItem(userId, productId, thumbnailUrl: null);

        var result = await _handler.Handle(new GetWishlistQuery { UserId = userId }, CancellationToken.None);

        result.Items[0].ThumbnailUrl.Should().BeNull();
    }

    [Fact]
    public async Task Handle_ThumbnailIsAbsoluteUrl_ReturnedAsIs()
    {
        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        const string absoluteUrl = "https://legacy.cdn.example.com/old/path.jpg";

        SetupSingleItem(userId, productId, thumbnailUrl: absoluteUrl);

        var result = await _handler.Handle(new GetWishlistQuery { UserId = userId }, CancellationToken.None);

        result.Items[0].ThumbnailUrl.Should().Be(absoluteUrl);
    }

    [Fact]
    public async Task Handle_UpdatedAt_IsMaxAddedAtAcrossItems()
    {
        var userId = Guid.NewGuid();
        var p1 = Guid.NewGuid();
        var p2 = Guid.NewGuid();
        var older = DateTime.UtcNow.AddDays(-2);
        var newer = DateTime.UtcNow.AddDays(-1);

        var wishlistItems = new List<WishlistItem>
        {
            new() { Id = Guid.NewGuid(), UserId = userId, ProductId = p1, AddedAt = older },
            new() { Id = Guid.NewGuid(), UserId = userId, ProductId = p2, AddedAt = newer },
        };
        var products = new List<ProductEntity>
        {
            new() { Id = p1, Name = "A", Slug = "a", Status = EProductStatus.Active },
            new() { Id = p2, Name = "B", Slug = "b", Status = EProductStatus.Active },
        };

        _wishlistRepo.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync(wishlistItems);
        _productsRepo.Setup(r => r.FindByCondition(
                It.IsAny<System.Linq.Expressions.Expression<Func<ProductEntity, bool>>>(), false))
            .Returns(products.BuildMock());

        var result = await _handler.Handle(new GetWishlistQuery { UserId = userId }, CancellationToken.None);

        result.UpdatedAt.Should().Be(newer);
    }

    [Fact]
    public async Task Handle_ProductDeletedFromCatalog_ItemSilentlyExcluded()
    {
        var userId = Guid.NewGuid();
        var existingProductId = Guid.NewGuid();
        var deletedProductId = Guid.NewGuid();

        var wishlistItems = new List<WishlistItem>
        {
            new() { Id = Guid.NewGuid(), UserId = userId, ProductId = existingProductId, AddedAt = DateTime.UtcNow },
            new() { Id = Guid.NewGuid(), UserId = userId, ProductId = deletedProductId,  AddedAt = DateTime.UtcNow },
        };
        // Only the existing product is returned from the DB (deleted one is gone)
        var products = new List<ProductEntity>
        {
            new() { Id = existingProductId, Name = "Còn Tồn", Slug = "con-ton", Status = EProductStatus.Active }
        };

        _wishlistRepo.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync(wishlistItems);
        _productsRepo.Setup(r => r.FindByCondition(
                It.IsAny<System.Linq.Expressions.Expression<Func<ProductEntity, bool>>>(), false))
            .Returns(products.BuildMock());

        var result = await _handler.Handle(new GetWishlistQuery { UserId = userId }, CancellationToken.None);

        result.Items.Should().HaveCount(1);
        result.Items[0].ProductId.Should().Be(existingProductId.ToString());
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────

    private void SetupSingleItem(
        Guid userId,
        Guid productId,
        EProductStatus status = EProductStatus.Active,
        string? thumbnailUrl = "products/thumb.jpg")
    {
        _wishlistRepo.Setup(r => r.GetByUserIdAsync(userId))
            .ReturnsAsync([new WishlistItem
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                ProductId = productId,
                AddedAt = DateTime.UtcNow,
            }]);
        _productsRepo.Setup(r => r.FindByCondition(
                It.IsAny<System.Linq.Expressions.Expression<Func<ProductEntity, bool>>>(), false))
            .Returns(new List<ProductEntity>
            {
                new()
                {
                    Id = productId,
                    Name = "Test Product",
                    Slug = "test-product",
                    BasePrice = 45_000,
                    ThumbnailUrl = thumbnailUrl,
                    Status = status,
                }
            }.BuildMock());
    }
}
