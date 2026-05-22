using FluentAssertions;
using Moq;
using MoriiCoffee.Application.Commands.Wishlist.RemoveItemFromWishlist;
using MoriiCoffee.Application.SeedWork.Exceptions;
using MoriiCoffee.Domain.Repositories;
using MoriiCoffee.Domain.SeedWork.Persistence;
using Xunit;

namespace MoriiCoffee.Application.Tests.Commands.Wishlist;

public class RemoveItemFromWishlistCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IWishlistItemRepository> _wishlistRepo = new();
    private readonly RemoveItemFromWishlistCommandHandler _handler;

    public RemoveItemFromWishlistCommandHandlerTests()
    {
        _unitOfWork.Setup(u => u.WishlistItems).Returns(_wishlistRepo.Object);
        _unitOfWork.Setup(u => u.CommitAsync()).ReturnsAsync(1);
        _handler = new RemoveItemFromWishlistCommandHandler(_unitOfWork.Object);
    }

    [Fact]
    public async Task Handle_ItemNotInWishlist_ThrowsNotFoundException()
    {
        _wishlistRepo.Setup(r => r.RemoveAsync(It.IsAny<Guid>(), It.IsAny<Guid>()))
            .ReturnsAsync(false);

        var act = () => _handler.Handle(BuildCommand(), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage("*Wishlist item*");
    }

    [Fact]
    public async Task Handle_ItemExists_RemovesAndCommits()
    {
        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        _wishlistRepo.Setup(r => r.RemoveAsync(userId, productId))
            .ReturnsAsync(true);

        var result = await _handler.Handle(BuildCommand(productId, userId), CancellationToken.None);

        result.Should().BeTrue();
        _wishlistRepo.Verify(r => r.RemoveAsync(userId, productId), Times.Once);
        _unitOfWork.Verify(u => u.CommitAsync(), Times.Once);
    }

    [Fact]
    public async Task Handle_ItemNotFound_DoesNotCommit()
    {
        _wishlistRepo.Setup(r => r.RemoveAsync(It.IsAny<Guid>(), It.IsAny<Guid>()))
            .ReturnsAsync(false);

        await Assert.ThrowsAsync<NotFoundException>(
            () => _handler.Handle(BuildCommand(), CancellationToken.None));

        _unitOfWork.Verify(u => u.CommitAsync(), Times.Never);
    }

    private static RemoveItemFromWishlistCommand BuildCommand(
        Guid? productId = null,
        Guid? userId = null) => new()
    {
        UserId = userId ?? Guid.NewGuid(),
        ProductId = productId ?? Guid.NewGuid(),
    };
}
