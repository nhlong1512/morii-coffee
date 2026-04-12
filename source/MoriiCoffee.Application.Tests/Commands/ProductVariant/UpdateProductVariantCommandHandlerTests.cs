using AutoMapper;
using FluentAssertions;
using Moq;
using MoriiCoffee.Application.Commands.ProductVariant.UpdateProductVariant;
using MoriiCoffee.Application.SeedWork.DTOs.ProductVariant;
using MoriiCoffee.Application.SeedWork.Exceptions;
using MoriiCoffee.Domain.Repositories;
using MoriiCoffee.Domain.Shared.Enums.Product;
using MoriiCoffee.Domain.SeedWork.Persistence;
using Xunit;
using ProductEntity = MoriiCoffee.Domain.Aggregates.ProductAggregate.Product;
using ProductVariantEntity = MoriiCoffee.Domain.Aggregates.ProductAggregate.Entities.ProductVariant;

namespace MoriiCoffee.Application.Tests.Commands.ProductVariant;

public class UpdateProductVariantCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IProductVariantsRepository> _variantsRepo = new();
    private readonly Mock<IProductsRepository> _productsRepo = new();
    private readonly Mock<IMapper> _mapper = new();
    private readonly UpdateProductVariantCommandHandler _handler;

    public UpdateProductVariantCommandHandlerTests()
    {
        _unitOfWork.Setup(u => u.ProductVariants).Returns(_variantsRepo.Object);
        _unitOfWork.Setup(u => u.Products).Returns(_productsRepo.Object);
        _handler = new UpdateProductVariantCommandHandler(_unitOfWork.Object, _mapper.Object);
    }

    [Fact]
    public async Task Handle_Success_UpdatesAndReturnsDto()
    {
        var variantId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var variant = new ProductVariantEntity
        {
            Id = variantId,
            ProductId = productId,
            Name = "Small",
            Size = EProductSize.Small,
            AdditionalPrice = 0m,
            IsDefault = false
        };
        var product = new ProductEntity { Id = productId, BasePrice = 50_000m };
        var cmd = new UpdateProductVariantCommand(variantId, new UpdateProductVariantDto
        {
            Name = "Large",
            Size = EProductSize.Large,
            AdditionalPrice = 15_000m,
            IsDefault = false,
            IsAvailable = true
        });

        _variantsRepo.Setup(r => r.GetByIdAsync(variantId)).ReturnsAsync(variant);
        _productsRepo.Setup(r => r.GetByIdAsync(productId)).ReturnsAsync(product);
        _variantsRepo.Setup(r => r.Update(variant)).Returns(Task.CompletedTask);
        _unitOfWork.Setup(u => u.CommitAsync()).ReturnsAsync(1);
        _mapper.Setup(m => m.Map<ProductVariantDto>(variant)).Returns(new ProductVariantDto { Name = "Large" });

        var result = await _handler.Handle(cmd, CancellationToken.None);

        result.Should().NotBeNull();
        _unitOfWork.Verify(u => u.CommitAsync(), Times.Once);
    }

    [Fact]
    public async Task Handle_VariantNotFound_ThrowsNotFoundException()
    {
        var variantId = Guid.NewGuid();
        _variantsRepo.Setup(r => r.GetByIdAsync(variantId))
            .ReturnsAsync((ProductVariantEntity)null!);

        var cmd = new UpdateProductVariantCommand(variantId, new UpdateProductVariantDto
            { Name = "Large", Size = EProductSize.Large });

        await _handler.Invoking(h => h.Handle(cmd, CancellationToken.None))
            .Should().ThrowAsync<NotFoundException>();
    }
}
