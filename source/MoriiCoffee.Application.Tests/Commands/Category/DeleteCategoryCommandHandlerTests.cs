using FluentAssertions;
using Moq;
using MoriiCoffee.Application.Commands.Category.DeleteCategory;
using MoriiCoffee.Application.SeedWork.Exceptions;
using MoriiCoffee.Domain.Repositories;
using MoriiCoffee.Domain.SeedWork.Persistence;
using Xunit;
using CategoryEntity = MoriiCoffee.Domain.Aggregates.CategoryAggregate.Category;

namespace MoriiCoffee.Application.Tests.Commands.Category;

public class DeleteCategoryCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ICategoriesRepository> _categoriesRepo = new();
    private readonly DeleteCategoryCommandHandler _handler;

    public DeleteCategoryCommandHandlerTests()
    {
        _unitOfWork.Setup(u => u.Categories).Returns(_categoriesRepo.Object);
        _handler = new DeleteCategoryCommandHandler(_unitOfWork.Object);
    }

    [Fact]
    public async Task Handle_Success_SoftDeletesAndReturnsTrue()
    {
        var categoryId = Guid.NewGuid();
        var category = new CategoryEntity { Id = categoryId, Name = "Cold Brew" };
        _categoriesRepo.Setup(r => r.GetByIdAsync(categoryId)).ReturnsAsync(category);
        _categoriesRepo.Setup(r => r.SoftDelete(category)).Returns(Task.CompletedTask);
        _unitOfWork.Setup(u => u.CommitAsync()).ReturnsAsync(1);

        var result = await _handler.Handle(new DeleteCategoryCommand(categoryId), CancellationToken.None);

        result.Should().BeTrue();
        _categoriesRepo.Verify(r => r.SoftDelete(category), Times.Once);
        _unitOfWork.Verify(u => u.CommitAsync(), Times.Once);
    }

    [Fact]
    public async Task Handle_CategoryNotFound_ThrowsNotFoundException()
    {
        var categoryId = Guid.NewGuid();
        _categoriesRepo.Setup(r => r.GetByIdAsync(categoryId)).ReturnsAsync((CategoryEntity)null!);

        await _handler.Invoking(h => h.Handle(new DeleteCategoryCommand(categoryId), CancellationToken.None))
            .Should().ThrowAsync<NotFoundException>();
    }
}
