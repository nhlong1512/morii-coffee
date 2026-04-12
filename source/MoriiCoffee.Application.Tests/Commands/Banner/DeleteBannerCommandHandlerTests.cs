using FluentAssertions;
using Moq;
using MoriiCoffee.Application.Commands.Banner.DeleteBanner;
using MoriiCoffee.Application.SeedWork.Exceptions;
using MoriiCoffee.Domain.Repositories;
using MoriiCoffee.Domain.SeedWork.Persistence;
using Xunit;
using BannerEntity = MoriiCoffee.Domain.Aggregates.BannerAggregate.Banner;

namespace MoriiCoffee.Application.Tests.Commands.Banner;

public class DeleteBannerCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IBannersRepository> _bannersRepo = new();
    private readonly DeleteBannerCommandHandler _handler;

    public DeleteBannerCommandHandlerTests()
    {
        _unitOfWork.Setup(u => u.Banners).Returns(_bannersRepo.Object);
        _handler = new DeleteBannerCommandHandler(_unitOfWork.Object);
    }

    [Fact]
    public async Task Handle_Success_SoftDeletesAndReturnsTrue()
    {
        var bannerId = Guid.NewGuid();
        var banner = new BannerEntity { Id = bannerId, Title = "Summer Sale" };
        _bannersRepo.Setup(r => r.GetByIdAsync(bannerId)).ReturnsAsync(banner);
        _bannersRepo.Setup(r => r.SoftDelete(banner)).Returns(Task.CompletedTask);
        _unitOfWork.Setup(u => u.CommitAsync()).ReturnsAsync(1);

        var result = await _handler.Handle(new DeleteBannerCommand(bannerId), CancellationToken.None);

        result.Should().BeTrue();
        _bannersRepo.Verify(r => r.SoftDelete(banner), Times.Once);
        _unitOfWork.Verify(u => u.CommitAsync(), Times.Once);
    }

    [Fact]
    public async Task Handle_BannerNotFound_ThrowsNotFoundException()
    {
        var bannerId = Guid.NewGuid();
        _bannersRepo.Setup(r => r.GetByIdAsync(bannerId)).ReturnsAsync((BannerEntity)null!);

        await _handler.Invoking(h => h.Handle(new DeleteBannerCommand(bannerId), CancellationToken.None))
            .Should().ThrowAsync<NotFoundException>();
    }
}
