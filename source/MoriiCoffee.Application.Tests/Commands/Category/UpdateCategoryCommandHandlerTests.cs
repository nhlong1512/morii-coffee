using AutoMapper;
using FluentAssertions;
using Moq;
using MoriiCoffee.Application.Commands.Category.UpdateCategory;
using MoriiCoffee.Application.SeedWork.Abstractions;
using MoriiCoffee.Application.SeedWork.DTOs.Category;
using MoriiCoffee.Application.SeedWork.Exceptions;
using MoriiCoffee.Domain.Repositories;
using MoriiCoffee.Domain.SeedWork.Persistence;
using Xunit;
using CategoryEntity = MoriiCoffee.Domain.Aggregates.CategoryAggregate.Category;

namespace MoriiCoffee.Application.Tests.Commands.Category;

public class UpdateCategoryCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ICategoriesRepository> _categoriesRepo = new();
    private readonly Mock<IFileService> _fileService = new();
    private readonly Mock<IMapper> _mapper = new();
    private readonly UpdateCategoryCommandHandler _handler;

    public UpdateCategoryCommandHandlerTests()
    {
        _unitOfWork.Setup(u => u.Categories).Returns(_categoriesRepo.Object);
        _handler = new UpdateCategoryCommandHandler(_unitOfWork.Object, _fileService.Object, _mapper.Object);
    }

    [Fact]
    public async Task Handle_Success_UpdatesAndReturnsCategoryDto()
    {
        var categoryId = Guid.NewGuid();
        var category = new CategoryEntity { Id = categoryId, Name = "Old Name" };
        var cmd = new UpdateCategoryCommand(categoryId, new UpdateCategoryDto
        {
            Name = "Cold Brew",
            DisplayOrder = 1,
            IsActive = true
        });

        _categoriesRepo.Setup(r => r.GetByIdAsync(categoryId)).ReturnsAsync(category);
        _categoriesRepo.Setup(r => r.GetByNameAsync("Cold Brew")).ReturnsAsync((CategoryEntity?)null);
        _categoriesRepo.Setup(r => r.Update(category)).Returns(Task.CompletedTask);
        _unitOfWork.Setup(u => u.CommitAsync()).ReturnsAsync(1);
        _mapper.Setup(m => m.Map<CategoryDto>(category)).Returns(new CategoryDto { Name = "Cold Brew" });

        var result = await _handler.Handle(cmd, CancellationToken.None);

        result.Should().NotBeNull();
        result.Name.Should().Be("Cold Brew");
        _unitOfWork.Verify(u => u.CommitAsync(), Times.Once);
    }

    [Fact]
    public async Task Handle_CategoryNotFound_ThrowsNotFoundException()
    {
        var categoryId = Guid.NewGuid();
        _categoriesRepo.Setup(r => r.GetByIdAsync(categoryId)).ReturnsAsync((CategoryEntity)null!);

        var cmd = new UpdateCategoryCommand(categoryId, new UpdateCategoryDto { Name = "X", DisplayOrder = 0 });

        await _handler.Invoking(h => h.Handle(cmd, CancellationToken.None))
            .Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_NameConflictWithOtherCategory_ThrowsBadRequestException()
    {
        var categoryId = Guid.NewGuid();
        var category = new CategoryEntity { Id = categoryId, Name = "Old Name" };
        var conflicting = new CategoryEntity { Id = Guid.NewGuid(), Name = "Cold Brew" };
        var cmd = new UpdateCategoryCommand(categoryId, new UpdateCategoryDto
        {
            Name = "Cold Brew",
            DisplayOrder = 1
        });

        _categoriesRepo.Setup(r => r.GetByIdAsync(categoryId)).ReturnsAsync(category);
        _categoriesRepo.Setup(r => r.GetByNameAsync("Cold Brew")).ReturnsAsync(conflicting);

        await _handler.Invoking(h => h.Handle(cmd, CancellationToken.None))
            .Should().ThrowAsync<BadRequestException>();
    }
}
