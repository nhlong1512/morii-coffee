using AutoMapper;
using FluentAssertions;
using Moq;
using MoriiCoffee.Application.Queries.ProductVariant.GetVariantById;
using MoriiCoffee.Application.SeedWork.DTOs.ProductVariant;
using MoriiCoffee.Application.SeedWork.Exceptions;
using MoriiCoffee.Domain.Repositories;
using MoriiCoffee.Domain.SeedWork.Persistence;
using Xunit;
using ProductEntity = MoriiCoffee.Domain.Aggregates.ProductAggregate.Product;
using ProductVariantEntity = MoriiCoffee.Domain.Aggregates.ProductAggregate.Entities.ProductVariant;

namespace MoriiCoffee.Application.Tests.Queries.ProductVariant;

public class GetVariantByIdQueryHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IProductVariantsRepository> _variantsRepo = new();
    private readonly Mock<IMapper> _mapper = new();
    private readonly GetVariantByIdQueryHandler _handler;

    public GetVariantByIdQueryHandlerTests()
    {
        _unitOfWork.Setup(u => u.ProductVariants).Returns(_variantsRepo.Object);
        _handler = new GetVariantByIdQueryHandler(_unitOfWork.Object, _mapper.Object);
    }

    [Fact]
    public async Task Handle_VariantFound_ReturnsVariantDtoWithTotalPrice()
    {
        var variantId = Guid.NewGuid();
        var product = new ProductEntity { Id = Guid.NewGuid(), BasePrice = 50_000m };
        var variant = new ProductVariantEntity
        {
            Id = variantId,
            Name = "Large",
            AdditionalPrice = 15_000m,
            Product = product
        };

        _variantsRepo.Setup(r => r.GetByIdAsync(variantId,
                It.IsAny<System.Linq.Expressions.Expression<Func<ProductVariantEntity, object>>>()))
            .ReturnsAsync(variant);
        _mapper.Setup(m => m.Map<ProductVariantDto>(variant))
            .Returns(new ProductVariantDto { Name = "Large", AdditionalPrice = 15_000m });

        var result = await _handler.Handle(new GetVariantByIdQuery(variantId), CancellationToken.None);

        result.Should().NotBeNull();
        result.Name.Should().Be("Large");
        result.TotalPrice.Should().Be(65_000m);
    }

    [Fact]
    public async Task Handle_VariantNotFound_ThrowsNotFoundException()
    {
        var variantId = Guid.NewGuid();
        _variantsRepo.Setup(r => r.GetByIdAsync(variantId,
                It.IsAny<System.Linq.Expressions.Expression<Func<ProductVariantEntity, object>>>()))
            .ReturnsAsync((ProductVariantEntity)null!);

        await _handler.Invoking(h => h.Handle(new GetVariantByIdQuery(variantId), CancellationToken.None))
            .Should().ThrowAsync<NotFoundException>();
    }
}
