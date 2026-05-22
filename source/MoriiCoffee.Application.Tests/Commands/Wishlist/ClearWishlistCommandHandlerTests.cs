using FluentAssertions;
using Moq;
using MoriiCoffee.Application.Commands.Wishlist.ClearWishlist;
using MoriiCoffee.Domain.Repositories;
using MoriiCoffee.Domain.SeedWork.Persistence;
using Xunit;

namespace MoriiCoffee.Application.Tests.Commands.Wishlist;

public class ClearWishlistCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IWishlistItemRepository> _wishlistRepo = new();
    private readonly ClearWishlistCommandHandler _handler;

    public ClearWishlistCommandHandlerTests()
    {
        _unitOfWork.Setup(u => u.WishlistItems).Returns(_wishlistRepo.Object);
        _unitOfWork.Setup(u => u.CommitAsync()).ReturnsAsync(1);
        _handler = new ClearWishlistCommandHandler(_unitOfWork.Object);
    }

    [Fact]
    public async Task Handle_CallsClearAsyncWithCorrectUserId()
    {
        var userId = Guid.NewGuid();

        await _handler.Handle(new ClearWishlistCommand { UserId = userId }, CancellationToken.None);

        _wishlistRepo.Verify(r => r.ClearAsync(userId), Times.Once);
    }

    [Fact]
    public async Task Handle_CommitsAfterClear()
    {
        await _handler.Handle(new ClearWishlistCommand { UserId = Guid.NewGuid() }, CancellationToken.None);

        _unitOfWork.Verify(u => u.CommitAsync(), Times.Once);
    }

    [Fact]
    public async Task Handle_ReturnsTrue()
    {
        var result = await _handler.Handle(
            new ClearWishlistCommand { UserId = Guid.NewGuid() }, CancellationToken.None);

        result.Should().BeTrue();
    }
}
