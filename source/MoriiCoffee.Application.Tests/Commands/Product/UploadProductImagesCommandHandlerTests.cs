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
        var product = new ProductEntity { Id = productId, Name = "Iced Latte" };
        _productsRepo.Setup(r => r.GetByIdAsync(productId)).ReturnsAsync(product);
        _imagesRepo.Setup(r => r.CountByProductIdAsync(productId)).ReturnsAsync(0);
        _imagesRepo.Setup(r => r.CreateAsync(It.IsAny<ProductImage>())).Returns(Task.CompletedTask);

        var fileMock = new Mock<IFormFile>();
        fileMock.Setup(f => f.FileName).Returns("photo.jpg");
        var blobResponse = new BlobResponseDto { Blob = new BlobDto { Uri = "https://cdn.test/photo.jpg", Name = "photo.jpg" } };
        _fileService.Setup(f => f.UploadAsync(It.IsAny<IFormFile>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(blobResponse);
        _mapper.Setup(m => m.Map<ProductImageDto>(It.IsAny<ProductImage>()))
            .Returns(new ProductImageDto { Url = "https://cdn.test/photo.jpg" });

        var cmd = new UploadProductImagesCommand(productId, new List<IFormFile> { fileMock.Object });
        var result = await _handler.Handle(cmd, CancellationToken.None);

        result.Should().HaveCount(1);
        _fileService.Verify(f => f.UploadAsync(It.IsAny<IFormFile>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
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
