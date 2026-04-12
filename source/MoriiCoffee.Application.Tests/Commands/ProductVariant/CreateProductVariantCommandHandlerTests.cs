using AutoMapper;
using FluentAssertions;
using Moq;
using MoriiCoffee.Application.Commands.ProductVariant.CreateProductVariant;
using MoriiCoffee.Application.SeedWork.DTOs.ProductVariant;
using MoriiCoffee.Application.SeedWork.Exceptions;
using MoriiCoffee.Domain.Aggregates.ProductAggregate.Entities;
using MoriiCoffee.Domain.Repositories;
using MoriiCoffee.Domain.Shared.Enums.Product;
using MoriiCoffee.Domain.SeedWork.Persistence;
using Xunit;
using ProductEntity = MoriiCoffee.Domain.Aggregates.ProductAggregate.Product;
using ProductVariantEntity = MoriiCoffee.Domain.Aggregates.ProductAggregate.Entities.ProductVariant;

namespace MoriiCoffee.Application.Tests.Commands.ProductVariant;

public class CreateProductVariantCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IProductsRepository> _productsRepo = new();
    private readonly Mock<IProductVariantsRepository> _variantsRepo = new();
    private readonly Mock<IMapper> _mapper = new();
    private readonly CreateProductVariantCommandHandler _handler;

    public CreateProductVariantCommandHandlerTests()
    {
        _unitOfWork.Setup(u => u.Products).Returns(_productsRepo.Object);
        _unitOfWork.Setup(u => u.ProductVariants).Returns(_variantsRepo.Object);
        _handler = new CreateProductVariantCommandHandler(_unitOfWork.Object, _mapper.Object);
    }

    [Fact]
    public async Task Handle_Success_CreatesVariantsAndReturnsDto()
    {
        var productId = Guid.NewGuid();
        var product = new ProductEntity { Id = productId, Name = "Iced Latte", BasePrice = 50_000m };
        _productsRepo.Setup(r => r.GetByIdAsync(productId)).ReturnsAsync(product);
        _variantsRepo.Setup(r => r.GetByProductIdAsync(productId))
            .ReturnsAsync(new List<ProductVariantEntity>());
        _variantsRepo.Setup(r => r.CreateAsync(It.IsAny<ProductVariantEntity>()))
            .Returns(Task.CompletedTask);
        _unitOfWork.Setup(u => u.CommitAsync()).ReturnsAsync(1);
        _mapper.Setup(m => m.Map<ProductVariantDto>(It.IsAny<ProductVariantEntity>()))
            .Returns(new ProductVariantDto { Name = "Medium", TotalPrice = 60_000m });

        var cmd = new CreateProductVariantCommand(productId,
            new List<CreateProductVariantDto>
            {
                new CreateProductVariantDto
                {
                    Name = "Medium",
                    Size = EProductSize.Medium,
                    AdditionalPrice = 10_000m,
                    IsDefault = true,
                    IsAvailable = true
                }
            });

        _variantsRepo.Setup(r => r.ClearDefaultFlagAsync(productId, null)).Returns(Task.CompletedTask);

        var result = await _handler.Handle(cmd, CancellationToken.None);

        result.Should().HaveCount(1);
        _unitOfWork.Verify(u => u.CommitAsync(), Times.Once);
    }

    [Fact]
    public async Task Handle_ProductNotFound_ThrowsNotFoundException()
    {
        var productId = Guid.NewGuid();
        _productsRepo.Setup(r => r.GetByIdAsync(productId)).ReturnsAsync((ProductEntity)null!);

        var cmd = new CreateProductVariantCommand(productId,
            new List<CreateProductVariantDto>
            {
                new CreateProductVariantDto { Name = "Medium", Size = EProductSize.Medium, AdditionalPrice = 0 }
            });

        await _handler.Invoking(h => h.Handle(cmd, CancellationToken.None))
            .Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_DuplicateSizeExists_ThrowsBadRequestException()
    {
        var productId = Guid.NewGuid();
        var product = new ProductEntity { Id = productId, Name = "Iced Latte", BasePrice = 50_000m };
        var existingVariant = new ProductVariantEntity { Id = Guid.NewGuid(), Size = EProductSize.Medium };
        _productsRepo.Setup(r => r.GetByIdAsync(productId)).ReturnsAsync(product);
        _variantsRepo.Setup(r => r.GetByProductIdAsync(productId))
            .ReturnsAsync(new List<ProductVariantEntity> { existingVariant });

        var cmd = new CreateProductVariantCommand(productId,
            new List<CreateProductVariantDto>
            {
                new CreateProductVariantDto { Name = "Medium", Size = EProductSize.Medium, AdditionalPrice = 0 }
            });

        await _handler.Invoking(h => h.Handle(cmd, CancellationToken.None))
            .Should().ThrowAsync<BadRequestException>();
    }
}
