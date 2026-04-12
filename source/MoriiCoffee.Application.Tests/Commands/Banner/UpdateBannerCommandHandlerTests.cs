using AutoMapper;
using FluentAssertions;
using Moq;
using MoriiCoffee.Application.Commands.Banner.UpdateBanner;
using MoriiCoffee.Application.SeedWork.Abstractions;
using MoriiCoffee.Application.SeedWork.DTOs.Banner;
using MoriiCoffee.Application.SeedWork.Exceptions;
using MoriiCoffee.Domain.Repositories;
using MoriiCoffee.Domain.SeedWork.Persistence;
using Xunit;
using BannerEntity = MoriiCoffee.Domain.Aggregates.BannerAggregate.Banner;

namespace MoriiCoffee.Application.Tests.Commands.Banner;

public class UpdateBannerCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IBannersRepository> _bannersRepo = new();
    private readonly Mock<IFileService> _fileService = new();
    private readonly Mock<IMapper> _mapper = new();
    private readonly UpdateBannerCommandHandler _handler;

    public UpdateBannerCommandHandlerTests()
    {
        _unitOfWork.Setup(u => u.Banners).Returns(_bannersRepo.Object);
        _handler = new UpdateBannerCommandHandler(_unitOfWork.Object, _fileService.Object, _mapper.Object);
    }

    [Fact]
    public async Task Handle_Success_UpdatesAndReturnsBannerDto()
    {
        var bannerId = Guid.NewGuid();
        var banner = new BannerEntity { Id = bannerId, Title = "Old Title" };
        var cmd = new UpdateBannerCommand(bannerId, new UpdateBannerDto
        {
            Title = "New Title",
            DisplayOrder = 1,
            IsActive = true
        });

        _bannersRepo.Setup(r => r.GetByIdAsync(bannerId)).ReturnsAsync(banner);
        _bannersRepo.Setup(r => r.Update(banner)).Returns(Task.CompletedTask);
        _unitOfWork.Setup(u => u.CommitAsync()).ReturnsAsync(1);
        _mapper.Setup(m => m.Map<BannerDto>(banner)).Returns(new BannerDto { Title = "New Title" });

        var result = await _handler.Handle(cmd, CancellationToken.None);

        result.Should().NotBeNull();
        result.Title.Should().Be("New Title");
        _unitOfWork.Verify(u => u.CommitAsync(), Times.Once);
    }

    [Fact]
    public async Task Handle_BannerNotFound_ThrowsNotFoundException()
    {
        var bannerId = Guid.NewGuid();
        _bannersRepo.Setup(r => r.GetByIdAsync(bannerId)).ReturnsAsync((BannerEntity)null!);

        var cmd = new UpdateBannerCommand(bannerId, new UpdateBannerDto { Title = "X", DisplayOrder = 0 });

        await _handler.Invoking(h => h.Handle(cmd, CancellationToken.None))
            .Should().ThrowAsync<NotFoundException>();
    }
}
