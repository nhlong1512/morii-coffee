using AutoMapper;
using FluentAssertions;
using Moq;
using MoriiCoffee.Application.Queries.Category.GetCategoryById;
using MoriiCoffee.Application.SeedWork.DTOs.Category;
using MoriiCoffee.Application.SeedWork.Exceptions;
using MoriiCoffee.Domain.Repositories;
using MoriiCoffee.Domain.SeedWork.Persistence;
using Xunit;
using CategoryEntity = MoriiCoffee.Domain.Aggregates.CategoryAggregate.Category;

namespace MoriiCoffee.Application.Tests.Queries.Category;

public class GetCategoryByIdQueryHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ICategoriesRepository> _categoriesRepo = new();
    private readonly Mock<IMapper> _mapper = new();
    private readonly GetCategoryByIdQueryHandler _handler;

    public GetCategoryByIdQueryHandlerTests()
    {
        _unitOfWork.Setup(u => u.Categories).Returns(_categoriesRepo.Object);
        _handler = new GetCategoryByIdQueryHandler(_unitOfWork.Object, _mapper.Object);
    }

    [Fact]
    public async Task Handle_CategoryFound_ReturnsCategoryDto()
    {
        var categoryId = Guid.NewGuid();
        var category = new CategoryEntity { Id = categoryId, Name = "Cold Brew" };
        _categoriesRepo.Setup(r => r.GetByIdAsync(categoryId)).ReturnsAsync(category);
        _mapper.Setup(m => m.Map<CategoryDto>(category)).Returns(new CategoryDto { Name = "Cold Brew" });

        var result = await _handler.Handle(new GetCategoryByIdQuery(categoryId), CancellationToken.None);

        result.Should().NotBeNull();
        result.Name.Should().Be("Cold Brew");
    }

    [Fact]
    public async Task Handle_CategoryNotFound_ThrowsNotFoundException()
    {
        var categoryId = Guid.NewGuid();
        _categoriesRepo.Setup(r => r.GetByIdAsync(categoryId)).ReturnsAsync((CategoryEntity)null!);

        await _handler.Invoking(h => h.Handle(new GetCategoryByIdQuery(categoryId), CancellationToken.None))
            .Should().ThrowAsync<NotFoundException>();
    }
}
