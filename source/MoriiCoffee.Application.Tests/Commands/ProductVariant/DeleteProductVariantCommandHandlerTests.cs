using FluentAssertions;
using Moq;
using MoriiCoffee.Application.Commands.ProductVariant.DeleteProductVariant;
using MoriiCoffee.Application.SeedWork.Exceptions;
using MoriiCoffee.Domain.Repositories;
using MoriiCoffee.Domain.SeedWork.Persistence;
using Xunit;

namespace MoriiCoffee.Application.Tests.Commands.ProductVariant;

public class DeleteProductVariantCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IProductVariantsRepository> _variantsRepo = new();
    private readonly DeleteProductVariantCommandHandler _handler;

    public DeleteProductVariantCommandHandlerTests()
    {
        _unitOfWork.Setup(u => u.ProductVariants).Returns(_variantsRepo.Object);
        _handler = new DeleteProductVariantCommandHandler(_unitOfWork.Object);
    }

    [Fact]
    public async Task Handle_Success_SoftDeletesAndReturnsTrue()
    {
        var variantId = Guid.NewGuid();
        var variant = new MoriiCoffee.Domain.Aggregates.ProductAggregate.Entities.ProductVariant
            { Id = variantId };
        _variantsRepo.Setup(r => r.GetByIdAsync(variantId)).ReturnsAsync(variant);
        _variantsRepo.Setup(r => r.SoftDelete(variant)).Returns(Task.CompletedTask);
        _unitOfWork.Setup(u => u.CommitAsync()).ReturnsAsync(1);

        var result = await _handler.Handle(
            new DeleteProductVariantCommand(variantId), CancellationToken.None);

        result.Should().BeTrue();
        _variantsRepo.Verify(r => r.SoftDelete(variant), Times.Once);
        _unitOfWork.Verify(u => u.CommitAsync(), Times.Once);
    }

    [Fact]
    public async Task Handle_VariantNotFound_ThrowsNotFoundException()
    {
        var variantId = Guid.NewGuid();
        _variantsRepo.Setup(r => r.GetByIdAsync(variantId))
            .ReturnsAsync((MoriiCoffee.Domain.Aggregates.ProductAggregate.Entities.ProductVariant)null!);

        await _handler.Invoking(h => h.Handle(new DeleteProductVariantCommand(variantId), CancellationToken.None))
            .Should().ThrowAsync<NotFoundException>();
    }
}
