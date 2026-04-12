using AutoMapper;
using FluentAssertions;
using Moq;
using MoriiCoffee.Application.Queries.Product.GetPaginatedProducts;
using MoriiCoffee.Application.SeedWork.DTOs.Product;
using MoriiCoffee.Domain.Repositories;
using MoriiCoffee.Domain.SeedWork.Persistence;
using MoriiCoffee.Domain.Shared.SeedWork;
using Xunit;
using ProductEntity = MoriiCoffee.Domain.Aggregates.ProductAggregate.Product;

namespace MoriiCoffee.Application.Tests.Queries.Product;

public class GetPaginatedProductsQueryHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IProductsRepository> _productsRepo = new();
    private readonly Mock<IMapper> _mapper = new();
    private readonly GetPaginatedProductsQueryHandler _handler;

    public GetPaginatedProductsQueryHandlerTests()
    {
        _unitOfWork.Setup(u => u.Products).Returns(_productsRepo.Object);
        _handler = new GetPaginatedProductsQueryHandler(_unitOfWork.Object, _mapper.Object);
    }

    [Fact]
    public async Task Handle_WithProducts_ReturnsPaginatedResult()
    {
        var product1 = new ProductEntity { Id = Guid.NewGuid(), Name = "Latte", DisplayOrder = 1 };
        var product2 = new ProductEntity { Id = Guid.NewGuid(), Name = "Espresso", DisplayOrder = 2 };
        var products = new List<ProductEntity> { product1, product2 }.AsQueryable();

        _productsRepo.Setup(r => r.FindAll(false)).Returns(products);
        _mapper.Setup(m => m.Map<ProductSummaryDto>(It.IsAny<ProductEntity>()))
            .Returns((ProductEntity p) => new ProductSummaryDto { Name = p.Name });

        var query = new GetPaginatedProductsQuery(new ProductPaginationFilter { TakeAll = true });
        var result = await _handler.Handle(query, CancellationToken.None);

        result.Should().NotBeNull();
        result.Items.Should().HaveCount(2);
        result.Items.Select(i => i.Name).Should().Contain("Latte").And.Contain("Espresso");
    }

    [Fact]
    public async Task Handle_EmptyProducts_ReturnsEmptyPagination()
    {
        _productsRepo.Setup(r => r.FindAll(false)).Returns(new List<ProductEntity>().AsQueryable());
        _mapper.Setup(m => m.Map<ProductSummaryDto>(It.IsAny<ProductEntity>()))
            .Returns(new ProductSummaryDto());

        var query = new GetPaginatedProductsQuery(new ProductPaginationFilter { TakeAll = true });
        var result = await _handler.Handle(query, CancellationToken.None);

        result.Should().NotBeNull();
        result.Items.Should().BeEmpty();
        result.Metadata.TotalCount.Should().Be(0);
    }
}
