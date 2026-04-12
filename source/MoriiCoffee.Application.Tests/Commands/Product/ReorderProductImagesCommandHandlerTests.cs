using AutoMapper;
using FluentAssertions;
using Moq;
using MoriiCoffee.Application.Commands.Product.ReorderProductImages;
using MoriiCoffee.Application.SeedWork.DTOs.ProductImage;
using MoriiCoffee.Application.SeedWork.Exceptions;
using MoriiCoffee.Domain.Aggregates.ProductAggregate.Entities;
using MoriiCoffee.Domain.Repositories;
using MoriiCoffee.Domain.SeedWork.Persistence;
using Xunit;

namespace MoriiCoffee.Application.Tests.Commands.Product;

public class ReorderProductImagesCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IProductImagesRepository> _imagesRepo = new();
    private readonly Mock<IMapper> _mapper = new();
    private readonly ReorderProductImagesCommandHandler _handler;

    public ReorderProductImagesCommandHandlerTests()
    {
        _unitOfWork.Setup(u => u.ProductImages).Returns(_imagesRepo.Object);
        _handler = new ReorderProductImagesCommandHandler(_unitOfWork.Object, _mapper.Object);
    }

    [Fact]
    public async Task Handle_Success_ReordersImagesAndCommits()
    {
        var productId = Guid.NewGuid();
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();
        var images = new List<ProductImage>
        {
            new() { Id = id1, ProductId = productId, DisplayOrder = 1 },
            new() { Id = id2, ProductId = productId, DisplayOrder = 2 }
        };
        _imagesRepo.Setup(r => r.GetByProductIdAsync(productId)).ReturnsAsync(images);
        _imagesRepo.Setup(r => r.Update(It.IsAny<ProductImage>())).Returns(Task.CompletedTask);
        _unitOfWork.Setup(u => u.CommitAsync()).ReturnsAsync(1);
        _mapper.Setup(m => m.Map<ProductImageDto>(It.IsAny<ProductImage>()))
            .Returns((ProductImage img) => new ProductImageDto { DisplayOrder = img.DisplayOrder });

        var cmd = new ReorderProductImagesCommand(productId, new List<Guid> { id2, id1 });
        var result = await _handler.Handle(cmd, CancellationToken.None);

        result.Should().HaveCount(2);
        _unitOfWork.Verify(u => u.CommitAsync(), Times.Once);
    }

    [Fact]
    public async Task Handle_NoImagesFound_ThrowsNotFoundException()
    {
        var productId = Guid.NewGuid();
        _imagesRepo.Setup(r => r.GetByProductIdAsync(productId))
            .ReturnsAsync(new List<ProductImage>());

        var cmd = new ReorderProductImagesCommand(productId, new List<Guid> { Guid.NewGuid() });

        await _handler.Invoking(h => h.Handle(cmd, CancellationToken.None))
            .Should().ThrowAsync<NotFoundException>();
    }
}
