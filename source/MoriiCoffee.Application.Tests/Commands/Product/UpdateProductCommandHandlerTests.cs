using AutoMapper;
using FluentAssertions;
using Moq;
using MoriiCoffee.Application.Commands.Product.UpdateProduct;
using MoriiCoffee.Application.SeedWork.Abstractions;
using MoriiCoffee.Application.SeedWork.DTOs.Product;
using MoriiCoffee.Application.SeedWork.Exceptions;
using MoriiCoffee.Domain.Aggregates.ProductAggregate.ValueObjects;
using MoriiCoffee.Domain.Repositories;
using MoriiCoffee.Domain.Shared.Enums.Product;
using MoriiCoffee.Domain.SeedWork.Persistence;
using Xunit;
using CategoryEntity = MoriiCoffee.Domain.Aggregates.CategoryAggregate.Category;
using ProductEntity = MoriiCoffee.Domain.Aggregates.ProductAggregate.Product;

namespace MoriiCoffee.Application.Tests.Commands.Product;

public class UpdateProductCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IProductsRepository> _productsRepo = new();
    private readonly Mock<ICategoriesRepository> _categoriesRepo = new();
    private readonly Mock<IFileService> _fileService = new();
    private readonly Mock<IMapper> _mapper = new();
    private readonly UpdateProductCommandHandler _handler;

    public UpdateProductCommandHandlerTests()
    {
        _unitOfWork.Setup(u => u.Products).Returns(_productsRepo.Object);
        _unitOfWork.Setup(u => u.Categories).Returns(_categoriesRepo.Object);
        _handler = new UpdateProductCommandHandler(_unitOfWork.Object, _fileService.Object, _mapper.Object);
    }

    private static UpdateProductCommand ValidCommand(Guid productId, Guid categoryId) =>
        new(productId, new UpdateProductDto
        {
            Name = "Updated Latte",
            BasePrice = 60_000m,
            CategoryIds = new List<Guid> { categoryId },
            Status = EProductStatus.Active,
            DisplayOrder = 0
        });

    [Fact]
    public async Task Handle_Success_ReturnsProductDto()
    {
        var productId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var product = new ProductEntity
        {
            Id = productId,
            Name = "Iced Latte",
            Slug = "iced-latte",
            BasePrice = 55_000m,
            ProductCategories = new List<ProductCategory>()
        };
        var category = new CategoryEntity { Id = categoryId, Name = "Coffee" };
        var cmd = ValidCommand(productId, categoryId);

        _productsRepo.Setup(r => r.GetByIdAsync(productId, It.IsAny<System.Linq.Expressions.Expression<Func<ProductEntity, object>>>()))
            .ReturnsAsync(product);
        _categoriesRepo.Setup(r => r.GetByIdAsync(categoryId)).ReturnsAsync(category);
        _productsRepo.Setup(r => r.SlugExistsAsync(It.IsAny<string>(), productId)).ReturnsAsync(false);
        _productsRepo.Setup(r => r.Update(product)).Returns(Task.CompletedTask);
        _unitOfWork.Setup(u => u.CommitAsync()).ReturnsAsync(1);
        _mapper.Setup(m => m.Map<ProductDto>(product)).Returns(new ProductDto { Name = "Updated Latte" });

        var result = await _handler.Handle(cmd, CancellationToken.None);

        result.Should().NotBeNull();
        result.Name.Should().Be("Updated Latte");
        _unitOfWork.Verify(u => u.CommitAsync(), Times.Once);
    }

    [Fact]
    public async Task Handle_ProductNotFound_ThrowsNotFoundException()
    {
        var productId = Guid.NewGuid();
        var cmd = ValidCommand(productId, Guid.NewGuid());
        _productsRepo.Setup(r => r.GetByIdAsync(productId, It.IsAny<System.Linq.Expressions.Expression<Func<ProductEntity, object>>>()))
            .ReturnsAsync((ProductEntity)null!);

        await _handler.Invoking(h => h.Handle(cmd, CancellationToken.None))
            .Should().ThrowAsync<NotFoundException>();
    }
}
