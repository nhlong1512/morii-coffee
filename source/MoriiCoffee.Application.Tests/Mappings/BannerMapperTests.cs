using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using MoriiCoffee.Application.SeedWork.DTOs.Banner;
using MoriiCoffee.Application.SeedWork.Mappings;
using MoriiCoffee.Domain.Shared.Settings;
using Xunit;
using BannerEntity = MoriiCoffee.Domain.Aggregates.BannerAggregate.Banner;

namespace MoriiCoffee.Application.Tests.Mappings;

public class BannerMapperTests
{
    private readonly IMapper _mapper;
    private static readonly AwsS3Settings S3Settings = new() { CdnBaseUrl = "https://cdn.test" };

    public BannerMapperTests()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile(new BannerMapper(S3Settings)), NullLoggerFactory.Instance);
        config.AssertConfigurationIsValid();
        _mapper = config.CreateMapper();
    }

    [Fact]
    public void BannerToBannerDto_MapsCorrectly()
    {
        var bannerId = Guid.NewGuid();
        var banner = new BannerEntity
        {
            Id = bannerId,
            Title = "Summer Sale",
            DisplayOrder = 1,
            IsActive = true
        };

        var dto = _mapper.Map<BannerDto>(banner);

        dto.Id.Should().Be(bannerId);
        dto.Title.Should().Be("Summer Sale");
        dto.DisplayOrder.Should().Be(1);
        dto.IsActive.Should().BeTrue();
    }
}
