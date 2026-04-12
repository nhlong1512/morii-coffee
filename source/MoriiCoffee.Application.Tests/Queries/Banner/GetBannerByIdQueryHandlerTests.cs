using AutoMapper;
using FluentAssertions;
using Moq;
using MoriiCoffee.Application.Queries.Banner.GetBannerById;
using MoriiCoffee.Application.SeedWork.DTOs.Banner;
using MoriiCoffee.Application.SeedWork.Exceptions;
using MoriiCoffee.Domain.Repositories;
using MoriiCoffee.Domain.SeedWork.Persistence;
using Xunit;
using BannerEntity = MoriiCoffee.Domain.Aggregates.BannerAggregate.Banner;

namespace MoriiCoffee.Application.Tests.Queries.Banner;

public class GetBannerByIdQueryHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IBannersRepository> _bannersRepo = new();
    private readonly Mock<IMapper> _mapper = new();
    private readonly GetBannerByIdQueryHandler _handler;

    public GetBannerByIdQueryHandlerTests()
    {
        _unitOfWork.Setup(u => u.Banners).Returns(_bannersRepo.Object);
        _handler = new GetBannerByIdQueryHandler(_unitOfWork.Object, _mapper.Object);
    }

    [Fact]
    public async Task Handle_BannerFound_ReturnsBannerDto()
    {
        var bannerId = Guid.NewGuid();
        var banner = new BannerEntity { Id = bannerId, Title = "Summer Sale" };
        _bannersRepo.Setup(r => r.GetByIdAsync(bannerId)).ReturnsAsync(banner);
        _mapper.Setup(m => m.Map<BannerDto>(banner)).Returns(new BannerDto { Title = "Summer Sale" });

        var result = await _handler.Handle(new GetBannerByIdQuery(bannerId), CancellationToken.None);

        result.Should().NotBeNull();
        result.Title.Should().Be("Summer Sale");
    }

    [Fact]
    public async Task Handle_BannerNotFound_ThrowsNotFoundException()
    {
        var bannerId = Guid.NewGuid();
        _bannersRepo.Setup(r => r.GetByIdAsync(bannerId)).ReturnsAsync((BannerEntity)null!);

        await _handler.Invoking(h => h.Handle(new GetBannerByIdQuery(bannerId), CancellationToken.None))
            .Should().ThrowAsync<NotFoundException>();
    }
}
