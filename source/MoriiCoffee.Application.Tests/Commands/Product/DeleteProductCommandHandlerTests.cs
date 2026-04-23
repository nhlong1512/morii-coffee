using FluentAssertions;
using Moq;
using MoriiCoffee.Application.Commands.Product.DeleteProduct;
using MoriiCoffee.Application.SeedWork.Abstractions;
using MoriiCoffee.Application.SeedWork.Exceptions;
using MoriiCoffee.Domain.Repositories;
using MoriiCoffee.Domain.SeedWork.Persistence;
using Xunit;
using ProductEntity = MoriiCoffee.Domain.Aggregates.ProductAggregate.Product;

namespace MoriiCoffee.Application.Tests.Commands.Product;

public class DeleteProductCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IProductsRepository> _productsRepo = new();
    private readonly Mock<IProductCatalogCache> _catalogCache = new();
    private readonly DeleteProductCommandHandler _handler;

    public DeleteProductCommandHandlerTests()
    {
        _unitOfWork.Setup(u => u.Products).Returns(_productsRepo.Object);
        _catalogCache.Setup(c => c.InvalidateProductAsync(It.IsAny<Guid>())).Returns(Task.CompletedTask);
        _catalogCache.Setup(c => c.InvalidateAllListsAsync()).Returns(Task.CompletedTask);
        _handler = new DeleteProductCommandHandler(_unitOfWork.Object, _catalogCache.Object);
    }

    [Fact]
    public async Task Handle_Success_SoftDeletesAndReturnsTrue()
    {
        var productId = Guid.NewGuid();
        var product = new ProductEntity { Id = productId, Name = "Iced Latte" };
        _productsRepo.Setup(r => r.GetByIdAsync(productId)).ReturnsAsync(product);
        _productsRepo.Setup(r => r.SoftDelete(product)).Returns(Task.CompletedTask);
        _unitOfWork.Setup(u => u.CommitAsync()).ReturnsAsync(1);

        var result = await _handler.Handle(new DeleteProductCommand(productId), CancellationToken.None);

        result.Should().BeTrue();
        _productsRepo.Verify(r => r.SoftDelete(product), Times.Once);
        _unitOfWork.Verify(u => u.CommitAsync(), Times.Once);
    }

    [Fact]
    public async Task Handle_ProductNotFound_ThrowsNotFoundException()
    {
        var productId = Guid.NewGuid();
        _productsRepo.Setup(r => r.GetByIdAsync(productId)).ReturnsAsync((ProductEntity)null!);

        await _handler.Invoking(h => h.Handle(new DeleteProductCommand(productId), CancellationToken.None))
            .Should().ThrowAsync<NotFoundException>();
    }
}
