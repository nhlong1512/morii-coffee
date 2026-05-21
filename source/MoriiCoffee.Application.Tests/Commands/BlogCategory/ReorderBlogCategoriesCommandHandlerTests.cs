using FluentAssertions;
using Moq;
using MoriiCoffee.Application.Commands.BlogCategory.ReorderBlogCategories;
using MoriiCoffee.Application.SeedWork.DTOs.Blog;
using MoriiCoffee.Domain.Repositories;
using MoriiCoffee.Domain.SeedWork.Persistence;
using Xunit;
using BlogCategoryEntity = MoriiCoffee.Domain.Aggregates.BlogCategoryAggregate.BlogCategory;

namespace MoriiCoffee.Application.Tests.Commands.BlogCategory;

public class ReorderBlogCategoriesCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IBlogCategoriesRepository> _categoriesRepo = new();
    private readonly ReorderBlogCategoriesCommandHandler _handler;

    public ReorderBlogCategoriesCommandHandlerTests()
    {
        _unitOfWork.Setup(x => x.BlogCategories).Returns(_categoriesRepo.Object);
        _handler = new ReorderBlogCategoriesCommandHandler(_unitOfWork.Object);
    }

    [Fact]
    public async Task Handle_ValidItems_UpdatesOrders()
    {
        var category = BlogCategoryEntity.Create("Guides", "guides", null, 1, true);
        _categoriesRepo.Setup(x => x.GetByIdAsync(category.Id)).ReturnsAsync(category);
        _categoriesRepo.Setup(x => x.Update(category)).Returns(Task.CompletedTask);
        _unitOfWork.Setup(x => x.CommitAsync()).ReturnsAsync(1);

        var result = await _handler.Handle(new ReorderBlogCategoriesCommand(new ReorderBlogCategoriesDto
        {
            Items = new List<ReorderBlogCategoriesItemDto> { new() { Id = category.Id, DisplayOrder = 5 } }
        }), CancellationToken.None);

        result.Should().BeTrue();
        category.DisplayOrder.Should().Be(5);
    }
}
