using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using MoriiCoffee.Application.SeedWork.DTOs.Category;
using MoriiCoffee.Application.SeedWork.Mappings;
using Xunit;
using CategoryEntity = MoriiCoffee.Domain.Aggregates.CategoryAggregate.Category;

namespace MoriiCoffee.Application.Tests.Mappings;

public class CategoryMapperTests
{
    private readonly IMapper _mapper;

    public CategoryMapperTests()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<CategoryMapper>(), NullLoggerFactory.Instance);
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
}
