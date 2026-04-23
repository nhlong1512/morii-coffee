using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using MockQueryable;
using Moq;
using MoriiCoffee.Application.Queries.Product.GetProductById;
using MoriiCoffee.Application.SeedWork.Abstractions;
using MoriiCoffee.Application.SeedWork.DTOs.Product;
using MoriiCoffee.Application.SeedWork.Exceptions;
using MoriiCoffee.Domain.Repositories;
using MoriiCoffee.Domain.SeedWork.Persistence;
using Xunit;
using ProductEntity = MoriiCoffee.Domain.Aggregates.ProductAggregate.Product;

namespace MoriiCoffee.Application.Tests.Queries.Product;

public class GetProductByIdQueryHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IProductsRepository> _productsRepo = new();
    private readonly Mock<IMapper> _mapper = new();
    private readonly Mock<IProductCatalogCache> _catalogCache = new();
    private readonly Mock<ILogger<GetProductByIdQueryHandler>> _logger = new();
    private readonly GetProductByIdQueryHandler _handler;

    public GetProductByIdQueryHandlerTests()
    {
        _unitOfWork.Setup(u => u.Products).Returns(_productsRepo.Object);
        _handler = new GetProductByIdQueryHandler(
            _unitOfWork.Object,
            _mapper.Object,
            _catalogCache.Object,
            _logger.Object);
    }

    [Fact]
    public async Task Handle_ProductFound_ReturnsProductDto()
    {
        var productId = Guid.NewGuid();
        var product = new ProductEntity { Id = productId, Name = "Iced Latte", BasePrice = 55_000m };
        var mockData = new List<ProductEntity> { product }.BuildMock();

        _catalogCache.Setup(c => c.GetDetailAsync(productId)).ReturnsAsync((ProductDto?)null);
        _productsRepo.Setup(r => r.FindByCondition(
                It.IsAny<System.Linq.Expressions.Expression<Func<ProductEntity, bool>>>(), false))
            .Returns(mockData);
        _mapper.Setup(m => m.Map<ProductDto>(product))
            .Returns(new ProductDto { Id = productId, Name = "Iced Latte" });

        var result = await _handler.Handle(new GetProductByIdQuery(productId), CancellationToken.None);

        result.Should().NotBeNull();
        result.Id.Should().Be(productId);
    }

    [Fact]
    public async Task Handle_ProductNotFound_ThrowsNotFoundException()
    {
        var productId = Guid.NewGuid();
        var mockData = new List<ProductEntity>().BuildMock();

        _catalogCache.Setup(c => c.GetDetailAsync(productId)).ReturnsAsync((ProductDto?)null);
        _productsRepo.Setup(r => r.FindByCondition(
                It.IsAny<System.Linq.Expressions.Expression<Func<ProductEntity, bool>>>(), false))
            .Returns(mockData);

        await _handler.Invoking(h => h.Handle(new GetProductByIdQuery(productId), CancellationToken.None))
            .Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_CacheHit_ReturnsCachedResult_WithoutHittingDatabase()
    {
        var productId = Guid.NewGuid();
        var cachedDto = new ProductDto { Id = productId, Name = "Cached Latte" };

        _catalogCache.Setup(c => c.GetDetailAsync(productId)).ReturnsAsync(cachedDto);

        var result = await _handler.Handle(new GetProductByIdQuery(productId), CancellationToken.None);

        result.Should().Be(cachedDto);
        _productsRepo.Verify(r => r.FindByCondition(
            It.IsAny<System.Linq.Expressions.Expression<Func<ProductEntity, bool>>>(), It.IsAny<bool>()), Times.Never);
    }

    [Fact]
    public async Task Handle_CacheMiss_QueriesDatabaseAndPopulatesCache()
    {
        var productId = Guid.NewGuid();
        var product = new ProductEntity { Id = productId, Name = "Iced Latte", BasePrice = 55_000m };
        var mockData = new List<ProductEntity> { product }.BuildMock();

        _catalogCache.Setup(c => c.GetDetailAsync(productId)).ReturnsAsync((ProductDto?)null);
        _catalogCache.Setup(c => c.SetDetailAsync(productId, It.IsAny<ProductDto>())).Returns(Task.CompletedTask);
        _productsRepo.Setup(r => r.FindByCondition(
                It.IsAny<System.Linq.Expressions.Expression<Func<ProductEntity, bool>>>(), false))
            .Returns(mockData);
        _mapper.Setup(m => m.Map<ProductDto>(product))
            .Returns(new ProductDto { Id = productId, Name = "Iced Latte", Variants = new() });

        await _handler.Handle(new GetProductByIdQuery(productId), CancellationToken.None);

        _catalogCache.Verify(c => c.SetDetailAsync(productId, It.IsAny<ProductDto>()), Times.Once);
    }

    [Fact]
    public async Task Handle_RedisUnavailable_FallsBackToDatabase()
    {
        var productId = Guid.NewGuid();
        var product = new ProductEntity { Id = productId, Name = "Fallback Latte", BasePrice = 50_000m };
        var mockData = new List<ProductEntity> { product }.BuildMock();

        // Cache returns null (as ProductCatalogCache does when Redis fails)
        _catalogCache.Setup(c => c.GetDetailAsync(productId)).ReturnsAsync((ProductDto?)null);
        _productsRepo.Setup(r => r.FindByCondition(
                It.IsAny<System.Linq.Expressions.Expression<Func<ProductEntity, bool>>>(), false))
            .Returns(mockData);
        _mapper.Setup(m => m.Map<ProductDto>(product))
            .Returns(new ProductDto { Id = productId, Name = "Fallback Latte", Variants = new() });

        var result = await _handler.Handle(new GetProductByIdQuery(productId), CancellationToken.None);

        result.Should().NotBeNull();
        result.Id.Should().Be(productId);
    }
}
