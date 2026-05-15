using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using MoriiCoffee.Application.SeedWork.DTOs.Category;
using MoriiCoffee.Application.SeedWork.Mappings;
using MoriiCoffee.Domain.Shared.Settings;
using Xunit;
using CategoryEntity = MoriiCoffee.Domain.Aggregates.CategoryAggregate.Category;

namespace MoriiCoffee.Application.Tests.Mappings;

public class CategoryMapperTests
{
    private readonly IMapper _mapper;
    private static readonly AwsS3Settings S3Settings = new() { CdnBaseUrl = "https://cdn.test" };

    public CategoryMapperTests()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile(new CategoryMapper(S3Settings)), NullLoggerFactory.Instance);
        config.AssertConfigurationIsValid();
        _mapper = config.CreateMapper();
    }

    [Fact]
    public void CategoryToCategoryDto_MapsCorrectly()
    {
        var categoryId = Guid.NewGuid();
        var category = new CategoryEntity
        {
            Id = categoryId,
            Name = "Cold Brew",
            DisplayOrder = 1,
            IsActive = true
        };

        var dto = _mapper.Map<CategoryDto>(category);

        dto.Id.Should().Be(categoryId);
        dto.Name.Should().Be("Cold Brew");
        dto.DisplayOrder.Should().Be(1);
        dto.IsActive.Should().BeTrue();
    }

    [Fact]
    public void CategoryToCategoryDto_StorageKey_ResolvesToCdnUrl()
    {
        var category = new CategoryEntity
        {
            Id = Guid.NewGuid(),
            Name = "Espresso",
            IconUrl = "categories/abc/123-espresso.png"
        };

        var dto = _mapper.Map<CategoryDto>(category);

        dto.IconUrl.Should().Be("https://cdn.test/categories/abc/123-espresso.png");
    }

    [Fact]
    public void CategoryToCategoryDto_AbsoluteUrl_PassthroughAsIs()
    {
        var category = new CategoryEntity
        {
            Id = Guid.NewGuid(),
            Name = "Espresso",
            IconUrl = "https://legacy-cdn.example.com/categories/espresso.png"
        };

        var dto = _mapper.Map<CategoryDto>(category);

        dto.IconUrl.Should().Be("https://legacy-cdn.example.com/categories/espresso.png");
    }
}
