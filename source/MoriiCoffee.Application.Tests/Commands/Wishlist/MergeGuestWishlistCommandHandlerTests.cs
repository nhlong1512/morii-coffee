using System.Linq.Expressions;
using FluentAssertions;
using MockQueryable;
using Moq;
using MoriiCoffee.Application.Commands.Wishlist.MergeGuestWishlist;
using MoriiCoffee.Domain.Aggregates.WishlistAggregate;
using MoriiCoffee.Domain.Repositories;
using MoriiCoffee.Domain.SeedWork.Persistence;
using MoriiCoffee.Domain.Shared.Enums.Product;
using MoriiCoffee.Domain.Shared.Settings;
using Xunit;
using ProductEntity = MoriiCoffee.Domain.Aggregates.ProductAggregate.Product;

namespace MoriiCoffee.Application.Tests.Commands.Wishlist;

public class MergeGuestWishlistCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IWishlistItemRepository> _wishlistRepo = new();
    private readonly Mock<IProductsRepository> _productsRepo = new();
    private readonly AwsS3Settings _s3Settings = new() { CdnBaseUrl = "https://cdn.example.com" };
    private readonly MergeGuestWishlistCommandHandler _handler;

    public MergeGuestWishlistCommandHandlerTests()
    {
        _unitOfWork.Setup(u => u.WishlistItems).Returns(_wishlistRepo.Object);
        _unitOfWork.Setup(u => u.Products).Returns(_productsRepo.Object);
        _unitOfWork.Setup(u => u.CommitAsync()).ReturnsAsync(1);
        _handler = new MergeGuestWishlistCommandHandler(_unitOfWork.Object, _s3Settings);
    }

    [Fact]
    public async Task Handle_EmptyGuestItems_SkipsInsertAndReturnsExistingWishlist()
    {
        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var existingItem = BuildWishlistItem(userId, productId);
        var product = BuildProduct(productId);

        _wishlistRepo.Setup(r => r.GetByUserIdAsync(userId))
            .ReturnsAsync([existingItem]);
        _productsRepo.Setup(r => r.FindByCondition(
                It.IsAny<System.Linq.Expressions.Expression<Func<ProductEntity, bool>>>(), false))
            .Returns(new List<ProductEntity> { product }.BuildMock());

        var result = await _handler.Handle(
            new MergeGuestWishlistCommand { UserId = userId, GuestProductIds = [] },
            CancellationToken.None);

        result.Items.Should().HaveCount(1);
        _wishlistRepo.Verify(r => r.AddAsync(It.IsAny<WishlistItem>()), Times.Never);
        _unitOfWork.Verify(u => u.CommitAsync(), Times.Never);
    }

    [Fact]
    public async Task Handle_NewGuestItems_InsertsOnlyItemsNotAlreadyInWishlist()
    {
        var userId = Guid.NewGuid();
        var existingProductId = Guid.NewGuid();
        var newProductId = Guid.NewGuid();

        var existingItem = BuildWishlistItem(userId, existingProductId);
        var mergedItems = new List<WishlistItem>
        {
            BuildWishlistItem(userId, existingProductId),
            BuildWishlistItem(userId, newProductId),
        };
        var products = new List<ProductEntity>
        {
            BuildProduct(existingProductId),
            BuildProduct(newProductId),
        };

        _wishlistRepo
            .SetupSequence(r => r.GetByUserIdAsync(userId))
            .ReturnsAsync([existingItem])
            .ReturnsAsync(mergedItems);

        // Apply the predicate so each FindByCondition call only returns matching products
        _productsRepo.Setup(r => r.FindByCondition(
                It.IsAny<Expression<Func<ProductEntity, bool>>>(), false))
            .Returns((Expression<Func<ProductEntity, bool>> predicate, bool _) =>
                products.Where(predicate.Compile()).ToList().BuildMock());

        var result = await _handler.Handle(new MergeGuestWishlistCommand
        {
            UserId = userId,
            GuestProductIds = [existingProductId, newProductId],
        }, CancellationToken.None);

        _wishlistRepo.Verify(r => r.AddAsync(It.Is<WishlistItem>(i =>
            i.ProductId == newProductId && i.UserId == userId)), Times.Once);
        _wishlistRepo.Verify(r => r.AddAsync(It.Is<WishlistItem>(i =>
            i.ProductId == existingProductId)), Times.Never);
        _unitOfWork.Verify(u => u.CommitAsync(), Times.Once);
        result.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task Handle_GuestItemWithInvalidProductId_SilentlyIgnored()
    {
        var userId = Guid.NewGuid();
        var invalidId = Guid.NewGuid();

        _wishlistRepo.Setup(r => r.GetByUserIdAsync(userId))
            .ReturnsAsync([]);
        // FindByCondition returns empty — product doesn't exist in DB
        _productsRepo.Setup(r => r.FindByCondition(
                It.IsAny<System.Linq.Expressions.Expression<Func<ProductEntity, bool>>>(), false))
            .Returns(new List<ProductEntity>().BuildMock());

        var result = await _handler.Handle(new MergeGuestWishlistCommand
        {
            UserId = userId,
            GuestProductIds = [invalidId],
        }, CancellationToken.None);

        // No items inserted — product IDs not found in DB
        _wishlistRepo.Verify(r => r.AddAsync(It.IsAny<WishlistItem>()), Times.Never);
        result.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_DuplicateGuestProductIds_InsertedOnlyOnce()
    {
        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var product = BuildProduct(productId);
        var mergedItem = BuildWishlistItem(userId, productId);

        _wishlistRepo
            .SetupSequence(r => r.GetByUserIdAsync(userId))
            .ReturnsAsync([])
            .ReturnsAsync([mergedItem]);
        _productsRepo.Setup(r => r.FindByCondition(
                It.IsAny<System.Linq.Expressions.Expression<Func<ProductEntity, bool>>>(), false))
            .Returns(new List<ProductEntity> { product }.BuildMock());

        await _handler.Handle(new MergeGuestWishlistCommand
        {
            UserId = userId,
            GuestProductIds = [productId, productId],
        }, CancellationToken.None);

        _wishlistRepo.Verify(r => r.AddAsync(It.IsAny<WishlistItem>()), Times.Once);
    }

    [Fact]
    public async Task Handle_MappedItem_InStockTrueWhenProductStatusIsActive()
    {
        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var item = BuildWishlistItem(userId, productId);
        var product = BuildProduct(productId, status: EProductStatus.Active);

        _wishlistRepo.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync([item]);
        _productsRepo.Setup(r => r.FindByCondition(
                It.IsAny<System.Linq.Expressions.Expression<Func<ProductEntity, bool>>>(), false))
            .Returns(new List<ProductEntity> { product }.BuildMock());

        var result = await _handler.Handle(
            new MergeGuestWishlistCommand { UserId = userId, GuestProductIds = [] },
            CancellationToken.None);

        result.Items.Should().HaveCount(1);
        result.Items[0].InStock.Should().BeTrue();
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────

    private static WishlistItem BuildWishlistItem(Guid userId, Guid productId) => new()
    {
        Id = Guid.NewGuid(),
        UserId = userId,
        ProductId = productId,
        AddedAt = DateTime.UtcNow,
    };

    private static ProductEntity BuildProduct(
        Guid productId,
        EProductStatus status = EProductStatus.Active) => new()
    {
        Id = productId,
        Name = "Cà Phê Sữa",
        Slug = "ca-phe-sua",
        BasePrice = 45_000,
        ThumbnailUrl = "products/espresso.jpg",
        Status = status,
    };
}
