using AutoMapper;
using FluentAssertions;
using MockQueryable;
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
    private readonly Mock<IOrderRepository> _ordersRepo = new();
    private readonly Mock<IMapper> _mapper = new();
    private readonly GetPaginatedProductsQueryHandler _handler;

    public GetPaginatedProductsQueryHandlerTests()
    {
        _unitOfWork.Setup(u => u.Products).Returns(_productsRepo.Object);
        _unitOfWork.Setup(u => u.Orders).Returns(_ordersRepo.Object);
        _handler = new GetPaginatedProductsQueryHandler(_unitOfWork.Object, _mapper.Object);
    }

    [Fact]
    public async Task Handle_WithProducts_ReturnsPaginatedResult()
    {
        var product1 = new ProductEntity { Id = Guid.NewGuid(), Name = "Latte", DisplayOrder = 1 };
        var product2 = new ProductEntity { Id = Guid.NewGuid(), Name = "Espresso", DisplayOrder = 2 };
        var products = new List<ProductEntity> { product1, product2 }.BuildMock();

        _productsRepo.Setup(r => r.FindAll(false)).Returns(products);
        _mapper.Setup(m => m.Map<ProductSummaryDto>(It.IsAny<ProductEntity>()))
            .Returns((ProductEntity p) => new ProductSummaryDto { Name = p.Name });
        _ordersRepo.Setup(r => r.GetSoldQuantitiesByProductIdsAsync(It.IsAny<IEnumerable<Guid>>()))
            .ReturnsAsync(new Dictionary<Guid, int>());

        var query = new GetPaginatedProductsQuery(new ProductPaginationFilter { TakeAll = true });
        var result = await _handler.Handle(query, CancellationToken.None);

        result.Should().NotBeNull();
        result.Items.Should().HaveCount(2);
        result.Items.Select(i => i.Name).Should().Contain("Latte").And.Contain("Espresso");
    }

    [Fact]
    public async Task Handle_EmptyProducts_ReturnsEmptyPagination()
    {
        _productsRepo.Setup(r => r.FindAll(false)).Returns(new List<ProductEntity>().BuildMock());
        _mapper.Setup(m => m.Map<ProductSummaryDto>(It.IsAny<ProductEntity>()))
            .Returns(new ProductSummaryDto());
        _ordersRepo.Setup(r => r.GetSoldQuantitiesByProductIdsAsync(It.IsAny<IEnumerable<Guid>>()))
            .ReturnsAsync(new Dictionary<Guid, int>());

        var query = new GetPaginatedProductsQuery(new ProductPaginationFilter { TakeAll = true });
        var result = await _handler.Handle(query, CancellationToken.None);

        result.Should().NotBeNull();
        result.Items.Should().BeEmpty();
        result.Metadata.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task Handle_WithOrders_ReturnsProductsWithCorrectQuantitySold()
    {
        var productId = Guid.NewGuid();
        var product = new ProductEntity { Id = productId, Name = "Latte", DisplayOrder = 1 };
        var products = new List<ProductEntity> { product }.BuildMock();

        var soldCounts = new Dictionary<Guid, int> { { productId, 42 } };

        _productsRepo.Setup(r => r.FindAll(false)).Returns(products);
        _mapper.Setup(m => m.Map<ProductSummaryDto>(It.IsAny<ProductEntity>()))
            .Returns((ProductEntity p) => new ProductSummaryDto { Id = p.Id, Name = p.Name });
        _ordersRepo.Setup(r => r.GetSoldQuantitiesByProductIdsAsync(It.IsAny<IEnumerable<Guid>>()))
            .ReturnsAsync(soldCounts);

        var query = new GetPaginatedProductsQuery(new ProductPaginationFilter { TakeAll = true });
        var result = await _handler.Handle(query, CancellationToken.None);

        result.Items.Should().HaveCount(1);
        result.Items[0].QuantitySold.Should().Be(42);
    }

    [Fact]
    public async Task Handle_WithNoOrders_ReturnsProductsWithZeroQuantitySold()
    {
        var productId = Guid.NewGuid();
        var product = new ProductEntity { Id = productId, Name = "Latte", DisplayOrder = 1 };
        var products = new List<ProductEntity> { product }.BuildMock();

        _productsRepo.Setup(r => r.FindAll(false)).Returns(products);
        _mapper.Setup(m => m.Map<ProductSummaryDto>(It.IsAny<ProductEntity>()))
            .Returns((ProductEntity p) => new ProductSummaryDto { Id = p.Id, Name = p.Name });
        _ordersRepo.Setup(r => r.GetSoldQuantitiesByProductIdsAsync(It.IsAny<IEnumerable<Guid>>()))
            .ReturnsAsync(new Dictionary<Guid, int>());

        var query = new GetPaginatedProductsQuery(new ProductPaginationFilter { TakeAll = true });
        var result = await _handler.Handle(query, CancellationToken.None);

        result.Items.Should().HaveCount(1);
        result.Items[0].QuantitySold.Should().Be(0);
    }

    [Fact]
    public async Task Handle_WithMultipleProducts_ReturnsSeparateSoldCountsPerProduct()
    {
        var productId1 = Guid.NewGuid();
        var productId2 = Guid.NewGuid();
        var product1 = new ProductEntity { Id = productId1, Name = "Latte", DisplayOrder = 1 };
        var product2 = new ProductEntity { Id = productId2, Name = "Espresso", DisplayOrder = 2 };
        var products = new List<ProductEntity> { product1, product2 }.BuildMock();

        var soldCounts = new Dictionary<Guid, int>
        {
            { productId1, 42 },
            { productId2, 15 }
        };

        _productsRepo.Setup(r => r.FindAll(false)).Returns(products);
        _mapper.Setup(m => m.Map<ProductSummaryDto>(It.IsAny<ProductEntity>()))
            .Returns((ProductEntity p) => new ProductSummaryDto { Id = p.Id, Name = p.Name });
        _ordersRepo.Setup(r => r.GetSoldQuantitiesByProductIdsAsync(It.IsAny<IEnumerable<Guid>>()))
            .ReturnsAsync(soldCounts);

        var query = new GetPaginatedProductsQuery(new ProductPaginationFilter { TakeAll = true });
        var result = await _handler.Handle(query, CancellationToken.None);

        result.Items.Should().HaveCount(2);
        result.Items[0].QuantitySold.Should().Be(42);
        result.Items[1].QuantitySold.Should().Be(15);
    }
}
