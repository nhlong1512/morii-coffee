using AutoMapper;
using FluentAssertions;
using Moq;
using MoriiCoffee.Application.Queries.ProductVariant.GetVariantsByProductId;
using MoriiCoffee.Application.SeedWork.DTOs.ProductVariant;
using MoriiCoffee.Application.SeedWork.Exceptions;
using MoriiCoffee.Domain.Repositories;
using MoriiCoffee.Domain.SeedWork.Persistence;
using Xunit;
using ProductEntity = MoriiCoffee.Domain.Aggregates.ProductAggregate.Product;
using ProductVariantEntity = MoriiCoffee.Domain.Aggregates.ProductAggregate.Entities.ProductVariant;

namespace MoriiCoffee.Application.Tests.Queries.ProductVariant;

public class GetVariantsByProductIdQueryHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IProductsRepository> _productsRepo = new();
    private readonly Mock<IProductVariantsRepository> _variantsRepo = new();
    private readonly Mock<IMapper> _mapper = new();
    private readonly GetVariantsByProductIdQueryHandler _handler;

    public GetVariantsByProductIdQueryHandlerTests()
    {
        _unitOfWork.Setup(u => u.Products).Returns(_productsRepo.Object);
        _unitOfWork.Setup(u => u.ProductVariants).Returns(_variantsRepo.Object);
        _handler = new GetVariantsByProductIdQueryHandler(_unitOfWork.Object, _mapper.Object);
    }

    [Fact]
    public async Task Handle_ProductAndVariantsFound_ReturnsVariantDtos()
    {
        var productId = Guid.NewGuid();
        var product = new ProductEntity { Id = productId, BasePrice = 50_000m };
        var variant = new ProductVariantEntity
        {
            Id = Guid.NewGuid(),
            ProductId = productId,
            Name = "Medium",
            AdditionalPrice = 10_000m
        };

        _productsRepo.Setup(r => r.GetByIdAsync(productId)).ReturnsAsync(product);
        _variantsRepo.Setup(r => r.GetByProductIdAsync(productId))
            .ReturnsAsync(new List<ProductVariantEntity> { variant });
        _mapper.Setup(m => m.Map<ProductVariantDto>(variant))
            .Returns(new ProductVariantDto { Name = "Medium", AdditionalPrice = 10_000m });

        var result = await _handler.Handle(new GetVariantsByProductIdQuery(productId), CancellationToken.None);

        result.Should().HaveCount(1);
        result.First().Name.Should().Be("Medium");
        result.First().TotalPrice.Should().Be(60_000m);
    }

    [Fact]
    public async Task Handle_ProductNotFound_ThrowsNotFoundException()
    {
        var productId = Guid.NewGuid();
        _productsRepo.Setup(r => r.GetByIdAsync(productId)).ReturnsAsync((ProductEntity)null!);

        await _handler.Invoking(h => h.Handle(new GetVariantsByProductIdQuery(productId), CancellationToken.None))
            .Should().ThrowAsync<NotFoundException>();
    }
}
