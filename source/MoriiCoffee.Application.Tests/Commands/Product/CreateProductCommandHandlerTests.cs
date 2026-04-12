using AutoMapper;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Moq;
using MoriiCoffee.Application.Commands.Product.CreateProduct;
using MoriiCoffee.Application.SeedWork.Abstractions;
using MoriiCoffee.Application.SeedWork.DTOs.File;
using MoriiCoffee.Application.SeedWork.DTOs.Product;
using MoriiCoffee.Application.SeedWork.Exceptions;
using MoriiCoffee.Domain.Repositories;
using MoriiCoffee.Domain.SeedWork.Persistence;
using Xunit;
using CategoryEntity = MoriiCoffee.Domain.Aggregates.CategoryAggregate.Category;
using ProductEntity = MoriiCoffee.Domain.Aggregates.ProductAggregate.Product;
using ProductImageEntity = MoriiCoffee.Domain.Aggregates.ProductAggregate.Entities.ProductImage;

namespace MoriiCoffee.Application.Tests.Commands.Product;

public class CreateProductCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IProductsRepository> _productsRepo = new();
    private readonly Mock<ICategoriesRepository> _categoriesRepo = new();
    private readonly Mock<IProductImagesRepository> _imagesRepo = new();
    private readonly Mock<IFileService> _fileService = new();
    private readonly Mock<IMapper> _mapper = new();
    private readonly CreateProductCommandHandler _handler;

    public CreateProductCommandHandlerTests()
    {
        _unitOfWork.Setup(u => u.Products).Returns(_productsRepo.Object);
        _unitOfWork.Setup(u => u.Categories).Returns(_categoriesRepo.Object);
        _unitOfWork.Setup(u => u.ProductImages).Returns(_imagesRepo.Object);
        _handler = new CreateProductCommandHandler(_unitOfWork.Object, _fileService.Object, _mapper.Object);
    }

    private static CreateProductCommand CommandWithoutThumbnail(Guid categoryId) =>
        new(new CreateProductDto
        {
            Name = "Iced Latte",
            BasePrice = 55_000m,
            CategoryIds = new List<Guid> { categoryId }
        });

    [Fact]
    public async Task Handle_SuccessWithoutThumbnail_CommitsAndReturnsProductDto()
    {
        var categoryId = Guid.NewGuid();
        var category = new CategoryEntity { Id = categoryId, Name = "Coffee" };
        _categoriesRepo.Setup(r => r.GetByIdAsync(categoryId)).ReturnsAsync(category);
        _productsRepo.Setup(r => r.SlugExistsAsync(It.IsAny<string>(), null)).ReturnsAsync(false);
        _productsRepo.Setup(r => r.CreateAsync(It.IsAny<ProductEntity>())).Returns(Task.CompletedTask);
        _unitOfWork.Setup(u => u.CommitAsync()).ReturnsAsync(1);
        _mapper.Setup(m => m.Map<ProductDto>(It.IsAny<ProductEntity>()))
            .Returns(new ProductDto { Name = "Iced Latte" });

        var cmd = CommandWithoutThumbnail(categoryId);
        var result = await _handler.Handle(cmd, CancellationToken.None);

        result.Should().NotBeNull();
        result.Name.Should().Be("Iced Latte");
        _unitOfWork.Verify(u => u.CommitAsync(), Times.Once);
        _fileService.Verify(f => f.UploadAsync(It.IsAny<IFormFile>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Handle_SuccessWithThumbnail_UploadsFileAndCommits()
    {
        var categoryId = Guid.NewGuid();
        var category = new CategoryEntity { Id = categoryId, Name = "Coffee" };
        _categoriesRepo.Setup(r => r.GetByIdAsync(categoryId)).ReturnsAsync(category);
        _productsRepo.Setup(r => r.SlugExistsAsync(It.IsAny<string>(), null)).ReturnsAsync(false);
        _productsRepo.Setup(r => r.CreateAsync(It.IsAny<ProductEntity>())).Returns(Task.CompletedTask);
        _imagesRepo.Setup(r => r.CreateAsync(It.IsAny<ProductImageEntity>())).Returns(Task.CompletedTask);
        _unitOfWork.Setup(u => u.CommitAsync()).ReturnsAsync(1);

        var fileMock = new Mock<IFormFile>();
        fileMock.Setup(f => f.FileName).Returns("thumb.jpg");
        var blobResponse = new BlobResponseDto { Blob = new BlobDto { Uri = "https://cdn.test/thumb.jpg", Name = "thumb.jpg" } };
        _fileService.Setup(f => f.UploadAsync(It.IsAny<IFormFile>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(blobResponse);
        _mapper.Setup(m => m.Map<ProductDto>(It.IsAny<ProductEntity>()))
            .Returns(new ProductDto { Name = "Iced Latte" });

        var dto = new CreateProductDto
        {
            Name = "Iced Latte",
            BasePrice = 55_000m,
            CategoryIds = new List<Guid> { categoryId },
            Thumbnail = fileMock.Object
        };
        var cmd = new CreateProductCommand(dto);
        var result = await _handler.Handle(cmd, CancellationToken.None);

        result.Should().NotBeNull();
        _fileService.Verify(f => f.UploadAsync(It.IsAny<IFormFile>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task Handle_CategoryNotFound_ThrowsNotFoundException()
    {
        var categoryId = Guid.NewGuid();
        _categoriesRepo.Setup(r => r.GetByIdAsync(categoryId)).ReturnsAsync((CategoryEntity)null!);

        var cmd = CommandWithoutThumbnail(categoryId);

        await _handler.Invoking(h => h.Handle(cmd, CancellationToken.None))
            .Should().ThrowAsync<NotFoundException>();
    }
}
