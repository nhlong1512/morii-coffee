using AutoMapper;
using FluentAssertions;
using Moq;
using MoriiCoffee.Application.Queries.Banner.GetAllBanners;
using MoriiCoffee.Application.SeedWork.DTOs.Banner;
using MoriiCoffee.Domain.Repositories;
using MoriiCoffee.Domain.SeedWork.Persistence;
using Xunit;
using BannerEntity = MoriiCoffee.Domain.Aggregates.BannerAggregate.Banner;

namespace MoriiCoffee.Application.Tests.Queries.Banner;

public class GetAllBannersQueryHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IBannersRepository> _bannersRepo = new();
    private readonly Mock<IMapper> _mapper = new();
    private readonly GetAllBannersQueryHandler _handler;

    public GetAllBannersQueryHandlerTests()
    {
        _unitOfWork.Setup(u => u.Banners).Returns(_bannersRepo.Object);
        _handler = new GetAllBannersQueryHandler(_unitOfWork.Object, _mapper.Object);
    }

    [Fact]
    public async Task Handle_WithBanners_ReturnsBannerDtoList()
    {
        var banners = new List<BannerEntity>
        {
            new() { Id = Guid.NewGuid(), Title = "Summer Sale" },
            new() { Id = Guid.NewGuid(), Title = "New Collection" }
        };
        _bannersRepo.Setup(r => r.GetAllOrderedAsync()).ReturnsAsync(banners);
        _mapper.Setup(m => m.Map<List<BannerDto>>(banners))
            .Returns(new List<BannerDto>
            {
                new() { Title = "Summer Sale" },
                new() { Title = "New Collection" }
            });

        var result = await _handler.Handle(new GetAllBannersQuery(), CancellationToken.None);

        result.Should().HaveCount(2);
        result.Select(b => b.Title).Should().Contain("Summer Sale");
    }

    [Fact]
    public async Task Handle_NoBanners_ReturnsEmptyList()
    {
        _bannersRepo.Setup(r => r.GetAllOrderedAsync()).ReturnsAsync(new List<BannerEntity>());
        _mapper.Setup(m => m.Map<List<BannerDto>>(It.IsAny<List<BannerEntity>>()))
            .Returns(new List<BannerDto>());

        var result = await _handler.Handle(new GetAllBannersQuery(), CancellationToken.None);

        result.Should().BeEmpty();
    }
}
