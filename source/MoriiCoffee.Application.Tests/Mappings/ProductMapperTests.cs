using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using MoriiCoffee.Application.SeedWork.DTOs.Product;
using MoriiCoffee.Application.SeedWork.DTOs.ProductImage;
using MoriiCoffee.Application.SeedWork.DTOs.ProductVariant;
using MoriiCoffee.Application.SeedWork.Mappings;
using MoriiCoffee.Domain.Aggregates.ProductAggregate;
using MoriiCoffee.Domain.Aggregates.ProductAggregate.Entities;
using MoriiCoffee.Domain.Aggregates.ProductAggregate.ValueObjects;
using Xunit;
using CategoryEntity = MoriiCoffee.Domain.Aggregates.CategoryAggregate.Category;
using ProductEntity = MoriiCoffee.Domain.Aggregates.ProductAggregate.Product;

namespace MoriiCoffee.Application.Tests.Mappings;

public class ProductMapperTests
{
    private readonly IMapper _mapper;

    public ProductMapperTests()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<ProductMapper>();
            cfg.AddProfile<CategoryMapper>(); // ProductDto.Categories maps Category → CategoryDto
        }, NullLoggerFactory.Instance);
        config.AssertConfigurationIsValid();
        _mapper = config.CreateMapper();
    }

    [Fact]
    public void ProductToProductDto_MapsCorrectly()
    {
        var productId = Guid.NewGuid();
        var product = new ProductEntity
        {
            Id = productId,
            Name = "Iced Latte",
            Slug = "iced-latte",
            BasePrice = 55_000m,
            ProductCategories = new List<ProductCategory>(),
            Variants = new List<ProductVariant>(),
            Images = new List<ProductImage>()
        };

        var dto = _mapper.Map<ProductDto>(product);

        dto.Id.Should().Be(productId);
        dto.Name.Should().Be("Iced Latte");
        dto.Slug.Should().Be("iced-latte");
        dto.BasePrice.Should().Be(55_000m);
    }

    [Fact]
    public void ProductToProductSummaryDto_MapsCorrectly()
    {
        var product = new ProductEntity
        {
            Id = Guid.NewGuid(),
            Name = "Espresso",
            Slug = "espresso",
            BasePrice = 45_000m,
            ProductCategories = new List<ProductCategory>()
        };

        var dto = _mapper.Map<ProductSummaryDto>(product);

        dto.Name.Should().Be("Espresso");
        dto.Slug.Should().Be("espresso");
        dto.BasePrice.Should().Be(45_000m);
        dto.CategoryNames.Should().BeEmpty();
    }

    [Fact]
    public void ProductVariantToProductVariantDto_MapsCorrectly()
    {
        var variant = new ProductVariant
        {
            Id = Guid.NewGuid(),
            Name = "Medium",
            AdditionalPrice = 10_000m
        };

        var dto = _mapper.Map<ProductVariantDto>(variant);

        dto.Name.Should().Be("Medium");
        dto.AdditionalPrice.Should().Be(10_000m);
        dto.TotalPrice.Should().Be(0m); // TotalPrice is ignored in mapper
    }

    [Fact]
    public void ProductImageToProductImageDto_MapsCorrectly()
    {
        var image = new ProductImage
        {
            Id = Guid.NewGuid(),
            Url = "https://cdn.test/photo.jpg",
            DisplayOrder = 1
        };

        var dto = _mapper.Map<ProductImageDto>(image);

        dto.Url.Should().Be("https://cdn.test/photo.jpg");
        dto.DisplayOrder.Should().Be(1);
    }
}
