using AutoMapper;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Moq;
using MoriiCoffee.Application.Commands.Product.UploadProductImages;
using MoriiCoffee.Application.SeedWork.Abstractions;
using MoriiCoffee.Application.SeedWork.DTOs.File;
using MoriiCoffee.Application.SeedWork.DTOs.ProductImage;
using MoriiCoffee.Application.SeedWork.Exceptions;
using MoriiCoffee.Domain.Aggregates.ProductAggregate.Entities;
using MoriiCoffee.Domain.Repositories;
using MoriiCoffee.Domain.SeedWork.Persistence;
using Xunit;
using ProductEntity = MoriiCoffee.Domain.Aggregates.ProductAggregate.Product;

namespace MoriiCoffee.Application.Tests.Commands.Product;

public class UploadProductImagesCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IProductsRepository> _productsRepo = new();
    private readonly Mock<IProductImagesRepository> _imagesRepo = new();
    private readonly Mock<IFileService> _fileService = new();
    private readonly Mock<IMapper> _mapper = new();
    private readonly UploadProductImagesCommandHandler _handler;

    public UploadProductImagesCommandHandlerTests()
    {
        _unitOfWork.Setup(u => u.Products).Returns(_productsRepo.Object);
        _unitOfWork.Setup(u => u.ProductImages).Returns(_imagesRepo.Object);
        _unitOfWork.Setup(u => u.ExecuteInTransactionAsync(It.IsAny<Func<Task>>()))
            .Returns((Func<Task> op) => op());
        _handler = new UploadProductImagesCommandHandler(_unitOfWork.Object, _fileService.Object, _mapper.Object);
    }

    [Fact]
    public async Task Handle_Success_UploadsFilesAndReturnsImageDtos()
    {
        var productId = Guid.NewGuid();
        var product = new ProductEntity { Id = productId, Name = "Iced Latte", ThumbnailUrl = "existing-key.jpg" };
        _productsRepo.Setup(r => r.GetByIdAsync(productId)).ReturnsAsync(product);
        _imagesRepo.Setup(r => r.CountByProductIdAsync(productId)).ReturnsAsync(0);
        _imagesRepo.Setup(r => r.CreateAsync(It.IsAny<ProductImage>())).Returns(Task.CompletedTask);

        var fileMock = new Mock<IFormFile>();
        fileMock.Setup(f => f.FileName).Returns("photo.jpg");
        var blobResponse = new BlobResponseDto { Blob = new BlobDto { StorageKey = "products/abc/123-photo.jpg" } };
        _fileService.Setup(f => f.UploadAsync(It.IsAny<IFormFile>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(blobResponse);
        _mapper.Setup(m => m.Map<ProductImageDto>(It.IsAny<ProductImage>()))
            .Returns(new ProductImageDto { Url = "https://cdn.test/products/abc/123-photo.jpg" });

        var cmd = new UploadProductImagesCommand(productId, new List<IFormFile> { fileMock.Object });
        var result = await _handler.Handle(cmd, CancellationToken.None);

        result.Should().HaveCount(1);
        _fileService.Verify(f => f.UploadAsync(It.IsAny<IFormFile>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ProductHasNoThumbnail_SetsThumbnailFromFirstImage()
    {
        var productId = Guid.NewGuid();
        var product = new ProductEntity { Id = productId, Name = "Iced Latte", ThumbnailUrl = null };
        _productsRepo.Setup(r => r.GetByIdAsync(productId)).ReturnsAsync(product);
        _productsRepo.Setup(r => r.Update(product)).Returns(Task.CompletedTask);
        _imagesRepo.Setup(r => r.CountByProductIdAsync(productId)).ReturnsAsync(0);
        _imagesRepo.Setup(r => r.CreateAsync(It.IsAny<ProductImage>())).Returns(Task.CompletedTask);

        var fileMock = new Mock<IFormFile>();
        fileMock.Setup(f => f.FileName).Returns("photo.jpg");
        const string storageKey = "products/abc/123-photo.jpg";
        var blobResponse = new BlobResponseDto { Blob = new BlobDto { StorageKey = storageKey } };
        _fileService.Setup(f => f.UploadAsync(It.IsAny<IFormFile>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(blobResponse);
        _mapper.Setup(m => m.Map<ProductImageDto>(It.IsAny<ProductImage>()))
            .Returns(new ProductImageDto { Url = storageKey });

        var cmd = new UploadProductImagesCommand(productId, new List<IFormFile> { fileMock.Object });
        await _handler.Handle(cmd, CancellationToken.None);

        product.ThumbnailUrl.Should().Be(storageKey);
        _productsRepo.Verify(r => r.Update(product), Times.Once);
    }

    [Fact]
    public async Task Handle_ProductAlreadyHasThumbnail_DoesNotOverrideThumbnail()
    {
        var productId = Guid.NewGuid();
        const string existingThumbnail = "products/abc/old-thumb.jpg";
        var product = new ProductEntity { Id = productId, Name = "Iced Latte", ThumbnailUrl = existingThumbnail };
        _productsRepo.Setup(r => r.GetByIdAsync(productId)).ReturnsAsync(product);
        _imagesRepo.Setup(r => r.CountByProductIdAsync(productId)).ReturnsAsync(1);
        _imagesRepo.Setup(r => r.CreateAsync(It.IsAny<ProductImage>())).Returns(Task.CompletedTask);

        var fileMock = new Mock<IFormFile>();
        fileMock.Setup(f => f.FileName).Returns("new-photo.jpg");
        var blobResponse = new BlobResponseDto { Blob = new BlobDto { StorageKey = "products/abc/456-new-photo.jpg" } };
        _fileService.Setup(f => f.UploadAsync(It.IsAny<IFormFile>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(blobResponse);
        _mapper.Setup(m => m.Map<ProductImageDto>(It.IsAny<ProductImage>()))
            .Returns(new ProductImageDto { Url = "products/abc/456-new-photo.jpg" });

        var cmd = new UploadProductImagesCommand(productId, new List<IFormFile> { fileMock.Object });
        await _handler.Handle(cmd, CancellationToken.None);

        product.ThumbnailUrl.Should().Be(existingThumbnail);
        _productsRepo.Verify(r => r.Update(It.IsAny<ProductEntity>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ExceedsMaxImages_ThrowsBadRequestException()
    {
        var productId = Guid.NewGuid();
        var product = new ProductEntity { Id = productId, Name = "Iced Latte" };
        _productsRepo.Setup(r => r.GetByIdAsync(productId)).ReturnsAsync(product);
        _imagesRepo.Setup(r => r.CountByProductIdAsync(productId)).ReturnsAsync(9);

        var files = Enumerable.Range(0, 2).Select(_ =>
        {
            var f = new Mock<IFormFile>();
            f.Setup(x => x.FileName).Returns("photo.jpg");
            return f.Object;
        }).ToList();

        var cmd = new UploadProductImagesCommand(productId, files);

        await _handler.Invoking(h => h.Handle(cmd, CancellationToken.None))
            .Should().ThrowAsync<BadRequestException>();
    }
}
